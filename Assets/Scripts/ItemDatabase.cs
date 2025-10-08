using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// База данных всех предметов в игре
/// Создать через Assets -> Create -> Item Database
/// </summary>
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [Header("Все предметы в игре")]
    [Tooltip("Список всех предметов с их иконками")]
    public List<ItemIconEntry> items = new List<ItemIconEntry>();

    /// <summary>
    /// Получить иконку для конкретного предмета по имени
    /// </summary>
    public Sprite GetIconForItem(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
            return null;

        foreach (var item in items)
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
    /// Добавить предмет в базу данных (для использования в Editor)
    /// </summary>
    public void AddItem(string itemName, ItemType itemType, Sprite icon)
    {
        // Проверяем, нет ли уже такого предмета
        foreach (var item in items)
        {
            if (item.itemName == itemName)
            {
                item.icon = icon;
                item.itemType = itemType;
                return;
            }
        }

        // Добавляем новый
        items.Add(new ItemIconEntry
        {
            itemName = itemName,
            itemType = itemType,
            icon = icon
        });
    }
}

/// <summary>
/// Запись о предмете с его иконкой
/// </summary>
[System.Serializable]
public class ItemIconEntry
{
    [Tooltip("Точное название предмета (должно совпадать с itemName в игре)")]
    public string itemName;

    [Tooltip("Тип предмета (для справки)")]
    public ItemType itemType;

    [Tooltip("Иконка для этого предмета")]
    public Sprite icon;
}
