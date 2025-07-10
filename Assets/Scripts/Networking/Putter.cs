using Fusion;
using UnityEngine;

public class Putter : NetworkBehaviour
{
    [Header("Refs")]
    public Transform interpolationTarget;
    public Rigidbody rb;
    new public SphereCollider collider;

    [Header("Putt Settings")]
    public float maxPuttStrength = 10f;
    public float puttGainFactor = 0.1f;   // 게이지 상승 속도
    public float speedLoss = 0.1f;

    /* ────────── 네트워크 변수 ────────── */
    [Networked] TickTimer PuttTimer { get; set; }
    [Networked] PlayerInput CurrInput { get; set; }

    PlayerInput prevInput = default;

    bool CanPutt => PuttTimer.ExpiredOrNotRunning(Runner);
    public bool controlFlag;


    [Networked] public int Strokes { get; set; } // 스트록 횟수 누적
    [Networked] public float TimeTaken { get; set; } // -1이면 미완료

    /* ────────── Mono / Fusion ────────── */
    public override void Spawned()
    {
        if (Object.HasStateAuthority)
            CameraController.AssignControl(interpolationTarget);
    }
    public override void Render()
    {
        if (CurrInput.isDragging)
        {
            InterfaceManager.instance.ChargeCircle.gameObject.SetActive(true);
            InterfaceManager.instance.ChargeCircle.fillAmount = CurrInput.dragDelta;
        }
        else
        {
            InterfaceManager.instance.ChargeCircle.gameObject.SetActive(false);
            InterfaceManager.instance.ChargeCircle.fillAmount = CurrInput.dragDelta;
        }

    }
    public override void FixedUpdateNetwork()
    {

        if (!controlFlag) return;
        if (GetInput(out PlayerInput input))
            CurrInput = input;

        if (!Runner.IsForward) return;


        // 손을 뗐을 때
        if (!CurrInput.isDragging && prevInput.isDragging)
        {
            if (CanPutt && prevInput.dragDelta > 0)
            {
                Vector3 dir = Quaternion.Euler(0, (float)prevInput.yaw, 0) * Vector3.forward;
                float finalPower = Mathf.Clamp(prevInput.dragDelta * maxPuttStrength, 0, maxPuttStrength);

                if (IsGrounded())
                    rb.AddForce(dir * finalPower, ForceMode.VelocityChange);
                else
                    rb.linearVelocity = dir * finalPower;
                Strokes++;
                PuttTimer = TickTimer.CreateFromSeconds(Runner, 3);
            }
        }

        prevInput = CurrInput;  // 다음 틱 대비

        /* 3) 마찰 감속 */
        if (IsGrounded() && rb.linearVelocity.sqrMagnitude > 0.00001f)
            rb.linearVelocity = Vector3.MoveTowards(
                rb.linearVelocity, Vector3.zero,
                Time.fixedDeltaTime * speedLoss);
    }

    /* ────────── Helper ────────── */
    bool IsGrounded() =>
        Physics.OverlapSphere(transform.position, collider.radius * 1.05f,
                              LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore).Length > 0;

    public void TryRegisterGoalArrival()
    {
        // 자신이 조종 중인 오브젝트인지 확인 (내 클라의 Putter인지)
        if (Object.InputAuthority != Runner.LocalPlayer) return;

        if (TimeTaken > 0f) return; // 이미 도착했으면 무시

        TimeTaken = Runner.SimulationTime;
        Debug.Log($"[{Runner.LocalPlayer}] 내 공 도착 → 시간 기록: {TimeTaken:F2}초");

        GameManager.instance.Rpc_NotifyGoalReached(Runner.LocalPlayer);
    }

}
