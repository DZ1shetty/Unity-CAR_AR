using UnityEngine;

/// <summary>
/// Interface for AR interactive objects
/// </summary>
public interface IARInteractable
{
    void OnInteract();
    void OnDeselect();
}