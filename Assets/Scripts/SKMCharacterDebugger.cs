using UnityEngine;

/// <summary>
/// Скрипт для отладки структуры префаба SKM_Character
/// </summary>
public class SKMCharacterDebugger : MonoBehaviour
{
    [Header("Debug Keys")]
    public KeyCode analyzeKey = KeyCode.F8;

    void Update()
    {
        if (Input.GetKeyDown(analyzeKey))
        {
            AnalyzeSKMCharacterStructure();
        }
    }

    void AnalyzeSKMCharacterStructure()
    {
        DebugLogger.Log(DebugLogger.LogCategory.General, "=== ANALYZING SKM_CHARACTER STRUCTURE ===");

        Character[] allCharacters = FindObjectsOfType<Character>();
        DebugLogger.Log(DebugLogger.LogCategory.General, $"Found {allCharacters.Length} characters in scene");

        foreach (Character character in allCharacters)
        {
            DebugLogger.Log(DebugLogger.LogCategory.General, $"--- CHARACTER: {character.GetFullName()} ---");
            DebugLogger.Log(DebugLogger.LogCategory.General, $"GameObject: {character.gameObject.name}");
            DebugLogger.Log(DebugLogger.LogCategory.General, $"Position: {character.transform.position}");
            DebugLogger.Log(DebugLogger.LogCategory.General, $"Faction: {character.GetFaction()}");

            // Анализируем рендереры
            Renderer[] renderers = character.GetComponentsInChildren<Renderer>();
            DebugLogger.Log(DebugLogger.LogCategory.General, $"Renderers found: {renderers.Length}");
            for (int i = 0; i < renderers.Length; i++)
            {
                DebugLogger.Log(DebugLogger.LogCategory.General, $"  Renderer {i}: {renderers[i].name} (enabled: {renderers[i].enabled})");
            }

            // Анализируем коллайдеры
            Collider[] colliders = character.GetComponentsInChildren<Collider>();
            DebugLogger.Log(DebugLogger.LogCategory.General, $"Colliders found: {colliders.Length}");
            for (int i = 0; i < colliders.Length; i++)
            {
                DebugLogger.Log(DebugLogger.LogCategory.General, $"  Collider {i}: {colliders[i].name} (enabled: {colliders[i].enabled}, type: {colliders[i].GetType().Name})");
            }

            // Анализируем дочерние объекты
            DebugLogger.Log(DebugLogger.LogCategory.General, $"Child objects: {character.transform.childCount}");
            for (int i = 0; i < character.transform.childCount; i++)
            {
                Transform child = character.transform.GetChild(i);
                DebugLogger.Log(DebugLogger.LogCategory.General, $"  Child {i}: {child.name}");

                // Проверяем компоненты на дочерних объектах
                Renderer childRenderer = child.GetComponent<Renderer>();
                Collider childCollider = child.GetComponent<Collider>();

                if (childRenderer != null)
                {
                    DebugLogger.Log(DebugLogger.LogCategory.General, $"    Has Renderer: {childRenderer.name}");
                }
                if (childCollider != null)
                {
                    DebugLogger.Log(DebugLogger.LogCategory.General, $"    Has Collider: {childCollider.name} ({childCollider.GetType().Name})");
                }
            }

            // Проверяем characterRenderer
            DebugLogger.Log(DebugLogger.LogCategory.General, $"characterRenderer field: {(character.characterRenderer != null ? character.characterRenderer.name : "NULL")}");

            DebugLogger.Log(DebugLogger.LogCategory.General, "");
        }

        DebugLogger.Log(DebugLogger.LogCategory.General, "=== STRUCTURE ANALYSIS COMPLETE ===");
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 100, 300, 30), $"Press {analyzeKey} to analyze SKM_Character structure");
    }
}