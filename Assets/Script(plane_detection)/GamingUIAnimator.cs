using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GamingUIAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Gaming UI Settings")]
    public Color accentColor = new Color(0.35f, 0.61f, 0.98f, 1f); // Discord Blue
    public Color normalColor = new Color(0.18f, 0.20f, 0.25f, 1f); // Dark Gray
    public Color hoverColor = new Color(0.22f, 0.24f, 0.29f, 1f);
    public float glowIntensity = 1.5f;
    public float animationDuration = 0.2f;
    
    private Vector3 originalScale;
    private Button button;
    private Image buttonImage;
    private Outline buttonOutline;
    
    void Start()
    {
        originalScale = transform.localScale;
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        
        // Add outline component if it doesn't exist
        buttonOutline = GetComponent<Outline>();
        if (buttonOutline == null)
        {
            buttonOutline = gameObject.AddComponent<Outline>();
        }
        
        SetupGamingStyle();
    }
    
    void SetupGamingStyle()
    {
        if (buttonImage != null)
            buttonImage.color = normalColor;
        
        if (buttonOutline != null)
        {
            buttonOutline.effectColor = accentColor;
            buttonOutline.effectDistance = Vector2.zero;
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button.interactable)
        {
            // Scale with slight overshoot
            LeanTween.scale(gameObject, originalScale * 1.05f, animationDuration)
                .setEase(LeanTweenType.easeOutBack);
            
            // Color transition
            if (buttonImage != null)
            {
                LeanTween.value(gameObject, UpdateColor, normalColor, hoverColor, animationDuration);
            }
            
            // Glow effect
            if (buttonOutline != null)
            {
                LeanTween.value(gameObject, UpdateOutlineDistance, Vector2.zero, Vector2.one * 2f, animationDuration);
                LeanTween.value(gameObject, UpdateOutlineAlpha, 0f, 1f, animationDuration);
            }
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (button.interactable)
        {
            LeanTween.scale(gameObject, originalScale, animationDuration)
                .setEase(LeanTweenType.easeOutCubic);
            
            if (buttonImage != null)
            {
                LeanTween.value(gameObject, UpdateColor, buttonImage.color, normalColor, animationDuration);
            }
            
            if (buttonOutline != null)
            {
                LeanTween.value(gameObject, UpdateOutlineDistance, buttonOutline.effectDistance, Vector2.zero, animationDuration);
                LeanTween.value(gameObject, UpdateOutlineAlpha, 1f, 0f, animationDuration);
            }
        }
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (button.interactable)
        {
            LeanTween.scale(gameObject, originalScale * 0.95f, animationDuration * 0.5f);
            
            // Bright accent flash
            if (buttonImage != null)
            {
                Color flashColor = Color.Lerp(hoverColor, accentColor, 0.3f);
                LeanTween.value(gameObject, UpdateColor, buttonImage.color, flashColor, animationDuration * 0.3f);
            }
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        if (button.interactable)
        {
            LeanTween.scale(gameObject, originalScale * 1.05f, animationDuration * 0.5f);
            
            if (buttonImage != null)
            {
                LeanTween.value(gameObject, UpdateColor, buttonImage.color, hoverColor, animationDuration * 0.5f);
            }
        }
    }
    
    void UpdateColor(Color color)
    {
        if (buttonImage != null)
            buttonImage.color = color;
    }
    
    void UpdateOutlineDistance(Vector2 distance)
    {
        if (buttonOutline != null)
            buttonOutline.effectDistance = distance;
    }
    
    void UpdateOutlineAlpha(float alpha)
    {
        if (buttonOutline != null)
        {
            Color outlineColor = buttonOutline.effectColor;
            outlineColor.a = alpha;
            buttonOutline.effectColor = outlineColor;
        }
    }
}