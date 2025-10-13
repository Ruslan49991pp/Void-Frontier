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

    void Awake()
    {
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
            EnsureSelectionInfoDisplay();
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
        FileLogger.Log("[GameInitializer] Initializing pause system");

        // Инициализируем GamePauseManager
        if (GamePauseManager.Instance != null)
        {
            FileLogger.Log("[GameInitializer] GamePauseManager initialized");
        }

        // Инициализируем PauseMenuManager
        if (PauseMenuManager.Instance != null)
        {
            FileLogger.Log("[GameInitializer] PauseMenuManager initialized");
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
    /// Убедиться что SelectionInfoDisplay существует в сцене и правильно настроен
    /// </summary>
    void EnsureSelectionInfoDisplay()
    {
        FileLogger.Log("[GameInitializer] Ensuring SelectionInfoDisplay exists");

        // Ищем SelectionInfoDisplay в сцене
        SelectionInfoDisplay selectionInfoDisplay = FindObjectOfType<SelectionInfoDisplay>();

        if (selectionInfoDisplay == null)
        {
            FileLogger.Log("[GameInitializer] SelectionInfoDisplay not found in scene, looking for SelectionInfoPanel");

            // Ищем SelectionInfoPanel на Canvas_MainUI
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>(true);
            GameObject selectionInfoPanel = null;

            foreach (GameObject obj in allObjects)
            {
                if (obj.name == "SelectionInfoPanel")
                {
                    selectionInfoPanel = obj;
                    FileLogger.Log($"[GameInitializer] Found SelectionInfoPanel: {obj.name}");
                    break;
                }
            }

            if (selectionInfoPanel != null)
            {
                FileLogger.Log($"[GameInitializer] SelectionInfoPanel active state: {selectionInfoPanel.activeSelf}");

                // Добавляем компонент SelectionInfoDisplay если его нет
                selectionInfoDisplay = selectionInfoPanel.GetComponent<SelectionInfoDisplay>();
                if (selectionInfoDisplay == null)
                {
                    // Активируем панель перед добавлением компонента чтобы вызвался Awake()
                    bool wasActive = selectionInfoPanel.activeSelf;
                    if (!wasActive)
                    {
                        selectionInfoPanel.SetActive(true);
                        FileLogger.Log("[GameInitializer] Activated SelectionInfoPanel to add component");
                    }

                    selectionInfoDisplay = selectionInfoPanel.AddComponent<SelectionInfoDisplay>();
                    FileLogger.Log("[GameInitializer] Added SelectionInfoDisplay component to SelectionInfoPanel");

                    // Деактивируем панель обратно если была неактивной
                    if (!wasActive)
                    {
                        selectionInfoPanel.SetActive(false);
                        FileLogger.Log("[GameInitializer] Deactivated SelectionInfoPanel after adding component");
                    }
                }
                else
                {
                    FileLogger.Log("[GameInitializer] SelectionInfoDisplay component already exists on SelectionInfoPanel");

                    // Активируем панель на момент инициализации чтобы вызвался Awake() и Start()
                    if (!selectionInfoPanel.activeSelf)
                    {
                        selectionInfoPanel.SetActive(true);
                        FileLogger.Log("[GameInitializer] Temporarily activated SelectionInfoPanel to initialize component");

                        // Деактивируем панель обратно после небольшой задержки
                        // Используем корутину на GameInitializer (активном объекте)
                        StartCoroutine(DeactivatePanelAfterDelay(selectionInfoPanel, 0.1f));
                    }
                }
            }
            else
            {
                FileLogger.LogError("[GameInitializer] SelectionInfoPanel not found in scene!");
            }
        }
        else
        {
            FileLogger.Log("[GameInitializer] SelectionInfoDisplay found in scene");
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
            FileLogger.Log("[GameInitializer] Deactivated SelectionInfoPanel after initialization");
        }
    }
}
