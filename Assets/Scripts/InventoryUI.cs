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
    public Text inventoryStatsText;
    public Text characterNameText;

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
    private GameObject humanImageContainer;

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

        // Инициализируем систему tooltips
        TooltipSystem.Instance.gameObject.transform.SetParent(transform, false);

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

        // Создаем изображение человека (будет перенесено на задний план)
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

        // Перемещаем изображение человека на задний план после создания всех слотов
        MoveHumanImageToBackground();

        // ВРЕМЕННО ОТКЛЮЧАЕМ панель информации о предмете
        // CreateItemInfoPanel(panelGO);

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

        // Создаем текст с именем персонажа
        GameObject characterNameGO = new GameObject("CharacterName");
        characterNameGO.transform.SetParent(parent.transform, false);

        RectTransform characterNameRect = characterNameGO.AddComponent<RectTransform>();
        characterNameRect.anchorMin = new Vector2(0, 0.85f);
        characterNameRect.anchorMax = new Vector2(1, 0.9f);
        characterNameRect.offsetMin = Vector2.zero;
        characterNameRect.offsetMax = Vector2.zero;

        characterNameText = characterNameGO.AddComponent<Text>();
        characterNameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        characterNameText.fontSize = 14;
        characterNameText.color = new Color(0.8f, 0.8f, 1f, 1f); // Светло-голубой цвет
        characterNameText.text = "Персонаж";
        characterNameText.alignment = TextAnchor.MiddleCenter;
    }

    /// <summary>
    /// Создание контейнера с изображением человека (справа)
    /// </summary>
    void CreateHumanImageContainer(GameObject parent)
    {
        Debug.Log("[InventoryUI] Creating human image container...");
        humanImageContainer = new GameObject("HumanImageContainer");
        humanImageContainer.transform.SetParent(parent.transform, false);

        RectTransform humanContainer = humanImageContainer.AddComponent<RectTransform>();
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
            Image humanImage = humanImageContainer.AddComponent<Image>();
            humanImage.sprite = humanSprite;
            humanImage.type = Image.Type.Simple;
            humanImage.preserveAspect = true;
            humanImage.color = new Color(1f, 1f, 1f, 0.8f);
            humanImage.raycastTarget = false; // Отключаем рейкасты для изображения человека
            Debug.Log("[InventoryUI] Applied I_man sprite to human container (raycastTarget: false)");
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
    /// Переместить изображение человека на задний план
    /// </summary>
    void MoveHumanImageToBackground()
    {
        if (humanImageContainer != null)
        {
            // Перемещаем на первую позицию в иерархии (задний план)
            humanImageContainer.transform.SetAsFirstSibling();
            Debug.Log("[InventoryUI] Human image moved to background layer");
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

        // Перемещаем контейнер экипировки на передний план для видимости
        if (containerGO != null)
        {
            containerGO.transform.SetAsLastSibling();
            containerGO.SetActive(true); // Принудительно активируем

            // Проверяем все компоненты Image в контейнере
            Image containerImage = containerGO.GetComponent<Image>();
            if (containerImage != null)
            {
                containerImage.enabled = true;
                Debug.Log($"[InventoryUI] Equipment container Image enabled: {containerImage.enabled}");
            }

            Debug.Log($"[InventoryUI] Equipment container moved to front layer, active: {containerGO.activeInHierarchy}");
        }

        // Принудительно обновляем layout и проверяем видимость слотов
        Canvas.ForceUpdateCanvases();

        // Проверяем, что все слоты активны и видимы
        foreach (var kvp in equipmentSlotUIs)
        {
            InventorySlotUI slotUI = kvp.Value;
            if (slotUI != null)
            {
                // Также принудительно перемещаем каждый слот на передний план
                slotUI.transform.SetAsLastSibling();
                Debug.Log($"[InventoryUI] Equipment slot {kvp.Key}: active={slotUI.gameObject.activeInHierarchy}, " +
                         $"position={slotUI.transform.position}, localPosition={slotUI.transform.localPosition}");
            }
        }
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
            slotUI.OnSlotDoubleClicked += OnEquipmentSlotDoubleClicked; // Добавляем обработчик двойного клика
            slotUI.OnSlotDragAndDrop += OnSlotDragAndDrop; // Используем общий обработчик
            equipmentSlotUIs[slot] = slotUI;
            Debug.Log($"[InventoryUI] Added equipment slot {slot} to dictionary. Dictionary count: {equipmentSlotUIs.Count}");

            // Делаем слоты экипировки максимально видимыми
            Image slotImage = slotUI.backgroundImage;
            if (slotImage != null)
            {
                slotImage.color = new Color(0.8f, 0.8f, 0.9f, 1.0f); // Очень яркий цвет
                slotImage.raycastTarget = true;
                slotImage.enabled = true; // Явно включаем компонент
                Debug.Log($"[InventoryUI] Equipment slot {slot} backgroundImage color set to: {slotImage.color}");
            }

            // Проверяем основной Image компонент слота
            Image mainImage = slotUI.GetComponent<Image>();
            if (mainImage != null)
            {
                mainImage.color = new Color(0.7f, 0.7f, 0.8f, 1.0f); // Яркий фон
                mainImage.raycastTarget = true;
                mainImage.enabled = true;
                Debug.Log($"[InventoryUI] Equipment slot {slot} mainImage color set to: {mainImage.color}");
            }

            // Принудительно включаем все Image компоненты в слоте
            Image[] allImages = slotUI.GetComponentsInChildren<Image>();
            foreach (Image img in allImages)
            {
                img.enabled = true;
                img.color = new Color(img.color.r, img.color.g, img.color.b, 1.0f); // Убираем прозрачность
                Debug.Log($"[InventoryUI] Equipment slot {slot} child image enabled: {img.name}, color: {img.color}");
            }
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
                SetCurrentInventory(character.GetInventory(), character);
                return;
            }
        }

        // Если не выделен союзный персонаж, скрываем инвентарь
        SetCurrentInventory(null, null);
    }

    /// <summary>
    /// Установить текущий инвентарь для отображения
    /// </summary>
    public void SetCurrentInventory(Inventory inventory, Character character = null)
    {
        // Отписываемся от событий предыдущего инвентаря
        if (currentInventory != null)
        {
            currentInventory.OnInventoryChanged -= UpdateInventoryDisplay;
            currentInventory.OnEquipmentChanged -= UpdateEquipmentDisplay;
        }

        currentInventory = inventory;

        // Обновляем имя персонажа
        if (characterNameText != null)
        {
            if (character != null)
            {
                characterNameText.text = character.GetFullName();
            }
            else
            {
                characterNameText.text = "Персонаж";
            }
        }

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
                slotUI.OnSlotDragAndDrop += OnSlotDragAndDrop;
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
                slotUI.OnSlotDragAndDrop -= OnSlotDragAndDrop;
                DestroyImmediate(slotUI.gameObject);
            }
        }
        slotUIElements.Clear();

        // НЕ очищаем слоты экипировки здесь, так как они создаются только при инициализации
        // и должны сохраняться между обновлениями инвентаря
        Debug.Log($"[InventoryUI] ClearSlotUIElements called - keeping equipment slots (count: {equipmentSlotUIs.Count})");
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

        // Обновляем визуальное состояние слотов экипировки
        Debug.Log($"[InventoryUI] About to call UpdateEquipmentSlotVisuals - equipmentSlotUIs count: {equipmentSlotUIs.Count}");
        foreach (var kvp in equipmentSlotUIs)
        {
            InventorySlotUI slotUI = kvp.Value;
            if (slotUI != null)
            {
                Debug.Log($"[InventoryUI] Before UpdateEquipmentSlotVisuals: {kvp.Key} slotUI={slotUI != null}, backgroundImage={slotUI.backgroundImage != null}");
                if (slotUI.backgroundImage != null)
                {
                    Debug.Log($"[InventoryUI] Before UpdateEquipmentSlotVisuals: {kvp.Key} color = {slotUI.backgroundImage.color}, enabled = {slotUI.backgroundImage.enabled}");
                }
            }
        }
        UpdateEquipmentSlotVisuals();
        Debug.Log("[InventoryUI] After UpdateEquipmentSlotVisuals - checking slot colors after...");
        foreach (var kvp in equipmentSlotUIs)
        {
            InventorySlotUI slotUI = kvp.Value;
            if (slotUI != null)
            {
                Debug.Log($"[InventoryUI] After UpdateEquipmentSlotVisuals: {kvp.Key} slotUI={slotUI != null}, backgroundImage={slotUI.backgroundImage != null}");
                if (slotUI.backgroundImage != null)
                {
                    Debug.Log($"[InventoryUI] After UpdateEquipmentSlotVisuals: {kvp.Key} color = {slotUI.backgroundImage.color}, enabled = {slotUI.backgroundImage.enabled}");
                }
            }
        }
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

                // Принудительно делаем слот видимым после обновления
                if (slotUI.backgroundImage != null)
                {
                    slotUI.backgroundImage.enabled = true;
                    slotUI.backgroundImage.color = new Color(0.8f, 0.8f, 0.9f, 1.0f);
                }

                // Также обеспечиваем видимость GameObject
                slotUI.gameObject.SetActive(true);
                Debug.Log($"[InventoryUI] Updated equipment slot {slotType}, active: {slotUI.gameObject.activeInHierarchy}");
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
            // Выделяем слот
            SelectSlot(slotIndex);
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

        // Информация о предмете отображается в tooltip при наведении курсора
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

            Debug.Log($"[DoubleClick] Attempting to auto-equip {item.itemName} ({item.itemType}, slot: {item.equipmentSlot})");

            // Проверяем, можно ли экипировать предмет
            if (item.CanBeEquipped() && item.equipmentSlot != EquipmentSlot.None)
            {
                // Для оружия проверяем доступные слоты рук
                if (item.itemType == ItemType.Weapon)
                {
                    EquipmentSlot targetSlot = FindAvailableWeaponSlot();
                    if (targetSlot != EquipmentSlot.None)
                    {
                        // Временно меняем слот предмета для экипировки
                        EquipmentSlot originalSlot = item.equipmentSlot;
                        item.equipmentSlot = targetSlot;

                        if (currentInventory.EquipItem(item))
                        {
                            currentInventory.RemoveItem(item, 1);
                            Debug.Log($"[DoubleClick] Successfully equipped {item.itemName} to {targetSlot}");
                            // Обновляем визуалы заблокированных слотов
                            UpdateEquipmentSlotVisuals();
                            // Снимаем выделение с пустого слота
                            ClearSlotSelection();
                        }
                        else
                        {
                            // Восстанавливаем оригинальный слот при неудаче
                            item.equipmentSlot = originalSlot;
                            Debug.Log($"[DoubleClick] Failed to equip weapon {item.itemName}");
                        }
                    }
                    else
                    {
                        Debug.Log($"[DoubleClick] No available weapon slots for {item.itemName}");
                    }
                }
                else
                {
                    // Для брони проверяем соответствующий слот
                    if (currentInventory.IsEquipmentSlotBlocked(item.equipmentSlot))
                    {
                        Debug.Log($"[DoubleClick] Cannot equip {item.itemName} - slot {item.equipmentSlot} is blocked");
                        return;
                    }

                    if (currentInventory.EquipItem(item))
                    {
                        currentInventory.RemoveItem(item, 1);
                        Debug.Log($"[DoubleClick] Successfully equipped {item.itemName} to {item.equipmentSlot}");
                        // Снимаем выделение с пустого слота
                        ClearSlotSelection();
                    }
                    else
                    {
                        Debug.Log($"[DoubleClick] Failed to equip {item.itemName} (slot may be occupied)");
                    }
                }
            }
            else
            {
                Debug.Log($"[DoubleClick] Item {item.itemName} cannot be equipped (CanBeEquipped: {item.CanBeEquipped()}, slot: {item.equipmentSlot})");
            }
        }
    }

    /// <summary>
    /// Найти доступный слот для оружия
    /// </summary>
    EquipmentSlot FindAvailableWeaponSlot()
    {
        // Проверяем правую руку сначала (приоритет)
        if (!currentInventory.IsEquipped(EquipmentSlot.RightHand))
        {
            return EquipmentSlot.RightHand;
        }

        // Потом левую руку
        if (!currentInventory.IsEquipped(EquipmentSlot.LeftHand))
        {
            return EquipmentSlot.LeftHand;
        }

        return EquipmentSlot.None; // Нет доступных слотов
    }

    /// <summary>
    /// Обработчик двойного клика по слоту экипировки (снятие предмета)
    /// </summary>
    void OnEquipmentSlotDoubleClicked(int slotIndex)
    {
        if (currentInventory == null) return;

        // Определяем слот экипировки по индексу
        EquipmentSlot equipmentSlot = (EquipmentSlot)slotIndex;

        Debug.Log($"[DoubleClick] Attempting to unequip from equipment slot {equipmentSlot}");

        // Проверяем, есть ли предмет в этом слоте
        if (currentInventory.IsEquipped(equipmentSlot))
        {
            // Получаем экипированный предмет для логирования
            ItemData equippedItem = currentInventory.GetEquippedItem(equipmentSlot);
            if (equippedItem != null)
            {
                // Проверяем, есть ли место в инвентаре
                int freeSlotIndex = FindNearestFreeInventorySlot();
                if (freeSlotIndex != -1)
                {
                    // Снимаем предмет с экипировки (UnequipItem уже добавляет предмет в инвентарь)
                    if (currentInventory.UnequipItem(equipmentSlot))
                    {
                        Debug.Log($"[DoubleClick] Successfully unequipped {equippedItem.itemName} from {equipmentSlot} to inventory");
                        // Обновляем визуалы заблокированных слотов
                        UpdateEquipmentSlotVisuals();
                        // Снимаем выделение
                        ClearSlotSelection();
                    }
                    else
                    {
                        Debug.Log($"[DoubleClick] Failed to unequip {equippedItem.itemName} from {equipmentSlot} - inventory might be full");
                    }
                }
                else
                {
                    Debug.Log($"[DoubleClick] No free inventory slots available for {equippedItem.itemName}");
                }
            }
            else
            {
                Debug.LogWarning($"[DoubleClick] Equipment slot {equipmentSlot} shows as equipped but no item found");
            }
        }
        else
        {
            Debug.Log($"[DoubleClick] No item equipped in slot {equipmentSlot}");
        }
    }

    /// <summary>
    /// Найти ближайший свободный слот в основном инвентаре
    /// </summary>
    int FindNearestFreeInventorySlot()
    {
        if (currentInventory == null) return -1;

        var allSlots = currentInventory.GetAllSlots();
        for (int i = 0; i < allSlots.Count; i++)
        {
            if (allSlots[i].IsEmpty())
            {
                return i;
            }
        }

        return -1; // Нет свободных слотов
    }

    /// <summary>
    /// Обработчик drag and drop между слотами
    /// </summary>
    void OnSlotDragAndDrop(int fromDragDropId, int toDragDropId)
    {
        if (currentInventory == null) return;

        Debug.Log($"[DragDrop] Moving from DragDropId {fromDragDropId} to DragDropId {toDragDropId}");

        // Определяем типы слотов по ID
        bool isFromEquipment = fromDragDropId >= 1000;
        bool isToEquipment = toDragDropId >= 1000;

        if (isFromEquipment && isToEquipment)
        {
            // Перемещение между слотами экипировки
            HandleEquipmentToEquipmentDrag(fromDragDropId - 1000, toDragDropId - 1000);
        }
        else if (isFromEquipment && !isToEquipment)
        {
            // Из слота экипировки в обычный инвентарь
            HandleEquipmentToInventoryDrag(fromDragDropId - 1000, toDragDropId);
        }
        else if (!isFromEquipment && isToEquipment)
        {
            // Из обычного инвентаря в слот экипировки
            HandleInventoryToEquipmentDrag(fromDragDropId, toDragDropId - 1000);
        }
        else
        {
            // Обычное перемещение между слотами инвентаря
            HandleInventoryToInventoryDrag(fromDragDropId, toDragDropId);
        }
    }

    /// <summary>
    /// Обработка перетаскивания между слотами инвентаря
    /// </summary>
    void HandleInventoryToInventoryDrag(int fromSlot, int toSlot)
    {
        Debug.Log($"[DragDrop] Inventory to inventory: {fromSlot} -> {toSlot}");
        bool success = currentInventory.MoveItem(fromSlot, toSlot);

        if (success)
        {
            Debug.Log($"Moved item from slot {fromSlot} to slot {toSlot}");
        }
        else
        {
            Debug.Log($"Failed to move item from slot {fromSlot} to slot {toSlot}");
        }
    }

    /// <summary>
    /// Обработка перетаскивания из инвентаря в экипировку
    /// </summary>
    void HandleInventoryToEquipmentDrag(int fromSlot, int equipSlotId)
    {
        EquipmentSlot equipSlot = (EquipmentSlot)equipSlotId;
        Debug.Log($"[DragDrop] Inventory to equipment: slot {fromSlot} -> {equipSlot} (ID: {equipSlotId})");

        InventorySlot fromInventorySlot = currentInventory.GetSlot(fromSlot);
        if (fromInventorySlot != null && !fromInventorySlot.IsEmpty())
        {
            ItemData item = fromInventorySlot.itemData;
            Debug.Log($"[DragDrop] Trying to equip {item.itemName} (requires: {item.equipmentSlot}) to slot {equipSlot}");

            // Проверяем совместимость
            bool canEquipToSlot = false;

            if (item.CanBeEquipped() && item.equipmentSlot != EquipmentSlot.None)
            {
                if (item.itemType == ItemType.Weapon)
                {
                    // Weapons can be equipped in either hand slot
                    canEquipToSlot = (equipSlot == EquipmentSlot.LeftHand || equipSlot == EquipmentSlot.RightHand);
                }
                else
                {
                    // Other items must match their specific equipment slot
                    canEquipToSlot = (item.equipmentSlot == equipSlot);
                }
            }

            if (canEquipToSlot)
            {
                // Check if the slot is blocked (e.g., second hand when weapon is already equipped)
                if (currentInventory.IsEquipmentSlotBlocked(equipSlot))
                {
                    Debug.Log($"Cannot equip {item.itemName} to {equipSlot} - slot is blocked (weapon already equipped in other hand)");
                    return;
                }

                // Temporarily change the item's equipment slot to match the target
                EquipmentSlot originalSlot = item.equipmentSlot;
                if (item.itemType == ItemType.Weapon)
                {
                    item.equipmentSlot = equipSlot; // Set to the slot we're equipping to
                }

                // Экипируем предмет
                if (currentInventory.EquipItem(item))
                {
                    currentInventory.RemoveItem(item, 1);
                    Debug.Log($"Equipped {item.itemName} to {equipSlot}");

                    // Update UI to show blocked slots
                    UpdateEquipmentSlotVisuals();
                }
                else
                {
                    // Restore original slot if equipping failed
                    if (item.itemType == ItemType.Weapon)
                    {
                        item.equipmentSlot = originalSlot;
                    }
                    Debug.Log("Failed to equip item - slot might be occupied");
                }
            }
            else
            {
                if (item.equipmentSlot == EquipmentSlot.None)
                {
                    Debug.Log($"Item {item.itemName} cannot be equipped - it's not an equipment item");
                }
                else
                {
                    Debug.Log($"Item {item.itemName} cannot be equipped in slot {equipSlot} - requires {item.equipmentSlot}");
                }
            }
        }
    }

    /// <summary>
    /// Обработка перетаскивания из экипировки в инвентарь
    /// </summary>
    void HandleEquipmentToInventoryDrag(int equipSlotId, int toSlot)
    {
        EquipmentSlot equipSlot = (EquipmentSlot)equipSlotId;
        Debug.Log($"[DragDrop] Equipment to inventory: {equipSlot} -> slot {toSlot}");

        // UnequipItem автоматически добавляет предмет в инвентарь, поэтому дополнительно добавлять не нужно
        if (currentInventory.UnequipItem(equipSlot))
        {
            Debug.Log($"Unequipped item from {equipSlot} to inventory");
            // Update UI to show unblocked slots
            UpdateEquipmentSlotVisuals();
        }
        else
        {
            Debug.Log("Failed to unequip item - inventory might be full");
        }
    }

    /// <summary>
    /// Обработка перетаскивания между слотами экипировки
    /// </summary>
    void HandleEquipmentToEquipmentDrag(int fromEquipSlotId, int toEquipSlotId)
    {
        EquipmentSlot fromEquipSlot = (EquipmentSlot)fromEquipSlotId;
        EquipmentSlot toEquipSlot = (EquipmentSlot)toEquipSlotId;
        Debug.Log($"[DragDrop] Equipment to equipment: {fromEquipSlot} -> {toEquipSlot}");

        ItemData fromItem = currentInventory.GetEquippedItem(fromEquipSlot);
        ItemData toItem = currentInventory.GetEquippedItem(toEquipSlot);

        if (fromItem == null)
        {
            Debug.Log("No item in source equipment slot");
            return;
        }

        // Снимаем предметы
        currentInventory.UnequipItem(fromEquipSlot);
        if (toItem != null)
        {
            currentInventory.UnequipItem(toEquipSlot);
        }

        // Пытаемся экипировать в новые слоты
        bool fromItemEquipped = false;
        bool toItemEquipped = false;

        // Предметы с None не могут быть экипированы
        if (fromItem.equipmentSlot != EquipmentSlot.None && fromItem.equipmentSlot == toEquipSlot)
        {
            fromItemEquipped = currentInventory.EquipItem(fromItem);
        }

        if (toItem != null && toItem.equipmentSlot != EquipmentSlot.None && toItem.equipmentSlot == fromEquipSlot)
        {
            toItemEquipped = currentInventory.EquipItem(toItem);
        }

        // Если не удалось экипировать, добавляем в инвентарь
        if (!fromItemEquipped)
        {
            currentInventory.AddItem(fromItem, 1);
        }
        if (toItem != null && !toItemEquipped)
        {
            currentInventory.AddItem(toItem, 1);
        }

        Debug.Log($"Equipment swap completed: {fromEquipSlot}<->{toEquipSlot}");
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
                    SetCurrentInventory(character.GetInventory(), character);
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

            Debug.Log("[InventoryUI] ShowInventory called - checking equipment slots visibility...");
            foreach (var kvp in equipmentSlotUIs)
            {
                InventorySlotUI slotUI = kvp.Value;
                if (slotUI != null)
                {
                    Debug.Log($"[InventoryUI] Equipment slot {kvp.Key}: active={slotUI.gameObject.activeInHierarchy}, " +
                             $"backgroundEnabled={slotUI.backgroundImage?.enabled}, " +
                             $"backgroundColor={slotUI.backgroundImage?.color}");
                }
            }

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

            // Скрываем tooltip при закрытии инвентаря
            if (TooltipSystem.Instance != null)
            {
                TooltipSystem.Instance.HideTooltip();
            }
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

        // Очищаем слоты экипировки только при уничтожении
        foreach (var kvp in equipmentSlotUIs)
        {
            InventorySlotUI slotUI = kvp.Value;
            if (slotUI != null)
            {
                slotUI.OnSlotClicked -= OnEquipmentSlotClicked;
                slotUI.OnSlotRightClicked -= OnEquipmentSlotRightClicked;
                slotUI.OnSlotDoubleClicked -= OnEquipmentSlotDoubleClicked;
                slotUI.OnSlotDragAndDrop -= OnSlotDragAndDrop;
                DestroyImmediate(slotUI.gameObject);
            }
        }
        equipmentSlotUIs.Clear();
    }

    /// <summary>
    /// Обновить визуальное отображение слотов экипировки (показать заблокированные)
    /// </summary>
    void UpdateEquipmentSlotVisuals()
    {
        if (currentInventory == null || equipmentSlotUIs == null) return;

        foreach (var kvp in equipmentSlotUIs)
        {
            EquipmentSlot slot = kvp.Key;
            InventorySlotUI slotUI = kvp.Value;

            if (slotUI != null && slotUI.backgroundImage != null)
            {
                // Check if this slot should be blocked
                bool isBlocked = currentInventory.IsEquipmentSlotBlocked(slot);
                bool hasItem = currentInventory.IsEquipped(slot);

                if (isBlocked && !hasItem)
                {
                    // Show blocked state (gray/darker) but still visible
                    slotUI.backgroundImage.color = new Color(0.4f, 0.4f, 0.4f, 1.0f); // Visible blocked state
                }
                else if (hasItem)
                {
                    // Show equipped state (bright for visibility)
                    slotUI.backgroundImage.color = new Color(0.6f, 0.8f, 1.0f, 1.0f); // Bright equipped color
                }
                else
                {
                    // Show empty state (still visible!)
                    slotUI.backgroundImage.color = new Color(0.8f, 0.8f, 0.9f, 1.0f); // Bright empty state
                }

                // Принудительно включаем видимость
                slotUI.backgroundImage.enabled = true;
                Debug.Log($"[InventoryUI] Updated equipment slot {slot} visual: blocked={isBlocked}, hasItem={hasItem}, color={slotUI.backgroundImage.color}");
            }
        }
    }
}