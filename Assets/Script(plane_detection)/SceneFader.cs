using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneFader : MonoBehaviour
{
    public Image fadeImage; // Drag your Image UI element here in the Inspector
    public float fadeSpeed = 0.8f;

    public static SceneFader Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // This is crucial for the fader to persist across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // On start, fade out (from black to transparent)
        StartCoroutine(Fade(1, 0));
    }

    public void FadeToScene(string sceneName)
    {
        // Start the fade-in and then load the new scene
        StartCoroutine(FadeAndLoadScene(sceneName));
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float timer = 0f;
        while (timer < fadeSpeed)
        {
            timer += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, endAlpha, timer / fadeSpeed);
            fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, newAlpha);
            yield return null;
        }
        fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, endAlpha);
    }

    private IEnumerator FadeAndLoadScene(string sceneName)
    {
        // Fade in to black
        yield return StartCoroutine(Fade(0, 1)); 

        // Load the new scene
        SceneManager.LoadScene(sceneName);

        // Once the new scene is loaded, fade out from black
        yield return StartCoroutine(Fade(1, 0));
    }
}