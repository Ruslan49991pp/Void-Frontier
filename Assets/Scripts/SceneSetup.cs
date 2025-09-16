using UnityEngine;

/// <summary>
/// Скрипт для быстрой настройки сцены с необходимыми объектами
/// Добавьте этот скрипт на любой GameObject в сцене для автоматической инициализации
/// </summary>
public class SceneSetup : MonoBehaviour
{
    [Header("Setup Options")]
    [SerializeField] private bool setupOnStart = true;
    [SerializeField] private bool createGameUI = true;
    [SerializeField] private bool createEventSystem = true;
    [SerializeField] private bool createGridManager = true;
    [SerializeField] private bool createLocationManager = true;

    void Start()
    {
        if (setupOnStart)
        {
            SetupScene();
        }
    }

    [ContextMenu("Setup Scene")]
    public void SetupScene()
    {
        if (createEventSystem)
        {
            CreateEventSystem();
        }

        if (createGridManager)
        {
            CreateGridManager();
        }

        if (createLocationManager)
        {
            CreateLocationManager();
        }

        if (createGameUI)
        {
            CreateGameUI();
        }
    }

    void CreateEventSystem()
    {
        var existing = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (existing == null)
        {
            var eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    void CreateGridManager()
    {
        var existing = FindObjectOfType<GridManager>();
        if (existing == null)
        {
            var gridManagerGO = new GameObject("GridManager");
            gridManagerGO.AddComponent<GridManager>();
        }
    }

    void CreateLocationManager()
    {
        var existing = FindObjectOfType<LocationManager>();
        if (existing == null)
        {
            var locationManagerGO = new GameObject("LocationManager");
            locationManagerGO.AddComponent<LocationManager>();
        }
    }

    void CreateGameUI()
    {
        var existing = FindObjectOfType<GameUI>();
        if (existing == null)
        {
            var gameUIGO = new GameObject("GameUI");
            gameUIGO.AddComponent<GameUI>();
        }
    }
}