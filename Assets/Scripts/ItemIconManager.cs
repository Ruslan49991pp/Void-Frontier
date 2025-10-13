using UnityEngine;

/// <summary>
/// Менеджер для управления иконками предметов
/// Добавить на GameObject в сцене
/// </summary>
public class ItemIconManager : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("База данных предметов с иконками")]
    public ItemDatabase itemDatabase;

    [Tooltip("Автоматически применять иконки при старте")]
    public bool applyIconsOnStart = true;

    private static ItemIconManager instance;

    public static ItemIconManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ItemIconManager>();
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Инициализируем фабрику предметов
        if (itemDatabase != null)
        {
            ItemFactory.Initialize(itemDatabase);
        }
        else
        {
            Debug.LogError("ItemIconManager: ItemDatabase не назначена!");
        }
    }

    void Start()
    {
        if (applyIconsOnStart && itemDatabase != null)
        {
            ApplyIconsToAllItems();
        }
    }

    /// <summary>
    /// Применить иконки ко всем предметам в игре
    /// </summary>
    public void ApplyIconsToAllItems()
    {
        if (itemDatabase == null)
        {
            return;
        }

        // Находим все Inventory компоненты в сцене
        Inventory[] inventories = FindObjectsOfType<Inventory>();

        foreach (Inventory inventory in inventories)
        {
            // Обрабатываем все слоты инвентаря
            var allSlots = inventory.GetAllSlots();
            if (allSlots != null)
            {
                foreach (InventorySlot slot in allSlots)
                {
                    if (slot != null && !slot.IsEmpty() && slot.itemData != null)
                    {
                        ApplyIconToItem(slot.itemData);
                    }
                }
            }

            // Также обрабатываем экипированные предметы
            var equipmentSlots = inventory.GetAllEquipmentSlots();
            if (equipmentSlots != null)
            {
                foreach (var kvp in equipmentSlots)
                {
                    InventorySlot slot = kvp.Value;
                    if (slot != null && !slot.IsEmpty() && slot.itemData != null)
                    {
                        ApplyIconToItem(slot.itemData);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Применить иконку к конкретному предмету
    /// </summary>
    public bool ApplyIconToItem(ItemData item)
    {
        if (item == null || itemDatabase == null)
            return false;

        Sprite icon = itemDatabase.GetIcon(item.itemName, item.itemType);

        if (icon != null)
        {
            item.icon = icon;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Получить иконку для предмета
    /// </summary>
    public Sprite GetIcon(string itemName, ItemType itemType)
    {
        if (itemDatabase == null)
        {
            return null;
        }

        return itemDatabase.GetIcon(itemName, itemType);
    }
}
