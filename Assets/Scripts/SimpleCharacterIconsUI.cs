using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Простая система иконок персонажей - более надежная версия
/// </summary>
public class SimpleCharacterIconsUI : MonoBehaviour
{
    [Header("Settings")]
    public float iconWidth = 120f;
    public float iconHeight = 50f;
    public float spacing = 10f;

    [Header("Selection Colors")]
    public Color normalBackgroundColor = new Color(0, 1, 0, 1f);
    public Color selectedBackgroundColor = new Color(1, 0.8f, 0, 1f); // Оранжевый
    public Color selectedBorderColor = new Color(1, 1, 0, 1f); // Желтый

    private Canvas canvas;
    private GameObject iconsContainer;
    private Dictionary<Character, GameObject> characterIcons = new Dictionary<Character, GameObject>();
    private SelectionManager selectionManager;

    void Start()
    {
        Debug.Log("=== SimpleCharacterIconsUI: Starting ===");

        try
        {
            // Ищем SelectionManager для отслеживания выделения
            selectionManager = FindObjectOfType<SelectionManager>();
            if (selectionManager != null)
            {
                selectionManager.OnSelectionChanged += OnSelectionChanged;
                Debug.Log("SimpleCharacterIconsUI: Connected to SelectionManager");
            }
            else
            {
                Debug.LogWarning("SimpleCharacterIconsUI: SelectionManager not found!");
            }

            CreateUI();
            Debug.Log("SimpleCharacterIconsUI: UI creation completed");

            // Отложенный поиск персонажей - даем время системе загрузиться
            Debug.Log("SimpleCharacterIconsUI: Scheduling character search in 1 and 3 seconds");
            Invoke("FindAndCreateIcons", 1f);
            Invoke("FindAndCreateIcons", 3f);

            Debug.Log("SimpleCharacterIconsUI: Start() completed successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SimpleCharacterIconsUI: Error in Start(): {e.Message}");
            Debug.LogError($"SimpleCharacterIconsUI: Stack trace: {e.StackTrace}");
        }
    }

    void Update()
    {
        // Проверяем новых персонажей каждые 2 секунды
        if (Time.time % 2f < Time.deltaTime)
        {
            CheckForNewCharacters();
        }
    }

    void CreateUI()
    {
        Debug.Log("SimpleCharacterIconsUI: Creating UI");

        try
        {
            // Создаем Canvas
            Debug.Log("SimpleCharacterIconsUI: Creating Canvas");
            GameObject canvasGO = new GameObject("SimpleCharacterIconsCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // Снижаем приоритет
            Debug.Log($"SimpleCharacterIconsUI: Canvas created with sorting order {canvas.sortingOrder}");

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();
            Debug.Log("SimpleCharacterIconsUI: Canvas components added");

            // Создаем контейнер для иконок
            Debug.Log("SimpleCharacterIconsUI: Creating icons container");
            iconsContainer = new GameObject("IconsContainer");
            iconsContainer.transform.SetParent(canvas.transform, false);

            RectTransform containerRect = iconsContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 1);
            containerRect.anchorMax = new Vector2(1, 1);
            containerRect.pivot = new Vector2(0.5f, 1);
            containerRect.anchoredPosition = new Vector2(0, -10);
            containerRect.sizeDelta = new Vector2(0, 60); // Горизонтальная панель вверху
            Debug.Log($"SimpleCharacterIconsUI: Container positioned at {containerRect.anchoredPosition} with size {containerRect.sizeDelta}");

            // Добавляем прозрачный фон для контейнера (убираем красный)
            Image containerBg = iconsContainer.AddComponent<Image>();
            containerBg.color = new Color(0, 0, 0, 0.3f); // Полупрозрачный темный фон
            Debug.Log("SimpleCharacterIconsUI: Container background added");

            // VerticalLayoutGroup мешает - отключаем и делаем ручное позиционирование
            // VerticalLayoutGroup layout = iconsContainer.AddComponent<VerticalLayoutGroup>();
            Debug.Log("SimpleCharacterIconsUI: Using manual positioning instead of LayoutGroup");

            // ContentSizeFitter может мешать - пока отключаем
            // ContentSizeFitter fitter = iconsContainer.AddComponent<ContentSizeFitter>();
            // fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Debug.Log("SimpleCharacterIconsUI: UI created successfully");

            // Добавляем тестовую метку чтобы убедиться что контейнер виден
            CreateTestLabel();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SimpleCharacterIconsUI: Error in CreateUI(): {e.Message}");
            Debug.LogError($"SimpleCharacterIconsUI: Stack trace: {e.StackTrace}");
        }
    }

    void CreateTestLabel()
    {
        Debug.Log("SimpleCharacterIconsUI: Creating test label");

        GameObject testLabelGO = new GameObject("TestLabel");
        testLabelGO.transform.SetParent(iconsContainer.transform, false);

        Text testLabel = testLabelGO.AddComponent<Text>();
        testLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        testLabel.fontSize = 14;
        testLabel.color = Color.yellow;
        testLabel.text = "CHARACTER ICONS PANEL";
        testLabel.alignment = TextAnchor.MiddleCenter;

        RectTransform testRect = testLabelGO.GetComponent<RectTransform>();
        testRect.sizeDelta = new Vector2(iconWidth, 30);

        Debug.Log("SimpleCharacterIconsUI: Test label created - panel should be visible now");
    }

    void FindAndCreateIcons()
    {
        Debug.Log("=== SimpleCharacterIconsUI: SEARCHING FOR CHARACTERS ===");

        try
        {
            if (iconsContainer == null)
            {
                Debug.LogError("SimpleCharacterIconsUI: iconsContainer is NULL! Cannot create icons.");
                return;
            }

            Character[] characters = FindObjectsOfType<Character>(true); // Включаем неактивные объекты

            // Подсчитываем только валидных персонажей (исключаем Template и другие)
            int validCharacterCount = 0;
            foreach (Character character in characters)
            {
                if (ShouldShowCharacterIcon(character))
                {
                    validCharacterCount++;
                }
            }

            Debug.Log($"SimpleCharacterIconsUI: Found {validCharacterCount} player characters in scene (total: {characters.Length})");

            if (characters.Length == 0)
            {
                Debug.LogWarning("SimpleCharacterIconsUI: NO CHARACTERS FOUND! Maybe they haven't spawned yet?");

                // Попробуем найти объекты с компонентом Character через другие способы
                GameObject[] allObjects = FindObjectsOfType<GameObject>();
                int charCount = 0;
                foreach (GameObject obj in allObjects)
                {
                    if (obj.GetComponent<Character>() != null)
                    {
                        charCount++;
                        Debug.Log($"SimpleCharacterIconsUI: Found Character component on {obj.name}");
                    }
                }
                Debug.Log($"SimpleCharacterIconsUI: Manual search found {charCount} objects with Character component");
            }
            else
            {
                Debug.Log("SimpleCharacterIconsUI: Processing found characters:");
                for (int i = 0; i < characters.Length; i++)
                {
                    Character character = characters[i];
                    Debug.Log($"SimpleCharacterIconsUI: [{i}] {character.name} - Active: {character.gameObject.activeInHierarchy}");

                    // Фильтруем персонажей: исключаем шаблоны и NPC
                    if (ShouldShowCharacterIcon(character))
                    {
                        if (!characterIcons.ContainsKey(character))
                        {
                            Debug.Log($"SimpleCharacterIconsUI: Creating new icon for {character.name}");
                            CreateCharacterIcon(character);
                        }
                        else
                        {
                            Debug.Log($"SimpleCharacterIconsUI: Icon already exists for {character.name}");
                        }
                    }
                    else
                    {
                        Debug.Log($"SimpleCharacterIconsUI: Skipping {character.name} - not a player character");
                    }
                }
            }

            Debug.Log($"SimpleCharacterIconsUI: Total icons created so far: {characterIcons.Count}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SimpleCharacterIconsUI: Error in FindAndCreateIcons(): {e.Message}");
            Debug.LogError($"SimpleCharacterIconsUI: Stack trace: {e.StackTrace}");
        }
    }

    void CheckForNewCharacters()
    {
        Character[] characters = FindObjectsOfType<Character>();
        foreach (Character character in characters)
        {
            if (!characterIcons.ContainsKey(character) && character != null && ShouldShowCharacterIcon(character))
            {
                Debug.Log($"SimpleCharacterIconsUI: Found new character {character.name}");
                CreateCharacterIcon(character);
            }
        }

        // Удаляем иконки уничтоженных персонажей
        List<Character> toRemove = new List<Character>();
        foreach (var kvp in characterIcons)
        {
            if (kvp.Key == null)
            {
                toRemove.Add(kvp.Key);
                if (kvp.Value != null)
                {
                    DestroyImmediate(kvp.Value);
                }
            }
        }

        foreach (Character character in toRemove)
        {
            characterIcons.Remove(character);
        }
    }

    void CreateCharacterIcon(Character character)
    {
        Debug.Log($"=== SimpleCharacterIconsUI: CREATING ICON FOR {character?.name} ===");

        try
        {
            if (character == null)
            {
                Debug.LogError("SimpleCharacterIconsUI: Character is NULL!");
                return;
            }

            if (iconsContainer == null)
            {
                Debug.LogError("SimpleCharacterIconsUI: iconsContainer is NULL!");
                return;
            }

            Debug.Log($"SimpleCharacterIconsUI: Character data check - Name: {character.name}, CharacterData: {(character.characterData != null ? "OK" : "NULL")}");

            if (character.characterData == null)
            {
                Debug.LogWarning($"SimpleCharacterIconsUI: Character {character.name} has no characterData - generating...");
                character.GenerateRandomCharacter();
            }

            // Основной объект иконки
            Debug.Log("SimpleCharacterIconsUI: Creating icon GameObject");
            GameObject iconGO = new GameObject($"CharacterIcon_{character.name}");
            iconGO.transform.SetParent(iconsContainer.transform, false);

            // RectTransform для размера
            RectTransform iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(iconWidth, iconHeight);

            // Принудительно устанавливаем позицию горизонтально
            iconRect.anchorMin = new Vector2(0, 1);
            iconRect.anchorMax = new Vector2(0, 1);
            iconRect.pivot = new Vector2(0, 1);

            // Подсчитываем существующие иконки (исключая null)
            int existingIconsCount = 0;
            foreach (var kvp in characterIcons)
            {
                if (kvp.Value != null) existingIconsCount++;
            }

            iconRect.anchoredPosition = new Vector2(10 + (existingIconsCount * (iconWidth + spacing)), -5); // Размещаем горизонтально

            Debug.Log($"SimpleCharacterIconsUI: Icon size: {iconRect.sizeDelta}, position: {iconRect.anchoredPosition}");

            // Фон иконки
            Image iconBg = iconGO.AddComponent<Image>();
            iconBg.color = normalBackgroundColor;
            Debug.Log("SimpleCharacterIconsUI: Icon background added");

            // Создаем рамку для выделения (изначально невидимая)
            GameObject borderGO = new GameObject("SelectionBorder");
            borderGO.transform.SetParent(iconGO.transform, false);

            Image borderImage = borderGO.AddComponent<Image>();
            borderImage.color = selectedBorderColor;

            RectTransform borderRect = borderGO.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(-3, -3); // Рамка чуть больше иконки
            borderRect.offsetMax = new Vector2(3, 3);
            borderRect.SetAsFirstSibling(); // Рамка сзади

            borderGO.SetActive(false); // Изначально скрыта
            Debug.Log("SimpleCharacterIconsUI: Selection border added");

            // Кнопка для выделения (основной клик) - НЕ перехватывает все события
            Button iconButton = iconGO.AddComponent<Button>();
            iconButton.interactable = true;
            iconButton.onClick.AddListener(() => OnIconClicked(character));

            // Важно: убираем Navigation чтобы избежать конфликтов
            Navigation nav = iconButton.navigation;
            nav.mode = Navigation.Mode.None;
            iconButton.navigation = nav;

            // Компонент для раскрытия (по двойному клику)
            SimpleCharacterIcon iconComponent = iconGO.AddComponent<SimpleCharacterIcon>();
            iconComponent.Initialize(character, iconWidth, iconHeight);

            // Создаем содержимое иконки СНАЧАЛА
            Debug.Log("SimpleCharacterIconsUI: Creating icon content");
            CreateIconContent(iconGO, character);

            // Добавляем кнопку для раскрытия ПОСЛЕ содержимого, чтобы она была поверх
            GameObject expandButtonGO = new GameObject("ExpandButton");
            expandButtonGO.transform.SetParent(iconGO.transform, false);

            // ВАЖНО: Добавляем RectTransform перед другими компонентами
            RectTransform expandButtonRect = expandButtonGO.AddComponent<RectTransform>();
            // ТЕСТИРОВАНИЕ: Делаем кнопку БОЛЬШОЙ и заметной
            expandButtonRect.anchorMin = new Vector2(0.1f, 0.6f);  // Левый верхний угол
            expandButtonRect.anchorMax = new Vector2(0.5f, 0.9f); // Большая кнопка для тестирования
            expandButtonRect.offsetMin = Vector2.zero;
            expandButtonRect.offsetMax = Vector2.zero;

            Image expandButtonImage = expandButtonGO.AddComponent<Image>();
            expandButtonImage.color = new Color(1f, 0.6f, 0f, 0.95f); // Приятный оранжевый цвет

            Button expandButton = expandButtonGO.AddComponent<Button>();
            expandButton.interactable = true;

            // Настраиваем цвета кнопки для лучшей обратной связи
            ColorBlock colors = expandButton.colors;
            colors.normalColor = new Color(1f, 0.6f, 0f, 0.95f); // Приятный оранжевый
            colors.highlightedColor = new Color(1f, 0.8f, 0.2f, 1f); // Светлее при наведении
            colors.pressedColor = new Color(0.8f, 0.4f, 0f, 1f); // Темнее при нажатии
            colors.selectedColor = new Color(1f, 0.6f, 0f, 0.95f);
            colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            expandButton.colors = colors;

            // Убираем Navigation чтобы избежать конфликтов
            Navigation expandNav = expandButton.navigation;
            expandNav.mode = Navigation.Mode.None;
            expandButton.navigation = expandNav;

            // Добавляем CanvasGroup для лучшего контроля событий
            CanvasGroup expandButtonGroup = expandButtonGO.AddComponent<CanvasGroup>();
            expandButtonGroup.blocksRaycasts = true;
            expandButtonGroup.interactable = true;

            // Создаем текст кнопки СНАЧАЛА
            GameObject expandTextGO = new GameObject("ExpandText");
            expandTextGO.transform.SetParent(expandButtonGO.transform, false);

            RectTransform expandTextRect = expandTextGO.AddComponent<RectTransform>();
            expandTextRect.anchorMin = Vector2.zero;
            expandTextRect.anchorMax = Vector2.one;
            expandTextRect.offsetMin = Vector2.zero;
            expandTextRect.offsetMax = Vector2.zero;

            Text expandText = expandTextGO.AddComponent<Text>();
            expandText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            expandText.fontSize = 14;  // Подходящий размер для кнопки
            expandText.color = Color.white; // Белый текст на оранжевом фоне
            expandText.text = "i"; // Символ информации
            expandText.alignment = TextAnchor.MiddleCenter;
            expandText.fontStyle = FontStyle.Bold;

            // Передаем ссылку на текст кнопки в компонент для изменения при раскрытии
            iconComponent.SetExpandButtonText(expandText);

            expandButton.onClick.AddListener(() => {
                Debug.Log($"SimpleCharacterIconsUI: Expand button clicked for {character.name}!");
                iconComponent.ToggleExpanded();
            });


            // Кнопка раскрытия должна быть поверх основной кнопки
            expandButtonGO.transform.SetAsLastSibling();

            // Кнопка использует родительский Canvas


            // Убеждаемся что кнопка на переднем плане
            expandButtonGO.transform.SetAsLastSibling();

            characterIcons[character] = iconGO;

            Debug.Log($"SimpleCharacterIconsUI: ✅ Icon created successfully for {character.name}! Total icons: {characterIcons.Count}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SimpleCharacterIconsUI: ERROR creating icon: {e.Message}");
            Debug.LogError($"SimpleCharacterIconsUI: Stack trace: {e.StackTrace}");

            // ВАЖНО: Даже при ошибке добавляем персонажа в словарь, чтобы избежать бесконечных попыток
            characterIcons[character] = null;
        }
    }

    void CreateIconContent(GameObject parent, Character character)
    {
        // Имя персонажа
        GameObject nameGO = new GameObject("NameText");
        nameGO.transform.SetParent(parent.transform, false);

        Text nameText = nameGO.AddComponent<Text>();
        nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameText.fontSize = 12;
        nameText.color = Color.black; // Черный текст для контраста с зеленым фоном
        nameText.text = character.GetFullName();
        nameText.alignment = TextAnchor.MiddleLeft;

        RectTransform nameRect = nameGO.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.05f, 0.5f);
        nameRect.anchorMax = new Vector2(0.7f, 1f);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;

        // Уровень
        GameObject levelGO = new GameObject("LevelText");
        levelGO.transform.SetParent(parent.transform, false);

        Text levelText = levelGO.AddComponent<Text>();
        levelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        levelText.fontSize = 10;
        levelText.color = Color.red; // Красный для контраста с зеленым фоном
        levelText.text = $"Lvl {character.characterData.level}";
        levelText.alignment = TextAnchor.MiddleRight;

        RectTransform levelRect = levelGO.GetComponent<RectTransform>();
        levelRect.anchorMin = new Vector2(0.7f, 0.5f);
        levelRect.anchorMax = new Vector2(0.95f, 1f);
        levelRect.offsetMin = Vector2.zero;
        levelRect.offsetMax = Vector2.zero;

        // Полоска здоровья
        GameObject healthBarGO = new GameObject("HealthBar");
        healthBarGO.transform.SetParent(parent.transform, false);

        Image healthBar = healthBarGO.AddComponent<Image>();
        float healthPercent = character.characterData.health / character.characterData.maxHealth;
        healthBar.color = Color.Lerp(Color.red, Color.green, healthPercent);

        RectTransform healthRect = healthBarGO.GetComponent<RectTransform>();
        healthRect.anchorMin = new Vector2(0.05f, 0f);
        healthRect.anchorMax = new Vector2(0.05f + 0.9f * healthPercent, 0.4f);
        healthRect.offsetMin = Vector2.zero;
        healthRect.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Проверить, должна ли отображаться иконка для этого персонажа
    /// </summary>
    bool ShouldShowCharacterIcon(Character character)
    {
        if (character == null) return false;

        // Исключаем шаблоны персонажей (любые объекты с "Template" в названии)
        if (character.name.Contains("Template"))
        {
            return false;
        }

        // Исключаем Character_Template специально (на случай если название изменится)
        if (character.name == "Character_Template")
        {
            return false;
        }

        // Исключаем неактивных персонажей
        if (!character.gameObject.activeInHierarchy)
        {
            return false;
        }

        // Исключаем персонажей, которые находятся очень далеко (вероятно, скрытые шаблоны)
        if (Vector3.Distance(character.transform.position, Vector3.zero) > 5000f)
        {
            return false;
        }

        // Можно добавить дополнительные проверки:
        // - Проверка принадлежности игроку
        // - Проверка команды
        // - Проверка состояния персонажа

        return true;
    }

    private List<GameObject> lastSelectedObjects = new List<GameObject>();

    /// <summary>
    /// Обработчик изменения выделения от SelectionManager
    /// </summary>
    void OnSelectionChanged(List<GameObject> selectedObjects)
    {
        // Проверяем, изменилось ли выделение на самом деле (избегаем лишних обновлений)
        if (AreListsEqual(lastSelectedObjects, selectedObjects))
        {
            return; // Выделение не изменилось, пропускаем обновление
        }

        Debug.Log($"SimpleCharacterIconsUI: Selection changed, {selectedObjects.Count} objects selected");

        // Сохраняем текущее выделение
        lastSelectedObjects = new List<GameObject>(selectedObjects);

        // Обновляем визуальное состояние всех иконок (пропускаем null иконки)
        foreach (var kvp in characterIcons)
        {
            Character character = kvp.Key;
            GameObject iconGO = kvp.Value;

            if (character != null && iconGO != null)
            {
                bool isSelected = IsCharacterSelected(character, selectedObjects);
                UpdateIconSelectionVisual(iconGO, isSelected);
            }
        }
    }

    /// <summary>
    /// Проверить равенство двух списков объектов
    /// </summary>
    bool AreListsEqual(List<GameObject> list1, List<GameObject> list2)
    {
        if (list1.Count != list2.Count) return false;

        for (int i = 0; i < list1.Count; i++)
        {
            if (list1[i] != list2[i]) return false;
        }

        return true;
    }

    /// <summary>
    /// Проверить, выделен ли персонаж среди выделенных объектов
    /// </summary>
    bool IsCharacterSelected(Character character, List<GameObject> selectedObjects)
    {
        foreach (GameObject selectedObj in selectedObjects)
        {
            if (selectedObj != null && selectedObj.GetComponent<Character>() == character)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Обновить визуальное состояние иконки (выделена/не выделена)
    /// </summary>
    void UpdateIconSelectionVisual(GameObject iconGO, bool isSelected)
    {
        if (iconGO == null) return;

        // Обновляем фон иконки
        Image iconBg = iconGO.GetComponent<Image>();
        if (iconBg != null)
        {
            iconBg.color = isSelected ? selectedBackgroundColor : normalBackgroundColor;
        }

        // Обновляем видимость рамки выделения
        Transform borderTransform = iconGO.transform.Find("SelectionBorder");
        if (borderTransform != null)
        {
            borderTransform.gameObject.SetActive(isSelected);
        }

        Debug.Log($"SimpleCharacterIconsUI: Updated icon visual for {iconGO.name}, selected: {isSelected}");
    }

    private float lastClickTime = 0f;
    private Character lastClickedCharacter = null;

    /// <summary>
    /// Обработчик клика по иконке персонажа
    /// </summary>
    void OnIconClicked(Character character)
    {
        if (character == null || selectionManager == null) return;

        float currentTime = Time.time;
        bool isDoubleClick = (currentTime - lastClickTime < 0.5f && lastClickedCharacter == character);

        Debug.Log($"SimpleCharacterIconsUI: Icon clicked for {character.name}, doubleClick: {isDoubleClick}");

        if (isDoubleClick)
        {
            // Двойной клик - фокусируем камеру на персонаже
            Debug.Log($"SimpleCharacterIconsUI: Double click detected - focusing camera");
            FocusCameraOnCharacter(character);
        }
        else
        {
            // Одинарный клик - выделение с задержкой чтобы избежать конфликтов
            Debug.Log($"SimpleCharacterIconsUI: Single click detected - handling selection");
            StartCoroutine(HandleSingleClickDelayed(character));
        }

        lastClickTime = currentTime;
        lastClickedCharacter = character;
    }

    /// <summary>
    /// Обработка одинарного клика с задержкой
    /// </summary>
    System.Collections.IEnumerator HandleSingleClickDelayed(Character character)
    {
        yield return new WaitForEndOfFrame(); // Ждём конца кадра
        HandleSingleClick(character);
    }

    /// <summary>
    /// Обработка одинарного клика
    /// </summary>
    void HandleSingleClick(Character character)
    {
        // Проверяем, выделен ли уже этот персонаж
        bool isCurrentlySelected = selectionManager.IsSelected(character.gameObject);
        Debug.Log($"SimpleCharacterIconsUI: Character {character.name} currently selected: {isCurrentlySelected}");

        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            // Ctrl + клик - переключаем выделение
            Debug.Log($"SimpleCharacterIconsUI: Ctrl+Click - toggling selection for {character.name}");
            selectionManager.ToggleSelection(character.gameObject);
        }
        else
        {
            // Обычный клик - очищаем все выделение и выделяем только этого персонажа
            Debug.Log($"SimpleCharacterIconsUI: Normal click - selecting only {character.name}");
            selectionManager.ClearSelection();
            selectionManager.AddToSelection(character.gameObject);
        }
    }

    /// <summary>
    /// Фокусировка камеры на персонаже
    /// </summary>
    void FocusCameraOnCharacter(Character character)
    {
        Debug.Log($"SimpleCharacterIconsUI: Focusing camera on {character.name}");

        // Ищем CameraController первым - он лучше управляет камерой
        CameraController cameraController = FindObjectOfType<CameraController>();
        if (cameraController != null)
        {
            Debug.Log("SimpleCharacterIconsUI: Found CameraController, using it to focus");

            // Устанавливаем персонажа как цель для фокуса
            cameraController.SetFocusTarget(character.transform);
            Debug.Log($"SimpleCharacterIconsUI: Set focus target to {character.name}");

            // Центрируем камеру на персонаже плавно
            cameraController.CenterOnTarget();
            Debug.Log($"SimpleCharacterIconsUI: Camera focused on {character.name} using CameraController");
            return;
        }

        // Fallback: используем основную камеру напрямую (но без резкого поворота)
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Debug.Log("SimpleCharacterIconsUI: Using main camera directly");
            // Плавно перемещаем камеру к персонажу без изменения поворота
            Vector3 characterPos = character.transform.position;
            Vector3 currentCameraPos = mainCamera.transform.position;
            Vector3 newCameraPos = new Vector3(characterPos.x, currentCameraPos.y, characterPos.z - 8);
            mainCamera.transform.position = newCameraPos;
            Debug.Log($"SimpleCharacterIconsUI: Camera moved to {newCameraPos} (keeping current rotation)");
        }
    }

    /// <summary>
    /// Очистка при уничтожении
    /// </summary>
    void OnDestroy()
    {
        // Отписываемся от событий SelectionManager
        if (selectionManager != null)
        {
            selectionManager.OnSelectionChanged -= OnSelectionChanged;
        }
    }
}

/// <summary>
/// Компонент для управления раскрытием иконки персонажа
/// </summary>
public class SimpleCharacterIcon : MonoBehaviour
{
    private Character character;
    private float normalHeight;
    private float expandedHeight;
    private bool isExpanded = false;
    private GameObject expandedContent;
    private Text expandButtonText;

    public void Initialize(Character characterData, float width, float height)
    {
        character = characterData;
        normalHeight = height;
        expandedHeight = height * 2.5f;
    }

    public void SetExpandButtonText(Text buttonText)
    {
        expandButtonText = buttonText;
    }

    public void ToggleExpanded()
    {
        Debug.Log($"=== SimpleCharacterIcon: TOGGLE CLICKED for {character?.name} ===");

        isExpanded = !isExpanded;

        RectTransform rect = GetComponent<RectTransform>();
        Image bg = GetComponent<Image>();

        if (isExpanded)
        {
            // Раскрываем
            Debug.Log($"SimpleCharacterIcon: Expanding {character.name}");
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, expandedHeight);
            bg.color = new Color(0.2f, 0.2f, 0.8f, 0.95f); // Темно-синий когда раскрыт
            CreateExpandedContent();

            // Изменяем текст кнопки на "i" (свернуть)
            if (expandButtonText != null)
            {
                expandButtonText.text = "i";
            }

            // Переместим иконку на передний план чтобы она была поверх других
            transform.SetAsLastSibling();

            Debug.Log($"SimpleCharacterIcon: {character.name} expanded to size {rect.sizeDelta}");
        }
        else
        {
            // Сворачиваем
            Debug.Log($"SimpleCharacterIcon: Collapsing {character.name}");
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, normalHeight);
            bg.color = new Color(0, 1, 0, 1f); // Зеленый когда свернут
            DestroyExpandedContent();

            // Изменяем текст кнопки на "i" (раскрыть)
            if (expandButtonText != null)
            {
                expandButtonText.text = "i";
            }

            Debug.Log($"SimpleCharacterIcon: {character.name} collapsed to size {rect.sizeDelta}");
        }
    }

    void CreateExpandedContent()
    {
        if (expandedContent != null) return;

        expandedContent = new GameObject("ExpandedContent");
        expandedContent.transform.SetParent(transform, false);

        RectTransform expandedRect = expandedContent.AddComponent<RectTransform>();
        expandedRect.anchorMin = new Vector2(0, 0);
        expandedRect.anchorMax = new Vector2(1, 0.6f);
        expandedRect.offsetMin = Vector2.zero;
        expandedRect.offsetMax = Vector2.zero;

        // Подробная информация
        GameObject detailsGO = new GameObject("DetailsText");
        detailsGO.transform.SetParent(expandedContent.transform, false);

        Text detailsText = detailsGO.AddComponent<Text>();
        detailsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        detailsText.fontSize = 10;
        detailsText.color = Color.white;
        detailsText.alignment = TextAnchor.UpperLeft;
        detailsText.text = character.GetCharacterInfo();

        RectTransform detailsRect = detailsGO.GetComponent<RectTransform>();
        detailsRect.anchorMin = Vector2.zero;
        detailsRect.anchorMax = Vector2.one;
        detailsRect.offsetMin = new Vector2(5, 5);
        detailsRect.offsetMax = new Vector2(-5, -5);
    }

    void DestroyExpandedContent()
    {
        if (expandedContent != null)
        {
            DestroyImmediate(expandedContent);
            expandedContent = null;
        }
    }
}