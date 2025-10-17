using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Диагностика системы ресурсов
/// </summary>
public class DiagnoseResourceSystem
{
    [MenuItem("Tools/Resources/Diagnose System")]
    public static void Diagnose()
    {
        Debug.Log("===== RESOURCE SYSTEM DIAGNOSTICS =====");

        bool allGood = true;

        // 1. Проверяем папку Resources
        if (!Directory.Exists("Assets/Resources"))
        {
            Debug.LogError("✗ Resources folder does NOT exist!");
            allGood = false;
        }
        else
        {
            Debug.Log("✓ Resources folder exists");
        }

        // 2. Проверяем ResourceManager
        ResourceManager rm = Resources.Load<ResourceManager>("ResourceManager");
        if (rm == null)
        {
            Debug.LogError("✗ ResourceManager NOT FOUND in Resources folder!");
            Debug.LogError("  Expected path: Assets/Resources/ResourceManager.asset");
            allGood = false;
        }
        else
        {
            Debug.Log($"✓ ResourceManager found with {rm.allResources.Count} resources");

            // Проверяем металл
            ResourceData metal = rm.GetResourceByName("Металл");
            if (metal == null)
            {
                Debug.LogWarning("⚠ Metal resource NOT found in ResourceManager!");
            }
            else
            {
                Debug.Log($"✓ Metal resource found: {metal.resourceName}");
            }
        }

        // 3. Проверяем SceneSetup
        SceneSetup sceneSetup = Object.FindObjectOfType<SceneSetup>();
        if (sceneSetup == null)
        {
            Debug.LogWarning("⚠ SceneSetup NOT FOUND - GridManager and LocationManager must exist in scene already");
        }
        else
        {
            Debug.Log($"✓ SceneSetup found: {sceneSetup.gameObject.name}");
        }

        // 4. Проверяем GridManager в сцене
        GridManager gm = Object.FindObjectOfType<GridManager>();

        if (gm == null)
        {
            if (sceneSetup != null)
            {
                Debug.LogWarning("⚠ GridManager NOT FOUND in Editor (will be created dynamically by SceneSetup at runtime)");
                Debug.Log("  This is OK - GridManager will be created when you press Play");
            }
            else
            {
                Debug.LogError("✗ GridManager NOT FOUND and no SceneSetup found!");
                Debug.LogError("  Make sure you opened the correct scene (Main.unity)");
                allGood = false;
            }
        }
        else
        {
            Debug.Log($"✓ GridManager found: {gm.gameObject.name}");
        }

        // 5. Проверяем LocationManager в сцене
        LocationManager lm = Object.FindObjectOfType<LocationManager>();
        if (lm == null)
        {
            Debug.LogWarning("⚠ LocationManager NOT FOUND in scene!");
            Debug.LogWarning("  You might be in the wrong scene. Open Main.unity");
        }
        else
        {
            Debug.Log($"✓ LocationManager found: {lm.gameObject.name}");
        }

        // 6. Проверяем ResourceSpawner в сцене
        ResourceSpawner rs = Object.FindObjectOfType<ResourceSpawner>();
        if (rs == null)
        {
            Debug.LogWarning("⚠ ResourceSpawner NOT FOUND in scene!");
            Debug.LogWarning("  Run 'Tools > Resources > Complete Setup' to add it");
        }
        else
        {
            Debug.Log($"✓ ResourceSpawner found: {rs.gameObject.name}");

            // Проверяем поля ResourceSpawner
            if (rs.resourceManager == null)
            {
                Debug.LogError("  ✗ ResourceSpawner.resourceManager is NULL!");
                allGood = false;
            }
            else
            {
                Debug.Log("  ✓ ResourceSpawner.resourceManager assigned");
            }

            if (rs.gridManager == null)
            {
                if (sceneSetup != null)
                {
                    Debug.LogWarning("  ⚠ ResourceSpawner.gridManager is NULL (will be found at runtime)");
                    Debug.Log("    This is OK - GridManager will be found after SceneSetup creates it");
                }
                else
                {
                    Debug.LogError("  ✗ ResourceSpawner.gridManager is NULL and no SceneSetup!");
                    allGood = false;
                }
            }
            else
            {
                Debug.Log("  ✓ ResourceSpawner.gridManager assigned");
            }

            if (rs.resourceParent == null)
            {
                Debug.LogWarning("  ⚠ ResourceSpawner.resourceParent is NULL (will be auto-created)");
            }
            else
            {
                Debug.Log("  ✓ ResourceSpawner.resourceParent assigned");
            }

            Debug.Log($"  • autoSpawnOnStart: {rs.autoSpawnOnStart}");
            Debug.Log($"  • minMetalSpawns: {rs.minMetalSpawns}");
            Debug.Log($"  • maxMetalSpawns: {rs.maxMetalSpawns}");
        }

        // 7. Проверяем ResourcePanelUI (опционально)
        ResourcePanelUI rp = Object.FindObjectOfType<ResourcePanelUI>();
        if (rp != null)
        {
            if (rp.resourceManager == null)
            {
                Debug.LogWarning("⚠ ResourcePanelUI.resourceManager is NULL");
            }
            else
            {
                Debug.Log("✓ ResourcePanelUI configured");
            }
        }

        Debug.Log("====================================");

        if (allGood)
        {
            Debug.Log("<color=green><b>✓ ALL SYSTEMS READY!</b></color>");
            Debug.Log("You can now press Play to spawn resources.");
        }
        else
        {
            Debug.LogError("<color=red><b>✗ ISSUES DETECTED!</b></color>");
            Debug.LogError("Run 'Tools > Resources > Complete Setup' to fix issues");
        }

        // Показываем диалог с результатами
        string message = allGood
            ? "All systems are ready!\n\nPress Play to see resources spawn."
            : "Issues detected!\n\nRun 'Tools > Resources > Complete Setup' to fix them.\n\nCheck Console for details.";

        EditorUtility.DisplayDialog(
            allGood ? "Diagnostics: OK" : "Diagnostics: Issues Found",
            message,
            "OK"
        );
    }
}
