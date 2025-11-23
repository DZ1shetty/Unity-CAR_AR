using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CloseButtonHandler : MonoBehaviour
{
    void Start()
    {
        Button closeButton = GetComponent<Button>();
        closeButton.onClick.AddListener(OnCloseButtonClicked);
    }
    
    void OnCloseButtonClicked()
    {
        // Simply load the flashcards scene
        // The FlashcardManager will handle showing the correct accessory
        SceneManager.LoadScene("AccessoryFlashcards");
    }
}