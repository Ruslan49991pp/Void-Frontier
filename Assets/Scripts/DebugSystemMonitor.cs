using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Отладочный UI для мониторинга состояния всех систем игры
/// </summary>
public class DebugSystemMonitor : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool showOnStart = true;
    public KeyCode toggleKey = KeyCode.F12;
    public float updateInterval = 1f;

    // UI элементы
    private Canvas debugCanvas;
    private GameObject debugPanel;
    private Text debugText;
    private ScrollRect scrollRect;

    // Состояние
    private bool isVisible = false;
    private float lastUpdateTime = 0f;

    // Ссылки на системы
    private SelectionManager selectionManager;
    private EnemyTargetingSystem targetingSystem;
    private MovementController movementController;
    private GameInitializer gameInitializer;
    private SimpleCharacterIconsUI characterIcons;

    void Start()
    {
        DebugLogger.Log(DebugLogger.LogCategory.UI, "DebugSystemMonitor Starting...");

        CreateDebugUI();
        FindSystemReferences();

        if (showOnStart)
        {
            ShowDebugPanel();
            // Принудительно обновляем информацию сразу
            UpdateDebugInfo();
        }
        else
        {
            HideDebugPanel();
        }

        DebugLogger.LogSystemInfo();
        DebugLogger.LogAvailableCategories();
    }

    void Update()
    {
        // Переключение видимости по клавише
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleDebugPanel();
        }

        // Обновление информации
        if (isVisible && Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateDebugInfo();
            lastUpdateTime = Time.time;
        }
    }

    /// <summary>
    /// Создать отладочный UI
    /// </summary>
    void CreateDebugUI()
    {
        DebugLogger.Log(DebugLogger.LogCategory.UI, "Creating Debug UI...");

        // Создаем Canvas для дебага
        GameObject canvasGO = new GameObject("DebugCanvas");
        debugCanvas = canvasGO.AddComponent<Canvas>();
        debugCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        debugCanvas.sortingOrder = 1000; // Поверх всего остального

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGO.AddComponent<GraphicRaycaster>();

        // Создаем основную панель
        GameObject panelGO = new GameObject("DebugPanel");
        panelGO.transform.SetParent(debugCanvas.transform, false);
        debugPanel = panelGO;

        RectTransform panelRect = panelGO.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0.5f);
        panelRect.anchorMax = new Vector2(0.4f, 1f);
        panelRect.offsetMin = new Vector2(10, 10);
        panelRect.offsetMax = new Vector2(-10, -10);

        Image panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.9f);

        // Добавляем ScrollRect
        GameObject scrollViewGO = new GameObject("ScrollView");
        scrollViewGO.transform.SetParent(panelGO.transform, false);

        RectTransform scrollRect = scrollViewGO.AddComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = new Vector2(10, 40);
        scrollRect.offsetMax = new Vector2(-10, -10);

        ScrollRect scroll = scrollViewGO.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;

        // Контент для ScrollRect
        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(scrollViewGO.transform, false);

        RectTransform contentRect = contentGO.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        contentRect.pivot = new Vector2(0, 1);

        ContentSizeFitter fitter = contentGO.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scroll.content = contentRect;

        // Текст для информации
        GameObject textGO = new GameObject("DebugText");
        textGO.transform.SetParent(contentGO.transform, false);

        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 10);
        textRect.offsetMax = new Vector2(-10, -10);

        debugText = textGO.AddComponent<Text>();
        debugText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        debugText.fontSize = 14;
        debugText.color = Color.white;
        debugText.alignment = TextAnchor.UpperLeft;
        debugText.text = "DEBUG MONITOR INITIALIZING...";

        // Заголовок
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(panelGO.transform, false);

        RectTransform titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = new Vector2(10, -35);
        titleRect.offsetMax = new Vector2(-10, -5);

        Text titleText = titleGO.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 14;
        titleText.color = Color.yellow;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.text = $"DEBUG MONITOR (Press {toggleKey} to toggle)";
        titleText.fontStyle = FontStyle.Bold;

        DebugLogger.Log(DebugLogger.LogCategory.UI, "Debug UI created successfully");

        // Принудительно обновляем информацию сразу после создания
        if (debugText != null)
        {
            debugText.text = "DEBUG MONITOR READY!\nPress F12 to toggle\nInitializing systems...";
            DebugLogger.Log(DebugLogger.LogCategory.UI, "Debug text initialized with placeholder");
        }
    }

    /// <summary>
    /// Найти ссылки на системы
    /// </summary>
    void FindSystemReferences()
    {
        DebugLogger.Log(DebugLogger.LogCategory.UI, "Finding system references...");

        selectionManager = FindObjectOfType<SelectionManager>();
        targetingSystem = FindObjectOfType<EnemyTargetingSystem>();
        movementController = FindObjectOfType<MovementController>();
        gameInitializer = FindObjectOfType<GameInitializer>();
        characterIcons = FindObjectOfType<SimpleCharacterIconsUI>();

        DebugLogger.Log(DebugLogger.LogCategory.UI,
            $"System references found: " +
            $"Selection={selectionManager != null}, " +
            $"Targeting={targetingSystem != null}, " +
            $"Movement={movementController != null}, " +
            $"GameInit={gameInitializer != null}, " +
            $"Icons={characterIcons != null}");
    }

    /// <summary>
    /// Обновить отладочную информацию
    /// </summary>
    void UpdateDebugInfo()
    {
        if (debugText == null) return;

        var info = new System.Text.StringBuilder();

        info.AppendLine($"=== SYSTEM STATUS ===");
        info.AppendLine($"Time: {System.DateTime.Now:HH:mm:ss}");
        info.AppendLine($"FPS: {(1f / Time.deltaTime):F1}");
        info.AppendLine($"Frame: {Time.frameCount}");
        info.AppendLine();

        // GameInitializer
        info.AppendLine("=== GAME INITIALIZER ===");
        if (gameInitializer != null)
        {
            info.AppendLine($"✓ GameInitializer: ACTIVE");
            info.AppendLine($"  Auto Bootstrap: {gameInitializer.autoInitializeBootstrap}");
            info.AppendLine($"  Auto UI: {gameInitializer.autoInitializeUI}");
            info.AppendLine($"  Auto Targeting: {gameInitializer.autoInitializeEnemyTargeting}");
        }
        else
        {
            info.AppendLine("✗ GameInitializer: MISSING");
        }
        info.AppendLine();

        // Selection Manager
        info.AppendLine("=== SELECTION SYSTEM ===");
        if (selectionManager != null)
        {
            info.AppendLine($"✓ SelectionManager: ACTIVE");
            var selected = selectionManager.GetSelectedObjects();
            info.AppendLine($"  Selected Objects: {selected.Count}");
            info.AppendLine($"  Box Selecting: {selectionManager.IsBoxSelecting}");

            foreach (var obj in selected)
            {
                if (obj != null)
                {
                    Character character = obj.GetComponent<Character>();
                    if (character != null)
                    {
                        info.AppendLine($"    - {character.GetFullName()} ({character.GetFaction()})");
                    }
                    else
                    {
                        info.AppendLine($"    - {obj.name}");
                    }
                }
            }
        }
        else
        {
            info.AppendLine("✗ SelectionManager: MISSING");
        }
        info.AppendLine();

        // Targeting System
        info.AppendLine("=== TARGETING SYSTEM ===");
        if (targetingSystem != null)
        {
            info.AppendLine($"✓ EnemyTargetingSystem: ACTIVE");
            info.AppendLine($"  Debug Mode: {targetingSystem.debugMode}");
            info.AppendLine($"  Follow Distance: {targetingSystem.followDistance}");
            info.AppendLine($"  Update Interval: {targetingSystem.updateInterval}");
        }
        else
        {
            info.AppendLine("✗ EnemyTargetingSystem: MISSING");
        }
        info.AppendLine();

        // Movement Controller
        info.AppendLine("=== MOVEMENT SYSTEM ===");
        if (movementController != null)
        {
            info.AppendLine($"✓ MovementController: ACTIVE");
        }
        else
        {
            info.AppendLine("✗ MovementController: MISSING");
        }
        info.AppendLine();

        // Character Icons
        info.AppendLine("=== CHARACTER ICONS ===");
        if (characterIcons != null)
        {
            info.AppendLine($"✓ SimpleCharacterIconsUI: ACTIVE");
            info.AppendLine($"  Enabled: {characterIcons.enabled}");
            info.AppendLine($"  GameObject Active: {characterIcons.gameObject.activeInHierarchy}");
        }
        else
        {
            info.AppendLine("✗ SimpleCharacterIconsUI: MISSING");
        }
        info.AppendLine();

        // Characters in scene
        info.AppendLine("=== CHARACTERS ===");
        Character[] allCharacters = FindObjectsOfType<Character>();
        info.AppendLine($"Total Characters: {allCharacters.Length}");

        int playerCount = 0, enemyCount = 0, neutralCount = 0;
        foreach (var character in allCharacters)
        {
            switch (character.GetFaction())
            {
                case Faction.Player: playerCount++; break;
                case Faction.Enemy: enemyCount++; break;
                case Faction.Neutral: neutralCount++; break;
            }
        }

        info.AppendLine($"  Players: {playerCount}");
        info.AppendLine($"  Enemies: {enemyCount}");
        info.AppendLine($"  Neutral: {neutralCount}");
        info.AppendLine();

        // System managers
        info.AppendLine("=== CORE SYSTEMS ===");
        GridManager gridManager = FindObjectOfType<GridManager>();
        info.AppendLine($"GridManager: {(gridManager != null ? "✓ ACTIVE" : "✗ MISSING")}");

        SimplePathfinder pathfinder = FindObjectOfType<SimplePathfinder>();
        info.AppendLine($"SimplePathfinder: {(pathfinder != null ? "✓ ACTIVE" : "✗ MISSING")}");

        GameUI gameUI = FindObjectOfType<GameUI>();
        info.AppendLine($"GameUI: {(gameUI != null ? "✓ ACTIVE" : "✗ MISSING")}");

        debugText.text = info.ToString();
    }

    /// <summary>
    /// Показать панель отладки
    /// </summary>
    public void ShowDebugPanel()
    {
        if (debugPanel != null)
        {
            debugPanel.SetActive(true);
            isVisible = true;
            UpdateDebugInfo();
            DebugLogger.Log(DebugLogger.LogCategory.UI, "Debug panel SHOWN");
        }
    }

    /// <summary>
    /// Скрыть панель отладки
    /// </summary>
    public void HideDebugPanel()
    {
        if (debugPanel != null)
        {
            debugPanel.SetActive(false);
            isVisible = false;
            DebugLogger.Log(DebugLogger.LogCategory.UI, "Debug panel HIDDEN");
        }
    }

    /// <summary>
    /// Переключить видимость панели
    /// </summary>
    public void ToggleDebugPanel()
    {
        if (isVisible)
        {
            HideDebugPanel();
        }
        else
        {
            ShowDebugPanel();
        }
    }

    void OnDestroy()
    {
        if (debugCanvas != null)
        {
            DestroyImmediate(debugCanvas.gameObject);
        }
    }
}