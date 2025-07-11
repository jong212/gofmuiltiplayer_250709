using Fusion;
using UnityEngine;



/// ─────────────────────────────────────────────
/// 2. INetworkStruct 들
/// ─────────────────────────────────────────────
public struct RoundResults : INetworkStruct
{
    public int Strokes;
    public float TimeTaken;
}

public struct RoundData : INetworkStruct
{
    [Capacity(4)]//인원 몇명?
    public NetworkDictionary<PlayerRef, RoundResults> PlayerResults { get; }
}

/// ─────────────────────────────────────────────
/// 3. 결과 매니저 – NetworkBehaviour
/// ─────────────────────────────────────────────
public class RoundResult : NetworkBehaviour
{

    [Networked, Capacity(3)] // round
    public NetworkArray<RoundData> Rounds { get; }

    /*──────────────────────────────────────────
     * 결과 기록 : State-Authority(마스터) 전용
     *─────────────────────────────────────────*/
    public void RecordResult(int roundIndex, PlayerRef player, int strokes, float time)
    {
        if (!HasStateAuthority) return;

        // struct 값 꺼냄
        var rd = Rounds[roundIndex];

        // Dictionary에 저장
        rd.PlayerResults.Set(player, new RoundResults
        {
            Strokes = strokes,
            TimeTaken = time
        });

        // 다시 Set
        Rounds.Set(roundIndex, rd);
    }

    /*──────────────────────────────────────────
     * 결과 조회 : 아무 클라나 호출 가능
     *─────────────────────────────────────────*/
    public bool TryGetResult(int roundIdx,
                             PlayerRef player,
                             out RoundResults res)
    {

        res = default;

        if (roundIdx < 0 || roundIdx >= 3) return false;

        var dict = Rounds[roundIdx].PlayerResults;
        return dict.TryGet(player, out res);
    }
}
