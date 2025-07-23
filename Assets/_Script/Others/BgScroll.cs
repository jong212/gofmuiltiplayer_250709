using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class BgScroll : MonoBehaviour
{
    [SerializeField] private RawImage _img;
    [SerializeField] private Image _star1;
    [SerializeField] private Image _star2;
    [SerializeField] private Image _star3;
    [SerializeField] private Image _star4;
    [SerializeField] private Image _star5;

    [SerializeField] private float _x, _y;

    private void Start()
    {
        AnimateStar(_star1);
        AnimateStar(_star2);
        AnimateStar(_star3);
        AnimateStar(_star4);
        AnimateStar(_star5);
    }

    private void AnimateStar(Image star)
    {
        float delay = Random.Range(0f, 1.5f); // ·£´ý µô·¹ÀÌ

        // Å©±â
        star.transform.DOScale(1.2f, 1.5f)
            .SetDelay(delay)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutQuad);

        // ¾ËÆÄ
        star.DOFade(0.8f, 1.5f)
            .SetDelay(delay)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    void Update()
    {
        _img.uvRect = new Rect(_img.uvRect.position + new Vector2(_x, _y) * Time.deltaTime, _img.uvRect.size);
    }
}
