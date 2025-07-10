using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Fusion;
using DG.Tweening;
using TMPro;

[DefaultExecutionOrder(-200)]
public class GameManager : BaseCommonManager
{
    #region # Singleton
    public static GameManager instance;

    private void Awake()
    {
        if (instance)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
    #endregion

    [Networked] TickTimer RoundEndTimer { get; set; }
    [Networked] int CurrentRound { get; set; }

    private int _prevCountdown = -999;

    public override void Spawned()
    {
        base.Spawned();
        InitializeGame();
    }

    private void InitializeGame()
    {
        Level.Load();
        _startCountdownSeconds = 3;

        if (Object.HasStateAuthority)
        {
            StartCoroutine(StartPreGameCountdown());
        }
    }

    #region # render
    public override void Render()
    {
        if (!PreGameCountdownTimer.IsRunning) return;

        RoundStartTimer();
        RoundEndTimers();
        TempEscape();
        CheckForChanges();
    }
    #endregion

    private void RoundStartTimer()
    {
        float remainingTime = PreGameCountdownTimer.RemainingTime(Runner).Value;
        int countdown = Mathf.CeilToInt(remainingTime);

        if (countdown != _prevCountdown)
        {
            _prevCountdown = countdown;
            ShowCountdown(countdown);
        }
    }

    private void RoundEndTimers()
    {
        var remaining = RoundEndTimer.RemainingTime(Runner);
        if (remaining.HasValue)
        {
            Debug.Log($"남은 시간: {remaining.Value:F2}초");
        }
        else
        {
            Debug.Log("타이머가 설정되어 있지 않거나 만료됨");
        }
        if (RoundEndTimer.IsRunning && RoundEndTimer.Expired(Runner))
        {
            Debug.Log("끝남");
        }
    }

    private void TempEscape()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Matchmaker.Instance.Runner.Shutdown();
        }
    }

    private void ShowCountdown(int val)
    {
        var text = InterfaceManager.instance.countText;
        if (text == null) return;

        text.text = val > 0 ? val.ToString() : val == 0 ? "START" : "";

        if (val == 0)
        {
            var myPutter = GetMyPutter();
            if (myPutter != null)
            {
                myPutter.controlFlag = true;
            }
        }

        text.transform.localScale = Vector3.zero;
        text.color = new Color(1, 1, 1, 0);

        text.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
        text.DOFade(1f, 0.2f).OnComplete(() =>
        {
            text.DOFade(0f, 0.3f).SetDelay(0.3f);
        });
    }
    public Putter GetMyPutter()
    {
        var myRef = Runner.LocalPlayer;

        if (ObjectByRef.TryGet(myRef, out var putter))
        {
            return putter;
        }
        return null;
    }
    public override void FixedUpdateNetwork() { }

    private IEnumerator StartPreGameCountdown()
    {
        yield return new WaitUntil(() => Runner.TryGetPlayerObject(Object.StateAuthority, out _));
        PreGameCountdownTimer = TickTimer.CreateFromSeconds(Runner, _startCountdownSeconds);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_NotifyGoalReached(PlayerRef player)
    {
        Debug.Log($"[GameManager] 골 도착 알림 받음 from {player}");

        if (!RoundEndTimer.IsRunning)
        {
            RoundEndTimer = TickTimer.CreateFromSeconds(Runner, 10f);
            Debug.Log("[GameManager] ResultTimer 시작됨");
        }
    }

    private void CheckForChanges()
    {
        foreach (var change in _changeDetector.DetectChanges(this, out var previousBuffer, out var currentBuffer))
        {
            if (change == nameof(AddCount))
            {
                var reader = GetPropertyReader<int>(nameof(AddCount));
                int before = reader.Read(previousBuffer);
                int after = reader.Read(currentBuffer);
                InterfaceManager.instance.countText.text = after.ToString();
            }
        }
    }

    private void CleanupLeftPlayers()
    {
        // 향후 구현 예정
    }
}
