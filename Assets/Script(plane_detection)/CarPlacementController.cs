using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System.Collections.Generic;

public class CarPlacementController : MonoBehaviour
{
    [Header("AR Components")]
    public ARPlaneManager planeManager;
    public ARRaycastManager raycastManager;

    [Header("Car Prefab")]
    public GameObject carPrefab; // Renamed for generality
    private GameObject placedCar;

    private bool isCarPlaced = false;

    void Start()
    {
        // Validate required components
        if (planeManager == null || raycastManager == null || carPrefab == null)
        {
            Debug.LogError("CarPlacementController: One or more required fields not assigned in the Inspector.");
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (isCarPlaced)
            return;

        // Handle touch input for car placement
        if (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Began)
        {
            List<ARRaycastHit> hits = new List<ARRaycastHit>();
            if (raycastManager.Raycast(Input.touches[0].position, hits, UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinPolygon))
            {
                // Place car at raycast hit position
                Pose hitPose = hits[0].pose;
                if (placedCar == null)
                {
                    placedCar = Instantiate(carPrefab, hitPose.position, hitPose.rotation);
                }
                else
                {
                    placedCar.transform.position = hitPose.position;
                    placedCar.transform.rotation = hitPose.rotation;
                }

                isCarPlaced = true;
                // After placement, optionally disable further plane detection
                DisablePlaneDetection();
                EnableCarInteraction();
            }
        }
    }

    private void DisablePlaneDetection()
    {
        if (planeManager != null)
        {
            planeManager.enabled = false;
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(false);
            }
        }
    }

    public void DetectAnotherPlane()
    {
        // Re-enable plane detection if it was disabled
        if (planeManager != null)
        {
            planeManager.enabled = true;
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(true);
            }
        }

        // Destroy or hide the current car
        if (placedCar != null)
        {
            Destroy(placedCar);
            placedCar = null;
        }

        isCarPlaced = false;
    }

    private void EnableCarInteraction()
    {
        if (placedCar != null)
        {
            // Get the car interaction manager component
            CarInteractionManager carInteraction = placedCar.GetComponent<CarInteractionManager>();
            if (carInteraction != null)
            {
                carInteraction.gameObject.SetActive(true);
                carInteraction.enabled = true;
                EnableCarComponents(placedCar);
            }
        }
    }

    private void EnableCarComponents(GameObject car)
    {
        // Enable all colliders
        Collider[] colliders = car.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
        {
            collider.enabled = true;
        }

        // Enable all interaction-related scripts
        MonoBehaviour[] scripts = car.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (MonoBehaviour script in scripts)
        {
            if (script.GetType().Name.Contains("Interaction"))
            {
                script.enabled = true;
            }
        }
    }
}