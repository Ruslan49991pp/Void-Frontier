using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Управление иконками персонажей на Canvas - простая версия БЕЗ добавления компонентов
/// </summary>
public class CanvasCharacterIconsManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject characterPortraitPrefab;
    public RectTransform iconsContainer;

    private struct IconData
    {
        public GameObject iconObject;
        public Image background;
        public Image avatarImage;
        public TextMeshProUGUI nameLabel;
        public Button button;
        public Button inventoryButton;
        public float lastClickTime;
    }

    private Dictionary<Character, IconData> characterIcons = new Dictionary<Character, IconData>();
    private SelectionManager selectionManager;

    // Список портретов людей для циклического использования
    private string[] humanPortraits = new string[]
    {
        "Icons/Characters/Humans/Character_Ico",
        "Icons/Characters/Humans/Character_2_Ico",
        "Icons/Characters/Humans/Character_3_Ico"
    };

    private int currentHumanPortraitIndex = 0;

    void Start()
    {
        selectionManager = FindObjectOfType<SelectionManager>();
        if (selectionManager != null)
        {
            selectionManager.OnSelectionChanged += OnSelectionChanged;
        }

        // Создаем иконки через 0.5 секунд
        Invoke(nameof(CreateIcons), 0.5f);
    }

    void CreateIcons()
    {
        Character[] allCharacters = FindObjectsOfType<Character>();

        foreach (Character character in allCharacters)
        {
            if (character.IsPlayerCharacter())
            {
                AddCharacter(character);
            }
        }
    }

    public void AddCharacter(Character character)
    {
        if (character == null || characterIcons.ContainsKey(character))
            return;

        if (characterPortraitPrefab == null || iconsContainer == null)
            return;

        // Создаем иконку из префаба
        GameObject iconGO = Instantiate(characterPortraitPrefab, iconsContainer);
        iconGO.name = $"Portrait_{character.GetFullName()}";

        // Создаем структуру данных
        IconData iconData = new IconData();
        iconData.iconObject = iconGO;
        iconData.lastClickTime = 0f;

        // Находим элементы БЕЗ добавления компонентов
        Transform bgTransform = iconGO.transform.Find("Background");
        if (bgTransform != null)
        {
            iconData.background = bgTransform.GetComponent<Image>();
        }

        Transform nameLabelTransform = iconGO.transform.Find("NameLabel");
        if (nameLabelTransform != null)
        {
            iconData.nameLabel = nameLabelTransform.GetComponent<TextMeshProUGUI>();
            if (iconData.nameLabel != null)
            {
                iconData.nameLabel.text = character.characterData.firstName;
            }
        }

        // Находим Avatar для загрузки портрета
        Transform avatarTransform = iconGO.transform.Find("Avatar");
        if (avatarTransform != null)
        {
            iconData.avatarImage = avatarTransform.GetComponent<Image>();

            // Загружаем портрет персонажа
            if (iconData.avatarImage != null)
            {
                LoadCharacterPortrait(iconData.avatarImage, character);
            }
        }

        // Настраиваем кнопку для клика
        Image iconImage = iconGO.GetComponent<Image>();
        if (iconImage == null)
        {
            iconImage = iconGO.AddComponent<Image>();
            iconImage.color = new Color(0, 0, 0, 0);
        }
        iconImage.raycastTarget = true;

        iconData.button = iconGO.GetComponent<Button>();
        if (iconData.button == null)
        {
            iconData.button = iconGO.AddComponent<Button>();
        }

        iconData.button.targetGraphic = iconImage;
        iconData.button.transition = Selectable.Transition.None;

        Navigation nav = iconData.button.navigation;
        nav.mode = Navigation.Mode.None;
        iconData.button.navigation = nav;

        // Обработчик клика с замыканием
        Character capturedCharacter = character;
        iconData.button.onClick.AddListener(() => OnIconClicked(capturedCharacter));

        // Добавляем компонент для следования камеры при зажатии ЛКМ
        PortraitCameraFollow cameraFollow = iconGO.GetComponent<PortraitCameraFollow>();
        if (cameraFollow == null)
        {
            cameraFollow = iconGO.AddComponent<PortraitCameraFollow>();
        }
        cameraFollow.Initialize(capturedCharacter);

        // Находим и привязываем кнопку инвентаря
        Transform inventoryButtonTransform = iconGO.transform.Find("InventoryButton");
        if (inventoryButtonTransform != null)
        {
            iconData.inventoryButton = inventoryButtonTransform.GetComponent<Button>();
            if (iconData.inventoryButton != null)
            {
                // Добавляем обработчик для открытия инвентаря конкретного персонажа
                iconData.inventoryButton.onClick.AddListener(() => OnInventoryButtonClicked(capturedCharacter));
            }
            else
            {
                Debug.LogWarning($"[CanvasCharacterIconsManager] InventoryButton found but has no Button component for {character.GetFullName()}");
            }
        }
        else
        {
            Debug.LogWarning($"[CanvasCharacterIconsManager] InventoryButton not found in portrait for {character.GetFullName()}");
        }

        characterIcons[character] = iconData;
    }

    void OnIconClicked(Character character)
    {
        if (character == null || selectionManager == null)
            return;

        if (!characterIcons.TryGetValue(character, out IconData iconData))
            return;

        float currentTime = Time.time;
        bool isDoubleClick = (currentTime - iconData.lastClickTime < 0.5f);
        iconData.lastClickTime = currentTime;
        characterIcons[character] = iconData;

        // Ctrl + клик
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            selectionManager.ToggleSelection(character.gameObject);
        }
        else
        {
            selectionManager.ClearSelection();
            selectionManager.AddToSelection(character.gameObject);
        }

        // Двойной клик - фокус камеры
        if (isDoubleClick)
        {
            CameraController cameraController = FindObjectOfType<CameraController>();
            if (cameraController != null)
            {
                cameraController.SetFocusTarget(character.transform);
                cameraController.CenterOnTarget();
            }
        }
    }

    /// <summary>
    /// Обработчик клика по кнопке инвентаря конкретного персонажа
    /// </summary>
    void OnInventoryButtonClicked(Character character)
    {
        if (character == null)
        {
            Debug.LogWarning("[CanvasCharacterIconsManager] OnInventoryButtonClicked: character is null");
            return;
        }

        // Находим InventoryUI в сцене
        InventoryUI inventoryUI = FindObjectOfType<InventoryUI>();
        if (inventoryUI == null)
        {
            Debug.LogError("[CanvasCharacterIconsManager] InventoryUI not found in scene");
            return;
        }

        // Получаем инвентарь персонажа
        Inventory inventory = character.GetInventory();
        if (inventory == null)
        {
            Debug.LogWarning($"[CanvasCharacterIconsManager] Character {character.GetFullName()} has no inventory");
            return;
        }

        // Открываем инвентарь персонажа
        inventoryUI.SetCurrentInventory(inventory, character);
        inventoryUI.ShowInventory();
    }

    void OnSelectionChanged(List<GameObject> selectedObjects)
    {
        foreach (var kvp in characterIcons)
        {
            Character character = kvp.Key;
            IconData iconData = kvp.Value;

            if (character != null && iconData.background != null)
            {
                bool isSelected = false;
                foreach (GameObject selectedObj in selectedObjects)
                {
                    if (selectedObj != null && selectedObj.GetComponent<Character>() == character)
                    {
                        isSelected = true;
                        break;
                    }
                }

                iconData.background.color = isSelected
                    ? new Color(1f, 0.8f, 0f, 1f)
                    : new Color(0.49f, 0.57f, 0.62f, 1f);
            }
        }
    }

    /// <summary>
    /// Загрузить портрет персонажа из Resources
    /// </summary>
    void LoadCharacterPortrait(Image avatarImage, Character character)
    {
        // Получаем следующий портрет по кругу
        string portraitPath = humanPortraits[currentHumanPortraitIndex];
        currentHumanPortraitIndex = (currentHumanPortraitIndex + 1) % humanPortraits.Length;

        // Загружаем Sprite из Resources
        Sprite portraitSprite = Resources.Load<Sprite>(portraitPath);

        if (portraitSprite != null)
        {
            avatarImage.sprite = portraitSprite;
        }
        else
        {
            Debug.LogWarning($"[CanvasCharacterIconsManager] Failed to load portrait from '{portraitPath}' for {character.GetFullName()}");
        }
    }

    void OnDestroy()
    {
        if (selectionManager != null)
        {
            selectionManager.OnSelectionChanged -= OnSelectionChanged;
        }
    }
}
