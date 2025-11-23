using UnityEngine;

namespace CarAccessories
{
    [System.Serializable]
    public class AccessoryInfo
    {
        public string id;
        public string accessoryName;
        public string description;
        public Sprite accessoryImage;
        
        public string manufacturer;
        public string modelNumber;
        public float price;
        public string[] features;

        // Constructor with 2 arguments for the error CS1729
        public AccessoryInfo(string id, string name)
        {
            this.id = id;
            this.accessoryName = name;
        }

        // Default constructor
        public AccessoryInfo()
        {
            // Initialize with default values
            this.id = "";
            this.accessoryName = "";
            this.description = "";
            this.manufacturer = "";
            this.modelNumber = "";
            this.price = 0f;
            this.features = new string[0];
        }

        // Method for accessoryID
        public string accessoryID()
        {
            return id;
        }

        // Method for GetDisplayInfo
        public string GetDisplayInfo()
        {
            return $"{accessoryName} - {manufacturer} - {modelNumber}";
        }

        // Method for mountPoint
        public Vector3 mountPoint()
        {
            // Default implementation, might need to be customized
            return Vector3.zero;
        }
    }
}