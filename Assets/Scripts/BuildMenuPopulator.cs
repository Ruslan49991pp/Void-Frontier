using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Заполняет BuildMenuPanel слотами строительства из ShipBuildingSystem
/// </summary>
public class BuildMenuPopulator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Префаб слота строительства")]
    public GameObject buildSlotPrefab;

    [Tooltip("Content контейнер для слотов")]
    public RectTransform contentContainer;

    private ShipBuildingSystem buildingSystem;
    private List<GameObject> createdSlots = new List<GameObject>();

    private bool isInitialized = false;

    void Awake()
    {
        Debug.Log($"[BuildMenuPopulator] Awake called on {gameObject.name}");
    }

    void Start()
    {
        Debug.Log($"[BuildMenuPopulator] Start called on {gameObject.name}");
        Initialize();
    }

    void OnEnable()
    {
        Debug.Log($"[BuildMenuPopulator] OnEnable called on {gameObject.name}");

        // Если уже инициализированы, просто обновляем слоты
        if (isInitialized && buildingSystem != null && contentContainer != null && buildSlotPrefab != null)
        {
            PopulateSlots();
        }
    }

    void Initialize()
    {
        if (isInitialized)
        {
            Debug.Log("[BuildMenuPopulator] Already initialized, skipping");
            return;
        }

        Debug.Log("[BuildMenuPopulator] Initializing...");

        // Находим ShipBuildingSystem в сцене
        buildingSystem = FindObjectOfType<ShipBuildingSystem>();
        if (buildingSystem == null)
        {
            Debug.LogError("[BuildMenuPopulator] ShipBuildingSystem not found in scene! Please create a GameObject with ShipBuildingSystem component manually.");
            return;
        }

        Debug.Log("[BuildMenuPopulator] ShipBuildingSystem found in scene");

        // Находим Content контейнер если не назначен
        if (contentContainer == null)
        {
            contentContainer = FindContentContainer();
        }

        if (contentContainer == null)
        {
            Debug.LogError("[BuildMenuPopulator] Content container not found!");
            return;
        }

        // Проверяем префаб
        if (buildSlotPrefab == null)
        {
            Debug.LogError("[BuildMenuPopulator] BuildSlot prefab not assigned!");
            return;
        }

        isInitialized = true;

        // Заполняем слотами
        PopulateSlots();

        Debug.Log("[BuildMenuPopulator] Initialized successfully!");
    }

    /// <summary>
    /// Найти Content контейнер
    /// </summary>
    RectTransform FindContentContainer()
    {
        // Ищем BuildMenuPanel
        GameObject buildMenuPanel = GameObject.Find("BuildMenuPanel");
        if (buildMenuPanel == null)
        {
            Debug.LogError("[BuildMenuPopulator] BuildMenuPanel not found!");
            return null;
        }

        // Ищем Content внутри BuildMenuPanel
        Transform contentTransform = buildMenuPanel.transform.Find("ObjectsGrid/Viewport/Content");
        if (contentTransform == null)
        {
            // Пробуем альтернативные пути
            contentTransform = buildMenuPanel.transform.Find("Scroll View/Viewport/Content");
            if (contentTransform == null)
            {
                contentTransform = buildMenuPanel.transform.Find("Content");
                if (contentTransform == null)
                {
                    contentTransform = FindChildRecursive(buildMenuPanel.transform, "Content");
                }
            }
        }

        if (contentTransform != null)
        {
            Debug.Log($"[BuildMenuPopulator] Found Content at: {GetFullPath(contentTransform)}");
            return contentTransform.GetComponent<RectTransform>();
        }

        Debug.LogError("[BuildMenuPopulator] Content not found in BuildMenuPanel!");
        return null;
    }

    /// <summary>
    /// Рекурсивный поиск дочернего объекта по имени
    /// </summary>
    Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            Transform found = FindChildRecursive(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    /// <summary>
    /// Получить полный путь трансформа
    /// </summary>
    string GetFullPath(Transform transform)
    {
        string path = transform.name;
        Transform current = transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }

    /// <summary>
    /// Заполнить Content слотами
    /// </summary>
    void PopulateSlots()
    {
        // Очищаем старые слоты если есть
        ClearSlots();

        // Получаем список доступных комнат
        List<RoomData> availableRooms = buildingSystem.GetAvailableRooms();

        if (availableRooms == null || availableRooms.Count == 0)
        {
            Debug.LogWarning("[BuildMenuPopulator] No available rooms found in ShipBuildingSystem! Make sure availableRooms list is populated.");
            return;
        }

        Debug.Log($"[BuildMenuPopulator] Found {availableRooms.Count} available rooms");

        // Создаем слот для каждой комнаты
        for (int i = 0; i < availableRooms.Count; i++)
        {
            RoomData roomData = availableRooms[i];
            if (roomData != null)
            {
                CreateSlot(i, roomData);
            }
            else
            {
                Debug.LogWarning($"[BuildMenuPopulator] Room at index {i} is null, skipping");
            }
        }

        Debug.Log($"[BuildMenuPopulator] Created {createdSlots.Count} build slots");
    }

    /// <summary>
    /// Создать один слот
    /// </summary>
    void CreateSlot(int index, RoomData roomData)
    {
        GameObject slotObj = Instantiate(buildSlotPrefab, contentContainer);
        slotObj.name = $"BuildSlot_{roomData.roomName}";

        BuildSlotUI slotUI = slotObj.GetComponent<BuildSlotUI>();
        if (slotUI != null)
        {
            slotUI.SetData(index, roomData);
        }
        else
        {
            Debug.LogWarning($"[BuildMenuPopulator] BuildSlotUI component not found on slot {slotObj.name}");
        }

        createdSlots.Add(slotObj);
        Debug.Log($"[BuildMenuPopulator] Created slot: {roomData.roomName} (index: {index})");
    }

    /// <summary>
    /// Очистить все слоты
    /// </summary>
    void ClearSlots()
    {
        foreach (GameObject slot in createdSlots)
        {
            if (slot != null)
            {
                Destroy(slot);
            }
        }
        createdSlots.Clear();
    }

    void OnDestroy()
    {
        ClearSlots();
    }
}
