using UnityEngine;

// 플레이어를 따라가며 마우스/터치 드래그로 회전하는 3인칭 카메라 컨트롤러
public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("Target & Distance")]
    [SerializeField] private Transform target;      // 따라갈 대상
    [SerializeField] private float distance = 2.5f; // 대상과의 거리
    [SerializeField] private float height = 0.3f;   // 대상보다 얼마나 높게 위치할지

    [Header("Rotation")]
    [SerializeField] private float pitch = 20f;     // 카메라 상하 회전값 (초기값)
    [SerializeField] private float maxPitch = 60f;  // pitch 상하 회전 제한
    [SerializeField] private float sensitivity = 0.2f; // 드래그 회전 감도

    private float yaw;               // 좌우 회전값
    private Vector2 dragDelta;       // 입력으로 들어온 드래그 변화량 (외부에서 전달됨)

    private void Awake() => Instance = this;
   
    // 외부에서 카메라가 따라갈 대상을 지정
    public static void AssignControl(Transform t) => Instance.target = t;

    // Canvas 또는 입력 시스템에서 호출되는 드래그 입력 처리 메서드
    public void OnDragDelta(Vector2 delta)
    {
        dragDelta = delta;
    }

    // 드래그 입력을 기반으로 yaw/pitch 업데이트
    private void Update()
    {
        if (!target || !Application.isFocused) return;
        // 좌우 회전 (dragDelta.x → yaw 증가)
        yaw += dragDelta.x * sensitivity;
        
        // 상하 회전 (dragDelta.y → pitch 감소)
        pitch -= dragDelta.y * sensitivity;

        // pitch 각도 제한 (-maxPitch ~ +maxPitch)
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);

        // yaw 값을 -180 ~ +180 사이로 정리 (회전 값 누적 방지)
        yaw = Mathf.Repeat(yaw + 180f, 360f) - 180f;

        dragDelta = Vector2.zero;
    }

    private void LateUpdate()
    {
        if (!target) return;

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 offset = rot * Vector3.back * distance + Vector3.up * height;

        transform.position = target.position + offset;
        transform.rotation = rot;
    }

    public float Yaw => yaw;
}
