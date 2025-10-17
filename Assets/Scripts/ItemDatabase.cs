using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// База данных всех предметов в игре
/// Создать через Assets -> Create -> Item Database
/// </summary>
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [Header("Предметы найденные в коде")]
    [Tooltip("Предметы которые используются в коде игры")]
    public List<ItemIconEntry> itemsInCode = new List<ItemIconEntry>();

    [Header("Дополнительные предметы")]
    [Tooltip("Предметы созданные вручную, которые пока не используются в коде")]
    public List<ItemIconEntry> customItems = new List<ItemIconEntry>();

    [System.NonSerialized]
    private List<ItemIconEntry> _allItemsCache = null;

    /// <summary>
    /// Получить все предметы (из кода + кастомные)
    /// </summary>
    public List<ItemIconEntry> GetAllItems()
    {
        if (_allItemsCache == null)
        {
            _allItemsCache = new List<ItemIconEntry>();
            _allItemsCache.AddRange(itemsInCode);
            _allItemsCache.AddRange(customItems);
        }
        return _allItemsCache;
    }

    /// <summary>
    /// Сбросить кэш (вызывать после изменений)
    /// </summary>
    public void RefreshCache()
    {
        _allItemsCache = null;
    }

    // Для обратной совместимости со старым кодом
    [System.Obsolete("Используйте itemsInCode или customItems")]
    public List<ItemIconEntry> items
    {
        get { return GetAllItems(); }
        set
        {
            // Миграция старых данных
            if (value != null && value.Count > 0)
            {
                itemsInCode = value;
                RefreshCache();
            }
        }
    }

    /// <summary>
    /// Получить иконку для конкретного предмета по имени
    /// </summary>
    public Sprite GetIconForItem(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
            return null;

        var allItems = GetAllItems();
        foreach (var item in allItems)
        {
            if (item.itemName.Trim().Equals(itemName.Trim(), System.StringComparison.OrdinalIgnoreCase))
            {
                return item.icon;
            }
        }

        return null;
    }

    /// <summary>
    /// Получить иконку для предмета
    /// </summary>
    public Sprite GetIcon(string itemName, ItemType itemType)
    {
        // Ищем по точному имени, если не найдено - возвращаем null
        return GetIconForItem(itemName);
    }

    /// <summary>
    /// Получить префаб для конкретного предмета по имени
    /// </summary>
    public GameObject GetPrefabForItem(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
            return null;

        var allItems = GetAllItems();
        foreach (var item in allItems)
        {
            if (item.itemName.Trim().Equals(itemName.Trim(), System.StringComparison.OrdinalIgnoreCase))
            {
                return item.worldPrefab;
            }
        }

        return null;
    }

    /// <summary>
    /// Получить префаб для предмета
    /// </summary>
    public GameObject GetPrefab(string itemName, ItemType itemType)
    {
        // Ищем по точному имени, если не найдено - возвращаем null
        return GetPrefabForItem(itemName);
    }

    /// <summary>
    /// Получить полную запись о предмете
    /// </summary>
    public ItemIconEntry GetItemEntry(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
            return null;

        var allItems = GetAllItems();
        foreach (var item in allItems)
        {
            if (item.itemName.Trim().Equals(itemName.Trim(), System.StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }
        }

        return null;
    }

    /// <summary>
    /// Добавить предмет в базу данных (для использования в Editor)
    /// </summary>
    public void AddItem(string itemName, ItemType itemType, Sprite icon, GameObject prefab = null, bool isCustom = false)
    {
        RefreshCache();

        var targetList = isCustom ? customItems : itemsInCode;
        var allItems = GetAllItems();

        // Проверяем, нет ли уже такого предмета во всех списках
        foreach (var item in allItems)
        {
            if (item.itemName == itemName)
            {
                item.icon = icon;
                item.itemType = itemType;
                if (prefab != null)
                    item.worldPrefab = prefab;
                RefreshCache();
                return;
            }
        }

        // Добавляем новый
        targetList.Add(new ItemIconEntry
        {
            itemName = itemName,
            itemType = itemType,
            icon = icon,
            worldPrefab = prefab
        });

        RefreshCache();
    }

    /// <summary>
    /// Добавить кастомный предмет (созданный вручную)
    /// </summary>
    public void AddCustomItem(string itemName, ItemType itemType)
    {
        // Проверяем что такого предмета еще нет
        var allItems = GetAllItems();
        foreach (var item in allItems)
        {
            if (item.itemName == itemName)
            {
                Debug.LogWarning($"Предмет '{itemName}' уже существует в базе данных!");
                return;
            }
        }

        customItems.Add(new ItemIconEntry
        {
            itemName = itemName,
            itemType = itemType,
            icon = null,
            worldPrefab = null
        });

        RefreshCache();
    }

    /// <summary>
    /// Удалить кастомный предмет
    /// </summary>
    public bool RemoveCustomItem(string itemName)
    {
        for (int i = customItems.Count - 1; i >= 0; i--)
        {
            if (customItems[i].itemName == itemName)
            {
                customItems.RemoveAt(i);
                RefreshCache();
                return true;
            }
        }
        return false;
    }
}

/// <summary>
/// Запись о предмете с его иконкой и префабом
/// </summary>
[System.Serializable]
public class ItemIconEntry
{
    [Header("Основная информация")]
    [Tooltip("Точное название предмета (должно совпадать с itemName в игре)")]
    public string itemName;

    [Tooltip("Тип предмета (для справки)")]
    public ItemType itemType;

    [Tooltip("Найден в коде (через сканирование)")]
    public bool foundInCode = false;

    [Header("Визуализация")]
    [Tooltip("Иконка для инвентаря")]
    public Sprite icon;

    [Tooltip("Префаб для отображения предмета на карте (на полу)")]
    public GameObject worldPrefab;
}
