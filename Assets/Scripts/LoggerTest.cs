using System.Collections;
using UnityEngine;

/// <summary>
/// Тестовый компонент для проверки работы FileLogger
/// Генерирует различные типы логов для тестирования
/// </summary>
public class LoggerTest : MonoBehaviour
{
    [Header("Test Settings")]
    public bool runTestsOnStart = true;
    public float testInterval = 2f;
    public bool enablePeriodicTests = true;

    private int testCounter = 0;

    void Start()
    {
        if (runTestsOnStart)
        {
            StartCoroutine(RunInitialTests());
        }

        if (enablePeriodicTests)
        {
            StartCoroutine(RunPeriodicTests());
        }
    }

    /// <summary>
    /// Запуск начальных тестов логирования
    /// </summary>
    IEnumerator RunInitialTests()
    {
        yield return new WaitForSeconds(0.5f); // Ждем инициализации FileLogger

        // Тест кастомных логов через FileLogger
        FileLogger.Log("=== НАЧАЛО ТЕСТИРОВАНИЯ FILELOGGER ===");

        yield return new WaitForSeconds(0.1f);

        FileLogger.Log("Тест обычного лога через FileLogger.Log()");
        FileLogger.LogWarning("Тест предупреждения через FileLogger.LogWarning()");
        FileLogger.LogError("Тест ошибки через FileLogger.LogError()");

        yield return new WaitForSeconds(0.2f);

        // Тест Unity Debug логов (должны автоматически попасть в файл)




        yield return new WaitForSeconds(0.2f);

        // Информация о пути к файлу
        string logPath = FileLogger.GetLogFilePath();
        FileLogger.Log($"Путь к файлу логов: {logPath}");



        FileLogger.Log("=== НАЧАЛЬНОЕ ТЕСТИРОВАНИЕ ЗАВЕРШЕНО ===");
    }

    /// <summary>
    /// Периодические тесты для демонстрации работы
    /// </summary>
    IEnumerator RunPeriodicTests()
    {
        while (enablePeriodicTests)
        {
            yield return new WaitForSeconds(testInterval);

            testCounter++;

            // Различные типы тестовых сообщений
            switch (testCounter % 4)
            {
                case 0:
                    FileLogger.Log($"Периодический тест #{testCounter}: Игра работает нормально");
                    break;
                case 1:

                    break;
                case 2:
                    FileLogger.LogWarning($"Тестовое предупреждение #{testCounter}: Внимание, это тест!");
                    break;
                case 3:
                    // Симулируем какое-то игровое событие
                    FileLogger.Log($"Игровое событие #{testCounter}: Персонаж выполнил действие");
                    break;
            }

            // Останавливаем периодические тесты через 30 секунд
            if (testCounter >= 15)
            {
                enablePeriodicTests = false;
                FileLogger.Log("=== ПЕРИОДИЧЕСКИЕ ТЕСТЫ ЗАВЕРШЕНЫ ===");

            }
        }
    }

    /// <summary>
    /// Тест для кнопки в Inspector
    /// </summary>
    [ContextMenu("Запустить тест логирования")]
    public void RunManualTest()
    {
        FileLogger.Log("=== РУЧНОЙ ТЕСТ ЛОГИРОВАНИЯ ===");
        FileLogger.Log("Этот лог был создан вручную через ContextMenu");
        FileLogger.LogWarning("Тестовое предупреждение из ручного теста");
        FileLogger.LogError("Тестовая ошибка из ручного теста");




        FileLogger.Log($"Текущее время: {System.DateTime.Now:HH:mm:ss.fff}");
        FileLogger.Log("=== РУЧНОЙ ТЕСТ ЗАВЕРШЕН ===");


    }

    /// <summary>
    /// Симуляция игровых событий для тестирования
    /// </summary>
    void Update()
    {
        // Тест логирования при нажатии клавиш
        if (Input.GetKeyDown(KeyCode.L))
        {
            FileLogger.Log($"Нажата клавиша L в {Time.time:F2} секунд");
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            RunManualTest();
        }
    }

    void OnDestroy()
    {
        if (testCounter > 0)
        {
            FileLogger.Log($"LoggerTest уничтожен. Было выполнено {testCounter} тестов");
        }
    }
}