using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIpress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private Transform targetTransform;

    private void Awake()
    {
        targetTransform = transform;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetTransform.DOKill(); // 중복 애니메이션 제거
        targetTransform.DOScale(0.9f, 0.05f).SetEase(Ease.OutQuad);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("test1");
        targetTransform.DOKill();
        targetTransform.DOScale(1f, 0.1f).SetEase(Ease.OutBack);
    }
}
