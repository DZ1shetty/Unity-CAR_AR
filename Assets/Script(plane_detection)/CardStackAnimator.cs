using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class CardStackAnimator : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Basic Animation Settings")]
    public float liftHeight = 15f;
    public float animationDuration = 0.25f;
    public bool enableColorFeedback = true;
    public Color pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    public Color hoverColor = new Color(0.95f, 0.95f, 0.95f, 1f);
    
    [Header("Performance Settings")]
    public bool enablePerformanceOptimization = true;
    public bool reducedAnimationsOnMobile = true;
    
    [Header("Audio Feedback")]
    public bool enableAudioFeedback = true;
    public AudioClip hoverSound;
    public AudioClip pressSound;
    public AudioClip releaseSound;
    [Range(0f, 1f)]
    public float audioVolume = 0.5f;
    
    [Header("Particle Effects")]
    public bool enableParticleEffects = true;
    public ParticleSystem clickParticles;
    public ParticleSystem hoverParticles;
    public Color particleColor = Color.white;
    public int particleCount = 10;
    
    [Header("Theme System")]
    public bool enableThemes = true;
    public ButtonTheme currentTheme;
    public List<ButtonTheme> availableThemes = new List<ButtonTheme>();
    
    [Header("Gesture Recognition")]
    public bool enableGestures = true;
    public float swipeThreshold = 50f;
    public float longPressTime = 0.8f;
    public bool enableDoubleClick = true;
    public float doubleClickTime = 0.3f;
    
    [Header("Analytics & Tracking")]
    public bool enableAnalytics = true;
    public bool logInteractions = false;
    
    [Header("Animation Presets")]
    public bool enablePresets = true;
    public AnimationPreset currentPreset;
    public List<AnimationPreset> presets = new List<AnimationPreset>();
    
    // Events for gesture callbacks
    public System.Action OnSwipeLeft;
    public System.Action OnSwipeRight;
    public System.Action OnSwipeUp;
    public System.Action OnSwipeDown;
    public System.Action OnLongPress;
    public System.Action OnDoubleClick;
    
    // Private variables
    private Vector3 originalPosition;
    private Vector3 originalScale;
    private Color originalColor;
    private bool isTouching = false;
    private bool isLowPerformanceDevice = false;
    private Image buttonImage;
    private float currentAnimationDuration;
    private Vector2 touchStartPosition;
    private float touchStartTime;
    
    // Audio variables
    private AudioSource audioSource;
    private bool audioInitialized = false;
    
    // Theme variables
    private Material gradientMaterial;
    private bool themeInitialized = false;
    
    // Gesture variables
    private float lastClickTime = 0f;
    private int clickCount = 0;
    private bool isLongPressing = false;
    private Coroutine longPressCoroutine;
    
    // Analytics variables
    public ButtonAnalytics analytics = new ButtonAnalytics();
    private float hoverStartTime = 0f;
    
    // Define custom colors to avoid Unity compatibility issues
    private static readonly Color DarkGray = new Color(0.3f, 0.3f, 0.3f, 1f);
    private static readonly Color LightGray = new Color(0.7f, 0.7f, 0.7f, 1f);
    
    [System.Serializable]
    public class ButtonTheme
    {
        public string themeName = "Default";
        public Color normalColor = Color.white;
        public Color hoverColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        public Color pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        public Color accentColor = Color.blue;
        public Gradient backgroundGradient;
        public bool useGradient = false;
    }
    
    [System.Serializable]
    public class ButtonAnalytics
    {
        public int totalClicks = 0;
        public int totalHovers = 0;
        public int totalLongPresses = 0;
        public int totalDoubleClicks = 0;
        public float totalHoverTime = 0f;
        public float averageClickSpeed = 0f;
        public List<float> clickTimes = new List<float>();
        public DateTime lastInteraction;
    }
    
    [System.Serializable]
    public class AnimationPreset
    {
        public string presetName = "Default";
        public float liftHeight = 15f;
        public float animationDuration = 0.25f;
        public LeanTweenType easeType = LeanTweenType.easeOutCubic;
        public Vector3 scaleMultiplier = Vector3.one;
        public Color pressedColorMultiplier = new Color(0.7f, 0.7f, 0.7f, 1f);
        public bool enableBounce = true;
        public float bounceIntensity = 1.2f;
    }
    
    void Start()
    {
        InitializeComponent();
        InitializeAudio();
        InitializeThemes();
        InitializePresets();
        DetectMobileDevice();
    }
    
    void InitializeComponent()
    {
        originalPosition = transform.localPosition;
        originalScale = transform.localScale;
        buttonImage = GetComponent<Image>();
        
        if (buttonImage != null)
        {
            originalColor = buttonImage.color;
        }
        
        currentAnimationDuration = animationDuration;
    }
    
    void InitializeAudio()
    {
        if (enableAudioFeedback)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            audioSource.volume = audioVolume;
            audioSource.playOnAwake = false;
            audioInitialized = true;
        }
    }
    
    void InitializeThemes()
    {
        if (enableThemes)
        {
            // Initialize default themes if none exist
            if (availableThemes.Count == 0)
            {
                CreateDefaultThemes();
            }
            
            if (currentTheme == null && availableThemes.Count > 0)
            {
                currentTheme = availableThemes[0];
            }
            
            if (currentTheme != null)
            {
                if (currentTheme.useGradient && buttonImage != null)
                {
                    CreateGradientMaterial();
                }
                ApplyTheme(currentTheme);
                themeInitialized = true;
            }
        }
    }
    
    void InitializePresets()
    {
        if (enablePresets && presets.Count == 0)
        {
            CreateDefaultPresets();
        }
        
        if (currentPreset == null && presets.Count > 0)
        {
            currentPreset = presets[0];
            ApplyPreset(currentPreset);
        }
    }
    
    void CreateDefaultThemes()
    {
        availableThemes.Add(new ButtonTheme
        {
            themeName = "Default",
            normalColor = Color.white,
            hoverColor = new Color(0.95f, 0.95f, 0.95f),
            pressedColor = new Color(0.8f, 0.8f, 0.8f),
            accentColor = Color.blue
        });
        
        availableThemes.Add(new ButtonTheme
        {
            themeName = "Dark",
            normalColor = new Color(0.2f, 0.2f, 0.2f),
            hoverColor = new Color(0.3f, 0.3f, 0.3f),
            pressedColor = new Color(0.1f, 0.1f, 0.1f),
            accentColor = Color.cyan
        });
        
        availableThemes.Add(new ButtonTheme
        {
            themeName = "Neon",
            normalColor = new Color(0.1f, 0.1f, 0.1f),
            hoverColor = new Color(0.5f, 0f, 0.5f),
            pressedColor = new Color(1f, 0f, 1f),
            accentColor = Color.magenta
        });
    }
    
    void CreateDefaultPresets()
    {
        presets.Add(new AnimationPreset 
        { 
            presetName = "Gentle", 
            liftHeight = 10f, 
            animationDuration = 0.3f,
            easeType = LeanTweenType.easeOutSine,
            bounceIntensity = 1.1f
        });
        
        presets.Add(new AnimationPreset 
        { 
            presetName = "Energetic", 
            liftHeight = 25f, 
            animationDuration = 0.15f,
            easeType = LeanTweenType.easeOutBack,
            bounceIntensity = 1.5f
        });
        
        presets.Add(new AnimationPreset 
        { 
            presetName = "Smooth", 
            liftHeight = 12f, 
            animationDuration = 0.4f,
            easeType = LeanTweenType.easeOutCubic,
            enableBounce = false
        });
        
        presets.Add(new AnimationPreset 
        { 
            presetName = "Bouncy", 
            liftHeight = 20f, 
            animationDuration = 0.2f,
            easeType = LeanTweenType.easeOutElastic,
            bounceIntensity = 1.8f
        });
    }
    
    void DetectMobileDevice()
    {
        isLowPerformanceDevice = Application.isMobilePlatform;
        
        if (isLowPerformanceDevice && reducedAnimationsOnMobile)
        {
            currentAnimationDuration = animationDuration * 0.7f;
        }
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        isTouching = true;
        touchStartPosition = eventData.position;
        touchStartTime = Time.time;
        
        PerformPressAnimation();
        PlaySound(pressSound);
        
        if (enableGestures)
        {
            HandleGestureStart(eventData);
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        isTouching = false;
        
        PerformReleaseAnimation();
        PlaySound(releaseSound);
        TrackClick();
        
        if (enableGestures)
        {
            HandleGestureEnd(eventData);
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isTouching)
        {
            PerformHoverAnimation();
            PlaySound(hoverSound);
            TrackHoverStart();
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isTouching)
        {
            PerformHoverExitAnimation();
            TrackHoverEnd();
        }
    }
    
    void HandleGestureStart(PointerEventData eventData)
    {
        // Double click detection
        if (enableDoubleClick)
        {
            if (Time.time - lastClickTime < doubleClickTime)
            {
                clickCount++;
                if (clickCount >= 2)
                {
                    OnDoubleClick?.Invoke();
                    CreateDoubleClickEffect();
                    TrackDoubleClick();
                    clickCount = 0;
                    return;
                }
            }
            else
            {
                clickCount = 1;
            }
            lastClickTime = Time.time;
        }
        
        // Start long press detection
        if (longPressCoroutine != null)
        {
            StopCoroutine(longPressCoroutine);
        }
        longPressCoroutine = StartCoroutine(LongPressDetection());
    }
    
    void HandleGestureEnd(PointerEventData eventData)
    {
        // Stop long press detection
        isLongPressing = false;
        if (longPressCoroutine != null)
        {
            StopCoroutine(longPressCoroutine);
            longPressCoroutine = null;
        }
        
        // Swipe detection
        Vector2 swipeVector = eventData.position - touchStartPosition;
        if (swipeVector.magnitude > swipeThreshold)
        {
            DetectSwipeDirection(swipeVector);
        }
    }
    
    void PerformPressAnimation()
    {
        LeanTween.cancel(gameObject);
        
        Vector3 targetPosition = originalPosition + Vector3.back * liftHeight;
        Vector3 targetScale = originalScale * 0.95f;
        
        LeanTween.moveLocal(gameObject, targetPosition, currentAnimationDuration)
            .setEase(currentPreset?.easeType ?? LeanTweenType.easeOutCubic);
        
        LeanTween.scale(gameObject, targetScale, currentAnimationDuration)
            .setEase(currentPreset?.easeType ?? LeanTweenType.easeOutCubic);
        
        if (enableColorFeedback)
        {
            Color targetColor = currentTheme?.pressedColor ?? pressedColor;
            UpdateButtonColorWithTheme(targetColor);
        }
        
        CreateClickParticles(Vector2.zero);
        
        if (currentPreset != null && currentPreset.enableBounce)
        {
            PerformPresetBasedAnimation();
        }
    }
    
    void PerformReleaseAnimation()
    {
        LeanTween.cancel(gameObject);
        
        LeanTween.moveLocal(gameObject, originalPosition, currentAnimationDuration)
            .setEase(currentPreset?.easeType ?? LeanTweenType.easeOutCubic);
        
        LeanTween.scale(gameObject, originalScale, currentAnimationDuration)
            .setEase(currentPreset?.easeType ?? LeanTweenType.easeOutCubic);
        
        if (enableColorFeedback)
        {
            Color targetColor = currentTheme?.normalColor ?? originalColor;
            UpdateButtonColorWithTheme(targetColor);
        }
    }
    
    void PerformHoverAnimation()
    {
        if (isTouching) return;
        
        LeanTween.cancel(gameObject);
        
        Vector3 targetPosition = originalPosition + Vector3.back * (liftHeight * 0.3f);
        Vector3 targetScale = originalScale * 1.02f;
        
        LeanTween.moveLocal(gameObject, targetPosition, currentAnimationDuration * 0.8f)
            .setEase(LeanTweenType.easeOutSine);
        
        LeanTween.scale(gameObject, targetScale, currentAnimationDuration * 0.8f)
            .setEase(LeanTweenType.easeOutSine);
        
        if (enableColorFeedback)
        {
            Color targetColor = currentTheme?.hoverColor ?? hoverColor;
            UpdateButtonColorWithTheme(targetColor);
        }
    }
    
    void PerformHoverExitAnimation()
    {
        if (isTouching) return;
        
        LeanTween.cancel(gameObject);
        
        LeanTween.moveLocal(gameObject, originalPosition, currentAnimationDuration * 0.8f)
            .setEase(LeanTweenType.easeOutSine);
        
        LeanTween.scale(gameObject, originalScale, currentAnimationDuration * 0.8f)
            .setEase(LeanTweenType.easeOutSine);
        
        if (enableColorFeedback)
        {
            Color targetColor = currentTheme?.normalColor ?? originalColor;
            UpdateButtonColorWithTheme(targetColor);
        }
    }
    
    void PerformPresetBasedAnimation()
    {
        if (currentPreset == null) return;
        
        if (currentPreset.enableBounce)
        {
            LeanTween.scale(gameObject, originalScale * currentPreset.bounceIntensity, currentAnimationDuration * 0.5f)
                .setEase(LeanTweenType.easeOutBack)
                .setOnComplete(() => {
                    LeanTween.scale(gameObject, originalScale, currentAnimationDuration * 0.5f)
                        .setEase(LeanTweenType.easeOutCubic);
                });
        }
    }
    
    // Audio Methods
    void PlaySound(AudioClip clip)
    {
        if (audioInitialized && clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, audioVolume);
        }
    }
    
    // Particle Effects Methods
    void CreateClickParticles(Vector2 position)
    {
        if (!enableParticleEffects || isLowPerformanceDevice) return;
        
        GameObject particleObj = new GameObject("ClickParticles");
        particleObj.transform.SetParent(transform);
        particleObj.transform.localPosition = Vector3.zero;
        
        ParticleSystem particles = particleObj.AddComponent<ParticleSystem>();
        
        var main = particles.main;
        main.startLifetime = 0.5f;
        main.startSpeed = 5f;
        main.startColor = particleColor;
        main.maxParticles = particleCount;
        
        var emission = particles.emission;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0.0f, particleCount)
        });
        
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.5f;
        
        var velocityOverLifetime = particles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(2f);
        
        // Auto destroy after particles finish
        Destroy(particleObj, 1f);
    }
    
    // Theme Methods
    void CreateGradientMaterial()
    {
        Shader gradientShader = Shader.Find("UI/Default");
        gradientMaterial = new Material(gradientShader);
        
        if (buttonImage != null)
        {
            buttonImage.material = gradientMaterial;
        }
    }
    
    public void ApplyTheme(ButtonTheme theme)
    {
        currentTheme = theme;
        if (buttonImage != null)
        {
            buttonImage.color = theme.normalColor;
            originalColor = theme.normalColor;
        }
    }
    
    public void CycleThemes()
    {
        if (availableThemes.Count > 0)
        {
            int currentIndex = availableThemes.IndexOf(currentTheme);
            int nextIndex = (currentIndex + 1) % availableThemes.Count;
            ApplyTheme(availableThemes[nextIndex]);
        }
    }
    
    void UpdateButtonColorWithTheme(Color color)
    {
        if (buttonImage != null)
        {
            if (currentTheme != null && currentTheme.useGradient)
            {
                Color gradientColor = currentTheme.backgroundGradient.Evaluate(Time.time % 1f);
                buttonImage.color = Color.Lerp(color, gradientColor, 0.5f);
            }
            else
            {
                buttonImage.color = color;
            }
        }
    }
    
    // Gesture Methods
    IEnumerator LongPressDetection()
    {
        isLongPressing = true;
        yield return new WaitForSeconds(longPressTime);
        
        if (isTouching && isLongPressing)
        {
            OnLongPress?.Invoke();
            CreateLongPressEffect();
            TrackLongPress();
        }
    }
    
    void CreateLongPressEffect()
    {
        GameObject ring = new GameObject("LongPressRing");
        ring.transform.SetParent(transform);
        ring.transform.localPosition = Vector3.zero;
        ring.transform.localScale = Vector3.one;
        
        Image ringImage = ring.AddComponent<Image>();
        ringImage.color = new Color(1f, 1f, 0f, 0.5f);
        ringImage.raycastTarget = false;
        
        Texture2D ringTexture = CreateRingTexture(128, 0.8f, 1f);
        ringImage.sprite = Sprite.Create(ringTexture, new Rect(0, 0, 128, 128), Vector2.one * 0.5f);
        
        LeanTween.scale(ring, Vector3.one * 1.5f, 0.5f)
            .setEase(LeanTweenType.easeOutElastic);
        LeanTween.alpha(ring.GetComponent<RectTransform>(), 0f, 0.5f)
            .setOnComplete(() => Destroy(ring));
    }
    
    void DetectSwipeDirection(Vector2 swipeVector)
    {
        Vector2 normalizedSwipe = swipeVector.normalized;
        
        if (Mathf.Abs(normalizedSwipe.x) > Mathf.Abs(normalizedSwipe.y))
        {
            if (normalizedSwipe.x > 0)
            {
                OnSwipeRight?.Invoke();
                CreateSwipeEffect(Vector3.right);
            }
            else
            {
                OnSwipeLeft?.Invoke();
                CreateSwipeEffect(Vector3.left);
            }
        }
        else
        {
            if (normalizedSwipe.y > 0)
            {
                OnSwipeUp?.Invoke();
                CreateSwipeEffect(Vector3.up);
            }
            else
            {
                OnSwipeDown?.Invoke();
                CreateSwipeEffect(Vector3.down);
            }
        }
    }
    
    void CreateSwipeEffect(Vector3 direction)
    {
        GameObject trail = new GameObject("SwipeTrail");
        trail.transform.SetParent(transform);
        trail.transform.localPosition = Vector3.zero;
        
        Image trailImage = trail.AddComponent<Image>();
        trailImage.color = new Color(0f, 1f, 1f, 0.7f);
        trailImage.raycastTarget = false;
        
        Vector3 endPosition = direction * 100f;
        
        LeanTween.moveLocal(trail, endPosition, 0.3f)
            .setEase(LeanTweenType.easeOutQuart);
        LeanTween.alpha(trail.GetComponent<RectTransform>(), 0f, 0.3f)
            .setOnComplete(() => Destroy(trail));
    }
    
    void CreateDoubleClickEffect()
    {
        for (int i = 0; i < 3; i++)
        {
            GameObject circle = new GameObject($"DoubleClickCircle_{i}");
            circle.transform.SetParent(transform);
            circle.transform.localPosition = Vector3.zero;
            circle.transform.localScale = Vector3.zero;
            
            Image circleImage = circle.AddComponent<Image>();
            circleImage.color = new Color(1f, 0f, 1f, 0.6f);
            circleImage.raycastTarget = false;
            
            Texture2D circleTexture = CreateCircleTexture(64);
            circleImage.sprite = Sprite.Create(circleTexture, new Rect(0, 0, 64, 64), Vector2.one * 0.5f);
            
            float delay = i * 0.1f;
            float scale = 1f + (i * 0.3f);
            
            LeanTween.scale(circle, Vector3.one * scale, 0.4f)
                .setDelay(delay)
                .setEase(LeanTweenType.easeOutBack);
            LeanTween.alpha(circle.GetComponent<RectTransform>(), 0f, 0.4f)
                .setDelay(delay)
                .setOnComplete(() => Destroy(circle));
        }
    }
    
    // Utility Methods
    Texture2D CreateRingTexture(int size, float innerRadius, float outerRadius)
    {
        Texture2D texture = new Texture2D(size, size);
        Vector2 center = Vector2.one * (size * 0.5f);
        float inner = size * innerRadius * 0.5f;
        float outer = size * outerRadius * 0.5f;
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance >= inner && distance <= outer)
                {
                    float alpha = 1f - Mathf.Abs(distance - (inner + outer) * 0.5f) / ((outer - inner) * 0.5f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        texture.Apply();
        return texture;
    }
    
    Texture2D CreateCircleTexture(int size)
    {
        Texture2D texture = new Texture2D(size, size);
        Vector2 center = Vector2.one * (size * 0.5f);
        float radius = size * 0.4f;
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= radius)
                {
                    float alpha = 1f - (distance / radius);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        texture.Apply();
        return texture;
    }
    
    // Analytics Methods
    void TrackClick()
    {
        if (!enableAnalytics) return;
        
        analytics.totalClicks++;
        analytics.lastInteraction = System.DateTime.Now;
        
        float clickSpeed = Time.time - touchStartTime;
        analytics.clickTimes.Add(clickSpeed);
        
        float totalTime = 0f;
        foreach (float time in analytics.clickTimes)
        {
            totalTime += time;
        }
        analytics.averageClickSpeed = totalTime / analytics.clickTimes.Count;
        
        if (logInteractions)
        {
            Debug.Log($"Button clicked. Total clicks: {analytics.totalClicks}, Click speed: {clickSpeed:F2}s");
        }
    }
    
    void TrackHoverStart()
    {
        if (!enableAnalytics) return;
        
        analytics.totalHovers++;
        hoverStartTime = Time.time;
    }
    
    void TrackHoverEnd()
    {
        if (!enableAnalytics) return;
        
        if (hoverStartTime > 0f)
        {
            analytics.totalHoverTime += Time.time - hoverStartTime;
            hoverStartTime = 0f;
        }
    }
    
    void TrackLongPress()
    {
        if (!enableAnalytics) return;
        
        analytics.totalLongPresses++;
        analytics.lastInteraction = System.DateTime.Now;
        
        if (logInteractions)
        {
            Debug.Log($"Long press detected. Total long presses: {analytics.totalLongPresses}");
        }
    }
    
    void TrackDoubleClick()
    {
        if (!enableAnalytics) return;
        
        analytics.totalDoubleClicks++;
        analytics.lastInteraction = System.DateTime.Now;
        
        if (logInteractions)
        {
            Debug.Log($"Double click detected. Total double clicks: {analytics.totalDoubleClicks}");
        }
    }
    
    public string GetAnalyticsReport()
    {
        return $"Button Analytics Report:\n" +
               $"Total Clicks: {analytics.totalClicks}\n" +
               $"Total Hovers: {analytics.totalHovers}\n" +
               $"Total Hover Time: {analytics.totalHoverTime:F2}s\n" +
               $"Average Click Speed: {analytics.averageClickSpeed:F2}s\n" +
               $"Long Presses: {analytics.totalLongPresses}\n" +
               $"Double Clicks: {analytics.totalDoubleClicks}\n" +
               $"Last Interaction: {analytics.lastInteraction}";
    }
    
    public void ResetAnalytics()
    {
        analytics = new ButtonAnalytics();
    }
    
    // Preset Methods
    public void ApplyPreset(AnimationPreset preset)
    {
        currentPreset = preset;
        liftHeight = preset.liftHeight;
        animationDuration = preset.animationDuration;
        
        DetectMobileDevice();
    }
    
    public void ApplyPreset(string presetName)
    {
        AnimationPreset preset = presets.Find(p => p.presetName == presetName);
        if (preset != null)
        {
            ApplyPreset(preset);
        }
    }
    
    // Public utility methods
    public void SetAudioVolume(float volume)
    {
        audioVolume = Mathf.Clamp01(volume);
        if (audioSource != null)
        {
            audioSource.volume = audioVolume;
        }
    }
    
    public void ToggleAudio()
    {
        enableAudioFeedback = !enableAudioFeedback;
    }
    
    public void ToggleParticles()
    {
        enableParticleEffects = !enableParticleEffects;
    }
    
    public void ToggleGestures()
    {
        enableGestures = !enableGestures;
    }
    
    public void ToggleAnalytics()
    {
        enableAnalytics = !enableAnalytics;
    }
}