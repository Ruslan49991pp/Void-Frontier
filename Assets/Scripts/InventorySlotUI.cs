using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI элемент для отображения слота инвентаря
/// </summary>
public class InventorySlotUI : MonoBehaviour
{
    [Header("UI Components")]
    public Image backgroundImage;
    public Image itemIcon;
    public Text quantityText;
    public Button slotButton;

    [Header("Visual Settings")]
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    public Color selectedColor = new Color(0.5f, 0.7f, 1f, 0.8f);
    public Color emptyColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);

    // События
    public System.Action<int> OnSlotClicked;
    public System.Action<int> OnSlotRightClicked;

    // Внутренние переменные
    private int slotIndex = -1;
    private bool isSelected = false;
    private InventorySlot currentSlot;

    /// <summary>
    /// Инициализация компонентов слота
    /// </summary>
    public void Initialize(Image background, Image icon, Text quantity, Button button)
    {
        backgroundImage = background;
        itemIcon = icon;
        quantityText = quantity;
        slotButton = button;

        // Настраиваем обработчики событий
        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnSlotClick);
        }

        // Обработчик правого клика
        var eventTrigger = gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        }

        var rightClickEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
        rightClickEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick;
        rightClickEntry.callback.AddListener((data) => {
            var pointerData = (UnityEngine.EventSystems.PointerEventData)data;
            if (pointerData.button == UnityEngine.EventSystems.PointerEventData.InputButton.Right)
            {
                OnSlotRightClick();
            }
        });
        eventTrigger.triggers.Add(rightClickEntry);

        // Изначально пустой слот
        UpdateSlot(null);
    }

    /// <summary>
    /// Установить индекс слота
    /// </summary>
    public void SetSlotIndex(int index)
    {
        slotIndex = index;
    }

    /// <summary>
    /// Обновить отображение слота
    /// </summary>
    public void UpdateSlot(InventorySlot slot)
    {
        currentSlot = slot;

        if (slot == null || slot.IsEmpty())
        {
            // Пустой слот
            SetEmptySlot();
        }
        else
        {
            // Заполненный слот
            SetFilledSlot(slot);
        }

        // Обновляем цвет фона
        UpdateBackgroundColor();
    }

    /// <summary>
    /// Настроить пустой слот
    /// </summary>
    void SetEmptySlot()
    {
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.color = Color.clear;
        }

        if (quantityText != null)
        {
            quantityText.text = "";
        }
    }

    /// <summary>
    /// Настроить заполненный слот
    /// </summary>
    void SetFilledSlot(InventorySlot slot)
    {
        if (itemIcon != null)
        {
            // Устанавливаем иконку предмета
            if (slot.itemData.icon != null)
            {
                itemIcon.sprite = slot.itemData.icon;
                itemIcon.color = Color.white;
            }
            else
            {
                // Создаем простую иконку на основе типа предмета
                itemIcon.sprite = CreateSimpleIcon(slot.itemData);
                itemIcon.color = slot.itemData.GetRarityColor();
            }
        }

        if (quantityText != null)
        {
            // Показываем количество только если больше 1
            if (slot.quantity > 1)
            {
                quantityText.text = slot.quantity.ToString();
            }
            else
            {
                quantityText.text = "";
            }
        }
    }

    /// <summary>
    /// Создать простую иконку для предмета без спрайта
    /// </summary>
    Sprite CreateSimpleIcon(ItemData itemData)
    {
        // Создаем простую текстурку с символом в зависимости от типа
        Texture2D texture = new Texture2D(32, 32);
        Color32[] colors = new Color32[32 * 32];

        // Определяем цвет на основе типа предмета
        Color32 itemColor = GetItemTypeColor(itemData.itemType);

        // Заполняем простым квадратом
        for (int i = 0; i < colors.Length; i++)
        {
            int x = i % 32;
            int y = i / 32;

            // Создаем простую рамку
            if (x < 2 || x >= 30 || y < 2 || y >= 30)
            {
                colors[i] = Color.white;
            }
            else
            {
                colors[i] = itemColor;
            }
        }

        texture.SetPixels32(colors);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
    }

    /// <summary>
    /// Получить цвет для типа предмета
    /// </summary>
    Color32 GetItemTypeColor(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Weapon: return new Color32(255, 100, 100, 255);
            case ItemType.Armor: return new Color32(100, 100, 255, 255);
            case ItemType.Tool: return new Color32(200, 200, 100, 255);
            case ItemType.Medical: return new Color32(100, 255, 100, 255);
            case ItemType.Resource: return new Color32(150, 100, 50, 255);
            case ItemType.Consumable: return new Color32(255, 200, 100, 255);
            default: return new Color32(128, 128, 128, 255);
        }
    }

    /// <summary>
    /// Установить состояние выделения
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateBackgroundColor();
    }

    /// <summary>
    /// Обновить цвет фона
    /// </summary>
    void UpdateBackgroundColor()
    {
        if (backgroundImage == null) return;

        if (isSelected)
        {
            backgroundImage.color = selectedColor;
        }
        else if (currentSlot == null || currentSlot.IsEmpty())
        {
            backgroundImage.color = emptyColor;
        }
        else
        {
            backgroundImage.color = normalColor;
        }
    }

    /// <summary>
    /// Обработчик левого клика
    /// </summary>
    void OnSlotClick()
    {
        OnSlotClicked?.Invoke(slotIndex);
    }

    /// <summary>
    /// Обработчик правого клика
    /// </summary>
    void OnSlotRightClick()
    {
        OnSlotRightClicked?.Invoke(slotIndex);
    }

    /// <summary>
    /// Получить информацию о слоте для отладки
    /// </summary>
    public string GetSlotInfo()
    {
        if (currentSlot == null || currentSlot.IsEmpty())
        {
            return $"Slot {slotIndex}: Empty";
        }

        return $"Slot {slotIndex}: {currentSlot.itemData.itemName} x{currentSlot.quantity}";
    }

    void OnDestroy()
    {
        // Очищаем события
        OnSlotClicked = null;
        OnSlotRightClicked = null;
    }
}