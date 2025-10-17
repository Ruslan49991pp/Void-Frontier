using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Компонент для портрета персонажа - следование камеры при зажатии ЛКМ
/// </summary>
public class PortraitCameraFollow : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private Character character;
    private CameraController cameraController;

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
        // При зажатии ЛКМ начинаем следование камеры
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (character != null && cameraController != null)
            {
                cameraController.StartFollowingTarget(character.transform);
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // При отпускании ЛКМ останавливаем следование
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (cameraController != null)
            {
                cameraController.StopFollowingTarget();
            }
        }
    }
}
