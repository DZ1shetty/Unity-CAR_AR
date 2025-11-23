using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.EventSystems;

public class ARPlaneDetector : MonoBehaviour
{
    public ARRaycastManager raycastManager;
    public ARInteractionManager interactionManager;
    
    private void Update()
    {
        // Don't do anything if the interaction mode is already active
        if (interactionManager != null && interactionManager.IsInteractionModeActive())
            return;
            
        // Check for touch input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            // Ignore UI touches
            if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                return;
                
            if (touch.phase == TouchPhase.Began)
            {
                // Raycast to find planes
                List<ARRaycastHit> hits = new List<ARRaycastHit>();
                if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
                {
                    // Get the hit pose
                    Pose hitPose = hits[0].pose;
                    
                    // Place the car
                    interactionManager.PlaceCar(hitPose.position, hitPose.rotation);
                }
            }
        }
    }
}