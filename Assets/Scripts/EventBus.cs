using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// EventBus - С†РµРЅС‚СЂР°Р»РёР·РѕРІР°РЅРЅР°СЏ СЃРёСЃС‚РµРјР° РѕР±РјРµРЅР° СЃРѕРѕР±С‰РµРЅРёСЏРјРё РјРµР¶РґСѓ СЃРёСЃС‚РµРјР°РјРё
///
/// РђР РҐРРўР•РљРўРЈР Рђ: Event-driven architecture РїРѕР·РІРѕР»СЏРµС‚:
/// 1. РџРѕР»РЅРѕСЃС‚СЊСЋ РѕС‚РґРµР»РёС‚СЊ РѕС‚РїСЂР°РІРёС‚РµР»РµР№ РѕС‚ РїРѕР»СѓС‡Р°С‚РµР»РµР№
/// 2. РњРЅРѕР¶РµСЃС‚РІРµРЅРЅС‹Рµ СЃР»СѓС€Р°С‚РµР»Рё РґР»СЏ РѕРґРЅРѕРіРѕ СЃРѕР±С‹С‚РёСЏ
/// 3. Р›РµРіРєРѕ РґРѕР±Р°РІР»СЏС‚СЊ/СѓРґР°Р»СЏС‚СЊ СЃР»СѓС€Р°С‚РµР»Рё Р±РµР· РёР·РјРµРЅРµРЅРёСЏ РєРѕРґР°
/// 4. РЈРїСЂРѕСЃС‚РёС‚СЊ РѕС‚Р»Р°РґРєСѓ С‡РµСЂРµР· С†РµРЅС‚СЂР°Р»РёР·РѕРІР°РЅРЅСѓСЋ С‚РѕС‡РєСѓ
///
/// PERFORMANCE: РСЃРїРѕР»СЊР·СѓРµС‚ Dictionary РґР»СЏ Р±С‹СЃС‚СЂРѕРіРѕ РґРѕСЃС‚СѓРїР° Рє РїРѕРґРїРёСЃС‡РёРєР°Рј
///
/// РРЎРџРћР›Р¬Р—РћР’РђРќРР•:
///   // РџРѕРґРїРёСЃРєР°
///   EventBus.Subscribe<CharacterSpawnedEvent>(OnCharacterSpawned);
///
///   void OnCharacterSpawned(CharacterSpawnedEvent evt) {
///       Debug.Log($"Character spawned: {evt.character.GetFullName()}");
///   }
///
///   // РџСѓР±Р»РёРєР°С†РёСЏ
///   EventBus.Publish(new CharacterSpawnedEvent(character));
///
///   // РћС‚РїРёСЃРєР° (Р’РђР–РќРћ: РІСЃРµРіРґР° РѕС‚РїРёСЃС‹РІР°Р№С‚РµСЃСЊ РІ OnDestroy!)
///   EventBus.Unsubscribe<CharacterSpawnedEvent>(OnCharacterSpawned);
/// </summary>
public static class EventBus
{
    // РЎР»РѕРІР°СЂСЊ РїРѕРґРїРёСЃС‡РёРєРѕРІ РґР»СЏ РєР°Р¶РґРѕРіРѕ С‚РёРїР° СЃРѕР±С‹С‚РёСЏ
    private static readonly Dictionary<Type, List<Delegate>> subscribers = new Dictionary<Type, List<Delegate>>();

    // Р¤Р»Р°Рі РґР»СЏ РѕС‚Р»Р°РґРєРё
    private static bool debugMode = false;

    /// <summary>
    /// РџРѕРґРїРёСЃР°С‚СЊСЃСЏ РЅР° СЃРѕР±С‹С‚РёРµ
    /// </summary>
    public static void Subscribe<T>(Action<T> handler) where T : GameEvent
    {
        Type eventType = typeof(T);

        if (!subscribers.ContainsKey(eventType))
        {
            subscribers[eventType] = new List<Delegate>();
        }

        // РџСЂРѕРІРµСЂСЏРµРј, С‡С‚Рѕ СЌС‚РѕС‚ РѕР±СЂР°Р±РѕС‚С‡РёРє РµС‰Рµ РЅРµ РїРѕРґРїРёСЃР°РЅ
        if (!subscribers[eventType].Contains(handler))
        {
            subscribers[eventType].Add(handler);

            if (debugMode)
            {
            }
        }
        else
        {
        }
    }

    /// <summary>
    /// РћС‚РїРёСЃР°С‚СЊСЃСЏ РѕС‚ СЃРѕР±С‹С‚РёСЏ
    /// Р’РђР–РќРћ: Р’СЃРµРіРґР° РІС‹Р·С‹РІР°Р№С‚Рµ РІ OnDestroy() С‡С‚РѕР±С‹ РёР·Р±РµР¶Р°С‚СЊ СѓС‚РµС‡РµРє РїР°РјСЏС‚Рё!
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
                }

                // РЈРґР°Р»СЏРµРј РїСѓСЃС‚С‹Рµ СЃРїРёСЃРєРё
                if (subscribers[eventType].Count == 0)
                {
                    subscribers.Remove(eventType);
                }
            }
            else
            {
            }
        }
    }

    /// <summary>
    /// РћРїСѓР±Р»РёРєРѕРІР°С‚СЊ СЃРѕР±С‹С‚РёРµ - СѓРІРµРґРѕРјРёС‚СЊ РІСЃРµС… РїРѕРґРїРёСЃС‡РёРєРѕРІ
    /// </summary>
    public static void Publish<T>(T gameEvent) where T : GameEvent
    {
        Type eventType = typeof(T);

        if (subscribers.ContainsKey(eventType))
        {
            // РЎРѕР·РґР°РµРј РєРѕРїРёСЋ СЃРїРёСЃРєР° РЅР° СЃР»СѓС‡Р°Р№ РµСЃР»Рё РєС‚Рѕ-С‚Рѕ РѕС‚РїРёС€РµС‚СЃСЏ РІРѕ РІСЂРµРјСЏ РѕР±СЂР°Р±РѕС‚РєРё
            List<Delegate> handlersCopy = new List<Delegate>(subscribers[eventType]);

            if (debugMode)
            {
            }

            foreach (Delegate handler in handlersCopy)
            {
                try
                {
                    // Р’С‹Р·С‹РІР°РµРј РѕР±СЂР°Р±РѕС‚С‡РёРє
                    (handler as Action<T>)?.Invoke(gameEvent);
                }
                catch (Exception ex)
                {
                }
            }
        }
        else
        {
            if (debugMode)
            {
            }
        }
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ, РµСЃС‚СЊ Р»Рё РїРѕРґРїРёСЃС‡РёРєРё РЅР° СЃРѕР±С‹С‚РёРµ
    /// </summary>
    public static bool HasSubscribers<T>() where T : GameEvent
    {
        Type eventType = typeof(T);
        return subscribers.ContainsKey(eventType) && subscribers[eventType].Count > 0;
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РєРѕР»РёС‡РµСЃС‚РІРѕ РїРѕРґРїРёСЃС‡РёРєРѕРІ РЅР° СЃРѕР±С‹С‚РёРµ
    /// </summary>
    public static int GetSubscriberCount<T>() where T : GameEvent
    {
        Type eventType = typeof(T);
        return subscribers.ContainsKey(eventType) ? subscribers[eventType].Count : 0;
    }

    /// <summary>
    /// РћС‡РёСЃС‚РёС‚СЊ РІСЃРµС… РїРѕРґРїРёСЃС‡РёРєРѕРІ РґР»СЏ РІСЃРµС… СЃРѕР±С‹С‚РёР№
    /// Р’РќРРњРђРќРР•: РСЃРїРѕР»СЊР·СѓР№С‚Рµ С‚РѕР»СЊРєРѕ РїСЂРё РїРµСЂРµС…РѕРґРµ РјРµР¶РґСѓ СЃС†РµРЅР°РјРё!
    /// </summary>
    public static void Clear()
    {
        subscribers.Clear();
    }

    /// <summary>
    /// Р’РєР»СЋС‡РёС‚СЊ/РІС‹РєР»СЋС‡РёС‚СЊ СЂРµР¶РёРј РѕС‚Р»Р°РґРєРё
    /// </summary>
    public static void SetDebugMode(bool enabled)
    {
        debugMode = enabled;
    }

    /// <summary>
    /// Р’С‹РІРµСЃС‚Рё СЃС‚Р°С‚РёСЃС‚РёРєСѓ РїРѕРґРїРёСЃС‡РёРєРѕРІ (РґР»СЏ РѕС‚Р»Р°РґРєРё)
    /// </summary>
    public static void DebugPrintStatistics()
    {

        foreach (var kvp in subscribers)
        {
        }
    }
}
