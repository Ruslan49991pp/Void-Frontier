using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    public TMP_Text characterNameTextTMP; // TextMeshPro версия

    [Header("Equipment Display")]
    public Dictionary<EquipmentSlot, InventorySlotUI> equipmentSlotUIs;

    [Header("Settings")]
    public bool showInventoryOnStart = false;
    public KeyCode toggleKey = KeyCode.I;
    public int slotsPerRow = 5;
    public Vector2 slotSize = new Vector2(60, 60);
    public Vector2 slotSpacing = new Vector2(5, 5);
    public int defaultInventorySlots = 24; // Количество слотов по умолчанию

    [Header("Prefabs")]
    public GameObject itemSlotPrefab; // Префаб ItemSlot

    // Внутренние переменные
    private bool isInventoryVisible = false;
    private Inventory currentInventory;
    private List<InventorySlotUI> slotUIElements = new List<InventorySlotUI>();
    private SelectionManager selectionManager;
    private InventorySlotUI selectedSlotUI;
    private RectTransform contentPanel; // Content панель для спавна слотов

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

        // Скрываем инвентарь сразу после создания
        if (!showInventoryOnStart && inventoryPanel != null)
        {
            inventoryPanel.gameObject.SetActive(false);
            isInventoryVisible = false;
        }
    }

    void Start()
    {
        if (selectionManager != null)
        {
            selectionManager.OnSelectionChanged += OnSelectionChanged;
        }
        else
        {

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
        // Ищем существующий Canvas_MainUI и Inventory внутри него
        GameObject mainUICanvas = GameObject.Find("Canvas_MainUI");
        if (mainUICanvas != null)
        {
            mainCanvas = mainUICanvas.GetComponent<Canvas>();

            // Ищем существующий Inventory объект по полному пути
            Transform windowsTransform = mainUICanvas.transform.Find("Windows");
            if (windowsTransform != null)
            {
                Transform inventoryTransform = windowsTransform.Find("Inventory");
                if (inventoryTransform != null)
                {
                    inventoryPanel = inventoryTransform.GetComponent<RectTransform>();

                    // Ищем Content панель для спавна ItemSlot
                    Transform itemsAreaTransform = inventoryTransform.Find("ItemsArea");
                    if (itemsAreaTransform != null)
                    {
                        Transform viewportTransform = itemsAreaTransform.Find("Viewport");
                        if (viewportTransform != null)
                        {
                            Transform contentTransform = viewportTransform.Find("Content");
                            if (contentTransform != null)
                            {
                                contentPanel = contentTransform.GetComponent<RectTransform>();
                            }
                        }
                    }

                    // Ищем Character name Text и CloseButton внутри Inventory
                    Transform headerTransform = inventoryTransform.Find("Header ");
                    if (headerTransform != null)
                    {
                        // Находим Character name
                        Transform characterNameTransform = headerTransform.Find("Character name");
                        if (characterNameTransform != null)
                        {
                            // Пробуем найти TextMeshPro компонент (приоритет)
                            characterNameTextTMP = characterNameTransform.GetComponent<TMP_Text>();
                            if (characterNameTextTMP == null)
                            {
                                // Если нет TMP, пробуем обычный Text
                                characterNameText = characterNameTransform.GetComponent<Text>();
                            }
                        }

                        // Находим и привязываем CloseButton
                        Transform closeButtonTransform = headerTransform.Find("CloseButton");
                        if (closeButtonTransform != null)
                        {
                            Button closeButton = closeButtonTransform.GetComponent<Button>();
                            if (closeButton != null)
                            {
                                // Очищаем существующие обработчики (если есть) и добавляем наш
                                closeButton.onClick.RemoveAllListeners();
                                closeButton.onClick.AddListener(HideInventory);
                            }
                        }
                    }

                    // Ищем и инициализируем EquipmentPanel
                    InitializeEquipmentPanel(inventoryTransform);

                    // Скрываем панель по умолчанию
                    if (!showInventoryOnStart)
                    {
                        inventoryPanel.gameObject.SetActive(false);
                    }
                }
                else
                {
                    Debug.LogError("InventoryUI: Inventory object not found in Canvas_MainUI/Windows!");
                }
            }
            else
            {
                Debug.LogError("InventoryUI: Windows object not found in Canvas_MainUI!");
            }
        }
        else
        {
            Debug.LogError("InventoryUI: Canvas_MainUI not found in scene!");
        }

        // Инициализируем систему tooltips
        TooltipSystem.Instance.gameObject.transform.SetParent(transform, false);
    }

    /// <summary>
    /// Инициализация EquipmentPanel и привязка к существующим слотам
    /// </summary>
    void InitializeEquipmentPanel(Transform inventoryTransform)
    {
        // Ищем слоты экипировки в Canvas_MainUI (с родителями)
        GameObject canvasMainUI = GameObject.Find("Canvas_MainUI");
        if (canvasMainUI == null)
        {
            Debug.LogWarning("InventoryUI: Canvas_MainUI not found for equipment slots");
            return;
        }

        // Ищем слоты по их реальным именам и привязываем к соответствующему EquipmentSlot
        BindEquipmentSlotInHierarchy(canvasMainUI.transform, "EquipmentSlot_Helmet", EquipmentSlot.Head);
        BindEquipmentSlotInHierarchy(canvasMainUI.transform, "EquipmentSlot_Armor", EquipmentSlot.Chest);
        BindEquipmentSlotInHierarchy(canvasMainUI.transform, "EquipmentSlot_Weapon", EquipmentSlot.RightHand);
        BindEquipmentSlotInHierarchy(canvasMainUI.transform, "EquipmentSlot_Pants", EquipmentSlot.Legs);
        BindEquipmentSlotInHierarchy(canvasMainUI.transform, "EquipmentSlot_Boots", EquipmentSlot.Feet);
    }

    /// <summary>
    /// Привязать существующий слот экипировки к системе (рекурсивный поиск)
    /// </summary>
    void BindEquipmentSlotInHierarchy(Transform parent, string slotName, EquipmentSlot equipmentSlot)
    {
        // Рекурсивный поиск слота в иерархии
        Transform slotTransform = FindTransformRecursive(parent, slotName);

        if (slotTransform == null)
        {
            Debug.LogWarning($"InventoryUI: Equipment slot '{slotName}' not found in Canvas_MainUI hierarchy");
            return;
        }

        GameObject slotGO = slotTransform.gameObject;

        // Получаем или добавляем InventorySlotUI компонент
        InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
        if (slotUI == null)
        {
            // Находим компоненты для инициализации InventorySlotUI
            Image background = slotGO.GetComponent<Image>();

            // Ищем Icon внутри слота
            Transform iconTransform = slotTransform.Find("Icon");
            Image iconImage = iconTransform != null ? iconTransform.GetComponent<Image>() : null;

            // Если Icon не найден, логируем все дочерние объекты
            if (iconTransform == null)
            {
                Debug.LogWarning($"InventoryUI: Icon not found in '{slotName}', listing children:");
                foreach (Transform child in slotTransform)
                {
                    Debug.LogWarning($"  - Child: {child.name}");
                }
            }

            // Для Text используем пустую ссылку (можно добавить QuantityText если нужно)
            Text quantityText = null;

            // Получаем или добавляем Button компонент
            Button button = slotGO.GetComponent<Button>();
            if (button == null)
            {
                button = slotGO.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
            }

            // Добавляем и инициализируем InventorySlotUI
            slotUI = slotGO.AddComponent<InventorySlotUI>();
            slotUI.Initialize(background, iconImage, quantityText, button);
        }

        // Настраиваем слот для экипировки
        slotUI.SetEquipmentSlot(equipmentSlot);
        slotUI.SetSlotIndex((int)equipmentSlot);

        // Подписываемся на события
        slotUI.OnSlotClicked += OnEquipmentSlotClicked;
        slotUI.OnSlotRightClicked += OnEquipmentSlotRightClicked;
        slotUI.OnSlotDoubleClicked += OnEquipmentSlotDoubleClicked;
        slotUI.OnSlotDragAndDrop += OnSlotDragAndDrop;

        // Сохраняем в словарь
        equipmentSlotUIs[equipmentSlot] = slotUI;
    }

    /// <summary>
    /// Рекурсивный поиск Transform по имени
    /// </summary>
    Transform FindTransformRecursive(Transform parent, string name)
    {
        // Проверяем текущий объект
        if (parent.name == name)
            return parent;

        // Проверяем всех детей
        foreach (Transform child in parent)
        {
            Transform result = FindTransformRecursive(child, name);
            if (result != null)
                return result;
        }

        return null;
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

        // Обновляем имя персонажа (используем TMP_Text если доступен, иначе обычный Text)
        if (characterNameTextTMP != null)
        {
            characterNameTextTMP.text = character != null ? character.GetFullName() : "Персонаж";
        }
        else if (characterNameText != null)
        {
            characterNameText.text = character != null ? character.GetFullName() : "Персонаж";
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
        // Используем Content панель если она найдена, иначе fallback на slotsContainer
        RectTransform targetContainer = contentPanel != null ? contentPanel : slotsContainer;

        if (targetContainer == null)
        {
            Debug.LogWarning("InventoryUI: No container found for inventory slots!");
            return;
        }

        // Удаляем существующие слоты
        ClearSlotUIElements();

        // Всегда создаем 24 слота в Content (независимо от maxSlots в Inventory)
        int slotCount = defaultInventorySlots;

        // Загружаем префаб ItemSlot если не назначен
        GameObject prefabToUse = itemSlotPrefab;
        if (prefabToUse == null)
        {
            prefabToUse = Resources.Load<GameObject>("Prefabs/UI/ItemSlot");
            if (prefabToUse == null)
            {
                // Fallback на программно созданный префаб
                prefabToUse = slotPrefab;
            }
        }

        if (prefabToUse == null)
        {
            Debug.LogError("InventoryUI: No slot prefab available!");
            return;
        }

        // Создаем новые слоты
        for (int i = 0; i < slotCount; i++)
        {
            GameObject slotGO = Instantiate(prefabToUse, targetContainer);
            slotGO.SetActive(true);
            slotGO.name = $"ItemSlot_{i}";

            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            if (slotUI != null)
            {
                // Проверяем, нужна ли инициализация (если префаб не был инициализирован)
                if (slotUI.backgroundImage == null || slotUI.itemIcon == null)
                {
                    // Ищем компоненты для инициализации
                    Image background = slotGO.GetComponent<Image>();
                    Transform iconTransform = slotGO.transform.Find("Icon");
                    if (iconTransform == null)
                        iconTransform = slotGO.transform.Find("ItemIcon");

                    Image iconImage = iconTransform != null ? iconTransform.GetComponent<Image>() : null;

                    Transform quantityTransform = slotGO.transform.Find("QuantityText");
                    Text quantityText = quantityTransform != null ? quantityTransform.GetComponent<Text>() : null;

                    Button button = slotGO.GetComponent<Button>();

                    // Инициализируем слот
                    slotUI.Initialize(background, iconImage, quantityText, button);
                }

                // Устанавливаем Canvas для drag & drop операций
                slotUI.SetDragCanvas(mainCanvas);

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

        foreach (var kvp in equipmentSlotUIs)
        {
            InventorySlotUI slotUI = kvp.Value;
            if (slotUI != null)
            {

                if (slotUI.backgroundImage != null)
                {

                }
            }
        }
        UpdateEquipmentSlotVisuals();

        foreach (var kvp in equipmentSlotUIs)
        {
            InventorySlotUI slotUI = kvp.Value;
            if (slotUI != null)
            {

                if (slotUI.backgroundImage != null)
                {

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



            // Проверяем, можно ли экипировать предмет
            if (item.CanBeEquipped() && item.equipmentSlot != EquipmentSlot.None)
            {
                // Для оружия проверяем доступные слоты рук
                if (item.itemType == ItemType.Weapon)
                {
                    // ПРОВЕРКА: сначала проверяем ВСЕ слоты рук на наличие такого же предмета
                    ItemData rightHandItem = currentInventory.GetEquippedItem(EquipmentSlot.RightHand);
                    ItemData leftHandItem = currentInventory.GetEquippedItem(EquipmentSlot.LeftHand);

                    if ((rightHandItem != null && rightHandItem.itemName == item.itemName) ||
                        (leftHandItem != null && leftHandItem.itemName == item.itemName))
                    {
                        return;
                    }

                    EquipmentSlot targetSlot = FindAvailableWeaponSlot();
                    if (targetSlot != EquipmentSlot.None)
                    {
                        // Временно меняем слот предмета для экипировки
                        EquipmentSlot originalSlot = item.equipmentSlot;
                        item.equipmentSlot = targetSlot;

                        if (currentInventory.EquipItem(item))
                        {
                            // ВАЖНО: удаляем предмет из конкретного слота, а не первый найденный!
                            currentInventory.RemoveItemFromSlot(slotIndex, 1);

                            // Обновляем визуалы заблокированных слотов
                            UpdateEquipmentSlotVisuals();
                            // Снимаем выделение с пустого слота
                            ClearSlotSelection();
                        }
                        else
                        {
                            // Восстанавливаем оригинальный слот при неудаче
                            item.equipmentSlot = originalSlot;

                        }
                    }
                    else
                    {
                        // Оба слота рук заняты
                    }
                }
                else
                {
                    // Для брони проверяем соответствующий слот
                    if (currentInventory.IsEquipmentSlotBlocked(item.equipmentSlot))
                    {

                        return;
                    }

                    // ПРОВЕРКА: не экипирован ли уже такой же предмет (по имени) в целевом слоте
                    ItemData equippedItem = currentInventory.GetEquippedItem(item.equipmentSlot);
                    if (equippedItem != null && equippedItem.itemName == item.itemName)
                    {
                        return;
                    }

                    if (currentInventory.EquipItem(item))
                    {
                        // ВАЖНО: удаляем предмет из конкретного слота, а не первый найденный!
                        currentInventory.RemoveItemFromSlot(slotIndex, 1);

                        // Снимаем выделение с пустого слота
                        ClearSlotSelection();
                    }
                    else
                    {

                    }
                }
            }
            else
            {

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

                        // Обновляем визуалы заблокированных слотов
                        UpdateEquipmentSlotVisuals();
                        // Снимаем выделение
                        ClearSlotSelection();
                    }
                    else
                    {

                    }
                }
                else
                {

                }
            }
            else
            {

            }
        }
        else
        {

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

        bool success = currentInventory.MoveItem(fromSlot, toSlot);

        if (success)
        {

        }
        else
        {

        }
    }

    /// <summary>
    /// Обработка перетаскивания из инвентаря в экипировку
    /// </summary>
    void HandleInventoryToEquipmentDrag(int fromSlot, int equipSlotId)
    {
        EquipmentSlot equipSlot = (EquipmentSlot)equipSlotId;

        InventorySlot fromInventorySlot = currentInventory.GetSlot(fromSlot);
        if (fromInventorySlot != null && !fromInventorySlot.IsEmpty())
        {
            ItemData item = fromInventorySlot.itemData;

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
                    return;
                }

                // ПРОВЕРКА: для оружия проверяем ВСЕ слоты рук, для остальных - целевой слот
                if (item.itemType == ItemType.Weapon)
                {
                    ItemData rightHandItem = currentInventory.GetEquippedItem(EquipmentSlot.RightHand);
                    ItemData leftHandItem = currentInventory.GetEquippedItem(EquipmentSlot.LeftHand);

                    if ((rightHandItem != null && rightHandItem.itemName == item.itemName) ||
                        (leftHandItem != null && leftHandItem.itemName == item.itemName))
                    {
                        return;
                    }
                }
                else
                {
                    // Для не-оружия проверяем только целевой слот
                    ItemData equippedItem = currentInventory.GetEquippedItem(equipSlot);
                    if (equippedItem != null && equippedItem.itemName == item.itemName)
                    {
                        return;
                    }
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
                    // ВАЖНО: удаляем предмет из конкретного слота, а не первый найденный!
                    currentInventory.RemoveItemFromSlot(fromSlot, 1);

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

        // UnequipItem автоматически добавляет предмет в инвентарь, поэтому дополнительно добавлять не нужно
        if (currentInventory.UnequipItem(equipSlot))
        {
            // Update UI to show unblocked slots
            UpdateEquipmentSlotVisuals();
        }
    }

    /// <summary>
    /// Обработка перетаскивания между слотами экипировки
    /// </summary>
    void HandleEquipmentToEquipmentDrag(int fromEquipSlotId, int toEquipSlotId)
    {
        EquipmentSlot fromEquipSlot = (EquipmentSlot)fromEquipSlotId;
        EquipmentSlot toEquipSlot = (EquipmentSlot)toEquipSlotId;

        ItemData fromItem = currentInventory.GetEquippedItem(fromEquipSlot);
        ItemData toItem = currentInventory.GetEquippedItem(toEquipSlot);

        if (fromItem == null)
        {
            return;
        }

        // ВАЖНО: UnequipItem автоматически добавляет предметы в инвентарь!
        // Поэтому нам НЕ нужно вызывать AddItem если экипировка не удалась

        // Проверяем, можно ли выполнить обмен ПЕРЕД снятием предметов
        bool canEquipFromToTarget = (fromItem.equipmentSlot == toEquipSlot);
        bool canEquipToToSource = (toItem != null && toItem.equipmentSlot == fromEquipSlot);

        // Если ни один предмет не может быть экипирован в целевой слот, отменяем операцию
        if (!canEquipFromToTarget && !canEquipToToSource)
        {
            return; // Не снимаем предметы, если обмен невозможен
        }

        // Снимаем предметы (они автоматически попадут в инвентарь)
        currentInventory.UnequipItem(fromEquipSlot);
        if (toItem != null)
        {
            currentInventory.UnequipItem(toEquipSlot);
        }

        // Пытаемся экипировать в новые слоты
        if (canEquipFromToTarget)
        {
            if (currentInventory.EquipItem(fromItem))
            {
                // Удаляем из инвентаря, так как экипировка удалась
                currentInventory.RemoveItem(fromItem, 1);
            }
        }

        if (toItem != null && canEquipToToSource)
        {
            if (currentInventory.EquipItem(toItem))
            {
                // Удаляем из инвентаря, так как экипировка удалась
                currentInventory.RemoveItem(toItem, 1);
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
        // Если инвентарь открыт, всегда позволяем его закрыть
        if (isInventoryVisible)
        {
            HideInventory();
            return;
        }

        // Для открытия инвентаря нужен выделенный персонаж
        if (selectionManager == null)
        {
            selectionManager = FindObjectOfType<SelectionManager>();
        }

        if (selectionManager != null)
        {
            List<GameObject> selectedObjects = selectionManager.GetSelectedObjects();

            // Проверяем, есть ли выделенный персонаж игрока
            Character selectedCharacter = null;
            foreach (GameObject obj in selectedObjects)
            {
                Character character = obj.GetComponent<Character>();
                if (character != null && character.IsPlayerCharacter())
                {
                    selectedCharacter = character;
                    break;
                }
            }

            // Если есть выделенный персонаж игрока, открываем его инвентарь
            if (selectedCharacter != null)
            {
                SetCurrentInventory(selectedCharacter.GetInventory(), selectedCharacter);
                ShowInventory();
            }
            else
            {
                // Нет выделенного персонажа - не открываем инвентарь
                Debug.LogWarning("Невозможно открыть инвентарь: не выделен ни один персонаж");
            }
        }
    }

    /// <summary>
    /// Показать инвентарь
    /// </summary>
    public void ShowInventory()
    {
        // Активируем только панель Inventory, не весь Canvas
        if (inventoryPanel != null)
        {
            inventoryPanel.gameObject.SetActive(true);
            isInventoryVisible = true;
            IsAnyInventoryOpen = true;


            foreach (var kvp in equipmentSlotUIs)
            {
                InventorySlotUI slotUI = kvp.Value;
                if (slotUI != null)
                {
                    // Слот существует - обновляем состояние
                }
            }

            // Обновляем отображение
            UpdateInventoryDisplay();
        }
        else
        {

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

            }
        }
    }
}
