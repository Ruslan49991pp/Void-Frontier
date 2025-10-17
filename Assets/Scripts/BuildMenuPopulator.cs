using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Размещает 2 префаба в ShipBuildMenuPanel: BuildSlot и Del_BuildSlot
/// Добавляет кнопку AddBuild для подтверждения постройки
/// ВАЖНО: Этот скрипт должен быть прикреплен ТОЛЬКО к ShipBuildMenuPanel!
/// Для RoomBuildMenuPanel используется отдельная логика с другими кнопками.
/// </summary>
public class BuildMenuPopulator : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("Префаб BuildSlot")]
    public GameObject buildSlotPrefab;

    [Tooltip("Префаб Del_BuildSlot")]
    public GameObject delBuildSlotPrefab;

    [Header("References")]
    [Tooltip("RoomDragBuilder компонент")]
    public RoomDragBuilder roomDragBuilder;

    [Header("UI")]
    [Tooltip("Кнопка AddBuild для подтверждения постройки")]
    public Button addBuildButton;

    private GameObject buildSlotInstance;
    private GameObject delBuildSlotInstance;
    private bool isInitialized = false;

    void Awake()
    {
        if (roomDragBuilder == null)
        {
            roomDragBuilder = FindObjectOfType<RoomDragBuilder>();
            if (roomDragBuilder == null)
            {
                GameObject rbObj = new GameObject("RoomDragBuilder");
                roomDragBuilder = rbObj.AddComponent<RoomDragBuilder>();
            }
        }
    }

    void Start()
    {
        if (gameObject.name != "ShipBuildMenuPanel")
        {
            Debug.LogWarning($"[BuildMenuPopulator] This script should ONLY be attached to ShipBuildMenuPanel! Currently attached to: {gameObject.name}. Skipping initialization.");
            return;
        }

        Transform contentTransform = transform.Find("ObjectsGrid/Viewport/Content");
        if (contentTransform == null)
        {
            Debug.LogError("[BuildMenuPopulator] Content not found at ObjectsGrid/Viewport/Content!");
            return;
        }

        #if UNITY_EDITOR
        if (buildSlotPrefab == null)
        {
            buildSlotPrefab = LoadPrefabByName("BuildSlot", "Assets/Prefabs/UI/");
        }

        if (delBuildSlotPrefab == null)
        {
            delBuildSlotPrefab = LoadPrefabByName("Del_BuildSlot", "Assets/Prefabs/UI/Buildings/");
        }
        #endif

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

        buildSlotInstance = Instantiate(buildSlotPrefab, contentTransform);
        buildSlotInstance.name = "BuildSlot";

        Button buildSlotButton = buildSlotInstance.GetComponentInChildren<Button>(true);
        if (buildSlotButton != null)
        {
            buildSlotButton.onClick.AddListener(OnBuildSlotClicked);
        }
        else
        {
            Debug.LogError("[BuildMenuPopulator] BuildSlot prefab does not have a Button component anywhere in hierarchy!");
        }

        delBuildSlotInstance = Instantiate(delBuildSlotPrefab, contentTransform);
        delBuildSlotInstance.name = "Del_BuildSlot";

        Button delBuildSlotButton = delBuildSlotInstance.GetComponentInChildren<Button>(true);
        if (delBuildSlotButton != null)
        {
            delBuildSlotButton.onClick.AddListener(OnDelBuildSlotClicked);
        }
        else
        {
            Debug.LogError("[BuildMenuPopulator] Del_BuildSlot prefab does not have a Button component anywhere in hierarchy!");
        }

        SetupAddBuildButton();
        isInitialized = true;
    }

    void SetupAddBuildButton()
    {
        if (addBuildButton == null)
        {
            GameObject canvasMainUI = GameObject.Find("Canvas_MainUI");
            if (canvasMainUI != null)
            {
                Button[] allButtons = canvasMainUI.GetComponentsInChildren<Button>(true);
                foreach (Button btn in allButtons)
                {
                    if (btn.gameObject.name == "AddBuild")
                    {
                        addBuildButton = btn;
                        break;
                    }
                }
            }

            if (addBuildButton == null)
            {
                Debug.LogWarning("[BuildMenuPopulator] AddBuild button not found in Canvas_MainUI");
            }
        }

        if (addBuildButton != null)
        {
            addBuildButton.onClick.AddListener(OnAddBuildClicked);
            addBuildButton.gameObject.SetActive(true);
            addBuildButton.interactable = false;
        }
    }

    string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }

    void OnBuildSlotClicked()
    {
        if (roomDragBuilder == null)
        {
            Debug.LogError("[BuildMenuPopulator] RoomDragBuilder is null!");
            return;
        }

        roomDragBuilder.ActivateDragMode();
    }

    void OnAddBuildClicked()
    {
        // Теперь вызываем ConfirmBuild() вместо FinalizeBuild()
        // ConfirmBuild() применяет материал M_Add_Build_Ghost и запускает систему строительства персонажами
        roomDragBuilder.ConfirmBuild();
    }

    void OnDelBuildSlotClicked()
    {
        if (roomDragBuilder == null)
        {
            Debug.LogError("[BuildMenuPopulator] RoomDragBuilder is null!");
            return;
        }

        if (!roomDragBuilder.IsDeletionModeActive())
        {
            roomDragBuilder.DeactivateDragMode();
        }

        roomDragBuilder.ActivateDeletionMode();
    }

    void Update()
    {
        if (!isInitialized) return;

        if (addBuildButton != null && roomDragBuilder != null)
        {
            bool shouldBeInteractable = roomDragBuilder.CanConfirmBuild();
            if (addBuildButton.interactable != shouldBeInteractable)
            {
                addBuildButton.interactable = shouldBeInteractable;
            }
        }
    }

    void OnDisable()
    {
        if (!isInitialized) return;

        if (roomDragBuilder != null)
        {
            roomDragBuilder.DeactivateDragMode();
            roomDragBuilder.DeactivateDeletionMode();
        }
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

        Debug.LogWarning($"[BuildMenuPopulator] Prefab '{prefabName}' not found in '{searchPath}'");
        return null;
    }
    #endif
}
