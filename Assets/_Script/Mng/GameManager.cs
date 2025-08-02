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
    [Networked] NetworkObject _currentLevelInstance { get; set; }
    bool startlatency;
    private int _prevCountdown = -999;

    [Header("Timer")] 
    [Networked] public TickTimer RoundStartTimer { get; set; }  // 라운드 시작 타이머 3..2..1..
    [Networked] TickTimer RoundEndWaitTimer { get; set; }       // 잠시 대기
    [Networked] TickTimer RoundEndTimer { get; set; }           // 라운드 도착 타이머 10...9...
    [Networked] TickTimer ScoreBoardTimer { get; set; }         // 라운드 도착 타이머 10...9...
    
    [Header("State")]
    [Networked] public GameStatus CurrentStatus { get; set; }
    [Networked] public int CurrentRound { get; set; }             // 현재 라운드

    public override void Spawned()
    {
        base.Spawned();
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
        if (remainingTime.HasValue && remainingTime.Value <= 3)
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
            //Debug.Log($"라운드 종료까지: {remainSec}초");
        }
        if (RoundEndTimer.IsRunning && RoundEndTimer.Expired(Runner))
        {

            var myPutter = Common_Utils.GetMyPutter();
            if (myPutter == null) return; // 예외 방지

            myPutter.controlFlag = false;

            if (!myPutter.RoundResults.ContainsKey(CurrentRound))
            {
                // 내가 도착 못 했고, 아직 기록 안 했으면 기록
                myPutter.RoundResults.Set(CurrentRound, new RoundResultStruct
                {
                    Strokes = myPutter.Strokes,
                    TimeTaken = 0f
                });

                Debug.Log($"[로컬] 미도착 → 내 퍼팅 수만 기록됨: {myPutter.Strokes}타");
            }
            if(Object.HasStateAuthority)
            {
                CurrentStatus = GameStatus.RoundEndWait;
                RoundEndTimer = TickTimer.None;
                RoundEndWaitTimer = TickTimer.CreateFromSeconds(Runner, 2);

            }
        }
    }
    void RoundResult()
    {
        FinalScoreBoard board = new();
        var players = GameManager.instance.ObjectByRef;
        int maxRound = GameManager.instance.CurrentRound + 1;

        for (int r = 0; r < maxRound; r++)
        {
            /* ─────────────────────────────────────
             * 1) 도착자 : 한 번에 모으고 정렬
             * ────────────────────────────────────*/
            var arrived = players
                .Where(kv => kv.Value.RoundResults.TryGet(r, out var rr) && rr.TimeTaken > 0f)
                .Select(kv => (kv.Key, kv.Value.RoundResults[r].Strokes, kv.Value.RoundResults[r].TimeTaken))
                .OrderBy(t => t.TimeTaken)
                .ToList();

            var arrivedSet = new HashSet<PlayerRef>(arrived.Select(t => t.Key));  // O(1) lookup

            /* 2) 점수 부여 – 도착자 */
            for (int i = 0; i < arrived.Count; i++)
                Add(board, arrived[i].Key, r, arrived[i].Strokes, i + 1, ScoreByRank(i + 1));

            /* 3) 미도착자 0점 처리 – HashSet으로 빠른 필터링 */
            foreach (var (p, putter) in players)
                if (!arrivedSet.Contains(p))
                {
                    int s = putter.RoundResults.TryGet(r, out var rr) ? rr.Strokes : 0;
                    Add(board, p, r, s, -1, 0);
                }
        }

        /* 4) 총합 순위 */
        board.RankedByTotal = board.TotalScores
            .OrderByDescending(kv => kv.Value)
            .Select(kv => (kv.Key, kv.Value))
            .ToList();


        /* ────────────────────────
         * 로컬 헬퍼 (중복↓)
         * ───────────────────────*/
        void Add(FinalScoreBoard b, PlayerRef p, int rnd, int strokes, int rank, int score)
        {
            if (!b.RoundScores.TryGetValue(p, out var dict))
                b.RoundScores[p] = dict = new();
            dict[rnd] = new RoundScoreData { Strokes = strokes, Rank = rank, Score = score };
            b.TotalScores[p] = b.TotalScores.GetValueOrDefault(p) + score;
        }

        int ScoreByRank(int rank) => rank switch { 1 => 100, 2 => 80, 3 => 60, _ => 0 };
        debugscore(board);
    }

    void debugscore(FinalScoreBoard board)
    {
        Debug.Log("===== 라운드별 스코어 상세 =====");
        foreach (var playerEntry in board.RoundScores)
        {
            PlayerRef player = playerEntry.Key;
            int totalScore = board.TotalScores.GetValueOrDefault(player);

            Debug.Log($"<color=yellow>{player}</color> (총합 {totalScore}점)");

            foreach (var roundEntry in playerEntry.Value.OrderBy(k => k.Key))
            {
                int r = roundEntry.Key + 1;          // 사람이 읽기 좋게 1-base
                var data = roundEntry.Value;
                string rankS = data.Rank > 0 ? $"{data.Rank}등" : "미도착";

                Debug.Log($"   ▸ Round {r} : " +
                          $"{data.Strokes}타 / {rankS} / {data.Score}점");
            }
        }

        // 최종 랭킹
        Debug.Log("===== 최종 랭킹 =====");
        for (int i = 0; i < board.RankedByTotal.Count; i++)
        {
            var (player, total) = board.RankedByTotal[i];
            Debug.Log($"{i + 1}등 ▶ {player} : {total}점");
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
        Debug.Log(text);
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
    public override void FixedUpdateNetwork() {
        if (!Object.HasStateAuthority) return;

        switch (CurrentStatus)
        {
            case GameStatus.WaitingToStart:
                // PlayerObject가 전부 들어왔는지 확인
                if(_currentLevelInstance == null && !startlatency)
                {
                    startlatency = true;
                    Runner.Spawn(Levels[CurrentRound]);
                }
                if (AllPlayersReady())
                {
                    Debug.Log("[GameManager] 모든 플레이어 준비됨 → 게임 시작");

                    RoundStartTimer = TickTimer.CreateFromSeconds(Runner, 3);
                    _prevCountdown = -999;
                    CurrentStatus = GameStatus.Playing;
                }
                break;
            case GameStatus.RoundEndWait:
                if (RoundEndWaitTimer.Expired(Runner))
                {
                    CurrentStatus = GameStatus.RoundEnd;
                }
                break;
            case GameStatus.RoundEnd:
                ScoreBoardTimer = TickTimer.CreateFromSeconds(Runner, 5);
                Rpc_CurrentRoundScoreBoard();
                CurrentStatus = GameStatus.NextRound;
                break;

            case GameStatus.NextRound:
                int remainSec = Common_Utils.GetRemainingSecondsInt(ScoreBoardTimer, Runner);
                if (remainSec >= 0)
                {
                    //Debug.Log($"스코어 보드 꺼지기 까지: {remainSec}초");
                }
                if (ScoreBoardTimer.Expired(Runner))
                {
                    if (CurrentRound >= 1) {CurrentStatus = GameStatus.GameEnd;  break; }
                    CurrentRound++;
                    // 기존 맵 제거
                    if (_currentLevelInstance != null)
                    {
                        Runner.Despawn(_currentLevelInstance);
                        _currentLevelInstance = null;
                    }
                    _currentLevelInstance = Runner.Spawn(Levels[CurrentRound]);

                    Rpc_PlayerSpawnPosition();
                    RoundStartTimer = TickTimer.CreateFromSeconds(Runner, 5);
                    _prevCountdown = -999;
                    CurrentStatus = GameStatus.Playing;
                }
                break;
            case GameStatus.GameEnd:
                // TO DO 결산 및 종료
                Debug.Log("게임종료");
                break;
        }
    }
    private bool AllPlayersReady()
    {
        foreach (var player in Runner.ActivePlayers)
        {
            if (!Runner.TryGetPlayerObject(player, out _))
                return false;
        }
        return true;
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
        myplayer.controlFlag = false;
        myplayer.rb.linearVelocity = Vector3.zero;
        myplayer.rb.angularVelocity = Vector3.zero;
    }


    private void CheckForChanges()
    {
        if (_changeDetector == null)
        {
            Debug.LogError("_changeDetector is NULL!");
            return;
        }

        foreach (var change in _changeDetector.DetectChanges(this, out var previousBuffer, out var currentBuffer))
        {
            if (change == nameof(AddCount))
            {
                Debug.Log(change.ToString());
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
 
    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

}
