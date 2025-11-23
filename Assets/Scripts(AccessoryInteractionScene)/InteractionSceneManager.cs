using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class InteractionSceneManager : MonoBehaviour
{
    [System.Serializable]
    public class AccessoryModel
    {
        public string accessoryName;
        public string manufacturer;
        public float price;
        public string description;
        public string[] features;
        public Sprite accessoryImage;
        public GameObject modelPrefab;
    }
    
    // IMPORTANT: Direct reference to the scene camera 
    [SerializeField] private Camera sceneCamera;
    
    [SerializeField] private AccessoryModel[] accessoryModels;
    [SerializeField] private Transform accessoryModelHolder;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button resetViewButton;
    [SerializeField] private Transform spawnArea;
    [SerializeField] private Vector3 fixedModelPosition = new Vector3(0, 0, 3);
    [SerializeField] private bool useFixedPosition = true;
    
    // UI references for details display
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI manufacturerText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI featuresText;
    [SerializeField] private Image accessoryImageDisplay;
    
    // Visual hints for user guidance
    [SerializeField] private GameObject rotationHint;
    [SerializeField] private GameObject zoomHint;
    [SerializeField] private GameObject moveHint;
    [SerializeField] private float hintFadeTime = 3.0f;
    
    // Model positioning and scaling parameters
    [SerializeField] private float initialDistance = 1.5f;
    [SerializeField] private Vector3 modelOffset = new Vector3(0, -0.25f, 0);
    [SerializeField] private Vector3 initialScale = new Vector3(0.75f, 0.75f, 0.75f);
    
    // Spawn point references for each accessory type
    [SerializeField] private Transform steeringWheelSpawnPoint;
    [SerializeField] private Transform mirrorSpawnPoint;
    [SerializeField] private Transform pedalSpawnPoint; 
    [SerializeField] private Transform exhaustSpawnPoint;
    [SerializeField] private Transform gearStickSpawnPoint;
    [SerializeField] private Transform speedometerSpawnPoint;
    [SerializeField] private Transform defaultSpawnPoint;
    
    // Dictionary to map accessory types to spawn points
    private Dictionary<string, Transform> spawnPointMap;
    
    private GameObject currentAccessoryModel;
    private float rotationSpeed = 30.0f;
    
    // OPTIMIZED ZOOM FACTORS - WIDER RANGE BUT SLOWER, MORE CONTROLLED SPEED
    private float zoomSpeed = 2.5f; // Reduced from 15.0f to 2.5f for smoother control
    private float minZoom = 0.005f; // Even closer than before (was 0.01f) - 10x closer inspection!
    private float maxZoom = 100.0f; // Doubled the range (was 50.0f) - 2x wider zoom range!
    private float currentZoom = 1.0f;
    
    // Movement control
    private bool movementMode = false;
    private float moveSpeed = 0.01f;
    
    private bool isDragging = false;
    private Vector3 previousMousePosition;
    
    private string[] accessoryNames = { "Steering Wheel", "Rear View Mirror", "Accelerator Pedal", "Exhaust", "Gear Stick", "Speedometer" };
    
    // Reset functionality - Store original values for proper reset
    private Vector3 originalModelPosition;
    private Quaternion originalModelRotation;
    private Vector3 originalModelScale;
    private float originalZoom = 1.0f;
    private float hintTimer;
    
    // Touch interaction tracking variables
    private Vector2 lastTouchPosition;
    private bool wasTouching = false;
    private float lastPinchDistance = 0f;
    private bool wasPinching = false;
    
    private float touchRotationSensitivity = 0.25f;
    
    // BALANCED TOUCH ZOOM SENSITIVITY - RESPONSIVE BUT CONTROLLED
    private float touchZoomSensitivity = 0.08f; // Reduced from 0.2f to 0.08f for better control
    private float touchMoveSpeed = 0.007f;
    
    // Flag to track initialization attempts
    private bool modelInitializationComplete = false;
    private int initializationAttempts = 0;
    private const int MAX_INITIALIZATION_ATTEMPTS = 3;
    
    void Awake()
    {
        if (sceneCamera == null)
        {
            sceneCamera = Camera.main;
            
            if (sceneCamera == null)
            {
                Debug.LogWarning("Main camera not found! Searching for any camera in scene.");
                
                Camera[] allCameras = FindObjectsOfType<Camera>();
                if (allCameras.Length > 0)
                {
                    sceneCamera = allCameras[0];
                    Debug.Log("Found camera: " + sceneCamera.name);
                }
                else
                {
                    Debug.LogError("No cameras found in scene! Creating a temporary camera.");
                    
                    GameObject tempCamObj = new GameObject("TempCamera");
                    sceneCamera = tempCamObj.AddComponent<Camera>();
                    
                    tempCamObj.transform.position = new Vector3(0, 1, -5);
                    tempCamObj.transform.rotation = Quaternion.Euler(0, 0, 0);
                }
            }
        }
        
        Debug.Log("Using camera: " + (sceneCamera != null ? sceneCamera.name : "NULL"));
        InitializeSpawnPoints();
    }
    
    private void InitializeSpawnPoints()
    {
        spawnPointMap = new Dictionary<string, Transform>();
        
        if (steeringWheelSpawnPoint != null)
        {
            spawnPointMap["Alfa Romeo 33 Stradale 1967 Classic Steering Wheel"] = steeringWheelSpawnPoint;
            spawnPointMap["Steering Wheel"] = steeringWheelSpawnPoint;
        }
        
        if (mirrorSpawnPoint != null)
        {
            spawnPointMap["Alfa Romeo 33 Stradale 1967 Classic Rear View Mirror"] = mirrorSpawnPoint;
            spawnPointMap["Rear View Mirror"] = mirrorSpawnPoint;
        }
        
        if (pedalSpawnPoint != null)
        {
            spawnPointMap["Alfa Romeo 33 Stradale 1967 Accelerator Pedal"] = pedalSpawnPoint;
            spawnPointMap["Accelerator Pedal"] = pedalSpawnPoint;
        }
        
        if (exhaustSpawnPoint != null)
        {
            spawnPointMap["Alfa Romeo 33 Stradale 1967 Signature Exhaust System"] = exhaustSpawnPoint;
            spawnPointMap["Exhaust"] = exhaustSpawnPoint;
            spawnPointMap["Exhaust System"] = exhaustSpawnPoint;
        }
        
        if (gearStickSpawnPoint != null)
        {
            spawnPointMap["Alfa Romeo Stradale 1967 Manual Gear Stick"] = gearStickSpawnPoint;
            spawnPointMap["Gear Stick"] = gearStickSpawnPoint;
        }
        
        if (speedometerSpawnPoint != null)
        {
            spawnPointMap["Alfa Romeo 33 Stradale Speedometer"] = speedometerSpawnPoint;
            spawnPointMap["Speedometer"] = speedometerSpawnPoint;
        }
        
        Debug.Log($"Initialized spawn point map with {spawnPointMap.Count} entries");
    }
    
    void OnEnable()
    {
        SetupButtons();
    }
    
    void SetupButtons()
    {
        if (resetViewButton != null)
        {
            resetViewButton.onClick.RemoveAllListeners();
            resetViewButton.onClick.AddListener(ResetView);
            Debug.Log("Reset view button listener added");
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(OnCloseButtonClicked);
            Debug.Log("Close button listener added");
        }
    }
    
    void Start()
    {
        EnsureCameraReference();
        
        Debug.Log("Using camera in Start: " + (sceneCamera != null ? sceneCamera.name : "NULL"));
        
        if (spawnPointMap == null || spawnPointMap.Count == 0)
        {
            InitializeSpawnPoints();
        }
        
        SetupButtons();
        
        int selectedIndex = PlayerPrefs.GetInt("SelectedAccessoryIndex", 0);
        LoadAccessoryModel(selectedIndex);
        
        ShowInteractionHints();
        hintTimer = hintFadeTime;
        
        Debug.Log($"Starting with accessory index: {selectedIndex}");
        
        StartCoroutine(EnsureModelVisibilitySequence());
    }
    
    void LoadAccessoryModel(int index)
    {
        if (index < 0 || index >= accessoryModels.Length)
        {
            Debug.LogError($"Invalid accessory index: {index}");
            return;
        }
        
        EnsureCameraReference();
        
        modelInitializationComplete = false;
        initializationAttempts = 0;
        
        if (accessoryModels[index].modelPrefab != null)
        {
            try
            {
                if (currentAccessoryModel != null)
                    Destroy(currentAccessoryModel);
                
                string modelName = accessoryModels[index].accessoryName;
                Transform spawnPoint = GetSpawnPointForModel(modelName);
                
                if (accessoryModelHolder == null)
                {
                    GameObject holderObj = new GameObject("AccessoryModelHolder");
                    accessoryModelHolder = holderObj.transform;
                    Debug.Log("Created new accessory model holder");
                }
                
                if (spawnPoint != null)
                {
                    accessoryModelHolder.position = spawnPoint.position;
                    accessoryModelHolder.rotation = spawnPoint.rotation;
                    Debug.Log($"Using spawn point {spawnPoint.name} at {spawnPoint.position}");
                }
                else
                {
                    PositionModelHolderInFrontOfCamera();
                    Debug.Log("No spawn point found, using camera-relative positioning");
                }
                
                currentAccessoryModel = Instantiate(accessoryModels[index].modelPrefab, accessoryModelHolder);
                
                currentAccessoryModel.transform.localPosition = Vector3.zero;
                currentAccessoryModel.transform.localRotation = Quaternion.identity;
                
                currentAccessoryModel.SetActive(true);
                
                ApplyModelSpecificScale(index);
                EnsureColliders(currentAccessoryModel);
                ForceRenderersVisible(currentAccessoryModel);
                
                // Store original values for proper reset functionality
                originalModelPosition = accessoryModelHolder.position;
                originalModelRotation = accessoryModelHolder.rotation;
                originalModelScale = currentAccessoryModel.transform.localScale;
                originalZoom = 1.0f;
                currentZoom = originalZoom;
                
                StartCoroutine(DelayedPositionAdjustment());
                
                Debug.Log($"Successfully loaded model for {modelName}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error instantiating model: {e.Message}");
                StartCoroutine(EmergencyModelInstantiation(index));
            }
        }
        else
        {
            Debug.LogError($"No model prefab assigned for index {index}");
        }
        
        UpdateAccessoryDetails(index);
    }
    
    private Transform GetSpawnPointForModel(string modelName)
    {
        if (string.IsNullOrEmpty(modelName) || spawnPointMap == null)
            return defaultSpawnPoint;
            
        if (spawnPointMap.TryGetValue(modelName, out Transform exactSpawn))
        {
            return exactSpawn;
        }
        
        foreach (KeyValuePair<string, Transform> entry in spawnPointMap)
        {
            if (modelName.Contains(entry.Key) || entry.Key.Contains(modelName))
            {
                Debug.Log($"Found spawn point by partial match: {entry.Key}");
                return entry.Value;
            }
        }
        
        if (modelName.Contains("Steering") && steeringWheelSpawnPoint != null)
            return steeringWheelSpawnPoint;
            
        if (modelName.Contains("Mirror") && mirrorSpawnPoint != null)
            return mirrorSpawnPoint;
            
        if (modelName.Contains("Pedal") && pedalSpawnPoint != null)
            return pedalSpawnPoint;
            
        if (modelName.Contains("Exhaust") && exhaustSpawnPoint != null)
            return exhaustSpawnPoint;
            
        if (modelName.Contains("Gear") && gearStickSpawnPoint != null)
            return gearStickSpawnPoint;
            
        if (modelName.Contains("Speed") && speedometerSpawnPoint != null)
            return speedometerSpawnPoint;
        
        return defaultSpawnPoint;
    }
    
    void ApplyModelSpecificScale(int index)
    {
        if (currentAccessoryModel == null)
            return;
            
        string modelName = accessoryModels[index].accessoryName.ToLower();
        Vector3 specificScale = Vector3.one * 0.75f;
        
        if (modelName.Contains("steering wheel"))
        {
            specificScale = new Vector3(0.55f, 0.55f, 0.55f);
        }
        else if (modelName.Contains("mirror"))
        {
            specificScale = new Vector3(0.65f, 0.65f, 0.65f);
        }
        else if (modelName.Contains("pedal"))
        {
            specificScale = new Vector3(0.7f, 0.7f, 0.7f);
        }
        else if (modelName.Contains("exhaust"))
        {
            specificScale = new Vector3(0.45f, 0.45f, 0.45f);
        }
        else if (modelName.Contains("gear"))
        {
            specificScale = new Vector3(0.6f, 0.6f, 0.6f);
        }
        else if (modelName.Contains("speedometer"))
        {
            specificScale = new Vector3(0.5f, 0.5f, 0.5f);
        }
        
        currentAccessoryModel.transform.localScale = specificScale;
        initialScale = specificScale;
        
        Debug.Log($"Applied specific scale {specificScale} for {modelName}");
    }
    
    IEnumerator EnsureModelVisibilitySequence()
    {
        yield return new WaitForSeconds(0.2f);
        
        if (currentAccessoryModel == null || !IsModelVisibleToCamera())
        {
            Debug.Log("Model not properly visible after initial loading, trying emergency positioning");
            
            ForceEmergencyPlacement();
            yield return new WaitForSeconds(0.1f);
            
            if (!IsModelVisibleToCamera())
            {
                ReinitializeModelCompletely();
                
                yield return new WaitForSeconds(0.1f);
                
                if (!IsModelVisibleToCamera())
                {
                    UltraEmergencyPlacement();
                }
            }
        }
        
        yield return new WaitForSeconds(0.1f);
        if (IsModelVisibleToCamera())
        {
            Debug.Log("Model is now confirmed visible");
        }
        else
        {
            Debug.LogError("All visibility attempts failed! Model may not be visible.");
        }
    }
    
    void UltraEmergencyPlacement()
    {
        Debug.Log("ULTRA EMERGENCY PLACEMENT - Last resort attempt");
        
        if (currentAccessoryModel == null || sceneCamera == null)
            return;
        
        try
        {
            currentAccessoryModel.transform.SetParent(null);
            
            Vector3 position = sceneCamera.transform.position + sceneCamera.transform.forward * 0.6f;
            position.y = sceneCamera.transform.position.y - 0.05f;
            currentAccessoryModel.transform.position = position;
            
            currentAccessoryModel.transform.localScale = Vector3.one * 0.15f;
            currentAccessoryModel.transform.forward = -sceneCamera.transform.forward;
            
            Renderer[] renderers = currentAccessoryModel.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                if (r != null)
                {
                    r.enabled = true;
                    r.material.renderQueue = 3000;
                    
                    if (r.material.HasProperty("_Mode"))
                    {
                        r.material.SetFloat("_Mode", 0);
                    }
                    
                    if (r.material.HasProperty("_ZWrite"))
                    {
                        r.material.SetInt("_ZWrite", 1);
                    }
                }
            }
            
            Debug.Log("Ultra emergency placement applied at " + position);
            
            if (accessoryModelHolder != null)
            {
                accessoryModelHolder.position = position;
                accessoryModelHolder.rotation = currentAccessoryModel.transform.rotation;
                
                currentAccessoryModel.transform.SetParent(accessoryModelHolder);
                currentAccessoryModel.transform.localPosition = Vector3.zero;
                currentAccessoryModel.transform.localRotation = Quaternion.identity;
            }
            
            // Update original values after emergency placement
            originalModelPosition = position;
            originalModelRotation = currentAccessoryModel.transform.rotation;
            originalModelScale = currentAccessoryModel.transform.localScale;
            originalZoom = 1.0f;
            currentZoom = originalZoom;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ultra emergency placement failed: {e.Message}");
        }
    }
    
    void ReinitializeModelCompletely()
    {
        Debug.Log("Reinitializing model completely");
        
        int selectedIndex = PlayerPrefs.GetInt("SelectedAccessoryIndex", 0);
        
        if (selectedIndex >= 0 && selectedIndex < accessoryModels.Length && accessoryModels[selectedIndex].modelPrefab != null)
        {
            try
            {
                if (currentAccessoryModel != null)
                {
                    Destroy(currentAccessoryModel);
                    currentAccessoryModel = null;
                }
                
                EnsureCameraReference();
                
                if (sceneCamera != null)
                {
                    Vector3 position = sceneCamera.transform.position + sceneCamera.transform.forward * 1.0f;
                    position.y = sceneCamera.transform.position.y - 0.1f;
                    
                    if (accessoryModelHolder == null)
                    {
                        GameObject holder = new GameObject("EmergencyModelHolder");
                        accessoryModelHolder = holder.transform;
                    }
                    
                    accessoryModelHolder.position = position;
                    accessoryModelHolder.rotation = Quaternion.LookRotation(-sceneCamera.transform.forward);
                    
                    currentAccessoryModel = Instantiate(
                        accessoryModels[selectedIndex].modelPrefab,
                        accessoryModelHolder
                    );
                    
                    currentAccessoryModel.transform.localPosition = Vector3.zero;
                    currentAccessoryModel.transform.localRotation = Quaternion.identity;
                    currentAccessoryModel.transform.localScale = Vector3.one * 0.4f;
                    
                    ForceRenderersVisible(currentAccessoryModel);
                    EnsureColliders(currentAccessoryModel);
                    
                    // Update original values
                    originalModelPosition = accessoryModelHolder.position;
                    originalModelRotation = accessoryModelHolder.rotation;
                    originalModelScale = currentAccessoryModel.transform.localScale;
                    originalZoom = 1.0f;
                    currentZoom = originalZoom;
                    
                    Debug.Log("Model recreated successfully at " + position);
                }
                else
                {
                    Debug.LogError("Cannot reinitialize model: no camera available");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Model reinitialization failed: {e.Message}");
            }
        }
    }
    
    void EnsureCameraReference()
    {
        if (sceneCamera == null)
        {
            Debug.LogWarning("Camera reference lost! Finding camera again...");
            
            sceneCamera = Camera.main;
            if (sceneCamera != null)
            {
                Debug.Log("Found main camera via Camera.main");
                return;
            }
            
            GameObject mainCamObj = GameObject.FindGameObjectWithTag("MainCamera");
            if (mainCamObj != null)
            {
                sceneCamera = mainCamObj.GetComponent<Camera>();
                if (sceneCamera != null)
                {
                    Debug.Log("Found camera via MainCamera tag");
                    return;
                }
            }
            
            Camera[] allCameras = FindObjectsOfType<Camera>();
            if (allCameras.Length > 0)
            {
                foreach (Camera cam in allCameras)
                {
                    if (cam.enabled)
                    {
                        sceneCamera = cam;
                        Debug.Log("Found enabled camera: " + sceneCamera.name);
                        return;
                    }
                }
                
                sceneCamera = allCameras[0];
                Debug.Log("Using first available camera: " + sceneCamera.name);
            }
            else
            {
                GameObject tempCamObj = new GameObject("EmergencyCamera");
                sceneCamera = tempCamObj.AddComponent<Camera>();
                tempCamObj.transform.position = new Vector3(0, 1, -5);
                Debug.LogError("Created emergency camera as last resort!");
            }
        }
    }
    
    IEnumerator EmergencyModelInstantiation(int index)
    {
        Debug.Log("Attempting emergency model instantiation");
        
        yield return new WaitForSeconds(0.1f);
        
        if (index < 0 || index >= accessoryModels.Length || accessoryModels[index].modelPrefab == null)
            yield break;
            
        try
        {
            if (sceneCamera != null)
            {
                Vector3 position = sceneCamera.transform.position + sceneCamera.transform.forward * 1.0f;
                position.y = sceneCamera.transform.position.y - 0.1f;
                
                currentAccessoryModel = Instantiate(
                    accessoryModels[index].modelPrefab,
                    position,
                    Quaternion.LookRotation(-sceneCamera.transform.forward)
                );
                
                currentAccessoryModel.transform.localScale = Vector3.one * 0.5f;
                initialScale = currentAccessoryModel.transform.localScale;
                
                ForceRenderersVisible(currentAccessoryModel);
                EnsureColliders(currentAccessoryModel);
                
                // Update original values
                originalModelPosition = currentAccessoryModel.transform.position;
                originalModelRotation = currentAccessoryModel.transform.rotation;
                originalModelScale = currentAccessoryModel.transform.localScale;
                originalZoom = 1.0f;
                currentZoom = originalZoom;
                
                Debug.Log("Emergency model instantiation complete at " + position);
            }
            else
            {
                Debug.LogError("Cannot complete emergency instantiation: no camera available");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Emergency instantiation failed: {e.Message}");
        }
    }
    
    void EnsureColliders(GameObject model)
    {
        if (model == null) return;
        
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        
        bool hasColliders = false;
        
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;
            
            Collider collider = renderer.GetComponent<Collider>();
            if (collider != null)
            {
                hasColliders = true;
                break;
            }
        }
        
        if (!hasColliders)
        {
            foreach (Renderer renderer in renderers)
            {
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer))
                    continue;
                
                if (renderer.GetComponent<Collider>() == null)
                {
                    BoxCollider boxCollider = renderer.gameObject.AddComponent<BoxCollider>();
                    
                    if (renderer is MeshRenderer meshRenderer)
                    {
                        MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                        if (meshFilter != null && meshFilter.sharedMesh != null)
                        {
                            boxCollider.center = meshFilter.sharedMesh.bounds.center;
                            boxCollider.size = meshFilter.sharedMesh.bounds.size;
                        }
                        else
                        {
                            boxCollider.size = renderer.bounds.size;
                            boxCollider.center = renderer.bounds.center - renderer.transform.position;
                        }
                    }
                    else
                    {
                        boxCollider.size = renderer.bounds.size;
                        boxCollider.center = renderer.bounds.center - renderer.transform.position;
                    }
                }
            }
            
            Debug.Log($"Added colliders to {renderers.Length} renderers for interaction");
        }
    }
    
    void PositionModelHolderInFrontOfCamera()
    {
        EnsureCameraReference();
        
        if (accessoryModelHolder == null)
        {
            Debug.LogError("Cannot position model: accessoryModelHolder is null");
            return;
        }
        
        if (sceneCamera != null)
        {
            float fov = sceneCamera.fieldOfView;
            float optimalDistance = Mathf.Clamp(5.0f / fov, 1.5f, 3.0f);
            
            Vector3 position = sceneCamera.transform.position + sceneCamera.transform.forward * optimalDistance;
            position.y = sceneCamera.transform.position.y - 0.15f;
            
            accessoryModelHolder.position = position;
            accessoryModelHolder.rotation = Quaternion.LookRotation(-sceneCamera.transform.forward);
            
            Debug.Log($"Model positioned in front of camera at distance: {optimalDistance}");
        }
        else
        {
            accessoryModelHolder.position = new Vector3(0, 0, 5);
            accessoryModelHolder.rotation = Quaternion.Euler(0, 180f, 0);
            Debug.LogWarning("Using default position due to missing camera");
        }
        
        // Store original values
        originalModelPosition = accessoryModelHolder.position;
        originalModelRotation = accessoryModelHolder.rotation;
    }
    
    void ForceRenderersVisible(GameObject model)
    {
        if (model == null) return;
        
        try
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.gameObject.SetActive(true);
                    renderer.enabled = true;
                    
                    Material[] materials = renderer.materials;
                    foreach (Material material in materials)
                    {
                        if (material != null)
                        {
                            if (material.HasProperty("_Color"))
                            {
                                Color color = material.color;
                                color.a = 1.0f;
                                material.color = color;
                            }
                            
                            if (material.HasProperty("_Mode"))
                            {
                                material.SetFloat("_Mode", 0);
                            }
                            
                            material.SetInt("_ZWrite", 1);
                            material.renderQueue = 2000;
                            
                            if (material.HasProperty("_Cull"))
                            {
                                material.SetInt("_Cull", 2);
                            }
                        }
                    }
                }
            }
            
            Debug.Log($"Forced {renderers.Length} renderers to be visible");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error forcing renderers visible: {e.Message}");
        }
    }
    
    IEnumerator DelayedPositionAdjustment()
    {
        yield return new WaitForSeconds(0.05f);
        
        if (currentAccessoryModel != null)
        {
            ForceRenderersVisible(currentAccessoryModel);
            
            if (!IsModelVisibleToCamera())
            {
                Debug.LogWarning("Model not visible after initial positioning, trying center");
                CenterModel();
                
                yield return new WaitForSeconds(0.1f);
                
                if (!IsModelVisibleToCamera())
                {
                    Debug.LogError("Model still not visible! Using guaranteed placement.");
                    GuaranteedModelPlacement();
                    
                    yield return new WaitForSeconds(0.1f);
                    
                    if (!IsModelVisibleToCamera())
                    {
                        ForceEmergencyPlacement();
                    }
                }
            }
        }
        
        yield return new WaitForSeconds(0.1f);
        Debug.Log("Model visibility after adjustments: " + IsModelVisibleToCamera());
    }
    
    void ForceEmergencyPlacement()
    {
        Debug.Log("EMERGENCY PLACEMENT INITIATED");
        
        EnsureCameraReference();
        
        if (currentAccessoryModel == null)
        {
            Debug.LogError("No model to position!");
            return;
        }
        
        if (sceneCamera == null)
        {
            Debug.LogError("No camera for emergency placement!");
            return;
        }
        
        try
        {
            Transform originalParent = currentAccessoryModel.transform.parent;
            
            currentAccessoryModel.transform.SetParent(null);
            
            Vector3 position = sceneCamera.transform.position + sceneCamera.transform.forward * 0.8f;
            position.y = sceneCamera.transform.position.y - 0.1f;
            currentAccessoryModel.transform.position = position;
            
            currentAccessoryModel.transform.forward = -sceneCamera.transform.forward;
            currentAccessoryModel.transform.localScale = Vector3.one * 0.3f;
            
            ForceRenderersVisible(currentAccessoryModel);
            
            if (originalParent != null && accessoryModelHolder != null)
            {
                accessoryModelHolder.position = position;
                accessoryModelHolder.rotation = currentAccessoryModel.transform.rotation;
                
                currentAccessoryModel.transform.SetParent(accessoryModelHolder);
                currentAccessoryModel.transform.localPosition = Vector3.zero;
                currentAccessoryModel.transform.localRotation = Quaternion.identity;
            }
            
            Debug.Log("Emergency placement complete at " + position);
            
            // Update original values
            originalModelPosition = position;
            originalModelRotation = currentAccessoryModel.transform.rotation;
            originalModelScale = currentAccessoryModel.transform.localScale;
            originalZoom = 1.0f;
            currentZoom = originalZoom;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Emergency placement failed: {e.Message}");
        }
    }
    
    void GuaranteedModelPlacement()
    {
        Debug.Log("GUARANTEED PLACEMENT INITIATED");
        
        if (sceneCamera == null)
        {
            EnsureCameraReference();
            if (sceneCamera == null)
            {
                Debug.LogError("No camera available for guaranteed placement");
                return;
            }
        }
            
        try
        {
            Vector3 absolutePosition = sceneCamera.transform.position + sceneCamera.transform.forward * 0.9f;
            absolutePosition.y = sceneCamera.transform.position.y - 0.1f;
            
            if (accessoryModelHolder != null)
            {
                accessoryModelHolder.position = absolutePosition;
                accessoryModelHolder.forward = -sceneCamera.transform.forward;
            }
            
            if (currentAccessoryModel != null)
            {
                if (accessoryModelHolder == null || currentAccessoryModel.transform.parent != accessoryModelHolder)
                {
                    currentAccessoryModel.transform.position = absolutePosition;
                    currentAccessoryModel.transform.forward = -sceneCamera.transform.forward;
                }
                
                currentAccessoryModel.transform.localScale = Vector3.one * 0.35f;
                ForceRenderersVisible(currentAccessoryModel);
            }
            
            // Update original values
            originalModelPosition = absolutePosition;
            originalModelRotation = accessoryModelHolder != null ? 
                accessoryModelHolder.rotation : 
                Quaternion.LookRotation(-sceneCamera.transform.forward);
            
            originalModelScale = currentAccessoryModel != null ? 
                currentAccessoryModel.transform.localScale : 
                Vector3.one * 0.35f;
            
            originalZoom = 1.0f;
            currentZoom = originalZoom;
            
            Debug.Log("GUARANTEED PLACEMENT applied at " + absolutePosition);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Guaranteed placement failed: {e.Message}");
        }
    }
    
    // Simple center model function (without button creation)
    public void CenterModel()
    {
        EnsureCameraReference();
        
        if (accessoryModelHolder == null)
        {
            Debug.LogError("Cannot center model: accessoryModelHolder is null");
            return;
        }
        
        try
        {
            if (sceneCamera != null)
            {
                Vector3 position = sceneCamera.transform.position + sceneCamera.transform.forward * 1.0f;
                position.y = sceneCamera.transform.position.y - 0.1f;
                
                accessoryModelHolder.position = position;
                accessoryModelHolder.forward = -sceneCamera.transform.forward;
                
                if (currentAccessoryModel != null)
                {
                    currentAccessoryModel.transform.localScale = Vector3.one * 0.5f;
                    currentZoom = 1.0f;
                    ForceRenderersVisible(currentAccessoryModel);
                }
                
                Debug.Log("Model centered directly in front of camera at: " + position);
                
                // Update original values
                originalModelPosition = accessoryModelHolder.position;
                originalModelRotation = accessoryModelHolder.rotation;
                originalModelScale = currentAccessoryModel != null ? currentAccessoryModel.transform.localScale : Vector3.one * 0.5f;
                originalZoom = 1.0f;
            }
            else
            {
                Debug.LogError("Cannot center model: no camera available");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error centering model: {e.Message}");
        }
    }
    
    void UpdateAccessoryDetails(int index)
    {
        if (index < 0 || index >= accessoryModels.Length)
            return;
        
        AccessoryModel model = accessoryModels[index];
        
        if (nameText != null)
            nameText.text = model.accessoryName;
        
        if (manufacturerText != null)
            manufacturerText.text = model.manufacturer;
        
        if (priceText != null)
            priceText.text = model.price.ToString("C0");
        
        if (descriptionText != null)
            descriptionText.text = model.description;
        
        if (featuresText != null && model.features != null && model.features.Length > 0)
        {
            string featuresString = "";
            foreach (string feature in model.features)
            {
                featuresString += "• " + feature + "\n";
            }
            featuresText.text = featuresString;
        }
        
        if (accessoryImageDisplay != null && model.accessoryImage != null)
        {
            accessoryImageDisplay.sprite = model.accessoryImage;
            accessoryImageDisplay.enabled = true;
            Debug.Log($"Loaded image for {model.accessoryName}");
        }
        else
        {
            Debug.LogWarning($"Image not found for {model.accessoryName}");
            if (accessoryImageDisplay != null)
                accessoryImageDisplay.enabled = false;
        }
    }
    
    bool IsModelVisibleToCamera()
    {
        if (currentAccessoryModel == null || sceneCamera == null)
            return false;
        
        try
        {
            Renderer[] renderers = currentAccessoryModel.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return false;
            
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(sceneCamera);
            
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null && GeometryUtility.TestPlanesAABB(planes, renderer.bounds))
                    return true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Visibility check error: " + e.Message);
        }
        
        return false;
    }
    
    void Update()
    {
        if (currentAccessoryModel == null)
            return;
        
        if (sceneCamera == null)
        {
            EnsureCameraReference();
            if (sceneCamera == null) return;
        }
        
        if (!modelInitializationComplete && initializationAttempts < MAX_INITIALIZATION_ATTEMPTS)
        {
            if (!IsModelVisibleToCamera())
            {
                initializationAttempts++;
                Debug.Log($"Model not visible, attempt {initializationAttempts} of {MAX_INITIALIZATION_ATTEMPTS}");
                
                if (initializationAttempts == 1)
                {
                    CenterModel();
                }
                else if (initializationAttempts == 2)
                {
                    GuaranteedModelPlacement();
                }
                else
                {
                    ForceEmergencyPlacement();
                    modelInitializationComplete = true;
                }
            }
            else
            {
                modelInitializationComplete = true;
                Debug.Log("Model visibility confirmed");
            }
        }
        
        if (Application.platform != RuntimePlatform.Android && Application.platform != RuntimePlatform.IPhonePlayer)
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                movementMode = !movementMode;
                Debug.Log("Movement mode " + (movementMode ? "enabled" : "disabled"));
            }
            
            if (Input.GetKeyDown(KeyCode.C))
            {
                CenterModel();
            }
            
            if (Input.GetKeyDown(KeyCode.G))
            {
                GuaranteedModelPlacement();
            }
            
            if (Input.GetKeyDown(KeyCode.E))
            {
                ForceEmergencyPlacement();
            }
            
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetView();
            }
            
            // CONTROLLED ZOOM KEYBOARD SHORTCUTS FOR TESTING
            if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                ApplyZoom(1.0f); // Controlled zoom in
            }
            
            if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                ApplyZoom(-1.0f); // Controlled zoom out
            }
            
            if (movementMode)
            {
                HandleDesktopMovement();
            }
            else
            {
                HandleDesktopRotation();
            }
            
            HandleDesktopZoom();
            HandleKeyboardMovement();
        }
        else
        {
            HandleTouchInteraction();
        }
        
        if (hintTimer > 0)
        {
            hintTimer -= Time.deltaTime;
            if (hintTimer <= 0)
            {
                HideInteractionHints();
            }
        }
        
        if (Input.GetMouseButtonDown(0) || Input.GetAxis("Mouse ScrollWheel") != 0 || Input.touchCount > 0)
        {
            HideInteractionHints();
        }
    }
    
    void HandleKeyboardMovement()
    {
        float horizontal = 0;
        float vertical = 0;
        
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            vertical += 1;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            vertical -= 1;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            horizontal -= 1;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            horizontal += 1;
        
        if (horizontal != 0 || vertical != 0)
        {
            Vector3 right = sceneCamera.transform.right * horizontal * moveSpeed;
            Vector3 up = sceneCamera.transform.up * vertical * moveSpeed;
            
            accessoryModelHolder.position += right + up;
        }
    }
    
    void HandleDesktopMovement()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            previousMousePosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
        
        if (isDragging)
        {
            Vector3 delta = Input.mousePosition - previousMousePosition;
            
            Vector3 right = sceneCamera.transform.right * delta.x * moveSpeed;
            Vector3 up = sceneCamera.transform.up * delta.y * moveSpeed;
            
            accessoryModelHolder.position += right + up;
            
            previousMousePosition = Input.mousePosition;
        }
    }
    
    void HandleDesktopRotation()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            previousMousePosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
        
        if (isDragging)
        {
            Vector3 delta = Input.mousePosition - previousMousePosition;
            
            float screenFactor = Screen.height / 1080f;
            float adjustedSpeed = rotationSpeed * screenFactor;
            
            accessoryModelHolder.Rotate(Vector3.up, -delta.x * adjustedSpeed * Time.deltaTime, Space.World);
            accessoryModelHolder.Rotate(Vector3.right, delta.y * adjustedSpeed * Time.deltaTime, Space.World);
            
            previousMousePosition = Input.mousePosition;
        }
    }
    
    void HandleDesktopZoom()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0)
        {
            // CONTROLLED DESKTOP ZOOM WITH SMOOTH RESPONSIVENESS
            float zoomFactor = scrollInput * zoomSpeed * 1.2f; // Balanced multiplier for smooth control
            ApplyZoom(zoomFactor);
        }
    }
    
    // OPTIMIZED ZOOM FUNCTION - WIDER RANGE BUT SMOOTH AND CONTROLLED
    void ApplyZoom(float increment)
    {
        if (currentAccessoryModel == null)
            return;
        
        // Smooth zoom calculation with controlled responsiveness
        float zoomMultiplier = 0.8f; // Reduced from 2.0f to 0.8f for smoother control
        float newZoom = Mathf.Clamp(currentZoom + (increment * zoomMultiplier), minZoom, maxZoom);
        
        if (Mathf.Abs(newZoom - currentZoom) > 0.001f)
        {
            currentZoom = newZoom;
            
            // Apply zoom with smooth logarithmic scaling curve for natural feel
            float scaleCurve = Mathf.Pow(currentZoom, 0.85f); // Balanced curve (was 0.7f) for smooth transitions
            Vector3 targetScale = originalModelScale * scaleCurve;
            
            // Smooth scale interpolation for fluid zoom
            currentAccessoryModel.transform.localScale = Vector3.Lerp(
                currentAccessoryModel.transform.localScale,
                targetScale,
                Time.deltaTime * 8.0f // Smooth interpolation
            );
            
            // Smooth position adjustment for optimal viewing
            if (sceneCamera != null && accessoryModelHolder != null)
            {
                float distanceAdjustment = (1.0f - scaleCurve) * 0.25f; // Moderate adjustment factor
                Vector3 adjustedPosition = originalModelPosition + 
                    sceneCamera.transform.forward * distanceAdjustment;
                
                // Very smooth position interpolation
                accessoryModelHolder.position = Vector3.Lerp(
                    accessoryModelHolder.position, 
                    adjustedPosition, 
                    Time.deltaTime * 4.0f // Smooth position adjustment
                );
            }
            
            // Debug output with better formatting for zoom level tracking
            if (Time.frameCount % 10 == 0) // Only log every 10 frames to avoid spam
            {
                Debug.Log($"Zoom: {currentZoom:F2} | Scale: {scaleCurve:F2} | Range: {minZoom:F3}-{maxZoom:F0}");
            }
        }
    }
    
    void HandleTouchInteraction()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began)
            {
                isDragging = true;
                lastTouchPosition = touch.position;
                wasTouching = true;
                wasPinching = false;
                
                if (touch.tapCount >= 2)
                {
                    movementMode = !movementMode;
                    Debug.Log("Movement mode " + (movementMode ? "enabled" : "disabled"));
                }
            }
            else if ((touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary) && isDragging)
            {
                if (wasTouching)
                {
                    Vector2 touchDelta = touch.position - lastTouchPosition;
                    
                    if (movementMode)
                    {
                        Vector3 right = sceneCamera.transform.right * touchDelta.x * touchMoveSpeed;
                        Vector3 up = sceneCamera.transform.up * -touchDelta.y * touchMoveSpeed;
                        accessoryModelHolder.position += right + up;
                    }
                    else
                    {
                        float xSensitivity = touchRotationSensitivity * (1.0f + Mathf.Abs(touchDelta.x) * 0.01f);
                        float ySensitivity = touchRotationSensitivity * (1.0f + Mathf.Abs(touchDelta.y) * 0.01f);
                        
                        accessoryModelHolder.Rotate(Vector3.up, -touchDelta.x * xSensitivity, Space.World);
                        accessoryModelHolder.Rotate(Vector3.right, touchDelta.y * ySensitivity, Space.World);
                    }
                }
                
                lastTouchPosition = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isDragging = false;
                wasTouching = false;
            }
        }
        else if (Input.touchCount == 2)
        {
            isDragging = false;
            wasTouching = false;
            
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);
            
            float currentPinchDistance = Vector2.Distance(touch0.position, touch1.position);
            
            if (!wasPinching)
            {
                lastPinchDistance = currentPinchDistance;
                wasPinching = true;
            }
            else
            {
                float pinchDelta = currentPinchDistance - lastPinchDistance;
                
                float screenDiagonal = Mathf.Sqrt(Screen.width * Screen.width + Screen.height * Screen.height);
                float normalizedPinchDelta = pinchDelta / screenDiagonal;
                
                // BALANCED TOUCH ZOOM - RESPONSIVE BUT CONTROLLED
                float zoomAmount = normalizedPinchDelta * 12.0f; // Reduced from 50.0f to 12.0f for better control
                ApplyZoom(-zoomAmount);
                
                lastPinchDistance = currentPinchDistance;
                
                // Reduced debug frequency for touch zoom
                if (Time.frameCount % 30 == 0) // Only log every 30 frames
                {
                    Debug.Log($"Touch zoom delta: {normalizedPinchDelta:F4}, zoom amount: {zoomAmount:F2}");
                }
            }
            
            if (touch0.phase == TouchPhase.Ended || touch0.phase == TouchPhase.Canceled ||
                touch1.phase == TouchPhase.Ended || touch1.phase == TouchPhase.Canceled)
            {
                wasPinching = false;
            }
        }
        else if (Input.touchCount == 3)
        {
            try
            {
                Touch touch = Input.GetTouch(0);
                Vector2 touchDelta = touch.deltaPosition;
                
                Vector3 right = sceneCamera.transform.right * -touchDelta.x * touchMoveSpeed * 1.5f;
                Vector3 up = sceneCamera.transform.up * -touchDelta.y * touchMoveSpeed * 1.5f;
                accessoryModelHolder.position += right + up;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Three-finger touch error: " + e.Message);
            }
        }
        else if (Input.touchCount == 4 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            ForceEmergencyPlacement();
        }
        else
        {
            isDragging = false;
            wasTouching = false;
            wasPinching = false;
        }
    }
    
    // COMPLETELY FIXED RESET VIEW FUNCTION
    public void ResetView()
    {
        Debug.Log("Reset View button clicked - Resetting to original state");
        
        EnsureCameraReference();
        
        if (accessoryModelHolder == null || currentAccessoryModel == null)
        {
            Debug.LogError("Reset failed: Missing model components");
            return;
        }
        
        try
        {
            // Reset position and rotation to original values
            accessoryModelHolder.position = originalModelPosition;
            accessoryModelHolder.rotation = originalModelRotation;
            
            // Reset scale to original value
            currentAccessoryModel.transform.localScale = originalModelScale;
            
            // Reset zoom to original value
            currentZoom = originalZoom;
            
            // Ensure the model is visible after reset
            ForceRenderersVisible(currentAccessoryModel);
            
            // Reset movement mode to rotation
            movementMode = false;
            
            Debug.Log($"View reset complete - Position: {originalModelPosition}, Rotation: {originalModelRotation}, Scale: {originalModelScale}, Zoom: {originalZoom}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Reset view failed: {e.Message}");
            
            // Fallback to emergency placement if reset fails
            ForceEmergencyPlacement();
        }
    }
    
    void ShowInteractionHints()
    {
        if (rotationHint != null)
            rotationHint.SetActive(true);
        if (zoomHint != null)
            zoomHint.SetActive(true);
        if (moveHint != null)
            moveHint.SetActive(true);
    }
    
    void HideInteractionHints()
    {
        if (rotationHint != null)
            rotationHint.SetActive(false);
        if (zoomHint != null)
            zoomHint.SetActive(false);
        if (moveHint != null)
            moveHint.SetActive(false);
    }
    
    void OnCloseButtonClicked()
    {
        SceneManager.LoadScene("AccessoryFlashcards");
    }
    
    void OnDrawGizmos()
    {
        if (accessoryModelHolder != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(accessoryModelHolder.position, 0.5f);
            
            Camera gizmoCam = sceneCamera;
            if (gizmoCam == null)
                gizmoCam = Camera.current;
                
            if (gizmoCam != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(gizmoCam.transform.position, accessoryModelHolder.position);
            }
        }
        
        if (spawnArea != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(spawnArea.position, Vector3.one);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (currentAccessoryModel == null) return;
        
        Renderer[] renderers = currentAccessoryModel.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        
        Bounds combinedBounds = new Bounds();
        bool boundsInitialized = false;
        
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;
            
            if (!boundsInitialized)
            {
                combinedBounds = renderer.bounds;
                boundsInitialized = true;
            }
            else
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }
        }
        
        if (boundsInitialized)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(combinedBounds.center, combinedBounds.size);
            
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(combinedBounds.center, 0.05f);
        }
    }
}