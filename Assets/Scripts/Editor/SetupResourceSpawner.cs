using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor скрипт для автоматической настройки ResourceSpawner в сцене
/// </summary>
public class SetupResourceSpawner
{
    [MenuItem("Tools/Resources/Setup Resource Spawner in Scene")]
    public static void SetupSpawnerInScene()
    {
        // Ищем LocationManager
        LocationManager locationManager = Object.FindObjectOfType<LocationManager>();
        if (locationManager == null)
        {
            EditorUtility.DisplayDialog("Error",
                "LocationManager not found in scene! Please open the Main scene first.",
                "OK");
            return;
        }

        // Проверяем, есть ли уже ResourceSpawner
        ResourceSpawner existingSpawner = Object.FindObjectOfType<ResourceSpawner>();
        if (existingSpawner != null)
        {
            Debug.LogWarning("ResourceSpawner already exists in the scene!");
            Selection.activeGameObject = existingSpawner.gameObject;
            return;
        }

        // Создаем новый GameObject для ResourceSpawner
        GameObject spawnerObj = new GameObject("ResourceSpawner");
        spawnerObj.transform.SetParent(locationManager.transform);
        spawnerObj.transform.localPosition = Vector3.zero;

        // Добавляем компонент ResourceSpawner
        ResourceSpawner spawner = spawnerObj.AddComponent<ResourceSpawner>();

        // Загружаем ResourceManager
        ResourceManager resourceManager = Resources.Load<ResourceManager>("ResourceManager");
        if (resourceManager != null)
        {
            spawner.resourceManager = resourceManager;
            Debug.Log("ResourceManager assigned to ResourceSpawner");
        }
        else
        {
            Debug.LogWarning("ResourceManager not found! Create it via Tools/Resources/Create Resource Manager");
        }

        // Находим GridManager
        GridManager gridManager = Object.FindObjectOfType<GridManager>();
        if (gridManager != null)
        {
            spawner.gridManager = gridManager;
            Debug.Log("GridManager assigned to ResourceSpawner");
        }
        else
        {
            Debug.LogWarning("GridManager not found in scene!");
        }

        // Настраиваем параметры спавна
        spawner.minMetalSpawns = 15;
        spawner.maxMetalSpawns = 30;
        spawner.resourceWorldSize = 0.3f;
        spawner.autoSpawnOnStart = true;
        spawner.metalColor = new Color(0.7f, 0.7f, 0.8f);

        // Создаем родительский объект для ресурсов
        GameObject resourceParent = new GameObject("SpawnedResources");
        resourceParent.transform.SetParent(spawnerObj.transform);
        spawner.resourceParent = resourceParent.transform;

        // Выделяем созданный объект
        Selection.activeGameObject = spawnerObj;

        // Сохраняем изменения в сцене
        EditorUtility.SetDirty(spawnerObj);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("ResourceSpawner successfully created and configured!");
        Debug.Log("Make sure to create ResourceManager asset via Tools/Resources/Create Resource Manager");

        EditorUtility.DisplayDialog("Success",
            "ResourceSpawner has been added to the scene!\n\n" +
            "Next steps:\n" +
            "1. Create ResourceManager via Tools/Resources/Create Resource Manager\n" +
            "2. Create Metal resource via Tools/Resources/Create Metal Resource\n" +
            "3. Play the scene to spawn resources",
            "OK");
    }

    [MenuItem("Tools/Resources/Test Resource Spawn")]
    public static void TestResourceSpawn()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Error",
                "This function can only be used in Play Mode!",
                "OK");
            return;
        }

        ResourceSpawner spawner = Object.FindObjectOfType<ResourceSpawner>();
        if (spawner == null)
        {
            EditorUtility.DisplayDialog("Error",
                "ResourceSpawner not found in scene!\n" +
                "Use Tools/Resources/Setup Resource Spawner in Scene first.",
                "OK");
            return;
        }

        spawner.ClearSpawnedResources();
        spawner.SpawnMetalResources();

        Debug.Log($"Resource spawn test completed. Check the scene for spawned resources.");
    }
}
