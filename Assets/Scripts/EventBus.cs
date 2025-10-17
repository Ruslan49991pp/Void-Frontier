using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// EventBus - централизованная система обмена сообщениями между системами
///
/// АРХИТЕКТУРА: Event-driven architecture позволяет:
/// 1. Полностью отделить отправителей от получателей
/// 2. Множественные слушатели для одного события
/// 3. Легко добавлять/удалять слушатели без изменения кода
/// 4. Упростить отладку через централизованную точку
///
/// PERFORMANCE: Использует Dictionary для быстрого доступа к подписчикам
///
/// ИСПОЛЬЗОВАНИЕ:
///   // Подписка
///   EventBus.Subscribe<CharacterSpawnedEvent>(OnCharacterSpawned);
///
///   void OnCharacterSpawned(CharacterSpawnedEvent evt) {
///       Debug.Log($"Character spawned: {evt.character.GetFullName()}");
///   }
///
///   // Публикация
///   EventBus.Publish(new CharacterSpawnedEvent(character));
///
///   // Отписка (ВАЖНО: всегда отписывайтесь в OnDestroy!)
///   EventBus.Unsubscribe<CharacterSpawnedEvent>(OnCharacterSpawned);
/// </summary>
public static class EventBus
{
    // Словарь подписчиков для каждого типа события
    private static readonly Dictionary<Type, List<Delegate>> subscribers = new Dictionary<Type, List<Delegate>>();

    // Флаг для отладки
    private static bool debugMode = false;

    /// <summary>
    /// Подписаться на событие
    /// </summary>
    public static void Subscribe<T>(Action<T> handler) where T : GameEvent
    {
        Type eventType = typeof(T);

        if (!subscribers.ContainsKey(eventType))
        {
            subscribers[eventType] = new List<Delegate>();
        }

        // Проверяем, что этот обработчик еще не подписан
        if (!subscribers[eventType].Contains(handler))
        {
            subscribers[eventType].Add(handler);

            if (debugMode)
            {
                Debug.Log($"[EventBus] Subscribed to {eventType.Name}. Total subscribers: {subscribers[eventType].Count}");
            }
        }
        else
        {
            Debug.LogWarning($"[EventBus] Handler already subscribed to {eventType.Name}");
        }
    }

    /// <summary>
    /// Отписаться от события
    /// ВАЖНО: Всегда вызывайте в OnDestroy() чтобы избежать утечек памяти!
    /// </summary>
    public static void Unsubscribe<T>(Action<T> handler) where T : GameEvent
    {
        Type eventType = typeof(T);

        if (subscribers.ContainsKey(eventType))
        {
            if (subscribers[eventType].Remove(handler))
            {
                if (debugMode)
                {
                    Debug.Log($"[EventBus] Unsubscribed from {eventType.Name}. Remaining subscribers: {subscribers[eventType].Count}");
                }

                // Удаляем пустые списки
                if (subscribers[eventType].Count == 0)
                {
                    subscribers.Remove(eventType);
                }
            }
            else
            {
                Debug.LogWarning($"[EventBus] Handler not found for {eventType.Name}");
            }
        }
    }

    /// <summary>
    /// Опубликовать событие - уведомить всех подписчиков
    /// </summary>
    public static void Publish<T>(T gameEvent) where T : GameEvent
    {
        Type eventType = typeof(T);

        if (subscribers.ContainsKey(eventType))
        {
            // Создаем копию списка на случай если кто-то отпишется во время обработки
            List<Delegate> handlersCopy = new List<Delegate>(subscribers[eventType]);

            if (debugMode)
            {
                Debug.Log($"[EventBus] Publishing {eventType.Name} to {handlersCopy.Count} subscribers");
            }

            foreach (Delegate handler in handlersCopy)
            {
                try
                {
                    // Вызываем обработчик
                    (handler as Action<T>)?.Invoke(gameEvent);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventBus] Error invoking handler for {eventType.Name}: {ex.Message}");
                    Debug.LogError($"[EventBus] Stack trace: {ex.StackTrace}");
                }
            }
        }
        else
        {
            if (debugMode)
            {
                Debug.Log($"[EventBus] No subscribers for {eventType.Name}");
            }
        }
    }

    /// <summary>
    /// Проверить, есть ли подписчики на событие
    /// </summary>
    public static bool HasSubscribers<T>() where T : GameEvent
    {
        Type eventType = typeof(T);
        return subscribers.ContainsKey(eventType) && subscribers[eventType].Count > 0;
    }

    /// <summary>
    /// Получить количество подписчиков на событие
    /// </summary>
    public static int GetSubscriberCount<T>() where T : GameEvent
    {
        Type eventType = typeof(T);
        return subscribers.ContainsKey(eventType) ? subscribers[eventType].Count : 0;
    }

    /// <summary>
    /// Очистить всех подписчиков для всех событий
    /// ВНИМАНИЕ: Используйте только при переходе между сценами!
    /// </summary>
    public static void Clear()
    {
        subscribers.Clear();
        Debug.Log("[EventBus] Cleared all subscribers");
    }

    /// <summary>
    /// Включить/выключить режим отладки
    /// </summary>
    public static void SetDebugMode(bool enabled)
    {
        debugMode = enabled;
        Debug.Log($"[EventBus] Debug mode {(enabled ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// Вывести статистику подписчиков (для отладки)
    /// </summary>
    public static void DebugPrintStatistics()
    {
        Debug.Log($"[EventBus] Statistics: {subscribers.Count} event types registered");

        foreach (var kvp in subscribers)
        {
            Debug.Log($"  - {kvp.Key.Name}: {kvp.Value.Count} subscribers");
        }
    }
}
