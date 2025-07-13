using Fusion;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Collections.Unicode;
public enum GameStatus
{
    Playing,        // ���� ���� �÷��� ��
    RoundEndWait,   // ��� ��� (�̵����� ���� ó��)
    RoundEnd,       // ��� ���� �ð�
    NextRound,      // ���� ���� �غ�
    GameEnd, // ��
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

// �� �÷��̾��� �� ���� ���
public struct RoundScoreData
{
    public int Strokes;     // ���� ��
    public int Rank;        // ���� ����
    public int Score;       // ���� ����
}

// ��ü ����� ���� �޸� ����
public class FinalScoreBoard
{
    public Dictionary<PlayerRef, Dictionary<int, RoundScoreData>> RoundScores
        = new(); // [Player][RoundIndex] �� ���� ����

    public Dictionary<PlayerRef, int> TotalScores
        = new(); // [Player] �� ���� ����

    public List<(PlayerRef player, int totalScore)> RankedByTotal
        = new(); // ���� ������ ���ĵ� ���� ����
}