using UnityEngine;

/// <summary>
/// Р¤Р°Р±СЂРёРєР° РґР»СЏ СЃРѕР·РґР°РЅРёСЏ РїСЂРµРґРјРµС‚РѕРІ СЃ Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРёРј РїСЂРёРјРµРЅРµРЅРёРµРј РёРєРѕРЅРѕРє
/// </summary>
public static class ItemFactory
{
    private static ItemDatabase itemDatabase;

    /// <summary>
    /// РРЅРёС†РёР°Р»РёР·РёСЂРѕРІР°С‚СЊ С„Р°Р±СЂРёРєСѓ СЃ Р±Р°Р·РѕР№ РґР°РЅРЅС‹С…
    /// </summary>
    public static void Initialize(ItemDatabase database)
    {
        itemDatabase = database;
    }

    /// <summary>
    /// РЎРѕР·РґР°С‚СЊ РїСЂРµРґРјРµС‚ СЃ Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРёРј РїСЂРёРјРµРЅРµРЅРёРµРј РёРєРѕРЅРєРё
    /// </summary>
    public static ItemData CreateItem(string itemName, ItemType itemType, EquipmentSlot equipSlot = EquipmentSlot.None)
    {
        ItemData item = new ItemData();
        item.itemName = itemName;
        item.itemType = itemType;
        item.equipmentSlot = equipSlot;

        // РђРІС‚РѕРјР°С‚РёС‡РµСЃРєРё РїСЂРёРјРµРЅСЏРµРј РёРєРѕРЅРєСѓ
        ApplyIcon(item);

        return item;
    }

    /// <summary>
    /// РџСЂРёРјРµРЅРёС‚СЊ РёРєРѕРЅРєСѓ Рє СЃСѓС‰РµСЃС‚РІСѓСЋС‰РµРјСѓ РїСЂРµРґРјРµС‚Сѓ
    /// </summary>
    public static void ApplyIcon(ItemData item)
    {
        if (item == null)
        {
            return;
        }

        if (itemDatabase == null)
        {
            return;
        }

        Sprite icon = itemDatabase.GetIcon(item.itemName, item.itemType);

        if (icon != null)
        {
            item.icon = icon;
        }
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ Р±Р°Р·Сѓ РґР°РЅРЅС‹С…
    /// </summary>
    public static ItemDatabase GetDatabase()
    {
        return itemDatabase;
    }
}
