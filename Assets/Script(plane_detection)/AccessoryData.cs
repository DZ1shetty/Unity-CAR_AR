using UnityEngine;

namespace CarAccessories
{
    [CreateAssetMenu(fileName = "New AccessoryData", menuName = "Car Accessories/Accessory Data")]
    public class AccessoryData : ScriptableObject
    {
        public string id;
        public string accessoryName;
        public string description;
        public Sprite accessoryImage;
        
        // Additional fields that match the AccessoryInfo structure
        public string manufacturer;
        public string modelNumber;
        public float price;
        public string[] features = new string[0];
        
        // This is the missing method that needs to be added
        public AccessoryInfo ToAccessoryInfo()
        {
            return new AccessoryInfo
            {
                id = this.id,
                accessoryName = this.accessoryName,
                description = this.description,
                accessoryImage = this.accessoryImage,
                manufacturer = this.manufacturer,
                modelNumber = this.modelNumber,
                price = this.price,
                features = this.features
            };
        }
    }
}