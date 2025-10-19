using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// РЎРёСЃС‚РµРјР° РїРѕРґР±РѕСЂР° РїСЂРµРґРјРµС‚РѕРІ РїРѕ РЅР°Р¶Р°С‚РёСЋ РєРЅРѕРїРєРё
/// РЈРЎРўРђР Р•Р›Рћ: РўРµРїРµСЂСЊ РїРѕРґР±РѕСЂ РїСЂРµРґРјРµС‚РѕРІ СЂР°Р±РѕС‚Р°РµС‚ С‡РµСЂРµР· РџРљРњ (SelectionManager.HandleRightClick)
/// РС‰РµС‚ Р±Р»РёР¶Р°Р№С€РёР№ РїСЂРµРґРјРµС‚ РІ СЂР°РґРёСѓСЃРµ Рё РїРѕРґР±РёСЂР°РµС‚ РµРіРѕ РїСЂРё РЅР°Р¶Р°С‚РёРё E
/// </summary>
public class ItemPickupSystem : MonoBehaviour
{
    [Header("Enable/Disable")]
    [Tooltip("Р’РђР–РќРћ: РЎРёСЃС‚РµРјР° РѕС‚РєР»СЋС‡РµРЅР° РїРѕ СѓРјРѕР»С‡Р°РЅРёСЋ. РСЃРїРѕР»СЊР·СѓР№С‚Рµ РџРљРњ РґР»СЏ РїРѕРґР±РѕСЂР° РїСЂРµРґРјРµС‚РѕРІ.")]
    public bool enablePickupByKey = false;

    [Header("Settings")]
    [Tooltip("Р Р°РґРёСѓСЃ РїРѕРёСЃРєР° РїСЂРµРґРјРµС‚РѕРІ РІРѕРєСЂСѓРі РїРµСЂСЃРѕРЅР°Р¶Р°")]
    public float pickupRadius = 3f;

    [Tooltip("РљРЅРѕРїРєР° РґР»СЏ РїРѕРґР±РѕСЂР° РїСЂРµРґРјРµС‚РѕРІ")]
    public KeyCode pickupKey = KeyCode.E;

    [Tooltip("РџРѕРєР°Р·С‹РІР°С‚СЊ РїРѕРґСЃРєР°Р·РєСѓ UI РЅР°Рґ Р±Р»РёР¶Р°Р№С€РёРј РїСЂРµРґРјРµС‚РѕРј")]
    public bool showPickupHint = true;

    [Header("Visual Settings")]
    [Tooltip("Р¦РІРµС‚ РїРѕРґСЃРІРµС‚РєРё Р±Р»РёР¶Р°Р№С€РµРіРѕ РїСЂРµРґРјРµС‚Р°")]
    public Color highlightColor = new Color(1f, 1f, 0f, 0.3f);

    [Tooltip("РџСЂРµС„Р°Р± UI РїРѕРґСЃРєР°Р·РєРё (РѕРїС†РёРѕРЅР°Р»СЊРЅРѕ)")]
    public GameObject pickupHintPrefab;

    // Р’РЅСѓС‚СЂРµРЅРЅРёРµ РїРµСЂРµРјРµРЅРЅС‹Рµ
    private Character character;
    private Item nearestItem;
    private Material originalMaterial;
    private Material highlightMaterial;
    private GameObject currentHintUI;

    void Start()
    {
        // Р•СЃР»Рё СЃРёСЃС‚РµРјР° РѕС‚РєР»СЋС‡РµРЅР° - РѕС‚РєР»СЋС‡Р°РµРј РІРµСЃСЊ РєРѕРјРїРѕРЅРµРЅС‚
        if (!enablePickupByKey)
        {

            enabled = false;
            return;
        }

        character = GetComponent<Character>();
        if (character == null)
        {
            enabled = false;
            return;
        }

        // РЎРѕР·РґР°РµРј РјР°С‚РµСЂРёР°Р» РґР»СЏ РїРѕРґСЃРІРµС‚РєРё
        highlightMaterial = new Material(Shader.Find("Standard"));
        highlightMaterial.color = highlightColor;
        highlightMaterial.SetFloat("_Mode", 3); // Transparent
        highlightMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        highlightMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        highlightMaterial.SetInt("_ZWrite", 0);
        highlightMaterial.DisableKeyword("_ALPHATEST_ON");
        highlightMaterial.EnableKeyword("_ALPHABLEND_ON");
        highlightMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        highlightMaterial.renderQueue = 3000;
    }

    void Update()
    {
        // РџСЂРѕРІРµСЂСЏРµРј, РІРєР»СЋС‡РµРЅР° Р»Рё СЃРёСЃС‚РµРјР°
        if (!enablePickupByKey)
        {
            // РЈР±РёСЂР°РµРј РїРѕРґСЃРІРµС‚РєСѓ РµСЃР»Рё СЃРёСЃС‚РµРјР° РѕС‚РєР»СЋС‡РµРЅР°
            if (nearestItem != null)
            {
                RemoveHighlight();
                nearestItem = null;
            }
            HidePickupHint();
            return;
        }

        // РС‰РµРј Р±Р»РёР¶Р°Р№С€РёР№ РїСЂРµРґРјРµС‚
        FindNearestItem();

        // РџРѕРґР±РёСЂР°РµРј РїСЂРё РЅР°Р¶Р°С‚РёРё РєР»Р°РІРёС€Рё
        if (Input.GetKeyDown(pickupKey) && nearestItem != null)
        {
            PickupNearestItem();
        }
    }

    /// <summary>
    /// РќР°Р№С‚Рё Р±Р»РёР¶Р°Р№С€РёР№ РїСЂРµРґРјРµС‚ РІ СЂР°РґРёСѓСЃРµ
    /// </summary>
    void FindNearestItem()
    {
        // Р—РђР©РРўРђ: РџСЂРѕРІРµСЂСЏРµРј С‡С‚Рѕ nearestItem РЅРµ Р±С‹Р» СѓРЅРёС‡С‚РѕР¶РµРЅ РїРµСЂРµРґ РѕР±СЂР°С‰РµРЅРёРµРј
        if (nearestItem != null && ReferenceEquals(nearestItem, null))
        {

            nearestItem = null;
            originalMaterial = null;
        }

        // РЈР±РёСЂР°РµРј РїРѕРґСЃРІРµС‚РєСѓ СЃ РїСЂРµРґС‹РґСѓС‰РµРіРѕ РїСЂРµРґРјРµС‚Р°
        if (nearestItem != null)
        {
            RemoveHighlight();
        }

        nearestItem = null;
        float nearestDistance = float.MaxValue;

        // РС‰РµРј РІСЃРµ РїСЂРµРґРјРµС‚С‹ РІ СЂР°РґРёСѓСЃРµ
        Collider[] colliders = Physics.OverlapSphere(transform.position, pickupRadius);

        foreach (Collider col in colliders)
        {
            if (col == null || ReferenceEquals(col, null))
                continue;

            Item item = null;
            try
            {
                item = col.GetComponent<Item>();
                // Р”РѕРїРѕР»РЅРёС‚РµР»СЊРЅР°СЏ РїСЂРѕРІРµСЂРєР° С‡С‚Рѕ Item РЅРµ СѓРЅРёС‡С‚РѕР¶РµРЅ
                if (item != null && ReferenceEquals(item, null))
                {
                    item = null;
                }
            }
            catch (System.Exception)
            {
                continue;
            }

            if (item != null && item.canBePickedUp)
            {
                float distance = Vector3.Distance(transform.position, item.transform.position);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestItem = item;
                }
            }
        }

        // РџРѕРґСЃРІРµС‡РёРІР°РµРј Р±Р»РёР¶Р°Р№С€РёР№ РїСЂРµРґРјРµС‚
        if (nearestItem != null && !ReferenceEquals(nearestItem, null))
        {
            HighlightItem(nearestItem);

            if (showPickupHint)
            {
                ShowPickupHint();
            }
        }
        else
        {
            HidePickupHint();
        }
    }

    /// <summary>
    /// РџРѕРґСЃРІРµС‚РёС‚СЊ РїСЂРµРґРјРµС‚
    /// </summary>
    void HighlightItem(Item item)
    {
        Renderer renderer = item.GetComponent<Renderer>();
        if (renderer != null)
        {
            // РЎРѕС…СЂР°РЅСЏРµРј РѕСЂРёРіРёРЅР°Р»СЊРЅС‹Р№ РјР°С‚РµСЂРёР°Р»
            if (originalMaterial == null)
            {
                originalMaterial = renderer.material;
            }

            // РњРµРЅСЏРµРј С†РІРµС‚ РґР»СЏ РїРѕРґСЃРІРµС‚РєРё (РЅРµ РјРµРЅСЏСЏ РјР°С‚РµСЂРёР°Р» РїРѕР»РЅРѕСЃС‚СЊСЋ)
            Color originalColor = renderer.material.color;
            Color highlightedColor = Color.Lerp(originalColor, highlightColor, 0.5f);
            renderer.material.color = highlightedColor;
        }
    }

    /// <summary>
    /// РЈР±СЂР°С‚СЊ РїРѕРґСЃРІРµС‚РєСѓ СЃ РїСЂРµРґРјРµС‚Р°
    /// </summary>
    void RemoveHighlight()
    {
        // РљР РРўРР§Р•РЎРљР Р’РђР–РќРћ: РџСЂРѕРІРµСЂСЏРµРј С‡С‚Рѕ Item РЅРµ Р±С‹Р» СѓРЅРёС‡С‚РѕР¶РµРЅ
        if (nearestItem != null && !ReferenceEquals(nearestItem, null))
        {
            try
            {
                Renderer renderer = nearestItem.GetComponent<Renderer>();
                if (renderer != null && originalMaterial != null)
                {
                    renderer.material = originalMaterial;
                    originalMaterial = null;
                }
            }
            catch (System.Exception ex)
            {
                // РћС‡РёС‰Р°РµРј СЃСЃС‹Р»РєРё С‡С‚РѕР±С‹ РёР·Р±РµР¶Р°С‚СЊ РїРѕРІС‚РѕСЂРЅС‹С… РѕС€РёР±РѕРє
                nearestItem = null;
                originalMaterial = null;
            }
        }
        else if (nearestItem != null && ReferenceEquals(nearestItem, null))
        {
            // Item Р±С‹Р» СѓРЅРёС‡С‚РѕР¶РµРЅ - РѕС‡РёС‰Р°РµРј СЃСЃС‹Р»РєСѓ

            nearestItem = null;
            originalMaterial = null;
        }
    }

    /// <summary>
    /// РџРѕРєР°Р·Р°С‚СЊ РїРѕРґСЃРєР°Р·РєСѓ UI
    /// </summary>
    void ShowPickupHint()
    {
        // Р—РђР©РРўРђ: РџСЂРѕРІРµСЂСЏРµРј С‡С‚Рѕ nearestItem РЅРµ Р±С‹Р» СѓРЅРёС‡С‚РѕР¶РµРЅ
        if (nearestItem != null && ReferenceEquals(nearestItem, null))
        {

            HidePickupHint();
            nearestItem = null;
            return;
        }

        if (currentHintUI == null && pickupHintPrefab != null)
        {
            currentHintUI = Instantiate(pickupHintPrefab);
        }

        // РџРѕР·РёС†РёРѕРЅРёСЂСѓРµРј РїРѕРґСЃРєР°Р·РєСѓ РЅР°Рґ РїСЂРµРґРјРµС‚РѕРј
        if (currentHintUI != null && nearestItem != null && !ReferenceEquals(nearestItem, null))
        {
            try
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(nearestItem.transform.position + Vector3.up * 0.5f);
                currentHintUI.transform.position = screenPos;
            }
            catch (System.Exception ex)
            {
                HidePickupHint();
                nearestItem = null;
            }
        }
    }

    /// <summary>
    /// РЎРєСЂС‹С‚СЊ РїРѕРґСЃРєР°Р·РєСѓ UI
    /// </summary>
    void HidePickupHint()
    {
        if (currentHintUI != null)
        {
            Destroy(currentHintUI);
            currentHintUI = null;
        }
    }

    /// <summary>
    /// РџРѕРґРѕР±СЂР°С‚СЊ Р±Р»РёР¶Р°Р№С€РёР№ РїСЂРµРґРјРµС‚
    /// </summary>
    void PickupNearestItem()
    {
        if (nearestItem == null || character == null)
            return;



        // РџСЂРѕРІРµСЂСЏРµРј, РјРѕР¶РµС‚ Р»Рё РїРµСЂСЃРѕРЅР°Р¶ РїРѕРґРЅСЏС‚СЊ РїСЂРµРґРјРµС‚
        if (nearestItem.CanBePickedUpBy(character))
        {
            // РћСЃРІРѕР±РѕР¶РґР°РµРј РєР»РµС‚РєСѓ РІ GridManager РµСЃР»Рё РїСЂРµРґРјРµС‚ Р·Р°РЅРёРјР°Р» РµС‘
            GridManager gridManager = FindObjectOfType<GridManager>();
            if (gridManager != null)
            {
                Vector2Int gridPos = gridManager.WorldToGrid(nearestItem.transform.position);
                gridManager.FreeCell(gridPos);
            }

            // РџРѕРґР±РёСЂР°РµРј РїСЂРµРґРјРµС‚
            nearestItem.PickUp(character);



            // РЈР±РёСЂР°РµРј РїРѕРґСЃРєР°Р·РєСѓ
            HidePickupHint();

            nearestItem = null;
            originalMaterial = null;
        }
        else
        {
        }
    }

    void OnDisable()
    {
        // РЈР±РёСЂР°РµРј РїРѕРґСЃРІРµС‚РєСѓ РїСЂРё РѕС‚РєР»СЋС‡РµРЅРёРё
        RemoveHighlight();
        HidePickupHint();
    }

    void OnDrawGizmosSelected()
    {
        // РџРѕРєР°Р·С‹РІР°РµРј СЂР°РґРёСѓСЃ РїРѕРёСЃРєР° РїСЂРµРґРјРµС‚РѕРІ
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);

        // РџРѕРєР°Р·С‹РІР°РµРј Р»РёРЅРёСЋ Рє Р±Р»РёР¶Р°Р№С€РµРјСѓ РїСЂРµРґРјРµС‚Сѓ
        if (nearestItem != null && Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, nearestItem.transform.position);
        }
    }
}
