using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ServiceLocator - централизованный паттерн для управления всеми сервисами/менеджерами игры
/// Заменяет использование FindObjectOfType и обеспечивает единую точку доступа ко всем системам
///
/// PERFORMANCE: FindObjectOfType - очень медленная операция (сканирует всю сцену)
/// ServiceLocator использует Dictionary - O(1) доступ вместо O(n) сканирования
///
/// Использование:
///   // Регистрация сервиса
///   ServiceLocator.Register<IGridManager>(gridManager);
///
///   // Получение сервиса
///   var gridManager = ServiceLocator.Get<IGridManager>();
///
///   // Проверка наличия сервиса
///   if (ServiceLocator.TryGet<IGridManager>(out var manager)) { ... }
/// </summary>
public static class ServiceLocator
{
    // Словарь всех зарегистрированных сервисов
    private static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();

    // Флаг инициализации
    private static bool isInitialized = false;

    /// <summary>
    /// Зарегистрировать сервис в ServiceLocator
    /// </summary>
    public static void Register<T>(T service) where T : class
    {
        Type type = typeof(T);

        if (services.ContainsKey(type))
        {
            Debug.LogWarning($"[ServiceLocator] Service {type.Name} is already registered. Replacing...");
            services[type] = service;
        }
        else
        {
            services.Add(type, service);
            Debug.Log($"[ServiceLocator] Registered service: {type.Name}");
        }
    }

    /// <summary>
    /// Получить сервис из ServiceLocator
    /// Бросает исключение если сервис не найден
    /// </summary>
    public static T Get<T>() where T : class
    {
        Type type = typeof(T);

        if (services.TryGetValue(type, out object service))
        {
            return service as T;
        }

        Debug.LogError($"[ServiceLocator] Service {type.Name} not found! Make sure it's registered in GameBootstrap.");
        return null;
    }

    /// <summary>
    /// Попытаться получить сервис из ServiceLocator
    /// Возвращает true если сервис найден, false если нет
    /// </summary>
    public static bool TryGet<T>(out T service) where T : class
    {
        Type type = typeof(T);

        if (services.TryGetValue(type, out object foundService))
        {
            service = foundService as T;
            return service != null;
        }

        service = null;
        return false;
    }

    /// <summary>
    /// Проверить, зарегистрирован ли сервис
    /// </summary>
    public static bool Has<T>() where T : class
    {
        return services.ContainsKey(typeof(T));
    }

    /// <summary>
    /// Отменить регистрацию сервиса
    /// </summary>
    public static void Unregister<T>() where T : class
    {
        Type type = typeof(T);

        if (services.ContainsKey(type))
        {
            services.Remove(type);
            Debug.Log($"[ServiceLocator] Unregistered service: {type.Name}");
        }
    }

    /// <summary>
    /// Очистить все зарегистрированные сервисы
    /// Используется при переходе между сценами
    /// </summary>
    public static void Clear()
    {
        services.Clear();
        isInitialized = false;
        Debug.Log("[ServiceLocator] Cleared all services");
    }

    /// <summary>
    /// Получить количество зарегистрированных сервисов
    /// </summary>
    public static int ServiceCount => services.Count;

    /// <summary>
    /// Установить флаг инициализации
    /// </summary>
    public static void SetInitialized()
    {
        isInitialized = true;
        Debug.Log($"[ServiceLocator] Initialized with {services.Count} services");
    }

    /// <summary>
    /// Проверить, инициализирован ли ServiceLocator
    /// </summary>
    public static bool IsInitialized => isInitialized;

    /// <summary>
    /// Вывести список всех зарегистрированных сервисов (для отладки)
    /// </summary>
    public static void DebugPrintServices()
    {
        Debug.Log($"[ServiceLocator] Registered services ({services.Count}):");
        foreach (var kvp in services)
        {
            Debug.Log($"  - {kvp.Key.Name}: {kvp.Value?.GetType().Name ?? "null"}");
        }
    }
}
