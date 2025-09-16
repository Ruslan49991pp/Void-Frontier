using UnityEngine;

/// <summary>
/// Тестер для проверки всех исправлений UI
/// </summary>
public class UIFixesTester : MonoBehaviour
{
    [Header("Test Keys")]
    public KeyCode testCenterKey = KeyCode.F;
    public KeyCode logCharactersKey = KeyCode.L;
    public KeyCode logUIComponentsKey = KeyCode.U;

    void Update()
    {
        // Тест кнопки Center - должна НЕ работать
        if (Input.GetKeyDown(testCenterKey))
        {
            Debug.Log("F key pressed - Center function should NOT work!");
        }

        // Логировать найденных персонажей
        if (Input.GetKeyDown(logCharactersKey))
        {
            LogCharacters();
        }

        // Логировать UI компоненты
        if (Input.GetKeyDown(logUIComponentsKey))
        {
            LogUIComponents();
        }
    }

    void LogCharacters()
    {
        Character[] characters = FindObjectsOfType<Character>();
        Debug.Log($"UIFixesTester: Found {characters.Length} characters in scene:");

        foreach (Character character in characters)
        {
            if (character.characterData != null)
            {
                Debug.Log($"- {character.GetFullName()} (Level {character.characterData.level}, Health: {character.characterData.health}/{character.characterData.maxHealth})");
            }
            else
            {
                Debug.Log($"- {character.name} (No character data)");
            }
        }
    }

    void LogUIComponents()
    {
        // Проверяем CharacterIconsPanel
        CharacterIconsPanel iconsPanel = FindObjectOfType<CharacterIconsPanel>();
        if (iconsPanel != null)
        {
            Debug.Log("UIFixesTester: CharacterIconsPanel found!");
        }
        else
        {
            Debug.Log("UIFixesTester: CharacterIconsPanel NOT found!");
        }

        // Проверяем GameUI
        GameUI gameUI = FindObjectOfType<GameUI>();
        if (gameUI != null)
        {
            Debug.Log("UIFixesTester: GameUI found!");
        }
        else
        {
            Debug.Log("UIFixesTester: GameUI NOT found!");
        }

        // Проверяем Canvas'ы
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        Debug.Log($"UIFixesTester: Found {canvases.Length} canvases:");
        foreach (Canvas canvas in canvases)
        {
            Debug.Log($"- {canvas.name} (Order: {canvas.sortingOrder})");
        }
    }

    void OnGUI()
    {
        // Инструкции на экране
        int yPos = 300;
        GUI.Label(new Rect(10, yPos, 400, 20), $"Press {testCenterKey} to test Center button (should NOT work)");
        GUI.Label(new Rect(10, yPos + 20, 400, 20), $"Press {logCharactersKey} to log all characters");
        GUI.Label(new Rect(10, yPos + 40, 400, 20), $"Press {logUIComponentsKey} to log UI components");

        // Статус проверок
        GUI.Label(new Rect(10, yPos + 70, 400, 20), "Expected behavior:");
        GUI.Label(new Rect(10, yPos + 90, 400, 20), "1. No Center button working");
        GUI.Label(new Rect(10, yPos + 110, 400, 20), "2. Character icons in top-left");
        GUI.Label(new Rect(10, yPos + 130, 400, 20), "3. Detailed object info in bottom-left");
    }
}