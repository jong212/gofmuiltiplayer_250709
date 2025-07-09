using UnityEngine;
using Fusion;

[DefaultExecutionOrder(-1000)]
public sealed class Ball3DController : NetworkBehaviour, IAfterClientPredictionReset, IBeforeAllTicks, IAfterTick
{
    [SerializeField] private float _speed = 6f;
    [SerializeField] private float _radius = 0.5f; // ⬅️ 공 반지름 (Collider radius와 동일해야 함)
    [SerializeField] private Vector2 _arenaBounds = new(9f, 5f); // x, z 경계 (중심 기준)

    [Networked] private Vector3 _position { get; set; }
    [Networked] private Vector3 _velocity { get; set; }

    private Transform _transform;
    private int _lastRenderFrame;

    public override void Spawned()
    {
        _transform = transform;

        if (HasStateAuthority)
        {
            _position = Vector3.zero;

            // ✅ XZ 평면 기준 랜덤 방향
            float angle = Random.Range(-45f, 45f);
            Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            _velocity = dir.normalized * _speed;

            StoreTransform();
        }
        else
        {
            RestoreTransform();
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        _velocity = _velocity.normalized * _speed;
        Vector3 nextPos = _position + _velocity * Runner.DeltaTime;

        // ✅ X축 반사 (왼쪽, 오른쪽 벽)
        if (Mathf.Abs(nextPos.x) >= _arenaBounds.x - _radius)
        {
            Debug.Log($"X 반사 발생 | nextPos.x = {nextPos.x}, 경계 = {_arenaBounds.x - _radius}");

            Vector3 vel = _velocity;
            vel.x *= -1;
            _velocity = vel;
        }

        // ✅ Z축 반사 (위쪽, 아래쪽 벽)
        if (Mathf.Abs(nextPos.z) >= _arenaBounds.y - _radius)
        {
            Debug.Log($"Z 반사 발생 | nextPos.z = {nextPos.z}, 경계 = {_arenaBounds.y - _radius}");

            Vector3 vel = _velocity;
            vel.z *= -1;
            _velocity = vel;
        }

        _position = nextPos;
        _transform.position = nextPos;

        StoreTransform();
    }

    public override void Render()
    {
        _lastRenderFrame = Time.frameCount;

        if (Runner.GameMode == GameMode.Shared && IsProxy)
        {
            InterpolateSharedPosition();
        }
        else
        {
            _transform.position = _position;
        }
    }

    private void InterpolateSharedPosition()
    {
        if (!TryGetSnapshotsBuffers(out var from, out var to, out float alpha)) return;

        var reader = GetPropertyReader<Vector3>(nameof(_position));
        (Vector3 fromPos, Vector3 toPos) = reader.Read(from, to);
        Vector3 interpolated = Vector3.Lerp(fromPos, toPos, alpha);

        _transform.position = interpolated;
    }

    private void StoreTransform()
    {
        _position = _transform.position;
    }

    private void RestoreTransform()
    {
        _transform.position = _position;
    }

    public void Reflect(Vector3 normal)
    {
        if (!HasStateAuthority) return;
        _velocity = Vector3.Reflect(_velocity, normal.normalized);
    }
    public void ResetBall(Vector3 direction)
    {
        if (!HasStateAuthority) return;
        _position = Vector3.zero;
        _velocity = direction.normalized * _speed;
        _transform.position = _position;
    }

    // Fusion 보정 관련 인터페이스
    void IAfterClientPredictionReset.AfterClientPredictionReset()
    {
        RestoreTransform();
    }

    void IBeforeAllTicks.BeforeAllTicks(bool resimulation, int tickCount)
    {
        if (!resimulation) return;
        if (Time.frameCount - 1 != _lastRenderFrame) return;
        RestoreTransform();
    }

    void IAfterTick.AfterTick()
    {
        if (Runner.GameMode == GameMode.Shared && !HasStateAuthority) return;
        StoreTransform();
    }
}
