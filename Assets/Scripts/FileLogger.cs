using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Система логирования в файл с автоматической очисткой при запуске игры
/// </summary>
public class FileLogger : MonoBehaviour
{
    [Header("Logger Settings")]
    public bool enableFileLogging = true;
    public string logFileName = "game_log.txt";
    public bool includeStackTrace = false;
    public bool includeTimestamp = true;

    private static FileLogger instance;
    private string logFilePath;
    private bool isInitialized = false;

    // Singleton pattern
    public static FileLogger Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject loggerObj = new GameObject("FileLogger");
                instance = loggerObj.AddComponent<FileLogger>();
                DontDestroyOnLoad(loggerObj);
            }
            return instance;
        }
    }

    void Awake()
    {
        // Singleton pattern
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeLogger();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Инициализация системы логирования
    /// </summary>
    void InitializeLogger()
    {
        if (!enableFileLogging) return;

        // Определяем путь к файлу логов
        logFilePath = Path.Combine(Application.persistentDataPath, logFileName);

        // Очищаем старые логи при запуске новой игры
        ClearOldLogs();

        // Подписываемся на события Unity логов
        Application.logMessageReceived += OnLogMessageReceived;

        isInitialized = true;

        // Записываем первое сообщение о запуске
        LogToFile($"=== GAME SESSION STARTED ===", LogType.Log);
        LogToFile($"Session Start Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", LogType.Log);
        LogToFile($"Unity Version: {Application.unityVersion}", LogType.Log);
        LogToFile($"Platform: {Application.platform}", LogType.Log);
        LogToFile($"Log File Path: {logFilePath}", LogType.Log);
        LogToFile("================================", LogType.Log);
    }

    /// <summary>
    /// Очистка старых логов
    /// </summary>
    void ClearOldLogs()
    {
        try
        {
            if (File.Exists(logFilePath))
            {
                File.Delete(logFilePath);
            }
        }
        catch (Exception)
        {
            // Silently ignore file deletion errors
        }
    }

    /// <summary>
    /// Обработчик Unity логов
    /// </summary>
    void OnLogMessageReceived(string logString, string stackTrace, LogType type)
    {
        if (!enableFileLogging || !isInitialized) return;

        LogToFile(logString, type, stackTrace);
    }

    /// <summary>
    /// Запись сообщения в файл
    /// </summary>
    void LogToFile(string message, LogType type, string stackTrace = "")
    {
        if (!enableFileLogging || string.IsNullOrEmpty(logFilePath)) return;

        try
        {
            string timestamp = includeTimestamp ? $"[{DateTime.Now:HH:mm:ss.fff}] " : "";
            string logLevel = $"[{type.ToString().ToUpper()}] ";
            string finalMessage = $"{timestamp}{logLevel}{message}";

            // Добавляем stack trace если нужно и он есть
            if (includeStackTrace && !string.IsNullOrEmpty(stackTrace))
            {
                finalMessage += $"\nStack Trace:\n{stackTrace}";
            }

            finalMessage += "\n";

            // Записываем в файл (append mode)
            File.AppendAllText(logFilePath, finalMessage);
        }
        catch (Exception)
        {
            // Silently ignore file writing errors
        }
    }

    /// <summary>
    /// Публичный метод для записи кастомных логов
    /// </summary>
    public static void Log(string message)
    {
        Instance.LogToFile(message, LogType.Log);
    }

    /// <summary>
    /// Публичный метод для записи предупреждений
    /// </summary>
    public static void LogWarning(string message)
    {
        Instance.LogToFile(message, LogType.Warning);
    }

    /// <summary>
    /// Публичный метод для записи ошибок
    /// </summary>
    public static void LogError(string message)
    {
        Instance.LogToFile(message, LogType.Error);
    }

    /// <summary>
    /// Получить путь к файлу логов
    /// </summary>
    public static string GetLogFilePath()
    {
        return Instance.logFilePath;
    }

    /// <summary>
    /// Включить/выключить логирование
    /// </summary>
    public static void SetLoggingEnabled(bool enabled)
    {
        Instance.enableFileLogging = enabled;
    }

    /// <summary>
    /// Автоматическое создание FileLogger при запуске игры
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoInitialize()
    {
        // Создаем FileLogger автоматически при запуске игры
        var logger = Instance;
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (enableFileLogging && isInitialized)
        {
            LogToFile($"Application Pause: {pauseStatus}", LogType.Log);
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (enableFileLogging && isInitialized)
        {
            LogToFile($"Application Focus: {hasFocus}", LogType.Log);
        }
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            if (enableFileLogging && isInitialized)
            {
                LogToFile("=== GAME SESSION ENDED ===", LogType.Log);
                LogToFile($"Session End Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", LogType.Log);
                LogToFile("==============================", LogType.Log);
            }

            // Отписываемся от событий
            Application.logMessageReceived -= OnLogMessageReceived;
        }
    }
}