using UnityEngine;
using UnityEditor;

/// <summary>
/// Редактор для ItemDatabase с кнопками быстрого доступа
/// </summary>
[CustomEditor(typeof(ItemDatabase))]
public class ItemDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ItemDatabase database = (ItemDatabase)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Управление базой данных", EditorStyles.boldLabel);

        if (GUILayout.Button("Синхронизировать с кодом (найти все предметы)", GUILayout.Height(30)))
        {
            SyncWithCode(database);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Действия", EditorStyles.boldLabel);

        if (GUILayout.Button("Показать все предметы в игре"))
        {
            ShowAllItemsInGame(database);
        }

        if (GUILayout.Button("Применить иконки ко всем предметам"))
        {
            ApplyIconsToAllItems(database);
        }

        if (GUILayout.Button("Показать предметы без иконок"))
        {
            ShowItemsWithoutIcons();
        }

        if (GUILayout.Button("Очистить базу данных"))
        {
            if (EditorUtility.DisplayDialog("Очистить базу?",
                "Вы уверены что хотите удалить все предметы из базы данных?",
                "Да", "Отмена"))
            {
                database.items.Clear();
                EditorUtility.SetDirty(database);
                Debug.Log("База данных очищена!");
            }
        }
    }

    private void SyncWithCode(ItemDatabase database)
    {
        // Список всех предметов которые должны быть в базе (из кода)
        System.Collections.Generic.Dictionary<string, ItemType> codeItems = new System.Collections.Generic.Dictionary<string, ItemType>();

        // Добавляем предметы из Character.cs
        codeItems["Weapon"] = ItemType.Weapon;
        codeItems["Helmet"] = ItemType.Armor;
        codeItems["Body Armor"] = ItemType.Armor;
        codeItems["Pants"] = ItemType.Armor;
        codeItems["Boots"] = ItemType.Armor;
        codeItems["Medkit"] = ItemType.Medical;
        codeItems["Tool"] = ItemType.Tool;
        codeItems["Resource"] = ItemType.Resource;
        codeItems["Consumable"] = ItemType.Consumable;

        // Добавляем предметы из InventoryManager.cs (если они отличаются)
        // (эти уже есть выше, не дублируем)

        Debug.Log($"=== Синхронизация с кодом ===");
        Debug.Log($"Найдено уникальных предметов в коде: {codeItems.Count}");

        // Сохраняем старые иконки
        System.Collections.Generic.Dictionary<string, UnityEngine.Sprite> oldIcons = new System.Collections.Generic.Dictionary<string, UnityEngine.Sprite>();
        foreach (var item in database.items)
        {
            if (item.icon != null && !oldIcons.ContainsKey(item.itemName))
            {
                oldIcons[item.itemName] = item.icon;
            }
        }

        // Очищаем базу
        database.items.Clear();

        // Добавляем все предметы из кода
        int restoredIcons = 0;
        foreach (var kvp in codeItems)
        {
            ItemIconEntry entry = new ItemIconEntry
            {
                itemName = kvp.Key,
                itemType = kvp.Value,
                icon = null
            };

            // Восстанавливаем старую иконку если была
            if (oldIcons.ContainsKey(kvp.Key))
            {
                entry.icon = oldIcons[kvp.Key];
                restoredIcons++;
            }

            database.items.Add(entry);
        }

        EditorUtility.SetDirty(database);

        Debug.Log($"База данных синхронизирована!");
        Debug.Log($"Предметов в базе: {database.items.Count}");
        Debug.Log($"Восстановлено старых иконок: {restoredIcons}");
        Debug.Log($"Предметов без иконок: {database.items.Count - restoredIcons}");
    }

    private void ApplyIconsToAllItems(ItemDatabase database)
    {
        ItemIconManager manager = FindObjectOfType<ItemIconManager>();

        if (manager == null)
        {
            Debug.LogWarning("ItemIconManager не найден в сцене! Добавьте его на GameObject.");
            return;
        }

        if (manager.itemDatabase == null)
        {
            manager.itemDatabase = database;
            EditorUtility.SetDirty(manager);
        }

        manager.ApplyIconsToAllItems();
    }

    private void ShowAllItemsInGame(ItemDatabase database)
    {
        Inventory[] inventories = FindObjectsOfType<Inventory>();
        System.Collections.Generic.HashSet<string> uniqueItems = new System.Collections.Generic.HashSet<string>();

        Debug.Log("=== Все предметы в игре ===");

        foreach (Inventory inventory in inventories)
        {
            var allSlots = inventory.GetAllSlots();
            if (allSlots != null)
            {
                foreach (InventorySlot slot in allSlots)
                {
                    if (slot != null && !slot.IsEmpty() && slot.itemData != null)
                    {
                        uniqueItems.Add($"{slot.itemData.itemName}|{slot.itemData.itemType}");
                    }
                }
            }

            var equipmentSlots = inventory.GetAllEquipmentSlots();
            if (equipmentSlots != null)
            {
                foreach (var kvp in equipmentSlots)
                {
                    InventorySlot slot = kvp.Value;
                    if (slot != null && !slot.IsEmpty() && slot.itemData != null)
                    {
                        uniqueItems.Add($"{slot.itemData.itemName}|{slot.itemData.itemType}");
                    }
                }
            }
        }

        foreach (string itemData in uniqueItems)
        {
            string[] parts = itemData.Split('|');
            string itemName = parts[0];
            ItemType itemType = (ItemType)System.Enum.Parse(typeof(ItemType), parts[1]);
            bool hasIcon = database.GetIconForItem(itemName) != null;

            Debug.Log($"- {itemName} ({itemType}) {(hasIcon ? "✓ Есть иконка" : "✗ Нет иконки")}");
        }

        Debug.Log($"Всего уникальных предметов: {uniqueItems.Count}");
    }

    private void ShowItemsWithoutIcons()
    {
        Inventory[] inventories = FindObjectsOfType<Inventory>();

        Debug.Log("=== Предметы без иконок ===");
        int count = 0;

        foreach (Inventory inventory in inventories)
        {
            var allSlots = inventory.GetAllSlots();
            if (allSlots != null)
            {
                foreach (InventorySlot slot in allSlots)
                {
                    if (slot != null && !slot.IsEmpty() && slot.itemData != null && slot.itemData.icon == null)
                    {
                        Debug.Log($"- {slot.itemData.itemName} ({slot.itemData.itemType})");
                        count++;
                    }
                }
            }

            var equipmentSlots = inventory.GetAllEquipmentSlots();
            if (equipmentSlots != null)
            {
                foreach (var kvp in equipmentSlots)
                {
                    InventorySlot slot = kvp.Value;
                    if (slot != null && !slot.IsEmpty() && slot.itemData != null && slot.itemData.icon == null)
                    {
                        Debug.Log($"- {slot.itemData.itemName} ({slot.itemData.itemType}) [Экипировано]");
                        count++;
                    }
                }
            }
        }

        if (count == 0)
        {
            Debug.Log("Все предметы имеют иконки!");
        }
        else
        {
            Debug.Log($"Найдено предметов без иконок: {count}");
        }
    }
}
