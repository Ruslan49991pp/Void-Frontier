using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Менеджер ресурсов - база данных всех ресурсов в игре
/// Создать через Assets -> Create -> Resources/Resource Manager
/// </summary>
[CreateAssetMenu(fileName = "ResourceManager", menuName = "Resources/Resource Manager")]
public class ResourceManager : ScriptableObject
{
    [Header("База данных ресурсов")]
    [Tooltip("Список всех ресурсов в игре")]
    public List<ResourceData> allResources = new List<ResourceData>();

    /// <summary>
    /// Получить ресурс по названию
    /// </summary>
    public ResourceData GetResourceByName(string resourceName)
    {
        if (string.IsNullOrEmpty(resourceName))
            return null;

        return allResources.FirstOrDefault(r =>
            r != null && r.resourceName.Equals(resourceName, System.StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Получить все ресурсы определенной категории
    /// </summary>
    public List<ResourceData> GetResourcesByCategory(ResourceCategory category)
    {
        return allResources.Where(r => r != null && r.category == category).ToList();
    }

    /// <summary>
    /// Получить иконку ресурса по названию
    /// </summary>
    public Sprite GetResourceIcon(string resourceName)
    {
        ResourceData resource = GetResourceByName(resourceName);
        return resource != null ? resource.icon : null;
    }

    /// <summary>
    /// Получить префаб ресурса по названию
    /// </summary>
    public GameObject GetResourcePrefab(string resourceName)
    {
        ResourceData resource = GetResourceByName(resourceName);
        return resource != null ? resource.prefab : null;
    }

    /// <summary>
    /// Проверить, существует ли ресурс
    /// </summary>
    public bool ResourceExists(string resourceName)
    {
        return GetResourceByName(resourceName) != null;
    }

    /// <summary>
    /// Получить общее количество ресурсов
    /// </summary>
    public int GetTotalResourceCount()
    {
        return allResources.Count(r => r != null);
    }

    /// <summary>
    /// Получить все названия ресурсов
    /// </summary>
    public List<string> GetAllResourceNames()
    {
        return allResources.Where(r => r != null).Select(r => r.resourceName).ToList();
    }

    /// <summary>
    /// Добавить ресурс в базу данных (для использования в Editor)
    /// </summary>
    public void AddResource(ResourceData resource)
    {
        if (resource == null)
            return;

        // Проверяем, нет ли уже такого ресурса
        if (!allResources.Contains(resource) && !ResourceExists(resource.resourceName))
        {
            allResources.Add(resource);
        }
        else
        {
            Debug.LogWarning($"Ресурс '{resource.resourceName}' уже существует в базе данных!");
        }
    }

    /// <summary>
    /// Удалить ресурс из базы данных (для использования в Editor)
    /// </summary>
    public void RemoveResource(ResourceData resource)
    {
        if (resource == null)
            return;

        allResources.Remove(resource);
    }

    /// <summary>
    /// Получить статистику по категориям
    /// </summary>
    public string GetCategoryStats()
    {
        var stats = "Статистика ресурсов:\n";
        foreach (ResourceCategory category in System.Enum.GetValues(typeof(ResourceCategory)))
        {
            int count = GetResourcesByCategory(category).Count;
            stats += $"  {category}: {count}\n";
        }
        stats += $"Всего ресурсов: {GetTotalResourceCount()}";
        return stats;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Очистить null элементы из списка
    /// </summary>
    [ContextMenu("Очистить NULL элементы")]
    public void RemoveNullEntries()
    {
        int beforeCount = allResources.Count;
        allResources.RemoveAll(r => r == null);
        int afterCount = allResources.Count;
        int removed = beforeCount - afterCount;

        if (removed > 0)
        {
            Debug.Log($"Удалено {removed} null элементов из базы данных");
            UnityEditor.EditorUtility.SetDirty(this);
        }
        else
        {
            Debug.Log("NULL элементы не найдены");
        }
    }

    /// <summary>
    /// Проверить целостность базы данных
    /// </summary>
    [ContextMenu("Проверить целостность")]
    public void ValidateDatabase()
    {
        int errors = 0;
        int nullCount = 0;

        foreach (var resource in allResources)
        {
            if (resource == null)
            {
                Debug.LogError("Найден null ресурс в базе данных!");
                nullCount++;
                errors++;
                continue;
            }

            if (string.IsNullOrEmpty(resource.resourceName))
            {
                Debug.LogError($"Ресурс без имени: {resource.name}");
                errors++;
            }

            if (resource.icon == null)
            {
                Debug.LogWarning($"У ресурса '{resource.resourceName}' отсутствует иконка!");
            }

            if (resource.prefab == null)
            {
                Debug.LogWarning($"У ресурса '{resource.resourceName}' отсутствует префаб!");
            }
        }

        if (nullCount > 0)
        {
            Debug.LogError($"Найдено {nullCount} null элементов! Используйте 'Очистить NULL элементы' для их удаления.");
        }

        if (errors == 0)
        {
            Debug.Log($"База данных ресурсов валидна! Всего ресурсов: {GetTotalResourceCount()}");
        }
        else
        {
            Debug.LogError($"Найдено ошибок: {errors}");
        }
    }

    /// <summary>
    /// Вывести статистику
    /// </summary>
    [ContextMenu("Показать статистику")]
    public void ShowStats()
    {
        Debug.Log(GetCategoryStats());
    }
#endif
}
