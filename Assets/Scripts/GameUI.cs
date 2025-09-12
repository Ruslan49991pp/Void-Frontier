using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("UI References")]
    public Canvas mainCanvas;
    public RectTransform bottomPanel;
    public RectTransform portraitArea;
    public RectTransform infoArea;
    public RectTransform commandArea;
    
    [Header("Portrait Settings")]
    public int maxPortraits = 12;
    public float portraitSize = 64f;
    public float portraitSpacing = 4f;
    
    [Header("Templates")]
    public GameObject portraitTemplate;
    
    // Внутренние компоненты
    private List<CharacterPortrait> activePortraits = new List<CharacterPortrait>();
    private Text infoText;
    private SelectionManager selectionManager;
    
    // Текущее выделение
    private List<GameObject> currentSelection = new List<GameObject>();
    
    void Awake()
    {
        InitializeUI();
        FindSelectionManager();
    }
    
    void Start()
    {
        if (selectionManager != null)
        {
            selectionManager.OnSelectionChanged += OnSelectionChanged;
        }
    }
    
    /// <summary>
    /// Инициализация UI системы
    /// </summary>
    void InitializeUI()
    {
        // Создаем главный Canvas если его нет
        if (mainCanvas == null)
        {
            GameObject canvasGO = new GameObject("GameUI_Canvas");
            mainCanvas = canvasGO.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 100; // Поверх других UI
            
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        
        CreateBottomPanel();
        CreatePortraitTemplate();
        
        Debug.Log("GameUI инициализирован");
    }
    
    /// <summary>
    /// Создание нижней панели
    /// </summary>
    void CreateBottomPanel()
    {
        // Главная нижняя панель
        GameObject bottomPanelGO = new GameObject("BottomPanel");
        bottomPanelGO.transform.SetParent(mainCanvas.transform, false);
        
        bottomPanel = bottomPanelGO.AddComponent<RectTransform>();
        bottomPanel.anchorMin = new Vector2(0, 0);
        bottomPanel.anchorMax = new Vector2(1, 0);
        bottomPanel.pivot = new Vector2(0.5f, 0);
        bottomPanel.anchoredPosition = Vector2.zero;
        bottomPanel.sizeDelta = new Vector2(0, 150); // Высота панели
        
        // Фон панели
        Image backgroundImage = bottomPanelGO.AddComponent<Image>();
        backgroundImage.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
        
        // Создаем области внутри панели
        CreatePortraitArea();
        CreateInfoArea();
        CreateCommandArea();
    }
    
    /// <summary>
    /// Создание области портретов
    /// </summary>
    void CreatePortraitArea()
    {
        GameObject portraitAreaGO = new GameObject("PortraitArea");
        portraitAreaGO.transform.SetParent(bottomPanel.transform, false);
        
        portraitArea = portraitAreaGO.AddComponent<RectTransform>();
        portraitArea.anchorMin = new Vector2(0, 0);
        portraitArea.anchorMax = new Vector2(0.4f, 1); // 40% ширины
        portraitArea.pivot = new Vector2(0, 0);
        portraitArea.anchoredPosition = Vector2.zero;
        portraitArea.sizeDelta = Vector2.zero;
        
        // Фон области портретов
        Image portraitBg = portraitAreaGO.AddComponent<Image>();
        portraitBg.color = new Color(0.05f, 0.05f, 0.1f, 0.5f);
        
        // Настраиваем сетку для портретов
        GridLayoutGroup gridLayout = portraitAreaGO.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(portraitSize, portraitSize);
        gridLayout.spacing = new Vector2(portraitSpacing, portraitSpacing);
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.childAlignment = TextAnchor.UpperLeft;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 6; // 6 портретов в ряд
        
        // Отступы
        ContentSizeFitter sizeFitter = portraitAreaGO.AddComponent<ContentSizeFitter>();
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }
    
    /// <summary>
    /// Создание области информации
    /// </summary>
    void CreateInfoArea()
    {
        GameObject infoAreaGO = new GameObject("InfoArea");
        infoAreaGO.transform.SetParent(bottomPanel.transform, false);
        
        infoArea = infoAreaGO.AddComponent<RectTransform>();
        infoArea.anchorMin = new Vector2(0.4f, 0);
        infoArea.anchorMax = new Vector2(0.7f, 1); // 30% ширины
        infoArea.pivot = new Vector2(0, 0);
        infoArea.anchoredPosition = Vector2.zero;
        infoArea.sizeDelta = Vector2.zero;
        
        // Фон области информации
        Image infoBg = infoAreaGO.AddComponent<Image>();
        infoBg.color = new Color(0.05f, 0.1f, 0.05f, 0.5f);
        
        // Текст информации
        GameObject textGO = new GameObject("InfoText");
        textGO.transform.SetParent(infoAreaGO.transform, false);
        
        infoText = textGO.AddComponent<Text>();
        infoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        infoText.fontSize = 12;
        infoText.color = Color.white;
        infoText.text = "Выберите персонажа для просмотра информации";
        infoText.alignment = TextAnchor.UpperLeft;
        
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 10);
        textRect.offsetMax = new Vector2(-10, -10);
    }
    
    /// <summary>
    /// Создание области команд
    /// </summary>
    void CreateCommandArea()
    {
        GameObject commandAreaGO = new GameObject("CommandArea");
        commandAreaGO.transform.SetParent(bottomPanel.transform, false);
        
        commandArea = commandAreaGO.AddComponent<RectTransform>();
        commandArea.anchorMin = new Vector2(0.7f, 0);
        commandArea.anchorMax = new Vector2(1, 1); // 30% ширины
        commandArea.pivot = new Vector2(0, 0);
        commandArea.anchoredPosition = Vector2.zero;
        commandArea.sizeDelta = Vector2.zero;
        
        // Фон области команд
        Image commandBg = commandAreaGO.AddComponent<Image>();
        commandBg.color = new Color(0.1f, 0.05f, 0.05f, 0.5f);
        
        // Заглушка для команд
        GameObject placeholderGO = new GameObject("CommandPlaceholder");
        placeholderGO.transform.SetParent(commandAreaGO.transform, false);
        
        Text placeholderText = placeholderGO.AddComponent<Text>();
        placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        placeholderText.fontSize = 10;
        placeholderText.color = Color.gray;
        placeholderText.text = "Команды\n(В разработке)";
        placeholderText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform placeholderRect = placeholderGO.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = Vector2.zero;
        placeholderRect.offsetMax = Vector2.zero;
    }
    
    /// <summary>
    /// Создание шаблона портрета
    /// </summary>
    void CreatePortraitTemplate()
    {
        if (portraitTemplate == null)
        {
            portraitTemplate = new GameObject("PortraitTemplate");
            portraitTemplate.transform.SetParent(transform, false);
            
            // Основа портрета
            Image portraitBg = portraitTemplate.AddComponent<Image>();
            portraitBg.color = new Color(0.2f, 0.2f, 0.3f, 1f);
            
            RectTransform bgRect = portraitTemplate.GetComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(portraitSize, portraitSize);
            
            // Добавляем компонент для управления портретом
            CharacterPortrait portrait = portraitTemplate.AddComponent<CharacterPortrait>();
            portrait.backgroundImage = portraitBg;
            
            // Создаем иконку персонажа (заглушка)
            GameObject iconGO = new GameObject("CharacterIcon");
            iconGO.transform.SetParent(portraitTemplate.transform, false);
            
            Image iconImage = iconGO.AddComponent<Image>();
            iconImage.color = Color.green; // Цвет персонажа по умолчанию
            
            RectTransform iconRect = iconGO.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.1f, 0.1f);
            iconRect.anchorMax = new Vector2(0.9f, 0.9f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            
            portrait.characterIcon = iconImage;
            
            // Создаем текст имени
            GameObject nameGO = new GameObject("CharacterName");
            nameGO.transform.SetParent(portraitTemplate.transform, false);
            
            Text nameText = nameGO.AddComponent<Text>();
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 8;
            nameText.color = Color.white;
            nameText.text = "Name";
            nameText.alignment = TextAnchor.LowerCenter;
            
            RectTransform nameRect = nameGO.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0);
            nameRect.anchorMax = new Vector2(1, 0.3f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;
            
            portrait.nameText = nameText;
            
            portraitTemplate.SetActive(false);
        }
    }
    
    /// <summary>
    /// Поиск SelectionManager в сцене
    /// </summary>
    void FindSelectionManager()
    {
        selectionManager = FindObjectOfType<SelectionManager>();
        if (selectionManager == null)
        {
            Debug.LogWarning("GameUI: SelectionManager не найден в сцене");
        }
    }
    
    /// <summary>
    /// Обработчик изменения выделения
    /// </summary>
    void OnSelectionChanged(List<GameObject> selectedObjects)
    {
        currentSelection = selectedObjects;
        UpdatePortraits();
        UpdateInfoArea();
    }
    
    /// <summary>
    /// Обновление портретов
    /// </summary>
    void UpdatePortraits()
    {
        // Очищаем старые портреты
        ClearPortraits();
        
        // Создаем новые портреты только для персонажей
        List<Character> selectedCharacters = new List<Character>();
        
        foreach (GameObject obj in currentSelection)
        {
            Character character = obj.GetComponent<Character>();
            if (character != null)
            {
                selectedCharacters.Add(character);
            }
        }
        
        // Ограничиваем количество портретов
        int portraitCount = Mathf.Min(selectedCharacters.Count, maxPortraits);
        
        for (int i = 0; i < portraitCount; i++)
        {
            CreateCharacterPortrait(selectedCharacters[i]);
        }
        
        Debug.Log($"Обновлены портреты: {portraitCount} персонажей");
    }
    
    /// <summary>
    /// Создание портрета персонажа
    /// </summary>
    void CreateCharacterPortrait(Character character)
    {
        GameObject portraitGO = Instantiate(portraitTemplate, portraitArea);
        portraitGO.SetActive(true);
        
        CharacterPortrait portrait = portraitGO.GetComponent<CharacterPortrait>();
        portrait.SetCharacter(character);
        
        activePortraits.Add(portrait);
    }
    
    /// <summary>
    /// Очистка всех портретов
    /// </summary>
    void ClearPortraits()
    {
        foreach (CharacterPortrait portrait in activePortraits)
        {
            if (portrait != null && portrait.gameObject != null)
            {
                DestroyImmediate(portrait.gameObject);
            }
        }
        activePortraits.Clear();
    }
    
    /// <summary>
    /// Обновление области информации
    /// </summary>
    void UpdateInfoArea()
    {
        if (infoText == null) return;
        
        if (currentSelection.Count == 0)
        {
            infoText.text = "Выберите персонажа для просмотра информации";
            return;
        }
        
        // Показываем информацию о первом персонаже если есть персонажи
        Character firstCharacter = null;
        foreach (GameObject obj in currentSelection)
        {
            Character character = obj.GetComponent<Character>();
            if (character != null)
            {
                firstCharacter = character;
                break;
            }
        }
        
        if (firstCharacter != null)
        {
            infoText.text = firstCharacter.GetCharacterInfo();
        }
        else
        {
            // Показываем общую информацию о выделении
            string info = $"Выделено: {currentSelection.Count} объект(а/ов)\n\n";
            foreach (GameObject obj in currentSelection)
            {
                LocationObjectInfo objectInfo = obj.GetComponent<LocationObjectInfo>();
                if (objectInfo != null)
                {
                    info += $"• {objectInfo.objectName} ({objectInfo.objectType})\n";
                }
            }
            infoText.text = info;
        }
    }
    
    void OnDestroy()
    {
        if (selectionManager != null)
        {
            selectionManager.OnSelectionChanged -= OnSelectionChanged;
        }
        
        ClearPortraits();
    }
}

/// <summary>
/// Компонент для управления отдельным портретом персонажа
/// </summary>
public class CharacterPortrait : MonoBehaviour
{
    [Header("UI Components")]
    public Image backgroundImage;
    public Image characterIcon;
    public Text nameText;
    
    private Character linkedCharacter;
    
    /// <summary>
    /// Установка персонажа для портрета
    /// </summary>
    public void SetCharacter(Character character)
    {
        linkedCharacter = character;
        UpdatePortrait();
    }
    
    /// <summary>
    /// Обновление отображения портрета
    /// </summary>
    void UpdatePortrait()
    {
        if (linkedCharacter == null) return;
        
        // Обновляем имя
        if (nameText != null)
        {
            nameText.text = linkedCharacter.characterData.firstName;
        }
        
        // Обновляем цвет иконки (зеленый для не выделенных, красный для выделенных)
        if (characterIcon != null)
        {
            characterIcon.color = linkedCharacter.IsSelected() ? Color.red : Color.green;
        }
        
        // Обновляем фон в зависимости от состояния
        if (backgroundImage != null)
        {
            backgroundImage.color = linkedCharacter.IsSelected() ? 
                new Color(0.4f, 0.2f, 0.2f, 1f) : 
                new Color(0.2f, 0.2f, 0.3f, 1f);
        }
    }
    
    void Update()
    {
        // Постоянно обновляем портрет для отражения изменений в персонаже
        UpdatePortrait();
    }
}