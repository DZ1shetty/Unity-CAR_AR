using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using CarAccessories;

public class AccessoryInfoPanelManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject detailPanel;
    public Text accessoryNameText;
    public Text manufacturerText;
    public Text modelNumberText;
    public Text priceText;
    public Text descriptionText;
    public Text featuresText;
    public Image accessoryImage;
    public Button closeDetailButton;
    public Button installButton;
    
    private AccessoryInfo currentAccessory;
    
    private void Start()
    {
        if (closeDetailButton != null)
            closeDetailButton.onClick.AddListener(ClosePanel);
            
        if (detailPanel != null)
            detailPanel.SetActive(false);
    }
    
    // Add this method to solve the error
    public void ShowAccessoryInfo(AccessoryInfo accessory)
    {
        DisplayAccessoryInfo(accessory);
    }
    
    // Also add this method to solve the error with the string parameter
    public void ShowAccessoryInfo(string accessoryId)
    {
        AccessoryDatabase database = FindObjectOfType<AccessoryDatabase>();
        if (database != null)
        {
            AccessoryInfo info = database.GetAccessoryByID(accessoryId);
            if (info != null)
            {
                DisplayAccessoryInfo(info);
            }
        }
    }
    
    public void DisplayAccessoryInfo(AccessoryInfo accessory)
    {
        if (accessory == null || detailPanel == null) return;
        
        currentAccessory = accessory;
        
        if (accessoryNameText != null) accessoryNameText.text = accessory.accessoryName;
        if (manufacturerText != null) manufacturerText.text = accessory.manufacturer;
        if (modelNumberText != null) modelNumberText.text = accessory.modelNumber;
        if (priceText != null) priceText.text = "$" + accessory.price.ToString();
        if (descriptionText != null) descriptionText.text = accessory.description;
        
        if (featuresText != null)
        {
            string featuresList = "";
            foreach (string feature in accessory.features)
            {
                featuresList += "• " + feature + "\n";
            }
            featuresText.text = featuresList;
        }
        
        if (accessoryImage != null && accessory.accessoryImage != null)
        {
            accessoryImage.sprite = accessory.accessoryImage;
            accessoryImage.gameObject.SetActive(true);
        }
        else if (accessoryImage != null)
        {
            accessoryImage.gameObject.SetActive(false);
        }
        
        detailPanel.SetActive(true);
        
        // Add haptic feedback when panel opens
        Handheld.Vibrate();
    }
    
    public AccessoryInfo GetCurrentAccessory()
    {
        return currentAccessory;
    }
    
    public void ClosePanel()
    {
        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
        }
    }
}