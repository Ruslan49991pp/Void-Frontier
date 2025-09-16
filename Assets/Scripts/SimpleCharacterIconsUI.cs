using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Простая система иконок персонажей - более надежная версия
/// </summary>
public class SimpleCharacterIconsUI : MonoBehaviour
{
    [Header("Settings")]
    public float iconWidth = 150f;
    public float iconHeight = 50f;
    public float spacing = 5f;

    private Canvas canvas;
    private GameObject iconsContainer;
    private Dictionary<Character, GameObject> characterIcons = new Dictionary<Character, GameObject>();

    void Start()
    {
        Debug.Log("=== SimpleCharacterIconsUI: Starting ===");

        try
        {
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
            containerRect.anchorMax = new Vector2(0, 1);
            containerRect.pivot = new Vector2(0, 1);
            containerRect.anchoredPosition = new Vector2(10, -10);
            containerRect.sizeDelta = new Vector2(200, 600); // Уже и выше
            Debug.Log($"SimpleCharacterIconsUI: Container positioned at {containerRect.anchoredPosition} with size {containerRect.sizeDelta}");

            // Добавляем фон для видимости
            Image containerBg = iconsContainer.AddComponent<Image>();
            containerBg.color = new Color(1, 0, 0, 0.8f); // КРАСНЫЙ фон для лучшей видимости
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
            Debug.Log($"SimpleCharacterIconsUI: Found {characters.Length} characters in scene");

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
            if (!characterIcons.ContainsKey(character) && character != null)
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

            // Принудительно устанавливаем позицию (игнорируем Layout Group)
            iconRect.anchorMin = new Vector2(0, 1);
            iconRect.anchorMax = new Vector2(0, 1);
            iconRect.pivot = new Vector2(0, 1);
            iconRect.anchoredPosition = new Vector2(10, -10 - (characterIcons.Count * 60)); // Размещаем вертикально

            Debug.Log($"SimpleCharacterIconsUI: Icon size: {iconRect.sizeDelta}, position: {iconRect.anchoredPosition}");

            // Фон иконки - делаем более яркий для тестирования
            Image iconBg = iconGO.AddComponent<Image>();
            iconBg.color = new Color(0, 1, 0, 1f); // ЗЕЛЕНЫЙ фон для максимальной видимости
            Debug.Log("SimpleCharacterIconsUI: Icon background added");

            // Кнопка для раскрытия
            Button iconButton = iconGO.AddComponent<Button>();
            SimpleCharacterIcon iconComponent = iconGO.AddComponent<SimpleCharacterIcon>();
            iconComponent.Initialize(character, iconWidth, iconHeight);
            iconButton.onClick.AddListener(() => iconComponent.ToggleExpanded());
            Debug.Log("SimpleCharacterIconsUI: Button and component added");

            // Создаем содержимое иконки
            Debug.Log("SimpleCharacterIconsUI: Creating icon content");
            CreateIconContent(iconGO, character);

            characterIcons[character] = iconGO;

            Debug.Log($"SimpleCharacterIconsUI: ✅ Icon created successfully for {character.name}! Total icons: {characterIcons.Count}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SimpleCharacterIconsUI: ERROR creating icon: {e.Message}");
            Debug.LogError($"SimpleCharacterIconsUI: Stack trace: {e.StackTrace}");
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

    public void Initialize(Character characterData, float width, float height)
    {
        character = characterData;
        normalHeight = height;
        expandedHeight = height * 2.5f;
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
            bg.color = new Color(0, 0, 1, 0.9f); // Синий когда раскрыт
            CreateExpandedContent();
            Debug.Log($"SimpleCharacterIcon: {character.name} expanded to size {rect.sizeDelta}");
        }
        else
        {
            // Сворачиваем
            Debug.Log($"SimpleCharacterIcon: Collapsing {character.name}");
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, normalHeight);
            bg.color = new Color(0, 1, 0, 1f); // Зеленый когда свернут
            DestroyExpandedContent();
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