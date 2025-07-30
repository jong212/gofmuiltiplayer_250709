using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class BgScroll : MonoBehaviour
{
    [Header("Background Elements")]
    [SerializeField] private RawImage _scrollBackground;
    [SerializeField] private float scrollSpeedX = 0.01f;
    [SerializeField] private float scrollSpeedY = 0f;

    [Header("Floating Elements (Left/Right Move)")]
    [SerializeField] private Image _floatLeft;
    //[SerializeField] private Image _floatRight;
    [SerializeField] private float floatDistance = 50f;
    [SerializeField] private float floatDuration = 2f;

    [Header("Rotating Elements")]
    [SerializeField] private Image _rotateLeft;
    [SerializeField] private Image _rotateRight;
    [SerializeField] private float rotateAngle = 5f;
    [SerializeField] private float rotateDuration = 2f;

    [Header("Boat Movement")]
    [SerializeField] private RectTransform boat;
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private float boatMoveDuration = 30f;

    private float boatWidth;

    private void Start()
    {
        AnimateFloat(_floatLeft, floatDistance);

        AnimateRotate(_rotateLeft, rotateAngle);
        AnimateRotate(_rotateRight, -rotateAngle);

        boatWidth = boat.rect.width;
        StartBoatLoop();
    }

    private void AnimateFloat(Image target, float distance)
    {
        RectTransform rect = target.GetComponent<RectTransform>();
        float originalX = rect.anchoredPosition.x;

        rect.DOAnchorPosX(originalX + distance, floatDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    private void AnimateRotate(Image target, float angle)
    {
        target.transform.DORotate(new Vector3(0, 0, angle), rotateDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    private void StartBoatLoop()
    {
        float rightOutside = canvasRect.rect.width / 2 + boatWidth;
        float leftOutside = -canvasRect.rect.width / 2 - boatWidth;

        boat.anchoredPosition = new Vector2(rightOutside, boat.anchoredPosition.y);

        boat.DOAnchorPosX(leftOutside, boatMoveDuration)
            .SetEase(Ease.Linear)
            .OnComplete(StartBoatLoop); // Àç±Í ¹Ýº¹
    }

    private void Update()
    {
        ScrollBackground();
    }

    private void ScrollBackground()
    {
        if (_scrollBackground != null)
        {
            _scrollBackground.uvRect = new Rect(
                _scrollBackground.uvRect.position + new Vector2(scrollSpeedX, scrollSpeedY) * Time.deltaTime,
                _scrollBackground.uvRect.size
            );
        }
    }
    public void RegisterWindTarget(Transform t)
    {
        t.DORotate(new Vector3(0, 0, 5), 2f, RotateMode.LocalAxisAdd)
         .SetEase(Ease.InOutSine)
         .SetLoops(-1, LoopType.Yoyo)
         .SetDelay(Random.Range(0f, 1f));
    }

}
