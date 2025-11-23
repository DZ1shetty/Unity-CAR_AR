using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResetManager : MonoBehaviour
{
    public Button resetButton; // (Optional) Assign your UI Button in the Inspector

    private bool isResetting = false;

    public void ResetScene()
    {
        if (!isResetting)
            StartCoroutine(ReloadSceneRoutine());
    }

    private System.Collections.IEnumerator ReloadSceneRoutine()
    {
        isResetting = true;
        if (resetButton) resetButton.interactable = false;

        // Optional: Wait a short moment to let UI feedback (button highlight) show
        yield return new WaitForSeconds(0.1f);

        // Reload the current active scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        
        // No further code is needed: all objects, AR session, planes, and content will be destroyed and re-initialized
    }
}