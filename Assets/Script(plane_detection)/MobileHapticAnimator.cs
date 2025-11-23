using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MobileHapticAnimator : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Mobile Touch Settings")]
    public float touchScale = 0.94f;
    public float releaseScale = 1.02f;
    public float touchDuration = 0.08f;
    public float releaseDuration = 0.12f;
    
    [Header("Visual Feedback")]
    public Color normalColor = new Color(0.2f, 0.5f, 1f, 1f);
    public Color touchColor = new Color(0.15f, 0.4f, 0.8f, 1f);
    public float brightnessBoost = 1.1f;
    
    [Header("Performance Optimization")]
    public bool useSimpleEasing = true;
    
    private Vector3 originalScale;
    private Color originalColor;
    private Button button;
    private Image buttonImage;
    private bool isTouching = false;
    
    void Start()
    {
        originalScale = transform.localScale;
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        originalColor = buttonImage != null ? buttonImage.color : normalColor;
        
        // Optimize for mobile
        if (Application.isMobilePlatform)
        {
            touchDuration *= 0.8f; // Faster on mobile
            releaseDuration *= 0.8f;
        }
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (button.interactable && !isTouching)
        {
            isTouching = true;
            
            // Quick scale down with mobile-optimized easing
            LeanTween.cancel(gameObject);
            LeanTween.scale(gameObject, originalScale * touchScale, touchDuration)
                .setEase(useSimpleEasing ? LeanTweenType.easeOutQuart : LeanTweenType.easeOutCubic);
            
            // Color feedback
            if (buttonImage != null)
            {
                LeanTween.value(gameObject, UpdateColor, originalColor, touchColor, touchDuration);
            }
            
            // Simulate haptic feedback (visual cue for haptic)
            CreateTouchRipple();
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        if (button.interactable && isTouching)
        {
            isTouching = false;
            
            // Bounce back with slight overshoot
            LeanTween.cancel(gameObject);
            LeanTween.scale(gameObject, originalScale * releaseScale, releaseDuration * 0.6f)
                .setEase(LeanTweenType.easeOutBack)
                .setOnComplete(() => {
                    LeanTween.scale(gameObject, originalScale, releaseDuration * 0.4f)
                        .setEase(LeanTweenType.easeOutQuart);
                });
            
            // Color return
            if (buttonImage != null)
            {
                LeanTween.value(gameObject, UpdateColor, buttonImage.color, originalColor, releaseDuration);
            }
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Minimal hover for mobile (mostly for mouse/trackpad users)
        if (!isTouching && !Application.isMobilePlatform)
        {
            LeanTween.scale(gameObject, originalScale * 1.01f, 0.1f);
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isTouching)
        {
            LeanTween.cancel(gameObject);
            LeanTween.scale(gameObject, originalScale, 0.1f);
            if (buttonImage != null)
            {
                LeanTween.value(gameObject, UpdateColor, buttonImage.color, originalColor, 0.1f);
            }
        }
    }
    
    void CreateTouchRipple()
    {
        // Lightweight ripple effect for touch feedback
        GameObject ripple = new GameObject("TouchRipple");
        ripple.transform.SetParent(transform);
        ripple.transform.localPosition = Vector3.zero;
        ripple.transform.localScale = Vector3.zero;
        
        Image rippleImage = ripple.AddComponent<Image>();
        rippleImage.color = new Color(1f, 1f, 1f, 0.3f);
        rippleImage.raycastTarget = false;
        
        // Quick ripple animation
        LeanTween.scale(ripple, Vector3.one * 1.5f, 0.3f)
            .setEase(LeanTweenType.easeOutQuart);
        LeanTween.alpha(ripple.GetComponent<RectTransform>(), 0f, 0.3f)
            .setOnComplete(() => Destroy(ripple));
    }
    
    void UpdateColor(Color color)
    {
        if (buttonImage != null)
            buttonImage.color = color;
    }
}