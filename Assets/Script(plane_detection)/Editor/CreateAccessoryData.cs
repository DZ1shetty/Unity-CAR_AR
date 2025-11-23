using UnityEngine;
using UnityEditor;
using CarAccessories; // Add the correct namespace here

public static class CreateAccessoryData 
{
    [MenuItem("Assets/Create/ScriptableObjects/AccessoryData")]
    public static void CreateAsset()
    {
        AccessoryData asset = ScriptableObject.CreateInstance<AccessoryData>();
        
        // Rest of the code remains the same
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(path))
        {
            path = "Assets";
        }
        else if (!System.IO.Directory.Exists(path))
        {
            path = System.IO.Path.GetDirectoryName(path);
        }
        
        string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/New AccessoryData.asset");
        AssetDatabase.CreateAsset(asset, assetPathAndName);
        AssetDatabase.SaveAssets();
        
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }
}