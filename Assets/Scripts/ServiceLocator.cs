using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ServiceLocator - С†РµРЅС‚СЂР°Р»РёР·РѕРІР°РЅРЅС‹Р№ РїР°С‚С‚РµСЂРЅ РґР»СЏ СѓРїСЂР°РІР»РµРЅРёСЏ РІСЃРµРјРё СЃРµСЂРІРёСЃР°РјРё/РјРµРЅРµРґР¶РµСЂР°РјРё РёРіСЂС‹
/// Р—Р°РјРµРЅСЏРµС‚ РёСЃРїРѕР»СЊР·РѕРІР°РЅРёРµ FindObjectOfType Рё РѕР±РµСЃРїРµС‡РёРІР°РµС‚ РµРґРёРЅСѓСЋ С‚РѕС‡РєСѓ РґРѕСЃС‚СѓРїР° РєРѕ РІСЃРµРј СЃРёСЃС‚РµРјР°Рј
///
/// PERFORMANCE: FindObjectOfType - РѕС‡РµРЅСЊ РјРµРґР»РµРЅРЅР°СЏ РѕРїРµСЂР°С†РёСЏ (СЃРєР°РЅРёСЂСѓРµС‚ РІСЃСЋ СЃС†РµРЅСѓ)
/// ServiceLocator РёСЃРїРѕР»СЊР·СѓРµС‚ Dictionary - O(1) РґРѕСЃС‚СѓРї РІРјРµСЃС‚Рѕ O(n) СЃРєР°РЅРёСЂРѕРІР°РЅРёСЏ
///
/// РСЃРїРѕР»СЊР·РѕРІР°РЅРёРµ:
///   // Р РµРіРёСЃС‚СЂР°С†РёСЏ СЃРµСЂРІРёСЃР°
///   ServiceLocator.Register<IGridManager>(gridManager);
///
///   // РџРѕР»СѓС‡РµРЅРёРµ СЃРµСЂРІРёСЃР°
///   var gridManager = ServiceLocator.Get<IGridManager>();
///
///   // РџСЂРѕРІРµСЂРєР° РЅР°Р»РёС‡РёСЏ СЃРµСЂРІРёСЃР°
///   if (ServiceLocator.TryGet<IGridManager>(out var manager)) { ... }
/// </summary>
public static class ServiceLocator
{
    // РЎР»РѕРІР°СЂСЊ РІСЃРµС… Р·Р°СЂРµРіРёСЃС‚СЂРёСЂРѕРІР°РЅРЅС‹С… СЃРµСЂРІРёСЃРѕРІ
    private static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();

    // Р¤Р»Р°Рі РёРЅРёС†РёР°Р»РёР·Р°С†РёРё
    private static bool isInitialized = false;

    /// <summary>
    /// Р—Р°СЂРµРіРёСЃС‚СЂРёСЂРѕРІР°С‚СЊ СЃРµСЂРІРёСЃ РІ ServiceLocator
    /// </summary>
    public static void Register<T>(T service) where T : class
    {
        Type type = typeof(T);

        if (services.ContainsKey(type))
        {
            services[type] = service;
        }
        else
        {
            services.Add(type, service);
        }
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ СЃРµСЂРІРёСЃ РёР· ServiceLocator
    /// Р‘СЂРѕСЃР°РµС‚ РёСЃРєР»СЋС‡РµРЅРёРµ РµСЃР»Рё СЃРµСЂРІРёСЃ РЅРµ РЅР°Р№РґРµРЅ
    /// </summary>
    public static T Get<T>() where T : class
    {
        Type type = typeof(T);

        if (services.TryGetValue(type, out object service))
        {
            return service as T;
        }

        return null;
    }

    /// <summary>
    /// РџРѕРїС‹С‚Р°С‚СЊСЃСЏ РїРѕР»СѓС‡РёС‚СЊ СЃРµСЂРІРёСЃ РёР· ServiceLocator
    /// Р’РѕР·РІСЂР°С‰Р°РµС‚ true РµСЃР»Рё СЃРµСЂРІРёСЃ РЅР°Р№РґРµРЅ, false РµСЃР»Рё РЅРµС‚
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
    /// РџСЂРѕРІРµСЂРёС‚СЊ, Р·Р°СЂРµРіРёСЃС‚СЂРёСЂРѕРІР°РЅ Р»Рё СЃРµСЂРІРёСЃ
    /// </summary>
    public static bool Has<T>() where T : class
    {
        return services.ContainsKey(typeof(T));
    }

    /// <summary>
    /// РћС‚РјРµРЅРёС‚СЊ СЂРµРіРёСЃС‚СЂР°С†РёСЋ СЃРµСЂРІРёСЃР°
    /// </summary>
    public static void Unregister<T>() where T : class
    {
        Type type = typeof(T);

        if (services.ContainsKey(type))
        {
            services.Remove(type);
        }
    }

    /// <summary>
    /// РћС‡РёСЃС‚РёС‚СЊ РІСЃРµ Р·Р°СЂРµРіРёСЃС‚СЂРёСЂРѕРІР°РЅРЅС‹Рµ СЃРµСЂРІРёСЃС‹
    /// РСЃРїРѕР»СЊР·СѓРµС‚СЃСЏ РїСЂРё РїРµСЂРµС…РѕРґРµ РјРµР¶РґСѓ СЃС†РµРЅР°РјРё
    /// </summary>
    public static void Clear()
    {
        services.Clear();
        isInitialized = false;
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РєРѕР»РёС‡РµСЃС‚РІРѕ Р·Р°СЂРµРіРёСЃС‚СЂРёСЂРѕРІР°РЅРЅС‹С… СЃРµСЂРІРёСЃРѕРІ
    /// </summary>
    public static int ServiceCount => services.Count;

    /// <summary>
    /// РЈСЃС‚Р°РЅРѕРІРёС‚СЊ С„Р»Р°Рі РёРЅРёС†РёР°Р»РёР·Р°С†РёРё
    /// </summary>
    public static void SetInitialized()
    {
        isInitialized = true;
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ, РёРЅРёС†РёР°Р»РёР·РёСЂРѕРІР°РЅ Р»Рё ServiceLocator
    /// </summary>
    public static bool IsInitialized => isInitialized;

    /// <summary>
    /// Р’С‹РІРµСЃС‚Рё СЃРїРёСЃРѕРє РІСЃРµС… Р·Р°СЂРµРіРёСЃС‚СЂРёСЂРѕРІР°РЅРЅС‹С… СЃРµСЂРІРёСЃРѕРІ (РґР»СЏ РѕС‚Р»Р°РґРєРё)
    /// </summary>
    public static void DebugPrintServices()
    {
        foreach (var kvp in services)
        {
        }
    }
}
