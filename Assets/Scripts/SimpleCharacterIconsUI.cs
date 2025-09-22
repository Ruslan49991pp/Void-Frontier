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
    public Color normalBackgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f); // Темно-серый
    public Color selectedBackgroundColor = new Color(1, 0.8f, 0, 1f); // Оранжевый
    public Color selectedBorderColor = new Color(1, 1, 0, 1f); // Желтый

    private Canvas canvas;
    private GameObject iconsContainer;
    private Dictionary<Character, GameObject> characterIcons = new Dictionary<Character, GameObject>();
    private SelectionManager selectionManager;

    void Start()
    {


        try
        {
            // Ищем SelectionManager для отслеживания выделения
            selectionManager = FindObjectOfType<SelectionManager>();
            if (selectionManager != null)
            {
                selectionManager.OnSelectionChanged += OnSelectionChanged;

            }
            else
            {

            }

            CreateUI();


            // Отложенный поиск персонажей - даем время системе загрузиться

            Invoke("FindAndCreateIcons", 1f);
            Invoke("FindAndCreateIcons", 3f);

            // Обновляем цвета иконок через 4 секунды после создания
            Invoke("RefreshIconColors", 4f);


        }
        catch (System.Exception)
        {

        }
    }

    void Update()
    {
        // Проверяем новых персонажей каждые 2 секунды
        if (Time.time % 2f < Time.deltaTime)
        {
            CheckForNewCharacters();
        }

        // Обновляем HP полоски каждый кадр
        UpdateHealthBars();
    }

    void CreateUI()
    {


        try
        {
            // Создаем Canvas

            GameObject canvasGO = new GameObject("SimpleCharacterIconsCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // Снижаем приоритет


            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();


            // Создаем контейнер для иконок

            iconsContainer = new GameObject("IconsContainer");
            iconsContainer.transform.SetParent(canvas.transform, false);

            RectTransform containerRect = iconsContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 1);
            containerRect.anchorMax = new Vector2(1, 1);
            containerRect.pivot = new Vector2(0.5f, 1);
            containerRect.anchoredPosition = new Vector2(0, -10);
            containerRect.sizeDelta = new Vector2(0, 60); // Горизонтальная панель вверху


            // Добавляем прозрачный фон для контейнера (убираем красный)
            Image containerBg = iconsContainer.AddComponent<Image>();
            containerBg.color = new Color(0, 0, 0, 0.3f); // Полупрозрачный темный фон


            // VerticalLayoutGroup мешает - отключаем и делаем ручное позиционирование
            // VerticalLayoutGroup layout = iconsContainer.AddComponent<VerticalLayoutGroup>();


            // ContentSizeFitter может мешать - пока отключаем
            // ContentSizeFitter fitter = iconsContainer.AddComponent<ContentSizeFitter>();
            // fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;



            // Добавляем тестовую метку чтобы убедиться что контейнер виден
            CreateTestLabel();
        }
        catch (System.Exception)
        {


        }
    }

    void CreateTestLabel()
    {


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


    }

    void FindAndCreateIcons()
    {


        try
        {
            if (iconsContainer == null)
            {

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



            if (characters.Length == 0)
            {


                // Попробуем найти объекты с компонентом Character через другие способы
                GameObject[] allObjects = FindObjectsOfType<GameObject>();
                int charCount = 0;
                foreach (GameObject obj in allObjects)
                {
                    if (obj.GetComponent<Character>() != null)
                    {
                        charCount++;

                    }
                }

            }
            else
            {

                for (int i = 0; i < characters.Length; i++)
                {
                    Character character = characters[i];


                    // Фильтруем персонажей: исключаем шаблоны и NPC
                    if (ShouldShowCharacterIcon(character))
                    {
                        if (!characterIcons.ContainsKey(character))
                        {

                            CreateCharacterIcon(character);
                        }
                        else
                        {

                        }
                    }
                    else
                    {

                    }
                }
            }


        }
        catch (System.Exception)
        {


        }
    }

    void CheckForNewCharacters()
    {
        Character[] characters = FindObjectsOfType<Character>();
        foreach (Character character in characters)
        {
            if (!characterIcons.ContainsKey(character) && character != null && ShouldShowCharacterIcon(character))
            {

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


        try
        {
            if (character == null)
            {

                return;
            }

            if (iconsContainer == null)
            {

                return;
            }



            if (character.characterData == null)
            {

                character.GenerateRandomCharacter();
            }

            // Основной объект иконки

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



            // Фон иконки
            Image iconBg = iconGO.AddComponent<Image>();
            iconBg.color = normalBackgroundColor;


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

                iconComponent.ToggleExpanded();
            });


            // Кнопка раскрытия должна быть поверх основной кнопки
            expandButtonGO.transform.SetAsLastSibling();

            // Кнопка использует родительский Canvas


            // Убеждаемся что кнопка на переднем плане
            expandButtonGO.transform.SetAsLastSibling();

            characterIcons[character] = iconGO;


        }
        catch (System.Exception)
        {



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

        // Показываем иконки только для дружественных персонажей
        if (!character.IsPlayerCharacter()) return false;

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



        if (isDoubleClick)
        {
            // Двойной клик - фокусируем камеру на персонаже

            FocusCameraOnCharacter(character);
        }
        else
        {
            // Одинарный клик - выделение с задержкой чтобы избежать конфликтов

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


        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            // Ctrl + клик - переключаем выделение

            selectionManager.ToggleSelection(character.gameObject);
        }
        else
        {
            // Обычный клик - очищаем все выделение и выделяем только этого персонажа

            selectionManager.ClearSelection();
            selectionManager.AddToSelection(character.gameObject);
        }
    }

    /// <summary>
    /// Фокусировка камеры на персонаже
    /// </summary>
    void FocusCameraOnCharacter(Character character)
    {


        // Ищем CameraController первым - он лучше управляет камерой
        CameraController cameraController = FindObjectOfType<CameraController>();
        if (cameraController != null)
        {


            // Устанавливаем персонажа как цель для фокуса
            cameraController.SetFocusTarget(character.transform);


            // Центрируем камеру на персонаже плавно
            cameraController.CenterOnTarget();

            return;
        }

        // Fallback: используем основную камеру напрямую (но без резкого поворота)
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {

            // Плавно перемещаем камеру к персонажу без изменения поворота
            Vector3 characterPos = character.transform.position;
            Vector3 currentCameraPos = mainCamera.transform.position;
            Vector3 newCameraPos = new Vector3(characterPos.x, currentCameraPos.y, characterPos.z - 8);
            mainCamera.transform.position = newCameraPos;

        }
    }

    /// <summary>
    /// Обновить полоски здоровья всех персонажей
    /// </summary>
    void UpdateHealthBars()
    {
        foreach (var kvp in characterIcons)
        {
            Character character = kvp.Key;
            GameObject iconGO = kvp.Value;

            if (character == null || iconGO == null) continue;

            // Находим полоску здоровья
            Transform healthBarTransform = iconGO.transform.Find("HealthBar");
            if (healthBarTransform != null)
            {
                Image healthBar = healthBarTransform.GetComponent<Image>();
                if (healthBar != null)
                {
                    // Обновляем цвет и ширину полоски здоровья
                    float healthPercent = character.characterData.health / character.characterData.maxHealth;
                    healthBar.color = Color.Lerp(Color.red, Color.green, healthPercent);

                    // Обновляем ширину полоски
                    RectTransform healthRect = healthBarTransform.GetComponent<RectTransform>();
                    if (healthRect != null)
                    {
                        healthRect.anchorMax = new Vector2(0.05f + 0.9f * healthPercent, 0.4f);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Нанести урон персонажу для тестирования
    /// </summary>
    [System.Obsolete("Используется только для тестирования")]
    public void TestDamageCharacter(Character character, float damage)
    {
        if (character != null)
        {
            character.TakeDamage(damage);
        }
    }

    /// <summary>
    /// Восстановить здоровье персонажу для тестирования
    /// </summary>
    [System.Obsolete("Используется только для тестирования")]
    public void TestHealCharacter(Character character, float amount)
    {
        if (character != null)
        {
            character.Heal(amount);
        }
    }

    /// <summary>
    /// Принудительно обновить цвета всех иконок
    /// </summary>
    public void RefreshIconColors()
    {
        if (selectionManager == null) return;

        var selectedObjects = selectionManager.GetSelectedObjects();

        foreach (var kvp in characterIcons)
        {
            Character character = kvp.Key;
            GameObject iconGO = kvp.Value;

            if (character == null || iconGO == null) continue;

            bool isSelected = selectedObjects.Contains(character.gameObject);
            UpdateIconSelectionVisual(iconGO, isSelected);
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


        isExpanded = !isExpanded;

        RectTransform rect = GetComponent<RectTransform>();
        Image bg = GetComponent<Image>();

        if (isExpanded)
        {
            // Раскрываем

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


        }
        else
        {
            // Сворачиваем

            rect.sizeDelta = new Vector2(rect.sizeDelta.x, normalHeight);
            bg.color = new Color(0, 1, 0, 1f); // Зеленый когда свернут
            DestroyExpandedContent();

            // Изменяем текст кнопки на "i" (раскрыть)
            if (expandButtonText != null)
            {
                expandButtonText.text = "i";
            }


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