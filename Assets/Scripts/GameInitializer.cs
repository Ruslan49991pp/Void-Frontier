using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    [Header("Auto Initialize")]
    public bool autoInitializeBootstrap = true;
    public bool autoInitializeUI = true;
    public bool autoInitializeEventSystem = true;
    public bool autoInitializeResolution = true;
    public bool autoInitializeCharacterIcons = true;

    void Awake()
    {
        if (autoInitializeBootstrap)
        {
            EnsureBootstrapManager();
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

        // Удаляем кнопки Center
        GameObject removerGO = new GameObject("RemoveCenterButtons");
        removerGO.AddComponent<RemoveCenterButtons>();

        // Добавляем тестовый спавнер персонажей
        GameObject spawnerGO = new GameObject("CharacterSpawnerTest");
        spawnerGO.AddComponent<CharacterSpawnerTest>();

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
            Debug.Log("GameInitializer: Created SimpleCharacterIconsUI");
        }
        else
        {
            Debug.Log("GameInitializer: SimpleCharacterIconsUI already exists");
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
}