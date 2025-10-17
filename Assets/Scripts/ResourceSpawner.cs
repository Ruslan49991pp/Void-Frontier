using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Спавнер ресурсов на локации
/// Создает ресурсы-предметы, которые можно подобрать
/// </summary>
public class ResourceSpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Менеджер ресурсов со списком доступных ресурсов")]
    public ResourceManager resourceManager;

    [Tooltip("Менеджер сетки для размещения ресурсов")]
    public GridManager gridManager;

    [Tooltip("Родительский объект для спавненных ресурсов")]
    public Transform resourceParent;

    [Header("Spawn Settings")]
    [Tooltip("Минимальное количество металла для спавна")]
    public int minMetalSpawns = 15;

    [Tooltip("Максимальное количество металла для спавна")]
    public int maxMetalSpawns = 30;

    [Tooltip("Размер ресурса в мире (масштаб)")]
    public float resourceWorldSize = 0.3f;

    [Tooltip("Автоматический спавн при старте")]
    public bool autoSpawnOnStart = true;

    [Tooltip("Цвет металла")]
    public Color metalColor = new Color(0.7f, 0.7f, 0.8f); // Металлический серо-голубой

    // Внутренние переменные
    private List<GameObject> spawnedResources = new List<GameObject>();

    void Start()
    {
        // Находим недостающие компоненты
        if (resourceManager == null)
        {
            resourceManager = Resources.Load<ResourceManager>("ResourceManager");
            if (resourceManager == null)
            {
                Debug.LogError("[ResourceSpawner] ResourceManager not found in Resources folder!");
                Debug.LogError("[ResourceSpawner] Please create ResourceManager via Tools > Resources > Complete Setup");
            }
        }

        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
            if (gridManager == null)
            {
                Debug.LogError("[ResourceSpawner] GridManager not found in scene!");
            }
        }

        if (resourceParent == null)
        {
            // Создаем родительский объект для организации иерархии
            GameObject parentObj = new GameObject("SpawnedResources");
            parentObj.transform.SetParent(transform);
            resourceParent = parentObj.transform;
        }

        if (autoSpawnOnStart)
        {
            // Задержка для того чтобы LocationManager успел создать препятствия
            StartCoroutine(DelayedSpawnResources());
        }
    }

    /// <summary>
    /// Спавн ресурсов с задержкой
    /// </summary>
    System.Collections.IEnumerator DelayedSpawnResources()
    {
        // Ждем пока LocationManager создаст все препятствия
        // И пока SceneSetup создаст GridManager
        yield return new WaitForSeconds(0.5f);

        // Пытаемся найти GridManager еще раз перед спавном
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();

            if (gridManager == null)
            {
                Debug.LogError("[ResourceSpawner] GridManager still not found! Make sure SceneSetup is creating it.");
                Debug.LogError("[ResourceSpawner] Cannot spawn resources without GridManager!");
                yield break;
            }
        }

        SpawnMetalResources();
    }

    /// <summary>
    /// Создать металлические ресурсы на локации
    /// </summary>
    public void SpawnMetalResources()
    {
        if (resourceManager == null)
        {
            Debug.LogError("[ResourceSpawner] ResourceManager is NULL! Make sure ResourceManager asset exists in Resources folder.");
            return;
        }

        if (gridManager == null)
        {
            Debug.LogError("[ResourceSpawner] GridManager is NULL! Make sure GridManager exists in the scene.");
            return;
        }

        // Получаем данные ресурса "Металл"
        ResourceData metalData = resourceManager.GetResourceByName(ItemNames.METAL);
        if (metalData == null)
        {
            Debug.LogWarning("[ResourceSpawner] Metal resource not found in ResourceManager!");
            return;
        }

        // Определяем количество для спавна
        int spawnCount = Random.Range(minMetalSpawns, maxMetalSpawns + 1);

        int successfulSpawns = 0;
        int attempts = 0;
        int maxAttempts = spawnCount * 3; // Даем больше попыток

        while (successfulSpawns < spawnCount && attempts < maxAttempts)
        {
            attempts++;

            // Получаем случайную свободную клетку
            GridCell cell = gridManager.GetRandomFreeCell();
            if (cell == null)
            {
                continue;
            }

            // Создаем ресурс-предмет
            GameObject resourceItem = CreateResourceItem(metalData, cell.worldPosition);
            if (resourceItem != null)
            {
                // Занимаем клетку
                gridManager.OccupyCell(cell.gridPosition, resourceItem, "Resource");

                // Регистрируем
                spawnedResources.Add(resourceItem);
                successfulSpawns++;
            }
        }


    }

    /// <summary>
    /// Создать предмет-ресурс в мире
    /// </summary>
    GameObject CreateResourceItem(ResourceData resourceData, Vector3 position)
    {
        GameObject resourceItem;

        // Если у ресурса есть префаб, используем его
        if (resourceData.prefab != null)
        {
            resourceItem = Instantiate(resourceData.prefab, position, Quaternion.identity, resourceParent);
        }
        else
        {
            // Создаем простой куб как fallback
            resourceItem = GameObject.CreatePrimitive(PrimitiveType.Cube);
            resourceItem.transform.position = position;
            resourceItem.transform.localScale = Vector3.one * resourceWorldSize;
            resourceItem.transform.SetParent(resourceParent);

            // Устанавливаем цвет ТОЛЬКО для fallback куба
            Renderer renderer = resourceItem.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Используем существующий материал и копируем его
                Material material = new Material(renderer.sharedMaterial);

                // Проверяем, есть ли у материала поддержка цвета
                if (material.HasProperty("_Color"))
                {
                    material.color = metalColor;
                }

                // Настраиваем металлический вид если шейдер поддерживает
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

        // Устанавливаем слой "Selectable" для взаимодействия
        // ВАЖНО: Устанавливаем рекурсивно для всех дочерних объектов!
        int selectableLayer = LayerMask.NameToLayer("Selectable");
        if (selectableLayer != -1)
        {
            SetLayerRecursively(resourceItem, selectableLayer);
        }
        else
        {
            Debug.LogWarning($"[WARNING] [ResourceSpawner] 'Selectable' layer not found! Resource may not be clickable.");
        }

        // Создаем ItemData из ResourceData
        ItemData itemData = CreateItemDataFromResource(resourceData);

        // Добавляем компонент Item
        Item itemComponent = resourceItem.GetComponent<Item>();
        if (itemComponent == null)
        {
            itemComponent = resourceItem.AddComponent<Item>();
        }

        // Настраиваем Item
        itemComponent.SetItemData(itemData);
        itemComponent.canBePickedUp = true;
        itemComponent.pickupRange = 1.5f; // ТОЛЬКО соседняя клетка!

        // Убеждаемся что есть коллайдер для взаимодействия
        Collider collider = resourceItem.GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true; // Триггер - игрок проходит сквозь
            // Размер коллайдера оставляем стандартным чтобы не блокировать NavMesh
        }
        else
        {
            Debug.LogWarning($"[ResourceSpawner] No collider found on resource item!");
        }

        // Убираем Rigidbody если есть (чтобы предмет не падал и не блокировал движение)
        Rigidbody rb = resourceItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Destroy(rb);
        }

        // Добавляем компонент LocationObjectInfo для консистентности
        LocationObjectInfo objectInfo = resourceItem.GetComponent<LocationObjectInfo>();
        if (objectInfo == null)
        {
            objectInfo = resourceItem.AddComponent<LocationObjectInfo>();
        }

        objectInfo.objectName = resourceData.resourceName;
        objectInfo.objectType = "Resource";
        objectInfo.health = 10f;
        objectInfo.canBeScavenged = true;

        // Добавляем медленное вращение для лучшей заметности
        RotateObject rotator = resourceItem.GetComponent<RotateObject>();
        if (rotator == null)
        {
            rotator = resourceItem.AddComponent<RotateObject>();
            rotator.rotationSpeed = 30f; // 30 градусов в секунду
            rotator.rotateY = true;
            rotator.enableBobbing = true; // Легкое движение вверх-вниз
            rotator.bobbingAmount = 0.05f; // Небольшая амплитуда
            rotator.bobbingSpeed = 2f;
        }

        return resourceItem;
    }

    /// <summary>
    /// Создать ItemData из ResourceData
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
    /// Очистить все спавненные ресурсы
    /// </summary>
    public void ClearSpawnedResources()
    {
        foreach (GameObject resource in spawnedResources)
        {
            if (resource != null)
            {
                // Освобождаем клетку в сетке
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
    /// Перегенерировать ресурсы
    /// </summary>
    [ContextMenu("Regenerate Resources")]
    public void RegenerateResources()
    {
        ClearSpawnedResources();
        SpawnMetalResources();
    }

    /// <summary>
    /// Получить количество спавненных ресурсов
    /// </summary>
    public int GetSpawnedResourceCount()
    {
        // Удаляем null ссылки (подобранные ресурсы)
        spawnedResources.RemoveAll(r => r == null);
        return spawnedResources.Count;
    }

    /// <summary>
    /// Установить слой рекурсивно для объекта и всех его дочерних объектов
    /// </summary>
    void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null)
            return;

        obj.layer = layer;

        // Рекурсивно устанавливаем слой для всех дочерних объектов
        foreach (Transform child in obj.transform)
        {
            if (child != null)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }
}
