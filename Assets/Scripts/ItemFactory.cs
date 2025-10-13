using UnityEngine;

/// <summary>
/// Фабрика для создания предметов с автоматическим применением иконок
/// </summary>
public static class ItemFactory
{
    private static ItemDatabase itemDatabase;

    /// <summary>
    /// Инициализировать фабрику с базой данных
    /// </summary>
    public static void Initialize(ItemDatabase database)
    {
        itemDatabase = database;
    }

    /// <summary>
    /// Создать предмет с автоматическим применением иконки
    /// </summary>
    public static ItemData CreateItem(string itemName, ItemType itemType, EquipmentSlot equipSlot = EquipmentSlot.None)
    {
        ItemData item = new ItemData();
        item.itemName = itemName;
        item.itemType = itemType;
        item.equipmentSlot = equipSlot;

        // Автоматически применяем иконку
        ApplyIcon(item);

        return item;
    }

    /// <summary>
    /// Применить иконку к существующему предмету
    /// </summary>
    public static void ApplyIcon(ItemData item)
    {
        if (item == null)
        {
            Debug.LogWarning("ItemFactory.ApplyIcon: item == null");
            return;
        }

        if (itemDatabase == null)
        {
            Debug.LogWarning($"ItemFactory.ApplyIcon: База данных не инициализирована! Предмет: {item.itemName}");
            return;
        }

        Sprite icon = itemDatabase.GetIcon(item.itemName, item.itemType);

        if (icon != null)
        {
            item.icon = icon;
        }
    }

    /// <summary>
    /// Получить базу данных
    /// </summary>
    public static ItemDatabase GetDatabase()
    {
        return itemDatabase;
    }
}
