using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

/// <summary>
/// Полная автоматическая настройка системы ресурсов
/// </summary>
public class CompleteResourceSystemSetup
{
    private const string RESOURCES_FOLDER = "Assets/Resources";
    private const string RESOURCE_MANAGER_PATH = "Assets/Resources/ResourceManager.asset";
    private const string METAL_RESOURCE_PATH = "Assets/Resources/MetalResource.asset";

    [MenuItem("Tools/Resources/Complete Setup (All in One)")]
    public static void CompleteSetup()
    {
        Debug.Log("===== Starting Complete Resource System Setup =====");

        // Шаг 1: Создаем папку Resources
        if (!Directory.Exists(RESOURCES_FOLDER))
        {
            Directory.CreateDirectory(RESOURCES_FOLDER);
            AssetDatabase.Refresh();
            Debug.Log("✓ Created Resources folder");
        }

        // Шаг 2: Создаем ResourceManager
        ResourceManager resourceManager = AssetDatabase.LoadAssetAtPath<ResourceManager>(RESOURCE_MANAGER_PATH);
        if (resourceManager == null)
        {
            resourceManager = ScriptableObject.CreateInstance<ResourceManager>();
            AssetDatabase.CreateAsset(resourceManager, RESOURCE_MANAGER_PATH);
            Debug.Log("✓ Created ResourceManager");
        }
        else
        {
            Debug.Log("✓ ResourceManager already exists");
            // Очищаем null элементы
            resourceManager.RemoveNullEntries();
        }

        // Шаг 3: Создаем ресурс Металл
        ResourceData metalResource = AssetDatabase.LoadAssetAtPath<ResourceData>(METAL_RESOURCE_PATH);
        if (metalResource == null)
        {
            metalResource = ScriptableObject.CreateInstance<ResourceData>();
            metalResource.resourceName = "Металл";
            metalResource.description = "Базовый материал для строительства";
            metalResource.category = ResourceCategory.Raw;
            metalResource.maxStackSize = 999;
            metalResource.weightPerUnit = 0.5f;
            metalResource.valuePerUnit = 10;

            AssetDatabase.CreateAsset(metalResource, METAL_RESOURCE_PATH);
            Debug.Log("✓ Created Metal resource");
        }
        else
        {
            Debug.Log("✓ Metal resource already exists");
        }

        // Шаг 4: Добавляем Metal в ResourceManager
        if (!resourceManager.allResources.Contains(metalResource))
        {
            resourceManager.allResources.Add(metalResource);
            EditorUtility.SetDirty(resourceManager);
            Debug.Log("✓ Added Metal to ResourceManager");
        }
        else
        {
            Debug.Log("✓ Metal already in ResourceManager");
        }

        // Шаг 5: Настраиваем ResourceSpawner в сцене
        LocationManager locationManager = Object.FindObjectOfType<LocationManager>();
        if (locationManager == null)
        {
            Debug.LogWarning("⚠ LocationManager not found. Please open the Main scene.");
            EditorUtility.DisplayDialog("Warning",
                "LocationManager not found in current scene.\n" +
                "Please open the Main scene and run this setup again.",
                "OK");
        }
        else
        {
            // Проверяем, есть ли уже ResourceSpawner
            ResourceSpawner existingSpawner = Object.FindObjectOfType<ResourceSpawner>();
            if (existingSpawner == null)
            {
                // Создаем новый GameObject для ResourceSpawner
                GameObject spawnerObj = new GameObject("ResourceSpawner");
                spawnerObj.transform.SetParent(locationManager.transform);
                spawnerObj.transform.localPosition = Vector3.zero;

                // Добавляем компонент ResourceSpawner
                ResourceSpawner spawner = spawnerObj.AddComponent<ResourceSpawner>();

                // Настраиваем ссылки
                spawner.resourceManager = resourceManager;

                GridManager gridManager = Object.FindObjectOfType<GridManager>();
                if (gridManager != null)
                {
                    spawner.gridManager = gridManager;
                    Debug.Log("✓ GridManager assigned");
                }
                else
                {
                    Debug.LogWarning("⚠ GridManager not found!");
                }

                // Настраиваем параметры
                spawner.minMetalSpawns = 15;
                spawner.maxMetalSpawns = 30;
                spawner.resourceWorldSize = 0.3f;
                spawner.autoSpawnOnStart = true;
                spawner.metalColor = new Color(0.7f, 0.7f, 0.8f);

                // Создаем родительский объект для ресурсов
                GameObject resourceParent = new GameObject("SpawnedResources");
                resourceParent.transform.SetParent(spawnerObj.transform);
                spawner.resourceParent = resourceParent.transform;

                EditorUtility.SetDirty(spawnerObj);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

                Debug.Log("✓ Created and configured ResourceSpawner");
                Selection.activeGameObject = spawnerObj;
            }
            else
            {
                // Обновляем существующий spawner
                existingSpawner.resourceManager = resourceManager;

                if (existingSpawner.gridManager == null)
                {
                    GridManager gridManager = Object.FindObjectOfType<GridManager>();
                    if (gridManager != null)
                    {
                        existingSpawner.gridManager = gridManager;
                    }
                }

                EditorUtility.SetDirty(existingSpawner.gameObject);
                Debug.Log("✓ Updated existing ResourceSpawner");
                Selection.activeGameObject = existingSpawner.gameObject;
            }
        }

        // Шаг 6: Настраиваем ResourcePanelUI
        ResourcePanelUI resourcePanel = Object.FindObjectOfType<ResourcePanelUI>();
        if (resourcePanel != null)
        {
            resourcePanel.resourceManager = resourceManager;
            EditorUtility.SetDirty(resourcePanel.gameObject);
            Debug.Log("✓ Updated ResourcePanelUI");
        }
        else
        {
            Debug.Log("ℹ ResourcePanelUI not found (optional)");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("===== Resource System Setup Complete! =====");
        Debug.Log("Now you can play the scene to see metal resources spawn on the location.");

        EditorUtility.DisplayDialog("Setup Complete!",
            "Resource system has been fully configured!\n\n" +
            "✓ ResourceManager created\n" +
            "✓ Metal resource created\n" +
            "✓ ResourceSpawner configured\n\n" +
            "Press Play to see resources spawn on the location.",
            "OK");
    }

    [MenuItem("Tools/Resources/Force Respawn Resources (Play Mode)")]
    public static void ForceRespawnResources()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Error",
                "This can only be used in Play Mode!\n" +
                "Start the game first, then use this to respawn resources.",
                "OK");
            return;
        }

        ResourceSpawner spawner = Object.FindObjectOfType<ResourceSpawner>();
        if (spawner == null)
        {
            Debug.LogError("ResourceSpawner not found! Run Complete Setup first.");
            return;
        }

        Debug.Log("Clearing existing resources...");
        spawner.ClearSpawnedResources();

        Debug.Log("Spawning metal resources...");
        spawner.SpawnMetalResources();

        int count = spawner.GetSpawnedResourceCount();
        Debug.Log($"✓ Spawned {count} metal resources");

        EditorUtility.DisplayDialog("Done",
            $"Spawned {count} metal resources!\n" +
            "Check the scene view to see them.",
            "OK");
    }
}
