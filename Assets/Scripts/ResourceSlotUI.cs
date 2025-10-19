using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI РєРѕРјРїРѕРЅРµРЅС‚ РґР»СЏ РѕС‚РѕР±СЂР°Р¶РµРЅРёСЏ РѕРґРЅРѕРіРѕ СЂРµСЃСѓСЂСЃР°
/// РџРѕРєР°Р·С‹РІР°РµС‚ РёРєРѕРЅРєСѓ СЂРµСЃСѓСЂСЃР° Рё РµРіРѕ РєРѕР»РёС‡РµСЃС‚РІРѕ
/// </summary>
public class ResourceSlotUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("РљРѕРјРїРѕРЅРµРЅС‚ Image РґР»СЏ РѕС‚РѕР±СЂР°Р¶РµРЅРёСЏ РёРєРѕРЅРєРё СЂРµСЃСѓСЂСЃР°")]
    public Image iconImage;

    [Tooltip("РўРµРєСЃС‚ РґР»СЏ РѕС‚РѕР±СЂР°Р¶РµРЅРёСЏ РєРѕР»РёС‡РµСЃС‚РІР° (TextMeshPro)")]
    public TextMeshProUGUI quantityText;

    [Tooltip("Р¤РѕРЅРѕРІРѕРµ РёР·РѕР±СЂР°Р¶РµРЅРёРµ СЃР»РѕС‚Р°")]
    public Image backgroundImage;

    [Header("Settings")]
    [Tooltip("Р Р°Р·РјРµСЂ РёРєРѕРЅРєРё (РёСЃРїРѕР»СЊР·СѓРµС‚СЃСЏ С‚РѕР»СЊРєРѕ РґР»СЏ РїСЂРѕРіСЂР°РјРјРЅРѕРіРѕ СЃРѕР·РґР°РЅРёСЏ)")]
    public Vector2 iconSize = new Vector2(50, 50);

    // Р”Р°РЅРЅС‹Рµ СЂРµСЃСѓСЂСЃР°
    private ResourceData resourceData;
    private int currentQuantity;

    void Awake()
    {
        // РђРІС‚РѕРјР°С‚РёС‡РµСЃРєРё РЅР°С…РѕРґРёРј РєРѕРјРїРѕРЅРµРЅС‚С‹ РёР· РїСЂРµС„Р°Р±Р° Res_Slot
        // РџСЂРµС„Р°Р± РёРјРµРµС‚ РґРµС‚РµР№: "Ico", "Text (TMP)", "Backgound"

        if (iconImage == null)
        {
            // РС‰РµРј child СЃ РёРјРµРЅРµРј "Ico"
            Transform iconTransform = transform.Find("Ico");
            if (iconTransform != null)
            {
                iconImage = iconTransform.GetComponent<Image>();

            }
            else
            {
            }
        }

        if (quantityText == null)
        {
            // РС‰РµРј child СЃ РёРјРµРЅРµРј "Text (TMP)"
            Transform textTransform = transform.Find("Text (TMP)");
            if (textTransform != null)
            {
                quantityText = textTransform.GetComponent<TextMeshProUGUI>();

            }
            else
            {
            }
        }

        if (backgroundImage == null)
        {
            // РС‰РµРј child СЃ РёРјРµРЅРµРј "Backgound"
            Transform backgroundTransform = transform.Find("Backgound");
            if (backgroundTransform != null)
            {
                backgroundImage = backgroundTransform.GetComponent<Image>();

            }
            else
            {
                // Р¤РѕР»Р±СЌРє: РїС‹С‚Р°РµРјСЃСЏ РЅР°Р№С‚Рё Image РЅР° РєРѕСЂРЅРµРІРѕРј РѕР±СЉРµРєС‚Рµ
                backgroundImage = GetComponent<Image>();
                if (backgroundImage != null)
                {

                }
            }
        }
    }

    /// <summary>
    /// РРЅРёС†РёР°Р»РёР·РёСЂРѕРІР°С‚СЊ СЃР»РѕС‚ СЂРµСЃСѓСЂСЃР°
    /// </summary>
    public void Initialize(ResourceData resource)
    {
        resourceData = resource;

        if (resourceData == null)
        {
            return;
        }

        // Р¤РћР›Р‘Р­Рљ: РЎРѕР·РґР°РµРј UI СЌР»РµРјРµРЅС‚С‹ РїСЂРѕРіСЂР°РјРјРЅРѕ, С‚РѕР»СЊРєРѕ РµСЃР»Рё РїСЂРµС„Р°Р± РёС… РЅРµ СЃРѕРґРµСЂР¶РёС‚
        if (iconImage == null || quantityText == null)
        {
            CreateUIElements();
        }

        // РЈСЃС‚Р°РЅР°РІР»РёРІР°РµРј РёРєРѕРЅРєСѓ (РЅРµ РјРµРЅСЏРµРј С†РІРµС‚ - РёСЃРїРѕР»СЊР·СѓРµРј РЅР°СЃС‚СЂРѕР№РєРё РёР· РїСЂРµС„Р°Р±Р°)
        if (iconImage != null && resourceData.icon != null)
        {
            iconImage.sprite = resourceData.icon;
        }

        // РќР• РјРµРЅСЏРµРј С†РІРµС‚ С„РѕРЅР° - РёСЃРїРѕР»СЊР·СѓРµРј РЅР°СЃС‚СЂРѕР№РєРё РёР· РїСЂРµС„Р°Р±Р°

        // РРЅРёС†РёР°Р»РёР·РёСЂСѓРµРј РєРѕР»РёС‡РµСЃС‚РІРѕ
        UpdateQuantity(0);

        // Р”РѕР±Р°РІР»СЏРµРј Tooltip (РѕРїС†РёРѕРЅР°Р»СЊРЅРѕ)
        AddTooltip();
    }

    /// <summary>
    /// РЎРѕР·РґР°С‚СЊ UI СЌР»РµРјРµРЅС‚С‹ РїСЂРѕРіСЂР°РјРјРЅРѕ
    /// </summary>
    void CreateUIElements()
    {
        // РќР°СЃС‚СЂР°РёРІР°РµРј РєРѕСЂРЅРµРІРѕР№ РѕР±СЉРµРєС‚
        RectTransform rootRect = GetComponent<RectTransform>();
        if (rootRect == null)
        {
            rootRect = gameObject.AddComponent<RectTransform>();
        }
        rootRect.sizeDelta = iconSize;

        // Р”РѕР±Р°РІР»СЏРµРј С„РѕРЅРѕРІРѕРµ РёР·РѕР±СЂР°Р¶РµРЅРёРµ
        if (backgroundImage == null)
        {
            backgroundImage = gameObject.AddComponent<Image>();
            // РСЃРїРѕР»СЊР·СѓРµРј РЅРµР№С‚СЂР°Р»СЊРЅС‹Р№ РїРѕР»СѓРїСЂРѕР·СЂР°С‡РЅС‹Р№ СЃРµСЂС‹Р№ С†РІРµС‚ РїРѕ СѓРјРѕР»С‡Р°РЅРёСЋ
            backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        }

        // РЎРѕР·РґР°РµРј РёРєРѕРЅРєСѓ
        if (iconImage == null)
        {
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(transform, false);

            iconImage = iconObj.AddComponent<Image>();
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.1f, 0.3f);
            iconRect.anchorMax = new Vector2(0.9f, 0.9f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
        }

        // РЎРѕР·РґР°РµРј С‚РµРєСЃС‚ РєРѕР»РёС‡РµСЃС‚РІР° (TextMeshPro)
        if (quantityText == null)
        {
            GameObject textObj = new GameObject("QuantityText");
            textObj.transform.SetParent(transform, false);

            quantityText = textObj.AddComponent<TextMeshProUGUI>();
            quantityText.fontSize = 14;
            quantityText.fontStyle = FontStyles.Bold;
            quantityText.color = Color.white;
            quantityText.alignment = TextAlignmentOptions.BottomRight;

            // Р’РєР»СЋС‡Р°РµРј Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРёР№ СЂР°Р·РјРµСЂ
            quantityText.enableAutoSizing = false;

            // Р’РєР»СЋС‡Р°РµРј Outline РґР»СЏ Р»СѓС‡С€РµР№ С‡РёС‚Р°РµРјРѕСЃС‚Рё
            quantityText.outlineWidth = 0.2f;
            quantityText.outlineColor = Color.black;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 0.3f);
            textRect.offsetMin = new Vector2(2, 2);
            textRect.offsetMax = new Vector2(-2, -2);
        }
    }

    /// <summary>
    /// РћР±РЅРѕРІРёС‚СЊ РєРѕР»РёС‡РµСЃС‚РІРѕ СЂРµСЃСѓСЂСЃР°
    /// </summary>
    public void UpdateQuantity(int quantity)
    {
        currentQuantity = quantity;

        if (quantityText != null)
        {
            // РџСЂРѕСЃС‚Рѕ РѕР±РЅРѕРІР»СЏРµРј С‚РµРєСЃС‚, РЅРµ РјРµРЅСЏСЏ СЃС‚РёР»СЊ РёР· РїСЂРµС„Р°Р±Р°
            // Р¤РѕСЂРјР°С‚РёСЂСѓРµРј РєРѕР»РёС‡РµСЃС‚РІРѕ РґР»СЏ РєРѕРјРїР°РєС‚РЅРѕРіРѕ РѕС‚РѕР±СЂР°Р¶РµРЅРёСЏ
            if (quantity >= 1000000)
            {
                quantityText.text = $"{quantity / 1000000f:F1}M";
            }
            else if (quantity >= 1000)
            {
                quantityText.text = $"{quantity / 1000f:F1}K";
            }
            else
            {
                quantityText.text = quantity.ToString();
            }

            // РќР• РјРµРЅСЏРµРј С†РІРµС‚, С€СЂРёС„С‚ Рё РґСЂСѓРіРёРµ РїР°СЂР°РјРµС‚СЂС‹ - РёСЃРїРѕР»СЊР·СѓРµРј РЅР°СЃС‚СЂРѕР№РєРё РёР· РїСЂРµС„Р°Р±Р°
        }
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ С‚РµРєСѓС‰РµРµ РєРѕР»РёС‡РµСЃС‚РІРѕ
    /// </summary>
    public int GetQuantity()
    {
        return currentQuantity;
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РґР°РЅРЅС‹Рµ СЂРµСЃСѓСЂСЃР°
    /// </summary>
    public ResourceData GetResourceData()
    {
        return resourceData;
    }

    /// <summary>
    /// Р”РѕР±Р°РІРёС‚СЊ Tooltip РґР»СЏ РѕС‚РѕР±СЂР°Р¶РµРЅРёСЏ РёРЅС„РѕСЂРјР°С†РёРё Рѕ СЂРµСЃСѓСЂСЃРµ
    /// </summary>
    void AddTooltip()
    {
        if (resourceData == null)
            return;

        // РџС‹С‚Р°РµРјСЃСЏ РЅР°Р№С‚Рё СЃРёСЃС‚РµРјСѓ Tooltip
        TooltipSystem tooltipSystem = FindObjectOfType<TooltipSystem>();
        if (tooltipSystem != null)
        {
            // РњРѕР¶РЅРѕ РґРѕР±Р°РІРёС‚СЊ РїРѕРґРґРµСЂР¶РєСѓ Tooltip РІ Р±СѓРґСѓС‰РµРј
            // РџРѕРєР° РѕСЃС‚Р°РІР»СЏРµРј Р·Р°РіР»СѓС€РєСѓ
        }
    }

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚С‡РёРє РЅР°РІРµРґРµРЅРёСЏ РјС‹С€Рё (РґР»СЏ Р±СѓРґСѓС‰РµРіРѕ Tooltip)
    /// </summary>
    public void OnPointerEnter()
    {
        // РќР• РјРµРЅСЏРµРј РІРЅРµС€РЅРёР№ РІРёРґ - РёСЃРїРѕР»СЊР·СѓРµРј РЅР°СЃС‚СЂРѕР№РєРё РёР· РїСЂРµС„Р°Р±Р°
        // РњРѕР¶РЅРѕ РґРѕР±Р°РІРёС‚СЊ РїРѕРєР°Р· Tooltip РІ Р±СѓРґСѓС‰РµРј
    }

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚С‡РёРє СѓС…РѕРґР° РјС‹С€Рё
    /// </summary>
    public void OnPointerExit()
    {
        // РќР• РјРµРЅСЏРµРј РІРЅРµС€РЅРёР№ РІРёРґ - РёСЃРїРѕР»СЊР·СѓРµРј РЅР°СЃС‚СЂРѕР№РєРё РёР· РїСЂРµС„Р°Р±Р°
    }
}
