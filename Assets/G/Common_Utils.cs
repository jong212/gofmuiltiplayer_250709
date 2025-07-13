using Fusion;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Collections.Unicode;
public enum GameStatus
{
    Playing,        // 실제 라운드 플레이 중
    RoundEndWait,   // 잠시 대기 (미도착자 점수 처리)
    RoundEnd,       // 결과 정리 시간
    NextRound,      // 다음 라운드 준비
    GameEnd, // 끝
}

public class Common_Utils 
{
    public static Putter GetMyPutter()
    {
        var myRef = GameManager.instance.Runner.LocalPlayer;

        if (GameManager.instance.ObjectByRef.TryGet(myRef, out var putter))
        {
            return putter;
        }
        return null;
    }
    public static int GetRemainingSecondsInt(TickTimer timer, NetworkRunner runner)
    {
        var remaining = timer.RemainingTime(runner);
        return remaining.HasValue ? Mathf.CeilToInt(remaining.Value) : -1;
    }
}

// 각 플레이어의 한 라운드 결과
public struct RoundScoreData
{
    public int Strokes;     // 퍼팅 수
    public int Rank;        // 도착 순위
    public int Score;       // 라운드 점수
}

// 전체 결과를 담을 메모리 구조
public class FinalScoreBoard
{
    public Dictionary<PlayerRef, Dictionary<int, RoundScoreData>> RoundScores
        = new(); // [Player][RoundIndex] → 개별 점수

    public Dictionary<PlayerRef, int> TotalScores
        = new(); // [Player] → 총합 점수

    public List<(PlayerRef player, int totalScore)> RankedByTotal
        = new(); // 총합 점수로 정렬된 최종 순위
}