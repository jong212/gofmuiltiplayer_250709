using UnityEngine;
using DG.Tweening;

[ExecuteAlways]
public class Dotween : MonoBehaviour
{
    public enum TriggerType { AutoPlay, OnClick }
    public enum EffectType { ScaleBounce, Rotate, Fade }

    [Header("Settings")]
    [SerializeField] private TriggerType triggerType = TriggerType.AutoPlay;
    [SerializeField] private EffectType effectType = EffectType.ScaleBounce;
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private Ease ease = Ease.InOutSine;
    [SerializeField] private int loops = -1;

    [Header("Effect Values")]
    [SerializeField] private float scaleMultiplier = 1.1f;
    [SerializeField] private float rotationAmount = 15f;
    [SerializeField] private float fadeTo = 0.2f;

    private SpriteRenderer spriteRenderer;
    private Tween currentTween;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (triggerType == TriggerType.AutoPlay && Application.isPlaying)
        {
            PlayEffect();
        }
    }

    void OnMouseDown()
    {
        if (triggerType == TriggerType.OnClick)
        {
            PlayEffect();
        }
    }

    private void PlayEffect()
    {
        currentTween?.Kill();

        switch (effectType)
        {
            case EffectType.ScaleBounce:
                transform.localScale = Vector3.one;
                currentTween = transform.DOScale(scaleMultiplier, duration)
                    .SetLoops(loops, LoopType.Yoyo)
                    .SetEase(ease);
                break;

            case EffectType.Rotate:
                transform.rotation = Quaternion.identity;
                currentTween = transform.DORotate(Vector3.forward * rotationAmount, duration, RotateMode.LocalAxisAdd)
                    .SetLoops(loops, LoopType.Yoyo)
                    .SetEase(ease);
                break;

            case EffectType.Fade:
                if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    Color original = spriteRenderer.color;
                    spriteRenderer.color = new Color(original.r, original.g, original.b, 1f);
                    currentTween = spriteRenderer.DOFade(fadeTo, duration)
                        .SetLoops(loops, LoopType.Yoyo)
                        .SetEase(ease);
                }
                break;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying) return;

        spriteRenderer = GetComponent<SpriteRenderer>();
        currentTween?.Kill();
        PlayEffect();
    }
#endif

    void OnDestroy()
    {
        currentTween?.Kill();
    }
}
