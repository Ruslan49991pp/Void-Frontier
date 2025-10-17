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
        EditorGUILayout.LabelField("Создание предметов", EditorStyles.boldLabel);

        if (GUILayout.Button("➕ Создать новый предмет", GUILayout.Height(25)))
        {
            ShowCreateItemDialog(database);
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

        if (GUILayout.Button("Показать предметы без префабов"))
        {
            ShowItemsWithoutPrefabs(database);
        }

        if (GUILayout.Button("Показать статистику базы данных"))
        {
            ShowDatabaseStatistics(database);
        }

        if (GUILayout.Button("Очистить базу данных"))
        {
            if (EditorUtility.DisplayDialog("Очистить базу?",
                "Вы уверены что хотите удалить ВСЕ предметы из базы данных (включая кастомные)?",
                "Да", "Отмена"))
            {
                database.itemsInCode.Clear();
                database.customItems.Clear();
                database.RefreshCache();
                EditorUtility.SetDirty(database);
                Debug.Log("База данных полностью очищена!");
            }
        }

        if (GUILayout.Button("Очистить только кастомные предметы"))
        {
            if (EditorUtility.DisplayDialog("Очистить кастомные?",
                "Удалить все кастомные предметы (созданные вручную)?",
                "Да", "Отмена"))
            {
                database.customItems.Clear();
                database.RefreshCache();
                EditorUtility.SetDirty(database);
                Debug.Log("Кастомные предметы удалены!");
            }
        }
    }

    private void SyncWithCode(ItemDatabase database)
    {
        // Список всех предметов которые должны быть в базе (из кода)
        System.Collections.Generic.Dictionary<string, ItemType> codeItems = new System.Collections.Generic.Dictionary<string, ItemType>();

        Debug.Log($"=== Автоматическое сканирование кода ===");

        // Сначала сканируем класс ItemNames для получения всех констант
        string itemNamesPath = System.IO.Path.Combine(Application.dataPath, "Scripts", "ItemNames.cs");
        if (System.IO.File.Exists(itemNamesPath))
        {
            Debug.Log("Сканирую ItemNames.cs для получения констант...");
            string itemNamesContent = System.IO.File.ReadAllText(itemNamesPath);

            // Ищем паттерн: public const string НАЗВАНИЕ = "значение"
            System.Text.RegularExpressions.MatchCollection constantMatches = System.Text.RegularExpressions.Regex.Matches(
                itemNamesContent,
                @"public\s+const\s+string\s+\w+\s*=\s*""([^""]+)""",
                System.Text.RegularExpressions.RegexOptions.Multiline
            );

            foreach (System.Text.RegularExpressions.Match match in constantMatches)
            {
                if (match.Groups.Count > 1)
                {
                    string itemName = match.Groups[1].Value;
                    ItemType itemType = GuessItemType(itemName, itemNamesContent);

                    if (!codeItems.ContainsKey(itemName))
                    {
                        codeItems[itemName] = itemType;
                        Debug.Log($"Найдена константа: {itemName} ({itemType}) из ItemNames");
                    }
                }
            }
        }

        // Автоматически ищем все .cs файлы в проекте
        string[] allScripts = System.IO.Directory.GetFiles(Application.dataPath, "*.cs", System.IO.SearchOption.AllDirectories);

        int filesScanned = 0;
        int itemsFound = 0;

        foreach (string scriptPath in allScripts)
        {
            filesScanned++;
            string fileContent = System.IO.File.ReadAllText(scriptPath);

            // Ищем паттерн: itemName = "название"
            System.Text.RegularExpressions.MatchCollection matches = System.Text.RegularExpressions.Regex.Matches(
                fileContent,
                @"itemName\s*=\s*""([^""]+)""",
                System.Text.RegularExpressions.RegexOptions.Multiline
            );

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    string itemName = match.Groups[1].Value;

                    // Пропускаем общие/тестовые имена
                    if (itemName == "Unknown Item" || itemName.StartsWith("Test ") || itemName.StartsWith("Random "))
                        continue;

                    // Определяем тип предмета по контексту
                    ItemType itemType = GuessItemType(itemName, fileContent);

                    if (!codeItems.ContainsKey(itemName))
                    {
                        codeItems[itemName] = itemType;
                        itemsFound++;
                        Debug.Log($"Найден предмет: {itemName} ({itemType}) в {System.IO.Path.GetFileName(scriptPath)}");
                    }
                }
            }
        }

        Debug.Log($"=== Результаты сканирования ===");
        Debug.Log($"Просканировано файлов: {filesScanned}");
        Debug.Log($"Найдено уникальных предметов в коде: {codeItems.Count}");

        // ВАЖНО: Сохраняем старые данные из ВСЕХ секций (itemsInCode + customItems)
        System.Collections.Generic.Dictionary<string, ItemIconEntry> allOldData = new System.Collections.Generic.Dictionary<string, ItemIconEntry>();

        // Сохраняем из itemsInCode
        foreach (var item in database.itemsInCode)
        {
            if (!allOldData.ContainsKey(item.itemName))
            {
                allOldData[item.itemName] = item;
            }
        }

        // Сохраняем из customItems
        foreach (var item in database.customItems)
        {
            if (!allOldData.ContainsKey(item.itemName))
            {
                allOldData[item.itemName] = item;
            }
        }

        Debug.Log($"Сохранено старых данных для {allOldData.Count} предметов");

        // Очищаем обе секции - будем их пересобирать
        database.itemsInCode.Clear();
        database.customItems.Clear();

        // Добавляем все предметы из кода в itemsInCode
        int restoredIcons = 0;
        int restoredPrefabs = 0;
        int migratedFromCustom = 0;

        foreach (var kvp in codeItems)
        {
            ItemIconEntry entry = new ItemIconEntry
            {
                itemName = kvp.Key,
                itemType = kvp.Value,
                foundInCode = true,
                icon = null,
                worldPrefab = null
            };

            // Восстанавливаем старые данные если были (из любой секции!)
            if (allOldData.ContainsKey(kvp.Key))
            {
                var oldItem = allOldData[kvp.Key];

                // ВАЖНО: Копируем иконку и префаб
                entry.icon = oldItem.icon;
                entry.worldPrefab = oldItem.worldPrefab;

                if (entry.icon != null)
                    restoredIcons++;
                if (entry.worldPrefab != null)
                    restoredPrefabs++;

                // Проверяем миграцию из кастомных
                if (!oldItem.foundInCode)
                {
                    migratedFromCustom++;
                    Debug.Log($"✓ Кастомный предмет '{kvp.Key}' найден в коде → мигрирован в секцию 'Items In Code'");
                }

                // Удаляем из словаря чтобы не добавить в customItems
                allOldData.Remove(kvp.Key);
            }

            database.itemsInCode.Add(entry);
        }

        // Восстанавливаем кастомные предметы которые НЕ найдены в коде
        foreach (var oldItem in allOldData.Values)
        {
            // Только если это был кастомный предмет (не найден в коде)
            if (!oldItem.foundInCode || !codeItems.ContainsKey(oldItem.itemName))
            {
                database.customItems.Add(oldItem);
            }
        }

        database.RefreshCache();
        EditorUtility.SetDirty(database);

        Debug.Log($"\n=== Синхронизация завершена ===");
        Debug.Log($"Предметов в коде: {database.itemsInCode.Count}");
        Debug.Log($"Кастомных предметов: {database.customItems.Count}");
        Debug.Log($"ВСЕГО предметов: {database.GetAllItems().Count}");
        Debug.Log($"Восстановлено иконок: {restoredIcons}/{database.itemsInCode.Count}");
        Debug.Log($"Восстановлено префабов: {restoredPrefabs}/{database.itemsInCode.Count}");

        if (migratedFromCustom > 0)
        {
            Debug.Log($"✓ Мигрировано из кастомных в код: {migratedFromCustom} предметов");
        }

        int missingData = database.itemsInCode.Count - System.Math.Max(restoredIcons, restoredPrefabs);
        if (missingData > 0)
        {
            Debug.Log($"⚠ Требуют назначения иконок/префабов: {missingData} предметов");
        }
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

    private void ShowItemsWithoutPrefabs(ItemDatabase database)
    {
        var allItems = database.GetAllItems();

        Debug.Log("=== Предметы без префабов ===");
        int count = 0;

        foreach (var item in allItems)
        {
            if (item.worldPrefab == null)
            {
                string source = database.itemsInCode.Contains(item) ? "[в коде]" : "[кастомный]";
                string iconStatus = item.icon != null ? "[есть иконка]" : "[нет иконки]";
                Debug.Log($"- {item.itemName} ({item.itemType}) {source} {iconStatus}");
                count++;
            }
        }

        if (count == 0)
        {
            Debug.Log("Все предметы имеют префабы!");
        }
        else
        {
            Debug.Log($"Найдено предметов без префабов: {count}");
        }
    }

    private void ShowCreateItemDialog(ItemDatabase database)
    {
        // Создаем окно для ввода данных нового предмета
        string itemName = "";
        ItemType itemType = ItemType.Resource;

        // Простой диалог с вводом текста
        itemName = EditorUtility.DisplayDialogComplex(
            "Создать новый предмет",
            "Введите название предмета:",
            "Отмена", "", ""
        ) == 0 ? "" : null;

        // Более продвинутый способ через EditorWindow было бы лучше, но для простоты используем текстовое поле
        // Упрощенная версия - запрашиваем только имя
        GenericMenu menu = new GenericMenu();

        foreach (ItemType type in System.Enum.GetValues(typeof(ItemType)))
        {
            ItemType capturedType = type;
            menu.AddItem(new GUIContent(type.ToString()), false, () => {
                string name = EditorUtility.DisplayDialog(
                    $"Создать предмет типа {capturedType}",
                    $"Введите имя для предмета типа {capturedType}:",
                    "Создать", "Отмена"
                ) ? "New " + capturedType.ToString() : null;

                if (!string.IsNullOrEmpty(name))
                {
                    // Запрашиваем имя через SaveFilePanel (хак, но работает)
                    string finalName = EditorUtility.SaveFilePanel(
                        $"Имя предмета (тип: {capturedType})",
                        "",
                        "New" + capturedType,
                        ""
                    );

                    // Извлекаем только имя файла без пути и расширения
                    if (!string.IsNullOrEmpty(finalName))
                    {
                        finalName = System.IO.Path.GetFileNameWithoutExtension(finalName);
                        CreateNewCustomItem(database, finalName, capturedType);
                    }
                }
            });
        }

        menu.ShowAsContext();
    }

    private void CreateNewCustomItem(ItemDatabase database, string itemName, ItemType itemType)
    {
        if (string.IsNullOrEmpty(itemName))
        {
            Debug.LogWarning("Имя предмета не может быть пустым!");
            return;
        }

        database.AddCustomItem(itemName, itemType);
        EditorUtility.SetDirty(database);

        Debug.Log($"✓ Создан новый кастомный предмет: '{itemName}' ({itemType})");
        Debug.Log($"Теперь вы можете назначить ему иконку и префаб в Inspector");
    }

    private void ShowDatabaseStatistics(ItemDatabase database)
    {
        var allItems = database.GetAllItems();

        Debug.Log("=== Статистика базы данных предметов ===");
        Debug.Log($"Предметов в коде: {database.itemsInCode.Count}");
        Debug.Log($"Кастомных предметов: {database.customItems.Count}");
        Debug.Log($"ВСЕГО предметов: {allItems.Count}");

        int withIcons = 0;
        int withPrefabs = 0;
        int complete = 0;

        foreach (var item in allItems)
        {
            if (item.icon != null)
                withIcons++;
            if (item.worldPrefab != null)
                withPrefabs++;
            if (item.icon != null && item.worldPrefab != null)
                complete++;
        }

        if (allItems.Count > 0)
        {
            Debug.Log($"Предметов с иконками: {withIcons} ({withIcons * 100 / allItems.Count}%)");
            Debug.Log($"Предметов с префабами: {withPrefabs} ({withPrefabs * 100 / allItems.Count}%)");
            Debug.Log($"Полностью готовых предметов (иконка + префаб): {complete} ({complete * 100 / allItems.Count}%)");
            Debug.Log($"Требуют внимания: {allItems.Count - complete} предметов");
        }

        // Показываем разбивку по типам
        Debug.Log("\n=== Разбивка по типам ===");
        var typeGroups = new System.Collections.Generic.Dictionary<ItemType, int>();
        foreach (var item in allItems)
        {
            if (!typeGroups.ContainsKey(item.itemType))
                typeGroups[item.itemType] = 0;
            typeGroups[item.itemType]++;
        }

        foreach (var kvp in typeGroups)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value} предметов");
        }

        // Показываем детально кастомные предметы
        if (database.customItems.Count > 0)
        {
            Debug.Log("\n=== Кастомные предметы (не в коде) ===");
            foreach (var item in database.customItems)
            {
                string status = "";
                if (item.icon != null && item.worldPrefab != null)
                    status = "✓ Готов";
                else if (item.icon != null)
                    status = "⚠ Нет префаба";
                else if (item.worldPrefab != null)
                    status = "⚠ Нет иконки";
                else
                    status = "✗ Пустой";

                Debug.Log($"- {item.itemName} ({item.itemType}) {status}");
            }
        }
    }

    /// <summary>
    /// Определить тип предмета по его имени и контексту в коде
    /// </summary>
    private ItemType GuessItemType(string itemName, string fileContext)
    {
        string lowerName = itemName.ToLower();

        // Ресурсы
        if (lowerName.Contains("metal") || lowerName.Contains("ore") || lowerName.Contains("iron") ||
            lowerName.Contains("wood") || lowerName.Contains("stone") || lowerName.Contains("crystal") ||
            lowerName.Contains("material"))
        {
            return ItemType.Resource;
        }

        // Медикаменты
        if (lowerName.Contains("medkit") || lowerName.Contains("heal") || lowerName.Contains("health") ||
            lowerName.Contains("medical") || lowerName.Contains("bandage"))
        {
            return ItemType.Medical;
        }

        // Инструменты
        if (lowerName.Contains("tool") || lowerName.Contains("pick") || lowerName.Contains("axe") ||
            lowerName.Contains("mining") || lowerName.Contains("utility"))
        {
            return ItemType.Tool;
        }

        // Оружие
        if (lowerName.Contains("weapon") || lowerName.Contains("gun") || lowerName.Contains("rifle") ||
            lowerName.Contains("sword") || lowerName.Contains("blade") || lowerName.Contains("pistol"))
        {
            return ItemType.Weapon;
        }

        // Броня
        if (lowerName.Contains("armor") || lowerName.Contains("helmet") || lowerName.Contains("shield") ||
            lowerName.Contains("pants") || lowerName.Contains("boots") || lowerName.Contains("legs") ||
            lowerName.Contains("body"))
        {
            return ItemType.Armor;
        }

        // Расходники
        if (lowerName.Contains("consumable") || lowerName.Contains("supply") || lowerName.Contains("food") ||
            lowerName.Contains("drink") || lowerName.Contains("potion"))
        {
            return ItemType.Consumable;
        }

        // Проверяем контекст в файле
        int itemIndex = fileContext.IndexOf($"\"{itemName}\"");
        if (itemIndex > 0)
        {
            // Берем 200 символов до и после упоминания предмета
            int contextStart = System.Math.Max(0, itemIndex - 200);
            int contextEnd = System.Math.Min(fileContext.Length, itemIndex + 200);
            string localContext = fileContext.Substring(contextStart, contextEnd - contextStart).ToLower();

            // Ищем подсказки в коде рядом с предметом
            if (localContext.Contains("itemtype.weapon"))
                return ItemType.Weapon;
            if (localContext.Contains("itemtype.armor"))
                return ItemType.Armor;
            if (localContext.Contains("itemtype.medical"))
                return ItemType.Medical;
            if (localContext.Contains("itemtype.tool"))
                return ItemType.Tool;
            if (localContext.Contains("itemtype.resource"))
                return ItemType.Resource;
            if (localContext.Contains("itemtype.consumable"))
                return ItemType.Consumable;
        }

        // По умолчанию - Resource
        return ItemType.Resource;
    }
}
