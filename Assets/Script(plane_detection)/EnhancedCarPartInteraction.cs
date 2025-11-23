using UnityEngine;
using UnityEngine.Events;
using CarAccessories;

public class EnhancedCarPartInteraction : MonoBehaviour
{
    [SerializeField] private string accessoryId;
    [SerializeField] private AccessoryInfo accessoryInfo;
    
    // Define a UnityEvent that passes AccessoryInfo
    [System.Serializable]
    public class AccessoryInfoEvent : UnityEvent<AccessoryInfo> { }
    
    public AccessoryInfoEvent OnShowDetails = new AccessoryInfoEvent();
    
    public void OnInteract()
    {
        if (accessoryInfo != null)
        {
            OnShowDetails.Invoke(accessoryInfo);
        }
        else if (!string.IsNullOrEmpty(accessoryId))
        {
            // Try to get AccessoryInfo from the database
            var accessoryDb = FindObjectOfType<AccessoryDatabase>();
            if (accessoryDb != null)
            {
                accessoryInfo = accessoryDb.GetAccessoryByID(accessoryId);
                if (accessoryInfo != null)
                {
                    OnShowDetails.Invoke(accessoryInfo);
                }
            }
        }
    }
}