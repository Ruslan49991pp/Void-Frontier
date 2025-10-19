using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// РЎРїР°РІРЅРµСЂ СЂРµСЃСѓСЂСЃРѕРІ РЅР° Р»РѕРєР°С†РёРё
/// РЎРѕР·РґР°РµС‚ СЂРµСЃСѓСЂСЃС‹-РїСЂРµРґРјРµС‚С‹, РєРѕС‚РѕСЂС‹Рµ РјРѕР¶РЅРѕ РїРѕРґРѕР±СЂР°С‚СЊ
/// </summary>
public class ResourceSpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("РњРµРЅРµРґР¶РµСЂ СЂРµСЃСѓСЂСЃРѕРІ СЃРѕ СЃРїРёСЃРєРѕРј РґРѕСЃС‚СѓРїРЅС‹С… СЂРµСЃСѓСЂСЃРѕРІ")]
    public ResourceManager resourceManager;

    [Tooltip("РњРµРЅРµРґР¶РµСЂ СЃРµС‚РєРё РґР»СЏ СЂР°Р·РјРµС‰РµРЅРёСЏ СЂРµСЃСѓСЂСЃРѕРІ")]
    public GridManager gridManager;

    [Tooltip("Р РѕРґРёС‚РµР»СЊСЃРєРёР№ РѕР±СЉРµРєС‚ РґР»СЏ СЃРїР°РІРЅРµРЅРЅС‹С… СЂРµСЃСѓСЂСЃРѕРІ")]
    public Transform resourceParent;

    [Header("Spawn Settings")]
    [Tooltip("РњРёРЅРёРјР°Р»СЊРЅРѕРµ РєРѕР»РёС‡РµСЃС‚РІРѕ РјРµС‚Р°Р»Р»Р° РґР»СЏ СЃРїР°РІРЅР°")]
    public int minMetalSpawns = 15;

    [Tooltip("РњР°РєСЃРёРјР°Р»СЊРЅРѕРµ РєРѕР»РёС‡РµСЃС‚РІРѕ РјРµС‚Р°Р»Р»Р° РґР»СЏ СЃРїР°РІРЅР°")]
    public int maxMetalSpawns = 30;

    [Tooltip("Р Р°Р·РјРµСЂ СЂРµСЃСѓСЂСЃР° РІ РјРёСЂРµ (РјР°СЃС€С‚Р°Р±)")]
    public float resourceWorldSize = 0.3f;

    [Tooltip("РђРІС‚РѕРјР°С‚РёС‡РµСЃРєРёР№ СЃРїР°РІРЅ РїСЂРё СЃС‚Р°СЂС‚Рµ")]
    public bool autoSpawnOnStart = true;

    [Tooltip("Р¦РІРµС‚ РјРµС‚Р°Р»Р»Р°")]
    public Color metalColor = new Color(0.7f, 0.7f, 0.8f); // РњРµС‚Р°Р»Р»РёС‡РµСЃРєРёР№ СЃРµСЂРѕ-РіРѕР»СѓР±РѕР№

    // Р’РЅСѓС‚СЂРµРЅРЅРёРµ РїРµСЂРµРјРµРЅРЅС‹Рµ
    private List<GameObject> spawnedResources = new List<GameObject>();

    void Start()
    {
        // РќР°С…РѕРґРёРј РЅРµРґРѕСЃС‚Р°СЋС‰РёРµ РєРѕРјРїРѕРЅРµРЅС‚С‹
        if (resourceManager == null)
        {
            resourceManager = Resources.Load<ResourceManager>("ResourceManager");
            if (resourceManager == null)
            {
            }
        }

        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
            if (gridManager == null)
            {
            }
        }

        if (resourceParent == null)
        {
            // РЎРѕР·РґР°РµРј СЂРѕРґРёС‚РµР»СЊСЃРєРёР№ РѕР±СЉРµРєС‚ РґР»СЏ РѕСЂРіР°РЅРёР·Р°С†РёРё РёРµСЂР°СЂС…РёРё
            GameObject parentObj = new GameObject("SpawnedResources");
            parentObj.transform.SetParent(transform);
            resourceParent = parentObj.transform;
        }

        if (autoSpawnOnStart)
        {
            // Р—Р°РґРµСЂР¶РєР° РґР»СЏ С‚РѕРіРѕ С‡С‚РѕР±С‹ LocationManager СѓСЃРїРµР» СЃРѕР·РґР°С‚СЊ РїСЂРµРїСЏС‚СЃС‚РІРёСЏ
            StartCoroutine(DelayedSpawnResources());
        }
    }

    /// <summary>
    /// РЎРїР°РІРЅ СЂРµСЃСѓСЂСЃРѕРІ СЃ Р·Р°РґРµСЂР¶РєРѕР№
    /// </summary>
    System.Collections.IEnumerator DelayedSpawnResources()
    {
        // Р–РґРµРј РїРѕРєР° LocationManager СЃРѕР·РґР°СЃС‚ РІСЃРµ РїСЂРµРїСЏС‚СЃС‚РІРёСЏ
        // Р РїРѕРєР° SceneSetup СЃРѕР·РґР°СЃС‚ GridManager
        yield return new WaitForSeconds(0.5f);

        // РџС‹С‚Р°РµРјСЃСЏ РЅР°Р№С‚Рё GridManager РµС‰Рµ СЂР°Р· РїРµСЂРµРґ СЃРїР°РІРЅРѕРј
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();

            if (gridManager == null)
            {
                yield break;
            }
        }

        SpawnMetalResources();
    }

    /// <summary>
    /// РЎРѕР·РґР°С‚СЊ РјРµС‚Р°Р»Р»РёС‡РµСЃРєРёРµ СЂРµСЃСѓСЂСЃС‹ РЅР° Р»РѕРєР°С†РёРё
    /// </summary>
    public void SpawnMetalResources()
    {
        if (resourceManager == null)
        {
            return;
        }

        if (gridManager == null)
        {
            return;
        }

        // РџРѕР»СѓС‡Р°РµРј РґР°РЅРЅС‹Рµ СЂРµСЃСѓСЂСЃР° "РњРµС‚Р°Р»Р»"
        ResourceData metalData = resourceManager.GetResourceByName(ItemNames.METAL);
        if (metalData == null)
        {
            return;
        }

        // РћРїСЂРµРґРµР»СЏРµРј РєРѕР»РёС‡РµСЃС‚РІРѕ РґР»СЏ СЃРїР°РІРЅР°
        int spawnCount = Random.Range(minMetalSpawns, maxMetalSpawns + 1);

        int successfulSpawns = 0;
        int attempts = 0;
        int maxAttempts = spawnCount * 3; // Р”Р°РµРј Р±РѕР»СЊС€Рµ РїРѕРїС‹С‚РѕРє

        while (successfulSpawns < spawnCount && attempts < maxAttempts)
        {
            attempts++;

            // РџРѕР»СѓС‡Р°РµРј СЃР»СѓС‡Р°Р№РЅСѓСЋ СЃРІРѕР±РѕРґРЅСѓСЋ РєР»РµС‚РєСѓ
            GridCell cell = gridManager.GetRandomFreeCell();
            if (cell == null)
            {
                continue;
            }

            // РЎРѕР·РґР°РµРј СЂРµСЃСѓСЂСЃ-РїСЂРµРґРјРµС‚
            GameObject resourceItem = CreateResourceItem(metalData, cell.worldPosition);
            if (resourceItem != null)
            {
                // Р—Р°РЅРёРјР°РµРј РєР»РµС‚РєСѓ
                gridManager.OccupyCell(cell.gridPosition, resourceItem, "Resource");

                // Р РµРіРёСЃС‚СЂРёСЂСѓРµРј
                spawnedResources.Add(resourceItem);
                successfulSpawns++;
            }
        }


    }

    /// <summary>
    /// РЎРѕР·РґР°С‚СЊ РїСЂРµРґРјРµС‚-СЂРµСЃСѓСЂСЃ РІ РјРёСЂРµ
    /// </summary>
    GameObject CreateResourceItem(ResourceData resourceData, Vector3 position)
    {
        GameObject resourceItem;

        // Р•СЃР»Рё Сѓ СЂРµСЃСѓСЂСЃР° РµСЃС‚СЊ РїСЂРµС„Р°Р±, РёСЃРїРѕР»СЊР·СѓРµРј РµРіРѕ
        if (resourceData.prefab != null)
        {
            resourceItem = Instantiate(resourceData.prefab, position, Quaternion.identity, resourceParent);
        }
        else
        {
            // РЎРѕР·РґР°РµРј РїСЂРѕСЃС‚РѕР№ РєСѓР± РєР°Рє fallback
            resourceItem = GameObject.CreatePrimitive(PrimitiveType.Cube);
            resourceItem.transform.position = position;
            resourceItem.transform.localScale = Vector3.one * resourceWorldSize;
            resourceItem.transform.SetParent(resourceParent);

            // РЈСЃС‚Р°РЅР°РІР»РёРІР°РµРј С†РІРµС‚ РўРћР›Р¬РљРћ РґР»СЏ fallback РєСѓР±Р°
            Renderer renderer = resourceItem.GetComponent<Renderer>();
            if (renderer != null)
            {
                // РСЃРїРѕР»СЊР·СѓРµРј СЃСѓС‰РµСЃС‚РІСѓСЋС‰РёР№ РјР°С‚РµСЂРёР°Р» Рё РєРѕРїРёСЂСѓРµРј РµРіРѕ
                Material material = new Material(renderer.sharedMaterial);

                // РџСЂРѕРІРµСЂСЏРµРј, РµСЃС‚СЊ Р»Рё Сѓ РјР°С‚РµСЂРёР°Р»Р° РїРѕРґРґРµСЂР¶РєР° С†РІРµС‚Р°
                if (material.HasProperty("_Color"))
                {
                    material.color = metalColor;
                }

                // РќР°СЃС‚СЂР°РёРІР°РµРј РјРµС‚Р°Р»Р»РёС‡РµСЃРєРёР№ РІРёРґ РµСЃР»Рё С€РµР№РґРµСЂ РїРѕРґРґРµСЂР¶РёРІР°РµС‚
                if (material.HasProperty("_Metallic"))
                {
                    material.SetFloat("_Metallic", 0.8f);
                }

                if (material.HasProperty("_Glossiness") || material.HasProperty("_Smoothness"))
                {
                    if (material.HasProperty("_Glossiness"))
                        material.SetFloat("_Glossiness", 0.6f);
                    else if (material.HasProperty("_Smoothness"))
                        material.SetFloat("_Smoothness", 0.6f);
                }

                renderer.material = material;
            }
        }

        resourceItem.name = $"Resource_{resourceData.resourceName}";

        // РЈСЃС‚Р°РЅР°РІР»РёРІР°РµРј СЃР»РѕР№ "Selectable" РґР»СЏ РІР·Р°РёРјРѕРґРµР№СЃС‚РІРёСЏ
        // Р’РђР–РќРћ: РЈСЃС‚Р°РЅР°РІР»РёРІР°РµРј СЂРµРєСѓСЂСЃРёРІРЅРѕ РґР»СЏ РІСЃРµС… РґРѕС‡РµСЂРЅРёС… РѕР±СЉРµРєС‚РѕРІ!
        int selectableLayer = LayerMask.NameToLayer("Selectable");
        if (selectableLayer != -1)
        {
            SetLayerRecursively(resourceItem, selectableLayer);
        }
        else
        {
        }

        // РЎРѕР·РґР°РµРј ItemData РёР· ResourceData
        ItemData itemData = CreateItemDataFromResource(resourceData);

        // Р”РѕР±Р°РІР»СЏРµРј РєРѕРјРїРѕРЅРµРЅС‚ Item
        Item itemComponent = resourceItem.GetComponent<Item>();
        if (itemComponent == null)
        {
            itemComponent = resourceItem.AddComponent<Item>();
        }

        // РќР°СЃС‚СЂР°РёРІР°РµРј Item
        itemComponent.SetItemData(itemData);
        itemComponent.canBePickedUp = true;
        itemComponent.pickupRange = 1.5f; // РўРћР›Р¬РљРћ СЃРѕСЃРµРґРЅСЏСЏ РєР»РµС‚РєР°!

        // РЈР±РµР¶РґР°РµРјСЃСЏ С‡С‚Рѕ РµСЃС‚СЊ РєРѕР»Р»Р°Р№РґРµСЂ РґР»СЏ РІР·Р°РёРјРѕРґРµР№СЃС‚РІРёСЏ
        Collider collider = resourceItem.GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true; // РўСЂРёРіРіРµСЂ - РёРіСЂРѕРє РїСЂРѕС…РѕРґРёС‚ СЃРєРІРѕР·СЊ
            // Р Р°Р·РјРµСЂ РєРѕР»Р»Р°Р№РґРµСЂР° РѕСЃС‚Р°РІР»СЏРµРј СЃС‚Р°РЅРґР°СЂС‚РЅС‹Рј С‡С‚РѕР±С‹ РЅРµ Р±Р»РѕРєРёСЂРѕРІР°С‚СЊ NavMesh
        }
        else
        {
        }

        // РЈР±РёСЂР°РµРј Rigidbody РµСЃР»Рё РµСЃС‚СЊ (С‡С‚РѕР±С‹ РїСЂРµРґРјРµС‚ РЅРµ РїР°РґР°Р» Рё РЅРµ Р±Р»РѕРєРёСЂРѕРІР°Р» РґРІРёР¶РµРЅРёРµ)
        Rigidbody rb = resourceItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Destroy(rb);
        }

        // Р”РѕР±Р°РІР»СЏРµРј РєРѕРјРїРѕРЅРµРЅС‚ LocationObjectInfo РґР»СЏ РєРѕРЅСЃРёСЃС‚РµРЅС‚РЅРѕСЃС‚Рё
        LocationObjectInfo objectInfo = resourceItem.GetComponent<LocationObjectInfo>();
        if (objectInfo == null)
        {
            objectInfo = resourceItem.AddComponent<LocationObjectInfo>();
        }

        objectInfo.objectName = resourceData.resourceName;
        objectInfo.objectType = "Resource";
        objectInfo.health = 10f;
        objectInfo.canBeScavenged = true;

        // Р”РѕР±Р°РІР»СЏРµРј РјРµРґР»РµРЅРЅРѕРµ РІСЂР°С‰РµРЅРёРµ РґР»СЏ Р»СѓС‡С€РµР№ Р·Р°РјРµС‚РЅРѕСЃС‚Рё
        RotateObject rotator = resourceItem.GetComponent<RotateObject>();
        if (rotator == null)
        {
            rotator = resourceItem.AddComponent<RotateObject>();
            rotator.rotationSpeed = 30f; // 30 РіСЂР°РґСѓСЃРѕРІ РІ СЃРµРєСѓРЅРґСѓ
            rotator.rotateY = true;
            rotator.enableBobbing = true; // Р›РµРіРєРѕРµ РґРІРёР¶РµРЅРёРµ РІРІРµСЂС…-РІРЅРёР·
            rotator.bobbingAmount = 0.05f; // РќРµР±РѕР»СЊС€Р°СЏ Р°РјРїР»РёС‚СѓРґР°
            rotator.bobbingSpeed = 2f;
        }

        return resourceItem;
    }

    /// <summary>
    /// РЎРѕР·РґР°С‚СЊ ItemData РёР· ResourceData
    /// </summary>
    ItemData CreateItemDataFromResource(ResourceData resourceData)
    {
        ItemData itemData = new ItemData();

        itemData.itemName = resourceData.resourceName;
        itemData.description = resourceData.description;
        itemData.itemType = ItemType.Resource;
        itemData.rarity = ItemRarity.Common;

        itemData.maxStackSize = resourceData.maxStackSize;
        itemData.weight = resourceData.weightPerUnit;
        itemData.value = resourceData.valuePerUnit;

        itemData.icon = resourceData.icon;
        itemData.prefab = resourceData.prefab;

        return itemData;
    }

    /// <summary>
    /// РћС‡РёСЃС‚РёС‚СЊ РІСЃРµ СЃРїР°РІРЅРµРЅРЅС‹Рµ СЂРµСЃСѓСЂСЃС‹
    /// </summary>
    public void ClearSpawnedResources()
    {
        foreach (GameObject resource in spawnedResources)
        {
            if (resource != null)
            {
                // РћСЃРІРѕР±РѕР¶РґР°РµРј РєР»РµС‚РєСѓ РІ СЃРµС‚РєРµ
                if (gridManager != null)
                {
                    Vector2Int gridPos = gridManager.WorldToGrid(resource.transform.position);
                    gridManager.FreeCell(gridPos);
                }

                Destroy(resource);
            }
        }

        spawnedResources.Clear();
    }

    /// <summary>
    /// РџРµСЂРµРіРµРЅРµСЂРёСЂРѕРІР°С‚СЊ СЂРµСЃСѓСЂСЃС‹
    /// </summary>
    [ContextMenu("Regenerate Resources")]
    public void RegenerateResources()
    {
        ClearSpawnedResources();
        SpawnMetalResources();
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РєРѕР»РёС‡РµСЃС‚РІРѕ СЃРїР°РІРЅРµРЅРЅС‹С… СЂРµСЃСѓСЂСЃРѕРІ
    /// </summary>
    public int GetSpawnedResourceCount()
    {
        // РЈРґР°Р»СЏРµРј null СЃСЃС‹Р»РєРё (РїРѕРґРѕР±СЂР°РЅРЅС‹Рµ СЂРµСЃСѓСЂСЃС‹)
        spawnedResources.RemoveAll(r => r == null);
        return spawnedResources.Count;
    }

    /// <summary>
    /// РЈСЃС‚Р°РЅРѕРІРёС‚СЊ СЃР»РѕР№ СЂРµРєСѓСЂСЃРёРІРЅРѕ РґР»СЏ РѕР±СЉРµРєС‚Р° Рё РІСЃРµС… РµРіРѕ РґРѕС‡РµСЂРЅРёС… РѕР±СЉРµРєС‚РѕРІ
    /// </summary>
    void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null)
            return;

        obj.layer = layer;

        // Р РµРєСѓСЂСЃРёРІРЅРѕ СѓСЃС‚Р°РЅР°РІР»РёРІР°РµРј СЃР»РѕР№ РґР»СЏ РІСЃРµС… РґРѕС‡РµСЂРЅРёС… РѕР±СЉРµРєС‚РѕРІ
        foreach (Transform child in obj.transform)
        {
            if (child != null)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }
}
