using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// GameUtilities - РєРѕР»Р»РµРєС†РёСЏ СѓС‚РёР»РёС‚Р°СЂРЅС‹С… РјРµС‚РѕРґРѕРІ РґР»СЏ РѕР±С‰РёС… РѕРїРµСЂР°С†РёР№
///
/// CODE QUALITY: Р¦РµРЅС‚СЂР°Р»РёР·Р°С†РёСЏ РґСѓР±Р»РёСЂСѓСЋС‰РµРіРѕСЃСЏ РєРѕРґР°:
/// 1. РЈРјРµРЅСЊС€Р°РµС‚ РґСѓР±Р»РёСЂРѕРІР°РЅРёРµ РєРѕРґР° (DRY principle)
/// 2. РћР±РµСЃРїРµС‡РёРІР°РµС‚ РµРґРёРЅРѕРѕР±СЂР°Р·РёРµ РѕРїРµСЂР°С†РёР№
/// 3. РЈРїСЂРѕС‰Р°РµС‚ С‚РµСЃС‚РёСЂРѕРІР°РЅРёРµ
/// 4. РЈР»СѓС‡С€Р°РµС‚ С‡РёС‚Р°РµРјРѕСЃС‚СЊ РєРѕРґР°
///
/// РРЎРџРћР›Р¬Р—РћР’РђРќРР•:
///   // Р‘РµР·РѕРїР°СЃРЅРѕРµ РїРѕР»СѓС‡РµРЅРёРµ РєРѕРјРїРѕРЅРµРЅС‚Р°
///   Character character = GameUtils.GetComponentSafe<Character>(gameObject);
///
///   // РџСЂРѕРІРµСЂРєР° РґРёСЃС‚Р°РЅС†РёРё
///   if (GameUtils.IsInRange(player, enemy, attackRange)) { ... }
///
///   // РЎРѕР·РґР°РЅРёРµ 2D РІРµРєС‚РѕСЂР° РёР· 3D
///   Vector2 flatPos = GameUtils.To2D(worldPosition);
/// </summary>
public static class GameUtils
{
    // ========================================================================
    // COMPONENT UTILITIES
    // ========================================================================

    /// <summary>
    /// Р‘РµР·РѕРїР°СЃРЅРѕРµ РїРѕР»СѓС‡РµРЅРёРµ РєРѕРјРїРѕРЅРµРЅС‚Р° СЃ Р»РѕРіРёСЂРѕРІР°РЅРёРµРј РѕС€РёР±РєРё
    /// </summary>
    public static T GetComponentSafe<T>(GameObject obj, bool logError = true) where T : Component
    {
        if (obj == null)
        {
            if (logError)
            return null;
        }

        T component = obj.GetComponent<T>();
        if (component == null && logError)
        {
        }

        return component;
    }

    /// <summary>
    /// Р‘РµР·РѕРїР°СЃРЅРѕРµ РїРѕР»СѓС‡РµРЅРёРµ РєРѕРјРїРѕРЅРµРЅС‚Р° РІ СЂРѕРґРёС‚РµР»СЏС…
    /// </summary>
    public static T GetComponentInParentSafe<T>(GameObject obj, bool logError = true) where T : Component
    {
        if (obj == null)
        {
            if (logError)
            return null;
        }

        T component = obj.GetComponentInParent<T>();
        if (component == null && logError)
        {
        }

        return component;
    }

    /// <summary>
    /// Р‘РµР·РѕРїР°СЃРЅРѕРµ РїРѕР»СѓС‡РµРЅРёРµ РєРѕРјРїРѕРЅРµРЅС‚Р° РІ РґРµС‚СЏС…
    /// </summary>
    public static T GetComponentInChildrenSafe<T>(GameObject obj, bool logError = true) where T : Component
    {
        if (obj == null)
        {
            if (logError)
            return null;
        }

        T component = obj.GetComponentInChildren<T>();
        if (component == null && logError)
        {
        }

        return component;
    }

    // ========================================================================
    // NULL CHECK UTILITIES
    // ========================================================================

    /// <summary>
    /// РџСЂРѕРІРµСЂРєР° С‡С‚Рѕ GameObject РЅРµ СѓРЅРёС‡С‚РѕР¶РµРЅ (Unity null check)
    /// Unity СѓРЅРёС‡С‚РѕР¶РµРЅРЅС‹Рµ РѕР±СЉРµРєС‚С‹ РјРѕРіСѓС‚ Р±С‹С‚СЊ != null, РЅРѕ ReferenceEquals РґР°РµС‚ РїСЂР°РІРґСѓ
    /// </summary>
    public static bool IsDestroyed(Object obj)
    {
        return ReferenceEquals(obj, null) || obj == null;
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРєР° С‡С‚Рѕ GameObject Р¶РёРІ Рё Р°РєС‚РёРІРµРЅ
    /// </summary>
    public static bool IsAliveAndActive(GameObject obj)
    {
        return obj != null && !ReferenceEquals(obj, null) && obj.activeInHierarchy;
    }

    // ========================================================================
    // POSITION & DISTANCE UTILITIES
    // ========================================================================

    /// <summary>
    /// РљРѕРЅРІРµСЂС‚Р°С†РёСЏ 3D РїРѕР·РёС†РёРё РІ 2D (XZ РїР»РѕСЃРєРѕСЃС‚СЊ)
    /// </summary>
    public static Vector2 To2D(Vector3 position)
    {
        return new Vector2(position.x, position.z);
    }

    /// <summary>
    /// РљРѕРЅРІРµСЂС‚Р°С†РёСЏ 2D РїРѕР·РёС†РёРё РІ 3D (XZ РїР»РѕСЃРєРѕСЃС‚СЊ, Y = 0)
    /// </summary>
    public static Vector3 To3D(Vector2 position, float y = 0f)
    {
        return new Vector3(position.x, y, position.y);
    }

    /// <summary>
    /// РџР»РѕСЃРєР°СЏ РґРёСЃС‚Р°РЅС†РёСЏ РјРµР¶РґСѓ РґРІСѓРјСЏ РѕР±СЉРµРєС‚Р°РјРё (РёРіРЅРѕСЂРёСЂСѓРµС‚ Y)
    /// </summary>
    public static float FlatDistance(Vector3 a, Vector3 b)
    {
        Vector2 flatA = To2D(a);
        Vector2 flatB = To2D(b);
        return Vector2.Distance(flatA, flatB);
    }

    /// <summary>
    /// РџР»РѕСЃРєР°СЏ РґРёСЃС‚Р°РЅС†РёСЏ РјРµР¶РґСѓ РґРІСѓРјСЏ GameObject
    /// </summary>
    public static float FlatDistance(GameObject a, GameObject b)
    {
        if (a == null || b == null) return float.MaxValue;
        return FlatDistance(a.transform.position, b.transform.position);
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРєР° РЅР°С…РѕРґСЏС‚СЃСЏ Р»Рё РґРІРµ РїРѕР·РёС†РёРё РІ РїСЂРµРґРµР»Р°С… РґРёСЃС‚Р°РЅС†РёРё
    /// </summary>
    public static bool IsInRange(Vector3 a, Vector3 b, float range)
    {
        return FlatDistance(a, b) <= range;
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРєР° РЅР°С…РѕРґСЏС‚СЃСЏ Р»Рё РґРІР° GameObject РІ РїСЂРµРґРµР»Р°С… РґРёСЃС‚Р°РЅС†РёРё
    /// </summary>
    public static bool IsInRange(GameObject a, GameObject b, float range)
    {
        if (a == null || b == null) return false;
        return IsInRange(a.transform.position, b.transform.position, range);
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РЅР°РїСЂР°РІР»РµРЅРёРµ РѕС‚ РѕРґРЅРѕР№ С‚РѕС‡РєРё Рє РґСЂСѓРіРѕР№ (РЅРѕСЂРјР°Р»РёР·РѕРІР°РЅРЅС‹Р№ РІРµРєС‚РѕСЂ РЅР° XZ РїР»РѕСЃРєРѕСЃС‚Рё)
    /// </summary>
    public static Vector3 GetFlatDirection(Vector3 from, Vector3 to)
    {
        Vector3 direction = to - from;
        direction.y = 0;
        return direction.normalized;
    }

    // ========================================================================
    // COLOR UTILITIES
    // ========================================================================

    /// <summary>
    /// РЎРѕР·РґР°С‚СЊ С†РІРµС‚ СЃ РёР·РјРµРЅРµРЅРЅРѕР№ Р°Р»СЊС„РѕР№
    /// </summary>
    public static Color WithAlpha(Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
    }

    /// <summary>
    /// РџР»Р°РІРЅС‹Р№ РїРµСЂРµС…РѕРґ РјРµР¶РґСѓ РґРІСѓРјСЏ С†РІРµС‚Р°РјРё
    /// </summary>
    public static Color LerpColor(Color from, Color to, float t)
    {
        return Color.Lerp(from, to, Mathf.Clamp01(t));
    }

    // ========================================================================
    // GAMEOBJECT UTILITIES
    // ========================================================================

    /// <summary>
    /// Р‘РµР·РѕРїР°СЃРЅРѕРµ СѓРЅРёС‡С‚РѕР¶РµРЅРёРµ GameObject
    /// </summary>
    public static void SafeDestroy(GameObject obj)
    {
        if (obj != null && !ReferenceEquals(obj, null))
        {
            Object.Destroy(obj);
        }
    }

    /// <summary>
    /// Р‘РµР·РѕРїР°СЃРЅРѕРµ РЅРµРјРµРґР»РµРЅРЅРѕРµ СѓРЅРёС‡С‚РѕР¶РµРЅРёРµ GameObject
    /// </summary>
    public static void SafeDestroyImmediate(GameObject obj)
    {
        if (obj != null && !ReferenceEquals(obj, null))
        {
            Object.DestroyImmediate(obj);
        }
    }

    /// <summary>
    /// Р‘РµР·РѕРїР°СЃРЅР°СЏ СѓСЃС‚Р°РЅРѕРІРєР° Р°РєС‚РёРІРЅРѕСЃС‚Рё GameObject
    /// </summary>
    public static void SafeSetActive(GameObject obj, bool active)
    {
        if (obj != null && !ReferenceEquals(obj, null))
        {
            obj.SetActive(active);
        }
    }

    /// <summary>
    /// РќР°Р№С‚Рё РІСЃРµ РґРѕС‡РµСЂРЅРёРµ GameObject СЃ РѕРїСЂРµРґРµР»РµРЅРЅС‹Рј С‚РµРіРѕРј
    /// </summary>
    public static List<GameObject> FindChildrenWithTag(GameObject parent, string tag)
    {
        List<GameObject> result = new List<GameObject>();

        if (parent == null) return result;

        foreach (Transform child in parent.transform)
        {
            if (child.CompareTag(tag))
            {
                result.Add(child.gameObject);
            }

            // Р РµРєСѓСЂСЃРёРІРЅС‹Р№ РїРѕРёСЃРє РІ РґРµС‚СЏС…
            result.AddRange(FindChildrenWithTag(child.gameObject, tag));
        }

        return result;
    }

    // ========================================================================
    // MATH UTILITIES
    // ========================================================================

    /// <summary>
    /// РћРіСЂР°РЅРёС‡РёС‚СЊ Р·РЅР°С‡РµРЅРёРµ РІ РїСЂРµРґРµР»Р°С… min-max
    /// </summary>
    public static float Clamp(float value, float min, float max)
    {
        return Mathf.Clamp(value, min, max);
    }

    /// <summary>
    /// РќРѕСЂРјР°Р»РёР·РѕРІР°С‚СЊ Р·РЅР°С‡РµРЅРёРµ РІ РґРёР°РїР°Р·РѕРЅРµ 0-1
    /// </summary>
    public static float Normalize(float value, float min, float max)
    {
        if (max - min == 0) return 0;
        return Mathf.Clamp01((value - min) / (max - min));
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРєР° РїСЂРёРјРµСЂРЅРѕРіРѕ СЂР°РІРµРЅСЃС‚РІР° С‡РёСЃРµР»
    /// </summary>
    public static bool Approximately(float a, float b, float epsilon = 0.001f)
    {
        return Mathf.Abs(a - b) < epsilon;
    }

    // ========================================================================
    // LAYER & TAG UTILITIES
    // ========================================================================

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ РЅР°С…РѕРґРёС‚СЃСЏ Р»Рё GameObject РЅР° РѕРїСЂРµРґРµР»РµРЅРЅРѕРј СЃР»РѕРµ
    /// </summary>
    public static bool IsOnLayer(GameObject obj, int layer)
    {
        if (obj == null) return false;
        return obj.layer == layer;
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ РІС…РѕРґРёС‚ Р»Рё GameObject РІ LayerMask
    /// </summary>
    public static bool IsInLayerMask(GameObject obj, LayerMask layerMask)
    {
        if (obj == null) return false;
        return (layerMask.value & (1 << obj.layer)) != 0;
    }

    // ========================================================================
    // TIME UTILITIES
    // ========================================================================

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ РїСЂРѕС€Р»Рѕ Р»Рё РґРѕСЃС‚Р°С‚РѕС‡РЅРѕ РІСЂРµРјРµРЅРё СЃ РїРѕСЃР»РµРґРЅРµРіРѕ РґРµР№СЃС‚РІРёСЏ
    /// </summary>
    public static bool HasElapsed(float lastTime, float interval)
    {
        return Time.time >= lastTime + interval;
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РїСЂРѕС†РµРЅС‚ РїСЂРѕРіСЂРµСЃСЃР° РјРµР¶РґСѓ РґРІСѓРјСЏ РІСЂРµРјРµРЅРЅС‹РјРё РјРµС‚РєР°РјРё
    /// </summary>
    public static float GetTimeProgress(float startTime, float duration)
    {
        float elapsed = Time.time - startTime;
        return Mathf.Clamp01(elapsed / duration);
    }
}
