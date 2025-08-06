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
    public bool ReadyToStart { get; private set; }


    [Networked, Capacity(4)]
    public NetworkDictionary<PlayerRef, LobbyPlayerStruct> Nicknames => default;


    // 🔹 게임 시작 대기용 타이머 (5초)
    [Networked] TickTimer StartGameTimer { get; set; }
    public override void Render()
    {
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.playerCount.text = PlayerCount.ToString() + " / 4";

            // 🔹 남은 시간 UI 표시 (옵션)
            if (StartGameTimer.IsRunning)
            {
                float remain = StartGameTimer.RemainingTime(Runner) ?? 0;
                LobbyManager.Instance.PlayerFullCountdown.text = $"{Mathf.CeilToInt(remain)}";
            }
            else
            {
                LobbyManager.Instance.PlayerFullCountdown.text = "";
            }
        }
    }

    public void Step11()
    {
        ReadyToStart = true;
    }
    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        // 1️⃣ 현재 인원 갱신
        PlayerCount = Runner.ActivePlayers.Count();

        // 2️⃣ 세션에서 빠진 유저 정리
        var toRemove = new List<PlayerRef>();
        foreach (var kv in Nicknames)
        {
            if (!Runner.ActivePlayers.Contains(kv.Key))
                toRemove.Add(kv.Key);
        }
        foreach (var key in toRemove)
            Nicknames.Remove(key);

        // 3️⃣ 게임 시작 조건 체크
        int maxPlayers = 2; // 방 최대 인원

        if (PlayerCount == maxPlayers)
        {
            // 타이머가 안 돌고 있으면 새로 시작
            if (!StartGameTimer.IsRunning)
            {
                StartGameTimer = TickTimer.CreateFromSeconds(Runner, 5);
                Runner.SessionInfo.IsOpen = false;   // 더 이상 Join 불가
                Runner.SessionInfo.IsVisible = false; // 로비에서도 숨김
            }
        }
        else
        {
            // 인원이 줄어들면 타이머 취소
            if (StartGameTimer.IsRunning)
            {
                StartGameTimer = TickTimer.None;
                Runner.SessionInfo.IsOpen = true;   // 다시 참여 가능
                Runner.SessionInfo.IsVisible = true;
            }
        }

        // 4️⃣ 타이머 만료 시 게임 시작
        if (StartGameTimer.IsRunning && StartGameTimer.Expired(Runner))
        {
            StartGameTimer = TickTimer.None;
            ManagerSystem.Instance.LobbySceneStepByCall("11_ChangeGameScene");
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
