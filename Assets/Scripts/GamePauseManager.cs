using UnityEngine;

/// <summary>
/// Менеджер паузы игры для режима строительства
/// </summary>
public class GamePauseManager : MonoBehaviour
{
    private static GamePauseManager instance;

    [Header("Pause Settings")]
    public bool pauseOnBuildMode = true;

    // События паузы
    public System.Action<bool> OnPauseStateChanged;

    // Состояние паузы
    private bool isPaused = false;
    private bool wasPausedBeforeBuild = false;
    private bool isBuildModePause = false;

    public static GamePauseManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject pauseManagerObj = new GameObject("GamePauseManager");
                instance = pauseManagerObj.AddComponent<GamePauseManager>();
                DontDestroyOnLoad(pauseManagerObj);
            }
            return instance;
        }
    }

    void Awake()
    {
        // Singleton pattern
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Подписываемся на события строительства если есть BuildingSystem
        var buildingSystem = FindObjectOfType<ShipBuildingSystem>();
        if (buildingSystem != null)
        {
            // Будем слушать изменения режима строительства через GameUI
        }
    }

    /// <summary>
    /// Установить паузу игры
    /// </summary>
    public void SetPaused(bool paused, string reason = "")
    {
        if (isPaused == paused) return;

        isPaused = paused;
        Time.timeScale = paused ? 0f : 1f;

        // Логируем изменение состояния паузы
        FileLogger.Log($"Game Pause: {(paused ? "ON" : "OFF")}" +
                      (string.IsNullOrEmpty(reason) ? "" : $" - Reason: {reason}"));

        // Вызываем событие
        OnPauseStateChanged?.Invoke(paused);
    }

    /// <summary>
    /// Паузу для режима строительства
    /// </summary>
    public void SetBuildModePause(bool buildModeActive)
    {
        if (!pauseOnBuildMode) return;

        isBuildModePause = buildModeActive;

        if (buildModeActive)
        {
            // Сохраняем текущее состояние паузы
            wasPausedBeforeBuild = isPaused;
            SetPaused(true, "Build Mode Activated");
        }
        else
        {
            // Восстанавливаем предыдущее состояние паузы
            isBuildModePause = false;
            SetPaused(wasPausedBeforeBuild, "Build Mode Deactivated");
        }
    }

    /// <summary>
    /// Переключить паузу
    /// </summary>
    public void TogglePause()
    {
        SetPaused(!isPaused, "Manual Toggle");
    }

    /// <summary>
    /// Получить текущее состояние паузы
    /// </summary>
    public bool IsPaused()
    {
        return isPaused;
    }

    /// <summary>
    /// Проверить, является ли текущая пауза паузой строительства
    /// </summary>
    public bool IsBuildModePause()
    {
        return isBuildModePause;
    }

    /// <summary>
    /// Получить реальное время (не зависит от Time.timeScale)
    /// </summary>
    public float GetRealTime()
    {
        return Time.realtimeSinceStartup;
    }

    void Update()
    {
        // Горячие клавиши для паузы (для отладки)
        if (Input.GetKeyDown(KeyCode.P))
        {
            TogglePause();
        }

        // ESC для выхода из режима строительства
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            var gameUI = FindObjectOfType<GameUI>();
            if (gameUI != null)
            {
                // ESC обрабатывается в ShipBuildingSystem
            }
        }
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            // Восстанавливаем нормальное время при уничтожении
            Time.timeScale = 1f;
        }
    }
}