using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    [Header("Auto Initialize")]
    public bool autoInitializeBootstrap = true;
    public bool autoInitializeUI = true;
    public bool autoInitializeEventSystem = true;
    public bool autoInitializeResolution = true;
    public bool autoInitializeCharacterIcons = true;
    public bool autoInitializeEnemyTargeting = true;
    public bool autoInitializeInventory = true;
    public bool autoInitializePauseSystem = true;
    public bool autoInitializeSelectionInfoDisplay = true;
    public bool autoInitializeObjectSelectDisplay = true;
    public bool autoInitializeItemFactory = true;

    void Awake()
    {
        if (autoInitializeItemFactory)
        {
            EnsureItemFactory();
        }

        if (autoInitializeBootstrap)
        {
            EnsureBootstrapManager();
        }

        if (autoInitializePauseSystem)
        {
            EnsurePauseSystem();
        }
    }

    void Start()
    {
        if (autoInitializeResolution)
        {
            EnsureResolutionManager();
        }

        // ОТКЛЮЧЕНО: Не используем динамически генерируемый UI
        // if (autoInitializeUI)
        // {
        //     EnsureGameUI();
        // }

        if (autoInitializeEventSystem)
        {
            EnsureEventSystem();
        }

        if (autoInitializeCharacterIcons)
        {
            EnsureCanvasCharacterIconsManager();
        }

        if (autoInitializeEnemyTargeting)
        {
            EnsureEnemyTargetingSystem();
            EnsureTargetingInstructions();
        }

        if (autoInitializeInventory)
        {
            EnsureInventoryManager();
        }

        if (autoInitializeSelectionInfoDisplay)
        {
            EnsureEnemySelectDisplay();
        }

        if (autoInitializeObjectSelectDisplay)
        {
            EnsureObjectSelectDisplay();
        }

        // ОТКЛЮЧЕНО: Весь динамический UI не используется
        // // Создаем простой дебаг дисплей
        // GameObject simpleDebugGO = new GameObject("SimpleDebugDisplay");
        // simpleDebugGO.AddComponent<SimpleDebugDisplay>();

        // // Создаем дебаг монитор
        // GameObject debugMonitorGO = new GameObject("DebugSystemMonitor");
        // debugMonitorGO.AddComponent<DebugSystemMonitor>();

        // // Создаем инструкции по отладке
        // GameObject debugInstructionsGO = new GameObject("DebugInstructions");
        // debugInstructionsGO.AddComponent<DebugInstructions>();

        // // Удаляем кнопки Center
        // GameObject removerGO = new GameObject("RemoveCenterButtons");
        // removerGO.AddComponent<RemoveCenterButtons>();

        // Добавляем тестовый спавнер персонажей
        GameObject spawnerGO = new GameObject("CharacterSpawnerTest");
        spawnerGO.AddComponent<CharacterSpawnerTest>();

        // Добавляем тестовый спавнер врагов
        GameObject enemySpawnerGO = new GameObject("EnemySpawnerTest");
        enemySpawnerGO.AddComponent<EnemySpawnerTest>();

        // // Добавляем систему обновления персонажей
        // GameObject refreshGO = new GameObject("CharacterRefreshTest");
        // refreshGO.AddComponent<CharacterRefreshTest>();

        // // Добавляем отладчик структуры SKM_Character
        // GameObject debuggerGO = new GameObject("SKMCharacterDebugger");
        // debuggerGO.AddComponent<SKMCharacterDebugger>();

        // ОТКЛЮЧЕНО: Не используем динамический UI
        // // Добавляем UI для тестирования HP
        // EnsureHPTestUI();

        // // ВРЕМЕННО: Скрываем панель строительства
        // GameObject hideActionAreaGO = new GameObject("HideActionArea");
        // hideActionAreaGO.AddComponent<HideActionArea>();
    }

    /// <summary>
    /// Убедиться что BootstrapManager существует в сцене
    /// </summary>
    void EnsureBootstrapManager()
    {
        BootstrapManager bootstrapManager = FindObjectOfType<BootstrapManager>();
        if (bootstrapManager == null)
        {
            GameObject bootstrapManagerGO = new GameObject("BootstrapManager");
            bootstrapManager = bootstrapManagerGO.AddComponent<BootstrapManager>();
        }
    }

    /// <summary>
    /// Убедиться что система паузы инициализирована
    /// </summary>
    void EnsurePauseSystem()
    {
        // Инициализируем GamePauseManager
        if (GamePauseManager.Instance != null)
        {
            // Initialized
        }

        // Инициализируем PauseMenuManager
        if (PauseMenuManager.Instance != null)
        {
            // Initialized
        }
    }

    /// <summary>
    /// Убедиться что ResolutionManager существует в сцене
    /// </summary>
    void EnsureResolutionManager()
    {
        ResolutionManager resolutionManager = FindObjectOfType<ResolutionManager>();
        if (resolutionManager == null)
        {
            GameObject resolutionManagerGO = new GameObject("ResolutionManager");
            resolutionManager = resolutionManagerGO.AddComponent<ResolutionManager>();
        }
    }

    /// <summary>
    /// Убедиться что GameUI существует в сцене
    /// </summary>
    void EnsureGameUI()
    {
        GameUI gameUI = FindObjectOfType<GameUI>();
        if (gameUI == null)
        {
            GameObject gameUIGO = new GameObject("GameUI");
            gameUI = gameUIGO.AddComponent<GameUI>();
        }
    }

    /// <summary>
    /// Убедиться что EventSystem существует для UI
    /// </summary>
    void EnsureEventSystem()
    {
        UnityEngine.EventSystems.EventSystem eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystem = eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    /// <summary>
    /// Убедиться что CanvasCharacterIconsManager существует в сцене
    /// </summary>
    void EnsureCanvasCharacterIconsManager()
    {
        CanvasCharacterIconsManager iconManager = FindObjectOfType<CanvasCharacterIconsManager>();
        if (iconManager == null)
        {
            GameObject iconManagerGO = new GameObject("CanvasCharacterIconsManager");
            iconManager = iconManagerGO.AddComponent<CanvasCharacterIconsManager>();

            // Загружаем префаб CharacterPortrait
            GameObject prefab = Resources.Load<GameObject>("Prefabs/UI/CharacterPortrait");
            if (prefab == null)
            {
                // Пробуем без папки Resources
                prefab = UnityEngine.Object.FindObjectOfType<GameObject>();
            }
            iconManager.characterPortraitPrefab = prefab;
        }
    }

    /// <summary>
    /// Убедиться что HPTestUI существует в сцене
    /// </summary>
    void EnsureHPTestUI()
    {
        HPTestUI hpTestUI = FindObjectOfType<HPTestUI>();
        if (hpTestUI == null)
        {
            GameObject hpTestUIGO = new GameObject("HPTestUI");
            hpTestUI = hpTestUIGO.AddComponent<HPTestUI>();
        }
    }

    /// <summary>
    /// Убедиться что EnemyTargetingSystem существует в сцене
    /// </summary>
    void EnsureEnemyTargetingSystem()
    {
        EnemyTargetingSystem enemyTargetingSystem = FindObjectOfType<EnemyTargetingSystem>();
        if (enemyTargetingSystem == null)
        {
            GameObject enemyTargetingGO = new GameObject("EnemyTargetingSystem");
            enemyTargetingSystem = enemyTargetingGO.AddComponent<EnemyTargetingSystem>();
        }
    }

    /// <summary>
    /// Убедиться что TargetingInstructions существует в сцене
    /// </summary>
    void EnsureTargetingInstructions()
    {
        TargetingInstructions targetingInstructions = FindObjectOfType<TargetingInstructions>();
        if (targetingInstructions == null)
        {
            GameObject instructionsGO = new GameObject("TargetingInstructions");
            targetingInstructions = instructionsGO.AddComponent<TargetingInstructions>();
        }
    }

    /// <summary>
    /// Убедиться что InventoryManager существует в сцене
    /// </summary>
    void EnsureInventoryManager()
    {
        InventoryManager inventoryManager = FindObjectOfType<InventoryManager>();
        if (inventoryManager == null)
        {
            GameObject inventoryManagerGO = new GameObject("InventoryManager");
            inventoryManager = inventoryManagerGO.AddComponent<InventoryManager>();
        }
    }

    /// <summary>
    /// Убедиться что EnemySelectDisplay существует в сцене и правильно настроен
    /// </summary>
    void EnsureEnemySelectDisplay()
    {
        // Ищем EnemySelectDisplay в сцене
        EnemySelectDisplay enemySelectDisplay = FindObjectOfType<EnemySelectDisplay>();

        if (enemySelectDisplay == null)
        {
            // Ищем EnemySelect на Canvas_MainUI
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>(true);
            GameObject enemySelectPanel = null;

            foreach (GameObject obj in allObjects)
            {
                if (obj.name == "EnemySelect")
                {
                    enemySelectPanel = obj;
                    break;
                }
            }

            if (enemySelectPanel != null)
            {
                // Добавляем компонент EnemySelectDisplay если его нет
                enemySelectDisplay = enemySelectPanel.GetComponent<EnemySelectDisplay>();
                if (enemySelectDisplay == null)
                {
                    // Активируем панель перед добавлением компонента чтобы вызвался Awake()
                    bool wasActive = enemySelectPanel.activeSelf;
                    if (!wasActive)
                    {
                        enemySelectPanel.SetActive(true);
                    }

                    enemySelectDisplay = enemySelectPanel.AddComponent<EnemySelectDisplay>();

                    // Деактивируем панель обратно если была неактивной
                    if (!wasActive)
                    {
                        enemySelectPanel.SetActive(false);
                    }
                }
                else
                {
                    // Активируем панель на момент инициализации чтобы вызвался Awake() и Start()
                    if (!enemySelectPanel.activeSelf)
                    {
                        enemySelectPanel.SetActive(true);

                        // Деактивируем панель обратно после небольшой задержки
                        // Используем корутину на GameInitializer (активном объекте)
                        StartCoroutine(DeactivatePanelAfterDelay(enemySelectPanel, 0.1f));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Убедиться что ObjectSelectDisplay существует в сцене и правильно настроен
    /// </summary>
    void EnsureObjectSelectDisplay()
    {
        // Ищем ObjectSelectDisplay в сцене
        ObjectSelectDisplay objectSelectDisplay = FindObjectOfType<ObjectSelectDisplay>();

        if (objectSelectDisplay == null)
        {
            // Ищем ObjectSelect на Canvas_MainUI
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>(true);
            GameObject objectSelectPanel = null;

            foreach (GameObject obj in allObjects)
            {
                if (obj.name == "ObjectSelect")
                {
                    objectSelectPanel = obj;
                    break;
                }
            }

            if (objectSelectPanel != null)
            {
                // Добавляем компонент ObjectSelectDisplay если его нет
                objectSelectDisplay = objectSelectPanel.GetComponent<ObjectSelectDisplay>();
                if (objectSelectDisplay == null)
                {
                    // Активируем панель перед добавлением компонента чтобы вызвался Awake(), Start() и OnEnable()
                    bool wasActive = objectSelectPanel.activeSelf;
                    if (!wasActive)
                    {
                        objectSelectPanel.SetActive(true);
                    }

                    objectSelectDisplay = objectSelectPanel.AddComponent<ObjectSelectDisplay>();
                    // Компонент сам скроет панель в Start()
                }
                else
                {
                    // Активируем панель на момент инициализации чтобы вызвался Start() и OnEnable()
                    if (!objectSelectPanel.activeSelf)
                    {
                        objectSelectPanel.SetActive(true);
                        // Компонент сам скроет панель в Start()
                    }
                }
            }
        }
    }

    /// <summary>
    /// Корутина для деактивации панели после задержки
    /// </summary>
    System.Collections.IEnumerator DeactivatePanelAfterDelay(GameObject panel, float delay)
    {
        yield return new UnityEngine.WaitForSeconds(delay);
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    /// <summary>
    /// Убедиться что ItemFactory инициализирован с ItemDatabase
    /// </summary>
    void EnsureItemFactory()
    {
        // Загружаем ItemDatabase из Resources
        ItemDatabase itemDatabase = Resources.Load<ItemDatabase>("ItemDatabase");

        if (itemDatabase == null)
        {
            Debug.LogWarning("[GameInitializer] ItemDatabase not found in Resources! Create it via Assets -> Create -> Inventory -> Item Database");
            return;
        }

        // Инициализируем ItemFactory
        ItemFactory.Initialize(itemDatabase);
        Debug.Log("[GameInitializer] ItemFactory initialized with ItemDatabase");
    }
}
