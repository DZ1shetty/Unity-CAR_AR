using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class ARTapInteractor : MonoBehaviour
{
    [SerializeField] private Camera arCamera;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private float maxRaycastDistance = 10f;
    
    private ARInteractionManager interactionManager;
    private AccessoryInfoPanelManager infoPanel;
    private AccessoryDetailManager detailManager;
    
    private void Start()
    {
        if (arCamera == null)
            arCamera = Camera.main;
            
        interactionManager = FindObjectOfType<ARInteractionManager>();
        infoPanel = FindObjectOfType<AccessoryInfoPanelManager>();
        detailManager = FindObjectOfType<AccessoryDetailManager>();
    }
    
    private void Update()
    {
        // Check if interaction mode is active
        if (interactionManager != null && !interactionManager.IsInteractionModeActive())
            return;
            
        // Process touch input
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            // Ignore UI touches
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                return;
                
            HandleTap(Input.GetTouch(0).position);
        }
    }
    
    private void HandleTap(Vector2 screenPosition)
    {
        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, maxRaycastDistance, interactableLayer))
        {
            // Check for accessory interaction component
            AccessoryInteraction accessoryInteraction = hit.transform.GetComponent<AccessoryInteraction>();
            if (accessoryInteraction != null)
            {
                accessoryInteraction.HandleInteraction();
                return;
            }
            
            // Check for enhanced car part interaction
            EnhancedCarPartInteraction carPart = hit.transform.GetComponent<EnhancedCarPartInteraction>();
            if (carPart != null)
            {
                carPart.OnInteract();
                return;
            }
            
            // Generic object interaction
            string accessoryID = hit.transform.gameObject.name;
            if (detailManager != null)
            {
                detailManager.ShowAccessoryDetails(accessoryID);
            }
            else if (interactionManager != null)
            {
                interactionManager.ShowAccessoryDetails(accessoryID);
            }
        }
    }
    
    public bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        
        if (Input.touchCount > 0)
        {
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        }
        
        return false;
    }
}