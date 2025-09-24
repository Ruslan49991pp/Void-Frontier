using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI для отображения и управления инвентарем
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    public Canvas mainCanvas;
    public RectTransform inventoryPanel;
    public Button inventoryToggleButton;

    [Header("Inventory Display")]
    public RectTransform slotsContainer;
    public RectTransform itemInfoPanel;
    public Text itemInfoText;
    public Text inventoryStatsText;

    [Header("Equipment Display")]
    public RectTransform equipmentContainer;
    public Dictionary<EquipmentSlot, InventorySlotUI> equipmentSlotUIs;

    [Header("Settings")]
    public bool showInventoryOnStart = false;
    public KeyCode toggleKey = KeyCode.I;
    public int slotsPerRow = 5;
    public Vector2 slotSize = new Vector2(60, 60);
    public Vector2 slotSpacing = new Vector2(5, 5);

    // Внутренние переменные
    private bool isInventoryVisible = false;
    private Inventory currentInventory;
    private List<InventorySlotUI> slotUIElements = new List<InventorySlotUI>();
    private SelectionManager selectionManager;
    private InventorySlotUI selectedSlotUI;

    // Статическое свойство для проверки открытого инвентаря
    public static bool IsAnyInventoryOpen { get; private set; } = false;

    // Префабы для создания UI элементов
    private GameObject slotPrefab;

    void Awake()
    {
        equipmentSlotUIs = new Dictionary<EquipmentSlot, InventorySlotUI>();
        FindSelectionManager();
        CreateSlotPrefab();
        InitializeUI();
    }

    void Start()
    {
        if (selectionManager != null)
        {
            selectionManager.OnSelectionChanged += OnSelectionChanged;
        }
        else
        {
            Debug.LogWarning("[InventoryUI] SelectionManager not found!");
        }

        if (showInventoryOnStart)
        {
            ShowInventory();
        }
        else
        {
            HideInventory();
        }
    }

    void Update()
    {
        // Переключение инвентаря по клавише
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleInventory();
        }

        // Обработка клика мыши для снятия выделения слота
        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }
    }

    /// <summary>
    /// Поиск SelectionManager в сцене
    /// </summary>
    void FindSelectionManager()
    {
        selectionManager = FindObjectOfType<SelectionManager>();
    }

    /// <summary>
    /// Создание префаба для слота инвентаря
    /// </summary>
    void CreateSlotPrefab()
    {
        GameObject prefab = new GameObject("InventorySlot");

        // Компоненты слота
        RectTransform rectTransform = prefab.AddComponent<RectTransform>();
        rectTransform.sizeDelta = slotSize;

        Image background = prefab.AddComponent<Image>();
        background.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        Button button = prefab.AddComponent<Button>();

        // Иконка предмета
        GameObject iconGO = new GameObject("ItemIcon");
        iconGO.transform.SetParent(prefab.transform, false);

        RectTransform iconRect = iconGO.AddComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(5, 5);
        iconRect.offsetMax = new Vector2(-5, -5);

        Image iconImage = iconGO.AddComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.color = Color.white;

        // Текст количества
        GameObject quantityGO = new GameObject("QuantityText");
        quantityGO.transform.SetParent(prefab.transform, false);

        RectTransform quantityRect = quantityGO.AddComponent<RectTransform>();
        quantityRect.anchorMin = new Vector2(0.7f, 0);
        quantityRect.anchorMax = new Vector2(1, 0.3f);
        quantityRect.offsetMin = Vector2.zero;
        quantityRect.offsetMax = Vector2.zero;

        Text quantityText = quantityGO.AddComponent<Text>();
        quantityText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        quantityText.fontSize = 12;
        quantityText.color = Color.white;
        quantityText.alignment = TextAnchor.LowerRight;
        quantityText.text = "";

        // Добавляем компонент InventorySlotUI
        InventorySlotUI slotUI = prefab.AddComponent<InventorySlotUI>();
        slotUI.Initialize(background, iconImage, quantityText, button);

        // Отключаем по умолчанию
        prefab.SetActive(false);
        slotPrefab = prefab;
    }

    /// <summary>
    /// Инициализация UI
    /// </summary>
    void InitializeUI()
    {
        // Создаем главный Canvas если его нет
        if (mainCanvas == null)
        {
            GameObject canvasGO = new GameObject("InventoryUI_Canvas");
            mainCanvas = canvasGO.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 200; // Поверх игрового UI

            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        CreateInventoryPanel();
        CreateToggleButton();
    }

    /// <summary>
    /// Создание панели инвентаря
    /// </summary>
    void CreateInventoryPanel()
    {
        Debug.Log("[InventoryUI] Creating main inventory panel...");
        GameObject panelGO = new GameObject("InventoryPanel");
        panelGO.transform.SetParent(mainCanvas.transform, false);

        inventoryPanel = panelGO.AddComponent<RectTransform>();
        inventoryPanel.anchorMin = new Vector2(0.1f, 0.1f);  // Увеличиваем размер панели
        inventoryPanel.anchorMax = new Vector2(0.9f, 0.9f);
        inventoryPanel.offsetMin = Vector2.zero;
        inventoryPanel.offsetMax = Vector2.zero;

        Debug.Log($"[InventoryUI] Main panel created with rect: {inventoryPanel.rect}, anchors: {inventoryPanel.anchorMin} - {inventoryPanel.anchorMax}");

        // Фон панели (полупрозрачный темный)
        Image panelBg = panelGO.AddComponent<Image>();
        panelBg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        // Создаем отдельный контейнер для изображения человека (справа)
        CreateHumanImageContainer(panelGO);

        // Добавляем CanvasGroup для блокировки рейкастов
        CanvasGroup canvasGroup = panelGO.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        // Заголовок
        CreateInventoryHeader(panelGO);

        // Контейнер для слотов инвентаря (слева)
        CreateSlotsContainer(panelGO);

        // Контейнер для экипировки (справа, поверх изображения человека)
        CreateEquipmentContainer(panelGO);

        // Панель информации о предмете
        CreateItemInfoPanel(panelGO);

        // Статистика инвентаря
        CreateInventoryStats(panelGO);

        // Кнопка закрытия
        CreateCloseButton(panelGO);
    }

    /// <summary>
    /// Создание заголовка инвентаря
    /// </summary>
    void CreateInventoryHeader(GameObject parent)
    {
        GameObject headerGO = new GameObject("Header");
        headerGO.transform.SetParent(parent.transform, false);

        RectTransform headerRect = headerGO.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 0.9f);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.offsetMin = Vector2.zero;
        headerRect.offsetMax = Vector2.zero;

        Text headerText = headerGO.AddComponent<Text>();
        headerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        headerText.fontSize = 18;
        headerText.color = Color.white;
        headerText.text = "ИНВЕНТАРЬ";
        headerText.alignment = TextAnchor.MiddleCenter;
    }

    /// <summary>
    /// Создание контейнера с изображением человека (справа)
    /// </summary>
    void CreateHumanImageContainer(GameObject parent)
    {
        Debug.Log("[InventoryUI] Creating human image container...");
        GameObject humanContainerGO = new GameObject("HumanImageContainer");
        humanContainerGO.transform.SetParent(parent.transform, false);

        RectTransform humanContainer = humanContainerGO.AddComponent<RectTransform>();
        humanContainer.anchorMin = new Vector2(0.6f, 0.15f);  // Справа
        humanContainer.anchorMax = new Vector2(0.95f, 0.85f);
        humanContainer.offsetMin = Vector2.zero;
        humanContainer.offsetMax = Vector2.zero;

        Debug.Log($"[InventoryUI] Human image container rect: {humanContainer.rect}");

        // Загружаем изображение человека
        Debug.Log("[InventoryUI] Trying to load I_man sprite from Resources...");
        Sprite humanSprite = Resources.Load<Sprite>("I_man");

        // Пробуем альтернативные пути
        if (humanSprite == null)
        {
            Debug.Log("[InventoryUI] Trying alternative path: Resources/I_man...");
            humanSprite = Resources.Load<Sprite>("Resources/I_man");
        }

        if (humanSprite != null)
        {
            Debug.Log($"[InventoryUI] Successfully loaded I_man sprite: {humanSprite.name}, size: {humanSprite.rect}");
            Image humanImage = humanContainerGO.AddComponent<Image>();
            humanImage.sprite = humanSprite;
            humanImage.type = Image.Type.Simple;
            humanImage.preserveAspect = true;
            humanImage.color = new Color(1f, 1f, 1f, 0.8f);
            Debug.Log("[InventoryUI] Applied I_man sprite to human container");
        }
        else
        {
            Debug.LogError("[InventoryUI] Failed to load I_man sprite from Resources!");

            // Проверяем что есть в ресурсах
            Sprite[] allSprites = Resources.LoadAll<Sprite>("");
            Debug.LogError($"[InventoryUI] Found {allSprites.Length} sprites in Resources");
            for (int i = 0; i < allSprites.Length; i++)
            {
                Debug.LogError($"[InventoryUI] Sprite {i}: {allSprites[i].name}");
            }
        }
    }

    /// <summary>
    /// Создание контейнера для экипировки (поверх изображения человека)
    /// </summary>
    void CreateEquipmentContainer(GameObject parent)
    {
        Debug.Log("[InventoryUI] Creating equipment container...");
        GameObject containerGO = new GameObject("EquipmentContainer");
        containerGO.transform.SetParent(parent.transform, false);

        equipmentContainer = containerGO.AddComponent<RectTransform>();
        equipmentContainer.anchorMin = new Vector2(0.6f, 0.15f);  // Справа, поверх изображения человека
        equipmentContainer.anchorMax = new Vector2(0.95f, 0.85f);
        equipmentContainer.offsetMin = Vector2.zero;
        equipmentContainer.offsetMax = Vector2.zero;

        Debug.Log($"[InventoryUI] Equipment container rect: {equipmentContainer.rect}");

        // Создаем слоты экипировки в позициях частей тела человека
        CreateEquipmentSlot(containerGO, EquipmentSlot.LeftHand, new Vector2(0.15f, 0.55f)); // Левая рука
        CreateEquipmentSlot(containerGO, EquipmentSlot.RightHand, new Vector2(0.85f, 0.55f)); // Правая рука
        CreateEquipmentSlot(containerGO, EquipmentSlot.Head, new Vector2(0.5f, 0.9f)); // Голова
        CreateEquipmentSlot(containerGO, EquipmentSlot.Chest, new Vector2(0.5f, 0.7f)); // Грудь
        CreateEquipmentSlot(containerGO, EquipmentSlot.Legs, new Vector2(0.5f, 0.45f)); // Ноги
        CreateEquipmentSlot(containerGO, EquipmentSlot.Feet, new Vector2(0.5f, 0.15f)); // Ступни

        Debug.Log("[InventoryUI] Equipment container created with 6 slots");
    }

    /// <summary>
    /// Создание слота экипировки
    /// </summary>
    void CreateEquipmentSlot(GameObject parent, EquipmentSlot slot, Vector2 normalizedPosition)
    {
        Debug.Log($"[InventoryUI] Creating equipment slot {slot} at position {normalizedPosition}");

        GameObject slotGO = Instantiate(slotPrefab, parent.transform);
        slotGO.SetActive(true);
        slotGO.name = $"EquipmentSlot_{slot}";

        RectTransform slotRect = slotGO.GetComponent<RectTransform>();

        // Сначала устанавливаем pivot для корректной привязки
        slotRect.pivot = new Vector2(0.5f, 0.5f);

        // Устанавливаем якорные точки
        slotRect.anchorMin = normalizedPosition;
        slotRect.anchorMax = normalizedPosition;

        // Обнуляем позицию и устанавливаем размер
        slotRect.anchoredPosition = Vector2.zero;
        slotRect.sizeDelta = new Vector2(60, 60);

        // Принудительно обновляем layout
        Canvas.ForceUpdateCanvases();

        Debug.Log($"[InventoryUI] Equipment slot {slot} - anchorMin: {slotRect.anchorMin}, anchorMax: {slotRect.anchorMax}");
        Debug.Log($"[InventoryUI] Equipment slot {slot} - anchoredPosition: {slotRect.anchoredPosition}, sizeDelta: {slotRect.sizeDelta}");
        Debug.Log($"[InventoryUI] Equipment slot {slot} - final rect: {slotRect.rect}");

        InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
        if (slotUI != null)
        {
            slotUI.SetEquipmentSlot(slot);
            slotUI.SetSlotIndex((int)slot); // Используем индекс слота экипировки
            slotUI.OnSlotClicked += OnEquipmentSlotClicked;
            slotUI.OnSlotRightClicked += OnEquipmentSlotRightClicked;
            equipmentSlotUIs[slot] = slotUI;

            // Меняем цвет фона для слотов экипировки
            slotUI.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.4f, 0.9f);
            Debug.Log($"[InventoryUI] Equipment slot {slot} configured successfully");
        }
        else
        {
            Debug.LogError($"[InventoryUI] Failed to get InventorySlotUI component for slot {slot}");
        }
    }

    /// <summary>
    /// Создание контейнера для слотов инвентаря
    /// </summary>
    void CreateSlotsContainer(GameObject parent)
    {
        Debug.Log("[InventoryUI] Creating slots container...");
        GameObject containerGO = new GameObject("SlotsContainer");
        containerGO.transform.SetParent(parent.transform, false);

        slotsContainer = containerGO.AddComponent<RectTransform>();
        slotsContainer.anchorMin = new Vector2(0.05f, 0.15f);  // Слева, занимает большую часть экрана
        slotsContainer.anchorMax = new Vector2(0.55f, 0.85f);  // До изображения человека
        slotsContainer.offsetMin = Vector2.zero;
        slotsContainer.offsetMax = Vector2.zero;

        Debug.Log($"[InventoryUI] Slots container rect: {slotsContainer.rect}");

        // Фон контейнера
        Image containerBg = containerGO.AddComponent<Image>();
        containerBg.color = new Color(0.05f, 0.05f, 0.1f, 0.8f);

        // Сетка для слотов
        GridLayoutGroup gridLayout = containerGO.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(60, 60); // Увеличиваем размер ячеек
        gridLayout.spacing = new Vector2(5, 5); // Больше отступы для лучшего вида
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 5; // 5 колонок для классического размещения 5x4
        gridLayout.childAlignment = TextAnchor.UpperLeft;
        gridLayout.padding = new RectOffset(5, 5, 5, 5);

        Debug.Log($"[InventoryUI] Grid layout configured: cellSize={gridLayout.cellSize}, spacing={gridLayout.spacing}, columns={gridLayout.constraintCount}");
    }

    /// <summary>
    /// Создание панели информации о предмете
    /// </summary>
    void CreateItemInfoPanel(GameObject parent)
    {
        GameObject infoPanelGO = new GameObject("ItemInfoPanel");
        infoPanelGO.transform.SetParent(parent.transform, false);

        itemInfoPanel = infoPanelGO.AddComponent<RectTransform>();
        itemInfoPanel.anchorMin = new Vector2(0.75f, 0.25f);  // Справа от фигуры человека
        itemInfoPanel.anchorMax = new Vector2(0.95f, 0.85f);
        itemInfoPanel.offsetMin = Vector2.zero;
        itemInfoPanel.offsetMax = Vector2.zero;

        // Фон панели информации
        Image infoBg = infoPanelGO.AddComponent<Image>();
        infoBg.color = new Color(0.1f, 0.05f, 0.05f, 0.8f);

        // Текст информации о предмете
        GameObject textGO = new GameObject("ItemInfoText");
        textGO.transform.SetParent(infoPanelGO.transform, false);

        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 10);
        textRect.offsetMax = new Vector2(-10, -10);

        itemInfoText = textGO.AddComponent<Text>();
        itemInfoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        itemInfoText.fontSize = 12;
        itemInfoText.color = Color.white;
        itemInfoText.text = "Выберите предмет для просмотра информации";
        itemInfoText.alignment = TextAnchor.UpperLeft;
    }

    /// <summary>
    /// Создание статистики инвентаря
    /// </summary>
    void CreateInventoryStats(GameObject parent)
    {
        GameObject statsGO = new GameObject("InventoryStats");
        statsGO.transform.SetParent(parent.transform, false);

        RectTransform statsRect = statsGO.AddComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0.05f, 0.05f);
        statsRect.anchorMax = new Vector2(0.95f, 0.25f);
        statsRect.offsetMin = Vector2.zero;
        statsRect.offsetMax = Vector2.zero;

        // Фон статистики
        Image statsBg = statsGO.AddComponent<Image>();
        statsBg.color = new Color(0.05f, 0.1f, 0.05f, 0.8f);

        // Текст статистики
        GameObject textGO = new GameObject("StatsText");
        textGO.transform.SetParent(statsGO.transform, false);

        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 10);
        textRect.offsetMax = new Vector2(-10, -10);

        inventoryStatsText = textGO.AddComponent<Text>();
        inventoryStatsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        inventoryStatsText.fontSize = 12;
        inventoryStatsText.color = Color.white;
        inventoryStatsText.text = "Статистика инвентаря";
        inventoryStatsText.alignment = TextAnchor.UpperLeft;
    }

    /// <summary>
    /// Создание кнопки закрытия
    /// </summary>
    void CreateCloseButton(GameObject parent)
    {
        GameObject buttonGO = new GameObject("CloseButton");
        buttonGO.transform.SetParent(parent.transform, false);

        RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.9f, 0.9f);
        buttonRect.anchorMax = new Vector2(1, 1);
        buttonRect.offsetMin = new Vector2(-30, -30);
        buttonRect.offsetMax = Vector2.zero;

        Image buttonBg = buttonGO.AddComponent<Image>();
        buttonBg.color = new Color(0.8f, 0.2f, 0.2f, 1f);

        Button closeButton = buttonGO.AddComponent<Button>();
        closeButton.image = buttonBg;
        closeButton.onClick.AddListener(HideInventory);

        // Текст кнопки
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);

        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text buttonText = textGO.AddComponent<Text>();
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 14;
        buttonText.color = Color.white;
        buttonText.text = "×";
        buttonText.alignment = TextAnchor.MiddleCenter;
    }

    /// <summary>
    /// Создание кнопки переключения инвентаря
    /// </summary>
    void CreateToggleButton()
    {
        GameObject buttonGO = new GameObject("InventoryToggleButton");
        buttonGO.transform.SetParent(mainCanvas.transform, false);

        RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1, 1);
        buttonRect.anchorMax = new Vector2(1, 1);
        buttonRect.pivot = new Vector2(1, 1);
        buttonRect.anchoredPosition = new Vector2(-10, -10);
        buttonRect.sizeDelta = new Vector2(60, 30);

        Image buttonBg = buttonGO.AddComponent<Image>();
        buttonBg.color = new Color(0.2f, 0.5f, 0.8f, 0.8f);

        inventoryToggleButton = buttonGO.AddComponent<Button>();
        inventoryToggleButton.image = buttonBg;
        inventoryToggleButton.onClick.AddListener(ToggleInventory);

        // Текст кнопки
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);

        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text buttonText = textGO.AddComponent<Text>();
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 10;
        buttonText.color = Color.white;
        buttonText.text = "I";
        buttonText.alignment = TextAnchor.MiddleCenter;
    }

    /// <summary>
    /// Обработчик изменения выделения
    /// </summary>
    void OnSelectionChanged(List<GameObject> selectedObjects)
    {
        if (selectedObjects.Count == 1)
        {
            Character character = selectedObjects[0].GetComponent<Character>();
            if (character != null && character.IsPlayerCharacter())
            {
                SetCurrentInventory(character.GetInventory());
                return;
            }
        }

        // Если не выделен союзный персонаж, скрываем инвентарь
        SetCurrentInventory(null);
    }

    /// <summary>
    /// Установить текущий инвентарь для отображения
    /// </summary>
    public void SetCurrentInventory(Inventory inventory)
    {
        // Отписываемся от событий предыдущего инвентаря
        if (currentInventory != null)
        {
            currentInventory.OnInventoryChanged -= UpdateInventoryDisplay;
            currentInventory.OnEquipmentChanged -= UpdateEquipmentDisplay;
        }

        currentInventory = inventory;

        // Подписываемся на события нового инвентаря
        if (currentInventory != null)
        {
            currentInventory.OnInventoryChanged += UpdateInventoryDisplay;
            currentInventory.OnEquipmentChanged += UpdateEquipmentDisplay;
            UpdateInventoryDisplay();
            UpdateEquipmentDisplay();
        }
        else
        {
            ClearInventoryDisplay();
        }
    }

    /// <summary>
    /// Создать слоты для инвентаря
    /// </summary>
    void CreateInventorySlots()
    {
        if (currentInventory == null || slotsContainer == null) return;

        // Удаляем существующие слоты
        ClearSlotUIElements();

        // Создаем новые слоты
        for (int i = 0; i < currentInventory.maxSlots; i++)
        {
            GameObject slotGO = Instantiate(slotPrefab, slotsContainer);
            slotGO.SetActive(true);
            slotGO.name = $"Slot_{i}";

            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            if (slotUI != null)
            {
                slotUI.SetSlotIndex(i);
                slotUI.OnSlotClicked += OnSlotClicked;
                slotUI.OnSlotRightClicked += OnSlotRightClicked;
                slotUI.OnSlotDoubleClicked += OnSlotDoubleClicked;
                slotUIElements.Add(slotUI);
            }
        }
    }

    /// <summary>
    /// Очистить UI элементы слотов
    /// </summary>
    void ClearSlotUIElements()
    {
        foreach (InventorySlotUI slotUI in slotUIElements)
        {
            if (slotUI != null)
            {
                slotUI.OnSlotClicked -= OnSlotClicked;
                slotUI.OnSlotRightClicked -= OnSlotRightClicked;
                slotUI.OnSlotDoubleClicked -= OnSlotDoubleClicked;
                DestroyImmediate(slotUI.gameObject);
            }
        }
        slotUIElements.Clear();
    }

    /// <summary>
    /// Обновить отображение инвентаря
    /// </summary>
    void UpdateInventoryDisplay()
    {
        if (currentInventory == null) return;

        // Создаем слоты если их еще нет
        if (slotUIElements.Count == 0)
        {
            CreateInventorySlots();
        }

        // Обновляем каждый слот
        var allSlots = currentInventory.GetAllSlots();
        for (int i = 0; i < slotUIElements.Count && i < allSlots.Count; i++)
        {
            InventorySlotUI slotUI = slotUIElements[i];
            InventorySlot slot = allSlots[i];

            if (slotUI != null)
            {
                slotUI.UpdateSlot(slot);
            }
        }

        // Обновляем статистику
        UpdateInventoryStats();
    }

    /// <summary>
    /// Обновить отображение экипировки
    /// </summary>
    void UpdateEquipmentDisplay()
    {
        if (currentInventory == null) return;

        var equipmentSlots = currentInventory.GetAllEquipmentSlots();
        foreach (var kvp in equipmentSlotUIs)
        {
            EquipmentSlot slotType = kvp.Key;
            InventorySlotUI slotUI = kvp.Value;

            if (equipmentSlots.ContainsKey(slotType))
            {
                InventorySlot slot = equipmentSlots[slotType];
                slotUI.UpdateSlot(slot);
            }
        }
    }

    /// <summary>
    /// Обновить статистику инвентаря
    /// </summary>
    void UpdateInventoryStats()
    {
        if (currentInventory == null || inventoryStatsText == null) return;

        string stats = $"Слоты: {currentInventory.GetUsedSlots()}/{currentInventory.maxSlots}\n";
        stats += $"Вес: {currentInventory.GetCurrentWeight():F1}/{currentInventory.maxWeight:F1}\n";
        stats += $"Заполненность: {(currentInventory.GetWeightPercent() * 100):F0}%";

        if (currentInventory.autoPickupEnabled)
        {
            stats += "\n\nАвтоподбор: Включен";
        }

        inventoryStatsText.text = stats;
    }

    /// <summary>
    /// Очистить отображение инвентаря
    /// </summary>
    void ClearInventoryDisplay()
    {
        ClearSlotUIElements();

        if (inventoryStatsText != null)
        {
            inventoryStatsText.text = "Нет доступного инвентаря";
        }

        if (itemInfoText != null)
        {
            itemInfoText.text = "Выберите союзного персонажа для просмотра инвентаря";
        }
    }

    /// <summary>
    /// Обработчик клика по слоту
    /// </summary>
    void OnSlotClicked(int slotIndex)
    {
        if (currentInventory == null) return;

        InventorySlot slot = currentInventory.GetSlot(slotIndex);
        if (slot != null && !slot.IsEmpty())
        {
            // Выделяем слот и показываем информацию о предмете
            SelectSlot(slotIndex);
            ShowItemInfo(slot.itemData);
        }
        else
        {
            // Снимаем выделение
            ClearSlotSelection();
        }
    }

    /// <summary>
    /// Обработчик правого клика по слоту (выброс предмета)
    /// </summary>
    void OnSlotRightClicked(int slotIndex)
    {
        if (currentInventory == null) return;

        InventorySlot slot = currentInventory.GetSlot(slotIndex);
        if (slot != null && !slot.IsEmpty())
        {
            // Выбрасываем один предмет
            currentInventory.DropItem(slotIndex, 1);
        }
    }

    /// <summary>
    /// Обработчик клика по слоту экипировки
    /// </summary>
    void OnEquipmentSlotClicked(int slotIndex)
    {
        // Для слотов экипировки используем slotIndex как тип EquipmentSlot
        EquipmentSlot equipSlot = (EquipmentSlot)slotIndex;

        if (currentInventory == null) return;

        ItemData equippedItem = currentInventory.GetEquippedItem(equipSlot);
        if (equippedItem != null)
        {
            ShowItemInfo(equippedItem);
        }
    }

    /// <summary>
    /// Обработчик правого клика по слоту экипировки (снятие экипировки)
    /// </summary>
    void OnEquipmentSlotRightClicked(int slotIndex)
    {
        EquipmentSlot equipSlot = (EquipmentSlot)slotIndex;

        if (currentInventory == null) return;

        // Снимаем экипировку
        currentInventory.UnequipItem(equipSlot);
    }

    /// <summary>
    /// Обработчик двойного клика по слоту (экипировка предмета)
    /// </summary>
    void OnSlotDoubleClicked(int slotIndex)
    {
        if (currentInventory == null) return;

        InventorySlot slot = currentInventory.GetSlot(slotIndex);
        if (slot != null && !slot.IsEmpty())
        {
            ItemData item = slot.itemData;

            // Проверяем, можно ли экипировать предмет
            if (item.CanBeEquipped())
            {
                // Экипируем предмет
                if (currentInventory.EquipItem(item))
                {
                    // Удаляем предмет из обычного инвентаря
                    currentInventory.RemoveItem(item, 1);
                }
            }
        }
    }

    /// <summary>
    /// Выделить слот
    /// </summary>
    void SelectSlot(int slotIndex)
    {
        // Снимаем выделение с предыдущего слота
        if (selectedSlotUI != null)
        {
            selectedSlotUI.SetSelected(false);
        }

        // Выделяем новый слот
        if (slotIndex >= 0 && slotIndex < slotUIElements.Count)
        {
            selectedSlotUI = slotUIElements[slotIndex];
            selectedSlotUI.SetSelected(true);
        }
    }

    /// <summary>
    /// Снять выделение слота
    /// </summary>
    void ClearSlotSelection()
    {
        if (selectedSlotUI != null)
        {
            selectedSlotUI.SetSelected(false);
            selectedSlotUI = null;
        }

        if (itemInfoText != null)
        {
            itemInfoText.text = "Выберите предмет для просмотра информации";
        }
    }

    /// <summary>
    /// Показать информацию о предмете
    /// </summary>
    void ShowItemInfo(ItemData itemData)
    {
        if (itemInfoText == null || itemData == null) return;

        string info = $"<color=#{ColorUtility.ToHtmlStringRGB(itemData.GetRarityColor())}>{itemData.itemName}</color>\n\n";
        info += $"Тип: {itemData.itemType}\n";
        info += $"Редкость: {itemData.rarity}\n\n";
        info += itemData.GetFullDescription();

        itemInfoText.text = info;
    }

    /// <summary>
    /// Обработка клика мыши
    /// </summary>
    void HandleMouseClick()
    {
        // Проверяем, кликнули ли вне UI инвентаря
        if (isInventoryVisible && !RectTransformUtility.RectangleContainsScreenPoint(inventoryPanel, Input.mousePosition, mainCanvas.worldCamera))
        {
            // Если кликнули вне панели, снимаем выделение слота
            ClearSlotSelection();
        }
    }

    /// <summary>
    /// Переключить видимость инвентаря
    /// </summary>
    public void ToggleInventory()
    {
        // Если нет текущего инвентаря, попробуем найти любого персонажа игрока для демо
        if (currentInventory == null)
        {
            Character[] allCharacters = FindObjectsOfType<Character>();
            foreach (Character character in allCharacters)
            {
                if (character.IsPlayerCharacter())
                {
                    SetCurrentInventory(character.GetInventory());
                    break;
                }
            }
        }

        if (isInventoryVisible)
        {
            HideInventory();
        }
        else
        {
            ShowInventory();
        }
    }

    /// <summary>
    /// Показать инвентарь
    /// </summary>
    public void ShowInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.gameObject.SetActive(true);
            isInventoryVisible = true;
            IsAnyInventoryOpen = true;

            // Обновляем отображение
            UpdateInventoryDisplay();
        }
        else
        {
            Debug.LogError("[InventoryUI] Cannot show inventory - inventoryPanel is null!");
        }
    }

    /// <summary>
    /// Скрыть инвентарь
    /// </summary>
    public void HideInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.gameObject.SetActive(false);
            isInventoryVisible = false;
            IsAnyInventoryOpen = false;

            // Снимаем выделение
            ClearSlotSelection();
        }
        else
        {
            Debug.LogError("[InventoryUI] Cannot hide inventory - inventoryPanel is null!");
        }
    }

    void OnDestroy()
    {
        if (selectionManager != null)
        {
            selectionManager.OnSelectionChanged -= OnSelectionChanged;
        }

        if (currentInventory != null)
        {
            currentInventory.OnInventoryChanged -= UpdateInventoryDisplay;
        }

        ClearSlotUIElements();
    }
}