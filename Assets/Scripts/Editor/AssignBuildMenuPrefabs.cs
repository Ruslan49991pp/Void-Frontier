using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor-скрипт для автоматического назначения префабов в BuildMenuPopulator
/// </summary>
public class AssignBuildMenuPrefabs : Editor
{
    [MenuItem("Tools/Assign Build Menu Prefabs")]
    static void AssignPrefabs()
    {
        Debug.Log("[AssignBuildMenuPrefabs] Starting...");

        // Находим BuildMenuPopulator в сцене
        BuildMenuPopulator populator = FindObjectOfType<BuildMenuPopulator>(true);
        if (populator == null)
        {
            Debug.LogError("[AssignBuildMenuPrefabs] BuildMenuPopulator not found in scene!");
            return;
        }

        Debug.Log($"[AssignBuildMenuPrefabs] Found BuildMenuPopulator on {populator.gameObject.name}");

        // Ищем префабы через AssetDatabase
        string[] buildSlotGuids = AssetDatabase.FindAssets("BuildSlot t:Prefab");
        string[] delBuildSlotGuids = AssetDatabase.FindAssets("Del_BuildSlot t:Prefab");

        GameObject buildSlotPrefab = null;
        GameObject delBuildSlotPrefab = null;

        // Ищем BuildSlot (не Buildings папка)
        foreach (string guid in buildSlotGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("/UI/") && !path.Contains("/Buildings/"))
            {
                buildSlotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Debug.Log($"[AssignBuildMenuPrefabs] Found BuildSlot prefab at: {path}");
                break;
            }
        }

        // Ищем Del_BuildSlot (в Buildings папке)
        foreach (string guid in delBuildSlotGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("/Buildings/"))
            {
                delBuildSlotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Debug.Log($"[AssignBuildMenuPrefabs] Found Del_BuildSlot prefab at: {path}");
                break;
            }
        }

        // Назначаем префабы
        bool changed = false;

        if (buildSlotPrefab != null && populator.buildSlotPrefab != buildSlotPrefab)
        {
            populator.buildSlotPrefab = buildSlotPrefab;
            Debug.Log("[AssignBuildMenuPrefabs] Assigned BuildSlot prefab");
            changed = true;
        }
        else if (buildSlotPrefab == null)
        {
            Debug.LogError("[AssignBuildMenuPrefabs] BuildSlot prefab not found!");
        }

        if (delBuildSlotPrefab != null && populator.delBuildSlotPrefab != delBuildSlotPrefab)
        {
            populator.delBuildSlotPrefab = delBuildSlotPrefab;
            Debug.Log("[AssignBuildMenuPrefabs] Assigned Del_BuildSlot prefab");
            changed = true;
        }
        else if (delBuildSlotPrefab == null)
        {
            Debug.LogError("[AssignBuildMenuPrefabs] Del_BuildSlot prefab not found!");
        }

        if (changed)
        {
            // Сохраняем изменения в сцене
            EditorUtility.SetDirty(populator);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(populator.gameObject.scene);
            Debug.Log("[AssignBuildMenuPrefabs] Prefabs assigned successfully! Please save the scene.");
        }
        else
        {
            Debug.Log("[AssignBuildMenuPrefabs] No changes needed.");
        }
    }
}
