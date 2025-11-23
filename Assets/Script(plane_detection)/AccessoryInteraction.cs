using UnityEngine;
using UnityEngine.EventSystems;
using CarAccessories; // Add this line

public class AccessoryInteraction : MonoBehaviour
{
    [SerializeField] private string accessoryID;
    [SerializeField] private string accessoryName;
    [SerializeField] private string detailSceneName;
    
    private ARInteractionManager interactionManager;
    private AccessoryInfoPanelManager infoPanel;
    
    private void Start()
    {
        interactionManager = FindObjectOfType<ARInteractionManager>();
        infoPanel = FindObjectOfType<AccessoryInfoPanelManager>();
    }
    
    private void OnMouseDown()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;
            
        if (interactionManager != null && interactionManager.IsInteractionModeActive())
        {
            HandleInteraction();
        }
    }
    
    public void HandleInteraction()
    {
        // Provide feedback
        Handheld.Vibrate();
        
        // Show accessory info
        if (infoPanel != null)
        {
            AccessoryInfo info = GetAccessoryInfo();
            infoPanel.DisplayAccessoryInfo(info);
        }
    }
    
    private AccessoryInfo GetAccessoryInfo()
    {
        // Get info from database if available
        if (interactionManager != null && interactionManager.accessoryDatabase != null)
        {
            AccessoryInfo accessoryInfo = interactionManager.accessoryDatabase.GetAccessoryByID(accessoryID);
            if (accessoryInfo != null)
            {
                return accessoryInfo;
            }
        }
        
        // Fallback: create basic info
        AccessoryInfo fallbackInfo = new AccessoryInfo(accessoryID, accessoryName);
        fallbackInfo.description = "Detailed information not available.";
        
        return fallbackInfo;
    }
    
    public string GetAccessoryID()
    {
        return accessoryID;
    }
    
    public string GetDetailSceneName()
    {
        return detailSceneName;
    }
}