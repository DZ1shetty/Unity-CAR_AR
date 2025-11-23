using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class FlashcardManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI manufacturerText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI featuresText;
    [SerializeField] private Image accessoryImage;
    
    [Header("Navigation Buttons")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button interactButton;
    [SerializeField] private Button backButton;
    
    [Header("Accessory Data")]
    [SerializeField] private AccessoryData[] accessories;
    private int currentIndex = 0;
    
    [Header("Flashcard GameObjects")]
    [SerializeField] private GameObject steeringWheelFlashcard;
    [SerializeField] private GameObject rearViewMirrorFlashcard;
    [SerializeField] private GameObject[] accessoryFlashcards; // Additional accessory flashcards
    
    [System.Serializable]
    public class AccessoryData
    {
        public string name;
        public string manufacturer;
        public string price;
        public string description;
        public string[] features;
        public Sprite image;
        public string internalName; // Used to match with GameObject names (e.g. "SteeringWheel")
    }
    
    void Start()
    {
        // Set up button listeners
        if (nextButton != null)
            nextButton.onClick.AddListener(NextCard);
        
        if (previousButton != null)
            previousButton.onClick.AddListener(PreviousCard);
            
        if (interactButton != null)
            interactButton.onClick.AddListener(InteractWithAccessory);
        
        // Set up back button listener
        if (backButton != null)
            backButton.onClick.AddListener(BackToMainScene);
        
        // Check if we need to load a specific accessory from CurrentAccessoryTracker
        if (CurrentAccessoryTracker.CurrentAccessoryName != null && !string.IsNullOrEmpty(CurrentAccessoryTracker.CurrentAccessoryName))
        {
            // Find the index of the accessory with the matching internal name
            for (int i = 0; i < accessories.Length; i++)
            {
                if (accessories[i].internalName == CurrentAccessoryTracker.CurrentAccessoryName)
                {
                    currentIndex = i;
                    break;
                }
            }
        }
        else if (PlayerPrefs.HasKey("SelectedAccessoryIndex"))
        {
            // Otherwise, use the stored index if available
            currentIndex = PlayerPrefs.GetInt("SelectedAccessoryIndex", 0);
        }
        
        // Display the current accessory
        DisplayCurrentAccessory();
        
        // Log for debugging
        Debug.Log("FlashcardManager started. Total accessories: " + accessories.Length);
    }
    
    void DisplayCurrentAccessory()
    {
        if (accessories == null || accessories.Length == 0)
        {
            Debug.LogError("No accessories defined!");
            return;
        }
        
        if (currentIndex < 0 || currentIndex >= accessories.Length)
        {
            Debug.LogError("Current index out of range: " + currentIndex);
            return;
        }
        
        AccessoryData current = accessories[currentIndex];
        
        // Update UI elements
        if (nameText != null) nameText.text = current.name;
        if (manufacturerText != null) manufacturerText.text = current.manufacturer;
        if (priceText != null) priceText.text = current.price;
        if (descriptionText != null) descriptionText.text = current.description;
        
        // Update features text
        if (featuresText != null && current.features != null)
        {
            string featuresStr = "";
            foreach (var feature in current.features)
            {
                featuresStr += "• " + feature + "\n";
            }
            featuresText.text = featuresStr;
        }
        
        // Update image
        if (accessoryImage != null && current.image != null)
        {
            accessoryImage.sprite = current.image;
            accessoryImage.enabled = true;
        }
        else if (accessoryImage != null)
        {
            accessoryImage.enabled = false;
        }
        
        // Hide all flashcards first, then show the appropriate one
        HideAllFlashcards();
        ShowFlashcardByName(current.internalName);
        
        // Save the current index and name for other scenes
        PlayerPrefs.SetInt("SelectedAccessoryIndex", currentIndex);
        if (CurrentAccessoryTracker.Instance != null)
        {
            CurrentAccessoryTracker.CurrentAccessoryName = current.internalName;
        }
        PlayerPrefs.Save();
        
        Debug.Log($"Displayed accessory {currentIndex}: {current.name}");
    }
    
    void HideAllFlashcards()
    {
        if (steeringWheelFlashcard != null) steeringWheelFlashcard.SetActive(false);
        if (rearViewMirrorFlashcard != null) rearViewMirrorFlashcard.SetActive(false);
        
        // Hide any additional flashcards
        if (accessoryFlashcards != null)
        {
            foreach (var flashcard in accessoryFlashcards)
            {
                if (flashcard != null) flashcard.SetActive(false);
            }
        }
    }
    
    void ShowFlashcardByName(string accessoryName)
    {
        switch(accessoryName)
        {
            case "SteeringWheel":
                if (steeringWheelFlashcard != null) steeringWheelFlashcard.SetActive(true);
                break;
                
            case "RearViewMirror":
                if (rearViewMirrorFlashcard != null) rearViewMirrorFlashcard.SetActive(true);
                break;
                
            // Add cases for other accessories as needed
            
            default:
                // If nothing matches or accessory name is not recognized,
                // use the standard UI display only
                break;
        }
    }
    
    void NextCard()
    {
        currentIndex = (currentIndex + 1) % accessories.Length;
        DisplayCurrentAccessory();
        Debug.Log("Moved to next card: " + currentIndex);
    }
    
    void PreviousCard()
    {
        currentIndex = (currentIndex - 1 + accessories.Length) % accessories.Length;
        DisplayCurrentAccessory();
        Debug.Log("Moved to previous card: " + currentIndex);
    }
    
    void InteractWithAccessory()
    {
        // Make sure the current index and name are saved before loading the interaction scene
        PlayerPrefs.SetInt("SelectedAccessoryIndex", currentIndex);
        
        if (CurrentAccessoryTracker.Instance != null)
        {
            CurrentAccessoryTracker.CurrentAccessoryName = accessories[currentIndex].internalName;
        }
        
        PlayerPrefs.Save();
        
        // Load the interaction scene
        SceneManager.LoadScene("AccessoryInteractionScene");
        Debug.Log("Loading interaction scene with accessory index: " + currentIndex);
    }
    
    // Method to handle the "Back To Main" button click
    public void BackToMainScene()
    {
        // Load the Home Scene
        SceneManager.LoadScene("Home Scene");
        Debug.Log("Loading Home Scene");
    }
}