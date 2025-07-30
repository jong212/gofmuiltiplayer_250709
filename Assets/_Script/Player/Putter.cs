using Fusion;
using System;
using UnityEngine;
using UnityEngine.InputSystem.HID;

[Serializable]
public struct RoundResultStruct : INetworkStruct
{
    public int Strokes;
    public float TimeTaken;
}
public class Putter : NetworkBehaviour
{
    [Header("Refs")]
    public Transform interpolationTarget;
    public Rigidbody rb;
    public Transform Arrow;
    public Transform body;
    public MeshRenderer ArrowMeshRenderer;
    new public SphereCollider collider;


    [Header("Putt Settings")]
    public float maxPuttStrength = 10f;
    public float puttGainFactor = 0.1f;   // 게이지 상승 속도
    public float speedLoss = 0.1f;

    /* ────────── 네트워크 변수 ────────── */
    [Networked] TickTimer PuttTimer { get; set; }
    [Networked] PlayerInput CurrInput { get; set; }

    PlayerInput prevInput = default;
    [Networked, Capacity(10)] // 최대 10라운드 예시
    public NetworkDictionary<int, RoundResultStruct> RoundResults => default;


    bool CanPutt => PuttTimer.ExpiredOrNotRunning(Runner);
    public bool controlFlag;
    public int Strokes { get; set; } // 스트록 횟수 누적
    //public float LocalTimeTaken { get; set; } // -1이면 미완료

    /* ────────── Mono / Fusion ────────── */
    public override void Spawned()
    {
        if (Object.HasStateAuthority)
            CameraController.AssignControl(interpolationTarget);

        PlayerVisualSet();
    }
    public override void Render()
    {
        if (!Object.HasStateAuthority) return;
        var scale = Arrow.localScale;
        scale.z = CurrInput.dragDelta;
        Arrow.rotation = Quaternion.Euler(0, (float)CurrInput.yaw, 0);

        if (CurrInput.isDragging)
        {
            ArrowMeshRenderer.material.SetColor("_EmissionColor", InterfaceManager.instance.PuttChargeColor.Evaluate(CurrInput.dragDelta));
            InterfaceManager.instance.ChargeCircle.color = InterfaceManager.instance.PuttChargeColor.Evaluate(CurrInput.dragDelta) ;

            InterfaceManager.instance.ChargeCircle.gameObject.SetActive(true);
            InterfaceManager.instance.ChargeCircle.fillAmount = CurrInput.dragDelta;
            Arrow.localScale = scale;
        }
        else
        {
            InterfaceManager.instance.ChargeCircle.gameObject.SetActive(false);
            InterfaceManager.instance.ChargeCircle.fillAmount = CurrInput.dragDelta;
            Arrow.localScale = scale;
        }

    }
    public void TeleportTo(Vector3 pos)
    {
        if (!HasStateAuthority) return;

        transform.position = pos;
        rb.linearVelocity = Vector3.zero;
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

        if (RoundResults.ContainsKey(GameManager.instance.CurrentRound)) return;

        RoundResults.Set(GameManager.instance.CurrentRound, new RoundResultStruct
        {
            Strokes = Strokes,
            TimeTaken = Runner.SimulationTime
        });
        Debug.Log($"[{Runner.LocalPlayer}] 내 공 도착 → 시간 기록: {Runner.SimulationTime:F2}초");

        GameManager.instance.Rpc_NotifyGoalReached(Runner.LocalPlayer);
    }
    void PlayerVisualSet()
    {
        // 난 n번 바디 착용중임
        var tempUserData = ManagerSystem.Instance.BackendCash.UserData;
        // 그럼 1번 가져와
        var tempMtData = ManagerSystem.Instance.BackendCash.ChartCharacter[tempUserData.SelectedCharId - 1];
        Debug.Log(tempMtData.name);
        GameObject bodyPrefab = AddressableMng.instance.GetPrefab("ballskin", tempMtData.name);

        Instantiate(bodyPrefab, body.transform);
    }
}
