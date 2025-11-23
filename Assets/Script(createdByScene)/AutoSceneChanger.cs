using UnityEngine;
using UnityEngine.SceneManagement;

public class AutoSceneChanger : MonoBehaviour
{
    public float delay = 5f; // Time in seconds before changing scene
    public string nextSceneName; // Name of the scene to load

    void Start()
    {
        Invoke("ChangeScene", delay);
    }

    void ChangeScene()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}