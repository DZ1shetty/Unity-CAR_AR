using UnityEngine;
using System.Collections.Generic;
using CarAccessories;

public class AccessoryDatabase : MonoBehaviour
{
    [SerializeField] private List<AccessoryData> accessories = new List<AccessoryData>();
    
    private Dictionary<string, AccessoryInfo> accessoryCache;
    
    private void Awake()
    {
        InitializeCache();
    }
    
    private void InitializeCache()
    {
        accessoryCache = new Dictionary<string, AccessoryInfo>();
        
        foreach (var accessory in accessories)
        {
            if (accessory != null)
            {
                accessoryCache[accessory.id] = accessory.ToAccessoryInfo();
            }
        }
    }
    
    public AccessoryInfo GetAccessoryByID(string accessoryId)
    {
        if (string.IsNullOrEmpty(accessoryId))
            return null;
            
        // Initialize cache if needed
        if (accessoryCache == null)
            InitializeCache();
            
        // Try to get from cache
        if (accessoryCache.TryGetValue(accessoryId, out AccessoryInfo info))
            return info;
            
        // If not found, search through accessories list
        foreach (var accessory in accessories)
        {
            if (accessory != null && accessory.id == accessoryId)
            {
                AccessoryInfo newInfo = accessory.ToAccessoryInfo();
                accessoryCache[accessoryId] = newInfo;
                return newInfo;
            }
        }
        
        return null;
    }
}