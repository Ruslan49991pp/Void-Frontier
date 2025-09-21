using UnityEngine;

/// <summary>
/// Инструкции по использованию системы отладки
/// </summary>
public class DebugInstructions : MonoBehaviour
{
    void Start()
    {
        // Показываем инструкции по отладке при запуске
        ShowDebugInstructions();
    }

    void ShowDebugInstructions()
    {
        DebugLogger.Log(DebugLogger.LogCategory.General, "=== DEBUG SYSTEM INSTRUCTIONS ===");
        DebugLogger.Log(DebugLogger.LogCategory.General, "KEYBOARD SHORTCUTS:");
        DebugLogger.Log(DebugLogger.LogCategory.General, "  F12 - Open/close Debug Monitor");
        DebugLogger.Log(DebugLogger.LogCategory.General, "  F11 - Log all characters in scene");
        DebugLogger.Log(DebugLogger.LogCategory.General, "  F10 - Log all system states");
        DebugLogger.Log(DebugLogger.LogCategory.General, "  F9  - Test enemy targeting system");
        DebugLogger.Log(DebugLogger.LogCategory.General, "  F8  - Analyze SKM_Character structure");
        DebugLogger.Log(DebugLogger.LogCategory.General, "  F7  - Recreate all enemies with fixed materials");
        DebugLogger.Log(DebugLogger.LogCategory.General, "  F1  - Toggle simple debug display");
        DebugLogger.Log(DebugLogger.LogCategory.General, "  R   - Refresh all characters (remove old, create new with SKM_Character)");
        DebugLogger.Log(DebugLogger.LogCategory.General, "  C   - Spawn additional test character");
        DebugLogger.Log(DebugLogger.LogCategory.General, "===");

        DebugLogger.Log(DebugLogger.LogCategory.General, "DEBUG CATEGORIES:");
        DebugLogger.Log(DebugLogger.LogCategory.Selection, "- SELECTION (Yellow): SelectionManager events");
        DebugLogger.Log(DebugLogger.LogCategory.Movement, "- MOVEMENT (Green): Character movement");
        DebugLogger.Log(DebugLogger.LogCategory.Targeting, "- TARGETING (Red): Enemy targeting system");
        DebugLogger.Log(DebugLogger.LogCategory.AI, "- AI (Blue): Character AI behavior");
        DebugLogger.Log(DebugLogger.LogCategory.UI, "- UI (Magenta): User interface");
        DebugLogger.Log(DebugLogger.LogCategory.Spawning, "- SPAWNING (Cyan): Character/enemy spawning");
        DebugLogger.Log(DebugLogger.LogCategory.GameInit, "- GAMEINIT (Orange): Game initialization");
        DebugLogger.Log(DebugLogger.LogCategory.Icons, "- ICONS (Gray): Character icons system");
        DebugLogger.Log(DebugLogger.LogCategory.General, "=== END INSTRUCTIONS ===");
    }

    void Update()
    {
        // Дополнительные hotkeys для отладки
        if (Input.GetKeyDown(KeyCode.F11))
        {
            // Показать статистику всех персонажей
            LogAllCharacters();
        }

        if (Input.GetKeyDown(KeyCode.F10))
        {
            // Показать состояние всех систем
            LogSystemStates();
        }

        if (Input.GetKeyDown(KeyCode.F9))
        {
            // Тестирование системы наведения на врагов
            TestEnemyTargetingSystem();
        }

        if (Input.GetKeyDown(KeyCode.F7))
        {
            // Пересоздать всех врагов
            RecreateEnemies();
        }
    }

    void LogAllCharacters()
    {
        DebugLogger.Log(DebugLogger.LogCategory.General, "=== ALL CHARACTERS IN SCENE ===");

        Character[] characters = FindObjectsOfType<Character>();
        DebugLogger.Log(DebugLogger.LogCategory.General, $"Total characters found: {characters.Length}");

        foreach (Character character in characters)
        {
            if (character != null)
            {
                string info = $"{character.GetFullName()} - " +
                             $"Faction: {character.GetFaction()}, " +
                             $"Health: {character.GetHealth():F0}/{character.GetMaxHealth():F0}, " +
                             $"Selected: {character.IsSelected()}, " +
                             $"Position: {character.transform.position}";

                DebugLogger.Log(DebugLogger.LogCategory.General, info, character);
            }
        }

        DebugLogger.Log(DebugLogger.LogCategory.General, "=== END CHARACTER LIST ===");
    }

    void LogSystemStates()
    {
        DebugLogger.Log(DebugLogger.LogCategory.General, "=== SYSTEM STATES ===");

        // SelectionManager
        SelectionManager selectionManager = FindObjectOfType<SelectionManager>();
        if (selectionManager != null)
        {
            var selected = selectionManager.GetSelectedObjects();
            DebugLogger.Log(DebugLogger.LogCategory.Selection,
                $"SelectionManager: {selected.Count} objects selected, BoxSelecting: {selectionManager.IsBoxSelecting}");
        }
        else
        {
            DebugLogger.LogError(DebugLogger.LogCategory.Selection, "SelectionManager NOT FOUND!");
        }

        // EnemyTargetingSystem
        EnemyTargetingSystem targetingSystem = FindObjectOfType<EnemyTargetingSystem>();
        if (targetingSystem != null)
        {
            DebugLogger.Log(DebugLogger.LogCategory.Targeting,
                $"EnemyTargetingSystem: Active, DebugMode: {targetingSystem.debugMode}");
        }
        else
        {
            DebugLogger.LogError(DebugLogger.LogCategory.Targeting, "EnemyTargetingSystem NOT FOUND!");
        }

        // Character Icons
        SimpleCharacterIconsUI iconsUI = FindObjectOfType<SimpleCharacterIconsUI>();
        if (iconsUI != null)
        {
            DebugLogger.Log(DebugLogger.LogCategory.Icons,
                $"SimpleCharacterIconsUI: Active, Enabled: {iconsUI.enabled}");
        }
        else
        {
            DebugLogger.LogError(DebugLogger.LogCategory.Icons, "SimpleCharacterIconsUI NOT FOUND!");
        }

        // MovementController
        MovementController movementController = FindObjectOfType<MovementController>();
        if (movementController != null)
        {
            DebugLogger.Log(DebugLogger.LogCategory.Movement, "MovementController: Active");
        }
        else
        {
            DebugLogger.LogError(DebugLogger.LogCategory.Movement, "MovementController NOT FOUND!");
        }

        DebugLogger.Log(DebugLogger.LogCategory.General, "=== END SYSTEM STATES ===");
    }

    void TestEnemyTargetingSystem()
    {
        DebugLogger.Log(DebugLogger.LogCategory.General, "=== TESTING ENEMY TARGETING SYSTEM (F9) ===");

        EnemyTargetingSystem targetingSystem = FindObjectOfType<EnemyTargetingSystem>();
        if (targetingSystem != null)
        {
            targetingSystem.TestTargetingSystem();
            targetingSystem.LogActiveTargets();
        }
        else
        {
            DebugLogger.LogError(DebugLogger.LogCategory.Targeting, "EnemyTargetingSystem not found! Cannot test.");
        }

        DebugLogger.Log(DebugLogger.LogCategory.General, "=== TEST COMPLETE ===");
    }

    void RecreateEnemies()
    {
        DebugLogger.Log(DebugLogger.LogCategory.General, "=== RECREATING ENEMIES (F7) ===");

        // Удаляем всех существующих врагов
        Character[] allCharacters = FindObjectsOfType<Character>();
        int removedCount = 0;

        foreach (Character character in allCharacters)
        {
            if (character.IsEnemyCharacter())
            {
                DebugLogger.Log(DebugLogger.LogCategory.General, $"Removing enemy: {character.GetFullName()}");
                DestroyImmediate(character.gameObject);
                removedCount++;
            }
        }

        DebugLogger.Log(DebugLogger.LogCategory.General, $"Removed {removedCount} enemies");

        // Пересоздаем врагов через EnemySpawnerTest
        EnemySpawnerTest enemySpawner = FindObjectOfType<EnemySpawnerTest>();
        if (enemySpawner != null)
        {
            DebugLogger.Log(DebugLogger.LogCategory.General, "Recreating enemies with improved materials...");
            enemySpawner.SpawnEnemies();
        }
        else
        {
            DebugLogger.LogError(DebugLogger.LogCategory.General, "EnemySpawnerTest not found! Cannot recreate enemies.");
        }

        DebugLogger.Log(DebugLogger.LogCategory.General, "=== ENEMY RECREATION COMPLETE ===");
    }
}