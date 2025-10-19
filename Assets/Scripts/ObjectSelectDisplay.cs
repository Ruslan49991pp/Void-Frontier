using UnityEngine;
using TMPro;

/// <summary>
/// РљРѕРјРїРѕРЅРµРЅС‚ РґР»СЏ РѕС‚РѕР±СЂР°Р¶РµРЅРёСЏ РёРЅС„РѕСЂРјР°С†РёРё Рѕ РІС‹РґРµР»РµРЅРЅРѕРј РѕР±СЉРµРєС‚Рµ (СЃС‚РµРЅР°, РєРѕРјРЅР°С‚Р°, РјРѕРґСѓР»СЊ) РІ РїР°РЅРµР»Рё ObjectSelect
/// </summary>
public class ObjectSelectDisplay : MonoBehaviour
{
    [Header("UI References")]
    public GameObject objectSelectPanel; // Р“Р»Р°РІРЅР°СЏ РїР°РЅРµР»СЊ ObjectSelect
    public TMP_Text textObjectInfo; // TextObjectInfo РґР»СЏ РѕС‚РѕР±СЂР°Р¶РµРЅРёСЏ РёРЅС„РѕСЂРјР°С†РёРё

    // РўРµРєСѓС‰РёР№ РІС‹РґРµР»РµРЅРЅС‹Р№ РѕР±СЉРµРєС‚
    private GameObject currentObject;
    private SelectionManager selectionManager;

    void Awake()
    {
        // РђРІС‚РѕРјР°С‚РёС‡РµСЃРєРё РЅР°С…РѕРґРёРј UI СЌР»РµРјРµРЅС‚С‹ РµСЃР»Рё РЅРµ РЅР°Р·РЅР°С‡РµРЅС‹
        if (objectSelectPanel == null)
        {
            objectSelectPanel = gameObject;
        }

        // РџС‹С‚Р°РµРјСЃСЏ РЅР°Р№С‚Рё TextObjectInfo РµСЃР»Рё РЅРµ РЅР°Р·РЅР°С‡РµРЅ
        if (textObjectInfo == null)
        {
            Transform textTransform = FindTransformRecursive(transform, "TextObjectInfo");
            if (textTransform != null)
            {
                textObjectInfo = textTransform.GetComponent<TMP_Text>();
            }
        }

        // РќР• СЃРєСЂС‹РІР°РµРј РїР°РЅРµР»СЊ Р·РґРµСЃСЊ, РёРЅР°С‡Рµ OnEnable() РЅРµ РІС‹Р·РѕРІРµС‚СЃСЏ
        // РўРѕР»СЊРєРѕ РѕС‡РёС‰Р°РµРј С‚РµРєСЃС‚
        if (textObjectInfo != null)
        {
            textObjectInfo.text = "";
        }
    }

    private bool isSubscribed = false; // Р¤Р»Р°Рі РїРѕРґРїРёСЃРєРё РЅР° СЃРѕР±С‹С‚РёСЏ

    void Start()
    {
        // РќР°С…РѕРґРёРј SelectionManager РІ СЃС†РµРЅРµ РµСЃР»Рё РµС‰Рµ РЅРµ РЅР°С€Р»Рё
        if (selectionManager == null)
        {
            selectionManager = FindObjectOfType<SelectionManager>();
        }

        // РџРѕРґРїРёСЃС‹РІР°РµРјСЃСЏ РЅР° СЃРѕР±С‹С‚РёСЏ РІС‹РґРµР»РµРЅРёСЏ РћР”РРќ Р РђР—
        if (selectionManager != null && !isSubscribed)
        {
            selectionManager.OnSelectionChanged += OnSelectionChanged;
            isSubscribed = true;
        }

        // РЎРєСЂС‹РІР°РµРј РїР°РЅРµР»СЊ РїРѕСЃР»Рµ РёРЅРёС†РёР°Р»РёР·Р°С†РёРё
        if (objectSelectPanel != null)
        {
            objectSelectPanel.SetActive(false);
        }
    }

    void OnDestroy()
    {
        // РћС‚РїРёСЃС‹РІР°РµРјСЃСЏ РѕС‚ СЃРѕР±С‹С‚РёР№ С‚РѕР»СЊРєРѕ РїСЂРё СѓРЅРёС‡С‚РѕР¶РµРЅРёРё РєРѕРјРїРѕРЅРµРЅС‚Р°
        if (selectionManager != null && isSubscribed)
        {
            selectionManager.OnSelectionChanged -= OnSelectionChanged;
            isSubscribed = false;
        }
    }

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚С‡РёРє РёР·РјРµРЅРµРЅРёСЏ РІС‹РґРµР»РµРЅРёСЏ
    /// </summary>
    void OnSelectionChanged(System.Collections.Generic.List<GameObject> selectedObjects)
    {
        // РЎР±СЂР°СЃС‹РІР°РµРј С‚РµРєСѓС‰РёР№ РѕР±СЉРµРєС‚
        currentObject = null;

        // РџСЂРѕРІРµСЂСЏРµРј, РІС‹РґРµР»РµРЅ Р»Рё РѕРґРёРЅ РѕР±СЉРµРєС‚
        if (selectedObjects.Count == 1)
        {
            GameObject selectedObject = selectedObjects[0];

            // РљР РРўРР§Р•РЎРљР Р’РђР–РќРћ: РџСЂРѕРІРµСЂСЏРµРј С‡С‚Рѕ РѕР±СЉРµРєС‚ РЅРµ Р±С‹Р» СѓРЅРёС‡С‚РѕР¶РµРЅ
            if (ReferenceEquals(selectedObject, null) || selectedObject == null)
            {
                HidePanel();
                return;
            }

            // РџСЂРѕРІРµСЂСЏРµРј, РќР• СЏРІР»СЏРµС‚СЃСЏ Р»Рё СЌС‚Рѕ РїРµСЂСЃРѕРЅР°Р¶РµРј РёР»Рё РІСЂР°РіРѕРј
            Character character = selectedObject.GetComponent<Character>();
            if (character != null)
            {
                // Р­С‚Рѕ РїРµСЂСЃРѕРЅР°Р¶ РёР»Рё РІСЂР°Рі - РЅРµ РїРѕРєР°Р·С‹РІР°РµРј РїР°РЅРµР»СЊ РѕР±СЉРµРєС‚РѕРІ
                HidePanel();
                return;
            }

            // РџСЂРѕРІРµСЂСЏРµРј РЅР°Р»РёС‡РёРµ LocationObjectInfo, RoomInfo РёР»Рё Item
            LocationObjectInfo locationInfo = selectedObject.GetComponent<LocationObjectInfo>();
            RoomInfo roomInfo = selectedObject.GetComponent<RoomInfo>();
            Item item = null;

            // Р—РђР©РРўРђ: Р‘РµР·РѕРїР°СЃРЅРѕ РїРѕР»СѓС‡Р°РµРј Item РєРѕРјРїРѕРЅРµРЅС‚
            try
            {
                item = selectedObject.GetComponent<Item>();
                if (item != null && ReferenceEquals(item, null))
                {
                    item = null;
                }
            }
            catch (System.Exception ex)
            {
                item = null;
            }

            // Р•СЃР»Рё РµСЃС‚СЊ РёРЅС„РѕСЂРјР°С†РёСЏ РѕР± РѕР±СЉРµРєС‚Рµ - РїРѕРєР°Р·С‹РІР°РµРј РїР°РЅРµР»СЊ
            if (locationInfo != null || roomInfo != null || item != null)
            {
                currentObject = selectedObject;
                UpdateObjectInfo();
                ShowPanel();
                return;
            }
        }

        // Р•СЃР»Рё РЅРёС‡РµРіРѕ РЅРµ РІС‹РґРµР»РµРЅРѕ РёР»Рё РІС‹РґРµР»РµРЅРѕ РЅРµСЃРєРѕР»СЊРєРѕ РѕР±СЉРµРєС‚РѕРІ - СЃРєСЂС‹РІР°РµРј РїР°РЅРµР»СЊ
        HidePanel();
    }

    /// <summary>
    /// РћР±РЅРѕРІРёС‚СЊ РІСЃСЋ РёРЅС„РѕСЂРјР°С†РёСЋ РѕР± РѕР±СЉРµРєС‚Рµ
    /// </summary>
    void UpdateObjectInfo()
    {
        if (currentObject == null || textObjectInfo == null)
        {
            return;
        }

        // РљР РРўРР§Р•РЎРљР Р’РђР–РќРћ: РџСЂРѕРІРµСЂСЏРµРј С‡С‚Рѕ currentObject РЅРµ Р±С‹Р» СѓРЅРёС‡С‚РѕР¶РµРЅ
        if (ReferenceEquals(currentObject, null))
        {
            HidePanel();
            return;
        }

        string infoText = "";

        try
        {
            // РџСЂРѕРІРµСЂСЏРµРј RoomInfo
            RoomInfo roomInfo = currentObject.GetComponent<RoomInfo>();
            if (roomInfo != null)
            {
                infoText = GetRoomInfoText(roomInfo);
            }
            else
            {
                // РџСЂРѕРІРµСЂСЏРµРј Item
                Item item = null;
                try
                {
                    item = currentObject.GetComponent<Item>();
                    // Р”РѕРїРѕР»РЅРёС‚РµР»СЊРЅР°СЏ РїСЂРѕРІРµСЂРєР° С‡С‚Рѕ Item РЅРµ СѓРЅРёС‡С‚РѕР¶РµРЅ
                    if (item != null && ReferenceEquals(item, null))
                    {
                        item = null;
                    }
                }
                catch (System.Exception ex)
                {
                    item = null;
                }

                if (item != null && !ReferenceEquals(item, null))
                {
                    // Р—РђР©РРўРђ: Р‘РµР·РѕРїР°СЃРЅРѕ РїРѕР»СѓС‡Р°РµРј itemData СЃ РґРѕРїРѕР»РЅРёС‚РµР»СЊРЅРѕР№ РїСЂРѕРІРµСЂРєРѕР№
                    ItemData itemData = null;
                    try
                    {
                        itemData = item.itemData;
                    }
                    catch (System.Exception ex)
                    {
                        itemData = null;
                    }

                    if (itemData != null)
                    {
                        infoText = GetItemInfoText(item);
                    }
                }
                else
                {
                    // РџСЂРѕРІРµСЂСЏРµРј LocationObjectInfo
                    LocationObjectInfo locationInfo = currentObject.GetComponent<LocationObjectInfo>();
                    if (locationInfo != null)
                    {
                        infoText = GetLocationInfoText(locationInfo);
                    }
                }
            }

            // Р’С‹РІРѕРґРёРј РёРЅС„РѕСЂРјР°С†РёСЋ РІ TextObjectInfo
            textObjectInfo.text = infoText;
        }
        catch (System.Exception ex)
        {
            HidePanel();
        }
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ С‚РµРєСЃС‚ РёРЅС„РѕСЂРјР°С†РёРё Рѕ РєРѕРјРЅР°С‚Рµ
    /// </summary>
    string GetRoomInfoText(RoomInfo roomInfo)
    {
        string text = "";

        // РРјСЏ РєРѕРјРЅР°С‚С‹
        if (!string.IsNullOrEmpty(roomInfo.roomName))
        {
            text += $"Room: {roomInfo.roomName}\n";
        }

        // РўРёРї РєРѕРјРЅР°С‚С‹
        if (!string.IsNullOrEmpty(roomInfo.roomType))
        {
            text += $"Type: {roomInfo.roomType}\n";
        }

        // Р Р°Р·РјРµСЂ
        text += $"Size: {roomInfo.roomSize.x}x{roomInfo.roomSize.y}\n";

        // РџРѕР·РёС†РёСЏ
        text += $"Position: ({roomInfo.gridPosition.x}, {roomInfo.gridPosition.y})\n";

        // Р—РґРѕСЂРѕРІСЊРµ СЃС‚РµРЅ
        text += $"Wall Health: {roomInfo.currentWallHealth:F0}/{roomInfo.maxWallHealth:F0}";

        // Р“Р»Р°РІРЅС‹Р№ РѕР±СЉРµРєС‚
        if (roomInfo.mainObject != null)
        {
            text += $"\n\nMain Object: {roomInfo.mainObject.objectName}";
        }
        else
        {
            text += "\n\nCan install main object";
        }

        return text;
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ С‚РµРєСЃС‚ РёРЅС„РѕСЂРјР°С†РёРё РѕР± РѕР±СЉРµРєС‚Рµ Р»РѕРєР°С†РёРё
    /// </summary>
    string GetLocationInfoText(LocationObjectInfo locationInfo)
    {
        string text = "";

        // РРјСЏ РѕР±СЉРµРєС‚Р°
        if (!string.IsNullOrEmpty(locationInfo.objectName))
        {
            text += $"{locationInfo.objectName}\n";
        }

        // РўРёРї РѕР±СЉРµРєС‚Р°
        if (!string.IsNullOrEmpty(locationInfo.objectType))
        {
            text += $"Type: {locationInfo.objectType}\n";
        }

        // Р—РґРѕСЂРѕРІСЊРµ
        text += $"Health: {locationInfo.health:F0} HP";

        // РњРµС‚Р°Р»Р» РґР»СЏ Р°СЃС‚РµСЂРѕРёРґРѕРІ
        if (locationInfo.IsOfType("Asteroid") && locationInfo.maxMetalAmount > 0)
        {
            text += $"\nMetal: {locationInfo.metalAmount}/{locationInfo.maxMetalAmount}";

            if (locationInfo.metalAmount > 0)
            {
                text += "\n<color=yellow>Right-click to mine</color>";
            }
            else
            {
                text += "\n<color=red>Depleted</color>";
            }
        }

        // Р”РѕРїРѕР»РЅРёС‚РµР»СЊРЅС‹Рµ СЃРІРѕР№СЃС‚РІР°
        if (locationInfo.isDestructible)
        {
            text += "\nDestructible";
        }

        if (locationInfo.canBeScavenged && !locationInfo.IsOfType("Asteroid"))
        {
            text += "\nSalvageable";
        }

        return text;
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ С‚РµРєСЃС‚ РёРЅС„РѕСЂРјР°С†РёРё Рѕ РїСЂРµРґРјРµС‚Рµ
    /// </summary>
    string GetItemInfoText(Item item)
    {
        string text = "";

        // Р—РђР©РРўРђ: РџСЂРѕРІРµСЂСЏРµРј С‡С‚Рѕ item РЅРµ Р±С‹Р» СѓРЅРёС‡С‚РѕР¶РµРЅ
        if (item == null || ReferenceEquals(item, null))
        {
            return "Item no longer exists";
        }

        // Р—РђР©РРўРђ: Р‘РµР·РѕРїР°СЃРЅРѕ РїРѕР»СѓС‡Р°РµРј itemData
        ItemData itemData = null;
        try
        {
            itemData = item.itemData;
        }
        catch (System.Exception ex)
        {
            return "Error reading item data";
        }

        if (itemData == null)
        {
            return "Item data not found";
        }

        // РРјСЏ РїСЂРµРґРјРµС‚Р°
        if (!string.IsNullOrEmpty(itemData.itemName))
        {
            text += $"ITEM: {itemData.itemName}\n";
        }

        // РўРёРї Рё СЂРµРґРєРѕСЃС‚СЊ
        text += $"Type: {itemData.itemType}\n";
        text += $"Rarity: {itemData.rarity}\n";

        // РћРїРёСЃР°РЅРёРµ
        if (!string.IsNullOrEmpty(itemData.description))
        {
            text += $"\n{itemData.description}\n";
        }

        // РҐР°СЂР°РєС‚РµСЂРёСЃС‚РёРєРё РѕСЂСѓР¶РёСЏ
        if (itemData.damage > 0)
            text += $"\nDamage: {itemData.damage}";

        // РҐР°СЂР°РєС‚РµСЂРёСЃС‚РёРєРё Р±СЂРѕРЅРё
        if (itemData.armor > 0)
            text += $"\nArmor: {itemData.armor}";

        // Р›РµС‡РµРЅРёРµ
        if (itemData.healing > 0)
            text += $"\nHealing: {itemData.healing}";

        // РЎР»РѕС‚ СЌРєРёРїРёСЂРѕРІРєРё
        if (itemData.equipmentSlot != EquipmentSlot.None)
            text += $"\nSlot: {itemData.GetEquipmentSlotName()}";

        // Р’РµСЃ Рё С†РµРЅРЅРѕСЃС‚СЊ
        text += $"\nWeight: {itemData.weight}";
        text += $"\nValue: {itemData.value}";

        // РЎС‚РµРє
        if (itemData.maxStackSize > 1)
            text += $"\nMax Stack: {itemData.maxStackSize}";

        return text;
    }

    /// <summary>
    /// РџРѕРєР°Р·Р°С‚СЊ РїР°РЅРµР»СЊ
    /// </summary>
    void ShowPanel()
    {
        if (objectSelectPanel != null)
        {
            objectSelectPanel.SetActive(true);
        }
    }

    /// <summary>
    /// РЎРєСЂС‹С‚СЊ РїР°РЅРµР»СЊ
    /// </summary>
    void HidePanel()
    {
        // РћС‡РёС‰Р°РµРј СЃСЃС‹Р»РєСѓ РЅР° РѕР±СЉРµРєС‚
        currentObject = null;

        // РЎРєСЂС‹РІР°РµРј РїР°РЅРµР»СЊ
        if (objectSelectPanel != null)
        {
            objectSelectPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Р РµРєСѓСЂСЃРёРІРЅС‹Р№ РїРѕРёСЃРє Transform РїРѕ РёРјРµРЅРё
    /// </summary>
    Transform FindTransformRecursive(Transform parent, string name)
    {
        if (parent.name == name)
            return parent;

        foreach (Transform child in parent)
        {
            Transform result = FindTransformRecursive(child, name);
            if (result != null)
                return result;
        }

        return null;
    }
}
