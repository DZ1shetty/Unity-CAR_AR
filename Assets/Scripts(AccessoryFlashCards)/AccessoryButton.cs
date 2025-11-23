using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class AccessoryButton : MonoBehaviour
{
    public string accessoryName; // Set this in the Inspector (e.g., "SteeringWheel", "RearViewMirror", etc.)
    
    void Start()
    {
        Button button = GetComponent<Button>();
        button.onClick.AddListener(OnInteractClicked);
    }
    
    void OnInteractClicked()
    {
        // Store which accessory we're going to interact with
        CurrentAccessoryTracker.CurrentAccessoryName = accessoryName;
        
        // Load the interaction scene
        SceneManager.LoadScene("AccessoryInteractionScene");
    }
}