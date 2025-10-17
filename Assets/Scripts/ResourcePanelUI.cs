using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// UI панель для отображения ресурсов корабля
/// Показывает ресурсы из инвентарей персонажей и лежащие на территории корабля
/// </summary>
public class ResourcePanelUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("ResourceManager со списком всех ресурсов")]
    public ResourceManager resourceManager;

    [Tooltip("Префаб для отображения одного ресурса")]
    public GameObject resourceSlotPrefab;

    [Tooltip("Родительский объект для иконок ресурсов")]
    public Transform resourceSlotsParent;

    [Header("Settings")]
    [Tooltip("Как часто обновлять панель (в секундах)")]
    public float updateInterval = 2f; // Увеличили интервал до 2 секунд

    // Внутренние переменные
    private Dictionary<string, ResourceSlotUI> resourceSlots = new Dictionary<string, ResourceSlotUI>();
    private float updateTimer;
    private GridManager gridManager;

    // Кэшируем массивы для оптимизации
    private Character[] cachedCharacters;
    private Item[] cachedItems;
    private float cacheRefreshTimer;
    private float cacheRefreshInterval = 5f; // Обновляем кэш каждые 5 секунд

    // Защита от повторной инициализации
    private bool isInitialized = false;

    void Awake()
    {
        Debug.Log($"[ResourcePanelUI] Awake called on {gameObject.name}, path: {GetGameObjectPath(gameObject)}");

        // ЗАЩИТА: Проверяем, не является ли этот объект клоном слота (рекурсия!)
        if (gameObject.name.Contains("ResourceSlot") || (gameObject.name.Contains("(Clone)") && transform.parent != null && transform.parent.name.Contains("ResourceSlot")))
        {
            Debug.LogError($"[ResourcePanelUI] RECURSION DETECTED in Awake! ResourcePanelUI on slot object: {GetGameObjectPath(gameObject)}");
            Debug.LogError("[ResourcePanelUI] Destroying this component immediately!");
            DestroyImmediate(this);
            return;
        }

        // Находим ResourceManager если не назначен
        if (resourceManager == null)
        {
            resourceManager = Resources.Load<ResourceManager>("ResourceManager");
            if (resourceManager == null)
            {
                Debug.LogError("[ResourcePanelUI] ResourceManager not found! Create it via Tools/Resources/Create Resource Manager");
            }
        }

        // Находим GridManager
        gridManager = FindObjectOfType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogWarning("[ResourcePanelUI] GridManager not found in Awake(), will try again in Start()");
        }

        // Если resourceSlotsParent не назначен, ищем его
        if (resourceSlotsParent == null)
        {
            resourceSlotsParent = transform;
        }
    }

    void Start()
    {
        // Защита от повторной инициализации
        if (isInitialized)
        {
            Debug.LogWarning($"[ResourcePanelUI] Already initialized on {gameObject.name}, skipping Start()");
            return;
        }

        // Пробуем найти GridManager еще раз если не нашли в Awake
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
            if (gridManager == null)
            {
                Debug.LogWarning("[ResourcePanelUI] GridManager still not found in Start(). Resources on ship territory won't be counted.");
            }
        }

        // НЕ создаем слоты заранее - они создаются динамически при наличии ресурсов
        RefreshCache();
        UpdateResourceDisplay();
        isInitialized = true;
    }

    void Update()
    {
        // Обновляем панель с заданным интервалом
        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            UpdateResourceDisplay();
        }

        // Обновляем кэш периодически
        cacheRefreshTimer += Time.deltaTime;
        if (cacheRefreshTimer >= cacheRefreshInterval)
        {
            cacheRefreshTimer = 0f;
            RefreshCache();
        }
    }

    /// <summary>
    /// Обновить кэш персонажей и предметов
    /// </summary>
    void RefreshCache()
    {
        cachedCharacters = FindObjectsOfType<Character>();
        cachedItems = FindObjectsOfType<Item>();
        Debug.Log($"[ResourcePanelUI] Cache refreshed: {cachedCharacters.Length} characters, {cachedItems.Length} items");
    }

    /// <summary>
    /// Инициализировать слоты для всех ресурсов
    /// </summary>
    void InitializeResourceSlots()
    {
        if (resourceManager == null || resourceSlotPrefab == null)
        {
            Debug.LogWarning("[ResourcePanelUI] Cannot initialize: ResourceManager or prefab is missing");
            return;
        }

        // ЗАЩИТА: Проверяем, что префаб не содержит ResourcePanelUI компонент
        if (resourceSlotPrefab.GetComponent<ResourcePanelUI>() != null)
        {
            Debug.LogError("[ResourcePanelUI] CRITICAL ERROR! resourceSlotPrefab contains ResourcePanelUI component!");
            Debug.LogError("[ResourcePanelUI] This will cause infinite recursion! Please assign correct ResourceSlot prefab in Unity Editor.");
            Debug.LogError($"[ResourcePanelUI] Current prefab: {resourceSlotPrefab.name}");
            return;
        }

        // ЗАЩИТА: Проверяем, не является ли этот объект клоном слота (рекурсия!)
        if (gameObject.name.Contains("ResourceSlot") || gameObject.name.Contains("(Clone)"))
        {
            Debug.LogError($"[ResourcePanelUI] RECURSION DETECTED! ResourcePanelUI on slot object: {GetGameObjectPath(gameObject)}");
            Debug.LogError("[ResourcePanelUI] This should NEVER happen! Check your prefab references in Unity Editor!");
            return;
        }

        // Очищаем существующие слоты
        ClearResourceSlots();

        // Создаем слот для каждого ресурса
        foreach (ResourceData resource in resourceManager.allResources)
        {
            CreateResourceSlot(resource);
        }

        Debug.Log($"[ResourcePanelUI] Initialized {resourceSlots.Count} resource slots");
    }

    /// <summary>
    /// Создать слот для одного ресурса
    /// </summary>
    void CreateResourceSlot(ResourceData resource)
    {
        if (resource == null || resourceSlotPrefab == null || resourceSlotsParent == null)
            return;

        // Создаем экземпляр префаба
        GameObject slotObj = Instantiate(resourceSlotPrefab, resourceSlotsParent);
        slotObj.name = $"ResourceSlot_{resource.resourceName}";
        slotObj.SetActive(true); // Всегда активный

        // Получаем компонент ResourceSlotUI
        ResourceSlotUI slotUI = slotObj.GetComponent<ResourceSlotUI>();
        if (slotUI == null)
        {
            slotUI = slotObj.AddComponent<ResourceSlotUI>();
        }

        // Инициализируем слот с данными ресурса
        slotUI.Initialize(resource);

        // Сохраняем в словарь
        resourceSlots[resource.resourceName] = slotUI;

        Debug.Log($"[ResourcePanelUI] Created slot for resource: {resource.resourceName}");
    }

    /// <summary>
    /// Удалить слот ресурса
    /// </summary>
    void RemoveResourceSlot(string resourceName)
    {
        if (resourceSlots.ContainsKey(resourceName))
        {
            ResourceSlotUI slotUI = resourceSlots[resourceName];
            if (slotUI != null && slotUI.gameObject != null)
            {
                Destroy(slotUI.gameObject);
            }

            resourceSlots.Remove(resourceName);
            Debug.Log($"[ResourcePanelUI] Removed slot for resource: {resourceName}");
        }
    }

    /// <summary>
    /// Обновить отображение всех ресурсов (динамическое создание/удаление слотов)
    /// </summary>
    public void UpdateResourceDisplay()
    {
        if (resourceManager == null)
            return;

        // Собираем информацию о доступных ресурсах
        Dictionary<string, int> availableResources = CollectAvailableResources();

        // Список ресурсов для удаления (закончились)
        List<string> resourcesToRemove = new List<string>();

        // 1. Обновляем существующие слоты
        foreach (var kvp in resourceSlots)
        {
            string resourceName = kvp.Key;
            ResourceSlotUI slotUI = kvp.Value;

            if (availableResources.ContainsKey(resourceName))
            {
                // Ресурс есть - обновляем количество
                int quantity = availableResources[resourceName];
                if (slotUI != null)
                {
                    slotUI.UpdateQuantity(quantity);
                }
            }
            else
            {
                // Ресурса больше нет - помечаем на удаление
                resourcesToRemove.Add(resourceName);
            }
        }

        // 2. Удаляем слоты ресурсов, которых больше нет
        foreach (string resourceName in resourcesToRemove)
        {
            RemoveResourceSlot(resourceName);
        }

        // 3. Создаем слоты для новых ресурсов
        foreach (var kvp in availableResources)
        {
            string resourceName = kvp.Key;
            int quantity = kvp.Value;

            // Если количество > 0 и слота еще нет - создаем
            if (quantity > 0 && !resourceSlots.ContainsKey(resourceName))
            {
                ResourceData resourceData = resourceManager.GetResourceByName(resourceName);
                if (resourceData != null)
                {
                    CreateResourceSlot(resourceData);

                    // Сразу обновляем количество
                    if (resourceSlots.ContainsKey(resourceName))
                    {
                        resourceSlots[resourceName].UpdateQuantity(quantity);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Собрать информацию о всех доступных ресурсах
    /// </summary>
    Dictionary<string, int> CollectAvailableResources()
    {
        Dictionary<string, int> resources = new Dictionary<string, int>();

        // 1. Собираем ресурсы из инвентарей персонажей игрока
        // Используем кэшированный массив вместо FindObjectsOfType
        if (cachedCharacters == null || cachedCharacters.Length == 0)
        {
            RefreshCache();
        }

        foreach (Character character in cachedCharacters)
        {
            if (character.IsPlayerCharacter())
            {
                Inventory inventory = character.GetComponent<Inventory>();
                if (inventory != null)
                {
                    CollectResourcesFromInventory(inventory, resources);
                }
            }
        }

        // 2. Собираем ресурсы, лежащие на территории корабля
        if (gridManager != null)
        {
            CollectResourcesFromShipTerritory(resources);
        }

        return resources;
    }

    /// <summary>
    /// Собрать ресурсы из инвентаря
    /// </summary>
    void CollectResourcesFromInventory(Inventory inventory, Dictionary<string, int> resources)
    {
        List<InventorySlot> slots = inventory.GetAllSlots();

        foreach (InventorySlot slot in slots)
        {
            if (!slot.IsEmpty() && slot.itemData.itemType == ItemType.Resource)
            {
                string resourceName = slot.itemData.itemName;
                int quantity = slot.quantity;

                if (resources.ContainsKey(resourceName))
                {
                    resources[resourceName] += quantity;
                }
                else
                {
                    resources[resourceName] = quantity;
                }
            }
        }
    }

    /// <summary>
    /// Собрать ресурсы, лежащие на территории корабля
    /// </summary>
    void CollectResourcesFromShipTerritory(Dictionary<string, int> resources)
    {
        // Получаем все клетки, занятые комнатами корабля
        List<GridCell> roomCells = gridManager.GetCellsByObjectType("Room");
        if (roomCells.Count == 0)
        {
            // Если нет комнат, используем территорию вокруг кокпита
            roomCells = gridManager.GetCellsByObjectType("Cockpit");
        }

        // Если нет ни комнат, ни кокпита, проверяем территорию вокруг персонажей игрока
        if (roomCells.Count == 0)
        {
            roomCells = GetPlayerCharacterCells();
        }

        // Создаем HashSet для быстрой проверки принадлежности к территории корабля
        HashSet<Vector2Int> shipTerritory = new HashSet<Vector2Int>();
        foreach (GridCell cell in roomCells)
        {
            shipTerritory.Add(cell.gridPosition);

            // Добавляем также соседние клетки (внутри комнат)
            AddAdjacentCells(cell.gridPosition, shipTerritory, 3);
        }

        // Ищем предметы-ресурсы на территории корабля
        // Используем кэшированный массив вместо FindObjectsOfType
        if (cachedItems == null || cachedItems.Length == 0)
        {
            RefreshCache();
        }

        foreach (Item item in cachedItems)
        {
            if (item.itemData != null && item.itemData.itemType == ItemType.Resource)
            {
                Vector2Int itemGridPos = gridManager.WorldToGrid(item.transform.position);

                // Проверяем, находится ли предмет на территории корабля
                if (shipTerritory.Contains(itemGridPos))
                {
                    string resourceName = item.itemData.itemName;

                    if (resources.ContainsKey(resourceName))
                    {
                        resources[resourceName] += 1;
                    }
                    else
                    {
                        resources[resourceName] = 1;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Получить клетки, где находятся персонажи игрока
    /// </summary>
    List<GridCell> GetPlayerCharacterCells()
    {
        List<GridCell> cells = new List<GridCell>();

        Character[] characters = FindObjectsOfType<Character>();
        foreach (Character character in characters)
        {
            if (character.IsPlayerCharacter())
            {
                Vector2Int gridPos = gridManager.WorldToGrid(character.transform.position);
                GridCell cell = gridManager.GetCell(gridPos);
                if (cell != null)
                {
                    cells.Add(cell);
                }
            }
        }

        return cells;
    }

    /// <summary>
    /// Добавить соседние клетки в радиусе
    /// </summary>
    void AddAdjacentCells(Vector2Int center, HashSet<Vector2Int> cellSet, int radius)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector2Int adjacentPos = new Vector2Int(center.x + x, center.y + y);
                if (gridManager.IsValidGridPosition(adjacentPos))
                {
                    cellSet.Add(adjacentPos);
                }
            }
        }
    }

    /// <summary>
    /// Очистить все слоты ресурсов
    /// </summary>
    void ClearResourceSlots()
    {
        foreach (var kvp in resourceSlots)
        {
            if (kvp.Value != null && kvp.Value.gameObject != null)
            {
                Destroy(kvp.Value.gameObject);
            }
        }
        resourceSlots.Clear();
    }

    /// <summary>
    /// Получить количество определенного ресурса
    /// </summary>
    public int GetResourceQuantity(string resourceName)
    {
        if (resourceSlots.ContainsKey(resourceName) && resourceSlots[resourceName] != null)
        {
            return resourceSlots[resourceName].GetQuantity();
        }
        return 0;
    }

    /// <summary>
    /// Принудительно обновить панель
    /// </summary>
    public void ForceUpdate()
    {
        UpdateResourceDisplay();
    }

    void OnDestroy()
    {
        Debug.Log($"[ResourcePanelUI] OnDestroy called on {gameObject.name}");
        ClearResourceSlots();
        isInitialized = false;
    }

    /// <summary>
    /// Получить полный путь GameObject в иерархии
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
}
