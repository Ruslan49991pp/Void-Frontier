using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor скрипт для настройки системы подбора предметов
/// </summary>
public class SetupItemPickup
{
    [MenuItem("Tools/Items/Add Pickup System to All Characters")]
    public static void AddPickupSystemToAllCharacters()
    {
        // Ищем всех персонажей в сцене
        Character[] characters = Object.FindObjectsOfType<Character>();

        if (characters.Length == 0)
        {
            EditorUtility.DisplayDialog("No Characters Found",
                "No Character components found in the scene.\n" +
                "Make sure you have characters with Character component in the scene.",
                "OK");
            return;
        }

        int addedCount = 0;

        foreach (Character character in characters)
        {
            // Проверяем, есть ли уже ItemPickupSystem
            ItemPickupSystem pickupSystem = character.GetComponent<ItemPickupSystem>();

            if (pickupSystem == null)
            {
                // Добавляем компонент
                pickupSystem = character.gameObject.AddComponent<ItemPickupSystem>();

                // Настраиваем параметры
                pickupSystem.pickupRadius = 3f;
                pickupSystem.pickupKey = KeyCode.E;
                pickupSystem.showPickupHint = true;

                EditorUtility.SetDirty(character.gameObject);
                addedCount++;

                Debug.Log($"✓ Added ItemPickupSystem to: {character.gameObject.name}");
            }
            else
            {
                Debug.Log($"○ ItemPickupSystem already exists on: {character.gameObject.name}");
            }
        }

        if (addedCount > 0)
        {
            EditorUtility.DisplayDialog("Success",
                $"Added ItemPickupSystem to {addedCount} character(s)!\n\n" +
                "Characters can now pick up items by pressing E when near them.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Already Setup",
                "All characters already have ItemPickupSystem!",
                "OK");
        }
    }

    [MenuItem("Tools/Items/Add Pickup System to Selected")]
    public static void AddPickupSystemToSelected()
    {
        GameObject selected = Selection.activeGameObject;

        if (selected == null)
        {
            EditorUtility.DisplayDialog("Error",
                "Please select a GameObject with Character component first.",
                "OK");
            return;
        }

        Character character = selected.GetComponent<Character>();
        if (character == null)
        {
            EditorUtility.DisplayDialog("Error",
                "Selected GameObject doesn't have Character component!",
                "OK");
            return;
        }

        ItemPickupSystem pickupSystem = selected.GetComponent<ItemPickupSystem>();
        if (pickupSystem != null)
        {
            EditorUtility.DisplayDialog("Already Exists",
                $"{selected.name} already has ItemPickupSystem!",
                "OK");
            return;
        }

        // Добавляем компонент
        pickupSystem = selected.AddComponent<ItemPickupSystem>();
        pickupSystem.pickupRadius = 3f;
        pickupSystem.pickupKey = KeyCode.E;
        pickupSystem.showPickupHint = true;

        EditorUtility.SetDirty(selected);

        Debug.Log($"✓ Added ItemPickupSystem to: {selected.name}");

        EditorUtility.DisplayDialog("Success",
            $"Added ItemPickupSystem to {selected.name}!\n\n" +
            "This character can now pick up items by pressing E when near them.",
            "OK");
    }
}
