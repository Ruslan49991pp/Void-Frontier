using UnityEngine;

/// <summary>
/// Компонент для поворота объекта лицом к камере (billboard эффект)
/// Используется для полос прогресса строительства
/// </summary>
public class Billboard : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        // Находим основную камеру
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogWarning("[Billboard] Main camera not found!");
        }
    }

    void LateUpdate()
    {
        if (mainCamera == null)
        {
            // Пытаемся найти камеру снова если она была null
            mainCamera = Camera.main;
            if (mainCamera == null)
                return;
        }

        // Поворачиваем объект лицом к камере
        transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                        mainCamera.transform.rotation * Vector3.up);
    }
}
