using UnityEngine;
using UnityEngine.EventSystems;

public class DragInput : MonoBehaviour, IDragHandler 
{
    [SerializeField] private float dragScale = 0.1f; // 👈 기존 감도와 맞추기 위해 스케일 조절

    public void OnDrag(PointerEventData eventData)
    {
        if (CameraController.Instance != null)
        {
            CameraController.Instance.OnDragDelta(eventData.delta * dragScale);
        }
    }
}
