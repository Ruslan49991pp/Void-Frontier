using UnityEngine;
using UnityEditor;

/// <summary>
/// Создание слоя "Selectable" для выделяемых объектов
/// </summary>
public class CreateSelectableLayer
{
    [MenuItem("Tools/Setup/Create Selectable Layer")]
    public static void CreateLayer()
    {
        // Получаем TagManager
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        // Проверяем, существует ли уже слой "Selectable"
        for (int i = 8; i < layers.arraySize; i++)  // Слои 0-7 зарезервированы Unity
        {
            SerializedProperty layerSP = layers.GetArrayElementAtIndex(i);
            if (layerSP.stringValue == "Selectable")
            {
                Debug.Log("✓ Layer 'Selectable' already exists");
                EditorUtility.DisplayDialog("Success", "Layer 'Selectable' already exists!", "OK");
                return;
            }
        }

        // Ищем первый свободный слой
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layerSP = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layerSP.stringValue))
            {
                layerSP.stringValue = "Selectable";
                tagManager.ApplyModifiedProperties();

                Debug.Log($"✓ Created layer 'Selectable' at index {i}");
                EditorUtility.DisplayDialog("Success",
                    $"Layer 'Selectable' created at index {i}!\n\nNow restart the scene to spawn resources with the correct layer.",
                    "OK");
                return;
            }
        }

        Debug.LogError("✗ No free layers available! All custom layers (8-31) are already in use.");
        EditorUtility.DisplayDialog("Error",
            "No free layers available!\n\nAll custom layers (8-31) are already in use.\n\nPlease free up a layer slot manually in Project Settings > Tags and Layers.",
            "OK");
    }
}
