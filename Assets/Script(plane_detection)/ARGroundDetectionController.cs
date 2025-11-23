using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARGroundDetectionController : MonoBehaviour
{
    [Header("AR Components")]
    [SerializeField] private ARSession arSession;
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARCameraManager cameraManager;
    [SerializeField] private Camera arCamera;

    [Header("Detection Settings")]
    [SerializeField] private float detectionDuration = 0.01f; // INSTANT detection
    [SerializeField] private float minPlaneArea = 0.10f;
    [SerializeField] private float stabilityThreshold = 0.2f;

    [Header("UI References")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Text statusText;
    [SerializeField] private Text instructionText;
    [SerializeField] private Image crosshair;
    [SerializeField] private Button resetButton;

    [Header("Visual Indicators")]
    [SerializeField] private GameObject detectionSphere;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color detectingColor = Color.yellow;
    [SerializeField] private Color completeColor = Color.green;
    [SerializeField] private Color errorColor = Color.red;

    [Header("Audio & Music")]
    [SerializeField] private AudioSource backgroundMusicSource;
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip startDetectionSound;
    [SerializeField] private AudioClip completeDetectionSound;
    [SerializeField] private AudioClip errorSound;
    [SerializeField] private AudioClip resetSound;
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private float musicVolume = 0.5f;
    [SerializeField] private float sfxVolume = 0.7f;

    [Header("Car Management")]
    [SerializeField] private GameObject carPrefab;
    [SerializeField] private Transform carParent;

    [Header("Interaction Management")]
    [SerializeField] private ARInteractionManager interactionManager;

    // Detection state
    private bool isDetecting = false;
    private bool detectionComplete = false;
    private float detectionTimer = 0f;
    private Vector3 lastDetectedPosition;
    private ARPlane currentPlane;
    private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();
    private List<GameObject> spawnedCars = new List<GameObject>();
    private bool isResetting = false;

    // Audio state
    private bool isMusicPlaying = false;
    private Coroutine fadeCoroutine = null;

    // Reset optimization
    private float lastResetTime = 0f;
    private const float MIN_RESET_INTERVAL = 0.2f;

    // Ground detection state for interaction system
    private bool isGroundDetected = false;

    // Events
    public Action<Vector3, ARPlane> OnGroundDetected;
    public Action OnDetectionStarted;
    public Action OnDetectionCancelled;
    public Action OnDetectionReset;
    public Action OnCompleteReset;
    public event Action<bool> OnGroundDetectionStateChanged;

    private void Awake()
    {
        if (arSession == null) arSession = FindObjectOfType<ARSession>();
        if (planeManager == null) planeManager = FindObjectOfType<ARPlaneManager>();
        if (raycastManager == null) raycastManager = FindObjectOfType<ARRaycastManager>();
        if (cameraManager == null) cameraManager = FindObjectOfType<ARCameraManager>();
        if (arCamera == null) arCamera = Camera.main;
        if (interactionManager == null) interactionManager = FindObjectOfType<ARInteractionManager>();

        SetupAudioSources();

        if (carParent == null)
        {
            GameObject carParentObj = new GameObject("SpawnedCars");
            carParent = carParentObj.transform;
        }
    }

    private void SetupAudioSources()
    {
        if (sfxAudioSource == null)
        {
            sfxAudioSource = GetComponent<AudioSource>();
            if (sfxAudioSource == null)
            {
                sfxAudioSource = gameObject.AddComponent<AudioSource>();
                sfxAudioSource.playOnAwake = false;
                sfxAudioSource.loop = false;
                sfxAudioSource.spatialBlend = 0f;
                sfxAudioSource.volume = sfxVolume;
            }
        }

        if (backgroundMusicSource == null)
        {
            GameObject musicObj = GameObject.Find("BackgroundMusic");
            if (musicObj == null)
            {
                musicObj = new GameObject("BackgroundMusic");
                DontDestroyOnLoad(musicObj);
            }

            backgroundMusicSource = musicObj.GetComponent<AudioSource>();
            if (backgroundMusicSource == null)
            {
                backgroundMusicSource = musicObj.AddComponent<AudioSource>();
                backgroundMusicSource.playOnAwake = false;
                backgroundMusicSource.loop = true;
                backgroundMusicSource.spatialBlend = 0f;
                backgroundMusicSource.volume = 0;
            }
        }
    }

    private void Start()
    {
        InitializeUI();
        ValidateARComponents();

        if (backgroundMusic != null)
        {
            PlayBackgroundMusic(backgroundMusic, true);
        }

        SetGroundDetectionState(false);
    }

    private void InitializeUI()
    {
        SafeSetSlider(progressSlider, 0f, 1f, 0f, false);
        UpdateCrosshairColor(normalColor);

        if (detectionSphere != null) detectionSphere.SetActive(false);

        if (resetButton != null)
        {
            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(OnResetButtonClicked);
            resetButton.gameObject.SetActive(true);
            resetButton.interactable = true;
        }

        UpdateStatusText("Point camera at ground to start detection");
        UpdateInstructionText("Find a flat surface to begin");
    }

    private void ValidateARComponents()
    {
        // UI robust error reporting
        if (arSession == null) ShowError("ERROR: AR Session missing.");
        if (planeManager == null) ShowError("ERROR: AR Plane Manager missing.");
        if (raycastManager == null) ShowError("ERROR: AR Raycast Manager missing.");
        if (arCamera == null) ShowError("ERROR: AR Camera missing.");
        if (cameraManager == null) ShowError("ERROR: AR Camera Manager missing.");
    }

    private void Update()
    {
        if (isResetting || detectionComplete ||
            !raycastManager || !raycastManager.enabled || !planeManager || !planeManager.enabled) return;

        // INSTANT ground detection: as soon as a valid plane is found, complete!
        if (!detectionComplete)
        {
            TryInstantGroundDetection();
        }
    }

    private void TryInstantGroundDetection()
    {
        List<Vector2> samplePoints = new List<Vector2>
        {
            new Vector2(Screen.width * 0.5f, Screen.height * 0.5f),
            new Vector2(Screen.width * 0.35f, Screen.height * 0.5f),
            new Vector2(Screen.width * 0.65f, Screen.height * 0.5f),
            new Vector2(Screen.width * 0.5f, Screen.height * 0.35f),
            new Vector2(Screen.width * 0.5f, Screen.height * 0.65f)
        };

        raycastHits.Clear();
        ARPlane foundPlane = null;
        Vector3 hitPosition = Vector3.zero;

        foreach (var pt in samplePoints)
        {
            List<ARRaycastHit> results = new List<ARRaycastHit>();
            if (raycastManager.Raycast(pt, results, TrackableType.PlaneWithinPolygon))
            {
                foreach (var hit in results)
                {
                    ARPlane plane = planeManager.GetPlane(hit.trackableId);
                    if (plane != null && IsValidGroundPlane(plane))
                    {
                        foundPlane = plane;
                        hitPosition = hit.pose.position;
                        break;
                    }
                }
            }
            if (foundPlane != null) break;
        }

        if (foundPlane != null)
        {
            detectionTimer = detectionDuration;
            lastDetectedPosition = hitPosition;
            currentPlane = foundPlane;
            CompleteDetectionInstant();
        }
        else
        {
            ShowSearchingUI();
        }
    }

    private bool IsValidGroundPlane(ARPlane plane)
    {
        if (plane.alignment != PlaneAlignment.HorizontalUp) return false;
        if (plane.size.x * plane.size.y < minPlaneArea) return false;
#if UNITY_2021_3_OR_NEWER
        if (plane.classification == PlaneClassification.Wall || plane.classification == PlaneClassification.Ceiling) return false;
#endif
        return true;
    }

    private void CompleteDetectionInstant()
    {
        isDetecting = false;
        detectionComplete = true;

        UpdateStatusText("Ground detected!");
        UpdateInstructionText("You can now place objects.");
        UpdateCrosshairColor(completeColor);
        SafeSetSlider(progressSlider, 0f, 1f, 1f, false);

        if (resetButton != null)
        {
            resetButton.gameObject.SetActive(true);
            resetButton.interactable = true;
        }

        if (detectionSphere != null)
        {
            detectionSphere.SetActive(true);
            detectionSphere.transform.position = lastDetectedPosition;
        }

        PlaySoundEffect(completeDetectionSound);
        SetGroundDetectionState(true);

        if (interactionManager != null)
        {
            interactionManager.GroundDetected();
        }

        OnGroundDetected?.Invoke(lastDetectedPosition, currentPlane);

        PlaceAndScaleCarOnPlane(currentPlane, lastDetectedPosition);

        Debug.Log($"Instant ground detection completed at position: {lastDetectedPosition}");
    }

    private void ShowSearchingUI()
    {
        UpdateStatusText("Searching for ground...");
        UpdateInstructionText("Move your device to find a flat surface");
        UpdateCrosshairColor(normalColor);
        SafeSetSlider(progressSlider, 0f, 1f, 0f, false);
        if (detectionSphere != null) detectionSphere.SetActive(false);
        if (resetButton != null) resetButton.interactable = true;
    }

    private void ShowError(string message)
    {
        UpdateStatusText(message);
        UpdateInstructionText("Try restarting the app or check device compatibility.");
        UpdateCrosshairColor(errorColor);
        SafeSetSlider(progressSlider, 0f, 1f, 0f, false);
        if (resetButton != null) resetButton.interactable = false;
        if (detectionSphere != null) detectionSphere.SetActive(false);
        PlaySoundEffect(errorSound);
        Debug.LogError(message);
    }

    private void SafeSetSlider(Slider slider, float min, float max, float value, bool interactable)
    {
        if (slider == null) return;
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = Mathf.Clamp(value, min, max);
        slider.interactable = interactable;
    }

    private void PlaceAndScaleCarOnPlane(ARPlane plane, Vector3 center)
    {
        if (carPrefab == null || carParent == null || plane == null) return;
        RemoveAllCarsInstant();

        GameObject car = Instantiate(carPrefab, center, Quaternion.identity, carParent);
        spawnedCars.Add(car);

        Vector2 planeSize = plane.size;
        var meshRenderers = car.GetComponentsInChildren<MeshRenderer>();
        Bounds carBounds = meshRenderers.Length > 0 ? meshRenderers[0].bounds : new Bounds(car.transform.position, Vector3.one);
        foreach (var mr in meshRenderers) carBounds.Encapsulate(mr.bounds);

        float carWidth = carBounds.size.x;
        float carLength = carBounds.size.z;
        float scaleFactor = 1.0f;
        float targetWidth = planeSize.x * 0.8f;
        float targetLength = planeSize.y * 0.8f;

        if (carWidth > 0.001f && carLength > 0.001f)
        {
            float widthScale = targetWidth / carWidth;
            float lengthScale = targetLength / carLength;
            scaleFactor = Mathf.Min(widthScale, lengthScale, 1.0f);
        }

        car.transform.localScale = carPrefab.transform.localScale * scaleFactor;
        car.transform.position = center;
        car.transform.rotation = Quaternion.LookRotation(arCamera.transform.forward, Vector3.up);
    }

    #region UI Utility Methods
    private void UpdateStatusText(string message)
    {
        if (statusText != null) statusText.text = message;
    }
    private void UpdateInstructionText(string message)
    {
        if (instructionText != null) instructionText.text = message;
    }
    private void UpdateCrosshairColor(Color color)
    {
        if (crosshair != null) crosshair.color = color;
    }
    #endregion

    #region State Management, Reset, Audio, Helper Methods

    private void SetGroundDetectionState(bool detected)
    {
        if (isGroundDetected != detected)
        {
            isGroundDetected = detected;
            OnGroundDetectionStateChanged?.Invoke(detected);
        }
    }

    /// <summary>
    /// Used by other scripts to check if ground is detected
    /// </summary>
    public bool IsGroundDetected()
    {
        return isGroundDetected;
    }

    private void OnResetButtonClicked()
    {
        if (Time.time - lastResetTime < MIN_RESET_INTERVAL) return;
        lastResetTime = Time.time;
        PlaySoundEffect(buttonClickSound);
        UltraFastReset();
    }

    public void UltraFastReset()
    {
        if (isResetting) return;
        StartCoroutine(UltraFastResetCoroutine());
    }

    private IEnumerator UltraFastResetCoroutine()
    {
        isResetting = true;
        if (resetButton != null) resetButton.interactable = false;
        UpdateStatusText("Resetting...");
        UpdateCrosshairColor(normalColor);
        if (detectionSphere != null) detectionSphere.SetActive(false);

        RemoveAllCarsInstant();
        if (raycastManager != null) raycastManager.enabled = false;
        if (planeManager != null) planeManager.enabled = false;

        ClearAllPlanesInstant();

        if (arSession != null)
        {
            arSession.Reset();
        }

        if (planeManager != null) planeManager.enabled = true;
        if (raycastManager != null) raycastManager.enabled = true;

        isDetecting = false;
        detectionComplete = false;
        detectionTimer = 0f;
        currentPlane = null;
        SetGroundDetectionState(false);

        if (interactionManager != null)
        {
            interactionManager.GroundLost();
        }

        InitializeUI();
        if (resetButton != null) resetButton.interactable = true;
        OnDetectionReset?.Invoke();
        OnCompleteReset?.Invoke();

        isResetting = false;
        yield break;
    }

    private void RemoveAllCarsInstant()
    {
        try
        {
            List<GameObject> carsToRemove = new List<GameObject>(spawnedCars);
            spawnedCars.Clear();

            foreach (GameObject car in carsToRemove)
            {
                if (car != null)
                {
                    DestroyImmediate(car);
                }
            }

            if (carParent != null && carParent.childCount > 0)
            {
                while (carParent.childCount > 0)
                {
                    DestroyImmediate(carParent.GetChild(0).gameObject);
                }
            }

            GameObject[] potentialCars = GameObject.FindGameObjectsWithTag("Car");
            foreach (GameObject obj in potentialCars)
            {
                if (obj != null && obj != carPrefab)
                {
                    DestroyImmediate(obj);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Error during car cleanup: {e.Message}");
        }
    }

    private void ClearAllPlanesInstant()
    {
        if (planeManager == null || planeManager.trackables == null) return;

        try
        {
            List<ARPlane> planesToRemove = new List<ARPlane>();
            foreach (ARPlane plane in planeManager.trackables)
            {
                if (plane != null)
                {
                    planesToRemove.Add(plane);
                }
            }

            foreach (ARPlane plane in planesToRemove)
            {
                if (plane != null && plane.gameObject != null)
                {
                    DestroyImmediate(plane.gameObject);
                }
            }

            if (planeManager.subsystem != null)
            {
                planeManager.subsystem.Stop();
                planeManager.subsystem.Start();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Error during plane cleanup: {e.Message}");
        }
    }

    public void PlayBackgroundMusic(AudioClip musicClip, bool fadeIn = true)
    {
        if (backgroundMusicSource == null || musicClip == null) return;
        if (isMusicPlaying && backgroundMusicSource.clip == musicClip) return;
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        backgroundMusicSource.clip = musicClip;

        if (fadeIn)
        {
            backgroundMusicSource.volume = 0;
            backgroundMusicSource.Play();
            fadeCoroutine = StartCoroutine(FadeMusicVolume(0, musicVolume, 1.5f));
        }
        else
        {
            backgroundMusicSource.volume = musicVolume;
            backgroundMusicSource.Play();
        }
        isMusicPlaying = true;
    }

    public void StopBackgroundMusic(bool fadeOut = true)
    {
        if (backgroundMusicSource == null || !isMusicPlaying) return;
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        if (fadeOut)
        {
            fadeCoroutine = StartCoroutine(FadeMusicVolumeAndStop(backgroundMusicSource.volume, 0, 1.0f));
        }
        else
        {
            backgroundMusicSource.Stop();
            isMusicPlaying = false;
        }
    }

    private IEnumerator FadeMusicVolume(float startVolume, float targetVolume, float duration)
    {
        if (backgroundMusicSource == null) yield break;
        float elapsedTime = 0;
        while (elapsedTime < duration)
        {
            backgroundMusicSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        backgroundMusicSource.volume = targetVolume;
        fadeCoroutine = null;
    }

    private IEnumerator FadeMusicVolumeAndStop(float startVolume, float targetVolume, float duration)
    {
        yield return FadeMusicVolume(startVolume, targetVolume, duration);
        if (backgroundMusicSource != null)
        {
            backgroundMusicSource.Stop();
            isMusicPlaying = false;
        }
    }

    private void PlaySoundEffect(AudioClip clip)
    {
        if (sfxAudioSource != null && clip != null && !sfxAudioSource.mute)
        {
            sfxAudioSource.PlayOneShot(clip, sfxVolume);
        }
    }

    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxAudioSource != null)
        {
            sfxAudioSource.volume = sfxVolume;
        }
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (backgroundMusicSource != null && isMusicPlaying)
        {
            backgroundMusicSource.volume = musicVolume;
        }
    }

    #endregion
}