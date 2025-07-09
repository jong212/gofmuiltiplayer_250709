using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using System.Linq;
public class Matchmaker : MonoBehaviour, INetworkRunnerCallbacks
{
	public static Matchmaker Instance { get; private set; }

	[SerializeField, ScenePath] string gameScene;
	public NetworkRunner runnerPrefab;
	public NetworkObject managerPrefab;
	public NetworkObject roomMngPrefab;

	// Memo1 
	public NetworkRunner Runner { get; private set; }

    Coroutine _joinRoutine;

	private void Awake()
	{
		if (Instance != null) { Destroy(gameObject); return; }
		Instance = this;
		DontDestroyOnLoad(gameObject);
	}

	private void OnDestroy()
	{
		if (Instance == this) Instance = null;
	}

	// 1-1 게임 시작 초반부 뭐 없음
	public void TryConnectShared()
	{
		TryConnectSharedSession(null);
	}

	public void TryConnectSharedSession(string sessionCode, System.Action successCallback = null)
	{
        _joinRoutine = StartCoroutine(ConnectSharedSessionRoutine(sessionCode, successCallback));
	}

	// 1-2 Runner가 있는지 체크하고 있으면 종료후 다시스폰 (겜도중 나온다음 다시 들어갈때 초기화 하는부분인듯)
	IEnumerator ConnectSharedSessionRoutine(string sessionCode, System.Action successCallback)
	{
		if (Runner) Runner.Shutdown();
       Runner = Instantiate(runnerPrefab);

        //1-3 이구간은 방판사람 컴퓨터에 게임매니저를 스폰하는 로직 이라고 보면 됨
        // 방판사람은 LoadScene(gameScene); 이걸통해 씬을 로드해서 게임방에 입장을 하고 마스터클라가 아닌사람은 그냥 StartGame 으로 마스터클라가 참여한 씬을 그냥 따라감
		// 씬이 로드 된 이후에 게임매니저 스폰되게 하기 위해 OnSceneLoadDene에 콜백을 건 것이고 마스터 클라 아닌 애들은 따로 겜매니저를 생성하지 않지만 씬에 참여하면 복제본 게임매니저가 Spawn을 대신 호출하긴 함

        NetworkEvents networkEvents = Runner.GetComponent<NetworkEvents>();

		void SpawnManager(NetworkRunner runner)
		{
			if (Runner.IsSharedModeMasterClient) {
                runner.Spawn(managerPrefab);
            }
			networkEvents.OnSceneLoadDone.RemoveListener(SpawnManager);
		}
		networkEvents.OnSceneLoadDone.AddListener(SpawnManager);

		Runner.AddCallbacks(this);
		//1-4 마스터클라는 아래서 따로 씬을 로드하고 일반 클리는 아래 Runner.StartGame 를 통해 씬 로드한다고 함
		Task<StartGameResult> task = Runner.StartGame(new StartGameArgs()
		{
			GameMode = GameMode.Shared,
			SessionName = null, //  << sessionCode 로 하면 친구끼리 이고 null 하면 랜덤방 매치메이킹
            SceneManager = Runner.GetComponent<INetworkSceneManager>(),
			ObjectProvider = Runner.GetComponent<INetworkObjectProvider>(),
		});

		while (!task.IsCompleted) yield return null;		
		StartGameResult result = task.Result;

        // 서버 연결 및 세션 참가 성공 → 이후 로직 진행 가능
        if (result.Ok)
		{
            //successCallback?.Invoke();
            Room_Mng roomManager = null;

            if (Runner.IsSharedModeMasterClient)
            {
                var roomObj = Runner.Spawn(roomMngPrefab);
                if (roomObj != null && roomObj.TryGetBehaviour<Room_Mng>(out var manager)) { roomManager = manager; }
				// 이거 예외처리 이렇게 한 이유는 이게 어쨋든 방인원이 찰 때 까지 대기하는 코루틴인데 memo2 번에 설명한 것처럼 interfacemanager 에서 shotdown 같은거 해서 네트워크가 끊기고 씬 이동 시키는데 moon_mng가 없는데 거기 잇엇던 네트워크 변수 업서진거에 참조하니까 터지는거임 그래서 IsValid 로 유효한지를 체크하는 것임
				yield return new WaitUntil
				(
					() =>
					roomManager &&                      
					roomManager.Object.IsValid &&      
					roomManager.ReadyToStart
				);

                Runner.LoadScene(gameScene);
            }
        }
	}
   



    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
	{
		Runner = null;
		if (_joinRoutine != null) { StopCoroutine(_joinRoutine); }
        if (shutdownReason == ShutdownReason.Ok)
		{
			SceneManager.LoadScene("Menu");
		}
	}

	#region INetworkRunnerCallbacks
	public void OnConnectedToServer(NetworkRunner runner) { }
	public void OnConnectFailed(NetworkRunner runner, Fusion.Sockets.NetAddress remoteAddress, Fusion.Sockets.NetConnectFailedReason reason) { }
	public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) {
        if (Runner.ActivePlayers.Count() >= 2)
        {
            request.Refuse(); // 인원 초과 - 참가 거절
            return;
        }

        request.Accept(); // 참가 허용
    }
	public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
	public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
	public void OnInput(NetworkRunner runner, NetworkInput input) { }
	public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
	public void OnSceneLoadStart(NetworkRunner runner) { }
	public void OnSceneLoadDone(NetworkRunner runner) { }
	public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
	public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
	public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
	public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
	public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
	public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
	public void OnDisconnectedFromServer(NetworkRunner runner, Fusion.Sockets.NetDisconnectReason reason) { }
	public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, Fusion.Sockets.ReliableKey key, System.ArraySegment<byte> data) { }
	public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, Fusion.Sockets.ReliableKey key, float progress) { }
	#endregion
}