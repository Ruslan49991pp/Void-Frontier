using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// Компонент для портрета персонажа - следование камеры при удержании ЛКМ
/// </summary>
public class PortraitCameraFollow : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private Character character;
    private CameraController cameraController;
    private Coroutine followCoroutine;

    [Tooltip("Время удержания кнопки (в секундах) перед началом следования")]
    public float holdThreshold = 0.1f;

    public void Initialize(Character character)
    {
        this.character = character;
    }

    void Start()
    {
        // Находим CameraController
        cameraController = FindObjectOfType<CameraController>();
        if (cameraController == null)
        {
            Debug.LogWarning("[PortraitCameraFollow] CameraController not found in scene!");
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // При нажатии ЛКМ запускаем корутину с задержкой
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (followCoroutine != null)
            {
                StopCoroutine(followCoroutine);
            }
            followCoroutine = StartCoroutine(StartFollowingAfterDelay());
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // При отпускании ЛКМ останавливаем корутину и следование
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Отменяем запуск следования если кнопка отпущена до истечения задержки
            if (followCoroutine != null)
            {
                StopCoroutine(followCoroutine);
                followCoroutine = null;
            }

            // Останавливаем следование если оно было активно
            if (cameraController != null)
            {
                cameraController.StopFollowingTarget();
            }
        }
    }

    /// <summary>
    /// Корутина для запуска следования с задержкой
    /// </summary>
    IEnumerator StartFollowingAfterDelay()
    {
        // Ждем указанное время
        yield return new WaitForSeconds(holdThreshold);

        // Если дошли сюда, значит кнопка удерживается достаточно долго
        if (character != null && cameraController != null)
        {
            cameraController.StartFollowingTarget(character.transform);
        }

        followCoroutine = null;
    }

    void OnDisable()
    {
        // Останавливаем следование при отключении компонента
        if (cameraController != null)
        {
            cameraController.StopFollowingTarget();
        }

        if (followCoroutine != null)
        {
            StopCoroutine(followCoroutine);
            followCoroutine = null;
        }
    }
}
