using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// UI РїР°РЅРµР»СЊ РґР»СЏ РѕС‚РѕР±СЂР°Р¶РµРЅРёСЏ СЂРµСЃСѓСЂСЃРѕРІ РєРѕСЂР°Р±Р»СЏ
/// РџРѕРєР°Р·С‹РІР°РµС‚ СЂРµСЃСѓСЂСЃС‹ РёР· РёРЅРІРµРЅС‚Р°СЂРµР№ РїРµСЂСЃРѕРЅР°Р¶РµР№ Рё Р»РµР¶Р°С‰РёРµ РЅР° С‚РµСЂСЂРёС‚РѕСЂРёРё РєРѕСЂР°Р±Р»СЏ
/// </summary>
public class ResourcePanelUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("ResourceManager СЃРѕ СЃРїРёСЃРєРѕРј РІСЃРµС… СЂРµСЃСѓСЂСЃРѕРІ")]
    public ResourceManager resourceManager;

    [Tooltip("РџСЂРµС„Р°Р± РґР»СЏ РѕС‚РѕР±СЂР°Р¶РµРЅРёСЏ РѕРґРЅРѕРіРѕ СЂРµСЃСѓСЂСЃР°")]
    public GameObject resourceSlotPrefab;

    [Tooltip("Р РѕРґРёС‚РµР»СЊСЃРєРёР№ РѕР±СЉРµРєС‚ РґР»СЏ РёРєРѕРЅРѕРє СЂРµСЃСѓСЂСЃРѕРІ")]
    public Transform resourceSlotsParent;

    [Header("Settings")]
    [Tooltip("РљР°Рє С‡Р°СЃС‚Рѕ РѕР±РЅРѕРІР»СЏС‚СЊ РїР°РЅРµР»СЊ (РІ СЃРµРєСѓРЅРґР°С…)")]
    public float updateInterval = 2f; // РЈРІРµР»РёС‡РёР»Рё РёРЅС‚РµСЂРІР°Р» РґРѕ 2 СЃРµРєСѓРЅРґ

    // Р’РЅСѓС‚СЂРµРЅРЅРёРµ РїРµСЂРµРјРµРЅРЅС‹Рµ
    private Dictionary<string, ResourceSlotUI> resourceSlots = new Dictionary<string, ResourceSlotUI>();
    private float updateTimer;
    private GridManager gridManager;

    // PERFORMANCE FIX: РљСЌС€РёСЂСѓРµРј РјР°СЃСЃРёРІС‹ Рё РѕР±РЅРѕРІР»СЏРµРј СЂРµР¶Рµ + РёСЃРїРѕР»СЊР·СѓРµРј СЃРѕР±С‹С‚РёСЏ
    private Character[] cachedCharacters;
    private Item[] cachedItems;
    private float cacheRefreshTimer;
    private float cacheRefreshInterval = 30f; // РЈРІРµР»РёС‡РёР»Рё СЃ 5 РґРѕ 30 СЃРµРєСѓРЅРґ РґР»СЏ РїСЂРѕРёР·РІРѕРґРёС‚РµР»СЊРЅРѕСЃС‚Рё
    private bool needsRefresh = false; // Р¤Р»Р°Рі РґР»СЏ РїСЂРёРЅСѓРґРёС‚РµР»СЊРЅРѕРіРѕ РѕР±РЅРѕРІР»РµРЅРёСЏ

    // Р—Р°С‰РёС‚Р° РѕС‚ РїРѕРІС‚РѕСЂРЅРѕР№ РёРЅРёС†РёР°Р»РёР·Р°С†РёРё
    private bool isInitialized = false;

    void Awake()
    {
        // Р—РђР©РРўРђ: РџСЂРѕРІРµСЂСЏРµРј, РЅРµ СЏРІР»СЏРµС‚СЃСЏ Р»Рё СЌС‚РѕС‚ РѕР±СЉРµРєС‚ РєР»РѕРЅРѕРј СЃР»РѕС‚Р° (СЂРµРєСѓСЂСЃРёСЏ!)
        if (gameObject.name.Contains("ResourceSlot") || (gameObject.name.Contains("(Clone)") && transform.parent != null && transform.parent.name.Contains("ResourceSlot")))
        {
            DestroyImmediate(this);
            return;
        }

        // РќР°С…РѕРґРёРј ResourceManager РµСЃР»Рё РЅРµ РЅР°Р·РЅР°С‡РµРЅ
        if (resourceManager == null)
        {
            resourceManager = Resources.Load<ResourceManager>("ResourceManager");
            if (resourceManager == null)
            {
            }
        }

        // РќР°С…РѕРґРёРј GridManager
        gridManager = FindObjectOfType<GridManager>();
        if (gridManager == null)
        {
        }

        // Р•СЃР»Рё resourceSlotsParent РЅРµ РЅР°Р·РЅР°С‡РµРЅ, РёС‰РµРј РµРіРѕ
        if (resourceSlotsParent == null)
        {
            resourceSlotsParent = transform;
        }
    }

    void Start()
    {
        // Р—Р°С‰РёС‚Р° РѕС‚ РїРѕРІС‚РѕСЂРЅРѕР№ РёРЅРёС†РёР°Р»РёР·Р°С†РёРё
        if (isInitialized)
        {
            return;
        }

        // РџСЂРѕР±СѓРµРј РЅР°Р№С‚Рё GridManager РµС‰Рµ СЂР°Р· РµСЃР»Рё РЅРµ РЅР°С€Р»Рё РІ Awake
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
            if (gridManager == null)
            {
            }
        }

        // PERFORMANCE FIX: РџРѕРґРїРёСЃС‹РІР°РµРјСЃСЏ РЅР° СЃРѕР±С‹С‚РёСЏ РґР»СЏ event-driven РѕР±РЅРѕРІР»РµРЅРёСЏ
        EventBus.Subscribe<CharacterSpawnedEvent>(OnCharacterSpawned);

        // РќР• СЃРѕР·РґР°РµРј СЃР»РѕС‚С‹ Р·Р°СЂР°РЅРµРµ - РѕРЅРё СЃРѕР·РґР°СЋС‚СЃСЏ РґРёРЅР°РјРёС‡РµСЃРєРё РїСЂРё РЅР°Р»РёС‡РёРё СЂРµСЃСѓСЂСЃРѕРІ
        RefreshCache();
        UpdateResourceDisplay();
        isInitialized = true;
    }

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚С‡РёРє СЃРїР°РІРЅР° РїРµСЂСЃРѕРЅР°Р¶Р° - РѕР±РЅРѕРІР»СЏРµРј РєСЌС€
    /// PERFORMANCE: Р’С‹Р·С‹РІР°РµС‚СЃСЏ С‚РѕР»СЊРєРѕ РєРѕРіРґР° СЃРїР°РІРЅРёС‚СЃСЏ РїРµСЂСЃРѕРЅР°Р¶, Р° РЅРµ РєР°Р¶РґС‹Рµ 5 СЃРµРєСѓРЅРґ
    /// </summary>
    void OnCharacterSpawned(CharacterSpawnedEvent evt)
    {
        needsRefresh = true;
    }

    void Update()
    {
        // PERFORMANCE FIX: РћР±РЅРѕРІР»СЏРµРј РєСЌС€ СЃСЂР°Р·Сѓ РµСЃР»Рё РµСЃС‚СЊ С„Р»Р°Рі, РёРЅР°С‡Рµ РїРѕ С‚Р°Р№РјРµСЂСѓ
        if (needsRefresh)
        {
            needsRefresh = false;
            RefreshCache();
            UpdateResourceDisplay();
            updateTimer = 0f; // РЎР±СЂР°СЃС‹РІР°РµРј С‚Р°Р№РјРµСЂ РѕР±РЅРѕРІР»РµРЅРёСЏ
            cacheRefreshTimer = 0f; // РЎР±СЂР°СЃС‹РІР°РµРј С‚Р°Р№РјРµСЂ РєСЌС€Р°
            return;
        }

        // РћР±РЅРѕРІР»СЏРµРј РїР°РЅРµР»СЊ СЃ Р·Р°РґР°РЅРЅС‹Рј РёРЅС‚РµСЂРІР°Р»РѕРј
        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            UpdateResourceDisplay();
        }

        // РћР±РЅРѕРІР»СЏРµРј РєСЌС€ РїРµСЂРёРѕРґРёС‡РµСЃРєРё (С‚РµРїРµСЂСЊ СЂР°Р· РІ 30 СЃРµРєСѓРЅРґ РІРјРµСЃС‚Рѕ 5)
        cacheRefreshTimer += Time.deltaTime;
        if (cacheRefreshTimer >= cacheRefreshInterval)
        {
            cacheRefreshTimer = 0f;
            RefreshCache();
        }
    }

    /// <summary>
    /// РћР±РЅРѕРІРёС‚СЊ РєСЌС€ РїРµСЂСЃРѕРЅР°Р¶РµР№ Рё РїСЂРµРґРјРµС‚РѕРІ
    /// </summary>
    void RefreshCache()
    {
        cachedCharacters = FindObjectsOfType<Character>();
        cachedItems = FindObjectsOfType<Item>();
    }

    /// <summary>
    /// РРЅРёС†РёР°Р»РёР·РёСЂРѕРІР°С‚СЊ СЃР»РѕС‚С‹ РґР»СЏ РІСЃРµС… СЂРµСЃСѓСЂСЃРѕРІ
    /// </summary>
    void InitializeResourceSlots()
    {
        if (resourceManager == null || resourceSlotPrefab == null)
        {
            return;
        }

        // Р—РђР©РРўРђ: РџСЂРѕРІРµСЂСЏРµРј, С‡С‚Рѕ РїСЂРµС„Р°Р± РЅРµ СЃРѕРґРµСЂР¶РёС‚ ResourcePanelUI РєРѕРјРїРѕРЅРµРЅС‚
        if (resourceSlotPrefab.GetComponent<ResourcePanelUI>() != null)
        {
            return;
        }

        // Р—РђР©РРўРђ: РџСЂРѕРІРµСЂСЏРµРј, РЅРµ СЏРІР»СЏРµС‚СЃСЏ Р»Рё СЌС‚РѕС‚ РѕР±СЉРµРєС‚ РєР»РѕРЅРѕРј СЃР»РѕС‚Р° (СЂРµРєСѓСЂСЃРёСЏ!)
        if (gameObject.name.Contains("ResourceSlot") || gameObject.name.Contains("(Clone)"))
        {
            return;
        }

        // РћС‡РёС‰Р°РµРј СЃСѓС‰РµСЃС‚РІСѓСЋС‰РёРµ СЃР»РѕС‚С‹
        ClearResourceSlots();

        // РЎРѕР·РґР°РµРј СЃР»РѕС‚ РґР»СЏ РєР°Р¶РґРѕРіРѕ СЂРµСЃСѓСЂСЃР°
        foreach (ResourceData resource in resourceManager.allResources)
        {
            CreateResourceSlot(resource);
        }
    }

    /// <summary>
    /// РЎРѕР·РґР°С‚СЊ СЃР»РѕС‚ РґР»СЏ РѕРґРЅРѕРіРѕ СЂРµСЃСѓСЂСЃР°
    /// </summary>
    void CreateResourceSlot(ResourceData resource)
    {
        if (resource == null || resourceSlotPrefab == null || resourceSlotsParent == null)
            return;

        // РЎРѕР·РґР°РµРј СЌРєР·РµРјРїР»СЏСЂ РїСЂРµС„Р°Р±Р°
        GameObject slotObj = Instantiate(resourceSlotPrefab, resourceSlotsParent);
        slotObj.name = $"ResourceSlot_{resource.resourceName}";
        slotObj.SetActive(true); // Р’СЃРµРіРґР° Р°РєС‚РёРІРЅС‹Р№

        // РџРѕР»СѓС‡Р°РµРј РєРѕРјРїРѕРЅРµРЅС‚ ResourceSlotUI
        ResourceSlotUI slotUI = slotObj.GetComponent<ResourceSlotUI>();
        if (slotUI == null)
        {
            slotUI = slotObj.AddComponent<ResourceSlotUI>();
        }

        // РРЅРёС†РёР°Р»РёР·РёСЂСѓРµРј СЃР»РѕС‚ СЃ РґР°РЅРЅС‹РјРё СЂРµСЃСѓСЂСЃР°
        slotUI.Initialize(resource);

        // РЎРѕС…СЂР°РЅСЏРµРј РІ СЃР»РѕРІР°СЂСЊ
        resourceSlots[resource.resourceName] = slotUI;
    }

    /// <summary>
    /// РЈРґР°Р»РёС‚СЊ СЃР»РѕС‚ СЂРµСЃСѓСЂСЃР°
    /// </summary>
    void RemoveResourceSlot(string resourceName)
    {
        if (resourceSlots.ContainsKey(resourceName))
        {
            ResourceSlotUI slotUI = resourceSlots[resourceName];
            if (slotUI != null && slotUI.gameObject != null)
            {
                Destroy(slotUI.gameObject);
            }

            resourceSlots.Remove(resourceName);
        }
    }

    /// <summary>
    /// РћР±РЅРѕРІРёС‚СЊ РѕС‚РѕР±СЂР°Р¶РµРЅРёРµ РІСЃРµС… СЂРµСЃСѓСЂСЃРѕРІ (РґРёРЅР°РјРёС‡РµСЃРєРѕРµ СЃРѕР·РґР°РЅРёРµ/СѓРґР°Р»РµРЅРёРµ СЃР»РѕС‚РѕРІ)
    /// </summary>
    public void UpdateResourceDisplay()
    {
        if (resourceManager == null)
            return;

        // РЎРѕР±РёСЂР°РµРј РёРЅС„РѕСЂРјР°С†РёСЋ Рѕ РґРѕСЃС‚СѓРїРЅС‹С… СЂРµСЃСѓСЂСЃР°С…
        Dictionary<string, int> availableResources = CollectAvailableResources();

        // РЎРїРёСЃРѕРє СЂРµСЃСѓСЂСЃРѕРІ РґР»СЏ СѓРґР°Р»РµРЅРёСЏ (Р·Р°РєРѕРЅС‡РёР»РёСЃСЊ)
        List<string> resourcesToRemove = new List<string>();

        // 1. РћР±РЅРѕРІР»СЏРµРј СЃСѓС‰РµСЃС‚РІСѓСЋС‰РёРµ СЃР»РѕС‚С‹
        foreach (var kvp in resourceSlots)
        {
            string resourceName = kvp.Key;
            ResourceSlotUI slotUI = kvp.Value;

            if (availableResources.ContainsKey(resourceName))
            {
                // Р РµСЃСѓСЂСЃ РµСЃС‚СЊ - РѕР±РЅРѕРІР»СЏРµРј РєРѕР»РёС‡РµСЃС‚РІРѕ
                int quantity = availableResources[resourceName];
                if (slotUI != null)
                {
                    slotUI.UpdateQuantity(quantity);
                }
            }
            else
            {
                // Р РµСЃСѓСЂСЃР° Р±РѕР»СЊС€Рµ РЅРµС‚ - РїРѕРјРµС‡Р°РµРј РЅР° СѓРґР°Р»РµРЅРёРµ
                resourcesToRemove.Add(resourceName);
            }
        }

        // 2. РЈРґР°Р»СЏРµРј СЃР»РѕС‚С‹ СЂРµСЃСѓСЂСЃРѕРІ, РєРѕС‚РѕСЂС‹С… Р±РѕР»СЊС€Рµ РЅРµС‚
        foreach (string resourceName in resourcesToRemove)
        {
            RemoveResourceSlot(resourceName);
        }

        // 3. РЎРѕР·РґР°РµРј СЃР»РѕС‚С‹ РґР»СЏ РЅРѕРІС‹С… СЂРµСЃСѓСЂСЃРѕРІ
        foreach (var kvp in availableResources)
        {
            string resourceName = kvp.Key;
            int quantity = kvp.Value;

            // Р•СЃР»Рё РєРѕР»РёС‡РµСЃС‚РІРѕ > 0 Рё СЃР»РѕС‚Р° РµС‰Рµ РЅРµС‚ - СЃРѕР·РґР°РµРј
            if (quantity > 0 && !resourceSlots.ContainsKey(resourceName))
            {
                ResourceData resourceData = resourceManager.GetResourceByName(resourceName);
                if (resourceData != null)
                {
                    CreateResourceSlot(resourceData);

                    // РЎСЂР°Р·Сѓ РѕР±РЅРѕРІР»СЏРµРј РєРѕР»РёС‡РµСЃС‚РІРѕ
                    if (resourceSlots.ContainsKey(resourceName))
                    {
                        resourceSlots[resourceName].UpdateQuantity(quantity);
                    }
                }
            }
        }
    }

    /// <summary>
    /// РЎРѕР±СЂР°С‚СЊ РёРЅС„РѕСЂРјР°С†РёСЋ Рѕ РІСЃРµС… РґРѕСЃС‚СѓРїРЅС‹С… СЂРµСЃСѓСЂСЃР°С…
    /// </summary>
    Dictionary<string, int> CollectAvailableResources()
    {
        Dictionary<string, int> resources = new Dictionary<string, int>();

        // 1. РЎРѕР±РёСЂР°РµРј СЂРµСЃСѓСЂСЃС‹ РёР· РёРЅРІРµРЅС‚Р°СЂРµР№ РїРµСЂСЃРѕРЅР°Р¶РµР№ РёРіСЂРѕРєР°
        // РСЃРїРѕР»СЊР·СѓРµРј РєСЌС€РёСЂРѕРІР°РЅРЅС‹Р№ РјР°СЃСЃРёРІ РІРјРµСЃС‚Рѕ FindObjectsOfType
        if (cachedCharacters == null || cachedCharacters.Length == 0)
        {
            RefreshCache();
        }

        foreach (Character character in cachedCharacters)
        {
            if (character.IsPlayerCharacter())
            {
                Inventory inventory = character.GetComponent<Inventory>();
                if (inventory != null)
                {
                    CollectResourcesFromInventory(inventory, resources);
                }
            }
        }

        // 2. РЎРѕР±РёСЂР°РµРј СЂРµСЃСѓСЂСЃС‹, Р»РµР¶Р°С‰РёРµ РЅР° С‚РµСЂСЂРёС‚РѕСЂРёРё РєРѕСЂР°Р±Р»СЏ
        if (gridManager != null)
        {
            CollectResourcesFromShipTerritory(resources);
        }

        return resources;
    }

    /// <summary>
    /// РЎРѕР±СЂР°С‚СЊ СЂРµСЃСѓСЂСЃС‹ РёР· РёРЅРІРµРЅС‚Р°СЂСЏ
    /// </summary>
    void CollectResourcesFromInventory(Inventory inventory, Dictionary<string, int> resources)
    {
        List<InventorySlot> slots = inventory.GetAllSlots();

        foreach (InventorySlot slot in slots)
        {
            if (!slot.IsEmpty() && slot.itemData.itemType == ItemType.Resource)
            {
                string resourceName = slot.itemData.itemName;
                int quantity = slot.quantity;

                if (resources.ContainsKey(resourceName))
                {
                    resources[resourceName] += quantity;
                }
                else
                {
                    resources[resourceName] = quantity;
                }
            }
        }
    }

    /// <summary>
    /// РЎРѕР±СЂР°С‚СЊ СЂРµСЃСѓСЂСЃС‹, Р»РµР¶Р°С‰РёРµ РЅР° С‚РµСЂСЂРёС‚РѕСЂРёРё РєРѕСЂР°Р±Р»СЏ
    /// </summary>
    void CollectResourcesFromShipTerritory(Dictionary<string, int> resources)
    {
        // РџРѕР»СѓС‡Р°РµРј РІСЃРµ РєР»РµС‚РєРё, Р·Р°РЅСЏС‚С‹Рµ РєРѕРјРЅР°С‚Р°РјРё РєРѕСЂР°Р±Р»СЏ
        List<GridCell> roomCells = gridManager.GetCellsByObjectType("Room");
        if (roomCells.Count == 0)
        {
            // Р•СЃР»Рё РЅРµС‚ РєРѕРјРЅР°С‚, РёСЃРїРѕР»СЊР·СѓРµРј С‚РµСЂСЂРёС‚РѕСЂРёСЋ РІРѕРєСЂСѓРі РєРѕРєРїРёС‚Р°
            roomCells = gridManager.GetCellsByObjectType("Cockpit");
        }

        // Р•СЃР»Рё РЅРµС‚ РЅРё РєРѕРјРЅР°С‚, РЅРё РєРѕРєРїРёС‚Р°, РїСЂРѕРІРµСЂСЏРµРј С‚РµСЂСЂРёС‚РѕСЂРёСЋ РІРѕРєСЂСѓРі РїРµСЂСЃРѕРЅР°Р¶РµР№ РёРіСЂРѕРєР°
        if (roomCells.Count == 0)
        {
            roomCells = GetPlayerCharacterCells();
        }

        // РЎРѕР·РґР°РµРј HashSet РґР»СЏ Р±С‹СЃС‚СЂРѕР№ РїСЂРѕРІРµСЂРєРё РїСЂРёРЅР°РґР»РµР¶РЅРѕСЃС‚Рё Рє С‚РµСЂСЂРёС‚РѕСЂРёРё РєРѕСЂР°Р±Р»СЏ
        HashSet<Vector2Int> shipTerritory = new HashSet<Vector2Int>();
        foreach (GridCell cell in roomCells)
        {
            shipTerritory.Add(cell.gridPosition);

            // Р”РѕР±Р°РІР»СЏРµРј С‚Р°РєР¶Рµ СЃРѕСЃРµРґРЅРёРµ РєР»РµС‚РєРё (РІРЅСѓС‚СЂРё РєРѕРјРЅР°С‚)
            AddAdjacentCells(cell.gridPosition, shipTerritory, 3);
        }

        // РС‰РµРј РїСЂРµРґРјРµС‚С‹-СЂРµСЃСѓСЂСЃС‹ РЅР° С‚РµСЂСЂРёС‚РѕСЂРёРё РєРѕСЂР°Р±Р»СЏ
        // РСЃРїРѕР»СЊР·СѓРµРј РєСЌС€РёСЂРѕРІР°РЅРЅС‹Р№ РјР°СЃСЃРёРІ РІРјРµСЃС‚Рѕ FindObjectsOfType
        if (cachedItems == null || cachedItems.Length == 0)
        {
            RefreshCache();
        }

        foreach (Item item in cachedItems)
        {
            // Р—РђР©РРўРђ: РџСЂРѕРІРµСЂСЏРµРј С‡С‚Рѕ item РЅРµ Р±С‹Р» СѓРЅРёС‡С‚РѕР¶РµРЅ
            if (item == null || ReferenceEquals(item, null))
            {
                continue;
            }

            // Р—РђР©РРўРђ: Р‘РµР·РѕРїР°СЃРЅРѕ РїРѕР»СѓС‡Р°РµРј itemData
            ItemData itemData = null;
            try
            {
                itemData = item.itemData;
            }
            catch (System.Exception)
            {
                // Item Р±С‹Р» СѓРЅРёС‡С‚РѕР¶РµРЅ РІРѕ РІСЂРµРјСЏ РѕР±СЂР°С‰РµРЅРёСЏ
                continue;
            }

            if (itemData != null && itemData.itemType == ItemType.Resource)
            {
                // Р—РђР©РРўРђ: Р‘РµР·РѕРїР°СЃРЅРѕ РїРѕР»СѓС‡Р°РµРј РїРѕР·РёС†РёСЋ
                Vector3 itemPosition;
                try
                {
                    itemPosition = item.transform.position;
                }
                catch (System.Exception)
                {
                    // Item Р±С‹Р» СѓРЅРёС‡С‚РѕР¶РµРЅ РІРѕ РІСЂРµРјСЏ РѕР±СЂР°С‰РµРЅРёСЏ
                    continue;
                }

                Vector2Int itemGridPos = gridManager.WorldToGrid(itemPosition);

                // РџСЂРѕРІРµСЂСЏРµРј, РЅР°С…РѕРґРёС‚СЃСЏ Р»Рё РїСЂРµРґРјРµС‚ РЅР° С‚РµСЂСЂРёС‚РѕСЂРёРё РєРѕСЂР°Р±Р»СЏ
                if (shipTerritory.Contains(itemGridPos))
                {
                    string resourceName = itemData.itemName;

                    if (resources.ContainsKey(resourceName))
                    {
                        resources[resourceName] += 1;
                    }
                    else
                    {
                        resources[resourceName] = 1;
                    }
                }
            }
        }
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РєР»РµС‚РєРё, РіРґРµ РЅР°С…РѕРґСЏС‚СЃСЏ РїРµСЂСЃРѕРЅР°Р¶Рё РёРіСЂРѕРєР°
    /// </summary>
    List<GridCell> GetPlayerCharacterCells()
    {
        List<GridCell> cells = new List<GridCell>();

        Character[] characters = FindObjectsOfType<Character>();
        foreach (Character character in characters)
        {
            if (character.IsPlayerCharacter())
            {
                Vector2Int gridPos = gridManager.WorldToGrid(character.transform.position);
                GridCell cell = gridManager.GetCell(gridPos);
                if (cell != null)
                {
                    cells.Add(cell);
                }
            }
        }

        return cells;
    }

    /// <summary>
    /// Р”РѕР±Р°РІРёС‚СЊ СЃРѕСЃРµРґРЅРёРµ РєР»РµС‚РєРё РІ СЂР°РґРёСѓСЃРµ
    /// </summary>
    void AddAdjacentCells(Vector2Int center, HashSet<Vector2Int> cellSet, int radius)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector2Int adjacentPos = new Vector2Int(center.x + x, center.y + y);
                if (gridManager.IsValidGridPosition(adjacentPos))
                {
                    cellSet.Add(adjacentPos);
                }
            }
        }
    }

    /// <summary>
    /// РћС‡РёСЃС‚РёС‚СЊ РІСЃРµ СЃР»РѕС‚С‹ СЂРµСЃСѓСЂСЃРѕРІ
    /// </summary>
    void ClearResourceSlots()
    {
        foreach (var kvp in resourceSlots)
        {
            if (kvp.Value != null && kvp.Value.gameObject != null)
            {
                Destroy(kvp.Value.gameObject);
            }
        }
        resourceSlots.Clear();
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РєРѕР»РёС‡РµСЃС‚РІРѕ РѕРїСЂРµРґРµР»РµРЅРЅРѕРіРѕ СЂРµСЃСѓСЂСЃР°
    /// </summary>
    public int GetResourceQuantity(string resourceName)
    {
        if (resourceSlots.ContainsKey(resourceName) && resourceSlots[resourceName] != null)
        {
            return resourceSlots[resourceName].GetQuantity();
        }
        return 0;
    }

    /// <summary>
    /// РџСЂРёРЅСѓРґРёС‚РµР»СЊРЅРѕ РѕР±РЅРѕРІРёС‚СЊ РїР°РЅРµР»СЊ
    /// </summary>
    public void ForceUpdate()
    {
        UpdateResourceDisplay();
    }

    void OnDestroy()
    {
        // PERFORMANCE FIX: РћС‚РїРёСЃС‹РІР°РµРјСЃСЏ РѕС‚ СЃРѕР±С‹С‚РёР№
        EventBus.Unsubscribe<CharacterSpawnedEvent>(OnCharacterSpawned);

        ClearResourceSlots();
        isInitialized = false;
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РїРѕР»РЅС‹Р№ РїСѓС‚СЊ GameObject РІ РёРµСЂР°СЂС…РёРё
    /// </summary>
    string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}
