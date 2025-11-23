using UnityEngine;

/// <summary>
/// This script makes the GameObject it is attached to always face the main camera.
/// It's essential for World Space UI in AR to ensure readability from any angle.
/// </summary>
public class FaceCamera : MonoBehaviour
{
    private Transform cameraTransform;

    private void Start()
    {
        // Find and cache the main camera's transform for efficiency.
        // Camera.main is convenient but relies on the camera having the "MainCamera" tag.
        if (Camera.main!= null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    // LateUpdate is called after all Update functions have been called.
    // This is the best place to a-dd camera-following logic to avoid jitter.
    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        // Make this object look at the camera's position.
        transform.LookAt(cameraTransform.position);

        // By default, LookAt also matches the camera's tilt. For UI, we often want it
        // to remain upright. We can achieve this by zeroing out the rotation on the X and Z axes.
        transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
    }
}