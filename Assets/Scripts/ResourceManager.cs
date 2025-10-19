using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// РњРµРЅРµРґР¶РµСЂ СЂРµСЃСѓСЂСЃРѕРІ - Р±Р°Р·Р° РґР°РЅРЅС‹С… РІСЃРµС… СЂРµСЃСѓСЂСЃРѕРІ РІ РёРіСЂРµ
/// РЎРѕР·РґР°С‚СЊ С‡РµСЂРµР· Assets -> Create -> Resources/Resource Manager
/// </summary>
[CreateAssetMenu(fileName = "ResourceManager", menuName = "Resources/Resource Manager")]
public class ResourceManager : ScriptableObject
{
    [Header("Р‘Р°Р·Р° РґР°РЅРЅС‹С… СЂРµСЃСѓСЂСЃРѕРІ")]
    [Tooltip("РЎРїРёСЃРѕРє РІСЃРµС… СЂРµСЃСѓСЂСЃРѕРІ РІ РёРіСЂРµ")]
    public List<ResourceData> allResources = new List<ResourceData>();

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ СЂРµСЃСѓСЂСЃ РїРѕ РЅР°Р·РІР°РЅРёСЋ
    /// </summary>
    public ResourceData GetResourceByName(string resourceName)
    {
        if (string.IsNullOrEmpty(resourceName))
            return null;

        return allResources.FirstOrDefault(r =>
            r != null && r.resourceName.Equals(resourceName, System.StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РІСЃРµ СЂРµСЃСѓСЂСЃС‹ РѕРїСЂРµРґРµР»РµРЅРЅРѕР№ РєР°С‚РµРіРѕСЂРёРё
    /// </summary>
    public List<ResourceData> GetResourcesByCategory(ResourceCategory category)
    {
        return allResources.Where(r => r != null && r.category == category).ToList();
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РёРєРѕРЅРєСѓ СЂРµСЃСѓСЂСЃР° РїРѕ РЅР°Р·РІР°РЅРёСЋ
    /// </summary>
    public Sprite GetResourceIcon(string resourceName)
    {
        ResourceData resource = GetResourceByName(resourceName);
        return resource != null ? resource.icon : null;
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РїСЂРµС„Р°Р± СЂРµСЃСѓСЂСЃР° РїРѕ РЅР°Р·РІР°РЅРёСЋ
    /// </summary>
    public GameObject GetResourcePrefab(string resourceName)
    {
        ResourceData resource = GetResourceByName(resourceName);
        return resource != null ? resource.prefab : null;
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ, СЃСѓС‰РµСЃС‚РІСѓРµС‚ Р»Рё СЂРµСЃСѓСЂСЃ
    /// </summary>
    public bool ResourceExists(string resourceName)
    {
        return GetResourceByName(resourceName) != null;
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РѕР±С‰РµРµ РєРѕР»РёС‡РµСЃС‚РІРѕ СЂРµСЃСѓСЂСЃРѕРІ
    /// </summary>
    public int GetTotalResourceCount()
    {
        return allResources.Count(r => r != null);
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РІСЃРµ РЅР°Р·РІР°РЅРёСЏ СЂРµСЃСѓСЂСЃРѕРІ
    /// </summary>
    public List<string> GetAllResourceNames()
    {
        return allResources.Where(r => r != null).Select(r => r.resourceName).ToList();
    }

    /// <summary>
    /// Р”РѕР±Р°РІРёС‚СЊ СЂРµСЃСѓСЂСЃ РІ Р±Р°Р·Сѓ РґР°РЅРЅС‹С… (РґР»СЏ РёСЃРїРѕР»СЊР·РѕРІР°РЅРёСЏ РІ Editor)
    /// </summary>
    public void AddResource(ResourceData resource)
    {
        if (resource == null)
            return;

        // РџСЂРѕРІРµСЂСЏРµРј, РЅРµС‚ Р»Рё СѓР¶Рµ С‚Р°РєРѕРіРѕ СЂРµСЃСѓСЂСЃР°
        if (!allResources.Contains(resource) && !ResourceExists(resource.resourceName))
        {
            allResources.Add(resource);
        }
        else
        {
        }
    }

    /// <summary>
    /// РЈРґР°Р»РёС‚СЊ СЂРµСЃСѓСЂСЃ РёР· Р±Р°Р·С‹ РґР°РЅРЅС‹С… (РґР»СЏ РёСЃРїРѕР»СЊР·РѕРІР°РЅРёСЏ РІ Editor)
    /// </summary>
    public void RemoveResource(ResourceData resource)
    {
        if (resource == null)
            return;

        allResources.Remove(resource);
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ СЃС‚Р°С‚РёСЃС‚РёРєСѓ РїРѕ РєР°С‚РµРіРѕСЂРёСЏРј
    /// </summary>
    public string GetCategoryStats()
    {
        var stats = "РЎС‚Р°С‚РёСЃС‚РёРєР° СЂРµСЃСѓСЂСЃРѕРІ:\n";
        foreach (ResourceCategory category in System.Enum.GetValues(typeof(ResourceCategory)))
        {
            int count = GetResourcesByCategory(category).Count;
            stats += $"  {category}: {count}\n";
        }
        stats += $"Р’СЃРµРіРѕ СЂРµСЃСѓСЂСЃРѕРІ: {GetTotalResourceCount()}";
        return stats;
    }

#if UNITY_EDITOR
    /// <summary>
    /// РћС‡РёСЃС‚РёС‚СЊ null СЌР»РµРјРµРЅС‚С‹ РёР· СЃРїРёСЃРєР°
    /// </summary>
    [ContextMenu("РћС‡РёСЃС‚РёС‚СЊ NULL СЌР»РµРјРµРЅС‚С‹")]
    public void RemoveNullEntries()
    {
        int beforeCount = allResources.Count;
        allResources.RemoveAll(r => r == null);
        int afterCount = allResources.Count;
        int removed = beforeCount - afterCount;

        if (removed > 0)
        {

            UnityEditor.EditorUtility.SetDirty(this);
        }
        else
        {

        }
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ С†РµР»РѕСЃС‚РЅРѕСЃС‚СЊ Р±Р°Р·С‹ РґР°РЅРЅС‹С…
    /// </summary>
    [ContextMenu("РџСЂРѕРІРµСЂРёС‚СЊ С†РµР»РѕСЃС‚РЅРѕСЃС‚СЊ")]
    public void ValidateDatabase()
    {
        int errors = 0;
        int nullCount = 0;

        foreach (var resource in allResources)
        {
            if (resource == null)
            {
                nullCount++;
                errors++;
                continue;
            }

            if (string.IsNullOrEmpty(resource.resourceName))
            {
                errors++;
            }

            if (resource.icon == null)
            {
            }

            if (resource.prefab == null)
            {
            }
        }

        if (nullCount > 0)
        {
        }

        if (errors == 0)
        {

        }
        else
        {
        }
    }

    /// <summary>
    /// Р’С‹РІРµСЃС‚Рё СЃС‚Р°С‚РёСЃС‚РёРєСѓ
    /// </summary>
    [ContextMenu("РџРѕРєР°Р·Р°С‚СЊ СЃС‚Р°С‚РёСЃС‚РёРєСѓ")]
    public void ShowStats()
    {

    }
#endif
}
