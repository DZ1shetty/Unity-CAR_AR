using UnityEngine;

public class SimpleSpinner : MonoBehaviour
{
    [Header("Spin Settings")]
    public float spinSpeed = 360f; // degrees per second
    
    void Update()
    {
        transform.Rotate(0, 0, spinSpeed * Time.deltaTime);
    }
}