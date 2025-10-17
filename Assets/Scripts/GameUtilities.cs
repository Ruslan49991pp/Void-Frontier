using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// GameUtilities - коллекция утилитарных методов для общих операций
///
/// CODE QUALITY: Централизация дублирующегося кода:
/// 1. Уменьшает дублирование кода (DRY principle)
/// 2. Обеспечивает единообразие операций
/// 3. Упрощает тестирование
/// 4. Улучшает читаемость кода
///
/// ИСПОЛЬЗОВАНИЕ:
///   // Безопасное получение компонента
///   Character character = GameUtils.GetComponentSafe<Character>(gameObject);
///
///   // Проверка дистанции
///   if (GameUtils.IsInRange(player, enemy, attackRange)) { ... }
///
///   // Создание 2D вектора из 3D
///   Vector2 flatPos = GameUtils.To2D(worldPosition);
/// </summary>
public static class GameUtils
{
    // ========================================================================
    // COMPONENT UTILITIES
    // ========================================================================

    /// <summary>
    /// Безопасное получение компонента с логированием ошибки
    /// </summary>
    public static T GetComponentSafe<T>(GameObject obj, bool logError = true) where T : Component
    {
        if (obj == null)
        {
            if (logError)
                Debug.LogError($"[GameUtils] Cannot get component {typeof(T).Name} - GameObject is null");
            return null;
        }

        T component = obj.GetComponent<T>();
        if (component == null && logError)
        {
            Debug.LogError($"[GameUtils] Component {typeof(T).Name} not found on {obj.name}");
        }

        return component;
    }

    /// <summary>
    /// Безопасное получение компонента в родителях
    /// </summary>
    public static T GetComponentInParentSafe<T>(GameObject obj, bool logError = true) where T : Component
    {
        if (obj == null)
        {
            if (logError)
                Debug.LogError($"[GameUtils] Cannot get component {typeof(T).Name} - GameObject is null");
            return null;
        }

        T component = obj.GetComponentInParent<T>();
        if (component == null && logError)
        {
            Debug.LogError($"[GameUtils] Component {typeof(T).Name} not found in parents of {obj.name}");
        }

        return component;
    }

    /// <summary>
    /// Безопасное получение компонента в детях
    /// </summary>
    public static T GetComponentInChildrenSafe<T>(GameObject obj, bool logError = true) where T : Component
    {
        if (obj == null)
        {
            if (logError)
                Debug.LogError($"[GameUtils] Cannot get component {typeof(T).Name} - GameObject is null");
            return null;
        }

        T component = obj.GetComponentInChildren<T>();
        if (component == null && logError)
        {
            Debug.LogError($"[GameUtils] Component {typeof(T).Name} not found in children of {obj.name}");
        }

        return component;
    }

    // ========================================================================
    // NULL CHECK UTILITIES
    // ========================================================================

    /// <summary>
    /// Проверка что GameObject не уничтожен (Unity null check)
    /// Unity уничтоженные объекты могут быть != null, но ReferenceEquals дает правду
    /// </summary>
    public static bool IsDestroyed(Object obj)
    {
        return ReferenceEquals(obj, null) || obj == null;
    }

    /// <summary>
    /// Проверка что GameObject жив и активен
    /// </summary>
    public static bool IsAliveAndActive(GameObject obj)
    {
        return obj != null && !ReferenceEquals(obj, null) && obj.activeInHierarchy;
    }

    // ========================================================================
    // POSITION & DISTANCE UTILITIES
    // ========================================================================

    /// <summary>
    /// Конвертация 3D позиции в 2D (XZ плоскость)
    /// </summary>
    public static Vector2 To2D(Vector3 position)
    {
        return new Vector2(position.x, position.z);
    }

    /// <summary>
    /// Конвертация 2D позиции в 3D (XZ плоскость, Y = 0)
    /// </summary>
    public static Vector3 To3D(Vector2 position, float y = 0f)
    {
        return new Vector3(position.x, y, position.y);
    }

    /// <summary>
    /// Плоская дистанция между двумя объектами (игнорирует Y)
    /// </summary>
    public static float FlatDistance(Vector3 a, Vector3 b)
    {
        Vector2 flatA = To2D(a);
        Vector2 flatB = To2D(b);
        return Vector2.Distance(flatA, flatB);
    }

    /// <summary>
    /// Плоская дистанция между двумя GameObject
    /// </summary>
    public static float FlatDistance(GameObject a, GameObject b)
    {
        if (a == null || b == null) return float.MaxValue;
        return FlatDistance(a.transform.position, b.transform.position);
    }

    /// <summary>
    /// Проверка находятся ли две позиции в пределах дистанции
    /// </summary>
    public static bool IsInRange(Vector3 a, Vector3 b, float range)
    {
        return FlatDistance(a, b) <= range;
    }

    /// <summary>
    /// Проверка находятся ли два GameObject в пределах дистанции
    /// </summary>
    public static bool IsInRange(GameObject a, GameObject b, float range)
    {
        if (a == null || b == null) return false;
        return IsInRange(a.transform.position, b.transform.position, range);
    }

    /// <summary>
    /// Получить направление от одной точки к другой (нормализованный вектор на XZ плоскости)
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
    /// Создать цвет с измененной альфой
    /// </summary>
    public static Color WithAlpha(Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
    }

    /// <summary>
    /// Плавный переход между двумя цветами
    /// </summary>
    public static Color LerpColor(Color from, Color to, float t)
    {
        return Color.Lerp(from, to, Mathf.Clamp01(t));
    }

    // ========================================================================
    // GAMEOBJECT UTILITIES
    // ========================================================================

    /// <summary>
    /// Безопасное уничтожение GameObject
    /// </summary>
    public static void SafeDestroy(GameObject obj)
    {
        if (obj != null && !ReferenceEquals(obj, null))
        {
            Object.Destroy(obj);
        }
    }

    /// <summary>
    /// Безопасное немедленное уничтожение GameObject
    /// </summary>
    public static void SafeDestroyImmediate(GameObject obj)
    {
        if (obj != null && !ReferenceEquals(obj, null))
        {
            Object.DestroyImmediate(obj);
        }
    }

    /// <summary>
    /// Безопасная установка активности GameObject
    /// </summary>
    public static void SafeSetActive(GameObject obj, bool active)
    {
        if (obj != null && !ReferenceEquals(obj, null))
        {
            obj.SetActive(active);
        }
    }

    /// <summary>
    /// Найти все дочерние GameObject с определенным тегом
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

            // Рекурсивный поиск в детях
            result.AddRange(FindChildrenWithTag(child.gameObject, tag));
        }

        return result;
    }

    // ========================================================================
    // MATH UTILITIES
    // ========================================================================

    /// <summary>
    /// Ограничить значение в пределах min-max
    /// </summary>
    public static float Clamp(float value, float min, float max)
    {
        return Mathf.Clamp(value, min, max);
    }

    /// <summary>
    /// Нормализовать значение в диапазоне 0-1
    /// </summary>
    public static float Normalize(float value, float min, float max)
    {
        if (max - min == 0) return 0;
        return Mathf.Clamp01((value - min) / (max - min));
    }

    /// <summary>
    /// Проверка примерного равенства чисел
    /// </summary>
    public static bool Approximately(float a, float b, float epsilon = 0.001f)
    {
        return Mathf.Abs(a - b) < epsilon;
    }

    // ========================================================================
    // LAYER & TAG UTILITIES
    // ========================================================================

    /// <summary>
    /// Проверить находится ли GameObject на определенном слое
    /// </summary>
    public static bool IsOnLayer(GameObject obj, int layer)
    {
        if (obj == null) return false;
        return obj.layer == layer;
    }

    /// <summary>
    /// Проверить входит ли GameObject в LayerMask
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
    /// Проверить прошло ли достаточно времени с последнего действия
    /// </summary>
    public static bool HasElapsed(float lastTime, float interval)
    {
        return Time.time >= lastTime + interval;
    }

    /// <summary>
    /// Получить процент прогресса между двумя временными метками
    /// </summary>
    public static float GetTimeProgress(float startTime, float duration)
    {
        float elapsed = Time.time - startTime;
        return Mathf.Clamp01(elapsed / duration);
    }
}
