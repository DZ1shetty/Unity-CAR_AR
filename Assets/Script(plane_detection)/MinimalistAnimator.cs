using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MinimalistAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Minimalist Settings")]
    public float subtleScale = 1.03f;
    public float pressScale = 0.97f;
    public float animationDuration = 0.15f;
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Color Settings")]
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(0.95f, 0.95f, 0.95f, 1f);
    public Color pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    
    private Vector3 originalScale;
    private Button button;
    private Image buttonImage;
    private Text buttonText;
    
    void Start()
    {
        originalScale = transform.localScale;
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        buttonText = GetComponentInChildren<Text>();
        
        if (buttonImage != null)
            buttonImage.color = normalColor;
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button.interactable)
        {
            // Extremely subtle scale
            LeanTween.scale(gameObject, originalScale * subtleScale, animationDuration)
                .setEase(LeanTweenType.easeOutCubic);
            
            // Gentle color transition
            if (buttonImage != null)
            {
                LeanTween.value(gameObject, UpdateColor, normalColor, hoverColor, animationDuration);
            }
            
            // Subtle text fade
            if (buttonText != null)
            {
                LeanTween.alpha(buttonText.rectTransform, 0.8f, animationDuration);
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
            
            if (buttonText != null)
            {
                LeanTween.alpha(buttonText.rectTransform, 1f, animationDuration);
            }
        }
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (button.interactable)
        {
            // Quick, subtle press
            LeanTween.scale(gameObject, originalScale * pressScale, animationDuration * 0.7f)
                .setEase(LeanTweenType.easeOutQuart);
            
            if (buttonImage != null)
            {
                LeanTween.value(gameObject, UpdateColor, buttonImage.color, pressedColor, animationDuration * 0.5f);
            }
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        if (button.interactable)
        {
            LeanTween.scale(gameObject, originalScale * subtleScale, animationDuration * 0.7f)
                .setEase(LeanTweenType.easeOutQuart);
            
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
}