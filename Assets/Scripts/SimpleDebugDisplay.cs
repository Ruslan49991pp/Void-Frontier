using UnityEngine;

/// <summary>
/// Простой дебаг дисплей без сложного UI - просто текст на экране
/// </summary>
public class SimpleDebugDisplay : MonoBehaviour
{
    [Header("Settings")]
    public KeyCode toggleKey = KeyCode.F1;
    public bool showOnStart = false; // Отключено по умолчанию
    public int fontSize = 12;

    private bool isVisible = true;
    private GUIStyle guiStyle;
    private string debugInfo = "";
    private float lastUpdateTime = 0f;
    private float updateInterval = 1f;

    void Start()
    {


        isVisible = showOnStart;

        // Создаем стиль для GUI
        guiStyle = new GUIStyle();
        guiStyle.fontSize = fontSize;
        guiStyle.normal.textColor = Color.white;
        guiStyle.wordWrap = false;

        UpdateDebugInfo();


    }

    void Update()
    {
        // Переключение видимости
        if (Input.GetKeyDown(toggleKey))
        {
            isVisible = !isVisible;

        }

        // Обновление информации
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateDebugInfo();
            lastUpdateTime = Time.time;
        }
    }

    void UpdateDebugInfo()
    {
        var info = new System.Text.StringBuilder();

        info.AppendLine($"=== DEBUG INFO ({System.DateTime.Now:HH:mm:ss}) ===");
        info.AppendLine($"FPS: {(1f / Time.deltaTime):F1}");
        info.AppendLine($"Press {toggleKey} to toggle this display");
        info.AppendLine($"Press F12 for full Debug Monitor");
        info.AppendLine();

        // Системы
        info.AppendLine("=== SYSTEMS ===");

        SelectionManager selectionManager = FindObjectOfType<SelectionManager>();
        info.AppendLine($"SelectionManager: {(selectionManager != null ? "✓" : "✗")}");
        if (selectionManager != null)
        {
            var selected = selectionManager.GetSelectedObjects();
            info.AppendLine($"  Selected: {selected.Count} objects");
        }

        EnemyTargetingSystem targetingSystem = FindObjectOfType<EnemyTargetingSystem>();
        info.AppendLine($"EnemyTargetingSystem: {(targetingSystem != null ? "✓" : "✗")}");

        MovementController movementController = FindObjectOfType<MovementController>();
        info.AppendLine($"MovementController: {(movementController != null ? "✓" : "✗")}");

        CanvasCharacterIconsManager iconsUI = FindObjectOfType<CanvasCharacterIconsManager>();
        info.AppendLine($"CharacterIconsUI: {(iconsUI != null ? "✓" : "✗")}");

        GridManager gridManager = FindObjectOfType<GridManager>();
        info.AppendLine($"GridManager: {(gridManager != null ? "✓" : "✗")}");

        info.AppendLine();

        // Персонажи
        Character[] allCharacters = FindObjectsOfType<Character>();
        info.AppendLine($"=== CHARACTERS ({allCharacters.Length}) ===");

        int playerCount = 0, enemyCount = 0;
        foreach (var character in allCharacters)
        {
            if (character.IsPlayerCharacter()) playerCount++;
            else if (character.IsEnemyCharacter()) enemyCount++;
        }

        info.AppendLine($"Players: {playerCount}, Enemies: {enemyCount}");

        // Показываем первых 5 персонажей
        int shown = 0;
        foreach (var character in allCharacters)
        {
            if (shown >= 5) break;

            string status = character.IsSelected() ? "[SELECTED]" : "";
            info.AppendLine($"  {character.GetFullName()} {status}");
            shown++;
        }

        if (allCharacters.Length > 5)
        {
            info.AppendLine($"  ... and {allCharacters.Length - 5} more");
        }

        debugInfo = info.ToString();
    }

    void OnGUI()
    {
        if (!isVisible) return;

        // Темный фон
        GUI.Box(new Rect(10, 10, 400, Screen.height - 100), "");

        // Текст с информацией
        GUI.Label(new Rect(20, 20, 380, Screen.height - 120), debugInfo, guiStyle);
    }
}