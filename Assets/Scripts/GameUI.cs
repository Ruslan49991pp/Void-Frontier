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
    private Text enemyInfoText; // Отдельный текст для информации о врагах
    private SelectionManager selectionManager;
    private List<Button> buildingButtons = new List<Button>();
    private List<Button> mainObjectButtons = new List<Button>();
    private Button buildModeButton;
    private Button destroyRoomButton;
    private MainObjectPlacementSystem mainObjectSystem;

    // HP бар для врагов
    private GameObject enemyHealthBarContainer;
    private GameObject healthBarBG;
    private GameObject healthBarFill;
    private Text healthBarText;
    
    // Текущее выделение
    private List<GameObject> currentSelection = new List<GameObject>();

    // Состояние UI
    private bool buildModeActive = false;
    private int selectedBuildingIndex = -1;
    private ShipBuildingSystem buildingSystem;
    
    void Awake()
    {
        // ПОЛНОСТЬЮ ОТКЛЮЧЕНО: Не используем динамический UI
        // InitializeUI();
        // FindSelectionManager();
    }

    void Start()
    {
        // ПОЛНОСТЬЮ ОТКЛЮЧЕНО: Не используем динамический UI
        // if (selectionManager != null)
        // {
        //     selectionManager.OnSelectionChanged += OnSelectionChanged;
        // }
        // else
        // {
        //     FileLogger.Log("[GameUI] WARNING: SelectionManager is null! Cannot subscribe to OnSelectionChanged");
        // }

        // InitializeBuildingSystem();
        // InitializeMainObjectSystem();
        // SyncBuildingDataWithShipBuildingSystem();
    }

    /// <summary>
    /// Инициализация системы размещения главных объектов
    /// </summary>
    void InitializeMainObjectSystem()
    {
        mainObjectSystem = FindObjectOfType<MainObjectPlacementSystem>();
        if (mainObjectSystem == null)
        {
            GameObject go = new GameObject("MainObjectPlacementSystem");
            mainObjectSystem = go.AddComponent<MainObjectPlacementSystem>();
        }

        FileLogger.Log("[GameUI] MainObjectPlacementSystem initialized");
    }

    void Update()
    {
        // ОТКЛЮЧЕНО: Старая система отображения HP врагов
        // Теперь используется SelectionInfoDisplay на SelectionInfoPanel
        // UpdateSelectedEnemyHealthBar();
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
        bottomPanel.sizeDelta = new Vector2(0, 150); // Увеличиваем высоту панели для вмещения всех элементов
        
        // Фон панели
        Image backgroundImage = bottomPanelGO.AddComponent<Image>();
        backgroundImage.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

        // Принудительно обновляем layout перед созданием дочерних элементов
        Canvas.ForceUpdateCanvases();

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

        // Создаем секцию главных объектов
        CreateMainObjectSection(parent);
    }

    /// <summary>
    /// Создание секции главных объектов
    /// </summary>
    void CreateMainObjectSection(GameObject parent)
    {
        // Заголовок для главных объектов (под списком модулей)
        GameObject objTitleGO = new GameObject("MainObjectTitle");
        objTitleGO.transform.SetParent(parent.transform, false);

        RectTransform objTitleRect = objTitleGO.AddComponent<RectTransform>();
        objTitleRect.anchorMin = new Vector2(0.05f, 0.38f);
        objTitleRect.anchorMax = new Vector2(0.95f, 0.45f);
        objTitleRect.offsetMin = Vector2.zero;
        objTitleRect.offsetMax = Vector2.zero;

        Text objTitleText = objTitleGO.AddComponent<Text>();
        objTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        objTitleText.fontSize = 12;
        objTitleText.color = Color.yellow;
        objTitleText.text = "ГЛАВНЫЕ ОБЪЕКТЫ";
        objTitleText.alignment = TextAnchor.MiddleCenter;

        // Контейнер для кнопок главных объектов
        GameObject objContainerGO = new GameObject("MainObjectContainer");
        objContainerGO.transform.SetParent(parent.transform, false);

        RectTransform objContainerRect = objContainerGO.AddComponent<RectTransform>();
        objContainerRect.anchorMin = new Vector2(0.05f, 0.05f);
        objContainerRect.anchorMax = new Vector2(0.95f, 0.35f);
        objContainerRect.offsetMin = Vector2.zero;
        objContainerRect.offsetMax = Vector2.zero;

        // Сетка для кнопок главных объектов
        GridLayoutGroup objGridLayout = objContainerGO.AddComponent<GridLayoutGroup>();
        objGridLayout.cellSize = new Vector2(80, 40);
        objGridLayout.spacing = new Vector2(5, 5);
        objGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        objGridLayout.constraintCount = 3; // 3 колонки
        objGridLayout.childAlignment = TextAnchor.MiddleCenter;

        // Создаем кнопки для главных объектов
        CreateMainObjectButtons(objContainerGO);
    }

    /// <summary>
    /// Создание кнопок для главных объектов
    /// </summary>
    void CreateMainObjectButtons(GameObject parent)
    {
        mainObjectButtons.Clear();

        if (buildingSystem == null || buildingSystem.availableMainObjects.Count == 0)
        {
            FileLogger.Log("[GameUI] No main objects available");
            return;
        }

        for (int i = 0; i < buildingSystem.availableMainObjects.Count; i++)
        {
            MainObjectData objData = buildingSystem.availableMainObjects[i];
            GameObject buttonGO = new GameObject($"MainObj_{i}");
            buttonGO.transform.SetParent(parent.transform, false);

            // Фон кнопки
            Image buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.4f, 0.3f, 0.2f, 1f);

            Button button = buttonGO.AddComponent<Button>();
            button.image = buttonImage;

            // Текст кнопки
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);

            Text buttonText = textGO.AddComponent<Text>();
            buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonText.fontSize = 9;
            buttonText.color = Color.white;
            buttonText.text = $"{objData.objectName}\nHP:{objData.maxHealth}\n{objData.cost}$";
            buttonText.alignment = TextAnchor.MiddleCenter;

            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5, 5);
            textRect.offsetMax = new Vector2(-5, -5);

            // Добавляем обработчик клика
            int index = i;
            button.onClick.AddListener(() => OnMainObjectButtonClick(index));

            mainObjectButtons.Add(button);

            FileLogger.Log($"[GameUI] Created main object button: {objData.objectName}");
        }
    }

    /// <summary>
    /// Обработчик клика на кнопку главного объекта
    /// </summary>
    void OnMainObjectButtonClick(int index)
    {
        if (mainObjectSystem == null || buildingSystem == null) return;

        if (index >= 0 && index < buildingSystem.availableMainObjects.Count)
        {
            MainObjectData objData = buildingSystem.availableMainObjects[index];
            FileLogger.Log($"[GameUI] Main object button clicked: {objData.objectName}");

            // Запускаем режим размещения главного объекта
            mainObjectSystem.StartPlacement(objData);
        }
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
        infoArea.offsetMin = Vector2.zero; // Устанавливаем отступы явно
        infoArea.offsetMax = Vector2.zero;

        // Фон области информации
        Image infoBg = infoAreaGO.AddComponent<Image>();
        infoBg.color = new Color(0.05f, 0.1f, 0.05f, 0.5f);

        // Текст информации
        GameObject textGO = new GameObject("InfoText");
        textGO.transform.SetParent(infoAreaGO.transform, false);

        infoText = textGO.AddComponent<Text>();
        infoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        infoText.fontSize = 14; // Увеличиваем размер шрифта для лучшей видимости
        infoText.color = Color.white;
        infoText.text = "Выберите объект для просмотра информации";
        infoText.alignment = TextAnchor.UpperLeft;

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 80); // Отступ снизу: кнопка (30px) + HP бар (25px) + отступы (25px) = 80px
        textRect.offsetMax = new Vector2(-10, -5); // Небольшой отступ сверху

        // Создаем кнопку разрушения комнаты в области информации
        CreateDestroyRoomButton(infoAreaGO);

        // ВРЕМЕННО ОТКЛЮЧЕНО: Создаем HP бар для врагов (используется SelectionInfoPanel)
        // CreateEnemyHealthBar(infoAreaGO);

        // ВРЕМЕННО ОТКЛЮЧЕНО: Создаем отдельный текст для информации о врагах (используется SelectionInfoPanel)
        // CreateEnemyInfoText(infoAreaGO);
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

        // ВРЕМЕННО СКРЫТО: Создаем область для выбора комнат строительства
        // CreateBuildingSelectionArea(actionAreaGO);

        // Временно скрываем всю панель действий
        actionAreaGO.SetActive(false);
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
        GameObject buttonGO = new GameObject("ShipBuildButton");
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
        buttonText.text = "РАЗРУШИТЬ МОДУЛЬ";
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
    /// Создание HP бара для врагов в области информации
    /// </summary>
    void CreateEnemyHealthBar(GameObject parent)
    {
        try
        {
            // Контейнер для HP бара
            GameObject containerGO = new GameObject("EnemyHealthBarContainer");
            containerGO.transform.SetParent(parent.transform, false);

            enemyHealthBarContainer = containerGO;

            RectTransform containerRect = containerGO.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.05f, 0);
            containerRect.anchorMax = new Vector2(0.95f, 0);
            containerRect.pivot = new Vector2(0.5f, 0);
            containerRect.anchoredPosition = new Vector2(0, 45); // Над кнопкой разрушения
            containerRect.sizeDelta = new Vector2(0, 25);

            // Фон HP бара
            GameObject backgroundGO = new GameObject("HealthBarBG");
            backgroundGO.transform.SetParent(containerGO.transform, false);

            healthBarBG = backgroundGO;
            Image bgImage = backgroundGO.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

            RectTransform bgRect = backgroundGO.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.4f);
            bgRect.anchorMax = new Vector2(1, 1f); // 60% высоты для самого бара
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Заливка HP бара
            GameObject fillGO = new GameObject("HealthBarFill");
            fillGO.transform.SetParent(backgroundGO.transform, false);

            healthBarFill = fillGO;
            Image fillImage = fillGO.AddComponent<Image>();
            fillImage.color = new Color(0.8f, 0.2f, 0.2f, 1f); // Красный цвет для врагов
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;

            RectTransform fillRect = fillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            // Текст HP
            GameObject textGO = new GameObject("HealthBarText");
            textGO.transform.SetParent(containerGO.transform, false);

            healthBarText = textGO.AddComponent<Text>();
            healthBarText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            healthBarText.fontSize = 10;
            healthBarText.color = Color.white;
            healthBarText.text = "";
            healthBarText.alignment = TextAnchor.MiddleCenter;

            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0f);
            textRect.anchorMax = new Vector2(1, 0.4f); // 40% высоты для текста
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // Изначально скрываем HP бар
            enemyHealthBarContainer.SetActive(false);


        }
        catch (System.Exception)
        {
            // Ошибка создания HP бара - игнорируем
        }
    }

    /// <summary>
    /// Создание отдельного текста для информации о врагах
    /// </summary>
    void CreateEnemyInfoText(GameObject parent)
    {
        try
        {
            // Создаем отдельный Canvas для текста врагов НА УРОВНЕ ГЛАВНОГО CANVAS
            GameObject enemyTextGO = new GameObject("EnemyInfoText");
            enemyTextGO.transform.SetParent(mainCanvas.transform, false); // Привязываем к главному Canvas

            // Добавляем Canvas для высокого приоритета отображения
            Canvas enemyCanvas = enemyTextGO.AddComponent<Canvas>();
            enemyCanvas.overrideSorting = true;
            enemyCanvas.sortingOrder = 500; // Очень высокий приоритет, выше всех UI элементов

            // Добавляем GraphicRaycaster
            enemyTextGO.AddComponent<GraphicRaycaster>();

            // Создаем сам текстовый компонент
            enemyInfoText = enemyTextGO.AddComponent<Text>();
            enemyInfoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            enemyInfoText.fontSize = 12; // Меньший размер для компактности
            enemyInfoText.color = Color.white; // Белый цвет на темной плашке
            enemyInfoText.text = "";
            enemyInfoText.alignment = TextAnchor.UpperLeft;

            // Настраиваем позиционирование ТОЛЬКО в области под HP баром
            RectTransform enemyTextRect = enemyTextGO.GetComponent<RectTransform>();
            enemyTextRect.anchorMin = new Vector2(0, 0);
            enemyTextRect.anchorMax = new Vector2(0.3f, 0); // Левые 30% экрана, привязка к низу
            enemyTextRect.anchoredPosition = Vector2.zero;
            enemyTextRect.offsetMin = new Vector2(15, 10); // Небольшой отступ от низа панели
            enemyTextRect.offsetMax = new Vector2(-15, 40); // Заканчиваем где-то под HP баром

            // Изначально скрываем
            enemyTextGO.SetActive(false);


        }
        catch (System.Exception)
        {
            // Ошибка создания HP бара - игнорируем
        }
    }

    /// <summary>
    /// Обновление HP бара врага
    /// </summary>
    void UpdateEnemyHealthBar(Character enemy)
    {



        if (enemyHealthBarContainer == null || healthBarFill == null || healthBarText == null)
        {

            return;
        }

        if (enemy == null || enemy.IsPlayerCharacter())
        {

            // Скрываем HP бар если нет врага или выбран союзник
            if (enemyHealthBarContainer.activeSelf)
            {
                enemyHealthBarContainer.SetActive(false);

            }
            return;
        }


        // Показываем HP бар
        if (!enemyHealthBarContainer.activeSelf)
        {
            enemyHealthBarContainer.SetActive(true);

        }

        // Вычисляем процент здоровья
        float healthPercent = enemy.GetHealthPercent();
        float currentHealth = enemy.GetHealth();
        float maxHealth = enemy.GetMaxHealth();



        // Обновляем заливку бара
        Image fillImage = healthBarFill.GetComponent<Image>();
        if (fillImage != null)
        {
            fillImage.fillAmount = healthPercent;

            // Меняем цвет в зависимости от уровня здоровья
            if (healthPercent > 0.6f)
                fillImage.color = new Color(0.2f, 0.8f, 0.2f, 1f); // Зеленый
            else if (healthPercent > 0.3f)
                fillImage.color = new Color(0.8f, 0.8f, 0.2f, 1f); // Желтый
            else
                fillImage.color = new Color(0.8f, 0.2f, 0.2f, 1f); // Красный


        }
        else
        {

        }

        // Обновляем текст HP
        if (healthBarText != null)
        {
            string hpText = $"HP: {currentHealth:F0}/{maxHealth:F0}";
            healthBarText.text = hpText;

        }
        else
        {

        }
    }

    /// <summary>
    /// Обновление HP бара для текущего выделенного врага (вызывается в Update)
    /// </summary>
    void UpdateSelectedEnemyHealthBar()
    {
        // Проверяем, активен ли HP бар
        if (enemyHealthBarContainer == null || !enemyHealthBarContainer.activeSelf)
            return;

        // Ищем выделенного вражеского персонажа
        Character selectedEnemy = GetSelectedEnemy();
        if (selectedEnemy != null)
        {
            UpdateEnemyHealthBar(selectedEnemy);
        }
    }

    /// <summary>
    /// Получить выделенного вражеского персонажа
    /// </summary>
    Character GetSelectedEnemy()
    {
        if (currentSelection.Count == 1)
        {
            GameObject obj = currentSelection[0];
            Character character = obj.GetComponent<Character>();
            if (character != null && !character.IsPlayerCharacter())
            {
                return character;
            }
        }
        return null;
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
            bool wasBuildingModeActive = buildingSystem.IsBuildingModeActive();
            FileLogger.Log($"[SelectBuilding] buildingIndex: {buildingIndex}, wasBuildingModeActive: {wasBuildingModeActive}");

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


        if (selectionManager == null)
        {

        }
        else
        {

        }
    }
    
    /// <summary>
    /// Обработчик изменения выделения
    /// </summary>
    void OnSelectionChanged(List<GameObject> selectedObjects)
    {
        // Проверяем, что GameUI полностью инициализирован
        if (infoText == null || !gameObject.activeInHierarchy)
        {
            return;
        }

        if (selectedObjects != null && selectedObjects.Count > 0)
        {
            for (int i = 0; i < selectedObjects.Count; i++)
            {
                GameObject obj = selectedObjects[i];
                Character character = obj?.GetComponent<Character>();

            }
        }

        currentSelection = selectedObjects;
        UpdateInfoArea();
    }
    
    /// <summary>
    /// Обновление области информации
    /// </summary>
    void UpdateInfoArea()
    {
        if (infoText == null || !gameObject.activeInHierarchy)
        {
            return;
        }

        // Не обновляем информацию если активен режим строительства
        if (buildModeActive)
        {
            return;
        }

        if (currentSelection.Count == 0)
        {
            infoText.text = "Выберите объект для просмотра информации";
            infoText.color = Color.white; // Возвращаем белый цвет для обычного текста
            infoText.fontSize = 14; // Обычный размер

            // Скрываем текст врагов
            if (enemyInfoText != null)
            {
                enemyInfoText.gameObject.SetActive(false);
            }

            // Скрываем кнопку разрушения
            if (destroyRoomButton != null)
            {
                destroyRoomButton.gameObject.SetActive(false);
            }
            // ОТКЛЮЧЕНО: Скрытие HP бара старой системой
            // UpdateEnemyHealthBar(null);
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

            // Скрываем текст врагов для комнат
            if (enemyInfoText != null)
            {
                enemyInfoText.gameObject.SetActive(false);
            }

            // Показываем кнопку разрушения
            if (destroyRoomButton != null)
            {
                destroyRoomButton.gameObject.SetActive(true);
            }
            // ОТКЛЮЧЕНО: Скрытие HP бара для комнат
            // UpdateEnemyHealthBar(null);
        }
        else
        {


            // Проверяем, что выделено
            if (currentSelection.Count == 1)
            {
                GameObject obj = currentSelection[0];
                Character character = obj.GetComponent<Character>();



                if (character != null)
                {
                    // Показываем информацию только о вражеских персонажах
                    // Информация о союзниках отображается в верхней панели с иконками
                    if (!character.IsPlayerCharacter())
                    {
                        string info = $"ВРАЖЕСКИЙ ПЕРСОНАЖ: {character.GetFullName()}\n";
                        info += $"Профессия: {character.characterData.profession}\n";
                        info += $"Уровень: {character.characterData.level}\n";
                        info += $"Здоровье: {character.characterData.health:F0}/{character.characterData.maxHealth:F0}\n";
                        info += $"Фракция: {character.characterData.faction}\n";

                        if (!string.IsNullOrEmpty(character.characterData.bio))
                        {
                            info += $"\n{character.characterData.bio}";
                        }

                        info += $"\nПозиция: {obj.transform.position:F1}";

                        // Скрываем обычный текст и используем специальный текст для врагов
                        infoText.text = "";

                        // Показываем информацию во втором текстовом элементе
                        if (enemyInfoText != null)
                        {
                            enemyInfoText.text = info;
                            enemyInfoText.gameObject.SetActive(true);
                        }

                        // Дополнительная диагностика UI








                        // ОТКЛЮЧЕНО: Обновление HP бара старой системой
                        // Теперь используется SelectionInfoDisplay
                        // UpdateEnemyHealthBar(character);

                        // Скрываем кнопку разрушения для персонажей
                        if (destroyRoomButton != null)
                        {
                            destroyRoomButton.gameObject.SetActive(false);
                        }
                        return; // Завершаем обработку для врага
                    }
                    else
                    {
                        // Для союзников показываем сообщение о том, что информация отображается вверху
                        infoText.text = "Информация о союзных персонажах\nотображается в верхней панели с иконками";

                        // Скрываем текст врагов для союзников
                        if (enemyInfoText != null)
                        {
                            enemyInfoText.gameObject.SetActive(false);
                        }

                        // ОТКЛЮЧЕНО: Скрытие HP бара для союзников
                        // UpdateEnemyHealthBar(null);
                    }
                }
                else
                {
                    // Проверяем, является ли это предметом инвентаря
                    Item item = obj.GetComponent<Item>();
                    if (item != null && item.itemData != null)
                    {
                        ItemData itemData = item.itemData;
                        string info = $"ПРЕДМЕТ: {itemData.itemName}\n";
                        info += $"Тип: {itemData.itemType}\n";
                        info += $"Редкость: {itemData.rarity}\n";

                        if (!string.IsNullOrEmpty(itemData.description))
                        {
                            info += $"\n{itemData.description}\n";
                        }

                        if (itemData.damage > 0)
                            info += $"\nУрон: {itemData.damage}";
                        if (itemData.armor > 0)
                            info += $"\nЗащита: {itemData.armor}";
                        if (itemData.healing > 0)
                            info += $"\nЛечение: {itemData.healing}";

                        if (itemData.equipmentSlot != EquipmentSlot.None)
                            info += $"\nСлот: {itemData.GetEquipmentSlotName()}";

                        info += $"\nВес: {itemData.weight}";
                        info += $"\nЦенность: {itemData.value}";

                        if (itemData.maxStackSize > 1)
                            info += $"\nМакс. стек: {itemData.maxStackSize}";

                        infoText.text = info;

                        // Скрываем текст врагов для предметов
                        if (enemyInfoText != null)
                        {
                            enemyInfoText.gameObject.SetActive(false);
                        }

                        // ОТКЛЮЧЕНО: Скрытие HP бара для предметов
                        // UpdateEnemyHealthBar(null);
                    }
                    else
                    {
                        // Показываем информацию об обычном объекте
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

                            // Скрываем текст врагов для обычных объектов
                            if (enemyInfoText != null)
                            {
                                enemyInfoText.gameObject.SetActive(false);
                            }

                            // ОТКЛЮЧЕНО: Скрытие HP бара для обычных объектов
                            // UpdateEnemyHealthBar(null);
                        }
                        else
                        {
                            infoText.text = $"ОБЪЕКТ: {obj.name}\nПозиция: {obj.transform.position:F1}";

                            // Скрываем текст врагов для неизвестных объектов
                            if (enemyInfoText != null)
                            {
                                enemyInfoText.gameObject.SetActive(false);
                            }

                            // ОТКЛЮЧЕНО: Скрытие HP бара для обычных объектов
                            // UpdateEnemyHealthBar(null);
                        }
                    }
                }
            }
            else if (currentSelection.Count > 1)
            {
                // Разделяем на союзников и остальные объекты
                List<Character> enemyCharacters = new List<Character>();
                List<GameObject> otherObjects = new List<GameObject>();

                foreach (GameObject obj in currentSelection)
                {
                    Character character = obj.GetComponent<Character>();
                    if (character != null)
                    {
                        if (!character.IsPlayerCharacter())
                        {
                            enemyCharacters.Add(character);
                        }
                        // Союзников пропускаем - их информация отображается в верхней панели
                    }
                    else
                    {
                        otherObjects.Add(obj);
                    }
                }

                string info = "";

                // Показываем информацию о вражеских персонажах
                if (enemyCharacters.Count > 0)
                {
                    info += $"Вражеские персонажи: {enemyCharacters.Count}\n";
                    foreach (Character enemy in enemyCharacters)
                    {
                        info += $"• {enemy.GetFullName()} (Lv.{enemy.characterData.level})\n";
                    }
                    info += "\n";
                }

                // Показываем информацию о других объектах
                if (otherObjects.Count > 0)
                {
                    info += $"Объекты: {otherObjects.Count}\n";
                    foreach (GameObject obj in otherObjects)
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
                }

                // Если есть союзники в выделении, добавляем напоминание
                int totalSelected = currentSelection.Count;
                int displayedCount = enemyCharacters.Count + otherObjects.Count;
                if (displayedCount < totalSelected)
                {
                    int allyCount = totalSelected - displayedCount;
                    info += $"\nСоюзники ({allyCount}): информация в верхней панели";
                }

                infoText.text = info.Trim();
            }

            // Скрываем кнопку разрушения
            if (destroyRoomButton != null)
            {
                destroyRoomButton.gameObject.SetActive(false);
            }
            // ОТКЛЮЧЕНО: Скрытие HP бара для множественного выделения
            // UpdateEnemyHealthBar(null);
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

                    // Очищаем выделение чтобы исчез красный кружок
                    if (selectionManager != null)
                    {
                        selectionManager.ClearSelection();
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
            buildingSystem.OnRoomBuilt -= OnRoomBuilt;
            buildingSystem.OnRoomDeleted -= OnRoomDeleted;
        }
    }
}
