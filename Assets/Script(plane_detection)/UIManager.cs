using UnityEngine;
using UnityEngine.UI;

namespace CarAccessories
{
    /// <summary>
    /// Singleton manager for handling all UI display logic.
    /// Responsible for instantiating, populating, and destroying the accessory info panel.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        // Singleton instance
        public static UIManager Instance { get; private set; }

        [Header("UI Prefab")]
        [SerializeField] private AccessoryPanelUI accessoryInfoPanelPrefab;

        private AccessoryPanelUI currentPanelInstance;

        private void Awake()
        {
            // Singleton pattern implementation
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
        }

        /// <summary>
        /// Public method to be called by UnityEvents from AccessoryInteraction components.
        /// It displays the info panel with the provided data.
        /// </summary>
        /// <param name="data">The ScriptableObject data of the selected accessory.</param>
        public void DisplayAccessoryInfo(AccessoryInfo data)
        {
            // If a panel is already showing, destroy it before creating a new one.
            if (currentPanelInstance != null)
            {
                Destroy(currentPanelInstance.gameObject);
            }

            // Determine a position in front of the camera to spawn the UI.
            Transform cameraTransform = Camera.main.transform;
            Vector3 spawnPosition = cameraTransform.position + (cameraTransform.forward * 1.5f); // 1.5 meters in front

            // Instantiate the panel prefab.
            currentPanelInstance = Instantiate(accessoryInfoPanelPrefab, spawnPosition, Quaternion.identity);

            // Populate the UI elements with data from the ScriptableObject.
            currentPanelInstance.nameText.text = data.accessoryName;
            currentPanelInstance.manufacturerText.text = $"Manufacturer: {data.manufacturer}";
            currentPanelInstance.descriptionText.text = data.description;
            currentPanelInstance.featuresText.text = $"Features: {data.features}";
            currentPanelInstance.modelNumberText.text = $"Model No: {data.modelNumber}";
            currentPanelInstance.priceText.text = $"Price: {data.price}";

            // Add a listener to the close button to call the HideAccessoryInfo method.
            currentPanelInstance.closeButton.onClick.AddListener(HideAccessoryInfo);
        }

        /// <summary>
        /// Hides and destroys the currently active info panel.
        /// </summary>
        public void HideAccessoryInfo()
        {
            if (currentPanelInstance != null)
            {
                currentPanelInstance.closeButton.onClick.RemoveListener(HideAccessoryInfo);
                Destroy(currentPanelInstance.gameObject);
                currentPanelInstance = null;
            }
        }
    }
}