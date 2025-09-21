using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI инструкции для системы указания целей
/// </summary>
public class TargetingInstructions : MonoBehaviour
{
    [Header("UI Settings")]
    public bool showInstructions = true;
    public float displayTime = 10f; // Время показа инструкций
    public Color instructionColor = Color.white;

    private Text instructionText;
    private GameObject instructionPanel;
    private Canvas uiCanvas;

    void Start()
    {
        if (showInstructions)
        {
            CreateInstructionUI();
            ShowInstructions();
        }
    }

    /// <summary>
    /// Создать UI для инструкций
    /// </summary>
    void CreateInstructionUI()
    {
        // Находим Canvas
        uiCanvas = FindObjectOfType<Canvas>();
        if (uiCanvas == null)
        {
            GameObject canvasGO = new GameObject("InstructionCanvas");
            uiCanvas = canvasGO.AddComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // Создаем панель для инструкций
        GameObject panelGO = new GameObject("TargetingInstructionsPanel");
        panelGO.transform.SetParent(uiCanvas.transform, false);

        instructionPanel = panelGO;

        // Настраиваем RectTransform панели
        RectTransform panelRect = panelGO.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -10f);
        panelRect.sizeDelta = new Vector2(-20f, 150f);

        // Добавляем фон панели
        Image panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.8f);

        // Создаем текст инструкций
        GameObject textGO = new GameObject("InstructionText");
        textGO.transform.SetParent(panelGO.transform, false);

        instructionText = textGO.AddComponent<Text>();
        instructionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        instructionText.fontSize = 14;
        instructionText.color = instructionColor;
        instructionText.alignment = TextAnchor.MiddleCenter;

        // Настраиваем RectTransform текста
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 10f);
        textRect.offsetMax = new Vector2(-10f, -10f);
    }

    /// <summary>
    /// Показать инструкции
    /// </summary>
    void ShowInstructions()
    {
        if (instructionText != null)
        {
            string instructions = "🎯 СИСТЕМА УКАЗАНИЯ ЦЕЛЕЙ\n" +
                                 "1. Выделите дружественных юнитов (ЛКМ или рамкой)\n" +
                                 "2. Наведите курсор на врага - он подсветится красным\n" +
                                 "3. Нажмите ЛКМ на враге - выделенные юниты будут следовать за ним\n" +
                                 "4. Красный индикатор под врагом показывает, что за ним следуют\n" +
                                 "5. ПКМ для обычного перемещения, снятие выделения останавливает следование";

            instructionText.text = instructions;

            // Скрываем инструкции через заданное время
            if (displayTime > 0)
            {
                Invoke(nameof(HideInstructions), displayTime);
            }
        }
    }

    /// <summary>
    /// Скрыть инструкции
    /// </summary>
    void HideInstructions()
    {
        if (instructionPanel != null)
        {
            instructionPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Показать инструкции снова (для кнопки)
    /// </summary>
    public void ShowInstructionsAgain()
    {
        if (instructionPanel != null)
        {
            instructionPanel.SetActive(true);

            // Скрываем через время
            if (displayTime > 0)
            {
                Invoke(nameof(HideInstructions), displayTime);
            }
        }
    }

    /// <summary>
    /// Переключить видимость инструкций
    /// </summary>
    public void ToggleInstructions()
    {
        if (instructionPanel != null)
        {
            bool isActive = instructionPanel.activeSelf;
            instructionPanel.SetActive(!isActive);

            // Если показали, запланировать скрытие
            if (!isActive && displayTime > 0)
            {
                Invoke(nameof(HideInstructions), displayTime);
            }
        }
    }

    void OnDestroy()
    {
        // Очистка UI
        if (instructionPanel != null)
        {
            DestroyImmediate(instructionPanel);
        }
    }
}