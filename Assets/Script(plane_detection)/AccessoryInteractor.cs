using UnityEngine;
using UnityEngine.UI;

public class AccessoryInteractor : MonoBehaviour
{
    public string accessoryID;
    public GameObject interactionButtonPrefab;
    private GameObject interactionButton;
    private bool isPlayerNear = false;
    private ARInteractionManager interactionManager;
    
    private void Start()
    {
        interactionManager = FindObjectOfType<ARInteractionManager>();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MainCamera") && interactionManager != null && interactionManager.IsInteractionModeActive())
        {
            isPlayerNear = true;
            ShowInteractionButton();
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("MainCamera"))
        {
            isPlayerNear = false;
            HideInteractionButton();
        }
    }
    
    private void ShowInteractionButton()
    {
        if (interactionButton == null && interactionButtonPrefab != null)
        {
            interactionButton = Instantiate(interactionButtonPrefab, transform.position + Vector3.up * 0.2f, Quaternion.identity);
            interactionButton.transform.SetParent(transform);
            
            Button button = interactionButton.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => interactionManager.ShowAccessoryDetails(accessoryID));
            }
        }
    }
    
    private void HideInteractionButton()
    {
        if (interactionButton != null)
        {
            Destroy(interactionButton);
            interactionButton = null;
        }
    }
    
    private void Update()
    {
        if (isPlayerNear && interactionButton != null)
        {
            // Make the button face the camera
            interactionButton.transform.LookAt(Camera.main.transform);
            interactionButton.transform.Rotate(0, 180, 0);
        }
    }
}