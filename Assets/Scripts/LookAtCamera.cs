using UnityEngine;

/// <summary>
/// Компонент для поворота объекта к камере (billboard эффект)
/// Идеально подходит для текста урона, иконок и других UI элементов в 3D пространстве
/// </summary>
public class LookAtCamera : MonoBehaviour
{
    [Header("Look At Camera Settings")]
    public bool lookAtCameraOnUpdate = true;    // Обновлять поворот каждый кадр
    public bool freezeYAxis = false;            // Заморозить поворот по Y оси
    public bool freezeXAxis = false;            // Заморозить поворот по X оси
    public bool freezeZAxis = false;            // Заморозить поворот по Z оси
    public bool useCameraUp = true;             // Использовать up вектор камеры
    public bool invertDirection = false;        // Инвертировать направление (смотреть от камеры)

    [Header("Performance")]
    public float updateInterval = 0.02f;        // Интервал обновления (для оптимизации)

    private Camera targetCamera;
    private float lastUpdateTime;
    private Vector3 lastCameraPosition;

    void Start()
    {
        // Находим камеру
        FindTargetCamera();

        // Выполняем первоначальный поворот
        if (targetCamera != null)
        {
            LookAtCameraImmediate();
        }
    }

    void Update()
    {
        if (!lookAtCameraOnUpdate || targetCamera == null)
            return;

        // Оптимизация: обновляем не каждый кадр, а с интервалом
        if (Time.time - lastUpdateTime < updateInterval)
            return;

        // Оптимизация: проверяем, сдвинулась ли камера
        if (Vector3.Distance(targetCamera.transform.position, lastCameraPosition) < 0.1f)
            return;

        LookAtCameraImmediate();
        lastUpdateTime = Time.time;
        lastCameraPosition = targetCamera.transform.position;
    }

    /// <summary>
    /// Найти целевую камеру
    /// </summary>
    void FindTargetCamera()
    {
        // Сначала пытаемся найти главную камеру
        targetCamera = Camera.main;

        // Если главной камеры нет, ищем любую активную камеру
        if (targetCamera == null)
        {
            Camera[] cameras = FindObjectsOfType<Camera>();
            foreach (Camera cam in cameras)
            {
                if (cam.enabled && cam.gameObject.activeInHierarchy)
                {
                    targetCamera = cam;
                    break;
                }
            }
        }

    }

    /// <summary>
    /// Немедленно повернуть к камере
    /// </summary>
    public void LookAtCameraImmediate()
    {
        if (targetCamera == null)
        {
            FindTargetCamera();
            if (targetCamera == null)
                return;
        }

        // Рассчитываем направление к камере
        Vector3 directionToCamera = targetCamera.transform.position - transform.position;

        // Инвертируем направление если нужно
        if (invertDirection)
        {
            directionToCamera = -directionToCamera;
        }

        // Применяем ограничения по осям
        if (freezeXAxis) directionToCamera.x = 0;
        if (freezeYAxis) directionToCamera.y = 0;
        if (freezeZAxis) directionToCamera.z = 0;

        // Проверяем, что направление не нулевое
        if (directionToCamera.magnitude < 0.001f)
            return;

        // Используем улучшенный алгоритм billboard
        PerformBillboardRotation();
    }

    /// <summary>
    /// Выполнить правильный billboard поворот
    /// </summary>
    private void PerformBillboardRotation()
    {
        // Получаем направление от объекта к камере
        Vector3 directionToCamera = (targetCamera.transform.position - transform.position).normalized;

        // Для billboard эффекта нам нужно, чтобы объект "смотрел" на камеру
        // но при этом текст был читаемым
        Vector3 forward = -directionToCamera; // Инвертируем, чтобы текст был лицом к камере
        Vector3 up = targetCamera.transform.up; // Используем up вектор камеры

        // Применяем ограничения по осям ПОСЛЕ расчета направления
        if (freezeYAxis)
        {
            // Если заморожена Y ось, проецируем направление на горизонтальную плоскость
            forward.y = 0;
            forward = forward.normalized;
            up = Vector3.up; // Используем мировой up
        }

        if (freezeXAxis)
        {
            forward.x = 0;
            forward = forward.normalized;
        }

        if (freezeZAxis)
        {
            forward.z = 0;
            forward = forward.normalized;
        }

        // Проверяем, что направление все еще валидное
        if (forward.magnitude < 0.001f)
        {
            return;
        }

        // Создаем поворот из направления
        transform.rotation = Quaternion.LookRotation(forward, up);
    }

    /// <summary>
    /// Установить целевую камеру вручную
    /// </summary>
    public void SetTargetCamera(Camera camera)
    {
        targetCamera = camera;
        if (camera != null)
        {
            LookAtCameraImmediate();
        }
    }

    /// <summary>
    /// Включить/выключить автоматическое обновление
    /// </summary>
    public void SetAutoUpdate(bool enabled)
    {
        lookAtCameraOnUpdate = enabled;
    }

    /// <summary>
    /// Заморозить поворот по определенным осям
    /// </summary>
    public void SetAxisConstraints(bool freezeX, bool freezeY, bool freezeZ)
    {
        freezeXAxis = freezeX;
        freezeYAxis = freezeY;
        freezeZAxis = freezeZ;

        if (targetCamera != null)
        {
            LookAtCameraImmediate();
        }
    }

    void OnDrawGizmosSelected()
    {
        if (targetCamera != null)
        {
            // Показываем линию к камере
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetCamera.transform.position);

            // Показываем направление взгляда
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.forward * 2f);
        }
    }

    #region Статические методы для удобства

    /// <summary>
    /// Добавить компонент LookAtCamera к объекту и сразу настроить его
    /// </summary>
    public static LookAtCamera AddToGameObject(GameObject target, bool freezeY = false)
    {
        LookAtCamera component = target.GetComponent<LookAtCamera>();
        if (component == null)
        {
            component = target.AddComponent<LookAtCamera>();
        }

        component.freezeYAxis = freezeY; // Часто полезно для текста
        component.LookAtCameraImmediate();

        return component;
    }

    /// <summary>
    /// Создать объект с текстом, который всегда смотрит на камеру
    /// </summary>
    public static GameObject CreateBillboardText(string text, Vector3 position, Color color, int fontSize = 10)
    {
        GameObject textObject = new GameObject("BillboardText");
        textObject.transform.position = position;

        // Добавляем TextMesh
        TextMesh textMesh = textObject.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.fontSize = fontSize;
        textMesh.color = color;
        textMesh.anchor = TextAnchor.MiddleCenter;

        // Добавляем LookAtCamera
        LookAtCamera lookAt = textObject.AddComponent<LookAtCamera>();
        lookAt.freezeYAxis = false; // Разрешаем поворот по всем осям
        lookAt.LookAtCameraImmediate();

        return textObject;
    }

    #endregion
}