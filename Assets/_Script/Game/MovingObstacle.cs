using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class MovingObstacle : NetworkBehaviour
{
    [Header("Rotation Settings")]
    public float rotationAngle = 360f;       // 한 바퀴 각도
    public float rotationDuration = 10f;      // 한 바퀴 도는데 걸리는 시간(초)
    public float timerInterval = 7f;         // 회전 주기(초)

    public Transform Wing;
    [Networked] private TickTimer RotationTimer { get; set; }

    private bool _isRotating;
    private float _currentAngle;
    private float _rotationSpeed;            // 초당 회전 속도

    private void Awake()
    {
        // 한 바퀴 시간 기준으로 초당 회전 속도 계산
        _rotationSpeed = rotationAngle / rotationDuration;
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            // 초기 지연 랜덤 추가
            float initialDelay = Random.Range(0f, timerInterval);
            RotationTimer = TickTimer.CreateFromSeconds(Runner, initialDelay);
        }
    }

    public override void Render()
    {
        if (!RotationTimer.IsRunning)
            return;

        if (_isRotating)
        {
            float delta = _rotationSpeed * Time.deltaTime;
            Wing.Rotate(Vector3.up, -delta, Space.Self); // Y축 반대 방향 회전
            _currentAngle += delta;

            if (_currentAngle >= rotationAngle)
            {
                _isRotating = false;
                _currentAngle = 0f;
            }
        }
        else
        {
            // 타이머 만료 시 회전 시작
            if (RotationTimer.Expired(Runner))
            {
                _isRotating = true;
                _currentAngle = 0f;

                // 권한 있는 쪽만 새 타이머 갱신
                if (Object.HasStateAuthority)
                {
                    RotationTimer = TickTimer.CreateFromSeconds(Runner, timerInterval);
                }
            }
        }
    }
}
