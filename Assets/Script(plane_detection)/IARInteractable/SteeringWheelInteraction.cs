using UnityEngine;

public class SteeringWheelInteraction : MonoBehaviour, IARInteractable
{
    [Header("Steering Settings")]
    [SerializeField] private float rotationSpeed = 5.0f;
    [SerializeField] private float maxRotationAngle = 45.0f;
    [SerializeField] private Transform steeringTransform; // The actual wheel mesh to rotate

    [Header("Visual Feedback")]
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Material selectedMaterial;
    [SerializeField] private Renderer wheelRenderer;

    // Interaction state
    private bool isSelected = false;
    private Vector2 lastTouchPos;
    
    private void Awake()
    {
        // Auto-assign if not set
        if (steeringTransform == null)
            steeringTransform = transform;
            
        if (wheelRenderer == null)
            wheelRenderer = GetComponentInChildren<Renderer>();
    }

    public void OnInteract()
    {
        isSelected = true;
        
        // Visual feedback
        if (wheelRenderer != null && selectedMaterial != null)
            wheelRenderer.material = selectedMaterial;
            
        // Store initial touch position
        if (Input.touchCount > 0)
            lastTouchPos = Input.GetTouch(0).position;
        else if (Input.GetMouseButton(0))
            lastTouchPos = Input.mousePosition;
            
        Debug.Log("Steering wheel selected");
    }

    public void OnDeselect()
    {
        isSelected = false;
        
        // Visual feedback
        if (wheelRenderer != null && defaultMaterial != null)
            wheelRenderer.material = defaultMaterial;
            
        Debug.Log("Steering wheel deselected");
    }

    private void Update()
    {
        if (!isSelected)
            return;
            
        // Handle rotation based on touch/drag
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            HandleRotation(touch.position);
            lastTouchPos = touch.position;
        }
        else if (Input.GetMouseButton(0))
        {
            HandleRotation(Input.mousePosition);
            lastTouchPos = Input.mousePosition;
        }
    }

    private void HandleRotation(Vector2 currentPos)
    {
        // Calculate drag direction
        Vector2 deltaPos = currentPos - lastTouchPos;
        
        // Rotate wheel based on horizontal movement
        float rotationAmount = -deltaPos.x * rotationSpeed * Time.deltaTime;
        
        // Get current rotation
        Vector3 currentRotation = steeringTransform.localEulerAngles;
        
        // Convert current y angle to -180 to 180 range
        float currentYAngle = currentRotation.y;
        if (currentYAngle > 180)
            currentYAngle -= 360;
            
        // Calculate new rotation with constraints
        float newYAngle = Mathf.Clamp(currentYAngle + rotationAmount, -maxRotationAngle, maxRotationAngle);
        
        // Apply the new rotation
        steeringTransform.localEulerAngles = new Vector3(
            currentRotation.x,
            newYAngle,
            currentRotation.z
        );
    }
}