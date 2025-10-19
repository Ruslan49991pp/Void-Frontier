using UnityEngine;

/// <summary>
/// РњРµРЅРµРґР¶РµСЂ РґР»СЏ СѓРїСЂР°РІР»РµРЅРёСЏ РёРєРѕРЅРєР°РјРё РїСЂРµРґРјРµС‚РѕРІ
/// Р”РѕР±Р°РІРёС‚СЊ РЅР° GameObject РІ СЃС†РµРЅРµ
/// </summary>
public class ItemIconManager : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Р‘Р°Р·Р° РґР°РЅРЅС‹С… РїСЂРµРґРјРµС‚РѕРІ СЃ РёРєРѕРЅРєР°РјРё")]
    public ItemDatabase itemDatabase;

    [Tooltip("РђРІС‚РѕРјР°С‚РёС‡РµСЃРєРё РїСЂРёРјРµРЅСЏС‚СЊ РёРєРѕРЅРєРё РїСЂРё СЃС‚Р°СЂС‚Рµ")]
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
            // Проверяем, является ли объект корневым перед применением DontDestroyOnLoad
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                // Если объект дочерний, применяем DontDestroyOnLoad к корневому объекту
                DontDestroyOnLoad(transform.root.gameObject);
            }
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // РРЅРёС†РёР°Р»РёР·РёСЂСѓРµРј С„Р°Р±СЂРёРєСѓ РїСЂРµРґРјРµС‚РѕРІ
        if (itemDatabase != null)
        {
            ItemFactory.Initialize(itemDatabase);
        }
        else
        {
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
    /// РџСЂРёРјРµРЅРёС‚СЊ РёРєРѕРЅРєРё РєРѕ РІСЃРµРј РїСЂРµРґРјРµС‚Р°Рј РІ РёРіСЂРµ
    /// </summary>
    public void ApplyIconsToAllItems()
    {
        if (itemDatabase == null)
        {
            return;
        }

        // РќР°С…РѕРґРёРј РІСЃРµ Inventory РєРѕРјРїРѕРЅРµРЅС‚С‹ РІ СЃС†РµРЅРµ
        Inventory[] inventories = FindObjectsOfType<Inventory>();

        foreach (Inventory inventory in inventories)
        {
            // РћР±СЂР°Р±Р°С‚С‹РІР°РµРј РІСЃРµ СЃР»РѕС‚С‹ РёРЅРІРµРЅС‚Р°СЂСЏ
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

            // РўР°РєР¶Рµ РѕР±СЂР°Р±Р°С‚С‹РІР°РµРј СЌРєРёРїРёСЂРѕРІР°РЅРЅС‹Рµ РїСЂРµРґРјРµС‚С‹
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
    /// РџСЂРёРјРµРЅРёС‚СЊ РёРєРѕРЅРєСѓ Рє РєРѕРЅРєСЂРµС‚РЅРѕРјСѓ РїСЂРµРґРјРµС‚Сѓ
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
    /// РџРѕР»СѓС‡РёС‚СЊ РёРєРѕРЅРєСѓ РґР»СЏ РїСЂРµРґРјРµС‚Р°
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
