using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// UI элемент для отображения слота инвентаря
/// </summary>
public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
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
    public System.Action<int> OnSlotDoubleClicked;
    public System.Action<int, int> OnSlotDragAndDrop; // from, to

    // Внутренние переменные
    private int slotIndex = -1;
    private bool isSelected = false;
    private InventorySlot currentSlot;
    private EquipmentSlot equipmentSlot = EquipmentSlot.None;
    private bool isEquipmentSlot = false;

    // Для обработки двойного клика
    private float lastClickTime = 0f;
    private const float doubleClickTimeLimit = 0.5f;

    // Для drag and drop
    private GameObject dragIcon;
    private Canvas dragCanvas;
    private static InventorySlotUI draggedSlot;
    private CanvasGroup canvasGroup;

    /// <summary>
    /// Инициализация компонентов слота
    /// </summary>
    public void Initialize(Image background, Image icon, Text quantity, Button button)
    {
        backgroundImage = background;
        itemIcon = icon;
        quantityText = quantity;
        slotButton = button;

        // Добавляем CanvasGroup для управления прозрачностью во время перетаскивания
        canvasGroup = gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

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

        // Ищем Canvas для drag операций
        dragCanvas = GetComponentInParent<Canvas>();
        if (dragCanvas == null)
        {
            dragCanvas = FindObjectOfType<Canvas>();
        }

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
    /// Получить индекс слота
    /// </summary>
    public int GetSlotIndex()
    {
        return slotIndex;
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
        float timeSinceLastClick = Time.time - lastClickTime;


        if (timeSinceLastClick <= doubleClickTimeLimit)
        {
            // Двойной клик

            OnSlotDoubleClicked?.Invoke(slotIndex);
        }
        else
        {
            // Одиночный клик

            OnSlotClicked?.Invoke(slotIndex);
        }

        lastClickTime = Time.time;
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

    /// <summary>
    /// Установить слот как слот экипировки
    /// </summary>
    public void SetEquipmentSlot(EquipmentSlot slot)
    {
        equipmentSlot = slot;
        isEquipmentSlot = true;
    }

    /// <summary>
    /// Проверить, является ли это слотом экипировки
    /// </summary>
    public bool IsEquipmentSlot()
    {
        return isEquipmentSlot;
    }

    /// <summary>
    /// Получить тип слота экипировки
    /// </summary>
    public EquipmentSlot GetEquipmentSlot()
    {
        return equipmentSlot;
    }

    /// <summary>
    /// Получить уникальный идентификатор слота для drag and drop
    /// Для обычных слотов: slotIndex (0-19)
    /// Для слотов экипировки: 1000 + equipmentSlot (1001-1006)
    /// </summary>
    public int GetDragDropId()
    {
        if (isEquipmentSlot)
        {
            return 1000 + (int)equipmentSlot;
        }
        return slotIndex;
    }

    #region Drag and Drop Implementation

    /// <summary>
    /// Начало перетаскивания
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        // Можем перетаскивать только непустые слоты
        if (currentSlot == null || currentSlot.IsEmpty())
            return;

        draggedSlot = this;



        // Скрываем tooltip во время перетаскивания
        TooltipSystem.Instance.HideTooltip();

        // Создаем иконку для перетаскивания
        CreateDragIcon();

        // Делаем исходный слот полупрозрачным
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.6f;
            canvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// Процесс перетаскивания
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon != null && draggedSlot == this)
        {
            // Перемещаем иконку за курсором
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragCanvas.transform as RectTransform,
                eventData.position,
                dragCanvas.worldCamera,
                out localPoint
            );

            dragIcon.transform.localPosition = localPoint;
        }
    }

    /// <summary>
    /// Конец перетаскивания
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        // Восстанавливаем исходный слот
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        // Уничтожаем иконку перетаскивания
        if (dragIcon != null)
        {
            Destroy(dragIcon);
            dragIcon = null;
        }

        draggedSlot = null;
    }

    /// <summary>
    /// Сброс предмета на слот
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        if (draggedSlot != null && draggedSlot != this)
        {
            // Вызываем событие перестановки предметов используя drag-drop ID
            OnSlotDragAndDrop?.Invoke(draggedSlot.GetDragDropId(), this.GetDragDropId());
        }
    }

    /// <summary>
    /// Создать иконку для перетаскивания
    /// </summary>
    void CreateDragIcon()
    {
        if (dragCanvas == null || currentSlot == null || currentSlot.IsEmpty())
            return;

        // Создаем объект для иконки
        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(dragCanvas.transform, false);

        // Добавляем Image компонент
        Image dragImage = dragIcon.AddComponent<Image>();

        // Копируем иконку из текущего слота
        if (itemIcon != null && itemIcon.sprite != null)
        {
            dragImage.sprite = itemIcon.sprite;
            dragImage.color = itemIcon.color;
        }

        // Настраиваем размер и позицию
        RectTransform dragRect = dragIcon.GetComponent<RectTransform>();
        dragRect.sizeDelta = new Vector2(60, 60);

        // Делаем иконку полупрозрачной
        CanvasGroup dragCanvasGroup = dragIcon.AddComponent<CanvasGroup>();
        dragCanvasGroup.alpha = 0.8f;
        dragCanvasGroup.blocksRaycasts = false;

        // Перемещаем на передний план
        dragIcon.transform.SetAsLastSibling();
    }

    #endregion

    #region Tooltip Implementation

    /// <summary>
    /// Курсор входит в область слота
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Показываем tooltip только если в слоте есть предмет
        if (currentSlot != null && !currentSlot.IsEmpty() && currentSlot.itemData != null)
        {
            string tooltipText = TooltipSystem.CreateItemTooltip(currentSlot.itemData);
            TooltipSystem.Instance.ShowTooltip(tooltipText);
        }
    }

    /// <summary>
    /// Курсор покидает область слота
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        // Скрываем tooltip
        TooltipSystem.Instance.HideTooltip();
    }

    #endregion

    #region IPointerClickHandler Implementation

    /// <summary>
    /// Обработчик кликов мыши (альтернативный способ обнаружения двойного клика)
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // Обрабатываем только левую кнопку мыши
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            float timeSinceLastClick = Time.time - lastClickTime;


            if (timeSinceLastClick <= doubleClickTimeLimit && timeSinceLastClick > 0.05f) // Минимальная задержка для избежания одного клика
            {
                // Двойной клик

                OnSlotDoubleClicked?.Invoke(slotIndex);
                lastClickTime = 0f; // Сбрасываем для избежания тройного клика
            }
            else
            {
                // Одиночный клик или первый клик в серии

                OnSlotClicked?.Invoke(slotIndex);
                lastClickTime = Time.time;
            }
        }
    }

    #endregion

    void OnDestroy()
    {
        // Скрываем tooltip если слот уничтожается
        if (TooltipSystem.Instance != null)
        {
            TooltipSystem.Instance.HideTooltip();
        }

        // Очищаем события
        OnSlotClicked = null;
        OnSlotRightClicked = null;
        OnSlotDoubleClicked = null;
        OnSlotDragAndDrop = null;

        // Уничтожаем иконку перетаскивания если осталась
        if (dragIcon != null)
        {
            Destroy(dragIcon);
        }
    }
}
