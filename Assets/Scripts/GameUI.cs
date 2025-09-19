using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class BuildingData
{
    public string name;
    public string size;
    public int cost;
}

public class GameUI : MonoBehaviour
{
    [Header("UI References")]
    public Canvas mainCanvas;
    public RectTransform bottomPanel;
    public RectTransform infoArea;      // Слева - информация
    public RectTransform actionArea;    // По центру - действия/строительство
    public RectTransform buttonArea;    // Справа - кнопки
    
    [Header("Building Settings")]
    public List<BuildingData> availableBuildings = new List<BuildingData>();
    
    // Внутренние компоненты
    private Text infoText;
    private SelectionManager selectionManager;
    private List<Button> buildingButtons = new List<Button>();
    private Button buildModeButton;
    private Button destroyModeButton;
    private Button destroyRoomButton;
    
    // Текущее выделение
    private List<GameObject> currentSelection = new List<GameObject>();

    // Состояние UI
    private bool buildModeActive = false;
    private bool deleteModeActive = false;
    private int selectedBuildingIndex = -1;
    private ShipBuildingSystem buildingSystem;
    
    void Awake()
    {
        InitializeUI();
        FindSelectionManager();
    }
    
    void Start()
    {
        if (selectionManager != null)
        {
            selectionManager.OnSelectionChanged += OnSelectionChanged;
        }

        InitializeBuildingSystem();
        SyncBuildingDataWithShipBuildingSystem();
    }
    
    /// <summary>
    /// Инициализация UI системы
    /// </summary>
    void InitializeUI()
    {
        // Создаем главный Canvas если его нет
        if (mainCanvas == null)
        {
            GameObject canvasGO = new GameObject("GameUI_Canvas");
            mainCanvas = canvasGO.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 100; // Поверх других UI

            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        CreateBottomPanel();
    }
    
    /// <summary>
    /// Создание нижней панели
    /// </summary>
    void CreateBottomPanel()
    {
        // Главная нижняя панель
        GameObject bottomPanelGO = new GameObject("BottomPanel");
        bottomPanelGO.transform.SetParent(mainCanvas.transform, false);
        
        bottomPanel = bottomPanelGO.AddComponent<RectTransform>();
        bottomPanel.anchorMin = new Vector2(0, 0);
        bottomPanel.anchorMax = new Vector2(1, 0);
        bottomPanel.pivot = new Vector2(0.5f, 0);
        bottomPanel.anchoredPosition = Vector2.zero;
        bottomPanel.sizeDelta = new Vector2(0, 75); // Высота панели уменьшена в 2 раза
        
        // Фон панели
        Image backgroundImage = bottomPanelGO.AddComponent<Image>();
        backgroundImage.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
        
        // Создаем области внутри панели
        CreateInfoArea();        // Слева - информация о выделенном
        CreateActionArea();      // По центру - действия и строительство
        CreateButtonArea();      // Справа - кнопки
    }
    
    /// <summary>
    /// Создание области выбора строительства (в центральной части)
    /// </summary>
    void CreateBuildingSelectionArea(GameObject parent)
    {
        // Заголовок
        GameObject titleGO = new GameObject("BuildingTitle");
        titleGO.transform.SetParent(parent.transform, false);

        RectTransform titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.05f, 0.8f);
        titleRect.anchorMax = new Vector2(0.95f, 0.95f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        Text titleText = titleGO.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 14;
        titleText.color = Color.white;
        titleText.text = "СТРОИТЕЛЬСТВО";
        titleText.alignment = TextAnchor.MiddleCenter;

        // Контейнер для кнопок зданий
        GameObject containerGO = new GameObject("BuildingContainer");
        containerGO.transform.SetParent(parent.transform, false);

        RectTransform containerRect = containerGO.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.05f, 0.1f);
        containerRect.anchorMax = new Vector2(0.95f, 0.75f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        // Сетка для кнопок зданий
        GridLayoutGroup gridLayout = containerGO.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(80, 60);
        gridLayout.spacing = new Vector2(5, 5);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 3; // 3 колонки
        gridLayout.childAlignment = TextAnchor.MiddleCenter;

        // Создаем кнопки для каждого типа здания
        CreateBuildingButtons(containerGO);
    }
    
    /// <summary>
    /// Создание области информации (слева)
    /// </summary>
    void CreateInfoArea()
    {
        GameObject infoAreaGO = new GameObject("InfoArea");
        infoAreaGO.transform.SetParent(bottomPanel.transform, false);

        infoArea = infoAreaGO.AddComponent<RectTransform>();
        infoArea.anchorMin = new Vector2(0, 0);
        infoArea.anchorMax = new Vector2(0.3f, 1); // 30% ширины слева
        infoArea.pivot = new Vector2(0, 0);
        infoArea.anchoredPosition = Vector2.zero;
        infoArea.sizeDelta = Vector2.zero;

        // Фон области информации
        Image infoBg = infoAreaGO.AddComponent<Image>();
        infoBg.color = new Color(0.05f, 0.1f, 0.05f, 0.5f);

        // Текст информации
        GameObject textGO = new GameObject("InfoText");
        textGO.transform.SetParent(infoAreaGO.transform, false);

        infoText = textGO.AddComponent<Text>();
        infoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        infoText.fontSize = 12;
        infoText.color = Color.white;
        infoText.text = "Выберите объект для просмотра информации";
        infoText.alignment = TextAnchor.UpperLeft;

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 50); // Оставляем место внизу для кнопки
        textRect.offsetMax = new Vector2(-10, -10);

        // Создаем кнопку разрушения комнаты в области информации
        CreateDestroyRoomButton(infoAreaGO);
    }
    
    /// <summary>
    /// Создание области действий (по центру)
    /// </summary>
    void CreateActionArea()
    {
        GameObject actionAreaGO = new GameObject("ActionArea");
        actionAreaGO.transform.SetParent(bottomPanel.transform, false);

        actionArea = actionAreaGO.AddComponent<RectTransform>();
        actionArea.anchorMin = new Vector2(0.3f, 0);
        actionArea.anchorMax = new Vector2(0.7f, 1); // 40% ширины по центру
        actionArea.pivot = new Vector2(0, 0);
        actionArea.anchoredPosition = Vector2.zero;
        actionArea.sizeDelta = Vector2.zero;

        // Фон области действий
        Image actionBg = actionAreaGO.AddComponent<Image>();
        actionBg.color = new Color(0.05f, 0.05f, 0.1f, 0.5f);

        // Создаем область для выбора комнат строительства
        CreateBuildingSelectionArea(actionAreaGO);
    }

    /// <summary>
    /// Создание области кнопок (справа)
    /// </summary>
    void CreateButtonArea()
    {
        GameObject buttonAreaGO = new GameObject("ButtonArea");
        buttonAreaGO.transform.SetParent(bottomPanel.transform, false);

        buttonArea = buttonAreaGO.AddComponent<RectTransform>();
        buttonArea.anchorMin = new Vector2(0.7f, 0);
        buttonArea.anchorMax = new Vector2(1, 1); // 30% ширины справа
        buttonArea.pivot = new Vector2(0, 0);
        buttonArea.anchoredPosition = Vector2.zero;
        buttonArea.sizeDelta = Vector2.zero;

        // Фон области кнопок
        Image buttonBg = buttonAreaGO.AddComponent<Image>();
        buttonBg.color = new Color(0.1f, 0.05f, 0.05f, 0.5f);

        // Создаем кнопки
        CreateBuildButton(buttonAreaGO);
        CreateDestroyButton(buttonAreaGO);

        // Изначально скрываем кнопку разрушения
        if (destroyModeButton != null)
        {
            destroyModeButton.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Создание кнопок для выбора зданий
    /// </summary>
    void CreateBuildingButtons(GameObject parent)
    {
        // Инициализируем список зданий если пустой
        if (availableBuildings.Count == 0)
        {
            InitializeDefaultBuildings();
        }

        buildingButtons.Clear();

        for (int i = 0; i < availableBuildings.Count; i++)
        {
            BuildingData building = availableBuildings[i];
            GameObject buttonGO = new GameObject($"Building_{i}");
            buttonGO.transform.SetParent(parent.transform, false);

            // Фон кнопки
            Image buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            Button button = buttonGO.AddComponent<Button>();
            button.image = buttonImage;

            // Текст кнопки
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);

            Text buttonText = textGO.AddComponent<Text>();
            buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonText.fontSize = 10;
            buttonText.color = Color.white;
            buttonText.text = $"{building.name}\n{building.size}";
            buttonText.alignment = TextAnchor.MiddleCenter;

            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // Назначаем обработчик
            int buildingIndex = i;
            button.onClick.AddListener(() => SelectBuilding(buildingIndex));

            buildingButtons.Add(button);
        }
    }

    /// <summary>
    /// Инициализация стандартных зданий
    /// </summary>
    void InitializeDefaultBuildings()
    {
        availableBuildings.Add(new BuildingData { name = "Коридор", size = "4x10", cost = 80 });
        availableBuildings.Add(new BuildingData { name = "Ангар", size = "10x10", cost = 200 });
        availableBuildings.Add(new BuildingData { name = "Жилой", size = "6x10", cost = 120 });
    }

    /// <summary>
    /// Создание кнопки строительства
    /// </summary>
    void CreateBuildButton(GameObject parent)
    {
        GameObject buttonGO = new GameObject("BuildButton");
        buttonGO.transform.SetParent(parent.transform, false);

        RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.1f, 0.6f);
        buttonRect.anchorMax = new Vector2(0.9f, 0.85f);
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.6f, 0.2f, 1f);

        buildModeButton = buttonGO.AddComponent<Button>();
        buildModeButton.image = buttonImage;
        buildModeButton.onClick.AddListener(ToggleBuildMode);

        // Текст кнопки
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);

        Text buttonText = textGO.AddComponent<Text>();
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 12;
        buttonText.color = Color.white;
        buttonText.text = "РЕЖИМ СТРОИТЕЛЬСТВА";
        buttonText.alignment = TextAnchor.MiddleCenter;

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Создание кнопки разрушения комнаты в области информации
    /// </summary>
    void CreateDestroyRoomButton(GameObject parent)
    {
        GameObject buttonGO = new GameObject("DestroyRoomButton");
        buttonGO.transform.SetParent(parent.transform, false);

        RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.1f, 0);
        buttonRect.anchorMax = new Vector2(0.9f, 0);
        buttonRect.pivot = new Vector2(0.5f, 0);
        buttonRect.anchoredPosition = new Vector2(0, 10);
        buttonRect.sizeDelta = new Vector2(0, 30);

        // Фон кнопки
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);

        // Компонент Button
        destroyRoomButton = buttonGO.AddComponent<Button>();
        destroyRoomButton.image = buttonImage;
        destroyRoomButton.onClick.AddListener(DestroySelectedRoom);

        // Текст кнопки
        GameObject textGO = new GameObject("ButtonText");
        textGO.transform.SetParent(buttonGO.transform, false);

        Text buttonText = textGO.AddComponent<Text>();
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 12;
        buttonText.color = Color.white;
        buttonText.text = "РАЗРУШИТЬ КОМНАТУ";
        buttonText.alignment = TextAnchor.MiddleCenter;

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Изначально скрываем кнопку
        destroyRoomButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// Создание кнопки разрушения
    /// </summary>
    void CreateDestroyButton(GameObject parentArea)
    {
        GameObject buttonGO = new GameObject("DestroyButton");
        buttonGO.transform.SetParent(parentArea.transform, false);

        RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.1f, 0.35f);
        buttonRect.anchorMax = new Vector2(0.9f, 0.55f);
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        // Фон кнопки
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = new Color(0.6f, 0.2f, 0.2f, 1f);

        // Компонент Button
        destroyModeButton = buttonGO.AddComponent<Button>();
        destroyModeButton.image = buttonImage;
        destroyModeButton.onClick.AddListener(ToggleDestroyMode);

        // Текст кнопки
        GameObject textGO = new GameObject("ButtonText");
        textGO.transform.SetParent(buttonGO.transform, false);

        Text buttonText = textGO.AddComponent<Text>();
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 12;
        buttonText.color = Color.white;
        buttonText.text = "РАЗРУШИТЬ";
        buttonText.alignment = TextAnchor.MiddleCenter;

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Инициализация системы строительства
    /// </summary>
    void InitializeBuildingSystem()
    {
        buildingSystem = FindObjectOfType<ShipBuildingSystem>();
        if (buildingSystem == null)
        {
            GameObject buildingSystemGO = new GameObject("ShipBuildingSystem");
            buildingSystem = buildingSystemGO.AddComponent<ShipBuildingSystem>();
        }

        // Подписываемся на события
        if (buildingSystem != null)
        {
            buildingSystem.OnBuildingModeChanged += OnBuildingModeChanged;
            buildingSystem.OnDeletionModeChanged += OnDeletionModeChanged;
            buildingSystem.OnRoomBuilt += OnRoomBuilt;
            buildingSystem.OnRoomDeleted += OnRoomDeleted;
        }
    }

    /// <summary>
    /// Синхронизация данных зданий с ShipBuildingSystem
    /// </summary>
    void SyncBuildingDataWithShipBuildingSystem()
    {
        if (buildingSystem != null && buildingSystem.GetAvailableRooms().Count > 0)
        {
            availableBuildings.Clear();
            var rooms = buildingSystem.GetAvailableRooms();

            foreach (var room in rooms)
            {
                availableBuildings.Add(new BuildingData
                {
                    name = room.roomName,
                    size = $"{room.size.x}x{room.size.y}",
                    cost = room.cost
                });
            }

            // Пересоздаем кнопки зданий с актуальными данными
            if (actionArea != null)
            {
                // Найдем контейнер кнопок и пересоздадим их
                Transform container = actionArea.Find("BuildingContainer");
                if (container != null)
                {
                    CreateBuildingButtons(container.gameObject);
                }
            }
        }
    }

    /// <summary>
    /// Переключение режима строительства
    /// </summary>
    void ToggleBuildMode()
    {
        buildModeActive = !buildModeActive;

        if (buildModeActive)
        {
            // Активируем режим строительства
            UpdateBuildingUI(true);
            // Ставим игру на паузу сразу при входе в режим строительства
            GamePauseManager.Instance.SetBuildModePause(true);
            FileLogger.Log("Build mode activated via UI button - game paused");
        }
        else
        {
            // Деактивируем режим строительства
            selectedBuildingIndex = -1;
            UpdateBuildingUI(false);
            if (buildingSystem != null)
            {
                buildingSystem.SetBuildMode(false);
            }
            // Снимаем паузу при выходе из режима строительства
            GamePauseManager.Instance.SetBuildModePause(false);
            FileLogger.Log("Build mode deactivated via UI button - game unpaused");
        }
    }

    /// <summary>
    /// Отменить выбор здания
    /// </summary>
    public void ClearBuildingSelection()
    {
        selectedBuildingIndex = -1;
        UpdateBuildingButtonsSelection();

        if (infoText != null)
        {
            infoText.text = "Режим строительства активен\n\nВыберите тип комнаты для строительства";
        }
    }

    /// <summary>
    /// Выбор здания для строительства
    /// </summary>
    void SelectBuilding(int buildingIndex)
    {
        selectedBuildingIndex = buildingIndex;
        UpdateBuildingButtonsSelection();

        if (buildingSystem != null)
        {
            buildingSystem.SetSelectedRoomType(buildingIndex);
            buildingSystem.SetBuildMode(true);
        }

        // Обновляем информационную область
        BuildingData building = availableBuildings[buildingIndex];
        if (infoText != null)
        {
            infoText.text = $"Строительство: {building.name}\nРазмер: {building.size}\nСтоимость: {building.cost}\n\nЛКМ - построить\nПКМ - отмена";
        }
    }

    /// <summary>
    /// Переключение режима разрушения
    /// </summary>
    void ToggleDestroyMode()
    {
        if (buildingSystem != null)
        {
            buildingSystem.ToggleDeletionMode();
        }
    }

    /// <summary>
    /// Обновление UI строительства
    /// </summary>
    void UpdateBuildingUI(bool active)
    {
        if (buildModeButton != null)
        {
            buildModeButton.image.color = active ?
                new Color(0.8f, 0.4f, 0.4f, 1f) :
                new Color(0.2f, 0.6f, 0.2f, 1f);

            Text buttonText = buildModeButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = active ? "ВЫЙТИ ИЗ СТРОИТЕЛЬСТВА" : "РЕЖИМ СТРОИТЕЛЬСТВА";
            }
        }

        // Показываем/скрываем кнопки зданий
        foreach (Button button in buildingButtons)
        {
            if (button != null)
            {
                button.gameObject.SetActive(active);
            }
        }

        // Показываем/скрываем кнопку разрушения только в режиме строительства
        if (destroyModeButton != null)
        {
            destroyModeButton.gameObject.SetActive(active);
        }

        if (!active && infoText != null)
        {
            infoText.text = "Выберите объект для просмотра информации";
        }
    }

    /// <summary>
    /// Обновление выделения кнопок зданий
    /// </summary>
    void UpdateBuildingButtonsSelection()
    {
        for (int i = 0; i < buildingButtons.Count; i++)
        {
            if (buildingButtons[i] != null)
            {
                Color buttonColor = (i == selectedBuildingIndex) ?
                    new Color(0.4f, 0.7f, 0.9f, 1f) :
                    new Color(0.3f, 0.3f, 0.3f, 1f);
                buildingButtons[i].image.color = buttonColor;
            }
        }
    }

    /// <summary>
    /// Обработчик изменения режима строительства
    /// </summary>
    void OnBuildingModeChanged()
    {
        if (buildingSystem != null)
        {
            bool systemBuildMode = buildingSystem.IsBuildingModeActive();
            if (buildModeActive != systemBuildMode)
            {
                buildModeActive = systemBuildMode;
                UpdateBuildingUI(buildModeActive);

                // Управляем паузой при любых изменениях режима строительства
                if (buildModeActive)
                {
                    GamePauseManager.Instance.SetBuildModePause(true);
                    FileLogger.Log("Build mode activated externally - game paused");
                }
                else
                {
                    GamePauseManager.Instance.SetBuildModePause(false);
                    FileLogger.Log("Build mode deactivated externally - game unpaused");
                }
            }
        }
    }

    /// <summary>
    /// Обработчик постройки комнаты
    /// </summary>
    void OnRoomBuilt(GameObject room)
    {
        if (infoText != null)
        {
            infoText.text = $"Построено: {room.name}\n\nВыберите новое здание для строительства";
        }

        // Сбрасываем выбор для повторного строительства
        selectedBuildingIndex = -1;
        UpdateBuildingButtonsSelection();
    }

    /// <summary>
    /// Обработчик изменения режима разрушения
    /// </summary>
    void OnDeletionModeChanged()
    {
        if (buildingSystem == null) return;

        deleteModeActive = buildingSystem.IsDeletionModeActive();

        if (destroyModeButton != null)
        {
            if (deleteModeActive)
            {
                destroyModeButton.image.color = new Color(0.8f, 0.4f, 0.4f, 1f);
                destroyModeButton.GetComponentInChildren<Text>().text = "ОТМЕНА";
                if (infoText != null)
                {
                    infoText.text = "Режим разрушения активен!\n\nУправление:\nЛКМ - разрушить комнату\nПКМ/ESC - отмена";
                }
            }
            else
            {
                destroyModeButton.image.color = new Color(0.6f, 0.2f, 0.2f, 1f);
                destroyModeButton.GetComponentInChildren<Text>().text = "РАЗРУШИТЬ";
                if (infoText != null && !buildModeActive)
                {
                    infoText.text = "Выберите объект для просмотра информации";
                }
            }
        }
    }


    /// <summary>
    /// Обработчик удаления комнаты
    /// </summary>
    void OnRoomDeleted(GameObject room)
    {
        // Показываем уведомление
        if (infoText != null)
        {
            infoText.text = $"Комната разрушена: {room.name}";
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
    /// Обработчик изменения выделения
    /// </summary>
    void OnSelectionChanged(List<GameObject> selectedObjects)
    {
        currentSelection = selectedObjects;
        UpdateInfoArea();
    }
    
    /// <summary>
    /// Обновление области информации
    /// </summary>
    void UpdateInfoArea()
    {
        if (infoText == null) return;

        // Не обновляем информацию если активен режим строительства или разрушения
        if (buildModeActive || deleteModeActive) return;

        if (currentSelection.Count == 0)
        {
            infoText.text = "Выберите объект для просмотра информации";
            // Скрываем кнопку разрушения
            if (destroyRoomButton != null)
            {
                destroyRoomButton.gameObject.SetActive(false);
            }
            return;
        }

        // Персонажи теперь не показываются в нижней панели - только объекты
        // Проверяем, есть ли среди выделенных объектов комната
        GameObject selectedRoom = GetSelectedRoom();
        bool hasRoom = selectedRoom != null;

        if (hasRoom)
        {
            // Показываем информацию о комнате
            RoomInfo roomInfo = selectedRoom.GetComponent<RoomInfo>();
            LocationObjectInfo objectInfo = selectedRoom.GetComponent<LocationObjectInfo>();

            string info = $"КОМНАТА: {roomInfo.roomName}\n";
            info += $"Тип: {roomInfo.roomType}\n";
            info += $"Размер: {roomInfo.roomSize.x}x{roomInfo.roomSize.y}\n";
            info += $"Позиция: {roomInfo.gridPosition.x},{roomInfo.gridPosition.y}\n";

            if (objectInfo != null)
            {
                info += $"Прочность: {objectInfo.health}\n";
            }

            infoText.text = info;

            // Показываем кнопку разрушения
            if (destroyRoomButton != null)
            {
                destroyRoomButton.gameObject.SetActive(true);
            }
        }
        else
        {
            // Показываем общую информацию о выделении (только объекты, не персонажи)
            List<GameObject> nonCharacterObjects = new List<GameObject>();
            foreach (GameObject obj in currentSelection)
            {
                // Исключаем персонажей из списка
                if (obj.GetComponent<Character>() == null)
                {
                    nonCharacterObjects.Add(obj);
                }
            }

            if (nonCharacterObjects.Count == 1)
            {
                // Показываем подробную информацию об одном объекте
                GameObject obj = nonCharacterObjects[0];
                LocationObjectInfo objectInfo = obj.GetComponent<LocationObjectInfo>();

                if (objectInfo != null)
                {
                    string info = $"ОБЪЕКТ: {objectInfo.objectName}\n";
                    info += $"Тип: {objectInfo.objectType}\n";
                    info += $"Прочность: {objectInfo.health:F0}\n";
                    if (objectInfo.isDestructible)
                    {
                        info += "Можно разрушить: Да\n";
                    }
                    else
                    {
                        info += "Можно разрушить: Нет\n";
                    }
                    if (objectInfo.canBeScavenged)
                    {
                        info += "Можно собрать ресурсы: Да\n";
                    }
                    info += $"\nПозиция: {obj.transform.position:F1}";

                    infoText.text = info;
                }
                else
                {
                    infoText.text = $"ОБЪЕКТ: {obj.name}\nПозиция: {obj.transform.position:F1}";
                }
            }
            else if (nonCharacterObjects.Count > 1)
            {
                // Показываем список нескольких объектов
                string info = $"Выделено объектов: {nonCharacterObjects.Count}\n\n";
                foreach (GameObject obj in nonCharacterObjects)
                {
                    LocationObjectInfo objectInfo = obj.GetComponent<LocationObjectInfo>();
                    if (objectInfo != null)
                    {
                        info += $"• {objectInfo.objectName} ({objectInfo.objectType})\n";
                    }
                    else
                    {
                        info += $"• {obj.name}\n";
                    }
                }
                infoText.text = info;
            }
            else
            {
                // Если выделены только персонажи или ничего
                infoText.text = "Выберите объект для просмотра информации\n\n(Информация о персонажах отображается в верхней панели)";
            }

            // Скрываем кнопку разрушения
            if (destroyRoomButton != null)
            {
                destroyRoomButton.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Разрушить выделенную комнату
    /// </summary>
    void DestroySelectedRoom()
    {
        // Ищем комнату среди выделенных объектов
        GameObject selectedRoom = GetSelectedRoom();
        if (selectedRoom != null)
        {
            // Используем систему строительства для удаления комнаты
            if (buildingSystem != null)
            {
                // Получаем информацию о комнате
                RoomInfo roomInfo = selectedRoom.GetComponent<RoomInfo>();
                if (roomInfo != null)
                {
                    // Удаляем комнату через ShipBuildingSystem
                    buildingSystem.DeleteRoom(selectedRoom);

                    // Обновляем UI
                    if (infoText != null)
                    {
                        infoText.text = $"Комната '{roomInfo.roomName}' была разрушена";
                    }

                    // Скрываем кнопку разрушения
                    if (destroyRoomButton != null)
                    {
                        destroyRoomButton.gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Получить выделенную комнату из текущего выделения
    /// </summary>
    GameObject GetSelectedRoom()
    {
        foreach (GameObject obj in currentSelection)
        {
            if (obj != null)
            {
                RoomInfo roomInfo = obj.GetComponent<RoomInfo>();
                if (roomInfo != null)
                {
                    return obj;
                }

                // Проверяем также родительские объекты
                Transform current = obj.transform.parent;
                while (current != null)
                {
                    RoomInfo parentRoomInfo = current.GetComponent<RoomInfo>();
                    if (parentRoomInfo != null)
                    {
                        return current.gameObject;
                    }
                    current = current.parent;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Проверить, есть ли среди выделенных объектов комната
    /// </summary>
    bool HasSelectedRoom()
    {
        return GetSelectedRoom() != null;
    }

    void OnDestroy()
    {
        if (selectionManager != null)
        {
            selectionManager.OnSelectionChanged -= OnSelectionChanged;
        }

        if (buildingSystem != null)
        {
            buildingSystem.OnBuildingModeChanged -= OnBuildingModeChanged;
            buildingSystem.OnDeletionModeChanged -= OnDeletionModeChanged;
            buildingSystem.OnRoomBuilt -= OnRoomBuilt;
            buildingSystem.OnRoomDeleted -= OnRoomDeleted;
        }
    }
}