using UnityEngine;
using UnityEngine.EventSystems;

public class CarInteractionManager : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private LayerMask interactableLayers;
    [SerializeField] private float maxRaycastDistance = 100f;
    
    [Header("UI References")]
    [SerializeField] private MonoBehaviour infoPanelManager; // Changed to MonoBehaviour
    
    private EnhancedCarPartInteraction currentPart;
    private Camera mainCamera;
    
    private void Start()
    {
        mainCamera = Camera.main;
        
        // Register to all parts' events in the scene
        EnhancedCarPartInteraction[] allParts = FindObjectsOfType<EnhancedCarPartInteraction>();
        foreach (var part in allParts)
        {
            // We need to adapt this to use our new approach without AccessoryData
            // This might need adjustment based on how EnhancedCarPartInteraction is defined
        }
    }
    
    private void Update()
    {
        // Check for touch/click input
        if (Input.GetMouseButtonDown(0))
        {
            // Check if we're clicking on UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;
                
            HandleTouchStart(Input.mousePosition);
        }
    }
    
    private void HandleTouchStart(Vector2 screenPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, maxRaycastDistance, interactableLayers))
        {
            // Try to get car part interaction component
            EnhancedCarPartInteraction part = hit.collider.GetComponent<EnhancedCarPartInteraction>();
            if (part != null)
            {
                currentPart = part;
                currentPart.OnInteract();
            }
        }
    }
    
    // Replace the methods that used AccessoryData with string-based methods
    
    // Replaced method - now only uses string ID
    public void ShowAccessoryInfo(string accessoryId)
    {
        if (string.IsNullOrEmpty(accessoryId))
        {
            Debug.LogWarning("Attempted to show info for null or empty accessory ID");
            return;
        }
        
        // Try to use reflection to call the method on infoPanelManager
        if (infoPanelManager != null)
        {
            var method = infoPanelManager.GetType().GetMethod("ShowAccessoryInfo", new[] { typeof(string) });
            if (method != null)
            {
                method.Invoke(infoPanelManager, new object[] { accessoryId });
            }
            else
            {
                Debug.LogWarning("ShowAccessoryInfo method not found on infoPanelManager");
                ShowAccessoryDetails(accessoryId);
            }
        }
        else
        {
            // Fallback to find panel manager in the scene
            var foundPanel = FindObjectOfType<MonoBehaviour>();
            if (foundPanel != null)
            {
                var method = foundPanel.GetType().GetMethod("ShowAccessoryInfo", new[] { typeof(string) });
                if (method != null)
                {
                    infoPanelManager = foundPanel; // Cache for future use
                    method.Invoke(foundPanel, new object[] { accessoryId });
                }
                else
                {
                    // Fallback if no suitable method found
                    ShowAccessoryDetails(accessoryId);
                }
            }
            else
            {
                // Fallback implementation if no panel manager is available
                ShowAccessoryDetails(accessoryId);
            }
        }
    }
    
    // Fallback implementation when no panel manager is available
    private void ShowAccessoryDetails(string accessoryId)
    {
        Debug.Log($"Showing details for accessory ID: {accessoryId} (fallback implementation)");
        
        // You might need to implement a data lookup method here using only string-based data
        // For example, using PlayerPrefs, a Dictionary, or other non-AccessoryData approaches
    }
}