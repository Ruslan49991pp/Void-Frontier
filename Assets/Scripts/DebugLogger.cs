using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Централизованная система отладки для всех компонентов игры
/// </summary>
public static class DebugLogger
{
    public enum LogCategory
    {
        General,
        Selection,
        Movement,
        Targeting,
        AI,
        UI,
        Spawning,
        GameInit,
        Icons
    }

    // Настройки логирования
    private static Dictionary<LogCategory, bool> logEnabled = new Dictionary<LogCategory, bool>
    {
        { LogCategory.General, false },
        { LogCategory.Selection, false },
        { LogCategory.Movement, false },
        { LogCategory.Targeting, false },
        { LogCategory.AI, false },
        { LogCategory.UI, false },
        { LogCategory.Spawning, false },
        { LogCategory.GameInit, false },
        { LogCategory.Icons, false }
    };

    private static Dictionary<LogCategory, Color> logColors = new Dictionary<LogCategory, Color>
    {
        { LogCategory.General, Color.white },
        { LogCategory.Selection, Color.yellow },
        { LogCategory.Movement, Color.green },
        { LogCategory.Targeting, Color.red },
        { LogCategory.AI, Color.blue },
        { LogCategory.UI, Color.magenta },
        { LogCategory.Spawning, Color.cyan },
        { LogCategory.GameInit, new Color(1f, 0.5f, 0f, 1f) }, // Orange color
        { LogCategory.Icons, Color.gray }
    };

    /// <summary>
    /// Основной метод логирования
    /// </summary>
    public static void Log(LogCategory category, string message, UnityEngine.Object context = null)
    {
        if (!logEnabled.GetValueOrDefault(category, true))
            return;

        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string formattedMessage = $"[{timestamp}] [{category}] {message}";

        // Используем цветное логирование в редакторе
#if UNITY_EDITOR
        string colorHex = ColorUtility.ToHtmlStringRGBA(logColors.GetValueOrDefault(category, Color.white));
        string coloredMessage = $"<color=#{colorHex}>{formattedMessage}</color>";

#else

#endif

        // Дублируем в FileLogger если он доступен
        try
        {
            FileLogger.Log($"DEBUG: {formattedMessage}");
        }
        catch
        {
            // FileLogger может быть недоступен
        }
    }

    /// <summary>
    /// Логирование ошибок
    /// </summary>
    public static void LogError(LogCategory category, string message, UnityEngine.Object context = null)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string formattedMessage = $"[{timestamp}] [ERROR-{category}] {message}";



        try
        {
            FileLogger.Log($"ERROR: {formattedMessage}");
        }
        catch { }
    }

    /// <summary>
    /// Логирование предупреждений
    /// </summary>
    public static void LogWarning(LogCategory category, string message, UnityEngine.Object context = null)
    {
        if (!logEnabled.GetValueOrDefault(category, true))
            return;

        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string formattedMessage = $"[{timestamp}] [WARNING-{category}] {message}";



        try
        {
            FileLogger.Log($"WARNING: {formattedMessage}");
        }
        catch { }
    }

    /// <summary>
    /// Включить/выключить логирование для категории
    /// </summary>
    public static void SetCategoryEnabled(LogCategory category, bool enabled)
    {
        logEnabled[category] = enabled;
        Log(LogCategory.General, $"Logging for {category} {(enabled ? "ENABLED" : "DISABLED")}");
    }

    /// <summary>
    /// Получить состояние логирования для категории
    /// </summary>
    public static bool IsCategoryEnabled(LogCategory category)
    {
        return logEnabled.GetValueOrDefault(category, true);
    }

    /// <summary>
    /// Логирование состояния компонента
    /// </summary>
    public static void LogComponentState(LogCategory category, string componentName, MonoBehaviour component)
    {
        if (component == null)
        {
            LogError(category, $"{componentName} is NULL!");
            return;
        }

        string state = $"{componentName} State: " +
                      $"GameObject={component.gameObject.name}, " +
                      $"Enabled={component.enabled}, " +
                      $"ActiveInHierarchy={component.gameObject.activeInHierarchy}, " +
                      $"Position={component.transform.position}";

        Log(category, state, component);
    }

    /// <summary>
    /// Логирование списка объектов
    /// </summary>
    public static void LogObjectList<T>(LogCategory category, string listName, IList<T> objects) where T : UnityEngine.Object
    {
        Log(category, $"{listName} Count: {objects?.Count ?? 0}");

        if (objects != null)
        {
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i] != null)
                {
                    Log(category, $"  [{i}] {objects[i].name}", objects[i]);
                }
                else
                {
                    LogWarning(category, $"  [{i}] NULL object in {listName}");
                }
            }
        }
    }

    /// <summary>
    /// Логирование системных требований
    /// </summary>
    public static void LogSystemInfo()
    {
        Log(LogCategory.General, "=== SYSTEM INFO ===");
        Log(LogCategory.General, $"Unity Version: {Application.unityVersion}");
        Log(LogCategory.General, $"Platform: {Application.platform}");
        Log(LogCategory.General, $"Data Path: {Application.dataPath}");
        Log(LogCategory.General, $"Screen: {Screen.width}x{Screen.height}");
        Log(LogCategory.General, $"Target Frame Rate: {Application.targetFrameRate}");
        Log(LogCategory.General, "=== END SYSTEM INFO ===");
    }

    /// <summary>
    /// Показать все доступные категории логирования
    /// </summary>
    public static void LogAvailableCategories()
    {
        Log(LogCategory.General, "=== LOGGING CATEGORIES ===");
        foreach (var kvp in logEnabled)
        {
            string status = kvp.Value ? "ENABLED" : "DISABLED";
            Log(LogCategory.General, $"{kvp.Key}: {status}");
        }
        Log(LogCategory.General, "=== END CATEGORIES ===");
    }
}