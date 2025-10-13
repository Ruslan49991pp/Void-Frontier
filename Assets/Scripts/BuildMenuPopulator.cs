using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Размещает 2 префаба в BuildMenuPanel: BuildSlot и Del_BuildSlot
/// </summary>
public class BuildMenuPopulator : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("Префаб BuildSlot")]
    public GameObject buildSlotPrefab;

    [Tooltip("Префаб Del_BuildSlot")]
    public GameObject delBuildSlotPrefab;

    void Start()
    {
        Debug.Log($"[BuildMenuPopulator] Start called on {gameObject.name}");

        // Находим Content контейнер
        Transform contentTransform = transform.Find("ObjectsGrid/Viewport/Content");
        if (contentTransform == null)
        {
            Debug.LogError("[BuildMenuPopulator] Content not found at ObjectsGrid/Viewport/Content!");
            return;
        }

        // Автоматически загружаем префабы если не назначены
        #if UNITY_EDITOR
        if (buildSlotPrefab == null)
        {
            Debug.Log("[BuildMenuPopulator] BuildSlot prefab not assigned, searching...");
            buildSlotPrefab = LoadPrefabByName("BuildSlot", "Assets/Prefabs/UI/");
            if (buildSlotPrefab != null)
            {
                Debug.Log($"[BuildMenuPopulator] Found and loaded BuildSlot prefab");
            }
        }

        if (delBuildSlotPrefab == null)
        {
            Debug.Log("[BuildMenuPopulator] Del_BuildSlot prefab not assigned, searching...");
            delBuildSlotPrefab = LoadPrefabByName("Del_BuildSlot", "Assets/Prefabs/UI/Buildings/");
            if (delBuildSlotPrefab != null)
            {
                Debug.Log($"[BuildMenuPopulator] Found and loaded Del_BuildSlot prefab");
            }
        }
        #endif

        // Проверяем префабы
        if (buildSlotPrefab == null)
        {
            Debug.LogError("[BuildMenuPopulator] BuildSlot prefab not assigned and could not be found!");
            return;
        }

        if (delBuildSlotPrefab == null)
        {
            Debug.LogError("[BuildMenuPopulator] Del_BuildSlot prefab not assigned and could not be found!");
            return;
        }

        // Просто создаем 2 префаба в Content
        GameObject buildSlot = Instantiate(buildSlotPrefab, contentTransform);
        buildSlot.name = "BuildSlot";
        Debug.Log($"[BuildMenuPopulator] Created BuildSlot");

        GameObject delBuildSlot = Instantiate(delBuildSlotPrefab, contentTransform);
        delBuildSlot.name = "Del_BuildSlot";
        Debug.Log($"[BuildMenuPopulator] Created Del_BuildSlot");

        Debug.Log("[BuildMenuPopulator] Initialized successfully!");
    }

    #if UNITY_EDITOR
    /// <summary>
    /// Загружает префаб по имени из указанной директории
    /// </summary>
    GameObject LoadPrefabByName(string prefabName, string searchPath)
    {
        string[] guids = AssetDatabase.FindAssets($"{prefabName} t:Prefab", new[] { searchPath });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null && prefab.name == prefabName)
            {
                Debug.Log($"[BuildMenuPopulator] Loaded prefab from: {path}");
                return prefab;
            }
        }

        Debug.LogWarning($"[BuildMenuPopulator] Prefab '{prefabName}' not found in '{searchPath}'");
        return null;
    }
    #endif
}
