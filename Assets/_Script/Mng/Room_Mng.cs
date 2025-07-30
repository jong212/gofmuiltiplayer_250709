using Fusion;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

[Serializable]
public struct LobbyPlayerStruct : INetworkStruct
{
    public NetworkString<_32> Nick;   // 닉 최대 32 자
    public int CharId;
}

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
    public NetworkDictionary<PlayerRef, LobbyPlayerStruct> Nicknames => default;


    public override void Render()
    {
        if(LobbyManager.Instance != null) LobbyManager.Instance.playerCount.text = PlayerCount.ToString() + " / 4";
    }
 
    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        PlayerCount = Runner.ActivePlayers.Count();
        // Nicknames 정리: 세션에서 빠진 유저 제거
        var toRemove = new List<PlayerRef>();

        foreach (var kv in Nicknames)
        {
            if (!Runner.ActivePlayers.Contains(kv.Key))
                toRemove.Add(kv.Key);
        }

        foreach (var key in toRemove)
        {
            Nicknames.Remove(key);
        }

        // 예: 인원이 4명 이상일 때 게임 시작 조건 만족
        if ( PlayerCount >=4)
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
            Nicknames.Set(info.Source, new LobbyPlayerStruct
            {
                Nick = nick,
                CharId = charId
            });

    }
}
