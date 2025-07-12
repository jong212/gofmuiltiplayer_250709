using Fusion;
using UnityEngine;
using static Unity.Collections.Unicode;
public enum GameStatus
{
    Idle,           // �ʱ� ���
    Countdown,      // 3-2-1 ī��Ʈ�ٿ�
    Playing,        // ���� ���� �÷��� ��
    EndCountdown,   // ���� ī��Ʈ�ٿ�
    RoundEnd,       // ��� ���� �ð�
    NextRound,      // ���� ���� �غ�
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
