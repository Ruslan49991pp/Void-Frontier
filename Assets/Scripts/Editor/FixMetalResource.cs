using UnityEngine;
using UnityEditor;

/// <summary>
/// Быстрое исправление ресурса Металл
/// </summary>
public class FixMetalResource
{
    [MenuItem("Tools/Resources/Fix Metal Resource (Remove Prefab)")]
    public static void RemovePrefabFromMetal()
    {
        ResourceManager resourceManager = Resources.Load<ResourceManager>("ResourceManager");
        if (resourceManager == null)
        {
            EditorUtility.DisplayDialog("Error", "ResourceManager not found!", "OK");
            return;
        }

        ResourceData metalData = resourceManager.GetResourceByName("Металл");
        if (metalData == null)
        {
            EditorUtility.DisplayDialog("Error", "Metal resource not found!", "OK");
            return;
        }

        // Убираем префаб чтобы использовался fallback куб
        metalData.prefab = null;
        EditorUtility.SetDirty(metalData);
        AssetDatabase.SaveAssets();

        Debug.Log("✓ Removed prefab from Metal resource - will use fallback cube");
        EditorUtility.DisplayDialog("Success",
            "Prefab removed from Metal resource!\n\nNow metal will appear as metallic gray-blue cubes.",
            "OK");
    }
}
