using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Размещает 2 префаба в BuildMenuPanel: BuildSlot и Del_BuildSlot
/// Добавляет кнопку AddBuild для подтверждения постройки
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

    void Awake()
    {
        // Находим или создаем RoomDragBuilder
        if (roomDragBuilder == null)
        {
            roomDragBuilder = FindObjectOfType<RoomDragBuilder>();
            if (roomDragBuilder == null)
            {
                GameObject rbObj = new GameObject("RoomDragBuilder");
                roomDragBuilder = rbObj.AddComponent<RoomDragBuilder>();
                Debug.Log("[BuildMenuPopulator] Created RoomDragBuilder");
            }
        }
    }

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
        buildSlotInstance = Instantiate(buildSlotPrefab, contentTransform);
        buildSlotInstance.name = "BuildSlot";
        Debug.Log($"[BuildMenuPopulator] Created BuildSlot");

        // Добавляем onClick для BuildSlot - ищем Button везде в иерархии
        Button buildSlotButton = buildSlotInstance.GetComponentInChildren<Button>(true); // includeInactive = true
        if (buildSlotButton != null)
        {
            buildSlotButton.onClick.AddListener(OnBuildSlotClicked);
            Debug.Log($"[BuildMenuPopulator] BuildSlot button listener added to: {buildSlotButton.gameObject.name}");
        }
        else
        {
            Debug.LogError("[BuildMenuPopulator] BuildSlot prefab does not have a Button component anywhere in hierarchy!");
        }

        delBuildSlotInstance = Instantiate(delBuildSlotPrefab, contentTransform);
        delBuildSlotInstance.name = "Del_BuildSlot";
        Debug.Log($"[BuildMenuPopulator] Created Del_BuildSlot");

        // Добавляем onClick для Del_BuildSlot - ищем Button везде в иерархии
        Button delBuildSlotButton = delBuildSlotInstance.GetComponentInChildren<Button>(true); // includeInactive = true
        if (delBuildSlotButton != null)
        {
            delBuildSlotButton.onClick.AddListener(OnDelBuildSlotClicked);
            Debug.Log($"[BuildMenuPopulator] Del_BuildSlot button listener added to: {delBuildSlotButton.gameObject.name}");
        }
        else
        {
            Debug.LogError("[BuildMenuPopulator] Del_BuildSlot prefab does not have a Button component anywhere in hierarchy!");
        }

        // Ищем или создаем кнопку AddBuild
        SetupAddBuildButton();

        Debug.Log("[BuildMenuPopulator] Initialized successfully!");
    }

    /// <summary>
    /// Настроить кнопку AddBuild
    /// </summary>
    void SetupAddBuildButton()
    {
        if (addBuildButton == null)
        {
            // Ищем кнопку AddBuild в Canvas_MainUI
            GameObject canvasMainUI = GameObject.Find("Canvas_MainUI");
            if (canvasMainUI != null)
            {
                // Ищем кнопку по имени во всей иерархии Canvas_MainUI
                Button[] allButtons = canvasMainUI.GetComponentsInChildren<Button>(true);
                foreach (Button btn in allButtons)
                {
                    if (btn.gameObject.name == "AddBuild")
                    {
                        addBuildButton = btn;
                        Debug.Log($"[BuildMenuPopulator] Found AddBuild button at path: {GetGameObjectPath(btn.gameObject)}");
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
            addBuildButton.gameObject.SetActive(true); // Всегда видна
            addBuildButton.interactable = false; // Но неактивна до готовности
            Debug.Log("[BuildMenuPopulator] AddBuild button configured");
        }
    }

    /// <summary>
    /// Получить полный путь к GameObject в иерархии
    /// </summary>
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

    /// <summary>
    /// Обработчик клика по BuildSlot - активировать drag режим
    /// </summary>
    void OnBuildSlotClicked()
    {
        Debug.Log("[BuildMenuPopulator] BuildSlot clicked - activating drag mode");

        if (roomDragBuilder == null)
        {
            Debug.LogError("[BuildMenuPopulator] RoomDragBuilder is null!");
            return;
        }

        roomDragBuilder.ActivateDragMode();
        Debug.Log("[BuildMenuPopulator] Drag mode activation requested");
    }

    /// <summary>
    /// Обработчик клика по AddBuild - подтвердить постройку
    /// </summary>
    void OnAddBuildClicked()
    {
        Debug.Log("[BuildMenuPopulator] AddBuild clicked - confirming build");
        roomDragBuilder.ConfirmBuild();

        // Делаем кнопку неактивной после подтверждения
        if (addBuildButton != null)
        {
            addBuildButton.interactable = false;
        }
    }

    /// <summary>
    /// Обработчик клика по Del_BuildSlot - активировать/деактивировать режим удаления
    /// </summary>
    void OnDelBuildSlotClicked()
    {
        Debug.Log("[BuildMenuPopulator] Del_BuildSlot clicked");

        if (roomDragBuilder == null)
        {
            Debug.LogError("[BuildMenuPopulator] RoomDragBuilder is null!");
            return;
        }

        // Проверяем, можем ли мы активировать режим удаления
        // Режим удаления работает только когда есть preview или confirmed
        if (!roomDragBuilder.IsReadyToConfirm() && !roomDragBuilder.IsConfirmed())
        {
            Debug.LogWarning("[BuildMenuPopulator] Cannot activate deletion mode - no room preview available");
            return;
        }

        // Переключаем режим удаления
        roomDragBuilder.ActivateDeletionMode();
        Debug.Log("[BuildMenuPopulator] Deletion mode activated - click on cells to delete them");
    }

    void Update()
    {
        // Делаем кнопку AddBuild активной/неактивной в зависимости от состояния
        if (addBuildButton != null)
        {
            bool shouldBeInteractable = roomDragBuilder != null && roomDragBuilder.IsReadyToConfirm();
            if (addBuildButton.interactable != shouldBeInteractable)
            {
                addBuildButton.interactable = shouldBeInteractable;
            }
        }
    }

    /// <summary>
    /// Вызывается при закрытии BuildMenuPanel - финализировать постройку
    /// </summary>
    void OnDisable()
    {
        if (roomDragBuilder != null && roomDragBuilder.IsConfirmed())
        {
            Debug.Log("[BuildMenuPopulator] Panel closing - finalizing build");
            roomDragBuilder.FinalizeBuild();
        }

        // Деактивируем drag режим при закрытии панели
        if (roomDragBuilder != null)
        {
            roomDragBuilder.DeactivateDragMode();
        }
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
