using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using CarAccessories; // Add this line

public class AccessoryDetailManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject detailPanel;
    public Text accessoryNameText;
    public Text manufacturerText;
    public Text modelNumberText;
    public Text priceText;
    public Text descriptionText;
    public Text featuresText;
    public Image accessoryImage;
    public Button closeButton;
    public Button purchaseButton;
    
    private AccessoryDatabase accessoryDatabase;
    private ARInteractionManager interactionManager;
    
    // Add this property to resolve the error
    private AccessoryInfo currentAccessory;
    
    // Add this public accessor
    public AccessoryInfo CurrentAccessory 
    { 
        get { return currentAccessory; }
    }
    
    private void Start()
    {
        accessoryDatabase = FindObjectOfType<AccessoryDatabase>();
        interactionManager = FindObjectOfType<ARInteractionManager>();
        
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseDetails);
        }
        
        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
        }
    }
    
    public void ShowAccessoryDetails(string accessoryID)
    {
        if (string.IsNullOrEmpty(accessoryID) || detailPanel == null)
            return;
            
        AccessoryInfo accessoryInfo = null;
        
        // Try to get from database first
        if (accessoryDatabase != null)
        {
            accessoryInfo = accessoryDatabase.GetAccessoryByID(accessoryID);
        }
        
        // If not found in database, try from interaction manager
        if (accessoryInfo == null && interactionManager != null && interactionManager.accessoryDatabase != null)
        {
            accessoryInfo = interactionManager.accessoryDatabase.GetAccessoryByID(accessoryID);
        }
        
        // If we have accessory info, display it
        if (accessoryInfo != null)
        {
            currentAccessory = accessoryInfo;  // Store the current accessory
            PopulateUI(accessoryInfo);
            detailPanel.SetActive(true);
            
            // Provide haptic feedback
            Handheld.Vibrate();
        }
    }
    
    private void PopulateUI(AccessoryInfo info)
    {
        if (accessoryNameText != null) accessoryNameText.text = info.accessoryName;
        if (manufacturerText != null) manufacturerText.text = info.manufacturer;
        if (modelNumberText != null) modelNumberText.text = info.modelNumber;
        if (priceText != null) priceText.text = "$" + info.price.ToString("F2");
        if (descriptionText != null) descriptionText.text = info.description;
        
        if (featuresText != null)
        {
            string featuresList = "";
            foreach (string feature in info.features)
            {
                featuresList += "• " + feature + "\n";
            }
            featuresText.text = featuresList;
        }
        
        if (accessoryImage != null)
        {
            if (info.accessoryImage != null)
            {
                accessoryImage.sprite = info.accessoryImage;
                accessoryImage.gameObject.SetActive(true);
            }
            else
            {
                accessoryImage.gameObject.SetActive(false);
            }
        }
    }
    
    public void CloseDetails()
    {
        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
        }
    }
    
    public void PurchaseAccessory()
    {
        // Implement purchasing logic here
        Debug.Log("Purchase button clicked");
    }
}