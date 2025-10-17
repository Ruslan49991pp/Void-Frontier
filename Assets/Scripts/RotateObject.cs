using UnityEngine;

/// <summary>
/// Простой компонент для медленного вращения объекта
/// Используется для ресурсов на локации, чтобы их было легче заметить
/// </summary>
public class RotateObject : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Скорость вращения по оси Y (градусов в секунду)")]
    public float rotationSpeed = 30f;

    [Tooltip("Вращать вокруг оси X")]
    public bool rotateX = false;

    [Tooltip("Вращать вокруг оси Y")]
    public bool rotateY = true;

    [Tooltip("Вращать вокруг оси Z")]
    public bool rotateZ = false;

    [Header("Optional Bobbing")]
    [Tooltip("Добавить плавное движение вверх-вниз")]
    public bool enableBobbing = false;

    [Tooltip("Амплитуда движения (высота)")]
    public float bobbingAmount = 0.1f;

    [Tooltip("Скорость движения вверх-вниз")]
    public float bobbingSpeed = 1f;

    // Внутренние переменные
    private Vector3 startPosition;
    private float bobbingTimer;

    void Start()
    {
        // Запоминаем начальную позицию для bobbing эффекта
        startPosition = transform.position;
    }

    void Update()
    {
        // Вращение
        float rotationAmount = rotationSpeed * Time.deltaTime;
        Vector3 rotation = new Vector3(
            rotateX ? rotationAmount : 0f,
            rotateY ? rotationAmount : 0f,
            rotateZ ? rotationAmount : 0f
        );
        transform.Rotate(rotation, Space.World);

        // Плавное движение вверх-вниз (опционально)
        if (enableBobbing)
        {
            bobbingTimer += Time.deltaTime * bobbingSpeed;
            float yOffset = Mathf.Sin(bobbingTimer) * bobbingAmount;
            transform.position = startPosition + new Vector3(0, yOffset, 0);
        }
    }
}
