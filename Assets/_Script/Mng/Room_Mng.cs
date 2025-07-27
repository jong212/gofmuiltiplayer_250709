using Fusion;
using System.Linq;
using Unity.Collections;
using UnityEngine;

public class Room_Mng : NetworkBehaviour
{
    public static Room_Mng instance;

    public override void Spawned()
    {
        if (instance == null)
            instance = this;
    }
    [Header("실시간 방 참여자 수 ")]
    [Networked] public int PlayerCount { get; private set; }    
    [Networked] public bool ReadyToStart { get; private set; }


    [Networked, Capacity(4)]
    public NetworkDictionary<PlayerRef, string> Nicknames => default;

    [Networked, Capacity(4)]
    public NetworkDictionary<PlayerRef, int> CharIdxArr => default;

    public override void Render()
    {
        if(LobbyManager.Instance != null) LobbyManager.Instance.playerCount.text = PlayerCount.ToString() + " / 4";
    }
 
    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        PlayerCount = Runner.ActivePlayers.Count();
        // 예: 인원이 4명 이상일 때 게임 시작 조건 만족
        if(  PlayerCount >=4)
        {
            ReadyToStart = true;
            Runner.SessionInfo.IsOpen = false; // 더 이상 Join 불가
            Runner.SessionInfo.IsVisible = false; // 로비·매치 리스트에서도 숨김
        }
    }
 
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SubmitNickname(string nick, int charId, RpcInfo info = default)
    {
        if (!Nicknames.ContainsKey(info.Source))
            Nicknames.Add(info.Source, nick);
            CharIdxArr.Add(info.Source, charId);        
    }
}
