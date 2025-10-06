using UnityEngine;

/// <summary>
/// Типы предметов в игре
/// </summary>
public enum ItemType
{
    Weapon,      // Оружие
    Armor,       // Броня
    Tool,        // Инструменты
    Medical,     // Медицинские принадлежности
    Resource,    // Ресурсы
    Consumable,  // Расходуемые предметы
    Misc         // Разное
}

/// <summary>
/// Редкость предмета
/// </summary>
public enum ItemRarity
{
    Common,      // Обычный
    Uncommon,    // Необычный
    Rare,        // Редкий
    Epic,        // Эпический
    Legendary    // Легендарный
}

/// <summary>
/// Слоты экипировки
/// </summary>
public enum EquipmentSlot
{
    None,        // Не экипируется
    LeftHand,    // Левая рука
    RightHand,   // Правая рука
    Head,        // Голова
    Chest,       // Грудь
    Legs,        // Ноги
    Feet         // Ступни
}

/// <summary>
/// Данные предмета
/// </summary>
[System.Serializable]
public class ItemData
{
    [Header("Basic Info")]
    public string itemName = "Unknown Item";
    public string description = "";
    public ItemType itemType = ItemType.Misc;
    public ItemRarity rarity = ItemRarity.Common;

    [Header("Properties")]
    public int maxStackSize = 1;  // Максимальное количество в стеке
    public float weight = 1f;     // Вес предмета
    public int value = 0;         // Ценность
    public EquipmentSlot equipmentSlot = EquipmentSlot.None; // Слот экипировки

    [Header("Visual")]
    public Sprite icon;           // Иконка для UI
    public GameObject prefab;     // Префаб для отображения в мире

    [Header("Stats")]
    public int damage = 0;        // Урон (для оружия)
    public int armor = 0;         // Защита (для брони)
    public int healing = 0;       // Лечение (для медицинских предметов)

    /// <summary>
    /// Получить цвет редкости
    /// </summary>
    public Color GetRarityColor()
    {
        switch (rarity)
        {
            case ItemRarity.Common:    return Color.white;
            case ItemRarity.Uncommon:  return Color.green;
            case ItemRarity.Rare:      return Color.blue;
            case ItemRarity.Epic:      return Color.magenta;
            case ItemRarity.Legendary: return Color.yellow;
            default: return Color.white;
        }
    }

    /// <summary>
    /// Получить описание с характеристиками
    /// </summary>
    public string GetFullDescription()
    {
        string fullDesc = description;

        if (damage > 0)
            fullDesc += $"\nДамаж: {damage}";
        if (armor > 0)
            fullDesc += $"\nЗащита: {armor}";
        if (healing > 0)
            fullDesc += $"\nЛечение: {healing}";

        if (equipmentSlot != EquipmentSlot.None)
            fullDesc += $"\nСлот: {GetEquipmentSlotName()}";

        fullDesc += $"\nВес: {weight}";
        fullDesc += $"\nЦенность: {value}";

        return fullDesc;
    }

    /// <summary>
    /// Получить название слота экипировки
    /// </summary>
    public string GetEquipmentSlotName()
    {
        switch (equipmentSlot)
        {
            case EquipmentSlot.LeftHand: return "Левая рука";
            case EquipmentSlot.RightHand: return "Правая рука";
            case EquipmentSlot.Head: return "Голова";
            case EquipmentSlot.Chest: return "Грудь";
            case EquipmentSlot.Legs: return "Ноги";
            case EquipmentSlot.Feet: return "Ступни";
            default: return "Не экипируется";
        }
    }

    /// <summary>
    /// Можно ли экипировать предмет
    /// </summary>
    public bool CanBeEquipped()
    {
        return equipmentSlot != EquipmentSlot.None;
    }
}

/// <summary>
/// Компонент предмета
/// </summary>
public class Item : MonoBehaviour
{
    [Header("Item Data")]
    public ItemData itemData;

    [Header("World Item Settings")]
    public bool canBePickedUp = true;
    public float pickupRange = 2f;

    // Внутренние переменные
    private Collider itemCollider;
    private Renderer itemRenderer;

    void Awake()
    {
        itemCollider = GetComponent<Collider>();
        itemRenderer = GetComponent<Renderer>();

        // Настраиваем коллайдер как триггер для взаимодействия
        // Это позволит:
        // 1. Выстрелы будут проходить сквозь предметы
        // 2. Предметы можно будет выделять (RaycastAll с QueryTriggerInteraction.Collide)
        // 3. Можно будет поднимать предметы
        if (itemCollider != null)
        {
            itemCollider.isTrigger = true;
        }

        // Если данные предмета не заданы, создаем базовые
        if (itemData == null)
        {
            itemData = new ItemData();
            itemData.itemName = gameObject.name;
        }
    }

    /// <summary>
    /// Получить данные предмета
    /// </summary>
    public ItemData GetItemData()
    {
        return itemData;
    }

    /// <summary>
    /// Установить данные предмета
    /// </summary>
    public void SetItemData(ItemData data)
    {
        itemData = data;

        // Обновляем визуал если есть префаб
        if (itemData.prefab != null && itemRenderer != null)
        {
            // Здесь можно добавить логику замены модели
        }
    }

    /// <summary>
    /// Проверить, может ли персонаж поднять предмет
    /// </summary>
    public bool CanBePickedUpBy(Character character)
    {
        if (!canBePickedUp || character == null)
            return false;

        float distance = Vector3.Distance(transform.position, character.transform.position);
        return distance <= pickupRange;
    }

    /// <summary>
    /// Поднять предмет
    /// </summary>
    public void PickUp(Character character)
    {
        if (!CanBePickedUpBy(character))
            return;

        // Получаем инвентарь персонажа
        Inventory inventory = character.GetComponent<Inventory>();
        if (inventory != null)
        {
            // Пытаемся добавить предмет в инвентарь
            if (inventory.AddItem(itemData, 1))
            {
                // Успешно добавлен - удаляем объект из мира
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// Создать предмет в мире
    /// </summary>
    public static GameObject CreateWorldItem(ItemData itemData, Vector3 position)
    {
        GameObject worldItem;

        // Если есть префаб, используем его
        if (itemData.prefab != null)
        {
            worldItem = Instantiate(itemData.prefab, position, Quaternion.identity);
        }
        else
        {
            // Создаем простой куб как fallback
            worldItem = GameObject.CreatePrimitive(PrimitiveType.Cube);
            worldItem.transform.position = position;
            worldItem.transform.localScale = Vector3.one * 0.5f;
        }

        // Добавляем компонент Item
        Item itemComponent = worldItem.GetComponent<Item>();
        if (itemComponent == null)
        {
            itemComponent = worldItem.AddComponent<Item>();
        }

        itemComponent.SetItemData(itemData);
        worldItem.name = itemData.itemName;

        return worldItem;
    }

    void OnDrawGizmosSelected()
    {
        // Показываем радиус подбора
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}