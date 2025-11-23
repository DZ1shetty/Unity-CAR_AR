using UnityEngine;

public class FitQuadToScreen : MonoBehaviour
{
    public Camera targetCamera;
    public float distanceFromCamera = 1f;

    void Start()
    {
        float height = 2f * distanceFromCamera * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float width = height * targetCamera.aspect;
        transform.localScale = new Vector3(width, height, 1f);
        transform.position = targetCamera.transform.position + targetCamera.transform.forward * distanceFromCamera;
    }
}