using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Система пулинга для призрачных блоков строительства
/// Переиспользует блоки вместо создания/удаления для оптимизации производительности
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

    // Словарь пулов для разных типов префабов
    private Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();

    // Контейнеры для организации иерархии
    private Dictionary<GameObject, Transform> poolContainers = new Dictionary<GameObject, Transform>();

    // Отслеживание активных блоков для быстрого возврата
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
    /// Получить блок из пула (или создать новый)
    /// </summary>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogError("[GhostBlockPool] Prefab is null!");
            return null;
        }

        // Инициализируем пул для этого префаба если его нет
        if (!pools.ContainsKey(prefab))
        {
            pools[prefab] = new Queue<GameObject>();

            // Создаем контейнер для неактивных блоков этого типа
            GameObject container = new GameObject($"Pool_{prefab.name}");
            container.transform.SetParent(transform);
            poolContainers[prefab] = container.transform;
        }

        GameObject block;

        // Берем из пула или создаем новый
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

        // Регистрируем активный блок
        activeBlocks[block] = prefab;

        return block;
    }

    /// <summary>
    /// Вернуть блок в пул
    /// </summary>
    public void Return(GameObject block)
    {
        if (block == null) return;

        // Проверяем что блок был взят из пула
        if (!activeBlocks.ContainsKey(block))
        {
            // Это блок созданный вне пула - просто уничтожаем
            Destroy(block);
            return;
        }

        GameObject prefab = activeBlocks[block];
        activeBlocks.Remove(block);

        // Деактивируем и помещаем в контейнер
        block.SetActive(false);
        block.transform.SetParent(poolContainers[prefab]);

        pools[prefab].Enqueue(block);
    }

    /// <summary>
    /// Вернуть несколько блоков в пул
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
    /// Получить количество блоков в пуле для конкретного префаба
    /// </summary>
    public int GetPoolSize(GameObject prefab)
    {
        if (!pools.ContainsKey(prefab))
            return 0;
        return pools[prefab].Count;
    }

    /// <summary>
    /// Получить количество активных блоков
    /// </summary>
    public int GetActiveCount()
    {
        return activeBlocks.Count;
    }

    /// <summary>
    /// Очистить все пулы (для освобождения памяти)
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
    /// Предварительно создать блоки в пуле
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
