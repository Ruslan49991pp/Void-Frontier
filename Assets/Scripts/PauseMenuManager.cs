using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Менеджер меню паузы - управляет Canvas_Popup
/// </summary>
public class PauseMenuManager : MonoBehaviour
{
    private static PauseMenuManager instance;

    [Header("UI References")]
    public GameObject pauseCanvas; // Canvas_Popup

    [Header("Settings")]
    public string pauseCanvasName = "Canvas_Popup";

    private bool isPauseMenuActive = false;

    public static PauseMenuManager Instance
    {
        get
        {
            // Проверяем что instance не уничтожен
            if (instance == null || instance.gameObject == null)
            {
                // Пытаемся найти существующий в сцене
                instance = FindObjectOfType<PauseMenuManager>();

                if (instance == null)
                {
                    GameObject pauseMenuObj = new GameObject("PauseMenuManager");
                    instance = pauseMenuObj.AddComponent<PauseMenuManager>();
                    // Не используем DontDestroyOnLoad - объект будет пересоздаваться в каждой сцене
                }
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // Не используем DontDestroyOnLoad для PauseMenuManager
            // Он будет пересоздаваться в каждой сцене через GameInitializer
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        FileLogger.Log("[PauseMenuManager] Start called");

        FindPauseCanvas();

        // Подписываемся на события паузы
        if (GamePauseManager.Instance != null)
        {
            GamePauseManager.Instance.OnPauseStateChanged += OnPauseStateChanged;
            FileLogger.Log("[PauseMenuManager] Subscribed to GamePauseManager events");
        }

        // Убеждаемся что меню паузы скрыто при старте
        if (pauseCanvas != null)
        {
            pauseCanvas.SetActive(false);
            isPauseMenuActive = false;
            FileLogger.Log("[PauseMenuManager] Pause canvas initialized and hidden");

            // Добавляем компонент для управления UI кнопками паузы
            SetupPauseMenuUI();
        }
    }

    /// <summary>
    /// Настроить UI компонент для меню паузы
    /// </summary>
    void SetupPauseMenuUI()
    {
        if (pauseCanvas == null) return;

        // Проверяем есть ли уже компонент
        PauseMenuUI pauseMenuUI = pauseCanvas.GetComponent<PauseMenuUI>();
        if (pauseMenuUI == null)
        {
            // Добавляем компонент
            pauseMenuUI = pauseCanvas.AddComponent<PauseMenuUI>();
            FileLogger.Log("[PauseMenuManager] PauseMenuUI component added to Canvas_Popup");
        }
        else
        {
            FileLogger.Log("[PauseMenuManager] PauseMenuUI component already exists on Canvas_Popup");
        }
    }

    void Update()
    {
        // ESC для переключения паузы (только если не в режиме строительства)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            FileLogger.Log("[PauseMenuManager] ESC pressed");

            // Проверяем не активен ли режим строительства
            ShipBuildingSystem buildingSystem = FindObjectOfType<ShipBuildingSystem>();
            if (buildingSystem != null && buildingSystem.IsBuildingModeActive())
            {
                // Если в режиме строительства - игнорируем ESC
                // (выход из строительства теперь только через ПКМ)
                FileLogger.Log("[PauseMenuManager] ESC ignored - building mode is active");
                return;
            }

            // Проверяем не активен ли режим размещения главного объекта
            MainObjectPlacementSystem mainObjSystem = FindObjectOfType<MainObjectPlacementSystem>();
            if (mainObjSystem != null && mainObjSystem.IsPlacementActive())
            {
                // Если в режиме размещения объекта - игнорируем ESC
                // (отмена размещения через ПКМ)
                FileLogger.Log("[PauseMenuManager] ESC ignored - main object placement is active");
                return;
            }

            // Переключаем паузу
            FileLogger.Log("[PauseMenuManager] Toggling pause");
            TogglePause();
        }
    }

    /// <summary>
    /// Найти Canvas_Popup на сцене
    /// </summary>
    void FindPauseCanvas()
    {
        if (pauseCanvas == null)
        {
            // Ищем по имени (включая неактивные объекты)
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj.name == pauseCanvasName && obj.scene.isLoaded)
                {
                    pauseCanvas = obj;
                    break;
                }
            }

            if (pauseCanvas == null)
            {
                FileLogger.Log($"[PauseMenuManager] {pauseCanvasName} not found in scene!");
            }
            else
            {
                FileLogger.Log($"[PauseMenuManager] Found {pauseCanvasName}");
            }
        }
    }


    /// <summary>
    /// Переключить паузу
    /// </summary>
    void TogglePause()
    {
        if (GamePauseManager.Instance.IsBuildModePause())
        {
            // Не переключаем паузу если активна пауза строительства
            FileLogger.Log("[PauseMenuManager] Cannot toggle pause - build mode pause is active");
            return;
        }

        bool newPauseState = !GamePauseManager.Instance.IsPaused();
        GamePauseManager.Instance.SetPaused(newPauseState, "ESC Menu Toggle");

        FileLogger.Log($"[PauseMenuManager] Pause toggled: {newPauseState}");
    }

    /// <summary>
    /// Обработчик изменения состояния паузы
    /// </summary>
    void OnPauseStateChanged(bool isPaused)
    {
        // Показываем/скрываем меню паузы только если это не пауза строительства
        if (!GamePauseManager.Instance.IsBuildModePause())
        {
            ShowPauseMenu(isPaused);
        }
    }

    /// <summary>
    /// Показать/скрыть меню паузы
    /// </summary>
    void ShowPauseMenu(bool show)
    {
        if (pauseCanvas == null)
        {
            FindPauseCanvas();
        }

        if (pauseCanvas != null)
        {
            pauseCanvas.SetActive(show);
            isPauseMenuActive = show;
            FileLogger.Log($"[PauseMenuManager] Pause menu {(show ? "shown" : "hidden")}");
        }
        else
        {
            FileLogger.Log("[PauseMenuManager] ERROR: pauseCanvas is null!");
        }
    }

    /// <summary>
    /// Проверить активно ли меню паузы
    /// </summary>
    public bool IsPauseMenuActive()
    {
        return isPauseMenuActive;
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            // Отписываемся от событий (без обращения к Instance чтобы не создавать новые объекты)
            GamePauseManager existingManager = FindObjectOfType<GamePauseManager>();
            if (existingManager != null)
            {
                existingManager.OnPauseStateChanged -= OnPauseStateChanged;
            }

            instance = null;
        }
    }
}
