using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public string sceneToLoad;

    public void LoadScene()
    {
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError("Scene name not specified!");
            return;
        }

        SceneManager.LoadScene(sceneToLoad);
    }
}