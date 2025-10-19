using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Р‘Р°Р·Р° РґР°РЅРЅС‹С… РІСЃРµС… РїСЂРµРґРјРµС‚РѕРІ РІ РёРіСЂРµ
/// РЎРѕР·РґР°С‚СЊ С‡РµСЂРµР· Assets -> Create -> Item Database
/// </summary>
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [Header("РџСЂРµРґРјРµС‚С‹ РЅР°Р№РґРµРЅРЅС‹Рµ РІ РєРѕРґРµ")]
    [Tooltip("РџСЂРµРґРјРµС‚С‹ РєРѕС‚РѕСЂС‹Рµ РёСЃРїРѕР»СЊР·СѓСЋС‚СЃСЏ РІ РєРѕРґРµ РёРіСЂС‹")]
    public List<ItemIconEntry> itemsInCode = new List<ItemIconEntry>();

    [Header("Р”РѕРїРѕР»РЅРёС‚РµР»СЊРЅС‹Рµ РїСЂРµРґРјРµС‚С‹")]
    [Tooltip("РџСЂРµРґРјРµС‚С‹ СЃРѕР·РґР°РЅРЅС‹Рµ РІСЂСѓС‡РЅСѓСЋ, РєРѕС‚РѕСЂС‹Рµ РїРѕРєР° РЅРµ РёСЃРїРѕР»СЊР·СѓСЋС‚СЃСЏ РІ РєРѕРґРµ")]
    public List<ItemIconEntry> customItems = new List<ItemIconEntry>();

    [System.NonSerialized]
    private List<ItemIconEntry> _allItemsCache = null;

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РІСЃРµ РїСЂРµРґРјРµС‚С‹ (РёР· РєРѕРґР° + РєР°СЃС‚РѕРјРЅС‹Рµ)
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
    /// РЎР±СЂРѕСЃРёС‚СЊ РєСЌС€ (РІС‹Р·С‹РІР°С‚СЊ РїРѕСЃР»Рµ РёР·РјРµРЅРµРЅРёР№)
    /// </summary>
    public void RefreshCache()
    {
        _allItemsCache = null;
    }

    // Р”Р»СЏ РѕР±СЂР°С‚РЅРѕР№ СЃРѕРІРјРµСЃС‚РёРјРѕСЃС‚Рё СЃРѕ СЃС‚Р°СЂС‹Рј РєРѕРґРѕРј
    [System.Obsolete("РСЃРїРѕР»СЊР·СѓР№С‚Рµ itemsInCode РёР»Рё customItems")]
    public List<ItemIconEntry> items
    {
        get { return GetAllItems(); }
        set
        {
            // РњРёРіСЂР°С†РёСЏ СЃС‚Р°СЂС‹С… РґР°РЅРЅС‹С…
            if (value != null && value.Count > 0)
            {
                itemsInCode = value;
                RefreshCache();
            }
        }
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РёРєРѕРЅРєСѓ РґР»СЏ РєРѕРЅРєСЂРµС‚РЅРѕРіРѕ РїСЂРµРґРјРµС‚Р° РїРѕ РёРјРµРЅРё
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
    /// РџРѕР»СѓС‡РёС‚СЊ РёРєРѕРЅРєСѓ РґР»СЏ РїСЂРµРґРјРµС‚Р°
    /// </summary>
    public Sprite GetIcon(string itemName, ItemType itemType)
    {
        // РС‰РµРј РїРѕ С‚РѕС‡РЅРѕРјСѓ РёРјРµРЅРё, РµСЃР»Рё РЅРµ РЅР°Р№РґРµРЅРѕ - РІРѕР·РІСЂР°С‰Р°РµРј null
        return GetIconForItem(itemName);
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РїСЂРµС„Р°Р± РґР»СЏ РєРѕРЅРєСЂРµС‚РЅРѕРіРѕ РїСЂРµРґРјРµС‚Р° РїРѕ РёРјРµРЅРё
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
    /// РџРѕР»СѓС‡РёС‚СЊ РїСЂРµС„Р°Р± РґР»СЏ РїСЂРµРґРјРµС‚Р°
    /// </summary>
    public GameObject GetPrefab(string itemName, ItemType itemType)
    {
        // РС‰РµРј РїРѕ С‚РѕС‡РЅРѕРјСѓ РёРјРµРЅРё, РµСЃР»Рё РЅРµ РЅР°Р№РґРµРЅРѕ - РІРѕР·РІСЂР°С‰Р°РµРј null
        return GetPrefabForItem(itemName);
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РїРѕР»РЅСѓСЋ Р·Р°РїРёСЃСЊ Рѕ РїСЂРµРґРјРµС‚Рµ
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
    /// Р”РѕР±Р°РІРёС‚СЊ РїСЂРµРґРјРµС‚ РІ Р±Р°Р·Сѓ РґР°РЅРЅС‹С… (РґР»СЏ РёСЃРїРѕР»СЊР·РѕРІР°РЅРёСЏ РІ Editor)
    /// </summary>
    public void AddItem(string itemName, ItemType itemType, Sprite icon, GameObject prefab = null, bool isCustom = false)
    {
        RefreshCache();

        var targetList = isCustom ? customItems : itemsInCode;
        var allItems = GetAllItems();

        // РџСЂРѕРІРµСЂСЏРµРј, РЅРµС‚ Р»Рё СѓР¶Рµ С‚Р°РєРѕРіРѕ РїСЂРµРґРјРµС‚Р° РІРѕ РІСЃРµС… СЃРїРёСЃРєР°С…
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

        // Р”РѕР±Р°РІР»СЏРµРј РЅРѕРІС‹Р№
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
    /// Р”РѕР±Р°РІРёС‚СЊ РєР°СЃС‚РѕРјРЅС‹Р№ РїСЂРµРґРјРµС‚ (СЃРѕР·РґР°РЅРЅС‹Р№ РІСЂСѓС‡РЅСѓСЋ)
    /// </summary>
    public void AddCustomItem(string itemName, ItemType itemType)
    {
        // РџСЂРѕРІРµСЂСЏРµРј С‡С‚Рѕ С‚Р°РєРѕРіРѕ РїСЂРµРґРјРµС‚Р° РµС‰Рµ РЅРµС‚
        var allItems = GetAllItems();
        foreach (var item in allItems)
        {
            if (item.itemName == itemName)
            {
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
    /// РЈРґР°Р»РёС‚СЊ РєР°СЃС‚РѕРјРЅС‹Р№ РїСЂРµРґРјРµС‚
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
/// Р—Р°РїРёСЃСЊ Рѕ РїСЂРµРґРјРµС‚Рµ СЃ РµРіРѕ РёРєРѕРЅРєРѕР№ Рё РїСЂРµС„Р°Р±РѕРј
/// </summary>
[System.Serializable]
public class ItemIconEntry
{
    [Header("РћСЃРЅРѕРІРЅР°СЏ РёРЅС„РѕСЂРјР°С†РёСЏ")]
    [Tooltip("РўРѕС‡РЅРѕРµ РЅР°Р·РІР°РЅРёРµ РїСЂРµРґРјРµС‚Р° (РґРѕР»Р¶РЅРѕ СЃРѕРІРїР°РґР°С‚СЊ СЃ itemName РІ РёРіСЂРµ)")]
    public string itemName;

    [Tooltip("РўРёРї РїСЂРµРґРјРµС‚Р° (РґР»СЏ СЃРїСЂР°РІРєРё)")]
    public ItemType itemType;

    [Tooltip("РќР°Р№РґРµРЅ РІ РєРѕРґРµ (С‡РµСЂРµР· СЃРєР°РЅРёСЂРѕРІР°РЅРёРµ)")]
    public bool foundInCode = false;

    [Header("Р’РёР·СѓР°Р»РёР·Р°С†РёСЏ")]
    [Tooltip("РРєРѕРЅРєР° РґР»СЏ РёРЅРІРµРЅС‚Р°СЂСЏ")]
    public Sprite icon;

    [Tooltip("РџСЂРµС„Р°Р± РґР»СЏ РѕС‚РѕР±СЂР°Р¶РµРЅРёСЏ РїСЂРµРґРјРµС‚Р° РЅР° РєР°СЂС‚Рµ (РЅР° РїРѕР»Сѓ)")]
    public GameObject worldPrefab;
}
