using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Компонент для управления иконками персонажей игрока в Canvas
/// Динамически создает и обновляет иконки в зависимости от состава команды
/// БЕЗ добавления дополнительных компонентов - работает напрямую с UI элементами
/// </summary>
public class PlayerCharacterIconsManager : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform iconsContainer; // Контейнер для иконок (Portraits)
    public GameObject characterPortraitPrefab; // Префаб CharacterPortrait

    [Header("Settings")]
    public Color normalBackgroundColor = new Color(0.48754004f, 0.56683874f, 0.6226415f, 1f);
    public Color selectedBackgroundColor = Color.yellow;
    public Color lowHealthBackgroundColor = Color.red;
    public float lowHealthThreshold = 0.3f;

    // Класс для хранения ссылок на UI элементы иконки
    private class IconData
    {
        public GameObject iconGameObject;
        public Image backgroundImage;
        public Image avatarImage;
        public Image healthBarFill;
        public TMPro.TMP_Text nameLabel;
    }

    // Словарь для связи персонажа с его иконкой
    private Dictionary<Character, IconData> characterIcons = new Dictionary<Character, IconData>();

    // Ссылки на системы
    private SelectionManager selectionManager;

    void Awake()
    {
        Debug.Log("[PlayerCharacterIconsManager] Awake called");

        // Находим SelectionManager
        selectionManager = FindObjectOfType<SelectionManager>();
        if (selectionManager != null)
        {
            Debug.Log("[PlayerCharacterIconsManager] SelectionManager found");
        }

        // Автопоиск контейнера если не назначен
        if (iconsContainer == null)
        {
            iconsContainer = GetComponent<RectTransform>();
            Debug.Log($"[PlayerCharacterIconsManager] Using self as iconsContainer: {iconsContainer.name}");
        }
    }

    void Start()
    {
        Debug.Log("[PlayerCharacterIconsManager] Start called");

        // Подписываемся на события выделения
        if (selectionManager != null)
        {
            selectionManager.OnSelectionChanged += OnSelectionChanged;
            Debug.Log("[PlayerCharacterIconsManager] Subscribed to SelectionManager.OnSelectionChanged");
        }

        // Подписываемся на событие создания персонажей игрока
        Character.OnPlayerCharacterSpawned += OnPlayerCharacterSpawned;
        Debug.Log("[PlayerCharacterIconsManager] Subscribed to Character.OnPlayerCharacterSpawned");

        // Проверяем, есть ли уже созданные персонажи игрока
        StartCoroutine(CheckForExistingCharacters());
    }

    System.Collections.IEnumerator CheckForExistingCharacters()
    {
        // Ждем 1 кадр чтобы все Start() методы выполнились
        yield return null;

        Debug.Log("[PlayerCharacterIconsManager] Checking for existing player characters...");

        Character[] allCharacters = FindObjectsOfType<Character>();
        int foundCount = 0;

        foreach (Character character in allCharacters)
        {
            if (character.IsPlayerCharacter() && !characterIcons.ContainsKey(character))
            {
                Debug.Log($"[PlayerCharacterIconsManager] Found existing player character: {character.GetFullName()}");
                OnPlayerCharacterSpawned(character);
                foundCount++;
            }
        }

        Debug.Log($"[PlayerCharacterIconsManager] Found {foundCount} existing player characters");
        Debug.Log("[PlayerCharacterIconsManager] CheckForExistingCharacters coroutine finished");
    }

    void OnPlayerCharacterSpawned(Character character)
    {
        Debug.Log($"[PlayerCharacterIconsManager] OnPlayerCharacterSpawned called for {character.GetFullName()}");

        if (!character.IsPlayerCharacter())
        {
            Debug.LogWarning($"[PlayerCharacterIconsManager] Character {character.GetFullName()} is not a player character!");
            return;
        }

        if (characterIcons.ContainsKey(character))
        {
            Debug.Log($"[PlayerCharacterIconsManager] Icon for {character.GetFullName()} already exists");
            return;
        }

        AddCharacterIcon(character);
    }

    void Update()
    {
        // ВРЕМЕННО ОТКЛЮЧЕНО для отладки зависания
        // if (characterIcons.Count > 0)
        // {
        //     UpdateIconStates();
        // }
    }

    void OnDestroy()
    {
        if (selectionManager != null)
        {
            selectionManager.OnSelectionChanged -= OnSelectionChanged;
        }

        Character.OnPlayerCharacterSpawned -= OnPlayerCharacterSpawned;
    }

    /// <summary>
    /// Добавить иконку для персонажа - БЕЗ добавления компонента
    /// </summary>
    public void AddCharacterIcon(Character character)
    {
        if (character == null || characterIcons.ContainsKey(character))
        {
            return;
        }

        Debug.Log($"[PlayerCharacterIconsManager] Adding icon for character: {character.GetFullName()}");

        if (characterPortraitPrefab == null || iconsContainer == null)
        {
            Debug.LogError("[PlayerCharacterIconsManager] Missing prefab or container!");
            return;
        }

        try
        {
            // Создаем экземпляр префаба
            GameObject iconGO = Instantiate(characterPortraitPrefab, iconsContainer);
            iconGO.name = $"Portrait_{character.GetFullName()}";

            // Создаем структуру для хранения ссылок
            IconData iconData = new IconData();
            iconData.iconGameObject = iconGO;

            // Находим UI элементы НАПРЯМУЮ, БЕЗ добавления компонента
            Transform bgTransform = iconGO.transform.Find("Background");
            if (bgTransform != null)
            {
                iconData.backgroundImage = bgTransform.GetComponent<Image>();
            }

            Transform avatarTransform = iconGO.transform.Find("Avatar");
            if (avatarTransform != null)
            {
                iconData.avatarImage = avatarTransform.GetComponent<Image>();
                if (iconData.avatarImage == null)
                {
                    Transform imageTransform = avatarTransform.Find("Image");
                    if (imageTransform != null)
                    {
                        iconData.avatarImage = imageTransform.GetComponent<Image>();
                    }
                }
            }

            Transform healthBarTransform = iconGO.transform.Find("HealthBar");
            if (healthBarTransform != null)
            {
                Transform helthbarTransform = healthBarTransform.Find("Helthbar");
                if (helthbarTransform != null)
                {
                    iconData.healthBarFill = helthbarTransform.GetComponent<Image>();
                    if (iconData.healthBarFill != null)
                    {
                        iconData.healthBarFill.type = Image.Type.Filled;
                        iconData.healthBarFill.fillMethod = Image.FillMethod.Horizontal;
                    }
                }
            }

            Transform nameLabelTransform = iconGO.transform.Find("NameLabel");
            if (nameLabelTransform != null)
            {
                iconData.nameLabel = nameLabelTransform.GetComponent<TMPro.TMP_Text>();
            }

            Debug.Log($"[PlayerCharacterIconsManager] Found UI elements: BG={iconData.backgroundImage != null}, Avatar={iconData.avatarImage != null}, HP={iconData.healthBarFill != null}, Name={iconData.nameLabel != null}");

            // Устанавливаем имя
            if (iconData.nameLabel != null)
            {
                iconData.nameLabel.text = character.characterData.firstName;
            }

            // Устанавливаем здоровье
            if (iconData.healthBarFill != null)
            {
                iconData.healthBarFill.fillAmount = character.GetHealthPercent();
            }

            // Сохраняем в словарь
            characterIcons[character] = iconData;

            Debug.Log($"[PlayerCharacterIconsManager] Icon created successfully for {character.GetFullName()}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerCharacterIconsManager] Error creating icon: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// Удалить иконку персонажа
    /// </summary>
    public void RemoveCharacterIcon(Character character)
    {
        if (character == null || !characterIcons.ContainsKey(character))
        {
            return;
        }

        IconData iconData = characterIcons[character];
        if (iconData.iconGameObject != null)
        {
            Destroy(iconData.iconGameObject);
        }

        characterIcons.Remove(character);
    }

    /// <summary>
    /// Обновление состояния всех иконок
    /// </summary>
    void UpdateIconStates()
    {
        foreach (var kvp in characterIcons)
        {
            Character character = kvp.Key;
            IconData iconData = kvp.Value;

            if (character != null && iconData.healthBarFill != null)
            {
                float healthPercent = character.GetHealthPercent();
                iconData.healthBarFill.fillAmount = Mathf.Clamp01(healthPercent);

                // Цвет HP бара
                if (healthPercent > 0.6f)
                {
                    iconData.healthBarFill.color = new Color(0.24913555f, 0.5849056f, 0, 1);
                }
                else if (healthPercent > 0.3f)
                {
                    iconData.healthBarFill.color = Color.yellow;
                }
                else
                {
                    iconData.healthBarFill.color = Color.red;
                }

                // Цвет фона
                bool isSelected = selectionManager != null && selectionManager.IsSelected(character.gameObject);
                Color targetColor = normalBackgroundColor;

                if (isSelected)
                {
                    targetColor = selectedBackgroundColor;
                }
                else if (healthPercent <= lowHealthThreshold)
                {
                    targetColor = lowHealthBackgroundColor;
                }

                if (iconData.backgroundImage != null)
                {
                    iconData.backgroundImage.color = targetColor;
                }
            }
        }
    }

    void OnSelectionChanged(List<GameObject> selectedObjects)
    {
        // Обновление цветов происходит в UpdateIconStates()
    }

    public int GetIconCount()
    {
        return characterIcons.Count;
    }
}
