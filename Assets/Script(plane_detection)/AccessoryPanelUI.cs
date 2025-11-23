using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Sits on the AccessoryInfoPanel prefab. Holds direct references to its UI elements
/// for efficient access by the UIManager.
/// </summary>
public class AccessoryPanelUI : MonoBehaviour
{
   
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI manufacturerText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI featuresText;
    public TextMeshProUGUI modelNumberText;
    public TextMeshProUGUI priceText;
    public Button closeButton;
}