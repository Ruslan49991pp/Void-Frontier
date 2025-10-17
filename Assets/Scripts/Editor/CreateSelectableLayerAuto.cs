using UnityEngine;
using UnityEditor;

/// <summary>
/// Автоматически создает слой "Selectable" при загрузке редактора
/// </summary>
[InitializeOnLoad]
public static class CreateSelectableLayerAuto
{
    static CreateSelectableLayerAuto()
    {
        // Проверяем, существует ли слой "Selectable"
        int selectableLayer = LayerMask.NameToLayer("Selectable");

        if (selectableLayer == -1)
        {
            // Слой не существует - создаем его
            CreateLayer("Selectable");
        }
    }

    /// <summary>
    /// Создать новый слой в Unity
    /// </summary>
    static void CreateLayer(string layerName)
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        SerializedProperty layers = tagManager.FindProperty("layers");

        // Ищем первый пустой слот (слои 0-7 зарезервированы Unity)
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layerSP = layers.GetArrayElementAtIndex(i);

            if (string.IsNullOrEmpty(layerSP.stringValue))
            {
                // Нашли пустой слот - создаем слой
                layerSP.stringValue = layerName;
                tagManager.ApplyModifiedProperties();

                Debug.Log($"[CreateSelectableLayerAuto] ✓ Created layer '{layerName}' at index {i}");
                return;
            }
        }

        Debug.LogWarning($"[CreateSelectableLayerAuto] Could not create layer '{layerName}' - all layer slots are full!");
    }
}
