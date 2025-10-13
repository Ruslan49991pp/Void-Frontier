using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI элемент для одного слота строительства
/// </summary>
public class BuildSlotUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText; // Используется для отображения Cost + Size
    public Button button;

    [Header("Data")]
    public int buildingIndex = -1;

    private ShipBuildingSystem buildingSystem;

    void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.AddListener(OnSlotClicked);
        }
    }

    /// <summary>
    /// Установить данные слота
    /// </summary>
    public void SetData(int index, RoomData roomData, Sprite icon = null)
    {
        buildingIndex = index;

        if (nameText != null)
        {
            nameText.text = roomData.roomName;
        }

        if (costText != null)
        {
            costText.text = $"Cost: ${roomData.cost} | Size: {roomData.size.x}x{roomData.size.y}";
        }

        if (iconImage != null && icon != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = true;
        }
        else if (iconImage != null)
        {
            iconImage.enabled = false;
        }

        Debug.Log($"[BuildSlotUI] Slot configured: {roomData.roomName}, index: {index}");
    }

    /// <summary>
    /// Обработчик клика по слоту
    /// </summary>
    void OnSlotClicked()
    {
        if (buildingIndex < 0)
        {
            Debug.LogWarning("[BuildSlotUI] Invalid building index!");
            return;
        }

        Debug.Log($"[BuildSlotUI] Slot clicked, building index: {buildingIndex}");

        // Находим систему строительства
        if (buildingSystem == null)
        {
            buildingSystem = FindObjectOfType<ShipBuildingSystem>();
        }

        if (buildingSystem != null)
        {
            // Устанавливаем выбранный тип комнаты
            buildingSystem.SetSelectedRoomType(buildingIndex);
            // Активируем режим строительства
            buildingSystem.SetBuildMode(true);

            Debug.Log($"[BuildSlotUI] Building mode activated for room index: {buildingIndex}");

            // Закрываем меню строительства
            BuildMenuManager menuManager = FindObjectOfType<BuildMenuManager>();
            if (menuManager != null)
            {
                // Симулируем клик по CloseButton
                menuManager.CloseBuildMenu();
            }
        }
        else
        {
            Debug.LogError("[BuildSlotUI] ShipBuildingSystem not found!");
        }
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnSlotClicked);
        }
    }
}
