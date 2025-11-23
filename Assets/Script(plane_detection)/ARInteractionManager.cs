using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;
using CarAccessories; // Add this line

public class ARInteractionManager : MonoBehaviour
{
    #region AR Components
    [Header("AR Components")]
    [SerializeField] private ARGroundDetectionController groundDetectionController;
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private Camera arCamera;
    [SerializeField] private GameObject bmwCarPrefab;

    public GameObject carPrefab; // For UI/DB integration
    public AccessoryDatabase accessoryDatabase;

    [Header("Plane Manager")]
    public ARPlaneManager planeManager;
    #endregion

    #region UI Elements
    [Header("UI Elements")]
    public GameObject placementUI;
    public GameObject interactionUI;
    public GameObject detailPanel;
    public Button startInteractionButton;
    public Button exitInteractionButton;
    public Button closeDetailButton;

    // UI Text elements for accessory details
    public Text accessoryNameText;
    public Text manufacturerText;
    public Text modelNumberText;
    public Text priceText;
    public Text descriptionText;
    public Text featuresText;
    public Image accessoryImage;
    #endregion

    #region Interaction Settings
    [Header("Interaction Settings")]
    [SerializeField] private LayerMask interactableLayerMask;
    [SerializeField] private float rotationSensitivity = 5f;
    [SerializeField] private float maxRotationAngle = 45f;
    #endregion

    #region Steering Wheel Settings
    [Header("Steering Wheel Settings")]
    [SerializeField] private Transform steeringWheel;
    [SerializeField] private float steeringReturnSpeed = 5f;
    [SerializeField] private float steeringDeadzone = 0.1f;
    [SerializeField] private AudioSource steeringAudioSource;
    [SerializeField] private AudioClip steeringSound;
    #endregion

    // Car reference and AR state
    private GameObject placedCar;
    private ARPlane selectedPlane;

    // UI/DB state
    private bool isCarPlaced = false;

    // Interaction state
    private bool isInInteractionMode = false;
    private bool isInteracting = false;
    private bool isSteeringWheelGrabbed = false;
    private Vector2 lastTouchPosition;
    private float currentSteeringAngle = 0f;
    private Quaternion originalSteeringRotation;

    // Accessory detail state
    private bool interactionModeActive = false;

    // Events
    public Action<float> OnSteeringWheelRotated;
    public Action OnSteeringWheelReleased;
    public Action<GameObject, Vector3> OnObjectTapped;
    public Action<GameObject, ARPlane> OnCarPlaced;

    // Cache for performance
    private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();
    private RaycastHit hit;

    #region Initialization

    private void Awake()
    {
        if (groundDetectionController == null)
            groundDetectionController = GetComponentInParent<ARGroundDetectionController>();

        if (raycastManager == null)
            raycastManager = FindObjectOfType<ARRaycastManager>();

        if (arCamera == null)
            arCamera = Camera.main;
    }

    private void Start()
    {
        // UI Setup
        if (placementUI != null) placementUI.SetActive(true);
        if (interactionUI != null) interactionUI.SetActive(false);
        if (detailPanel != null) detailPanel.SetActive(false);

        if (startInteractionButton != null)
            startInteractionButton.onClick.AddListener(StartInteractionMode);

        if (exitInteractionButton != null)
            exitInteractionButton.onClick.AddListener(ExitInteractionMode);

        if (closeDetailButton != null)
            closeDetailButton.onClick.AddListener(CloseAccessoryDetails);

        // AR Event Setup
        if (groundDetectionController != null)
        {
            groundDetectionController.OnGroundDetected += HandleGroundDetected;
            groundDetectionController.OnDetectionReset += HandleDetectionReset;
        }

        if (steeringWheel != null)
        {
            originalSteeringRotation = steeringWheel.localRotation;
        }
    }

    private void OnDestroy()
    {
        if (groundDetectionController != null)
        {
            groundDetectionController.OnGroundDetected -= HandleGroundDetected;
            groundDetectionController.OnDetectionReset -= HandleDetectionReset;
        }
    }

    #endregion

    #region Update Logic

    private void Update()
    {
        if (isInInteractionMode)
        {
            HandleTouchInput();
            ReturnSteeringWheelToCenter();
        }
    }

    private void HandleTouchInput()
    {
        if (IsPointerOverUI()) return;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    HandleTouchBegan(touch.position);
                    break;
                case TouchPhase.Moved:
                    HandleTouchMoved(touch.position);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    HandleTouchEnded();
                    break;
            }
        }
    }

    #endregion

    #region Touch Handling

    // Keeping IsPointerOverUI in case you use EventSystem for AR interactions
    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        if (Input.touchCount == 0) return false;

        return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
    }

    private void HandleTouchBegan(Vector2 touchPosition)
    {
        lastTouchPosition = touchPosition;
        isInteracting = true;

        Ray ray = arCamera.ScreenPointToRay(touchPosition);

        if (Physics.Raycast(ray, out hit, 100f, interactableLayerMask))
        {
            // Steering wheel check (by reference or as child)
            if ((steeringWheel != null && hit.transform == steeringWheel) || IsChildOfSteeringWheel(hit.transform))
            {
                isSteeringWheelGrabbed = true;
                PlaySteeringSound(0.5f);
            }
            else
            {
                OnObjectTapped?.Invoke(hit.transform.gameObject, hit.point);

                EnhancedCarPartInteraction carPart = hit.transform.GetComponent<EnhancedCarPartInteraction>();
                if (carPart != null)
                {
                    carPart.OnInteract();
                }
            }
        }
    }

    private void HandleTouchMoved(Vector2 touchPosition)
    {
        if (!isInteracting) return;

        if (isSteeringWheelGrabbed && steeringWheel != null)
        {
            float touchDelta = (touchPosition.x - lastTouchPosition.x) / Screen.width;
            float newAngle = currentSteeringAngle + (touchDelta * rotationSensitivity * 100f);

            newAngle = Mathf.Clamp(newAngle, -maxRotationAngle, maxRotationAngle);

            if (Mathf.Abs(newAngle - currentSteeringAngle) > steeringDeadzone)
            {
                SetSteeringWheelRotation(newAngle);

                float volume = Mathf.Abs(touchDelta) * 2f;
                PlaySteeringSound(volume);

                OnSteeringWheelRotated?.Invoke(currentSteeringAngle / maxRotationAngle);
            }
        }

        lastTouchPosition = touchPosition;
    }

    private void HandleTouchEnded()
    {
        isInteracting = false;

        if (isSteeringWheelGrabbed)
        {
            isSteeringWheelGrabbed = false;
            OnSteeringWheelReleased?.Invoke();
        }
    }

    private bool IsChildOfSteeringWheel(Transform obj)
    {
        if (steeringWheel == null) return false;

        Transform parent = obj.parent;
        while (parent != null)
        {
            if (parent == steeringWheel)
                return true;

            parent = parent.parent;
        }

        return false;
    }

    #endregion

    #region Steering Wheel Controls

    private void ReturnSteeringWheelToCenter()
    {
        if (!isSteeringWheelGrabbed && steeringWheel != null && Mathf.Abs(currentSteeringAngle) > 0.01f)
        {
            float newAngle = Mathf.Lerp(currentSteeringAngle, 0f, Time.deltaTime * steeringReturnSpeed);

            if (Mathf.Abs(newAngle) < steeringDeadzone)
                newAngle = 0f;

            SetSteeringWheelRotation(newAngle);

            if (Mathf.Abs(currentSteeringAngle) > steeringDeadzone)
            {
                OnSteeringWheelRotated?.Invoke(currentSteeringAngle / maxRotationAngle);
            }
        }
    }

    private void SetSteeringWheelRotation(float angle)
    {
        if (steeringWheel == null) return;

        currentSteeringAngle = angle;
        steeringWheel.localRotation = originalSteeringRotation * Quaternion.Euler(0f, 0f, -currentSteeringAngle);
    }

    private void PlaySteeringSound(float volume)
    {
        if (steeringAudioSource != null && steeringSound != null)
        {
            steeringAudioSource.PlayOneShot(steeringSound, Mathf.Clamp01(volume));
        }
    }

    #endregion

    #region Mode Management

    // Called when a car is placed on a plane
    public void RegisterPlacedCar(GameObject car, ARPlane plane)
    {
        placedCar = car;
        selectedPlane = plane;

        // Try to find the steering wheel using component first, then by name
        if (car != null)
        {
            var steeringComponent = car.GetComponentInChildren<SteeringWheelInteraction>();
            if (steeringComponent != null)
            {
                SetSteeringWheel(steeringComponent.transform);
            }
            else
            {
                // fallback: recursive search by name
                var found = FindChildByName(car.transform, "Steering Wheel");
                if (found != null)
                    SetSteeringWheel(found);
            }
        }

        OnCarPlaced?.Invoke(car, plane);
    }

    private Transform FindChildByName(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var result = FindChildByName(child, name);
            if (result != null) return result;
        }
        return null;
    }

    public void EnableInteractionMode()
    {
        if (planeManager != null)
        {
            planeManager.enabled = false;
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(false);
            }
        }

        SetCarInteractable(true);

        isInInteractionMode = true;
        interactionModeActive = true;

        // UI Switch
        if (placementUI != null) placementUI.SetActive(false);
        if (interactionUI != null) interactionUI.SetActive(true);

        Debug.Log("Interaction mode enabled");
    }

    public void EnablePlaneDetectionMode()
    {
        if (planeManager != null)
        {
            planeManager.enabled = true;
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(true);
            }
        }

        SetCarInteractable(false);

        isInInteractionMode = false;
        interactionModeActive = false;

        // UI Switch
        if (placementUI != null) placementUI.SetActive(true);
        if (interactionUI != null) interactionUI.SetActive(false);
        if (detailPanel != null) detailPanel.SetActive(false);

        Debug.Log("Plane detection mode enabled");
    }

    private void SetCarInteractable(bool interactable)
    {
        if (placedCar != null)
        {
            EnhancedCarPartInteraction[] carParts = placedCar.GetComponentsInChildren<EnhancedCarPartInteraction>();
            foreach (var part in carParts)
            {
                part.enabled = interactable;
            }

            SteeringWheelInteraction steeringWheelComponent = placedCar.GetComponentInChildren<SteeringWheelInteraction>();
            if (steeringWheelComponent != null)
            {
                steeringWheelComponent.enabled = interactable;
            }

            Collider[] colliders = placedCar.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
            {
                collider.enabled = interactable;
            }
        }
    }

    // UI/DB Methods
    public void PlaceCar(Vector3 position, Quaternion rotation)
    {
        if (!isCarPlaced && carPrefab != null)
        {
            placedCar = Instantiate(carPrefab, position, rotation);
            isCarPlaced = true;

            if (startInteractionButton != null)
                startInteractionButton.gameObject.SetActive(true);
        }
    }

    public void StartInteractionMode()
    {
        if (isCarPlaced)
        {
            EnableInteractionMode();
        }
    }

    public void ExitInteractionMode()
    {
        EnablePlaneDetectionMode();

        if (placedCar != null)
        {
            Destroy(placedCar);
            placedCar = null;
        }

        isCarPlaced = false;
        interactionModeActive = false;
    }

    public bool IsInteractionModeActive()
    {
        return interactionModeActive;
    }
    #endregion

    #region Accessory UI

    // Conversion method for accessory data to accessory info
    private AccessoryInfo ConvertToAccessoryInfo(AccessoryData data)
    {
        if (data == null) return null;
        return data.ToAccessoryInfo();
    }

    // Updated method to display accessory details
    public void ShowAccessoryDetails(string accessoryID)
    {
        if (accessoryDatabase != null)
        {
            AccessoryInfo info = accessoryDatabase.GetAccessoryByID(accessoryID);
            
            if (info != null && detailPanel != null)
            {
                // Check which UI manager to use
                AccessoryInfoPanelManager infoPanel = FindObjectOfType<AccessoryInfoPanelManager>();
                if (infoPanel != null)
                {
                    infoPanel.DisplayAccessoryInfo(info);
                }
                else
                {
                    AccessoryDetailManager detailManager = FindObjectOfType<AccessoryDetailManager>();
                    if (detailManager != null)
                    {
                        detailManager.ShowAccessoryDetails(accessoryID);
                    }
                    else
                    {
                        // Fall back to original implementation
                        if (accessoryNameText != null) accessoryNameText.text = info.accessoryName;
                        if (manufacturerText != null) manufacturerText.text = info.manufacturer;
                        if (modelNumberText != null) modelNumberText.text = info.modelNumber;
                        if (priceText != null) priceText.text = "$" + info.price.ToString();
                        if (descriptionText != null) descriptionText.text = info.description;
                        
                        if (featuresText != null)
                        {
                            string featuresList = "";
                            foreach (string feature in info.features)
                            {
                                featuresList += "• " + feature + "\n";
                            }
                            featuresText.text = featuresList;
                        }
                        
                        if (accessoryImage != null && info.accessoryImage != null)
                        {
                            accessoryImage.sprite = info.accessoryImage;
                            accessoryImage.gameObject.SetActive(true);
                        }
                        else if (accessoryImage != null)
                        {
                            accessoryImage.gameObject.SetActive(false);
                        }
                        
                        detailPanel.SetActive(true);
                        Handheld.Vibrate();
                    }
                }
            }
        }
    }

    public void CloseAccessoryDetails()
    {
        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
        }
    }

    #endregion

    #region Ground Detection Callbacks

    private void HandleGroundDetected(Vector3 position, ARPlane plane)
    {
        Debug.Log("Ground detected at: " + position);

        // Optionally, spawn car here if not placed
        GroundDetected();
    }

    private void HandleDetectionReset()
    {
        isInteracting = false;
        isSteeringWheelGrabbed = false;
        currentSteeringAngle = 0f;

        if (steeringWheel != null)
        {
            steeringWheel.localRotation = originalSteeringRotation;
        }

        EnablePlaneDetectionMode();

        placedCar = null;
        selectedPlane = null;

        GroundLost();
    }

    private bool IsGroundDetectionComplete()
    {
        if (groundDetectionController != null)
        {
            return groundDetectionController.IsGroundDetected();
        }
        return false;
    }

    #endregion

    #region Ground Detection UI

    public void GroundDetected()
    {
        Debug.Log("Ground detected - enabling placement interactions");
        // If you had a UI panel, this would be the place to show it.
    }

    public void GroundLost()
    {
        Debug.Log("Ground lost - disabling placement interactions");
        // Hide any placement UI if desired.
    }

    #endregion

    #region Public Methods

    public void SetSteeringWheel(Transform wheel)
    {
        steeringWheel = wheel;
        if (steeringWheel != null)
        {
            originalSteeringRotation = steeringWheel.localRotation;
        }
    }

    public float GetCurrentSteeringAngle()
    {
        return currentSteeringAngle;
    }

    public float GetNormalizedSteeringAngle()
    {
        return currentSteeringAngle / maxRotationAngle;
    }

    public GameObject GetPlacedCar()
    {
        return placedCar;
    }

    public bool IsInInteractionMode()
    {
        return isInInteractionMode;
    }

    #endregion
}