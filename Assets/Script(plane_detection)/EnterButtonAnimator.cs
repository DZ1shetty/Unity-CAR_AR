using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.Collections;

public class MobileTouchEnterButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Mobile Touch Settings")]
    public float touchPressScale = 0.95f;
    public float touchDuration = 0.2f;
    public float hapticIntensity = 0.5f;
    
    [Header("Innovative Wave Effect")]
    public float waveAmplitude = 10f;
    public float waveFrequency = 2f;
    public int waveSegments = 8;
    
    [Header("Touch Ripple")]
    public GameObject rippleEffect;
    public float rippleLifetime = 1f;
    
    private Vector3 originalScale;
    private Color originalColor;
    private Image buttonImage;
    private Button button;
    private bool isTouching = false;
    private Vector2 touchPosition;
    
    void Start()
    {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        originalScale = transform.localScale;
        originalColor = buttonImage.color;
        
        // Mobile-specific: Disable mouse hover effects
        #if UNITY_ANDROID || UNITY_IOS
        // This script only works on mobile
        #else
        Debug.LogWarning("This script is optimized for mobile devices only!");
        #endif
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!button.interactable) return;
        
        isTouching = true;
        touchPosition = eventData.position;
        
        // Haptic feedback (mobile only)
        #if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
        #endif
        
        // Touch press animation
        transform.DOScale(originalScale * touchPressScale, touchDuration * 0.5f)
            .SetEase(Ease.OutQuart);
        
        // Create touch ripple at exact touch position
        CreateTouchRipple(eventData.position);
        
        // Start innovative wave effect
        StartCoroutine(WaveEffect());
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!button.interactable || !isTouching) return;
        
        isTouching = false;
        
        // Execute button click with mobile-optimized animation
        ExecuteMobileClick();
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (isTouching)
        {
            isTouching = false;
            ReturnToNormal();
        }
    }
    
    private void ExecuteMobileClick()
    {
        // Stop any ongoing animations
        transform.DOKill();
        buttonImage.DOKill();
        
        var sequence = DOTween.Sequence();
        
        // 1. Quick bounce back
        sequence.Append(transform.DOScale(originalScale * 1.1f, 0.1f)
            .SetEase(Ease.OutBack));
        
        // 2. Innovative "digital dissolve" effect
        sequence.Join(StartDigitalDissolveEffect());
        
        // 3. Return to normal and proceed
        sequence.AppendInterval(0.2f);
        sequence.AppendCallback(() => {
            ReturnToNormal();
            StartCoroutine(ProceedToARScene());
        });
    }
    
    private Tween StartDigitalDissolveEffect()
    {
        // Create a digital "pixelation" effect
        return buttonImage.DOColor(Color.white, 0.15f)
            .SetLoops(4, LoopType.Yoyo)
            .SetEase(Ease.Flash);
    }
    
    private void CreateTouchRipple(Vector2 screenPosition)
    {
        if (rippleEffect != null)
        {
            // Convert screen position to world position
            Vector3 worldPos;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                transform as RectTransform, 
                screenPosition, 
                Camera.main, 
                out worldPos);
            
            GameObject ripple = Instantiate(rippleEffect, worldPos, Quaternion.identity, transform.parent);
            
            // Animate ripple
            ripple.transform.localScale = Vector3.zero;
            ripple.transform.DOScale(Vector3.one * 2f, rippleLifetime)
                .SetEase(Ease.OutQuart);
            
            Image rippleImg = ripple.GetComponent<Image>();
            if (rippleImg != null)
            {
                rippleImg.DOFade(0f, rippleLifetime)
                    .OnComplete(() => Destroy(ripple));
            }
        }
    }
    
    private IEnumerator WaveEffect()
    {
        float elapsedTime = 0f;
        Vector3[] originalPositions = new Vector3[waveSegments];
        
        // Store original positions
        for (int i = 0; i < waveSegments; i++)
        {
            originalPositions[i] = transform.position;
        }
        
        while (isTouching && elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime;
            
            // Create wave motion
            float wave = Mathf.Sin(elapsedTime * waveFrequency * Mathf.PI * 2) * waveAmplitude;
            transform.position = originalPositions[0] + Vector3.up * wave * 0.1f;
            
            yield return null;
        }
        
        // Return to original position
        transform.DOMove(originalPositions[0], 0.2f);
    }
    
    private void ReturnToNormal()
    {
        transform.DOScale(originalScale, touchDuration)
            .SetEase(Ease.OutBack);
        buttonImage.DOColor(originalColor, touchDuration);
    }
    
    private IEnumerator ProceedToARScene()
    {
        yield return new WaitForSeconds(0.3f);
        
        // Mobile-specific scene transition
        Debug.Log("Loading AR Scene on Mobile Device...");
        
        // Add your AR scene loading logic here
        // Example: SceneManager.LoadScene("ARScene");
    }
}