using UnityEngine;

/// <summary>
/// BaseManager - базовый класс для всех менеджеров/систем игры
///
/// АРХИТЕКТУРА: Обеспечивает:
/// 1. Единый паттерн инициализации для всех систем
/// 2. Интеграцию с ServiceLocator и EventBus
/// 3. Стандартизированный жизненный цикл
/// 4. Общие утилиты для логирования и отладки
///
/// ЖИЗНЕННЫЙ ЦИКЛ:
///   Start() -> InitializeManager() -> OnManagerInitialized()
///   OnDestroy() -> ShutdownManager() -> OnManagerShutdown()
///
/// ВАЖНО: Инициализация происходит в Start(), чтобы ServiceLocator был доступен
///
/// ИСПОЛЬЗОВАНИЕ:
///   public class MyManager : BaseManager
///   {
///       protected override void OnManagerInitialized()
///       {
///           // Инициализация вашей системы
///           // ServiceLocator уже доступен
///       }
///
///       protected override void OnManagerShutdown()
///       {
///           // Очистка ресурсов
///           // Отписка от событий
///       }
///   }
/// </summary>
public abstract class BaseManager : MonoBehaviour
{
    [Header("Base Manager Settings")]
    [Tooltip("Выводить отладочные сообщения в консоль")]
    [SerializeField] protected bool debugLogging = false;

    // Флаг инициализации
    private bool isInitialized = false;

    // Имя менеджера для логирования
    protected virtual string ManagerName => GetType().Name;

    // ========================================================================
    // UNITY LIFECYCLE
    // ========================================================================

    /// <summary>
    /// Start вызывается после Awake и после того как ServiceLocator инициализирован
    /// НЕ переопределяйте этот метод! Используйте OnManagerInitialized()
    /// </summary>
    protected virtual void Start()
    {
        InitializeManager();
    }

    /// <summary>
    /// OnDestroy вызывается при уничтожении объекта
    /// НЕ переопределяйте этот метод! Используйте OnManagerShutdown()
    /// </summary>
    protected virtual void OnDestroy()
    {
        ShutdownManager();
    }

    // ========================================================================
    // MANAGER LIFECYCLE
    // ========================================================================

    /// <summary>
    /// Инициализация менеджера
    /// </summary>
    private void InitializeManager()
    {
        if (isInitialized)
        {
            LogWarning("Already initialized!");
            return;
        }

        Log($"Initializing {ManagerName}...");

        // Вызываем переопределяемый метод инициализации
        OnManagerInitialized();

        isInitialized = true;
        Log($"{ManagerName} initialized successfully");
    }

    /// <summary>
    /// Завершение работы менеджера
    /// </summary>
    private void ShutdownManager()
    {
        if (!isInitialized)
        {
            return;
        }

        Log($"Shutting down {ManagerName}...");

        // Вызываем переопределяемый метод завершения
        OnManagerShutdown();

        isInitialized = false;
        Log($"{ManagerName} shut down successfully");
    }

    /// <summary>
    /// Переопределите этот метод для инициализации вашей системы
    /// Вызывается ОДИН РАЗ при создании менеджера
    /// </summary>
    protected virtual void OnManagerInitialized()
    {
        // Переопределите в дочерних классах
    }

    /// <summary>
    /// Переопределите этот метод для очистки ресурсов
    /// Вызывается ОДИН РАЗ при уничтожении менеджера
    /// ВАЖНО: Всегда отписывайтесь от EventBus здесь!
    /// </summary>
    protected virtual void OnManagerShutdown()
    {
        // Переопределите в дочерних классах
    }

    // ========================================================================
    // HELPER METHODS
    // ========================================================================

    /// <summary>
    /// Проверить, инициализирован ли менеджер
    /// </summary>
    public bool IsInitialized => isInitialized;

    /// <summary>
    /// Получить сервис из ServiceLocator с проверкой
    /// </summary>
    protected T GetService<T>() where T : class
    {
        if (!ServiceLocator.IsInitialized)
        {
            LogError("ServiceLocator is not initialized yet!");
            return null;
        }

        T service = ServiceLocator.Get<T>();
        if (service == null)
        {
            LogError($"Service {typeof(T).Name} not found in ServiceLocator!");
        }
        return service;
    }

    /// <summary>
    /// Попытаться получить сервис из ServiceLocator
    /// </summary>
    protected bool TryGetService<T>(out T service) where T : class
    {
        if (!ServiceLocator.IsInitialized)
        {
            service = null;
            return false;
        }

        return ServiceLocator.TryGet(out service);
    }

    // ========================================================================
    // LOGGING UTILITIES
    // ========================================================================

    /// <summary>
    /// Вывести сообщение в лог (если включен debugLogging)
    /// </summary>
    protected void Log(string message)
    {
        if (debugLogging)
        {
            Debug.Log($"[{ManagerName}] {message}");
        }
    }

    /// <summary>
    /// Вывести предупреждение в лог (всегда)
    /// </summary>
    protected void LogWarning(string message)
    {
        Debug.LogWarning($"[{ManagerName}] {message}");
    }

    /// <summary>
    /// Вывести ошибку в лог (всегда)
    /// </summary>
    protected void LogError(string message)
    {
        Debug.LogError($"[{ManagerName}] {message}");
    }

    /// <summary>
    /// Вывести сообщение в лог (принудительно, игнорируя debugLogging)
    /// </summary>
    protected void LogForce(string message)
    {
        Debug.Log($"[{ManagerName}] {message}");
    }
}
