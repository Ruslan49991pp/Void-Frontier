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



        // Также записываем путь в лог файл
        FileLogger.Log($"Log file location: {logPath}");
    }

    [ContextMenu("Показать путь к логам")]
    public void ShowLogPath()
    {
        string logPath = FileLogger.GetLogFilePath();

        FileLogger.Log("Manual log path check triggered");
    }

    [ContextMenu("Тест логирования")]
    public void TestLogging()
    {
        FileLogger.Log("Тест обычного лога");
        FileLogger.LogWarning("Тест предупреждения");
        FileLogger.LogError("Тест ошибки");






    }
}