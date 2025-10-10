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

        if (autoInitializeUI)
        {
            EnsureGameUI();
        }

        if (autoInitializeEventSystem)
        {
            EnsureEventSystem();
        }

        if (autoInitializeCharacterIcons)
        {
            EnsureSimpleCharacterIconsUI();
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

        // Создаем простой дебаг дисплей
        GameObject simpleDebugGO = new GameObject("SimpleDebugDisplay");
        simpleDebugGO.AddComponent<SimpleDebugDisplay>();

        // Создаем дебаг монитор
        GameObject debugMonitorGO = new GameObject("DebugSystemMonitor");
        debugMonitorGO.AddComponent<DebugSystemMonitor>();

        // Создаем инструкции по отладке
        GameObject debugInstructionsGO = new GameObject("DebugInstructions");
        debugInstructionsGO.AddComponent<DebugInstructions>();

        // Удаляем кнопки Center
        GameObject removerGO = new GameObject("RemoveCenterButtons");
        removerGO.AddComponent<RemoveCenterButtons>();

        // Добавляем тестовый спавнер персонажей
        GameObject spawnerGO = new GameObject("CharacterSpawnerTest");
        spawnerGO.AddComponent<CharacterSpawnerTest>();

        // Добавляем тестовый спавнер врагов
        GameObject enemySpawnerGO = new GameObject("EnemySpawnerTest");
        enemySpawnerGO.AddComponent<EnemySpawnerTest>();

        // Добавляем систему обновления персонажей
        GameObject refreshGO = new GameObject("CharacterRefreshTest");
        refreshGO.AddComponent<CharacterRefreshTest>();

        // Добавляем отладчик структуры SKM_Character
        GameObject debuggerGO = new GameObject("SKMCharacterDebugger");
        debuggerGO.AddComponent<SKMCharacterDebugger>();

        // Добавляем UI для тестирования HP
        EnsureHPTestUI();
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
    /// Убедиться что SimpleCharacterIconsUI существует в сцене
    /// </summary>
    void EnsureSimpleCharacterIconsUI()
    {
        SimpleCharacterIconsUI characterIconsUI = FindObjectOfType<SimpleCharacterIconsUI>();
        if (characterIconsUI == null)
        {
            GameObject characterIconsUIGO = new GameObject("SimpleCharacterIconsUI");
            characterIconsUI = characterIconsUIGO.AddComponent<SimpleCharacterIconsUI>();
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
}
