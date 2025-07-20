using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class BaseCommonManager : NetworkBehaviour, IStateAuthorityChanged,  INetworkRunnerCallbacks
{
    [Networked, Capacity(6)]
    public NetworkDictionary<PlayerRef, Putter> ObjectByRef => default;
    public ChangeDetector _changeDetector;
    public Joystick JoystickInstance;


    public override void Spawned() //NetworkBehaviour 안에 virtual로 정의되어 잇어서 override (재정의) 했고 이거 자식도 override하면됨
    {
        // [플레이어 체크]
        Runner.AddCallbacks(this);  
        if (Runner.IsSharedModeMasterClient)
        {
            foreach (var player in Runner.ActivePlayers)
            {
                OnPlayerJoined(Runner, player);
            }
        }

        // [ 조이스틱 ]
        if (InterfaceManager.instance?.mainCanvas != null && JoystickInstance == null) 
        {
            JoystickInstance = GameObject.Instantiate(InterfaceManager.instance._joystick, InterfaceManager.instance.mainCanvas.transform);
        }

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

    }
  
    #region # 실시간 Player Join Callback
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!Runner.IsSharedModeMasterClient) return;

        StartCoroutine(WaitAndRegister(player));
    }
    private IEnumerator WaitAndRegister(PlayerRef player)
    {
        Debug.Log($"[WaitAndRegister] Waiting for PlayerRef: {player}");

        yield return new WaitUntil(() =>
            Runner != null &&
            player != null &&
            Runner.TryGetPlayerObject(player, out var _)
        );

        if (Runner.TryGetPlayerObject(player, out var obj) && obj != null)
        {
            if (obj.TryGetComponent<Putter>(out var putter) && !ObjectByRef.ContainsKey(player))
            {
                ObjectByRef.Set(player, putter);
                Debug.Log($"[WaitAndRegister] Registered putter for {player}");
            }
        }
        else
        {
            Debug.LogWarning($"[WaitAndRegister] Could not find PlayerObject for {player} even after wait.");
        }
    }

    #endregion

    #region # 실시간 Player Leave Callback
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) // 인터페이스의 구현이기 때문에 override x
    {
        if (!Object || !Object.HasStateAuthority) //여기에 마스터 클라인지 체크하는 부분으로 하면 안 된다고 함 why? 마스터클라나갈때 남은 클라들 잠시 true되는 문제가 있다고함;
            return;

        if (ObjectByRef.ContainsKey(player))
        {
            ObjectByRef.Remove(player);
        }
    }
    #endregion

    #region # 마스터 클라 권한 이전 후 실행로직
    public virtual void StateAuthorityChanged()
    {
        Debug.Log($"🔁 StateAuthorityChanged - Now I Have Authority? {Object.HasStateAuthority} | IsServer: {Runner.IsServer}");

        if (Runner.IsSharedModeMasterClient)
        {
            CleanupLeftPlayers();
        }
    }
    void CleanupLeftPlayers()
    {
        if (!Object.HasStateAuthority) return;

        List<PlayerRef> toRemove = new();

        foreach (var kvp in ObjectByRef)
        {
            if (!Runner.ActivePlayers.Contains(kvp.Key))
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var pRef in toRemove)
        {
            ObjectByRef.Remove(pRef);
            Debug.Log($"🧹 Cleanup: Removed disconnected player {pRef}");
        }
    }
    #endregion

    #region # INetworkRunnerCallbacks
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }    
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    #endregion
}
