using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor скрипт для автоматической настройки иконки металла
/// </summary>
public static class SetupMetalIcon
{
    [MenuItem("Tools/Resources/Setup Metal Icon")]
    public static void Setup()
    {
        Debug.Log("=== Setting up Metal icon ===");

        // Находим или создаем ItemDatabase
        ItemDatabase itemDatabase = Resources.Load<ItemDatabase>("ItemDatabase");
        if (itemDatabase == null)
        {
            // Ищем в проекте
            string[] guids = AssetDatabase.FindAssets("t:ItemDatabase");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                itemDatabase = AssetDatabase.LoadAssetAtPath<ItemDatabase>(path);
                Debug.Log($"Found ItemDatabase at: {path}");
            }
        }

        if (itemDatabase == null)
        {
            Debug.LogError("ItemDatabase not found! Please create it via Assets -> Create -> Inventory -> Item Database");
            return;
        }

        // Загружаем иконку металла
        Sprite metalIcon = Resources.Load<Sprite>("Icons/Resources/Res_Metal_ico");

        // Если не нашли через Resources, ищем напрямую
        if (metalIcon == null)
        {
            string[] iconGuids = AssetDatabase.FindAssets("Res_Metal_ico t:Sprite");
            if (iconGuids.Length > 0)
            {
                string iconPath = AssetDatabase.GUIDToAssetPath(iconGuids[0]);
                metalIcon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
                Debug.Log($"Loaded metal icon from: {iconPath}");
            }
        }

        if (metalIcon == null)
        {
            Debug.LogError("Metal icon not found! Expected at: Assets/Icons/Resources/Res_Metal_ico.png");
            return;
        }

        // Добавляем или обновляем запись о металле
        bool found = false;
        foreach (var item in itemDatabase.items)
        {
            if (item.itemName == "Metal")
            {
                item.icon = metalIcon;
                item.itemType = ItemType.Resource;
                found = true;
                Debug.Log("Updated existing Metal entry with icon");
                break;
            }
        }

        if (!found)
        {
            // Добавляем новую запись
            itemDatabase.items.Add(new ItemIconEntry
            {
                itemName = "Metal",
                itemType = ItemType.Resource,
                icon = metalIcon
            });
            Debug.Log("Added new Metal entry to ItemDatabase");
        }

        // Сохраняем изменения
        EditorUtility.SetDirty(itemDatabase);
        AssetDatabase.SaveAssets();

        Debug.Log("✓ Metal icon setup complete!");

        // Инициализируем ItemFactory с этой базой данных
        ItemFactory.Initialize(itemDatabase);
        Debug.Log("✓ ItemFactory initialized with ItemDatabase");
    }
}
