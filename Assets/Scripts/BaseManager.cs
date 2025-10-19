using UnityEngine;

/// <summary>
/// BaseManager - Р±Р°Р·РѕРІС‹Р№ РєР»Р°СЃСЃ РґР»СЏ РІСЃРµС… РјРµРЅРµРґР¶РµСЂРѕРІ/СЃРёСЃС‚РµРј РёРіСЂС‹
///
/// РђР РҐРРўР•РљРўРЈР Рђ: РћР±РµСЃРїРµС‡РёРІР°РµС‚:
/// 1. Р•РґРёРЅС‹Р№ РїР°С‚С‚РµСЂРЅ РёРЅРёС†РёР°Р»РёР·Р°С†РёРё РґР»СЏ РІСЃРµС… СЃРёСЃС‚РµРј
/// 2. РРЅС‚РµРіСЂР°С†РёСЋ СЃ ServiceLocator Рё EventBus
/// 3. РЎС‚Р°РЅРґР°СЂС‚РёР·РёСЂРѕРІР°РЅРЅС‹Р№ Р¶РёР·РЅРµРЅРЅС‹Р№ С†РёРєР»
/// 4. РћР±С‰РёРµ СѓС‚РёР»РёС‚С‹ РґР»СЏ Р»РѕРіРёСЂРѕРІР°РЅРёСЏ Рё РѕС‚Р»Р°РґРєРё
///
/// Р–РР—РќР•РќРќР«Р™ Р¦РРљР›:
///   Start() -> InitializeManager() -> OnManagerInitialized()
///   OnDestroy() -> ShutdownManager() -> OnManagerShutdown()
///
/// Р’РђР–РќРћ: РРЅРёС†РёР°Р»РёР·Р°С†РёСЏ РїСЂРѕРёСЃС…РѕРґРёС‚ РІ Start(), С‡С‚РѕР±С‹ ServiceLocator Р±С‹Р» РґРѕСЃС‚СѓРїРµРЅ
///
/// РРЎРџРћР›Р¬Р—РћР’РђРќРР•:
///   public class MyManager : BaseManager
///   {
///       protected override void OnManagerInitialized()
///       {
///           // РРЅРёС†РёР°Р»РёР·Р°С†РёСЏ РІР°С€РµР№ СЃРёСЃС‚РµРјС‹
///           // ServiceLocator СѓР¶Рµ РґРѕСЃС‚СѓРїРµРЅ
///       }
///
///       protected override void OnManagerShutdown()
///       {
///           // РћС‡РёСЃС‚РєР° СЂРµСЃСѓСЂСЃРѕРІ
///           // РћС‚РїРёСЃРєР° РѕС‚ СЃРѕР±С‹С‚РёР№
///       }
///   }
/// </summary>
public abstract class BaseManager : MonoBehaviour
{
    [Header("Base Manager Settings")]
    [Tooltip("Р’С‹РІРѕРґРёС‚СЊ РѕС‚Р»Р°РґРѕС‡РЅС‹Рµ СЃРѕРѕР±С‰РµРЅРёСЏ РІ РєРѕРЅСЃРѕР»СЊ")]
    [SerializeField] protected bool debugLogging = false;

    // Р¤Р»Р°Рі РёРЅРёС†РёР°Р»РёР·Р°С†РёРё
    private bool isInitialized = false;

    // РРјСЏ РјРµРЅРµРґР¶РµСЂР° РґР»СЏ Р»РѕРіРёСЂРѕРІР°РЅРёСЏ
    protected virtual string ManagerName => GetType().Name;

    // ========================================================================
    // UNITY LIFECYCLE
    // ========================================================================

    /// <summary>
    /// Start РІС‹Р·С‹РІР°РµС‚СЃСЏ РїРѕСЃР»Рµ Awake Рё РїРѕСЃР»Рµ С‚РѕРіРѕ РєР°Рє ServiceLocator РёРЅРёС†РёР°Р»РёР·РёСЂРѕРІР°РЅ
    /// РќР• РїРµСЂРµРѕРїСЂРµРґРµР»СЏР№С‚Рµ СЌС‚РѕС‚ РјРµС‚РѕРґ! РСЃРїРѕР»СЊР·СѓР№С‚Рµ OnManagerInitialized()
    /// </summary>
    protected virtual void Start()
    {
        InitializeManager();
    }

    /// <summary>
    /// OnDestroy РІС‹Р·С‹РІР°РµС‚СЃСЏ РїСЂРё СѓРЅРёС‡С‚РѕР¶РµРЅРёРё РѕР±СЉРµРєС‚Р°
    /// РќР• РїРµСЂРµРѕРїСЂРµРґРµР»СЏР№С‚Рµ СЌС‚РѕС‚ РјРµС‚РѕРґ! РСЃРїРѕР»СЊР·СѓР№С‚Рµ OnManagerShutdown()
    /// </summary>
    protected virtual void OnDestroy()
    {
        ShutdownManager();
    }

    // ========================================================================
    // MANAGER LIFECYCLE
    // ========================================================================

    /// <summary>
    /// РРЅРёС†РёР°Р»РёР·Р°С†РёСЏ РјРµРЅРµРґР¶РµСЂР°
    /// </summary>
    private void InitializeManager()
    {
        if (isInitialized)
        {
            LogWarning("Already initialized!");
            return;
        }

        Log($"Initializing {ManagerName}...");

        // Р’С‹Р·С‹РІР°РµРј РїРµСЂРµРѕРїСЂРµРґРµР»СЏРµРјС‹Р№ РјРµС‚РѕРґ РёРЅРёС†РёР°Р»РёР·Р°С†РёРё
        OnManagerInitialized();

        isInitialized = true;
        Log($"{ManagerName} initialized successfully");
    }

    /// <summary>
    /// Р—Р°РІРµСЂС€РµРЅРёРµ СЂР°Р±РѕС‚С‹ РјРµРЅРµРґР¶РµСЂР°
    /// </summary>
    private void ShutdownManager()
    {
        if (!isInitialized)
        {
            return;
        }

        Log($"Shutting down {ManagerName}...");

        // Р’С‹Р·С‹РІР°РµРј РїРµСЂРµРѕРїСЂРµРґРµР»СЏРµРјС‹Р№ РјРµС‚РѕРґ Р·Р°РІРµСЂС€РµРЅРёСЏ
        OnManagerShutdown();

        isInitialized = false;
        Log($"{ManagerName} shut down successfully");
    }

    /// <summary>
    /// РџРµСЂРµРѕРїСЂРµРґРµР»РёС‚Рµ СЌС‚РѕС‚ РјРµС‚РѕРґ РґР»СЏ РёРЅРёС†РёР°Р»РёР·Р°С†РёРё РІР°С€РµР№ СЃРёСЃС‚РµРјС‹
    /// Р’С‹Р·С‹РІР°РµС‚СЃСЏ РћР”РРќ Р РђР— РїСЂРё СЃРѕР·РґР°РЅРёРё РјРµРЅРµРґР¶РµСЂР°
    /// </summary>
    protected virtual void OnManagerInitialized()
    {
        // РџРµСЂРµРѕРїСЂРµРґРµР»РёС‚Рµ РІ РґРѕС‡РµСЂРЅРёС… РєР»Р°СЃСЃР°С…
    }

    /// <summary>
    /// РџРµСЂРµРѕРїСЂРµРґРµР»РёС‚Рµ СЌС‚РѕС‚ РјРµС‚РѕРґ РґР»СЏ РѕС‡РёСЃС‚РєРё СЂРµСЃСѓСЂСЃРѕРІ
    /// Р’С‹Р·С‹РІР°РµС‚СЃСЏ РћР”РРќ Р РђР— РїСЂРё СѓРЅРёС‡С‚РѕР¶РµРЅРёРё РјРµРЅРµРґР¶РµСЂР°
    /// Р’РђР–РќРћ: Р’СЃРµРіРґР° РѕС‚РїРёСЃС‹РІР°Р№С‚РµСЃСЊ РѕС‚ EventBus Р·РґРµСЃСЊ!
    /// </summary>
    protected virtual void OnManagerShutdown()
    {
        // РџРµСЂРµРѕРїСЂРµРґРµР»РёС‚Рµ РІ РґРѕС‡РµСЂРЅРёС… РєР»Р°СЃСЃР°С…
    }

    // ========================================================================
    // HELPER METHODS
    // ========================================================================

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ, РёРЅРёС†РёР°Р»РёР·РёСЂРѕРІР°РЅ Р»Рё РјРµРЅРµРґР¶РµСЂ
    /// </summary>
    public bool IsInitialized => isInitialized;

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ СЃРµСЂРІРёСЃ РёР· ServiceLocator СЃ РїСЂРѕРІРµСЂРєРѕР№
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
    /// РџРѕРїС‹С‚Р°С‚СЊСЃСЏ РїРѕР»СѓС‡РёС‚СЊ СЃРµСЂРІРёСЃ РёР· ServiceLocator
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
    /// Р’С‹РІРµСЃС‚Рё СЃРѕРѕР±С‰РµРЅРёРµ РІ Р»РѕРі (РµСЃР»Рё РІРєР»СЋС‡РµРЅ debugLogging)
    /// </summary>
    protected void Log(string message)
    {
        if (debugLogging)
        {
        }
    }

    /// <summary>
    /// Р’С‹РІРµСЃС‚Рё РїСЂРµРґСѓРїСЂРµР¶РґРµРЅРёРµ РІ Р»РѕРі (РІСЃРµРіРґР°)
    /// </summary>
    protected void LogWarning(string message)
    {
    }

    /// <summary>
    /// Р’С‹РІРµСЃС‚Рё РѕС€РёР±РєСѓ РІ Р»РѕРі (РІСЃРµРіРґР°)
    /// </summary>
    protected void LogError(string message)
    {
    }

    /// <summary>
    /// Р’С‹РІРµСЃС‚Рё СЃРѕРѕР±С‰РµРЅРёРµ РІ Р»РѕРі (РїСЂРёРЅСѓРґРёС‚РµР»СЊРЅРѕ, РёРіРЅРѕСЂРёСЂСѓСЏ debugLogging)
    /// </summary>
    protected void LogForce(string message)
    {
    }
}
