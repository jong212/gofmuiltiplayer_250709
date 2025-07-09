using UnityEngine;

public class PaddleController : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Ball3DController>(out var ball))
        {
            Vector3 normal = transform.forward; // 라켓이 바라보는 방향 기준으로 반사
            ball.Reflect(normal);
        }
    }
}
