using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Размещает 2 префаба в RoomBuildMenuPanel: Wall_Int_Slot и Door_Slot
/// ВАЖНО: Этот скрипт должен быть прикреплен ТОЛЬКО к RoomBuildMenuPanel!
/// </summary>
public class RoomBuildMenuPopulator : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("Префаб Wall_Int_Slot")]
    public GameObject wallIntSlotPrefab;

    [Tooltip("Префаб Door_Slot")]
    public GameObject doorSlotPrefab;

    private GameObject wallIntSlotInstance;
    private GameObject doorSlotInstance;
    private bool isInitialized = false;

    void Start()
    {
        if (gameObject.name != "RoomBuildMenuPanel")
        {
            Debug.LogWarning($"[RoomBuildMenuPopulator] This script should ONLY be attached to RoomBuildMenuPanel! Currently attached to: {gameObject.name}. Skipping initialization.");
            return;
        }

        Transform contentTransform = transform.Find("ObjectsGrid/Viewport/Content");
        if (contentTransform == null)
        {
            Debug.LogError("[RoomBuildMenuPopulator] Content not found at ObjectsGrid/Viewport/Content!");
            return;
        }

        #if UNITY_EDITOR
        if (wallIntSlotPrefab == null)
        {
            wallIntSlotPrefab = LoadPrefabByName("Wall_Int_Slot", "Assets/Prefabs/UI/Interior/");
        }

        if (doorSlotPrefab == null)
        {
            doorSlotPrefab = LoadPrefabByName("Door_Slot", "Assets/Prefabs/UI/Interior/");
        }
        #endif

        if (wallIntSlotPrefab == null)
        {
            Debug.LogError("[RoomBuildMenuPopulator] Wall_Int_Slot prefab not assigned and could not be found!");
            return;
        }

        if (doorSlotPrefab == null)
        {
            Debug.LogError("[RoomBuildMenuPopulator] Door_Slot prefab not assigned and could not be found!");
            return;
        }

        wallIntSlotInstance = Instantiate(wallIntSlotPrefab, contentTransform);
        wallIntSlotInstance.name = "Wall_Int_Slot";

        Button wallIntSlotButton = wallIntSlotInstance.GetComponentInChildren<Button>(true);
        if (wallIntSlotButton != null)
        {
            wallIntSlotButton.onClick.AddListener(OnWallIntSlotClicked);
        }
        else
        {
            Debug.LogError("[RoomBuildMenuPopulator] Wall_Int_Slot prefab does not have a Button component anywhere in hierarchy!");
        }

        doorSlotInstance = Instantiate(doorSlotPrefab, contentTransform);
        doorSlotInstance.name = "Door_Slot";

        Button doorSlotButton = doorSlotInstance.GetComponentInChildren<Button>(true);
        if (doorSlotButton != null)
        {
            doorSlotButton.onClick.AddListener(OnDoorSlotClicked);
        }
        else
        {
            Debug.LogError("[RoomBuildMenuPopulator] Door_Slot prefab does not have a Button component anywhere in hierarchy!");
        }

        isInitialized = true;
    }

    void OnWallIntSlotClicked()
    {
        Debug.Log("[RoomBuildMenuPopulator] Wall_Int_Slot clicked!");
        // TODO: Добавить логику для строительства внутренних стен
    }

    void OnDoorSlotClicked()
    {
        Debug.Log("[RoomBuildMenuPopulator] Door_Slot clicked!");
        // TODO: Добавить логику для установки дверей
    }

    #if UNITY_EDITOR
    GameObject LoadPrefabByName(string prefabName, string searchPath)
    {
        string[] guids = AssetDatabase.FindAssets($"{prefabName} t:Prefab", new[] { searchPath });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null && prefab.name == prefabName)
            {
                return prefab;
            }
        }

        Debug.LogWarning($"[RoomBuildMenuPopulator] Prefab '{prefabName}' not found in '{searchPath}'");
        return null;
    }
    #endif
}
