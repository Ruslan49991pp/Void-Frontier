using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor скрипт для создания базовых ресурсов и менеджера
/// </summary>
public class CreateResourcesEditor
{
    private const string RESOURCES_FOLDER = "Assets/Resources/GameResources";
    private const string MANAGER_PATH = "Assets/Resources/ResourceManager.asset";

    /// <summary>
    /// Создать базовый ресурс "Металл"
    /// </summary>
    [MenuItem("Tools/Resources/Create Metal Resource")]
    public static void CreateMetalResource()
    {
        // Создаем папку, если её нет
        if (!Directory.Exists(RESOURCES_FOLDER))
        {
            Directory.CreateDirectory(RESOURCES_FOLDER);
            AssetDatabase.Refresh();
        }

        // Создаем ScriptableObject для металла
        ResourceData metal = ScriptableObject.CreateInstance<ResourceData>();
        metal.resourceName = "Металл";
        metal.description = "Базовый строительный материал. Используется для создания корпуса корабля, стен и различных конструкций.";
        metal.category = ResourceCategory.Raw;
        metal.maxStackSize = 999;
        metal.weightPerUnit = 0.5f;
        metal.valuePerUnit = 5;

        // Сохраняем asset
        string assetPath = Path.Combine(RESOURCES_FOLDER, "Metal.asset");
        AssetDatabase.CreateAsset(metal, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"Создан ресурс 'Металл' по пути: {assetPath}");
        Debug.Log("Не забудьте назначить иконку и префаб в инспекторе!");

        // Выделяем созданный ресурс
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = metal;

        // Пытаемся добавить в менеджер
        AddResourceToManager(metal);
    }

    /// <summary>
    /// Создать менеджер ресурсов
    /// </summary>
    [MenuItem("Tools/Resources/Create Resource Manager")]
    public static void CreateResourceManager()
    {
        // Проверяем, существует ли уже менеджер
        ResourceManager existingManager = AssetDatabase.LoadAssetAtPath<ResourceManager>(MANAGER_PATH);
        if (existingManager != null)
        {
            Debug.LogWarning("ResourceManager уже существует!");
            Selection.activeObject = existingManager;
            return;
        }

        // Создаем папку Resources, если её нет
        if (!Directory.Exists("Assets/Resources"))
        {
            Directory.CreateDirectory("Assets/Resources");
            AssetDatabase.Refresh();
        }

        // Создаем менеджер
        ResourceManager manager = ScriptableObject.CreateInstance<ResourceManager>();

        // Сохраняем asset
        AssetDatabase.CreateAsset(manager, MANAGER_PATH);
        AssetDatabase.SaveAssets();

        Debug.Log($"Создан ResourceManager по пути: {MANAGER_PATH}");

        // Выделяем созданный менеджер
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = manager;
    }

    /// <summary>
    /// Создать всё сразу: менеджер и базовый ресурс
    /// </summary>
    [MenuItem("Tools/Resources/Setup Resource System")]
    public static void SetupResourceSystem()
    {
        // Создаем менеджер
        CreateResourceManager();

        // Создаем металл
        CreateMetalResource();

        Debug.Log("Система ресурсов настроена! Не забудьте назначить иконку и префаб для металла.");
    }

    /// <summary>
    /// Добавить ресурс в менеджер
    /// </summary>
    private static void AddResourceToManager(ResourceData resource)
    {
        ResourceManager manager = AssetDatabase.LoadAssetAtPath<ResourceManager>(MANAGER_PATH);
        if (manager != null)
        {
            manager.AddResource(resource);
            EditorUtility.SetDirty(manager);
            AssetDatabase.SaveAssets();
            Debug.Log($"Ресурс '{resource.resourceName}' добавлен в ResourceManager");
        }
        else
        {
            Debug.LogWarning("ResourceManager не найден. Создайте его через Tools/Resources/Create Resource Manager");
        }
    }
}
