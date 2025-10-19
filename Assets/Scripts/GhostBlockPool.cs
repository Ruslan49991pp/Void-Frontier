using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// РЎРёСЃС‚РµРјР° РїСѓР»РёРЅРіР° РґР»СЏ РїСЂРёР·СЂР°С‡РЅС‹С… Р±Р»РѕРєРѕРІ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
/// РџРµСЂРµРёСЃРїРѕР»СЊР·СѓРµС‚ Р±Р»РѕРєРё РІРјРµСЃС‚Рѕ СЃРѕР·РґР°РЅРёСЏ/СѓРґР°Р»РµРЅРёСЏ РґР»СЏ РѕРїС‚РёРјРёР·Р°С†РёРё РїСЂРѕРёР·РІРѕРґРёС‚РµР»СЊРЅРѕСЃС‚Рё
/// </summary>
public class GhostBlockPool : MonoBehaviour
{
    private static GhostBlockPool instance;

    public static GhostBlockPool Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("GhostBlockPool");
                instance = go.AddComponent<GhostBlockPool>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    // РЎР»РѕРІР°СЂСЊ РїСѓР»РѕРІ РґР»СЏ СЂР°Р·РЅС‹С… С‚РёРїРѕРІ РїСЂРµС„Р°Р±РѕРІ
    private Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();

    // РљРѕРЅС‚РµР№РЅРµСЂС‹ РґР»СЏ РѕСЂРіР°РЅРёР·Р°С†РёРё РёРµСЂР°СЂС…РёРё
    private Dictionary<GameObject, Transform> poolContainers = new Dictionary<GameObject, Transform>();

    // РћС‚СЃР»РµР¶РёРІР°РЅРёРµ Р°РєС‚РёРІРЅС‹С… Р±Р»РѕРєРѕРІ РґР»СЏ Р±С‹СЃС‚СЂРѕРіРѕ РІРѕР·РІСЂР°С‚Р°
    private Dictionary<GameObject, GameObject> activeBlocks = new Dictionary<GameObject, GameObject>(); // instance -> prefab

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
        }
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ Р±Р»РѕРє РёР· РїСѓР»Р° (РёР»Рё СЃРѕР·РґР°С‚СЊ РЅРѕРІС‹Р№)
    /// </summary>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            return null;
        }

        // РРЅРёС†РёР°Р»РёР·РёСЂСѓРµРј РїСѓР» РґР»СЏ СЌС‚РѕРіРѕ РїСЂРµС„Р°Р±Р° РµСЃР»Рё РµРіРѕ РЅРµС‚
        if (!pools.ContainsKey(prefab))
        {
            pools[prefab] = new Queue<GameObject>();

            // РЎРѕР·РґР°РµРј РєРѕРЅС‚РµР№РЅРµСЂ РґР»СЏ РЅРµР°РєС‚РёРІРЅС‹С… Р±Р»РѕРєРѕРІ СЌС‚РѕРіРѕ С‚РёРїР°
            GameObject container = new GameObject($"Pool_{prefab.name}");
            container.transform.SetParent(transform);
            poolContainers[prefab] = container.transform;
        }

        GameObject block;

        // Р‘РµСЂРµРј РёР· РїСѓР»Р° РёР»Рё СЃРѕР·РґР°РµРј РЅРѕРІС‹Р№
        if (pools[prefab].Count > 0)
        {
            block = pools[prefab].Dequeue();
            block.transform.position = position;
            block.transform.rotation = rotation;
            block.SetActive(true);
        }
        else
        {
            block = Instantiate(prefab, position, rotation);
            block.name = $"{prefab.name}_Pooled";
        }

        // Р РµРіРёСЃС‚СЂРёСЂСѓРµРј Р°РєС‚РёРІРЅС‹Р№ Р±Р»РѕРє
        activeBlocks[block] = prefab;

        return block;
    }

    /// <summary>
    /// Р’РµСЂРЅСѓС‚СЊ Р±Р»РѕРє РІ РїСѓР»
    /// </summary>
    public void Return(GameObject block)
    {
        if (block == null) return;

        // РџСЂРѕРІРµСЂСЏРµРј С‡С‚Рѕ Р±Р»РѕРє Р±С‹Р» РІР·СЏС‚ РёР· РїСѓР»Р°
        if (!activeBlocks.ContainsKey(block))
        {
            // Р­С‚Рѕ Р±Р»РѕРє СЃРѕР·РґР°РЅРЅС‹Р№ РІРЅРµ РїСѓР»Р° - РїСЂРѕСЃС‚Рѕ СѓРЅРёС‡С‚РѕР¶Р°РµРј
            Destroy(block);
            return;
        }

        GameObject prefab = activeBlocks[block];
        activeBlocks.Remove(block);

        // Р”РµР°РєС‚РёРІРёСЂСѓРµРј Рё РїРѕРјРµС‰Р°РµРј РІ РєРѕРЅС‚РµР№РЅРµСЂ
        block.SetActive(false);
        block.transform.SetParent(poolContainers[prefab]);

        pools[prefab].Enqueue(block);
    }

    /// <summary>
    /// Р’РµСЂРЅСѓС‚СЊ РЅРµСЃРєРѕР»СЊРєРѕ Р±Р»РѕРєРѕРІ РІ РїСѓР»
    /// </summary>
    public void ReturnAll(List<GameObject> blocks)
    {
        if (blocks == null) return;

        for (int i = blocks.Count - 1; i >= 0; i--)
        {
            Return(blocks[i]);
        }
        blocks.Clear();
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РєРѕР»РёС‡РµСЃС‚РІРѕ Р±Р»РѕРєРѕРІ РІ РїСѓР»Рµ РґР»СЏ РєРѕРЅРєСЂРµС‚РЅРѕРіРѕ РїСЂРµС„Р°Р±Р°
    /// </summary>
    public int GetPoolSize(GameObject prefab)
    {
        if (!pools.ContainsKey(prefab))
            return 0;
        return pools[prefab].Count;
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РєРѕР»РёС‡РµСЃС‚РІРѕ Р°РєС‚РёРІРЅС‹С… Р±Р»РѕРєРѕРІ
    /// </summary>
    public int GetActiveCount()
    {
        return activeBlocks.Count;
    }

    /// <summary>
    /// РћС‡РёСЃС‚РёС‚СЊ РІСЃРµ РїСѓР»С‹ (РґР»СЏ РѕСЃРІРѕР±РѕР¶РґРµРЅРёСЏ РїР°РјСЏС‚Рё)
    /// </summary>
    public void ClearAllPools()
    {
        foreach (var pool in pools.Values)
        {
            while (pool.Count > 0)
            {
                GameObject block = pool.Dequeue();
                if (block != null)
                    Destroy(block);
            }
        }
        pools.Clear();
        poolContainers.Clear();
        activeBlocks.Clear();
    }

    /// <summary>
    /// РџСЂРµРґРІР°СЂРёС‚РµР»СЊРЅРѕ СЃРѕР·РґР°С‚СЊ Р±Р»РѕРєРё РІ РїСѓР»Рµ
    /// </summary>
    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null) return;

        if (!pools.ContainsKey(prefab))
        {
            pools[prefab] = new Queue<GameObject>();
            GameObject container = new GameObject($"Pool_{prefab.name}");
            container.transform.SetParent(transform);
            poolContainers[prefab] = container.transform;
        }

        for (int i = 0; i < count; i++)
        {
            GameObject block = Instantiate(prefab);
            block.name = $"{prefab.name}_Pooled";
            block.SetActive(false);
            block.transform.SetParent(poolContainers[prefab]);
            pools[prefab].Enqueue(block);
        }
    }
}
