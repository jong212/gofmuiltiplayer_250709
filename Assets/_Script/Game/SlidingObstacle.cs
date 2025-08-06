using Fusion;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(NetworkObject))]
public class SlidingObstacle : NetworkBehaviour
{
    [Header("Slide Settings")]
    public float moveDistance = 3f;     // 튀어나오는 거리
    public float moveDuration = 0.3f;   // 전진/후진 시간
    public float stayDuration = 1f;     // 튀어나온 상태 유지 시간
    public float interval = 3f;         // 다음 사이클까지 대기 시간
    public float localCooldown = 2f;    // 로컬 쿨타임 (중복 방지용)
    public Vector3 knockDirection;

    private Vector3 _startPos;
    private Vector3 _endPos;

    public Transform childObj;          // 실제로 움직일 자식 오브젝트

    private float _lastPlayTime;        // 로컬 쿨타임 기록
    private Sequence seq;               // DOTween 시퀀스 관리용

    [Networked] private TickTimer SlideTimer { get; set; }

    public override void Spawned()
    {
        _startPos = childObj.localPosition;
        _endPos = _startPos + Vector3.forward * moveDistance;

        if (Object.HasStateAuthority)
        {
            float initialDelay = Random.Range(0f, interval);
            SlideTimer = TickTimer.CreateFromSeconds(Runner, initialDelay);
        }
    }

    public override void Render()
    {
        // 로컬 쿨타임 체크
        if (Time.time - _lastPlayTime < localCooldown)
            return;

        // 네트워크 타이머 확인
        if (SlideTimer.ExpiredOrNotRunning(Runner))
        {
            _lastPlayTime = Time.time; // 로컬 쿨타임 갱신
            PlaySlideSequence();
        }
    }

    private void PlaySlideSequence()
    {
        seq?.Kill(); // 기존 시퀀스 제거 (중복 방지)

        seq = DOTween.Sequence();
        seq.Append(childObj.DOLocalMove(_endPos, moveDuration).SetEase(Ease.OutQuad));
        seq.AppendInterval(stayDuration);
        seq.Append(childObj.DOLocalMove(_startPos, moveDuration).SetEase(Ease.InQuad));

        seq.OnComplete(() =>
        {
            if (Object.HasStateAuthority)
            {
                SlideTimer = TickTimer.CreateFromSeconds(Runner, interval);
            }
        });
    }

    private void OnDestroy()
    {
        seq?.Kill();
    }
}
