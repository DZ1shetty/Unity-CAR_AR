using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class SmoothLoadingManager : MonoBehaviour
{
    [Header("UI Components")]
    public Canvas loadingCanvas;
    public Image fadeImage;
    public TextMeshProUGUI loadingText;
    
    [Header("Settings")]
    public string sceneToLoad = "";
    public float transitionSpeed = 2f;
    public float minimumLoadTime = 2f;
    
    private bool isTransitioning = false;
    
    void Start()
    {
        // Setup initial state
        if (loadingCanvas != null)
        {
            loadingCanvas.gameObject.SetActive(false);
        }
        
        // Make sure fade image is transparent at start
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }
    }
    
    public void StartSceneTransition()
    {
        if (!isTransitioning)
        {
            StartCoroutine(TransitionToScene());
        }
    }
    
    IEnumerator TransitionToScene()
    {
        isTransitioning = true;
        
        // 1. Show loading canvas
        loadingCanvas.gameObject.SetActive(true);
        
        // 2. Fade to black
        yield return StartCoroutine(FadeIn());
        
        // 3. Start loading text animation
        StartCoroutine(AnimateText());
        
        // 4. Wait minimum time
        yield return new WaitForSeconds(minimumLoadTime);
        
        // 5. Load scene if specified
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            // 6. Fade back if no scene change
            yield return StartCoroutine(FadeOut());
            loadingCanvas.gameObject.SetActive(false);
            isTransitioning = false;
        }
    }
    
    IEnumerator FadeIn()
    {
        float timer = 0f;
        Color c = fadeImage.color;
        
        while (timer < 1f)
        {
            timer += Time.deltaTime * transitionSpeed;
            c.a = Mathf.Lerp(0f, 1f, timer);
            fadeImage.color = c;
            yield return null;
        }
        
        c.a = 1f;
        fadeImage.color = c;
    }
    
    IEnumerator FadeOut()
    {
        float timer = 0f;
        Color c = fadeImage.color;
        
        while (timer < 1f)
        {
            timer += Time.deltaTime * transitionSpeed;
            c.a = Mathf.Lerp(1f, 0f, timer);
            fadeImage.color = c;
            yield return null;
        }
        
        c.a = 0f;
        fadeImage.color = c;
    }
    
    IEnumerator AnimateText()
    {
        string baseText = "Loading";
        int dotCount = 0;
        
        while (isTransitioning)
        {
            dotCount = (dotCount + 1) % 4;
            string dots = new string('.', dotCount);
            loadingText.text = baseText + dots;
            yield return new WaitForSeconds(0.5f);
        }
    }
}