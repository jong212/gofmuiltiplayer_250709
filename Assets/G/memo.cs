using UnityEngine;


/* My Memo
===============================
걍 메모
===============================
- physics material 값 바꾸기 전에 백업해두자... 그리고 sphere collider 의 radius 값 막 바꾸면 안된다 0.035긴 한데 퍼팅할때 흔들리는 형상 잇으면 이 값을 줄여봐라 그리고 이 값이랑 physicsmaterial 연관잇으니 메모...
- 게임 종료 로직 일단 참조 오류 방지를 위해 Game Maganger Despawned 에 처리하자
- 강의 Memo1 조이스틱 팩 코드 수정함  드래그중 체크하기 위해

===============================
스크립트 기능 정리
===============================
Putter.cs 
-       Despawned 메서드에 겜 종료 할때 내 클라에서만 타는 로직 작성함 (오류 방지)

===============================
게임 실행 로직 순서 정리
===============================
[ 전체 흐름 – 버튼 클릭 → 게임 루프 진입 ]
(괄호 안은 “어디에서 실행되는 코드인지” 표시)

0. 게임 시작 트리거
   (UI)
   - 모든 클라이언트: 사용자가 ‘게임 시작’ 버튼 클릭

1. 러너 준비
   (Matchmaker)
   - 모든 클라이언트:
     1) 기존 Runner가 있으면 Shutdown()으로 정리
     2) 새 Runner 생성  →  PlayerSpawner 스크립트도 같이 생성
     3) OnSceneLoadDone += SpawnManager  등록
        → SpawnManager 안에서 IsSharedModeMasterClient 조건으로
          마스터 클라이언트만 GameManager를 Spawn하도록 설정
     4) Runner.StartGame() 실행 (세션 참가, 아직 씬 전환 없음)

2. 세션 합류 직후 – 플레이어 등록
   (PlayerSpawner)
   - 모든 클라이언트: Fusion 이 PlayerSpawner.PlayerJoined(player) 자동 호출
   - 본인 클라이언트:
     1) SpawnRoutine() 코루틴 실행
     2) GameManager.instance == null 이므로 대기 (메뉴 씬에는 GameManager 없음)

3. 마스터 클라 – 게임 씬 로드 준비
   (Room_Mng)
   - 마스터 클라이언트:
     1) Room_Mng Spawn
     2) ReadyToStart == true  가 되면 Runner.LoadScene(gameScene) 실행
        → 모든 클라이언트가 게임 씬으로 전환됨

4. 씬 로딩 완료 → GameManager 생성
   - 모든 클라이언트: OnSceneLoadDone 이벤트 발생 (Fusion 시스템)
   - 마스터 클라이언트인 경우에만 아까 등록한 겜매니저 스폰하는 로직을 실행
     1) runner.Spawn(GameManager)
     2) GameManager.Awake()  →  GameManager.instance 할당
     3) GameManager.Spawned() → PrepareOrderTimer() 코루틴 진입
        (자신의 PlayerObject 가 생성될 때까지 대기)

5. PlayerSpawner는 
    아까 러너 만들떄 생성 되면서 PlayerJoined 부분으로 SpawnRoutine 코루틴이 이미  실행되면서 대기(겜매니저가 없어서)하고 있던 상태임 
     근데 지금 씬로딩이 완료 되면서 마스터 클라가 겜 매니저를 생성했기 때문에 아래 로직이 실행 됨 

   - 모든 클라이언트:
     1) GameManager.instance 가 null 아님을 확인 → 대기 해제
   - 본인 클라이언트:
     2) Runner.SpawnAsync(PlayerPrefab)  →  캐릭터 생성
     3) Runner.SetPlayerObject(LocalPlayer, obj)  →  PlayerObject 등록

6. GameManager.PrepareOrderTimer(코루틴) => 
   - 1) 이 코루틴은 위에서 마스터 클라가 씬 로드 후 겜매니저 생성 할 때 Spawned에서 마스터클라 체크하고 PrepareOrderTimer라는 코루틴을 실행시켜놨던 것임
     2) 왜 계속 대기를 하냐면 		yield return new WaitUntil(() => Runner.TryGetPlayerObject(Object.StateAuthority, out _));여기 로직에 걸렸던 것임
     3) 그치만 위에서 스포너에서 SetPlayerObject(Local...)을 했기 때문에 플레이어가 생성된 것을 true로 확인하고  대기가 해제 되어 본격 게임시작이 된 것임

     정리하면 아래와 같은 느낌 

   - 마스터 클라이언트:
     1) Runner.TryGetPlayerObject(StateAuthority) == true  →  대기 해제
     2) OrderTimer 등 초기화  →  본격 게임 시작
    
===============================
라운드 종료 순서 정리
===============================

1. Hole (도착 트리거)
   - Hole에 부착된 `OnTriggerEnter` 스크립트가 존재하며,
   - 플레이어(Putter)의 `TryRegisterGoalArrival()` 메서드를 호출함.
     ㄴ (설명) 즉, 플레이어가 홀에 도달하면 해당 메서드를 통해 "나 도착했어!" 라는 신호를 보냄.

2. Putter (도착 시간 기록 및 마스터에게 알림)
   - `TryRegisterGoalArrival()`에서는 내 도착 시간을 `Networked` 변수에 저장함.
     ㄴ 모든 클라이언트가 내 도착 시점을 알 수 있게 됨.
     ㄴ 참고: 이때 시간은 `Runner.SimulationTime` 기준으로 기록됨 (네트워크 시간 기준).

   - 이후 마스터 클라이언트에게 "도착했어요!"라고 알림을 보내야 함.
     ㄴ `GameManager.instance.Rpc_NotifyGoalReached(playerRef)` 호출
     ㄴ 이 메서드는 `[Rpc(RpcSources.All, RpcTargets.StateAuthority)]`로 되어 있어서,
        - **누구나 호출은 가능하지만**
        - **실제 실행은 마스터 클라이언트에서만** 발생함 (StateAuthority 대상)

3. GameManager (마스터가 타이머 설정)
   - 마스터 클라이언트에서는 도착 알림을 받으면 `ResultTimer`를 설정함:
     ```
     ResultTimer = TickTimer.CreateFromSeconds(Runner, 10f);
     ```
   - 이후 모든 클라이언트의 `GameManager.Render()`에서 이 `ResultTimer`의 남은 시간을 실시간으로 확인하며
     카운트다운을 시각적으로 출력함.

===============================
타이머 관련 설명
===============================

1. ResultTimer.IsRunning
   ㄴ 타이머가 TickTimer.None 상태가 아니라면 true 반환 (즉, 한 번이라도 설정된 적 있다면 true)
   ㄴ 타이머 시간이 이미 만료돼도 초기화( TickTimer.None; )를 안 하면 true 계속 반환함 
        

2. PuttTimer.ExpiredOrNotRunning(Runner)
   ㄴ 타이머가 아직 설정되지 않았음 → true
   ㄴ 타이머 만료됨 → true
   ㄴ 즉, 타이머가 실행 중이지 않은 모든 상황에서 true 반환
   
3 타이머가 한 번 실행된 적이 있으면서 만료된 경우를 체크하고 싶다면
if (RoundEndTimer.IsRunning && RoundEndTimer.Expired(Runner))





===============================
공통 해당
===============================

 -      Object.HasStateAuthority - 내가 이 오브젝트의 권한을 가지고 있다면 true 반환

 -      Object.StateAuthority -  이 NetworkObject를 현재 제어(상태 변경 권한)하고 있는 플레이어의 PlayerRef 를 반환

 -      !Object.StateAuthority.IsNone 이 오브젝트의 권한을 아무도 가지지 않으면의 반대니까 누군가가 이 오브젝트의 권한을 가지고 있다면 True 반환

 -      bool Object.IsValid  네트워크에 아직 존재하고, Despawn되지 않았다면 true 이고 이미 despawn 되었거나 오류로 제거된 상태라면 false

 -      [Rpc(RpcSources.All, RpcTargets.All)] 이거 모든 클라에서 호출되는줄 알았는데 [RpcTarget]이 있으면 모든 클라가 아니라 해당 클라에만 실행 됨 신기       
        private void Rpc_Authorized([RpcTarget] PlayerRef player)

 -      NetworkObject에서 Is Master Client Object 옵션을 체크하는 게 무슨의 미 ?https://cherry22.tistory.com/entry/photon-Network-Object-%EC%BB%B4%ED%8F%AC%EB%84%8C%ED%8A%B8%EC%97%90%EC%84%9C-is-Master-Client-Object-%EC%98%B5%EC%85%98%EC%9D%80-%EB%AD%98-%EC%9D%98%EB%AF%B8%ED%95%98%EB%82%98

 - 	    Matchmaker.Instance.Runner.Shutdown(); 이거 호출하면 OnShutdown() 콜백 자동 호출 됨
---------------------------------------------------------------------------------------------
  Memo1 Runner는 matchmaker에서 직접 Instance후 참조하고 있고 다른데서는 networkbehaviour의 Runner속성을 사용하는 것인데 매치메이커에서 생성된 runner와 동일 인스턴스임 걍 같은거임
---------------------------------------------------------------------------------------------
  Memo2 네트워크 변수가 선언된 스크립트를 참조하는 곳에서는 항상 에러처리를 해야한다
        에러 케이스 중에서 MatchMaker에서 Room_Mng의 ReadyToStart 네트워크 변수를 계속해서 참조하는 로직이였는데
        코루틴으로 돌아가다 보니까 방을 나갔을 때 Moom_Mng가 없어져서 터졌다 글서 IsValid 일 때에만 처리되게 예외처리 했고 
        방을 나갈 때 Matchmaker.Instance.Runner.Shutdown(); 이걸 호출시키면 알아서 콜백으로 public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) 이게 호출되는데 여기서 코루틴을 없애버림 그니까 network 변수를 참조하는 코루틴을 종료 함으로서 2중 방어
---------------------------------------------------------------------------------------------
  Memo3  ⚠️ [주의] Despawned()는 일반적으로 모든 클라이언트에서 호출되는 콜백이지만,
         현재 GameManager는 `Is Master Client Object` 옵션이 활성화되어 있음.
         이 설정이 켜져 있으면, 마스터 클라이언트가 나가도 이 오브젝트는 Despawn되지 않고,
         다음 마스터 클라이언트에게 State Authority가 자동으로 이전됨.
        
         따라서:
         👉 GameManager 오브젝트는 파괴되지 않고 그대로 유지됨.
         👉 이로 인해 Despawned()는 마스터 클라이언트(퇴장한 나)에게만 호출되고,
            다른 클라이언트에서는 호출되지 않음.
        
         ✅ 이 콜백 내에서는 씬 안에서 참조 중이던 리소스나 컴포넌트를 안전하게 정리해두는 것이 좋음.
            예: 카메라 대상 해제, HUD 정리, 싱글톤 초기화 등
         와 이거 골떄림 A클라가 나갓는데 B클라의 카메라가 해제 됨 위 내용은 despawned를 겜오브젝트에 했을때가정이고 지금은 putter 그 플레이어 공한테해놧는데 이공은 is masterclient이거아니니까 despawned 콜백이 모든 클라에서 호출 그니까 다른클라의 내 프록시 오브젝트에서 호출을하는데 이게 카메라싱글톤에접근해서 null떄려버리니까 다른 클라의 카메라가 해제되었음 ㅋㅋ
             if (HasInputAuthority) 이렇게 하면 프록시에서  실행 안하니까 괜찮을듯...
              {
                  CameraController.AssignControl(null);
                }
---------------------------------------------------------------------------------------------
Memo4 TickTimer.CreateFromSeconds 

의미 : 지금부터 특정 시간(초) 뒤에 만료되는 타이머 객체를 생성하는 것

    만약 내가 TickTimer.CreateFromSeconds(Runner, 3) 이렇게 썼다면? 
        → 현재 Fusion의 Tick 기준으로 3초 후에 만료되는 TickTimer를 만든다
        → 즉, AddCountTimer에는 실제로 만료되는 시점(Tick) 이 저장됨 이라는 뜻
        → 이건 시간 정보를 내부적으로 저장하는 구조지, 숫자 3을 저장하는 게 아님

    만료 시간을 체크하고 싶다면?
        if (AddCountTimer.Expired(Runner))
        {
            // 3초 지났을 때 실행할 로직
        }
        → Expired(Runner)로 Runner의 현재 시간과 비교하여 만료 여부를 확인함
        → true면 지정된 시간이 지났다는 뜻
---------------------------------------------------------------------------------------------


===============================
파이어베이스 공통
===============================
SetAsync()
   - 역할: 문서 전체 덮어쓰기 (기존 데이터를 전부 교체)
   - 문서가 존재하면: 완전히 덮어씀
   - 문서가 없으면: 새로 생성

SetAsync(..., Merge)
   - 역할: 문서의 일부 필드만 병합(덮어쓰기)
   - 문서가 존재하면: 지정한 필드만 업데이트 (나머지는 유지)
   - 문서가 없으면: 새로 생성

UpdateAsync()
   - 역할: 일부 필드만 수정
   - 문서가 존재하면: 지정한 필드만 수정
   - 문서가 없으면: 에러 발생

DeleteAsync()
   - 역할: 문서 전체 삭제
   - 문서가 존재하면: 삭제됨
   - 문서가 없으면: 에러 발생

GetSnapshotAsync() 로 가져온 snap 변수에는 뭐가 있을까? https://cherry22.tistory.com/entry/%ED%8C%8C%EC%9D%B4%EC%96%B4%EB%B2%A0%EC%9D%B4%EC%8A%A4-GetSnapShotAsync

===============================
TEMP CODE
===============================

- 플레이어 체크        
Debug.Log($"[Room_Mng] ObjectByRef Count: {ObjectByRef.Count}");
foreach (var kvp in ObjectByRef)
{
    string key = kvp.Key.ToString();
    string val = kvp.Value != null ? kvp.Value.name : "null";
    Debug.Log($"PlayerRef: {key} → Putter: {val}");
}
*/



public class memo : MonoBehaviour
{
    
}
