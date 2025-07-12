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
    public static GameManager instance;

    private void Awake()
    {
        if (instance && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    [Networked] public int AddCount { get; set; } //미사용

    [SerializeField] NetworkObject[] Levels;
    [Networked] NetworkObject _currentLevel { get; set; }

    private int _prevCountdown = -999;

    [Header("Timer")] 
    [Networked] public TickTimer RoundStartTimer { get; set; }  // 라운드 시작 타이머 3..2..1..
    [Networked] TickTimer RoundEndTimer { get; set; }           // 라운드 도착 타이머 10...9...
    [Networked] TickTimer ScoreBoardTimer { get; set; }           // 라운드 도착 타이머 10...9...
    
    [Header("State")]
    [Networked] public GameStatus CurrentStatus { get; set; }
    [Networked] public int CurrentRound { get; set; }             // 현재 라운드

    public override void Spawned()
    {
        base.Spawned();
        InitializeGame();
    }

    private void InitializeGame()
    {
        if (Object.HasStateAuthority)
        {
            _currentLevel = Runner.Spawn(Levels[CurrentRound]);
            StartCoroutine(StartCountdownCoroutine());
        }
    }
  
    #region # render
    public override void Render()
    {
        RoundStartTimers();
        RoundEndTimers();
        TempEscape();
        CheckForChanges();
    }
    #endregion

    private void RoundStartTimers()
    {

        var remainingTime = RoundStartTimer.RemainingTime(Runner);
        if (remainingTime.HasValue)
        {
            int countdown = Mathf.CeilToInt(remainingTime.Value); //예: 2.3초 → 3, 0.1초 → 1
            if (countdown != _prevCountdown)
            {
                _prevCountdown = countdown;
                ShowCountdown(countdown);
            }
        }

    }
    private void RoundEndTimers()
    {
        int remainSec = Common_Utils.GetRemainingSecondsInt(RoundEndTimer, Runner);
        if (remainSec >= 0)
        {
            Debug.Log($"남은 시간: {remainSec}초");
        }
        if (RoundEndTimer.IsRunning && RoundEndTimer.Expired(Runner))
        {
            if(Object.HasStateAuthority)
            {
                CurrentStatus = GameStatus.RoundEnd;
                RoundEndTimer = TickTimer.None;
            }
        }
    }
    void RoundResult()
    {
        foreach (var kvp in ObjectByRef)
        {
            var player = kvp.Key;
            var putter = kvp.Value;

            if (putter.RoundResults.TryGet(CurrentRound, out var result))
            {
                Debug.Log($"[{player}] 라운드 {CurrentRound} 결과 - 스트로크: {result.Strokes}, 시간: {result.TimeTaken:F2}");
                // 점수 집계 or 정렬 등...
            }
            else
            {
                Debug.LogWarning($"[{player}] 라운드 {CurrentRound} 결과 없음");
            }
        }
    }

    private void TempEscape()
    {
        if (Input.GetKey(KeyCode.F1) && Object.HasStateAuthority)
        {
            foreach (var kvp in ObjectByRef)
            {
                var putter = kvp.Value;
                putter.TeleportTo(Vector3.zero); // (0, 0, 0)으로 순간이동
            }
        }


        if (Input.GetKey(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            MatchManager.Instance.Runner.Shutdown();
        }
    }

    private void ShowCountdown(int val)
    {
        var text = InterfaceManager.instance.countText;
        if (text == null) return;

        text.text = val > 0 ? val.ToString() : val == 0 ? "START" : "";

        if (val == 0)
        {
            var myPutter = Common_Utils.GetMyPutter();
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
  /*  유틸함수로 바꿔서 필요 없을드? 일단주석
   *  
   *  public Putter GetMyPutter()
    {
        var myRef = Runner.LocalPlayer;

        if (ObjectByRef.TryGet(myRef, out var putter))
        {
            return putter;
        }
        return null;
    }*/
    public override void FixedUpdateNetwork() {
        if (!Object.HasStateAuthority) return;

        switch (CurrentStatus)
        {
            case GameStatus.RoundEnd:
                ScoreBoardTimer = TickTimer.CreateFromSeconds(Runner, 5);
                Rpc_CurrentRoundScoreBoard();
                CurrentStatus = GameStatus.NextRound;
                break;

            case GameStatus.NextRound:
                int remainSec = Common_Utils.GetRemainingSecondsInt(ScoreBoardTimer, Runner);
                if (remainSec >= 0)
                {
                    Debug.Log($"남은 시간: {remainSec}초");
                }
                if (ScoreBoardTimer.Expired(Runner))
                {
                    CurrentRound++;
                    // 기존 맵 제거
                    if (_currentLevel != null)
                    {
                        Runner.Despawn(_currentLevel);
                        _currentLevel = null;
                    }
                    _currentLevel = Runner.Spawn(Levels[CurrentRound]);

                    Rpc_PlayerSpawnPosition();
                    CurrentStatus = GameStatus.Playing;
                }
                break;

         /*   case GameStatus.Result:
                if (RoundEndTimer.Expired(Runner))
                {
                    Phase = GameStatus.Transition;
                    // ex: 맵 교체, 캐릭터 재배치
                    PrepareNextRound();
                }
                break;

            case GameStatus.Transition:
                // 모두 준비되었는지 확인 후 다음 라운드로
                if (EveryoneReady())
                {
                    CurrentRound++;
                    Phase = GameStatus.Countdown;
                    RoundStartTimer = TickTimer.CreateFromSeconds(Runner, _roundStartTimerInit);
                }
                break;*/
        }
    }

    private IEnumerator StartCountdownCoroutine()
    {
        yield return new WaitUntil(() => Runner.TryGetPlayerObject(Object.StateAuthority, out _));
        RoundStartTimer = TickTimer.CreateFromSeconds(Runner, 3);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_NotifyGoalReached(PlayerRef player)
    {
        Debug.Log($"[GameManager] 골 도착 알림 받음 from {player}");

        if (!RoundEndTimer.IsRunning)
        {
            RoundEndTimer = TickTimer.CreateFromSeconds(Runner, 10);
            Debug.Log("[GameManager] ResultTimer 시작됨");
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_CurrentRoundScoreBoard()
    {
        RoundResult();
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_PlayerSpawnPosition()
    {
        Putter myplayer = Common_Utils.GetMyPutter();
        if (myplayer == null) return;

        myplayer.TeleportTo(new Vector3(0,3,0));
        myplayer.Strokes = 0;
        myplayer.TimeTaken = -1;
        myplayer.controlFlag = false;
        myplayer.rb.linearVelocity = Vector3.zero;
        myplayer.rb.angularVelocity = Vector3.zero;
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
    private void temp()
    {

    }
    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

}
