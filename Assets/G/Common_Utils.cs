using Fusion;
using UnityEngine;
using static Unity.Collections.Unicode;
public enum GameStatus
{
    Idle,           // 초기 대기
    Countdown,      // 3-2-1 카운트다운
    Playing,        // 실제 라운드 플레이 중
    EndCountdown,   // 도착 카운트다운
    RoundEnd,       // 결과 정리 시간
    NextRound,      // 다음 라운드 준비
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
