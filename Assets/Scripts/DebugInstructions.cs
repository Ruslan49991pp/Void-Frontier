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


        Character[] characters = FindObjectsOfType<Character>();


        foreach (Character character in characters)
        {
            if (character != null)
            {
                string info = $"{character.GetFullName()} - " +
                             $"Faction: {character.GetFaction()}, " +
                             $"Health: {character.GetHealth():F0}/{character.GetMaxHealth():F0}, " +
                             $"Selected: {character.IsSelected()}, " +
                             $"Position: {character.transform.position}";


            }
        }


    }

    void LogSystemStates()
    {


        // SelectionManager
        SelectionManager selectionManager = FindObjectOfType<SelectionManager>();
        if (selectionManager != null)
        {
            var selected = selectionManager.GetSelectedObjects();
        }
        else
        {

        }

        // EnemyTargetingSystem
        EnemyTargetingSystem targetingSystem = FindObjectOfType<EnemyTargetingSystem>();
        if (targetingSystem != null)
        {
        }
        else
        {

        }

        // Character Icons
        SimpleCharacterIconsUI iconsUI = FindObjectOfType<SimpleCharacterIconsUI>();
        if (iconsUI != null)
        {
        }
        else
        {

        }

        // MovementController
        MovementController movementController = FindObjectOfType<MovementController>();
        if (movementController != null)
        {

        }
        else
        {

        }


    }

    void TestEnemyTargetingSystem()
    {


        EnemyTargetingSystem targetingSystem = FindObjectOfType<EnemyTargetingSystem>();
        if (targetingSystem != null)
        {
            targetingSystem.TestTargetingSystem();
            targetingSystem.LogActiveTargets();
        }
        else
        {

        }


    }

    void RecreateEnemies()
    {


        // Удаляем всех существующих врагов
        Character[] allCharacters = FindObjectsOfType<Character>();
        int removedCount = 0;

        foreach (Character character in allCharacters)
        {
            if (character.IsEnemyCharacter())
            {

                DestroyImmediate(character.gameObject);
                removedCount++;
            }
        }



        // Пересоздаем врагов через EnemySpawnerTest
        EnemySpawnerTest enemySpawner = FindObjectOfType<EnemySpawnerTest>();
        if (enemySpawner != null)
        {

            enemySpawner.SpawnEnemies();
        }
        else
        {

        }


    }
}