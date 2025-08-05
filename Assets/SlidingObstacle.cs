using Fusion;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(NetworkObject))]
public class SlidingObstacle : NetworkBehaviour
{
    [Header("Slide Settings")]
    public float moveDistance = 3f;     // Ƣ����� �Ÿ�
    public float moveDuration = 0.3f;   // ����/���� �ð�
    public float stayDuration = 1f;     // Ƣ��� ���� ���� �ð�
    public float interval = 3f;         // ���� ����Ŭ���� ��� �ð�
    public float localCooldown = 2f;    // ���� ��Ÿ�� (�ߺ� ������)
    public Vector3 knockDirection;

    private Vector3 _startPos;
    private Vector3 _endPos;

    public Transform childObj;          // ������ ������ �ڽ� ������Ʈ

    private float _lastPlayTime;        // ���� ��Ÿ�� ���
    private Sequence seq;               // DOTween ������ ������

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
        // ���� ��Ÿ�� üũ
        if (Time.time - _lastPlayTime < localCooldown)
            return;

        // ��Ʈ��ũ Ÿ�̸� Ȯ��
        if (SlideTimer.ExpiredOrNotRunning(Runner))
        {
            _lastPlayTime = Time.time; // ���� ��Ÿ�� ����
            PlaySlideSequence();
        }
    }

    private void PlaySlideSequence()
    {
        seq?.Kill(); // ���� ������ ���� (�ߺ� ����)

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
