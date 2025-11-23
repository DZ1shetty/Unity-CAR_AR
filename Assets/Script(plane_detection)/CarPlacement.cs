using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;

public class CarPlacement : MonoBehaviour
{
    [Header("AR Components")]
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;
    public ARSessionOrigin arSessionOrigin;
    public ARAnchorManager anchorManager;
    public Camera arCamera;
    public ARInteractionManager interactionManager;

    [Header("Car Prefab")]
    public GameObject carPrefab;

    [Header("Placement Settings")]
    public bool allowMultipleCars = false;
    public bool requirePlaneDetection = true;
    public float minPlaneArea = 0.1f;

    [Header("Scaling Settings")]
    [Range(0.1f, 1.0f)]
    public float carScaleFactor = 0.3f;
    public Vector3 minCarScale = new Vector3(0.1f, 0.1f, 0.1f);
    public Vector3 maxCarScale = new Vector3(2.0f, 2.0f, 2.0f);

    [Header("Exact Placement Control")]
    public bool useExactTouchPosition = true;
    public bool disablePositionAdjustments = true;
    public bool disableRotationAlignment = false;

    [Header("Stability Settings")] // NEW: Added stability settings
    public bool useAnchoring = true;
    public bool freezePositionAfterPlacement = true;
    public float placementConfirmationDelay = 0.1f;
    public int maxPlacementRetries = 3;
    public float placementRetryDelay = 0.05f;

    [Header("Input Settings")]
    public float touchSensitivity = 10f;
    public float doubleTapTime = 0.3f;
    public float touchHoldTime = 0.5f;  // NEW: Added touch hold time

    [Header("Performance Settings")]
    public int maxCarsAllowed = 10;
    public bool enableObjectPooling = true;

    [Header("Debug")]
    public bool showDebugInfo = true;
    public bool showPlacementMarkers = true;

    [Header("Car Rotation")]
    public float rotationSpeed = 120f; // degrees per second for swipe rotation

    // Private variables
    private List<CarPlacementData> placedCars = new List<CarPlacementData>(); // CHANGED to store additional data
    private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();
    private Vector3 originalCarScale = Vector3.one;
    private bool planesDetected = false;
    private int detectedPlaneCount = 0;

    private float lastTouchTime = 0f;
    private Vector2 lastTouchPosition;
    private bool isProcessingTouch = false;

    private Queue<GameObject> carPool = new Queue<GameObject>();
    private Transform poolParent;

    private List<GameObject> debugMarkers = new List<GameObject>();
    private bool isInInteractionMode = false;

    // Swipe/drag state
    private bool isDraggingCar = false;
    private CarPlacementData selectedCarData = null; // CHANGED to use CarPlacementData
    private Vector2 dragStartPos;
    private float lastDragAngle = 0f;
    
    // Touch tracking for better hit detection
    private Vector2 currentTouchPosition; 
    private bool touchRegistered = false;
    private float touchStartTime = 0f;
    private bool isTouchHeld = false;

    // NEW: Structure to store car data with anchor
    private class CarPlacementData
    {
        public GameObject carObject;
        public ARAnchor anchor;
        public ARPlane plane;
        public Vector3 originalPosition;
        public Quaternion originalRotation;
        public bool isLocked;
        
        public CarPlacementData(GameObject car, ARPlane p)
        {
            carObject = car;
            plane = p;
            originalPosition = car.transform.position;
            originalRotation = car.transform.rotation;
            isLocked = false;
            anchor = null; // Will be set later if anchoring is used
        }
    }

    void Start()
    {
        InitializeComponents();
        SetupObjectPooling();
        ValidateSetup();

        if (showDebugInfo)
        {
            Debug.Log($"CarPlacement: Initialized at {System.DateTime.Now}");
            Debug.Log($"CarPlacement: Exact placement mode: {useExactTouchPosition}");
        }
    }

    void InitializeComponents()
    {
        if (raycastManager == null)
            raycastManager = FindObjectOfType<ARRaycastManager>();
        if (planeManager == null)
            planeManager = FindObjectOfType<ARPlaneManager>();
        if (arCamera == null)
        {
            var xrOrigin = FindObjectOfType<XROrigin>();
            arCamera = xrOrigin != null && xrOrigin.Camera != null ? xrOrigin.Camera : Camera.main;
            if (arCamera == null)
                arCamera = FindObjectOfType<Camera>();
        }
        if (interactionManager == null)
            interactionManager = FindObjectOfType<ARInteractionManager>();
            
        if (arSessionOrigin == null)
            arSessionOrigin = FindObjectOfType<ARSessionOrigin>();
            
        if (anchorManager == null)
            anchorManager = FindObjectOfType<ARAnchorManager>();

        if (carPrefab != null)
            originalCarScale = carPrefab.transform.localScale;

        if (planeManager != null)
            planeManager.planesChanged += OnPlanesChanged;
    }

    void SetupObjectPooling()
    {
        if (!enableObjectPooling || carPrefab == null) return;

        GameObject poolContainer = new GameObject("CarPool");
        poolParent = poolContainer.transform;
        poolParent.SetParent(transform);
        int poolSize = Mathf.Max(3, maxCarsAllowed);
        for (int i = 0; i < poolSize; i++)
        {
            GameObject pooledCar = Instantiate(carPrefab, poolParent);
            pooledCar.SetActive(false);
            carPool.Enqueue(pooledCar);
        }
    }

    void ValidateSetup()
    {
        List<string> missing = new List<string>();
        if (carPrefab == null) missing.Add("Car Prefab");
        if (raycastManager == null) missing.Add("AR Raycast Manager");
        if (planeManager == null) missing.Add("AR Plane Manager");
        if (arCamera == null) missing.Add("AR Camera");
        if (missing.Count > 0)
            Debug.LogError($"CarPlacement: Missing components: {string.Join(", ", missing)}");
        if (carPrefab != null && carPrefab.GetComponent<Renderer>() == null)
            Debug.LogWarning("CarPlacement: Car prefab doesn't have a Renderer component");
            
        if (useAnchoring && anchorManager == null)
        {
            Debug.LogWarning("CarPlacement: Anchoring enabled but no ARAnchorManager found. Anchoring will be disabled.");
            useAnchoring = false;
        }
        
        // Add collider to car prefab if it doesn't have one (needed for touch detection)
        if (carPrefab != null && carPrefab.GetComponent<Collider>() == null)
        {
            // Try to add a collider based on the mesh bounds
            Renderer renderer = carPrefab.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                BoxCollider collider = carPrefab.AddComponent<BoxCollider>();
                collider.center = renderer.bounds.center - carPrefab.transform.position;
                collider.size = renderer.bounds.size;
                Debug.Log("CarPlacement: Added missing collider to car prefab");
            }
            else
            {
                // Fallback to a simple box collider
                BoxCollider collider = carPrefab.AddComponent<BoxCollider>();
                Debug.LogWarning("CarPlacement: Added default box collider to car prefab, but no renderer found to size it correctly");
            }
        }
    }

    void OnPlanesChanged(ARPlanesChangedEventArgs eventArgs)
    {
        detectedPlaneCount += eventArgs.added.Count;
        detectedPlaneCount -= eventArgs.removed.Count;
        planesDetected = detectedPlaneCount > 0;
        if (showDebugInfo)
            Debug.Log($"CarPlacement: Planes detected: {detectedPlaneCount}");
    }

    void Update()
    {
        if (isInInteractionMode) return;

        if (!requirePlaneDetection || planesDetected)
            HandleInput();
        else if (showDebugInfo && Time.frameCount % 120 == 0)
            Debug.Log("CarPlacement: Scanning for planes... Move your device.");

        CleanupNullReferences();
    }

    void HandleInput()
    {
        if (isProcessingTouch) return;

        bool isTouch = false;
        Vector2 touchPos = Vector2.zero;
        bool isTouchEnded = false;

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            isTouch = true;
            touchPos = Input.mousePosition;
            touchRegistered = true;
            currentTouchPosition = touchPos;
            touchStartTime = Time.time;
            isTouchHeld = false;
        }
        else if (Input.GetMouseButton(0) && touchRegistered)
        {
            touchPos = Input.mousePosition;
            currentTouchPosition = touchPos;
            if (Time.time - touchStartTime >= touchHoldTime)
            {
                isTouchHeld = true;
            }
        }
        else if (Input.GetMouseButtonUp(0) && touchRegistered)
        {
            touchPos = Input.mousePosition;
            isTouchEnded = true;
            touchRegistered = false;
        }
#else
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            touchPos = touch.position;
            
            if (touch.phase == TouchPhase.Began)
            {
                isTouch = true;
                touchRegistered = true;
                currentTouchPosition = touchPos;
                touchStartTime = Time.time;
                isTouchHeld = false;
            }
            else if ((touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary) && touchRegistered)
            {
                currentTouchPosition = touchPos;
                if (Time.time - touchStartTime >= touchHoldTime)
                {
                    isTouchHeld = true;
                }
            }
            else if ((touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) && touchRegistered)
            {
                isTouchEnded = true;
                touchRegistered = false;
            }
        }
#endif

        // Handle car selection and dragging
        if (touchRegistered && !isDraggingCar && placedCars.Count > 0)
        {
            // If touch is held and we're not already dragging, try to select a car
            if (isTouchHeld && !isDraggingCar) 
            {
                TrySelectCar(currentTouchPosition);
            }
        }

        // Handle car dragging/rotation if a car is selected
        if (isDraggingCar && selectedCarData != null)
        {
            ContinueDrag(currentTouchPosition);
            if (isTouchEnded)
            {
                EndDrag();
            }
        }
        
        // If not dragging a car and a touch begins, try to place a car
        if (isTouch && !isDraggingCar)
        {
            StartCoroutine(ProcessTouchForPlacement(touchPos));
        }
    }

    void TrySelectCar(Vector2 screenPosition)
    {
        if (isDraggingCar || isProcessingTouch) return;
        
        Camera cam = arCamera != null ? arCamera : Camera.main;
        Ray ray = cam.ScreenPointToRay(screenPosition);
        RaycastHit hit;
        
        foreach (var carData in placedCars)
        {
            if (carData == null || carData.carObject == null) continue;
            
            Collider carCol = carData.carObject.GetComponent<Collider>();
            if (carCol == null) continue;
            
            if (carCol.Raycast(ray, out hit, 100f))
            {
                isDraggingCar = true;
                selectedCarData = carData;
                dragStartPos = screenPosition;
                lastDragAngle = 0f;
                if (showDebugInfo)
                    Debug.Log("CarPlacement: Selected car for rotation");
                break;
            }
        }
    }

    void ContinueDrag(Vector2 screenPosition)
    {
        if (!isDraggingCar || selectedCarData == null || selectedCarData.carObject == null) return;
        
        // Don't rotate locked cars
        if (selectedCarData.isLocked) return;
        
        float deltaX = screenPosition.x - dragStartPos.x;
        float rotationDelta = (deltaX / 400f) * rotationSpeed;
        float deltaAngle = rotationDelta - lastDragAngle;
        lastDragAngle = rotationDelta;
        
        if (selectedCarData.plane != null)
        {
            // Rotate around the plane normal
            Vector3 normal = selectedCarData.plane.transform.up;
            selectedCarData.carObject.transform.Rotate(normal, deltaAngle, Space.World);
        }
        else
        {
            // Fallback to rotating around world up
            selectedCarData.carObject.transform.Rotate(Vector3.up, deltaAngle, Space.World);
        }
        
        // Update the original rotation value so it doesn't snap back
        selectedCarData.originalRotation = selectedCarData.carObject.transform.rotation;
    }

    void EndDrag()
    {
        isDraggingCar = false;
        if (showDebugInfo && selectedCarData != null)
            Debug.Log("CarPlacement: Ended car rotation");
        selectedCarData = null;
        dragStartPos = Vector2.zero;
        lastDragAngle = 0f;
    }

    IEnumerator ProcessTouchForPlacement(Vector2 screenPosition)
    {
        if (isProcessingTouch) yield break;
        isProcessingTouch = true;
        
        // Small delay to ensure we get the correct position data
        yield return new WaitForSeconds(0.05f);
        
        // Try to place the car with retries if needed
        bool placementSucceeded = false;
        int retryCount = 0;
        
        while (!placementSucceeded && retryCount < maxPlacementRetries)
        {
            if (retryCount > 0)
            {
                if (showDebugInfo)
                    Debug.Log($"CarPlacement: Retrying placement (attempt {retryCount+1}/{maxPlacementRetries})");
                yield return new WaitForSeconds(placementRetryDelay);
            }
            
            placementSucceeded = TryPlaceCar(screenPosition);
            retryCount++;
        }
        
        if (!placementSucceeded && showDebugInfo)
            Debug.Log("CarPlacement: Failed to place car after all retries");
            
        // Wait before allowing another placement
        yield return new WaitForSeconds(placementConfirmationDelay);
        isProcessingTouch = false;
    }

    bool TryPlaceCar(Vector2 screenPosition)
    {
        if (allowMultipleCars)
        {
            if (placedCars.Count >= maxCarsAllowed)
            {
                if (showDebugInfo)
                    Debug.Log($"CarPlacement: Maximum car limit ({maxCarsAllowed}) reached");
                return false;
            }
        }
        else if (placedCars.Count > 0)
        {
            RemoveCarAtIndex(0);
        }

        raycastHits.Clear();

        if (showDebugInfo)
            Debug.Log($"CarPlacement: Touch at screen position: {screenPosition}");

        // Use raycast against planes
        bool hitFound = raycastManager.Raycast(screenPosition, raycastHits, TrackableType.PlaneWithinPolygon);
        
        if (hitFound && raycastHits.Count > 0)
        {
            ARRaycastHit hit = raycastHits[0];
            ARPlane hitPlane = GetPlaneFromHit(hit);

            if (hitPlane != null && IsPlaneValid(hitPlane))
            {
                Vector3 hitPosition = hit.pose.position;
                Quaternion hitRotation = hit.pose.rotation;

                if (showDebugInfo)
                {
                    Debug.Log($"CarPlacement: Hit position: {hitPosition}");
                    Debug.Log($"CarPlacement: Hit plane ID: {hitPlane.trackableId}");
                    Debug.Log($"CarPlacement: Plane size: {hitPlane.size}");
                }

                if (showPlacementMarkers)
                    CreateDebugMarker(hitPosition);

                PlaceCarWithAnchor(hitPosition, hitRotation, hitPlane);
                return true;
            }
            else
            {
                if (showDebugInfo)
                    Debug.Log("CarPlacement: Invalid plane detected or plane too small");
                return false;
            }
        }
        else
        {
            if (showDebugInfo)
                Debug.Log("CarPlacement: No plane detected at touch position");
                
            // Try a different raycast approach as fallback (just for more robust detection)
            Ray ray = arCamera.ScreenPointToRay(screenPosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 10f);
            foreach (RaycastHit hit in hits)
            {
                // Check if we hit an ARPlane GameObject
                if (hit.collider.gameObject.GetComponent<ARPlane>() != null)
                {
                    ARPlane hitPlane = hit.collider.gameObject.GetComponent<ARPlane>();
                    if (IsPlaneValid(hitPlane))
                    {
                        if (showDebugInfo)
                            Debug.Log("CarPlacement: Found plane using Physics.Raycast fallback");
                        
                        Vector3 hitPosition = hit.point;
                        Quaternion hitRotation = Quaternion.LookRotation(
                            Vector3.ProjectOnPlane(arCamera.transform.forward, hitPlane.normal),
                            hitPlane.normal
                        );
                        
                        if (showPlacementMarkers)
                            CreateDebugMarker(hitPosition);
                            
                        PlaceCarWithAnchor(hitPosition, hitRotation, hitPlane);
                        return true;
                    }
                }
            }
            
            return false;
        }
    }

    void CreateDebugMarker(Vector3 position)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "TouchMarker_" + debugMarkers.Count;
        marker.transform.position = position;
        marker.transform.localScale = Vector3.one * 0.02f;
        Renderer markerRenderer = marker.GetComponent<Renderer>();
        Material markerMat = new Material(Shader.Find("Standard"));
        markerMat.color = new Color(1, 0, 0, 0.7f);
        markerMat.SetFloat("_Mode", 3);
        markerMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        markerMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        markerMat.SetInt("_ZWrite", 0);
        markerMat.DisableKeyword("_ALPHATEST_ON");
        markerMat.EnableKeyword("_ALPHABLEND_ON");
        markerMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        markerMat.renderQueue = 3000;
        markerRenderer.material = markerMat;
        debugMarkers.Add(marker);
        Destroy(marker, 10f);
        if (showDebugInfo)
            Debug.Log($"CarPlacement: Debug marker placed at: {position}");
    }

    ARPlane GetPlaneFromHit(ARRaycastHit hit)
    {
        if (hit.trackable is ARPlane plane)
            return plane;
        if (planeManager?.trackables != null)
        {
            foreach (var detectedPlane in planeManager.trackables)
            {
                if (detectedPlane?.trackableId == hit.trackableId)
                    return detectedPlane;
            }
        }
        return null;
    }

    bool IsPlaneValid(ARPlane plane)
    {
        if (plane == null) return false;
        try
        {
            float planeArea = plane.size.x * plane.size.y;
            bool isLargeEnough = planeArea >= minPlaneArea;
            bool isTracking = plane.trackingState == TrackingState.Tracking;
            bool hasValidSize = plane.size.x > 0 && plane.size.y > 0;
            if (showDebugInfo && !isLargeEnough)
                Debug.Log($"CarPlacement: Plane too small. Area: {planeArea:F4}, Required: {minPlaneArea}");
            return isLargeEnough && isTracking && hasValidSize;
        }
        catch (System.Exception e)
        {
            if (showDebugInfo)
                Debug.LogWarning($"CarPlacement: Error validating plane: {e.Message}");
            return false;
        }
    }

    Vector3 CalculateCarScale(ARPlane plane)
    {
        if (plane == null) return originalCarScale * carScaleFactor;

        try
        {
            Vector2 planeSize = plane.size;
            if (planeSize.x <= 0 || planeSize.y <= 0) return originalCarScale * carScaleFactor;

            // Soft clamp range: car only scales with plane size *within* this range
            float minResponsivePlane = 0.5f; // meters
            float maxResponsivePlane = 2.0f; // meters

            float averagePlaneSize = (planeSize.x + planeSize.y) * 0.5f;

            // Interpolation factor between min and max scale, only within the responsive range
            float t = Mathf.InverseLerp(minResponsivePlane, maxResponsivePlane, Mathf.Clamp(averagePlaneSize, minResponsivePlane, maxResponsivePlane));

            // Calculate base and max scale
            Vector3 minScale = originalCarScale * carScaleFactor;
            Vector3 maxScale = maxCarScale;

            // Lerp between min and max scale
            Vector3 calculatedScale = Vector3.Lerp(minScale, maxScale, t);

            // Clamp result to min/maxCarScale
            calculatedScale.x = Mathf.Clamp(calculatedScale.x, minCarScale.x, maxCarScale.x);
            calculatedScale.y = Mathf.Clamp(calculatedScale.y, minCarScale.y, maxCarScale.y);
            calculatedScale.z = Mathf.Clamp(calculatedScale.z, minCarScale.z, maxCarScale.z);

            return calculatedScale;
        }
        catch (System.Exception e)
        {
            if (showDebugInfo)
                Debug.LogWarning($"CarPlacement: Error calculating scale: {e.Message}");
            return originalCarScale * carScaleFactor;
        }
    }

    // NEW: Method for placing car with anchor support
    void PlaceCarWithAnchor(Vector3 position, Quaternion rotation, ARPlane plane)
    {
        if (carPrefab == null)
        {
            Debug.LogError("CarPlacement: Cannot place car - prefab is null!");
            return;
        }
        
        GameObject newCar = GetCarFromPool() ?? Instantiate(carPrefab);
        newCar.transform.position = position;
        newCar.transform.rotation = rotation;
        newCar.SetActive(true);
        Vector3 carScale = CalculateCarScale(plane);
        newCar.transform.localScale = carScale;
        
        if (!disableRotationAlignment)
            AlignCarToPlaneExact(newCar, plane, position);
        
        // Create car data object
        CarPlacementData carData = new CarPlacementData(newCar, plane);
        
        // Add an anchor if enabled
        if (useAnchoring && anchorManager != null)
        {
            ARAnchor anchor = null;
            
            // Try to attach anchor
            try
            {
                // Create a new anchor at the car's position and rotation
                anchor = anchorManager.AddAnchor(new Pose(position, newCar.transform.rotation));
                
                if (anchor != null)
                {
                    if (showDebugInfo)
                        Debug.Log($"CarPlacement: Created anchor with ID: {anchor.trackableId}");
                    
                    // Make the car a child of the anchor to keep it stable
                    newCar.transform.SetParent(anchor.transform);
                    
                    // Store the anchor reference
                    carData.anchor = anchor;
                    
                    // Ensure car maintains its world position and rotation after parenting
                    newCar.transform.position = position;
                    newCar.transform.rotation = carData.originalRotation;
                }
                else if (showDebugInfo)
                {
                    Debug.LogWarning("CarPlacement: Failed to create anchor");
                }
            }
            catch (System.Exception e)
            {
                if (showDebugInfo)
                    Debug.LogWarning($"CarPlacement: Error creating anchor: {e.Message}");
            }
        }
        
        // If freezing position is enabled, add a kinematic Rigidbody
        if (freezePositionAfterPlacement)
        {
            Rigidbody rb = newCar.GetComponent<Rigidbody>();
            if (rb == null)
                rb = newCar.AddComponent<Rigidbody>();
            
            rb.isKinematic = true;
            rb.useGravity = false;
            carData.isLocked = true;
            
            if (showDebugInfo)
                Debug.Log("CarPlacement: Added kinematic rigidbody to lock position");
        }
        
        // Add the car data to our list
        placedCars.Add(carData);

        if (interactionManager != null)
        {
            var method = interactionManager.GetType().GetMethod("OnCarPlaced");
            if (method != null)
            {
                try { method.Invoke(interactionManager, new object[] { newCar, plane }); }
                catch (System.Exception e)
                {
                    if (showDebugInfo)
                        Debug.LogWarning($"CarPlacement: Error notifying interaction manager: {e.Message}");
                }
            }
        }

        if (showDebugInfo)
        {
            Debug.Log($"CarPlacement: Car placed at: {position}");
            Debug.Log($"CarPlacement: Car actual position: {newCar.transform.position}");
            Debug.Log($"CarPlacement: Position difference: {Vector3.Distance(position, newCar.transform.position):F6}");
        }
    }

    void AlignCarToPlaneExact(GameObject car, ARPlane plane, Vector3 exactPosition)
    {
        if (car == null || plane == null) return;
        try
        {
            Vector3 savedPosition = exactPosition;
            Vector3 planeNormal = plane.transform.up;
            Vector3 carForward = car.transform.forward;
            Vector3 projectedForward = Vector3.ProjectOnPlane(carForward, planeNormal).normalized;
            
            // If we can't get a projected forward (e.g. camera looking straight down),
            // use a different approach
            if (projectedForward.sqrMagnitude < 0.001f)
            {
                projectedForward = Vector3.ProjectOnPlane(arCamera.transform.forward, planeNormal).normalized;
                if (projectedForward.sqrMagnitude < 0.001f)
                    projectedForward = Vector3.ProjectOnPlane(Vector3.forward, planeNormal).normalized;
            }
                
            if (projectedForward != Vector3.zero)
                car.transform.rotation = Quaternion.LookRotation(projectedForward, planeNormal);
                
            // Make sure to restore the exact position after alignment
            car.transform.position = savedPosition;
            if (showDebugInfo)
            {
                Debug.Log($"CarPlacement: Car position after alignment: {car.transform.position}");
                Debug.Log($"CarPlacement: Maintained exact position: {Vector3.Distance(savedPosition, car.transform.position) < 0.001f}");
            }
        }
        catch (System.Exception e)
        {
            if (showDebugInfo)
                Debug.LogWarning($"CarPlacement: Error aligning car: {e.Message}");
        }
    }

    GameObject GetCarFromPool()
    {
        if (!enableObjectPooling || carPool.Count == 0) return null;
        GameObject pooledCar = carPool.Dequeue();
        pooledCar.SetActive(true);
        return pooledCar;
    }

    void ReturnCarToPool(GameObject car)
    {
        if (!enableObjectPooling || car == null) return;
        car.SetActive(false);
        car.transform.SetParent(poolParent);
        carPool.Enqueue(car);
    }

    void CleanupNullReferences()
    {
        for (int i = placedCars.Count - 1; i >= 0; i--)
        {
            if (placedCars[i] == null || placedCars[i].carObject == null)
                placedCars.RemoveAt(i);
        }
        
        for (int i = debugMarkers.Count - 1; i >= 0; i--)
        {
            if (debugMarkers[i] == null)
                debugMarkers.RemoveAt(i);
        }
    }

    void RemoveCarAtIndex(int index)
    {
        if (index < 0 || index >= placedCars.Count) return;
        
        CarPlacementData carData = placedCars[index];
        if (carData == null) return;
        
        // First remove the anchor if it exists
        if (carData.anchor != null)
        {
            // Reparent the car before destroying the anchor
            if (carData.carObject != null)
                carData.carObject.transform.SetParent(null);
                
            if (anchorManager != null)
                anchorManager.RemoveAnchor(carData.anchor);
            else
                Destroy(carData.anchor.gameObject);
        }
        
        // Then handle the car object
        if (carData.carObject != null)
        {
            if (enableObjectPooling) 
                ReturnCarToPool(carData.carObject);
            else 
                Destroy(carData.carObject);
        }
        
        placedCars.RemoveAt(index);
    }

    public void RemoveAllCars()
    {
        for (int i = placedCars.Count - 1; i >= 0; i--)
            RemoveCarAtIndex(i);
        if (showDebugInfo)
            Debug.Log("CarPlacement: All cars removed");
    }

    public void RemoveLastCar()
    {
        if (placedCars.Count > 0) RemoveCarAtIndex(placedCars.Count - 1);
    }

    public void ClearDebugMarkers()
    {
        foreach (GameObject marker in debugMarkers)
            if (marker != null) Destroy(marker);
        debugMarkers.Clear();
        if (showDebugInfo)
            Debug.Log("CarPlacement: Debug markers cleared");
    }

    public void ToggleMultipleCars()
    {
        allowMultipleCars = !allowMultipleCars;
        if (showDebugInfo)
            Debug.Log($"CarPlacement: Multiple cars {(allowMultipleCars ? "enabled" : "disabled")}");
    }

    public bool ArePlanesDetected() => planesDetected;
    public int GetDetectedPlaneCount() => detectedPlaneCount;
    public int GetPlacedCarCount() => placedCars.Count;
    public bool CanPlaceMoreCars() => allowMultipleCars ? placedCars.Count < maxCarsAllowed : placedCars.Count == 0;
    public GameObject GetLastPlacedCar() => placedCars.Count > 0 ? placedCars[placedCars.Count - 1].carObject : null;

    public void EnterInteractionMode()
    {
        isInInteractionMode = true;
        if (showDebugInfo)
            Debug.Log("CarPlacement: Entering interaction mode - car placement disabled");
    }
    
    public void ExitInteractionMode()
    {
        isInInteractionMode = false;
        if (showDebugInfo)
            Debug.Log("CarPlacement: Exiting interaction mode - car placement enabled");
    }

    void OnDestroy()
    {
        if (planeManager != null)
            planeManager.planesChanged -= OnPlanesChanged;
        if (enableObjectPooling && poolParent != null)
        {
            while (carPool.Count > 0)
            {
                GameObject pooledCar = carPool.Dequeue();
                if (pooledCar != null) Destroy(pooledCar);
            }
            Destroy(poolParent.gameObject);
        }
        ClearDebugMarkers();
        
        // Clean up anchors
        foreach (var carData in placedCars)
        {
            if (carData != null && carData.anchor != null)
            {
                if (anchorManager != null)
                    anchorManager.RemoveAnchor(carData.anchor);
                else
                    Destroy(carData.anchor.gameObject);
            }
        }
    }

    [System.Serializable]
    public class CarPlacementSettings
    {
        public bool allowMultipleCars;
        public float carScaleFactor;
        public float minPlaneArea;
        public int maxCarsAllowed;
        public bool useExactTouchPosition;
        public bool disablePositionAdjustments;
        public bool useAnchoring;  // NEW: Added stability setting
        public bool freezePositionAfterPlacement;  // NEW: Added stability setting
    }

    public CarPlacementSettings GetCurrentSettings()
    {
        return new CarPlacementSettings
        {
            allowMultipleCars = this.allowMultipleCars,
            carScaleFactor = this.carScaleFactor,
            minPlaneArea = this.minPlaneArea,
            maxCarsAllowed = this.maxCarsAllowed,
            useExactTouchPosition = this.useExactTouchPosition,
            disablePositionAdjustments = this.disablePositionAdjustments,
            useAnchoring = this.useAnchoring,
            freezePositionAfterPlacement = this.freezePositionAfterPlacement
        };
    }

    public void ApplySettings(CarPlacementSettings settings)
    {
        allowMultipleCars = settings.allowMultipleCars;
        carScaleFactor = settings.carScaleFactor;
        minPlaneArea = settings.minPlaneArea;
        maxCarsAllowed = settings.maxCarsAllowed;
        useExactTouchPosition = settings.useExactTouchPosition;
        disablePositionAdjustments = settings.disablePositionAdjustments;
        useAnchoring = settings.useAnchoring;
        freezePositionAfterPlacement = settings.freezePositionAfterPlacement;
    }
}