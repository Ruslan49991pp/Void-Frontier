using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI компонент для отображения одного ресурса
/// Показывает иконку ресурса и его количество
/// </summary>
public class ResourceSlotUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Компонент Image для отображения иконки ресурса")]
    public Image iconImage;

    [Tooltip("Текст для отображения количества (TextMeshPro)")]
    public TextMeshProUGUI quantityText;

    [Tooltip("Фоновое изображение слота")]
    public Image backgroundImage;

    [Header("Settings")]
    [Tooltip("Размер иконки (используется только для программного создания)")]
    public Vector2 iconSize = new Vector2(50, 50);

    // Данные ресурса
    private ResourceData resourceData;
    private int currentQuantity;

    void Awake()
    {
        // Автоматически находим компоненты из префаба Res_Slot
        // Префаб имеет детей: "Ico", "Text (TMP)", "Backgound"

        if (iconImage == null)
        {
            // Ищем child с именем "Ico"
            Transform iconTransform = transform.Find("Ico");
            if (iconTransform != null)
            {
                iconImage = iconTransform.GetComponent<Image>();
                Debug.Log($"[ResourceSlotUI] Found 'Ico' component in prefab");
            }
            else
            {
                Debug.LogWarning($"[ResourceSlotUI] 'Ico' child not found in {gameObject.name}");
            }
        }

        if (quantityText == null)
        {
            // Ищем child с именем "Text (TMP)"
            Transform textTransform = transform.Find("Text (TMP)");
            if (textTransform != null)
            {
                quantityText = textTransform.GetComponent<TextMeshProUGUI>();
                Debug.Log($"[ResourceSlotUI] Found 'Text (TMP)' component in prefab");
            }
            else
            {
                Debug.LogWarning($"[ResourceSlotUI] 'Text (TMP)' child not found in {gameObject.name}");
            }
        }

        if (backgroundImage == null)
        {
            // Ищем child с именем "Backgound"
            Transform backgroundTransform = transform.Find("Backgound");
            if (backgroundTransform != null)
            {
                backgroundImage = backgroundTransform.GetComponent<Image>();
                Debug.Log($"[ResourceSlotUI] Found 'Backgound' component in prefab");
            }
            else
            {
                // Фолбэк: пытаемся найти Image на корневом объекте
                backgroundImage = GetComponent<Image>();
                if (backgroundImage != null)
                {
                    Debug.Log($"[ResourceSlotUI] Using root Image component as background");
                }
            }
        }
    }

    /// <summary>
    /// Инициализировать слот ресурса
    /// </summary>
    public void Initialize(ResourceData resource)
    {
        resourceData = resource;

        if (resourceData == null)
        {
            Debug.LogWarning("[ResourceSlotUI] Trying to initialize with null resource data");
            return;
        }

        // ФОЛБЭК: Создаем UI элементы программно, только если префаб их не содержит
        if (iconImage == null || quantityText == null)
        {
            Debug.LogWarning($"[ResourceSlotUI] Some UI components not found in prefab, creating programmatically as fallback");
            CreateUIElements();
        }

        // Устанавливаем иконку (не меняем цвет - используем настройки из префаба)
        if (iconImage != null && resourceData.icon != null)
        {
            iconImage.sprite = resourceData.icon;
        }

        // НЕ меняем цвет фона - используем настройки из префаба

        // Инициализируем количество
        UpdateQuantity(0);

        // Добавляем Tooltip (опционально)
        AddTooltip();
    }

    /// <summary>
    /// Создать UI элементы программно
    /// </summary>
    void CreateUIElements()
    {
        // Настраиваем корневой объект
        RectTransform rootRect = GetComponent<RectTransform>();
        if (rootRect == null)
        {
            rootRect = gameObject.AddComponent<RectTransform>();
        }
        rootRect.sizeDelta = iconSize;

        // Добавляем фоновое изображение
        if (backgroundImage == null)
        {
            backgroundImage = gameObject.AddComponent<Image>();
            // Используем нейтральный полупрозрачный серый цвет по умолчанию
            backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        }

        // Создаем иконку
        if (iconImage == null)
        {
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(transform, false);

            iconImage = iconObj.AddComponent<Image>();
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.1f, 0.3f);
            iconRect.anchorMax = new Vector2(0.9f, 0.9f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
        }

        // Создаем текст количества (TextMeshPro)
        if (quantityText == null)
        {
            GameObject textObj = new GameObject("QuantityText");
            textObj.transform.SetParent(transform, false);

            quantityText = textObj.AddComponent<TextMeshProUGUI>();
            quantityText.fontSize = 14;
            quantityText.fontStyle = FontStyles.Bold;
            quantityText.color = Color.white;
            quantityText.alignment = TextAlignmentOptions.BottomRight;

            // Включаем автоматический размер
            quantityText.enableAutoSizing = false;

            // Включаем Outline для лучшей читаемости
            quantityText.outlineWidth = 0.2f;
            quantityText.outlineColor = Color.black;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 0.3f);
            textRect.offsetMin = new Vector2(2, 2);
            textRect.offsetMax = new Vector2(-2, -2);
        }
    }

    /// <summary>
    /// Обновить количество ресурса
    /// </summary>
    public void UpdateQuantity(int quantity)
    {
        currentQuantity = quantity;

        if (quantityText != null)
        {
            // Просто обновляем текст, не меняя стиль из префаба
            // Форматируем количество для компактного отображения
            if (quantity >= 1000000)
            {
                quantityText.text = $"{quantity / 1000000f:F1}M";
            }
            else if (quantity >= 1000)
            {
                quantityText.text = $"{quantity / 1000f:F1}K";
            }
            else
            {
                quantityText.text = quantity.ToString();
            }

            // НЕ меняем цвет, шрифт и другие параметры - используем настройки из префаба
        }
    }

    /// <summary>
    /// Получить текущее количество
    /// </summary>
    public int GetQuantity()
    {
        return currentQuantity;
    }

    /// <summary>
    /// Получить данные ресурса
    /// </summary>
    public ResourceData GetResourceData()
    {
        return resourceData;
    }

    /// <summary>
    /// Добавить Tooltip для отображения информации о ресурсе
    /// </summary>
    void AddTooltip()
    {
        if (resourceData == null)
            return;

        // Пытаемся найти систему Tooltip
        TooltipSystem tooltipSystem = FindObjectOfType<TooltipSystem>();
        if (tooltipSystem != null)
        {
            // Можно добавить поддержку Tooltip в будущем
            // Пока оставляем заглушку
        }
    }

    /// <summary>
    /// Обработчик наведения мыши (для будущего Tooltip)
    /// </summary>
    public void OnPointerEnter()
    {
        // НЕ меняем внешний вид - используем настройки из префаба
        // Можно добавить показ Tooltip в будущем
    }

    /// <summary>
    /// Обработчик ухода мыши
    /// </summary>
    public void OnPointerExit()
    {
        // НЕ меняем внешний вид - используем настройки из префаба
    }
}
