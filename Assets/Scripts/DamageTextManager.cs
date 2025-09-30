using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

/// <summary>
/// Глобальный менеджер для отслеживания и принудительной очистки всех объектов текста урона
/// </summary>
public class DamageTextManager : MonoBehaviour
{
    private static DamageTextManager instance;
    private List<GameObject> activeDamageTexts = new List<GameObject>();
    private List<Coroutine> activeCoroutines = new List<Coroutine>();

    public static DamageTextManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("DamageTextManager");
                instance = go.AddComponent<DamageTextManager>();
                DontDestroyOnLoad(go);
                Debug.Log("[DamageTextManager] Created singleton instance");
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // Запускаем корутину для периодической очистки
            StartCoroutine(PeriodicCleanup());
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Зарегистрировать новый объект текста урона
    /// </summary>
    public void RegisterDamageText(GameObject damageTextObj, Coroutine animationCoroutine)
    {
        if (damageTextObj != null)
        {
            activeDamageTexts.Add(damageTextObj);
            activeCoroutines.Add(animationCoroutine);
            Debug.Log($"[DamageTextManager] Registered {damageTextObj.name}. Total active: {activeDamageTexts.Count}");
        }
    }

    /// <summary>
    /// Отменить регистрацию объекта текста урона
    /// </summary>
    public void UnregisterDamageText(GameObject damageTextObj)
    {
        int index = activeDamageTexts.IndexOf(damageTextObj);
        if (index >= 0)
        {
            activeDamageTexts.RemoveAt(index);
            if (index < activeCoroutines.Count)
            {
                activeCoroutines.RemoveAt(index);
            }
            Debug.Log($"[DamageTextManager] Unregistered {damageTextObj?.name}. Total active: {activeDamageTexts.Count}");
        }
    }

    /// <summary>
    /// Принудительно очистить конкретный объект
    /// </summary>
    public void ForceCleanupObject(GameObject damageTextObj)
    {
        if (damageTextObj == null)
        {
            Debug.Log("[DamageTextManager] ForceCleanupObject called with null object - skipping");
            return;
        }

        // Проверяем, не уничтожен ли уже объект
        try
        {
            if (damageTextObj.transform == null)
            {
                Debug.Log($"[DamageTextManager] Object {damageTextObj.name} already destroyed - skipping cleanup");
                return;
            }
        }
        catch (System.Exception)
        {
            Debug.Log("[DamageTextManager] Object already destroyed (exception caught) - skipping cleanup");
            // Убираем из списков и выходим
            UnregisterDamageText(damageTextObj);
            return;
        }

        Debug.Log($"[DamageTextManager] Force cleanup: {damageTextObj.name}");

        // Останавливаем корутину если она есть
        int index = activeDamageTexts.IndexOf(damageTextObj);
        if (index >= 0 && index < activeCoroutines.Count && activeCoroutines[index] != null)
        {
            StopCoroutine(activeCoroutines[index]);
            Debug.Log($"[DamageTextManager] Stopped coroutine for {damageTextObj.name}");
        }

        // Принудительная очистка
        CleanupDamageTextObject(damageTextObj);

        // Убираем из списков
        UnregisterDamageText(damageTextObj);
    }

    /// <summary>
    /// Очистить конкретный объект полностью
    /// </summary>
    private void CleanupDamageTextObject(GameObject obj)
    {
        if (obj == null) return;

        // Проверяем, не уничтожен ли уже объект
        try
        {
            if (obj.transform == null)
            {
                Debug.Log($"[DamageTextManager] CleanupDamageTextObject: Object already destroyed");
                return;
            }
        }
        catch (System.Exception)
        {
            Debug.Log("[DamageTextManager] CleanupDamageTextObject: Object already destroyed (exception caught)");
            return;
        }

        try
        {
            // Отключаем все компоненты
            var allComponents = obj.GetComponents<Component>();
            foreach (var component in allComponents)
            {
                if (component != null && !(component is Transform))
                {
                    if (component is MonoBehaviour monoBehaviour)
                    {
                        monoBehaviour.enabled = false;
                    }
                }
            }

            // Уничтожаем все дочерние объекты
            for (int i = obj.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = obj.transform.GetChild(i);
                if (child != null)
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            // Уничтожаем основной объект
            DestroyImmediate(obj);
            Debug.Log($"[DamageTextManager] Successfully destroyed object {obj.name}");
        }
        catch (System.Exception ex)
        {
            Debug.Log($"[DamageTextManager] Exception during cleanup: {ex.Message} - object likely already destroyed");
        }
    }

    /// <summary>
    /// Периодическая очистка "мертвых" объектов
    /// </summary>
    private IEnumerator PeriodicCleanup()
    {
        while (true)
        {
            yield return new WaitForSeconds(3f); // Проверяем каждые 3 секунды

            // Очищаем null объекты из списка
            for (int i = activeDamageTexts.Count - 1; i >= 0; i--)
            {
                if (activeDamageTexts[i] == null)
                {
                    Debug.Log($"[DamageTextManager] Found null object at index {i}, removing from list");
                    activeDamageTexts.RemoveAt(i);
                    if (i < activeCoroutines.Count)
                    {
                        activeCoroutines.RemoveAt(i);
                    }
                }
            }

            // Ищем все объекты урона в сцене по именам
            FindAndCleanupOrphanedDamageTexts();

            // Принудительно очищаем объекты старше 5 секунд (уменьшил время)
            List<GameObject> toCleanup = new List<GameObject>();
            foreach (var obj in activeDamageTexts)
            {
                if (obj != null)
                {
                    // Любой зарегистрированный объект старше 5 секунд - очищаем
                    toCleanup.Add(obj);
                }
            }

            foreach (var obj in toCleanup)
            {
                Debug.LogWarning($"[DamageTextManager] Force cleaning up old registered object: {obj.name}");
                ForceCleanupObject(obj);
            }

            if (activeDamageTexts.Count > 0)
            {
                Debug.Log($"[DamageTextManager] Periodic cleanup: {activeDamageTexts.Count} active damage texts remaining");
            }
        }
    }

    /// <summary>
    /// Найти и очистить все "сиротские" объекты урона в сцене
    /// </summary>
    private void FindAndCleanupOrphanedDamageTexts()
    {
        // Ищем все объекты с определенными именами
        string[] damageTextNames = { "BulletDamageText", "MeleeHitEffect", "BillboardText" };

        foreach (string name in damageTextNames)
        {
            GameObject[] foundObjects = GameObject.FindGameObjectsWithTag("Untagged")
                .Where(go => go.name.StartsWith(name)).ToArray();

            if (foundObjects.Length > 0)
            {
                Debug.LogWarning($"[DamageTextManager] Found {foundObjects.Length} orphaned {name} objects in scene!");

                foreach (var obj in foundObjects)
                {
                    Debug.Log($"[DamageTextManager] Force cleaning orphaned object: {obj.name} at {obj.transform.position}");
                    CleanupDamageTextObject(obj);
                }
            }
        }

        // Также ищем по компонентам TextMesh в воздухе
        TextMesh[] textMeshes = FindObjectsOfType<TextMesh>();
        foreach (var textMesh in textMeshes)
        {
            if (textMesh != null && textMesh.transform.position.y > 5f) // Высоко в воздухе
            {
                string text = textMesh.text;
                if (text.StartsWith("-") && char.IsDigit(text.Length > 1 ? text[1] : '0'))
                {
                    Debug.LogWarning($"[DamageTextManager] Found floating damage text: '{text}' at {textMesh.transform.position}");
                    CleanupDamageTextObject(textMesh.gameObject);
                }
            }
        }
    }

    /// <summary>
    /// Очистить все активные объекты текста урона
    /// </summary>
    public void CleanupAllDamageTexts()
    {
        Debug.Log($"[DamageTextManager] Cleaning up all {activeDamageTexts.Count} damage texts");

        // Останавливаем все корутины
        foreach (var coroutine in activeCoroutines)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }

        // Очищаем все объекты
        foreach (var obj in activeDamageTexts.ToArray())
        {
            if (obj != null)
            {
                CleanupDamageTextObject(obj);
            }
        }

        activeDamageTexts.Clear();
        activeCoroutines.Clear();
    }

    void OnDestroy()
    {
        CleanupAllDamageTexts();
    }

    /// <summary>
    /// Получить количество активных объектов урона
    /// </summary>
    public int GetActiveDamageTextCount()
    {
        return activeDamageTexts.Count;
    }

    /// <summary>
    /// Запустить анимацию текста урона на этом менеджере
    /// </summary>
    public void StartDamageTextAnimation(GameObject damageTextObj, float duration)
    {
        if (damageTextObj == null) return;

        Debug.Log($"[DamageTextManager] Starting animation for {damageTextObj.name}, duration: {duration}s");

        // Запускаем анимацию на менеджере, а не на пуле
        Coroutine animationCoroutine = StartCoroutine(AnimateDamageText(damageTextObj, duration));

        // Регистрируем для отслеживания
        RegisterDamageText(damageTextObj, animationCoroutine);

        // ПРИНУДИТЕЛЬНОЕ уничтожение ровно через заданное время
        StartCoroutine(ForceCleanupAfterDelay(damageTextObj, duration));
    }

    /// <summary>
    /// Анимация текста урона
    /// </summary>
    private IEnumerator AnimateDamageText(GameObject damageTextObj, float duration)
    {
        if (damageTextObj == null) yield break;

        Debug.Log($"[DamageTextManager] Starting damage text animation for {damageTextObj.name}, duration: {duration}s");

        TextMesh textMesh = damageTextObj.GetComponent<TextMesh>();
        if (textMesh == null)
        {
            Debug.LogWarning($"[DamageTextManager] No TextMesh found on {damageTextObj.name}");
            yield break;
        }

        Vector3 startPos = damageTextObj.transform.position;
        Vector3 endPos = new Vector3(startPos.x, 10f, startPos.z);
        Color startColor = textMesh.color;

        Debug.Log($"[DamageTextManager] Animation path: {startPos} -> {endPos}");

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            // Дополнительная проверка на уничтоженный объект
            try
            {
                if (damageTextObj == null || damageTextObj.transform == null)
                {
                    Debug.LogWarning("[DamageTextManager] damageTextObj destroyed during animation!");
                    yield break;
                }
            }
            catch (System.Exception)
            {
                Debug.LogWarning("[DamageTextManager] Exception accessing damageTextObj - object destroyed!");
                yield break;
            }

            float t = elapsedTime / duration;

            // Инициализируем переменные перед try-catch
            Color color = startColor;
            Vector3 currentPosition = Vector3.zero;

            try
            {
                // Движение вверх
                damageTextObj.transform.position = Vector3.Lerp(startPos, endPos, t);
                currentPosition = damageTextObj.transform.position;

                // Плавное исчезновение
                color = startColor;
                color.a = 1f - t;
                textMesh.color = color;

                // Увеличение размера в начале
                float scale = 1f + (0.5f * (1f - t));
                damageTextObj.transform.localScale = Vector3.one * scale;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[DamageTextManager] Exception during animation: {ex.Message} - stopping animation!");
                yield break;
            }

            elapsedTime += Time.deltaTime;

            // Логируем каждые 0.5 секунды
            if (Mathf.FloorToInt(elapsedTime * 2) > Mathf.FloorToInt((elapsedTime - Time.deltaTime) * 2))
            {
                Debug.Log($"[DamageTextManager] Animation progress: {t:F2}, position: {currentPosition}, alpha: {color.a:F2}");
            }

            yield return null;
        }

        Debug.Log($"[DamageTextManager] Animation completed for {damageTextObj.name}, elapsed time: {elapsedTime:F2}s");

        // Принудительно уничтожаем через менеджер
        if (damageTextObj != null)
        {
            Debug.Log($"[DamageTextManager] Requesting cleanup for {damageTextObj.name}");
            ForceCleanupObject(damageTextObj);
        }
    }

    /// <summary>
    /// Принудительная очистка объекта через заданное время
    /// </summary>
    private IEnumerator ForceCleanupAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (obj != null)
        {
            Debug.Log($"[DamageTextManager] MANDATORY cleanup after {delay}s for {obj.name}");
            ForceCleanupObject(obj);
        }
    }
}