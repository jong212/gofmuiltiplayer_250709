using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class MovingObstacle : NetworkBehaviour
{
    [Header("Rotation Settings")]
    public float rotationAngle = 360f;       // �� ���� ����
    public float rotationDuration = 10f;      // �� ���� ���µ� �ɸ��� �ð�(��)
    public float timerInterval = 7f;         // ȸ�� �ֱ�(��)

    public Transform Wing;
    [Networked] private TickTimer RotationTimer { get; set; }

    private bool _isRotating;
    private float _currentAngle;
    private float _rotationSpeed;            // �ʴ� ȸ�� �ӵ�

    private void Awake()
    {
        // �� ���� �ð� �������� �ʴ� ȸ�� �ӵ� ���
        _rotationSpeed = rotationAngle / rotationDuration;
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            // �ʱ� ���� ���� �߰�
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
            Wing.Rotate(Vector3.up, -delta, Space.Self); // Y�� �ݴ� ���� ȸ��
            _currentAngle += delta;

            if (_currentAngle >= rotationAngle)
            {
                _isRotating = false;
                _currentAngle = 0f;
            }
        }
        else
        {
            // Ÿ�̸� ���� �� ȸ�� ����
            if (RotationTimer.Expired(Runner))
            {
                _isRotating = true;
                _currentAngle = 0f;

                // ���� �ִ� �ʸ� �� Ÿ�̸� ����
                if (Object.HasStateAuthority)
                {
                    RotationTimer = TickTimer.CreateFromSeconds(Runner, timerInterval);
                }
            }
        }
    }
}
