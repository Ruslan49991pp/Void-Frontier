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
            // Проверяем что instance не уничтожен
            if (instance == null || instance.gameObject == null)
            {
                // Пытаемся найти существующий в сцене
                instance = FindObjectOfType<GamePauseManager>();

                if (instance == null)
                {
                    GameObject pauseManagerObj = new GameObject("GamePauseManager");
                    instance = pauseManagerObj.AddComponent<GamePauseManager>();
                    // Не используем DontDestroyOnLoad - объект будет пересоздаваться в каждой сцене
                }
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
            // Не используем DontDestroyOnLoad для GamePauseManager
            // Он будет пересоздаваться в каждой сцене через GameInitializer
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        FileLogger.Log("[GamePauseManager] Start called");

        // Не инициализируем PauseMenuManager здесь - он будет создан через GameInitializer
        // Это предотвращает создание объектов в OnDestroy при закрытии сцены
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
        // ESC теперь обрабатывается в PauseMenuManager
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            // Восстанавливаем нормальное время при уничтожении
            Time.timeScale = 1f;

            // Очищаем события чтобы не создавать новые объекты
            OnPauseStateChanged = null;

            instance = null;
        }
    }
}