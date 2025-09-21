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

    void Awake()
    {
        DebugLogger.Log(DebugLogger.LogCategory.GameInit, "GameInitializer Awake started");

        if (autoInitializeBootstrap)
        {
            DebugLogger.Log(DebugLogger.LogCategory.GameInit, "Auto-initializing BootstrapManager");
            EnsureBootstrapManager();
        }
        else
        {
            DebugLogger.Log(DebugLogger.LogCategory.GameInit, "BootstrapManager auto-initialization disabled");
        }

        DebugLogger.Log(DebugLogger.LogCategory.GameInit, "GameInitializer Awake completed");
    }

    void Start()
    {
        DebugLogger.Log(DebugLogger.LogCategory.GameInit, "GameInitializer Start began - initializing all systems");

        if (autoInitializeResolution)
        {
            DebugLogger.Log(DebugLogger.LogCategory.GameInit, "Initializing ResolutionManager");
            EnsureResolutionManager();
        }

        if (autoInitializeUI)
        {
            DebugLogger.Log(DebugLogger.LogCategory.GameInit, "Initializing GameUI");
            EnsureGameUI();
        }

        if (autoInitializeEventSystem)
        {
            DebugLogger.Log(DebugLogger.LogCategory.GameInit, "Initializing EventSystem");
            EnsureEventSystem();
        }

        if (autoInitializeCharacterIcons)
        {
            DebugLogger.Log(DebugLogger.LogCategory.GameInit, "Initializing Character Icons");
            EnsureSimpleCharacterIconsUI();
        }

        if (autoInitializeEnemyTargeting)
        {
            DebugLogger.Log(DebugLogger.LogCategory.GameInit, "Initializing Enemy Targeting System");
            EnsureEnemyTargetingSystem();
            EnsureTargetingInstructions();
        }

        // Создаем простой дебаг дисплей
        DebugLogger.Log(DebugLogger.LogCategory.GameInit, "Creating Simple Debug Display");
        GameObject simpleDebugGO = new GameObject("SimpleDebugDisplay");
        simpleDebugGO.AddComponent<SimpleDebugDisplay>();

        // Создаем дебаг монитор
        DebugLogger.Log(DebugLogger.LogCategory.GameInit, "Creating Debug System Monitor");
        GameObject debugMonitorGO = new GameObject("DebugSystemMonitor");
        debugMonitorGO.AddComponent<DebugSystemMonitor>();

        // Создаем инструкции по отладке
        DebugLogger.Log(DebugLogger.LogCategory.GameInit, "Creating Debug Instructions");
        GameObject debugInstructionsGO = new GameObject("DebugInstructions");
        debugInstructionsGO.AddComponent<DebugInstructions>();

        // Удаляем кнопки Center
        DebugLogger.Log(DebugLogger.LogCategory.GameInit, "Creating RemoveCenterButtons component");
        GameObject removerGO = new GameObject("RemoveCenterButtons");
        removerGO.AddComponent<RemoveCenterButtons>();

        // Добавляем тестовый спавнер персонажей
        DebugLogger.Log(DebugLogger.LogCategory.GameInit, "Creating CharacterSpawnerTest");
        GameObject spawnerGO = new GameObject("CharacterSpawnerTest");
        spawnerGO.AddComponent<CharacterSpawnerTest>();

        // Добавляем тестовый спавнер врагов
        DebugLogger.Log(DebugLogger.LogCategory.GameInit, "Creating EnemySpawnerTest");
        GameObject enemySpawnerGO = new GameObject("EnemySpawnerTest");
        enemySpawnerGO.AddComponent<EnemySpawnerTest>();

        // Добавляем систему обновления персонажей
        DebugLogger.Log(DebugLogger.LogCategory.GameInit, "Creating CharacterRefreshTest");
        GameObject refreshGO = new GameObject("CharacterRefreshTest");
        refreshGO.AddComponent<CharacterRefreshTest>();

        // Добавляем отладчик структуры SKM_Character
        DebugLogger.Log(DebugLogger.LogCategory.GameInit, "Creating SKMCharacterDebugger");
        GameObject debuggerGO = new GameObject("SKMCharacterDebugger");
        debuggerGO.AddComponent<SKMCharacterDebugger>();

        // Добавляем UI для тестирования HP
        DebugLogger.Log(DebugLogger.LogCategory.GameInit, "Initializing HPTestUI");
        EnsureHPTestUI();

        DebugLogger.Log(DebugLogger.LogCategory.GameInit, "GameInitializer Start completed - all systems initialized");
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
        DebugLogger.Log(DebugLogger.LogCategory.Icons, "Ensuring SimpleCharacterIconsUI exists...");

        SimpleCharacterIconsUI characterIconsUI = FindObjectOfType<SimpleCharacterIconsUI>();
        if (characterIconsUI == null)
        {
            DebugLogger.Log(DebugLogger.LogCategory.Icons, "SimpleCharacterIconsUI not found, creating new instance");
            GameObject characterIconsUIGO = new GameObject("SimpleCharacterIconsUI");
            characterIconsUI = characterIconsUIGO.AddComponent<SimpleCharacterIconsUI>();

            DebugLogger.LogComponentState(DebugLogger.LogCategory.Icons, "SimpleCharacterIconsUI", characterIconsUI);
            DebugLogger.Log(DebugLogger.LogCategory.Icons, "SimpleCharacterIconsUI created successfully");
        }
        else
        {
            DebugLogger.Log(DebugLogger.LogCategory.Icons, "SimpleCharacterIconsUI already exists");
            DebugLogger.LogComponentState(DebugLogger.LogCategory.Icons, "SimpleCharacterIconsUI", characterIconsUI);
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
            Debug.Log("GameInitializer: Created HPTestUI");
        }
        else
        {
            Debug.Log("GameInitializer: HPTestUI already exists");
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
            Debug.Log("GameInitializer: Created EnemyTargetingSystem");
        }
        else
        {
            Debug.Log("GameInitializer: EnemyTargetingSystem already exists");
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
            Debug.Log("GameInitializer: Created TargetingInstructions");
        }
        else
        {
            Debug.Log("GameInitializer: TargetingInstructions already exists");
        }
    }
}