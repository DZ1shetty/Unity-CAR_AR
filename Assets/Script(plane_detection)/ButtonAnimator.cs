using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening; // If you have DOTween (optional)

public class ButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Animation Settings")]
    public float hoverScale = 1.1f;
    public float clickScale = 0.9f;
    public float animationDuration = 0.2f;
    
    private Vector3 originalScale;
    private Button button;
    
    void Start()
    {
        originalScale = transform.localScale;
        button = GetComponent<Button>();
    }
    
    // When mouse enters button
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button.interactable)
        {
            LeanTween.scale(gameObject, originalScale * hoverScale, animationDuration)
                .setEase(LeanTweenType.easeOutBack);
        }
    }
    
    // When mouse leaves button
    public void OnPointerExit(PointerEventData eventData)
    {
        if (button.interactable)
        {
            LeanTween.scale(gameObject, originalScale, animationDuration)
                .setEase(LeanTweenType.easeOutBack);
        }
    }
    
    // When button is pressed
    public void OnPointerDown(PointerEventData eventData)
    {
        if (button.interactable)
        {
            LeanTween.scale(gameObject, originalScale * clickScale, animationDuration * 0.5f)
                .setEase(LeanTweenType.easeOutQuart);
        }
    }
    
    // When button is released
    public void OnPointerUp(PointerEventData eventData)
    {
        if (button.interactable)
        {
            LeanTween.scale(gameObject, originalScale * hoverScale, animationDuration * 0.5f)
                .setEase(LeanTweenType.easeOutQuart);
        }
    }
}