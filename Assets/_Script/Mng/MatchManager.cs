using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using System.Linq;
// Fusion 매치메이킹, 세션 관리 역할
public class MatchManager : MonoBehaviour, INetworkRunnerCallbacks
{

	public static MatchManager Instance { get; private set; }

    [SerializeField, ScenePath] string gameScene;
	public NetworkRunner runnerPrefab;
	public NetworkObject managerPrefab;
	public NetworkObject roomMngPrefab;

    public NetworkRunner Runner { get; private set; } // 현재 실행 중인 Runner

    Coroutine _joinRoutine;
    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

	public void Init()
	{
	
	}


    public void TryConnectShared()
	{
		TryConnectSharedSession(null);
	}

    // 세션에 참가 시도 (성공 시 콜백 실행 가능)
    public void TryConnectSharedSession(string sessionCode, System.Action successCallback = null)
	{
        _joinRoutine = StartCoroutine(ConnectSharedSessionRoutine(sessionCode, successCallback));
	}

    // 실제 세션 참가 루틴
    IEnumerator ConnectSharedSessionRoutine(string sessionCode, System.Action successCallback)
	{
        // 기존 Runner가 있으면 종료
		if (Runner) Runner.Shutdown();

        // 새로운 Runner 생성
        Runner = Instantiate(runnerPrefab);
 
        NetworkEvents networkEvents = Runner.GetComponent<NetworkEvents>();

        // 씬이 로드 완료되었을 때 호출할 콜백 등록
        void SpawnManager(NetworkRunner runner)
		{
            // 마스터 클라이언트만 GameManager를 스폰한다
            if (Runner.IsSharedModeMasterClient) {
                runner.Spawn(managerPrefab);
            }

            // 리스너 제거 (한 번만 실행되도록)
            networkEvents.OnSceneLoadDone.RemoveListener(SpawnManager);
		}
		networkEvents.OnSceneLoadDone.AddListener(SpawnManager);

		Runner.AddCallbacks(this);

        // [1] StartGame을 통해 세션에 참가하거나 없으면 새로 생성함 (생성한 클라는 자동으로 마스터클라 권한 생김)
        // 모든 클라이언트가 이 과정을 실행해야 세션에 참여 가능
        Task<StartGameResult> task = Runner.StartGame(new StartGameArgs()
		{
			GameMode = GameMode.Shared,
			SessionName = null, //  << sessionCode 로 하면 친구끼리 이고 null 하면 랜덤방 매치메이킹
            SceneManager = Runner.GetComponent<INetworkSceneManager>(),
			ObjectProvider = Runner.GetComponent<INetworkObjectProvider>(),
		});

		while (!task.IsCompleted) yield return null;		
		StartGameResult result = task.Result;

        // [2] 세션에 성공적으로 참가한 경우
        if (result.Ok)
		{
            //successCallback?.Invoke();
            Room_Mng roomManager = null;

            // [3] 마스터 클라이언트만 Room_Mng 객체를 직접 스폰
            if (Runner.IsSharedModeMasterClient)
            {
                // Room_Mng의 스폰 성공 여부 및 Ready 상태 대기
                var roomObj = Runner.Spawn(roomMngPrefab);
                if (roomObj != null && roomObj.TryGetBehaviour<Room_Mng>(out var manager)) { roomManager = manager; }
                ManagerSystem.Instance.LobbySceneStepByCall("10_GameStartBtnClick");

                // 방 입장 대기: roomManager가 유효하고 ReadyToStart 플래그가 true가 될 때까지 기다림
                yield return new WaitUntil
				(
					() =>
                    roomManager &&                       // Room_Mng 컴포넌트가 존재하고
                    roomManager.Object.IsValid &&        // 네트워크 오브젝트가 유효하며
                    roomManager.ReadyToStart             // 준비 완료 상태인 경우
                );
                // [4] 마스터 클라이언트만 씬 로드 → 일반 클라이언트는 자동으로 따라감
                Runner.LoadScene(gameScene);
            } else
            {
                ManagerSystem.Instance.LobbySceneStepByCall("10_GameStartBtnClick");
            }

        }
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
	{
		Runner = null;
        if (Room_Mng.instance != null) Room_Mng.instance = null;
		if (_joinRoutine != null) { StopCoroutine(_joinRoutine); }
        if (shutdownReason == ShutdownReason.Ok)
		{
			SceneManager.LoadScene("Lobby");
		}
	}
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
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