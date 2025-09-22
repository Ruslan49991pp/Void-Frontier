using UnityEngine;

/// <summary>
/// Скрипт для отладки структуры префаба SKM_Character
/// </summary>
public class SKMCharacterDebugger : MonoBehaviour
{
    [Header("Debug Keys")]
    public KeyCode analyzeKey = KeyCode.F8;
    public bool enableDebugger = false; // Отключено по умолчанию

    void Update()
    {
        if (enableDebugger && Input.GetKeyDown(analyzeKey))
        {
            AnalyzeSKMCharacterStructure();
        }
    }

    void AnalyzeSKMCharacterStructure()
    {


        Character[] allCharacters = FindObjectsOfType<Character>();


        foreach (Character character in allCharacters)
        {





            // Анализируем рендереры
            Renderer[] renderers = character.GetComponentsInChildren<Renderer>();

            for (int i = 0; i < renderers.Length; i++)
            {

            }

            // Анализируем коллайдеры
            Collider[] colliders = character.GetComponentsInChildren<Collider>();

            for (int i = 0; i < colliders.Length; i++)
            {

            }

            // Анализируем дочерние объекты

            for (int i = 0; i < character.transform.childCount; i++)
            {
                Transform child = character.transform.GetChild(i);


                // Проверяем компоненты на дочерних объектах
                Renderer childRenderer = child.GetComponent<Renderer>();
                Collider childCollider = child.GetComponent<Collider>();

                if (childRenderer != null)
                {

                }
                if (childCollider != null)
                {

                }
            }

            // Проверяем characterRenderer



        }


    }

    void OnGUI()
    {
        if (enableDebugger)
        {
            GUI.Label(new Rect(10, 100, 300, 30), $"Press {analyzeKey} to analyze SKM_Character structure");
        }
    }
}