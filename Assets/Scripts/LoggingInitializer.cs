using UnityEngine;

/// <summary>
/// Простой инициализатор для принудительного запуска логирования
/// Добавь этот компонент на любой GameObject в сцене
/// </summary>
public class LoggingInitializer : MonoBehaviour
{
    void Start()
    {
        // Принудительно создаем FileLogger
        var logger = FileLogger.Instance;

        // Получаем путь и выводим в консоль
        string logPath = FileLogger.GetLogFilePath();

        // Записываем тестовое сообщение
        FileLogger.Log("LoggingInitializer: Система логирования запущена");

        // Выводим путь в Unity Console
        Debug.Log($"=== ДЛЯ CLAUDE: Путь к файлу логов: {logPath} ===");
        Debug.Log($"FileLogger initialized. Log file: {logPath}");

        // Также записываем путь в лог файл
        FileLogger.Log($"Log file location: {logPath}");
    }

    [ContextMenu("Показать путь к логам")]
    public void ShowLogPath()
    {
        string logPath = FileLogger.GetLogFilePath();
        Debug.Log($"=== ПУТЬ К ЛОГАМ: {logPath} ===");
        FileLogger.Log("Manual log path check triggered");
    }

    [ContextMenu("Тест логирования")]
    public void TestLogging()
    {
        FileLogger.Log("Тест обычного лога");
        FileLogger.LogWarning("Тест предупреждения");
        FileLogger.LogError("Тест ошибки");

        Debug.Log("Unity Debug.Log тест");
        Debug.LogWarning("Unity Debug.LogWarning тест");
        Debug.LogError("Unity Debug.LogError тест");

        Debug.Log("Тест логирования выполнен - проверь файл логов!");
    }
}