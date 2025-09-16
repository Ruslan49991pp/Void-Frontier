using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Панель с иконками персонажей в левом верхнем углу
/// </summary>
public class CharacterIconsPanel : MonoBehaviour
{
    [Header("UI References")]
    public Canvas mainCanvas;
    public RectTransform iconsPanel;

    [Header("Icon Settings")]
    public float iconWidth = 120f;
    public float iconHeight = 40f;
    public float iconSpacing = 5f;
    public int maxIconsPerRow = 4;

    [Header("Colors")]
    public Color compactBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
    public Color expandedBackgroundColor = new Color(0.05f, 0.05f, 0.1f, 0.9f);
    public Color healthBarFullColor = Color.green;
    public Color healthBarLowColor = Color.red;

    // Внутренние компоненты
    private Dictionary<Character, CharacterIcon> characterIcons = new Dictionary<Character, CharacterIcon>();
    private List<Character> trackedCharacters = new List<Character>();

    void Start()
    {
        Debug.Log("CharacterIconsPanel: Starting initialization");
        InitializePanel();
        FindAndTrackCharacters();
        Debug.Log("CharacterIconsPanel: Initialization complete");
    }

    void Update()
    {
        UpdateCharacterIcons();
        CheckForNewCharacters();
    }

    /// <summary>
    /// Инициализация панели
    /// </summary>
    void InitializePanel()
    {
        Debug.Log("CharacterIconsPanel: Initializing panel");

        // Создаем главный Canvas если его нет
        if (mainCanvas == null)
        {
            Debug.Log("CharacterIconsPanel: Creating new canvas");
            GameObject canvasGO = new GameObject("CharacterIconsCanvas");
            mainCanvas = canvasGO.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 200; // Поверх других UI

            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            Debug.Log("CharacterIconsPanel: Canvas created");
        }

        // Создаем панель для иконок
        GameObject panelGO = new GameObject("CharacterIconsPanel");
        panelGO.transform.SetParent(mainCanvas.transform, false);

        iconsPanel = panelGO.AddComponent<RectTransform>();
        iconsPanel.anchorMin = new Vector2(0, 1);
        iconsPanel.anchorMax = new Vector2(0, 1);
        iconsPanel.pivot = new Vector2(0, 1);
        iconsPanel.anchoredPosition = new Vector2(10, -10);
        iconsPanel.sizeDelta = new Vector2(500, 200); // Начальный размер

        // Добавляем компонент для автоматической компоновки
        VerticalLayoutGroup verticalLayout = panelGO.AddComponent<VerticalLayoutGroup>();
        verticalLayout.childAlignment = TextAnchor.UpperLeft;
        verticalLayout.spacing = iconSpacing;
        verticalLayout.padding = new RectOffset(5, 5, 5, 5);

        // Компонент для автоматического изменения размера
        ContentSizeFitter sizeFitter = panelGO.AddComponent<ContentSizeFitter>();
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    /// <summary>
    /// Найти и отследить всех персонажей в сцене
    /// </summary>
    void FindAndTrackCharacters()
    {
        Character[] allCharacters = FindObjectsOfType<Character>();
        Debug.Log($"CharacterIconsPanel: Found {allCharacters.Length} characters in scene");

        foreach (Character character in allCharacters)
        {
            Debug.Log($"CharacterIconsPanel: Adding character {character.name}");
            AddCharacter(character);
        }
    }

    /// <summary>
    /// Проверить на появление новых персонажей
    /// </summary>
    void CheckForNewCharacters()
    {
        Character[] allCharacters = FindObjectsOfType<Character>();
        foreach (Character character in allCharacters)
        {
            if (!trackedCharacters.Contains(character))
            {
                AddCharacter(character);
            }
        }

        // Удаляем уничтоженных персонажей
        for (int i = trackedCharacters.Count - 1; i >= 0; i--)
        {
            if (trackedCharacters[i] == null)
            {
                RemoveCharacter(trackedCharacters[i]);
            }
        }
    }

    /// <summary>
    /// Добавить персонажа в панель
    /// </summary>
    public void AddCharacter(Character character)
    {
        if (character == null || trackedCharacters.Contains(character))
            return;

        trackedCharacters.Add(character);

        // Создаем иконку персонажа
        GameObject iconGO = new GameObject($"CharacterIcon_{character.name}");
        iconGO.transform.SetParent(iconsPanel.transform, false);

        CharacterIcon icon = iconGO.AddComponent<CharacterIcon>();
        icon.Initialize(character, iconWidth, iconHeight, this);

        characterIcons[character] = icon;

        Debug.Log($"Added character icon for: {character.name}");
    }

    /// <summary>
    /// Удалить персонажа из панели
    /// </summary>
    public void RemoveCharacter(Character character)
    {
        if (characterIcons.TryGetValue(character, out CharacterIcon icon))
        {
            if (icon != null && icon.gameObject != null)
            {
                DestroyImmediate(icon.gameObject);
            }
            characterIcons.Remove(character);
        }

        trackedCharacters.Remove(character);
    }

    /// <summary>
    /// Обновить все иконки персонажей
    /// </summary>
    void UpdateCharacterIcons()
    {
        foreach (var kvp in characterIcons)
        {
            Character character = kvp.Key;
            CharacterIcon icon = kvp.Value;

            if (character != null && icon != null)
            {
                icon.UpdateIcon();
            }
        }
    }

    /// <summary>
    /// Получить цвет полоски здоровья
    /// </summary>
    public Color GetHealthBarColor(float healthPercent)
    {
        return Color.Lerp(healthBarLowColor, healthBarFullColor, healthPercent);
    }
}

/// <summary>
/// Компонент иконки персонажа
/// </summary>
public class CharacterIcon : MonoBehaviour
{
    private Character character;
    private CharacterIconsPanel parentPanel;
    private Button iconButton;
    private Text nameText;
    private Text levelText;
    private Image healthBar;
    private Image background;
    private GameObject expandedInfo;

    private bool isExpanded = false;
    private float iconWidth;
    private float iconHeight;

    public void Initialize(Character characterData, float width, float height, CharacterIconsPanel panel)
    {
        character = characterData;
        iconWidth = width;
        iconHeight = height;
        parentPanel = panel;

        CreateCompactIcon();
    }

    void CreateCompactIcon()
    {
        // Настраиваем RectTransform
        RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(iconWidth, iconHeight);

        // Фон иконки
        background = gameObject.AddComponent<Image>();
        background.color = parentPanel.compactBackgroundColor;

        // Кнопка для нажатия
        iconButton = gameObject.AddComponent<Button>();
        iconButton.onClick.AddListener(ToggleExpanded);

        // Имя персонажа
        GameObject nameGO = new GameObject("NameText");
        nameGO.transform.SetParent(transform, false);

        nameText = nameGO.AddComponent<Text>();
        nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameText.fontSize = 12;
        nameText.color = Color.white;
        nameText.text = character.GetFullName();
        nameText.alignment = TextAnchor.MiddleLeft;

        RectTransform nameRect = nameGO.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.5f);
        nameRect.anchorMax = new Vector2(0.7f, 1);
        nameRect.offsetMin = new Vector2(5, 0);
        nameRect.offsetMax = new Vector2(-5, -2);

        // Уровень персонажа
        GameObject levelGO = new GameObject("LevelText");
        levelGO.transform.SetParent(transform, false);

        levelText = levelGO.AddComponent<Text>();
        levelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        levelText.fontSize = 10;
        levelText.color = Color.yellow;
        levelText.text = $"Lvl {character.characterData.level}";
        levelText.alignment = TextAnchor.MiddleRight;

        RectTransform levelRect = levelGO.GetComponent<RectTransform>();
        levelRect.anchorMin = new Vector2(0.7f, 0.5f);
        levelRect.anchorMax = new Vector2(1, 1);
        levelRect.offsetMin = new Vector2(0, 0);
        levelRect.offsetMax = new Vector2(-5, -2);

        // Полоска здоровья
        GameObject healthBarGO = new GameObject("HealthBar");
        healthBarGO.transform.SetParent(transform, false);

        healthBar = healthBarGO.AddComponent<Image>();
        healthBar.color = parentPanel.GetHealthBarColor(character.characterData.health / character.characterData.maxHealth);

        RectTransform healthRect = healthBarGO.GetComponent<RectTransform>();
        healthRect.anchorMin = new Vector2(0.05f, 0);
        healthRect.anchorMax = new Vector2(0.95f, 0.4f);
        healthRect.offsetMin = Vector2.zero;
        healthRect.offsetMax = Vector2.zero;
    }

    public void UpdateIcon()
    {
        if (character == null) return;

        // Обновляем полоску здоровья
        float healthPercent = character.characterData.health / character.characterData.maxHealth;
        healthBar.color = parentPanel.GetHealthBarColor(healthPercent);

        // Обновляем ширину полоски здоровья
        RectTransform healthRect = healthBar.GetComponent<RectTransform>();
        healthRect.anchorMax = new Vector2(0.05f + 0.9f * healthPercent, 0.4f);

        // Обновляем уровень
        if (levelText != null)
        {
            levelText.text = $"Lvl {character.characterData.level}";
        }

        // Обновляем расширенную информацию если она открыта
        if (isExpanded && expandedInfo != null)
        {
            UpdateExpandedInfo();
        }
    }

    void ToggleExpanded()
    {
        isExpanded = !isExpanded;

        if (isExpanded)
        {
            CreateExpandedInfo();
        }
        else
        {
            DestroyExpandedInfo();
        }
    }

    void CreateExpandedInfo()
    {
        // Увеличиваем размер иконки
        RectTransform rectTransform = GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(iconWidth, iconHeight * 3);

        // Меняем цвет фона
        background.color = parentPanel.expandedBackgroundColor;

        // Создаем область с подробной информацией
        expandedInfo = new GameObject("ExpandedInfo");
        expandedInfo.transform.SetParent(transform, false);

        RectTransform expandedRect = expandedInfo.AddComponent<RectTransform>();
        expandedRect.anchorMin = new Vector2(0, 0);
        expandedRect.anchorMax = new Vector2(1, 0.6f);
        expandedRect.offsetMin = Vector2.zero;
        expandedRect.offsetMax = Vector2.zero;

        // Текст с подробной информацией
        GameObject detailsGO = new GameObject("DetailsText");
        detailsGO.transform.SetParent(expandedInfo.transform, false);

        Text detailsText = detailsGO.AddComponent<Text>();
        detailsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        detailsText.fontSize = 9;
        detailsText.color = Color.white;
        detailsText.alignment = TextAnchor.UpperLeft;

        RectTransform detailsRect = detailsGO.GetComponent<RectTransform>();
        detailsRect.anchorMin = Vector2.zero;
        detailsRect.anchorMax = Vector2.one;
        detailsRect.offsetMin = new Vector2(5, 5);
        detailsRect.offsetMax = new Vector2(-5, -5);

        UpdateExpandedInfo();
    }

    void UpdateExpandedInfo()
    {
        if (expandedInfo == null || character == null) return;

        Text detailsText = expandedInfo.GetComponentInChildren<Text>();
        if (detailsText != null)
        {
            detailsText.text = character.GetCharacterInfo();
        }
    }

    void DestroyExpandedInfo()
    {
        if (expandedInfo != null)
        {
            DestroyImmediate(expandedInfo);
            expandedInfo = null;
        }

        // Возвращаем исходный размер и цвет
        RectTransform rectTransform = GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(iconWidth, iconHeight);

        background.color = parentPanel.compactBackgroundColor;
    }
}