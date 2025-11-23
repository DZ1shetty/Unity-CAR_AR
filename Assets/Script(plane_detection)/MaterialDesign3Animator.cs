using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MaterialDesign3Animator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Material Design Settings")]
    public float rippleScale = 1.2f;
    public float elevationScale = 1.02f;
    public Color rippleColor = new Color(1f, 1f, 1f, 0.3f);
    public float animationDuration = 0.3f;
    
    [Header("State Colors")]
    public Color normalColor = new Color(0.25f, 0.46f, 0.85f, 1f); // Material Blue
    public Color hoverColor = new Color(0.28f, 0.50f, 0.88f, 1f);
    public Color pressedColor = new Color(0.22f, 0.42f, 0.82f, 1f);
    
    private Vector3 originalScale;
    private Image buttonImage;
    private Button button;
    private GameObject rippleEffect;
    
    void Start()
    {
        originalScale = transform.localScale;
        buttonImage = GetComponent<Image>();
        button = GetComponent<Button>();
        buttonImage.color = normalColor;
        
        CreateRippleEffect();
    }
    
    void CreateRippleEffect()
    {
        // Create ripple overlay
        GameObject rippleObj = new GameObject("RippleEffect");
        rippleObj.transform.SetParent(transform);
        rippleObj.transform.localPosition = Vector3.zero;
        rippleObj.transform.localScale = Vector3.zero;
        
        Image rippleImage = rippleObj.AddComponent<Image>();
        rippleImage.color = rippleColor;
        rippleImage.sprite = buttonImage.sprite;
        rippleImage.raycastTarget = false;
        
        rippleEffect = rippleObj;
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button.interactable)
        {
            // Subtle elevation
            LeanTween.scale(gameObject, originalScale * elevationScale, animationDuration)
                .setEase(LeanTweenType.easeOutCubic);
            
            // Color transition
            LeanTween.value(gameObject, UpdateButtonColor, normalColor, hoverColor, animationDuration);
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (button.interactable)
        {
            LeanTween.scale(gameObject, originalScale, animationDuration)
                .setEase(LeanTweenType.easeOutCubic);
            
            LeanTween.value(gameObject, UpdateButtonColor, buttonImage.color, normalColor, animationDuration);
            
            // Hide ripple
            LeanTween.scale(rippleEffect, Vector3.zero, animationDuration * 0.5f);
        }
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (button.interactable)
        {
            // Ripple effect
            rippleEffect.transform.localScale = Vector3.zero;
            LeanTween.scale(rippleEffect, Vector3.one * rippleScale, animationDuration * 0.7f)
                .setEase(LeanTweenType.easeOutCirc);
            
            // Press color
            LeanTween.value(gameObject, UpdateButtonColor, buttonImage.color, pressedColor, animationDuration * 0.5f);
            
            // Slight scale down
            LeanTween.scale(gameObject, originalScale * 0.98f, animationDuration * 0.3f);
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        if (button.interactable)
        {
            LeanTween.scale(gameObject, originalScale * elevationScale, animationDuration * 0.5f);
            LeanTween.value(gameObject, UpdateButtonColor, buttonImage.color, hoverColor, animationDuration * 0.5f);
        }
    }
    
    void UpdateButtonColor(Color color)
    {
        buttonImage.color = color;
    }
}