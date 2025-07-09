using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Fusion;
using Unity.VisualScripting;
using DG.Tweening;
using TMPro;

/*

1-1 게임 시작 초반부 뭐 없음 / MatchMaker.cs
2-1 카운트다운
3-1 누가 나갔을 때 : 해당 캐릭터가 들고 있는 아이템을 가장 가까운 빈 WorkSurface에 안전하게 내려놓고, 그 후 캐릭터 오브젝트를 제거하는 자동 정리 로직이다.          모든 조작은 반드시 권한을 획득한 후 순차적으로 콜백 방식으로 실행된다. 
4-1 무브

*/
[DefaultExecutionOrder(-200)]
public class GameManager : BaseCommonManager
{

    #region Singleton

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
    [Networked] TickTimer ResultTimer { get; set; }
    private int prevCountdown = -999;
    public override void Spawned() // 부모가 원래 virtual이여야하는데 부모의 부모인 네트워크오브젝트에 정의도
    {
        base.Spawned(); // 부모의 Spawned 먼저 실행
       
        Level.Load();

        startCount = 3; // 기본 값 설정

        if (Object.HasStateAuthority) StartCoroutine(PrepareCountTime());

    }
    public Putter GetMyPutter()
    {
        var myRef = Runner.LocalPlayer;

        if (ObjectByRef.TryGet(myRef, out var putter))
        {
            return putter;
        }

        // 예외 처리용 로그 (선택)
        Debug.LogWarning("❗ 내 Putter를 ObjectByRef에서 찾을 수 없습니다.");
        return null;
    }

    public override void Render()
	{
        var remaining = ResultTimer.RemainingTime(Runner);
        if (remaining.HasValue)
        {
            Debug.Log($"남은 시간: {remaining.Value:F2}초");
        }
        else
        {
            Debug.Log("타이머가 설정되어 있지 않거나 만료됨");
        }

        if (!gameTimer.IsRunning) return;
        float remain = gameTimer.RemainingTime(Runner).Value;  //TickTimer가 만료되기까지 남은 시간 (초 단위로 반환, float)
        int countdown = Mathf.CeilToInt(remain);               //남은 시간을 올림 처리해서 3 → 2 → 1 → 0 정수 카운트로 바꿈
        if (countdown != prevCountdown)                        //이전에 보여줬던 숫자와 다를 때만 아래 실행
        {
            prevCountdown = countdown;                         //현재 숫자를 저장
            ShowCountdown(countdown);                          //실제로 "3", "2", "1", "START" 애니메이션 띄움
        }
        if (Input.GetKey(KeyCode.Escape))
		{
            Cursor.lockState = CursorLockMode.None;

            Matchmaker.Instance.Runner.Shutdown();
		}
        CheckForChanges();
/*
        Debug.Log($"[Room_Mng] ObjectByRef Count: {ObjectByRef.Count}");
        foreach (var kvp in ObjectByRef)
        {
            string key = kvp.Key.ToString();
            string val = kvp.Value != null ? kvp.Value.name : "null";
            Debug.Log($"PlayerRef: {key} → Putter: {val}");
        }*/

    }
    private void ShowCountdown(int val)
    {
        TextMeshProUGUI text = InterfaceManager.instance.countText;
        if (text == null) return;

        if (val > 0) {
            text.text = val.ToString();
        }else if (val == 0) {
            var myPutter = GetMyPutter();            
            if (myPutter != null)
            {
                myPutter.controlFlag = true;
            }
            text.text = "START";
        } else {
            text.text = "";
        }

        text.transform.localScale = Vector3.zero;
        text.color = new Color(1, 1, 1, 0);

        text.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
        text.DOFade(1f, 0.2f).OnComplete(() =>
        {
            text.DOFade(0f, 0.3f).SetDelay(0.3f);
        });
    }

    public override void FixedUpdateNetwork()
	{


        if (Object.HasStateAuthority && ResultTimer.Expired(Runner))
        {
            ResultTimer = TickTimer.None; // 재실행 방지
            //CalculateScores();
        }
        //필요할떄 쓰기
        //TimersCheck();
    }
 

    // 최초 타이머 설정 로직임, n초 뒤에 만료 되는 타이머 설정 함 이거 탄 이후엔 CountTime 에서
    public IEnumerator PrepareCountTime()
    {
        yield return new WaitUntil(() => Runner.TryGetPlayerObject(Object.StateAuthority, out _));
        {
            // Memo4
            gameTimer = TickTimer.CreateFromSeconds(Runner, startCount);
        }
    }
  
    private void CheckForChanges()
    {
        foreach (string change in _changes.DetectChanges(this, out NetworkBehaviourBuffer previousBuffer, out NetworkBehaviourBuffer currentBuffer))
        {
            switch (change)
            {
          
                // 2-4 : 네트워크 변수값중에 AddCount가 바뀌면 여기를 타게 되어있음
                // 그래서 방장뿐만 아니라 그냥 클라도 여기 render라 타고 들어옴
                case nameof(AddCount):
                    var addCountReader = GetPropertyReader<int>(nameof(AddCount));
                    int before = addCountReader.Read(previousBuffer);
                    int after = addCountReader.Read(currentBuffer);

                    InterfaceManager.instance.countText.text = after.ToString();
                    break;
            }
        }
    }



    // 추상 클래스의 자식 구현체 - TO DO 이거 BaseCommonManager에서 OnPlayerLeft 로직 다짜고나서 추상메서드 하나 뒤에 실행하게 한다음 이거 추가로 실행되게 해야할듯
    /*public override void PlayerLeft(PlayerRef player)
	{
		////////////////////////////이거 나중에 골프 겜나갈떄 이거 비슷한 로직 짜야해서 일단냄겨둠)////////////
        // 3-1 누가 나갔을 때 : 누군가가 나간경우 마스터 클라에서는 이 콜백함수가 호출 된다.
        if (Runner.IsSharedModeMasterClient)
		{
			CleanupLeftPlayers();
		}
	}*/
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_NotifyGoalReached(PlayerRef player)
    {
        Debug.Log($"[GameManager] 골 도착 알림 받음 from {player}");

        if (!ResultTimer.IsRunning)
        {
            ResultTimer = TickTimer.CreateFromSeconds(Runner, 10f);
            Debug.Log("[GameManager] ResultTimer 시작됨");
        }
    }
    void CleanupLeftPlayers()
	{
 
        // 3-2 마스터 클라의 하이어라키에서 Character 컴포들을 찾아 배열에 넣어 둔 다음 
        // Runner.ActivePlayers 와 배열을 비교함
        // 그니까 c가 ActivePlayers에 없으면 objs 리스트에 추가 됨

		/*
        Character[] objs = FindObjectsOfType<Character>()
			.Where(c => !Runner.ActivePlayers.Contains(c.Object.StateAuthority))
			.ToArray();

		foreach (Character c in objs)
		{
			if (c.Object.IsValid)
			{

				// 3-3 캐릭터 권한요청
				c.GetComponent<AuthorityHandler>().RequestAuthority(() =>
				{
					Item item = c.HeldItem;
					if (item)
					{
						// find the nearest empty work surface
						WorkSurface surf = FindObjectsOfType<WorkSurface>()
							.OrderBy(w => Vector2.Distance(
								new Vector2(c.transform.position.x, c.transform.position.z),
								new Vector2(w.transform.position.x, w.transform.position.z)))
							.FirstOrDefault(w => w.ItemOnTop == null);

						if (surf)
						{
							surf.GetComponent<AuthorityHandler>().RequestAuthority(() =>
							{
								surf.ItemOnTop = item;
								item.transform.SetPositionAndRotation(surf.SurfacePoint.position, surf.SurfacePoint.rotation);
								item.transform.SetParent(surf.Object.transform, true);
								Runner.Despawn(c.Object);
							});
						}
						else
						{
							Runner.Despawn(item.Object);
							Runner.Despawn(c.Object);
						}
					}
					else Runner.Despawn(c.Object);
				});
			}
		}*/
	}

}