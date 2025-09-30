using UnityEngine;
using UnityEditor;

/// <summary>
/// Скрипт для автоматической сборки и тестирования проекта
/// </summary>
public class BuildScript
{
    /// <summary>
    /// Метод для проверки компиляции проекта без создания билда
    /// </summary>
    [MenuItem("Build/Compile Only")]
    public static void CompileOnly()
    {
        Debug.Log("[BuildScript] Starting compilation test...");

        // Принудительно обновляем проект
        AssetDatabase.Refresh();

        // Проверяем наличие ошибок компиляции
        if (EditorUtility.scriptCompilationFailed)
        {
            Debug.LogError("[BuildScript] Compilation failed! Check console for errors.");
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("[BuildScript] Compilation successful!");

        // Проверяем, что все наши новые скрипты существуют
        CheckWeaponSystemScripts();

        Debug.Log("[BuildScript] All weapon system scripts verified successfully!");

        // В batch mode автоматически выходим
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(0);
        }
    }

    /// <summary>
    /// Проверить наличие всех скриптов системы оружия
    /// </summary>
    private static void CheckWeaponSystemScripts()
    {
        string[] requiredScripts = {
            "Assets/Scripts/Weapon.cs",
            "Assets/Scripts/MeleeWeapon.cs",
            "Assets/Scripts/RangedWeapon.cs",
            "Assets/Scripts/Bullet.cs",
            "Assets/Scripts/WeaponSystem.cs",
            "Assets/Scripts/WeaponSystemTest.cs"
        };

        foreach (string scriptPath in requiredScripts)
        {
            if (AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath) == null)
            {
                Debug.LogError($"[BuildScript] Required script not found: {scriptPath}");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
                return;
            }
            else
            {
                Debug.Log($"[BuildScript] ✓ Found: {scriptPath}");
            }
        }
    }

    /// <summary>
    /// Создать тестовую сборку
    /// </summary>
    [MenuItem("Build/Build and Run")]
    public static void BuildAndRun()
    {
        Debug.Log("[BuildScript] Starting build and run...");

        // Сначала проверяем компиляцию
        CompileOnly();

        // Настройки сборки
        BuildPlayerOptions buildOptions = new BuildPlayerOptions();
        buildOptions.scenes = new[] { "Assets/Scenes/SampleScene.unity" };
        buildOptions.locationPathName = "Builds/VoidFrontier.exe";
        buildOptions.target = BuildTarget.StandaloneWindows64;
        buildOptions.options = BuildOptions.AutoRunPlayer;

        // Создаем сборку
        var report = BuildPipeline.BuildPlayer(buildOptions);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("[BuildScript] Build succeeded!");
        }
        else
        {
            Debug.LogError("[BuildScript] Build failed!");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }
    }

    /// <summary>
    /// Запустить тесты системы оружия
    /// </summary>
    [MenuItem("Tools/Test Weapon System")]
    public static void TestWeaponSystem()
    {
        Debug.Log("[BuildScript] Running weapon system tests...");

        // Ищем тестовый объект
        WeaponSystemTest tester = Object.FindObjectOfType<WeaponSystemTest>();

        if (tester == null)
        {
            Debug.LogWarning("[BuildScript] WeaponSystemTest not found in scene. Creating one...");

            GameObject testObject = new GameObject("WeaponSystemTester");
            tester = testObject.AddComponent<WeaponSystemTest>();
        }

        // Запускаем тест
        tester.RunWeaponSystemTest();

        Debug.Log("[BuildScript] Weapon system tests completed!");
    }

    /// <summary>
    /// Очистить логи
    /// </summary>
    [MenuItem("Tools/Clear Console")]
    public static void ClearConsole()
    {
        var assembly = System.Reflection.Assembly.GetAssembly(typeof(SceneView));
        var type = assembly.GetType("UnityEditor.LogEntries");
        var method = type.GetMethod("Clear");
        method.Invoke(new object(), null);
    }
}