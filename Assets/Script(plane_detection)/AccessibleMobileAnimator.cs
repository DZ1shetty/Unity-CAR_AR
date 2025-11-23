using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class AccessibleMobileAnimator : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Accessibility Settings")]
    public bool respectSystemSettings = true;
    public float subtleScale = 1.03f;
    public float pressScale = 0.97f;
    public float quickDuration = 0.1f;
    
    [Header("High Contrast Mode")]
    public Color highContrastNormal = Color.white;
    public Color highContrastPressed = new Color(0.8f, 0.8f, 0.8f, 1f);
    
    [Header("Large Text Support")]
    public bool supportLargeText = true;
    
    private Vector3 originalScale;
    private Color originalColor;
    private Button button;
    private Image buttonImage;
    private Text buttonText;
    private bool isHighContrast = false;
    private bool isReducedMotion = false;
    
    void Start()
    {
        originalScale = transform.localScale;
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        buttonText = GetComponentInChildren<Text>();
        originalColor = buttonImage != null ? buttonImage.color : Color.white;
        
        // Check system accessibility settings
        CheckAccessibilitySettings();
    }
    
    void CheckAccessibilitySettings()
    {
        // Simulate checking system accessibility settings
        // In a real app, you'd check actual system settings
        if (respectSystemSettings)
        {
            // Check for high contrast
            if (SystemInfo.deviceType == DeviceType.Handheld)
            {
                // Mobile-specific accessibility checks would go here
                isHighContrast = false; // Would be actual system check
                isReducedMotion = false; // Would be actual system check
            }
        }
        
        ApplyAccessibilitySettings();
    }
    
    void ApplyAccessibilitySettings()
    {
        if (isHighContrast && buttonImage != null)
        {
            buttonImage.color = highContrastNormal;
        }
        
        if (supportLargeText && buttonText != null)
        {
            // Adjust layout for larger text
            float textScale = 1f; // Would be based on system text size
            buttonText.fontSize = Mathf.RoundToInt(buttonText.fontSize * textScale);
        }
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (button.interactable)
        {
            if (isReducedMotion)
            {
                // Minimal animation for reduced motion
                if (buttonImage != null)
                {
                    Color pressedColor = isHighContrast ? highContrastPressed : originalColor * 0.8f;
                    buttonImage.color = pressedColor;
                }
                return;
            }
            
            // Standard press animation
            LeanTween.cancel(gameObject);
            LeanTween.scale(gameObject, originalScale * pressScale, quickDuration)
                .setEase(LeanTweenType.easeOutQuart);
            
            // Visual feedback
            if (buttonImage != null)
            {
                Color pressedColor = isHighContrast ? highContrastPressed : originalColor * 0.9f;
                LeanTween.value(gameObject, UpdateColor, originalColor, pressedColor, quickDuration);
            }
            
            // Haptic feedback trigger point
            TriggerHapticFeedback();
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        if (button.interactable)
        {
            if (isReducedMotion)
            {
                // Instant return for reduced motion
                if (buttonImage != null)
                {
                    Color normalColor = isHighContrast ? highContrastNormal : originalColor;
                    buttonImage.color = normalColor;
                }
                return;
            }
            
            // Standard release animation
            LeanTween.cancel(gameObject);
            LeanTween.scale(gameObject, originalScale * subtleScale, quickDuration * 0.6f)
                .setEase(LeanTweenType.easeOutBack)
                .setOnComplete(() => {
                    LeanTween.scale(gameObject, originalScale, quickDuration * 0.4f);
                });
            
            // Color return
            if (buttonImage != null)
            {
                Color normalColor = isHighContrast ? highContrastNormal : originalColor;
                LeanTween.value(gameObject, UpdateColor, buttonImage.color, normalColor, quickDuration);
            }
        }
    }
    
    void TriggerHapticFeedback()
    {
        // On mobile, this would trigger actual haptic feedback
        if (Application.isMobilePlatform)
        {
            // Handheld.Vibrate(); // Uncomment for actual haptic feedback
            
            // Visual haptic cue for testing
            CreateHapticVisualCue();
        }
    }
    
    void CreateHapticVisualCue()
    {
        // Create a brief visual indicator for haptic feedback
        GameObject hapticCue = new GameObject("HapticCue");
        hapticCue.transform.SetParent(transform);
        hapticCue.transform.localPosition = Vector3.zero;
        hapticCue.transform.localScale = Vector3.zero;
        
        Image cueImage = hapticCue.AddComponent<Image>();
        cueImage.color = new Color(1f, 1f, 1f, 0.5f);
        cueImage.raycastTarget = false;
        
        // Quick flash
        LeanTween.scale(hapticCue, Vector3.one * 1.2f, 0.1f)
            .setEase(LeanTweenType.easeOutQuart);
        LeanTween.alpha(hapticCue.GetComponent<RectTransform>(), 0f, 0.1f)
            .setOnComplete(() => Destroy(hapticCue));
    }
    
    void UpdateColor(Color color)
    {
        if (buttonImage != null)
            buttonImage.color = color;
    }
    
    // Public method to update accessibility settings
    public void UpdateAccessibilitySettings(bool highContrast, bool reducedMotion)
    {
        isHighContrast = highContrast;
        isReducedMotion = reducedMotion;
        ApplyAccessibilitySettings();
    }
}