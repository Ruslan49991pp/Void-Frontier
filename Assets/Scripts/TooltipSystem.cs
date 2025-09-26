using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Система всплывающих подсказок для UI элементов
/// </summary>
public class TooltipSystem : MonoBehaviour
{
    [Header("Tooltip Settings")]
    public float showDelay = 0.5f;
    public Vector2 tooltipOffset = new Vector2(10, -10);
    public int fontSize = 12;
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
    public Color textColor = Color.white;
    public Vector2 padding = new Vector2(8, 6);
    public float maxWidth = 250f;

    // Singleton instance
    private static TooltipSystem instance;
    private static bool applicationIsQuitting = false;

    public static TooltipSystem Instance
    {
        get
        {
            if (applicationIsQuitting)
            {
                return null;
            }

            if (instance == null)
            {
                instance = FindObjectOfType<TooltipSystem>();
                if (instance == null)
                {
                    GameObject go = new GameObject("TooltipSystem");
                    instance = go.AddComponent<TooltipSystem>();
                }
            }
            return instance;
        }
    }

    // UI Components
    private Canvas tooltipCanvas;
    private GameObject tooltipPanel;
    private Text tooltipText;
    private Image tooltipBackground;
    private RectTransform tooltipRect;
    private CanvasGroup tooltipCanvasGroup;

    // State
    private bool isTooltipVisible = false;
    private float showTimer = 0f;
    private string pendingTooltipText = "";
    private bool isPendingTooltip = false;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        CreateTooltipUI();
    }

    void Update()
    {
        // Обработка задержки показа tooltip
        if (isPendingTooltip)
        {
            showTimer += Time.deltaTime;
            if (showTimer >= showDelay)
            {
                ShowTooltipImmediate(pendingTooltipText);
                isPendingTooltip = false;
            }
        }

        // Обновление позиции tooltip следом за мышью
        if (isTooltipVisible)
        {
            UpdateTooltipPosition();
        }
    }

    /// <summary>
    /// Создание UI для tooltip
    /// </summary>
    void CreateTooltipUI()
    {
        // Создаем Canvas для tooltip
        GameObject canvasGO = new GameObject("TooltipCanvas");
        canvasGO.transform.SetParent(transform, false);

        tooltipCanvas = canvasGO.AddComponent<Canvas>();
        tooltipCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        tooltipCanvas.sortingOrder = 1000; // Самый верхний слой

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Создаем панель tooltip
        GameObject panelGO = new GameObject("TooltipPanel");
        panelGO.transform.SetParent(canvasGO.transform, false);

        tooltipRect = panelGO.AddComponent<RectTransform>();
        tooltipRect.pivot = new Vector2(0, 1); // Pivot в левом верхнем углу

        // Фон tooltip
        tooltipBackground = panelGO.AddComponent<Image>();
        tooltipBackground.color = backgroundColor;
        tooltipBackground.type = Image.Type.Simple;

        // CanvasGroup для плавного появления
        tooltipCanvasGroup = panelGO.AddComponent<CanvasGroup>();
        tooltipCanvasGroup.alpha = 0f;
        tooltipCanvasGroup.blocksRaycasts = false;

        // Текст tooltip
        GameObject textGO = new GameObject("TooltipText");
        textGO.transform.SetParent(panelGO.transform, false);

        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(padding.x, padding.y);
        textRect.offsetMax = new Vector2(-padding.x, -padding.y);

        tooltipText = textGO.AddComponent<Text>();
        tooltipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tooltipText.fontSize = fontSize;
        tooltipText.color = textColor;
        tooltipText.alignment = TextAnchor.MiddleLeft;
        tooltipText.verticalOverflow = VerticalWrapMode.Overflow;
        tooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;

        // Изначально скрываем tooltip
        tooltipPanel = panelGO;
        tooltipPanel.SetActive(false);
    }

    /// <summary>
    /// Показать tooltip с задержкой
    /// </summary>
    public void ShowTooltip(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            HideTooltip();
            return;
        }

        pendingTooltipText = text;
        isPendingTooltip = true;
        showTimer = 0f;
    }

    /// <summary>
    /// Немедленно показать tooltip
    /// </summary>
    void ShowTooltipImmediate(string text)
    {
        if (tooltipText == null || tooltipPanel == null)
            return;

        tooltipText.text = text;

        // Рассчитываем размер tooltip
        Vector2 textSize = CalculateTextSize(text);
        tooltipRect.sizeDelta = new Vector2(textSize.x + padding.x * 2, textSize.y + padding.y * 2);

        tooltipPanel.SetActive(true);
        isTooltipVisible = true;

        // Плавное появление
        StartCoroutine(FadeTooltip(0f, 1f, 0.2f));

        UpdateTooltipPosition();
    }

    /// <summary>
    /// Скрыть tooltip
    /// </summary>
    public void HideTooltip()
    {
        isPendingTooltip = false;
        showTimer = 0f;

        if (isTooltipVisible)
        {
            StartCoroutine(FadeTooltip(1f, 0f, 0.1f));
        }
    }

    /// <summary>
    /// Рассчитать размер текста
    /// </summary>
    Vector2 CalculateTextSize(string text)
    {
        TextGenerator textGen = new TextGenerator();
        TextGenerationSettings settings = tooltipText.GetGenerationSettings(new Vector2(maxWidth, 0f));
        float width = textGen.GetPreferredWidth(text, settings);
        float height = textGen.GetPreferredHeight(text, settings);

        // Ограничиваем ширину
        width = Mathf.Min(width, maxWidth);

        // Если ширина превышена, пересчитываем высоту с учетом переносов
        if (width >= maxWidth)
        {
            settings = tooltipText.GetGenerationSettings(new Vector2(maxWidth, 0f));
            height = textGen.GetPreferredHeight(text, settings);
        }

        return new Vector2(width, height);
    }

    /// <summary>
    /// Обновить позицию tooltip
    /// </summary>
    void UpdateTooltipPosition()
    {
        if (tooltipRect == null) return;

        Vector2 mousePosition = Input.mousePosition;
        Vector2 targetPosition = mousePosition + tooltipOffset;

        // Проверяем границы экрана
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        Vector2 tooltipSize = tooltipRect.sizeDelta;

        // Корректируем позицию, чтобы tooltip не выходил за границы экрана
        if (targetPosition.x + tooltipSize.x > screenWidth)
        {
            targetPosition.x = mousePosition.x - tooltipSize.x - tooltipOffset.x;
        }

        if (targetPosition.y - tooltipSize.y < 0)
        {
            targetPosition.y = mousePosition.y + tooltipSize.y - tooltipOffset.y;
        }

        tooltipRect.position = targetPosition;
    }

    /// <summary>
    /// Плавное появление/исчезание tooltip
    /// </summary>
    System.Collections.IEnumerator FadeTooltip(float fromAlpha, float toAlpha, float duration)
    {
        if (tooltipCanvasGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration);
            tooltipCanvasGroup.alpha = alpha;
            yield return null;
        }

        tooltipCanvasGroup.alpha = toAlpha;

        // Если скрываем, деактивируем панель
        if (toAlpha <= 0f)
        {
            tooltipPanel.SetActive(false);
            isTooltipVisible = false;
        }
    }

    /// <summary>
    /// Создать текст tooltip для предмета
    /// </summary>
    public static string CreateItemTooltip(ItemData itemData)
    {
        if (itemData == null) return "";

        string tooltip = $"<color=#{ColorUtility.ToHtmlStringRGB(itemData.GetRarityColor())}><b>{itemData.itemName}</b></color>\n";
        tooltip += $"<color=grey>{itemData.itemType}</color>\n";

        if (!string.IsNullOrEmpty(itemData.description))
        {
            tooltip += $"\n{itemData.description}\n";
        }

        // Характеристики
        bool hasStats = false;
        if (itemData.damage > 0)
        {
            tooltip += $"\n<color=red>Урон: {itemData.damage}</color>";
            hasStats = true;
        }

        if (itemData.armor > 0)
        {
            tooltip += $"\n<color=blue>Защита: {itemData.armor}</color>";
            hasStats = true;
        }

        if (itemData.healing > 0)
        {
            tooltip += $"\n<color=green>Лечение: {itemData.healing}</color>";
            hasStats = true;
        }

        // Информация об экипировке
        if (itemData.equipmentSlot != EquipmentSlot.None)
        {
            tooltip += $"\n<color=yellow>Слот: {GetEquipmentSlotName(itemData.equipmentSlot)}</color>";
        }

        // Вес и стоимость
        if (hasStats || itemData.equipmentSlot != EquipmentSlot.None)
        {
            tooltip += "\n";
        }

        tooltip += $"\nВес: {itemData.weight:F1}";
        tooltip += $"\nСтоимость: {itemData.value}";

        return tooltip;
    }

    /// <summary>
    /// Получить название слота экипировки
    /// </summary>
    static string GetEquipmentSlotName(EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.LeftHand: return "Левая рука";
            case EquipmentSlot.RightHand: return "Правая рука";
            case EquipmentSlot.Head: return "Голова";
            case EquipmentSlot.Chest: return "Грудь";
            case EquipmentSlot.Legs: return "Ноги";
            case EquipmentSlot.Feet: return "Ступни";
            default: return "Неизвестно";
        }
    }

    void OnApplicationQuit()
    {
        applicationIsQuitting = true;
    }

    void OnDestroy()
    {
        // Очищаем static instance при уничтожении
        if (instance == this)
        {
            instance = null;
        }
    }
}