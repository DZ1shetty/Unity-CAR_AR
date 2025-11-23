using UnityEngine;

[CreateAssetMenu(fileName = "New Accessory", menuName = "Accessory Data")]
public class AccessoryItem : ScriptableObject
{
    public string accessoryName;
    public string manufacturer;
    public float price;
    public string description;
    [TextArea(3, 5)]
    public string features;
    public Sprite accessoryImage;
}