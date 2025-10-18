using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor утилита для удаления менеджеров, которые теперь создаются автоматически через GameBootstrap
///
/// ИСПОЛЬЗОВАНИЕ:
/// 1. Откройте вашу главную сцену (Main.unity)
/// 2. Выберите в меню: Tools → Cleanup Auto-Created Managers
/// 3. Скрипт удалит все менеджеры, которые GameBootstrap создает автоматически
///
/// ВАЖНО: GameBootstrap должен остаться в сцене!
/// </summary>
public class CleanupAutoCreatedManagers : EditorWindow
{
    /// <summary>
    /// Автоматическое удаление менеджеров (вызывается из командной строки)
    /// </summary>
    public static void AutoCleanup()
    {
        // Открываем главную сцену
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");

        Debug.Log("[CleanupManagers] Starting automatic cleanup...");

        PerformCleanup(false);

        Debug.Log("[CleanupManagers] Automatic cleanup complete!");
    }

    [MenuItem("Tools/Cleanup Auto-Created Managers")]
    public static void CleanupManagers()
    {
        PerformCleanup(true);
    }

    private static void PerformCleanup(bool showDialogs)
    {
        if (showDialogs && !EditorUtility.DisplayDialog(
            "Cleanup Auto-Created Managers",
            "This will remove all managers that are now auto-created by GameBootstrap:\n\n" +
            "- GridManager\n" +
            "- SelectionManager\n" +
            "- CombatSystem\n" +
            "- ConstructionManager\n" +
            "- MiningManager\n" +
            "- EnemyTargetingSystem\n" +
            "- ShipBuildingSystem\n" +
            "- ResourcePanelUI\n" +
            "- InventoryUI\n" +
            "- EventSystem\n" +
            "- CameraController\n" +
            "- GamePauseManager\n\n" +
            "IMPORTANT: BuildMenuManager and ItemIconManager will NOT be removed\n" +
            "(they require Inspector configuration).\n\n" +
            "GameBootstrap will remain in the scene.\n\n" +
            "Continue?",
            "Yes, Clean Up",
            "Cancel"))
        {
            return;
        }

        int removedCount = 0;

        // Список менеджеров для удаления (все кроме GameBootstrap, BuildMenuManager и ItemIconManager)
        string[] managersToRemove = new string[]
        {
            "GridManager",
            "SelectionManager",
            "CombatSystem",
            "ConstructionManager",
            "MiningManager",
            "EnemyTargetingSystem",
            "ShipBuildingSystem",
            "ResourcePanelUI",
            "InventoryUI",
            "EventSystem",
            "CameraController",
            "GamePauseManager"
        };

        foreach (string managerName in managersToRemove)
        {
            GameObject obj = GameObject.Find(managerName);
            if (obj != null)
            {
                Debug.Log($"[CleanupManagers] Removing {managerName} from scene");
                DestroyImmediate(obj);
                removedCount++;
            }
        }

        // Проверяем что GameBootstrap остался
        GameObject bootstrap = GameObject.Find("GameBootstrap");
        if (bootstrap == null)
        {
            Debug.LogError("[CleanupManagers] ERROR: GameBootstrap not found in scene! Scene needs GameBootstrap to function!");
            if (showDialogs)
            {
                EditorUtility.DisplayDialog(
                    "Error!",
                    "GameBootstrap not found in scene!\n\n" +
                    "Please add a GameObject with GameBootstrap component before running this cleanup.",
                    "OK");
            }
            return;
        }

        // Сохраняем сцену
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log($"[CleanupManagers] Cleanup complete! Removed {removedCount} manager(s)");

        if (showDialogs)
        {
            EditorUtility.DisplayDialog(
                "Cleanup Complete!",
                $"Successfully removed {removedCount} auto-created manager(s).\n\n" +
                "GameBootstrap will create these systems automatically when the game starts.\n\n" +
                "Scene has been saved.",
                "OK");
        }
    }
}
