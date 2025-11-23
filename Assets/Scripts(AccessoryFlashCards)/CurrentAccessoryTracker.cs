using UnityEngine;

public class CurrentAccessoryTracker : MonoBehaviour
{
    // Singleton pattern
    public static CurrentAccessoryTracker Instance { get; private set; }
    
    // Static property to track the current accessory name across scenes
    public static string CurrentAccessoryName { get; set; }
    
    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
}