using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// РЈРїСЂР°РІР»РµРЅРёРµ РёРєРѕРЅРєР°РјРё РїРµСЂСЃРѕРЅР°Р¶РµР№ РЅР° Canvas - РїСЂРѕСЃС‚Р°СЏ РІРµСЂСЃРёСЏ Р‘Р•Р— РґРѕР±Р°РІР»РµРЅРёСЏ РєРѕРјРїРѕРЅРµРЅС‚РѕРІ
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
        public Image healthBarFill;  // Р”РћР‘РђР’Р›Р•РќРћ: Health bar
        public TextMeshProUGUI nameLabel;
        public Button button;
        public Button inventoryButton;
        public float lastClickTime;
    }

    private Dictionary<Character, IconData> characterIcons = new Dictionary<Character, IconData>();
    private SelectionManager selectionManager;

    // РЎРїРёСЃРѕРє РїРѕСЂС‚СЂРµС‚РѕРІ Р»СЋРґРµР№ РґР»СЏ С†РёРєР»РёС‡РµСЃРєРѕРіРѕ РёСЃРїРѕР»СЊР·РѕРІР°РЅРёСЏ
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

        // Р”РћР‘РђР’Р›Р•РќРћ: РџРѕРґРїРёСЃРєР° РЅР° СЃРѕР±С‹С‚РёСЏ СѓСЂРѕРЅР°
        EventBus.Subscribe<CharacterDamagedEvent>(OnCharacterDamaged);

        // РЎРѕР·РґР°РµРј РёРєРѕРЅРєРё С‡РµСЂРµР· 0.5 СЃРµРєСѓРЅРґ
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

        // РЎРѕР·РґР°РµРј РёРєРѕРЅРєСѓ РёР· РїСЂРµС„Р°Р±Р°
        GameObject iconGO = Instantiate(characterPortraitPrefab, iconsContainer);
        iconGO.name = $"Portrait_{character.GetFullName()}";

        // РЎРѕР·РґР°РµРј СЃС‚СЂСѓРєС‚СѓСЂСѓ РґР°РЅРЅС‹С…
        IconData iconData = new IconData();
        iconData.iconObject = iconGO;
        iconData.lastClickTime = 0f;

        // РќР°С…РѕРґРёРј СЌР»РµРјРµРЅС‚С‹ Р‘Р•Р— РґРѕР±Р°РІР»РµРЅРёСЏ РєРѕРјРїРѕРЅРµРЅС‚РѕРІ
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

        // Р”РћР‘РђР’Р›Р•РќРћ: РќР°С…РѕРґРёРј Health Bar
        Transform healthBarTransform = iconGO.transform.Find("HealthBar");
        if (healthBarTransform != null)
        {
            Transform healthBarPlaneTransform = healthBarTransform.Find("HealthBar_Plane");
            if (healthBarPlaneTransform != null)
            {
                iconData.healthBarFill = healthBarPlaneTransform.GetComponent<Image>();
                if (iconData.healthBarFill != null)
                {
                    // РЈСЃС‚Р°РЅР°РІР»РёРІР°РµРј РЅР°С‡Р°Р»СЊРЅРѕРµ Р·РґРѕСЂРѕРІСЊРµ С‡РµСЂРµР· scale.x
                    RectTransform healthBarRect = iconData.healthBarFill.GetComponent<RectTransform>();
                    if (healthBarRect != null)
                    {
                        float healthPercent = character.GetHealthPercent();
                        Vector3 currentScale = healthBarRect.localScale;
                        healthBarRect.localScale = new Vector3(healthPercent, currentScale.y, currentScale.z);
                    }
                }
            }
            else
            {
            }
        }

        // РќР°С…РѕРґРёРј Avatar РґР»СЏ Р·Р°РіСЂСѓР·РєРё РїРѕСЂС‚СЂРµС‚Р°
        Transform avatarTransform = iconGO.transform.Find("Avatar");
        if (avatarTransform != null)
        {
            iconData.avatarImage = avatarTransform.GetComponent<Image>();

            // Р—Р°РіСЂСѓР¶Р°РµРј РїРѕСЂС‚СЂРµС‚ РїРµСЂСЃРѕРЅР°Р¶Р°
            if (iconData.avatarImage != null)
            {
                LoadCharacterPortrait(iconData.avatarImage, character);
            }
        }

        // РќР°СЃС‚СЂР°РёРІР°РµРј РєРЅРѕРїРєСѓ РґР»СЏ РєР»РёРєР°
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

        // РћР±СЂР°Р±РѕС‚С‡РёРє РєР»РёРєР° СЃ Р·Р°РјС‹РєР°РЅРёРµРј
        Character capturedCharacter = character;
        iconData.button.onClick.AddListener(() => OnIconClicked(capturedCharacter));

        // Р”РѕР±Р°РІР»СЏРµРј РєРѕРјРїРѕРЅРµРЅС‚ РґР»СЏ СЃР»РµРґРѕРІР°РЅРёСЏ РєР°РјРµСЂС‹ РїСЂРё Р·Р°Р¶Р°С‚РёРё Р›РљРњ
        PortraitCameraFollow cameraFollow = iconGO.GetComponent<PortraitCameraFollow>();
        if (cameraFollow == null)
        {
            cameraFollow = iconGO.AddComponent<PortraitCameraFollow>();
        }
        cameraFollow.Initialize(capturedCharacter);

        // РќР°С…РѕРґРёРј Рё РїСЂРёРІСЏР·С‹РІР°РµРј РєРЅРѕРїРєСѓ РёРЅРІРµРЅС‚Р°СЂСЏ
        Transform inventoryButtonTransform = iconGO.transform.Find("InventoryButton");
        if (inventoryButtonTransform != null)
        {
            iconData.inventoryButton = inventoryButtonTransform.GetComponent<Button>();
            if (iconData.inventoryButton != null)
            {
                // Р”РѕР±Р°РІР»СЏРµРј РѕР±СЂР°Р±РѕС‚С‡РёРє РґР»СЏ РѕС‚РєСЂС‹С‚РёСЏ РёРЅРІРµРЅС‚Р°СЂСЏ РєРѕРЅРєСЂРµС‚РЅРѕРіРѕ РїРµСЂСЃРѕРЅР°Р¶Р°
                iconData.inventoryButton.onClick.AddListener(() => OnInventoryButtonClicked(capturedCharacter));
            }
            else
            {
            }
        }
        else
        {
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

        // РћРґРёРЅР°СЂРЅС‹Р№ РєР»РёРє - С‚РѕР»СЊРєРѕ РІС‹РґРµР»РµРЅРёРµ
        if (!isDoubleClick)
        {
            // Ctrl + РєР»РёРє
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                selectionManager.ToggleSelection(character.gameObject);
            }
            else
            {
                selectionManager.ClearSelection();
                selectionManager.AddToSelection(character.gameObject);
            }
        }
        // Р”РІРѕР№РЅРѕР№ РєР»РёРє - РІС‹РґРµР»РµРЅРёРµ + С„РѕРєСѓСЃ РєР°РјРµСЂС‹
        else
        {
            // Р’С‹РґРµР»СЏРµРј РїРµСЂСЃРѕРЅР°Р¶Р°
            selectionManager.ClearSelection();
            selectionManager.AddToSelection(character.gameObject);

            // Р¤РѕРєСѓСЃРёСЂСѓРµРј РєР°РјРµСЂСѓ
            CameraController cameraController = FindObjectOfType<CameraController>();
            if (cameraController != null)
            {
                cameraController.SetFocusTarget(character.transform);
                cameraController.CenterOnTarget();
            }
        }
    }

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚С‡РёРє РєР»РёРєР° РїРѕ РєРЅРѕРїРєРµ РёРЅРІРµРЅС‚Р°СЂСЏ РєРѕРЅРєСЂРµС‚РЅРѕРіРѕ РїРµСЂСЃРѕРЅР°Р¶Р°
    /// </summary>
    void OnInventoryButtonClicked(Character character)
    {
        if (character == null)
        {
            return;
        }

        // РќР°С…РѕРґРёРј InventoryUI РІ СЃС†РµРЅРµ
        InventoryUI inventoryUI = FindObjectOfType<InventoryUI>();
        if (inventoryUI == null)
        {
            return;
        }

        // РџРѕР»СѓС‡Р°РµРј РёРЅРІРµРЅС‚Р°СЂСЊ РїРµСЂСЃРѕРЅР°Р¶Р°
        Inventory inventory = character.GetInventory();
        if (inventory == null)
        {
            return;
        }

        // РћС‚РєСЂС‹РІР°РµРј РёРЅРІРµРЅС‚Р°СЂСЊ РїРµСЂСЃРѕРЅР°Р¶Р°
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
    /// Р—Р°РіСЂСѓР·РёС‚СЊ РїРѕСЂС‚СЂРµС‚ РїРµСЂСЃРѕРЅР°Р¶Р° РёР· Resources
    /// </summary>
    void LoadCharacterPortrait(Image avatarImage, Character character)
    {
        // РџРѕР»СѓС‡Р°РµРј СЃР»РµРґСѓСЋС‰РёР№ РїРѕСЂС‚СЂРµС‚ РїРѕ РєСЂСѓРіСѓ
        string portraitPath = humanPortraits[currentHumanPortraitIndex];
        currentHumanPortraitIndex = (currentHumanPortraitIndex + 1) % humanPortraits.Length;

        // Р—Р°РіСЂСѓР¶Р°РµРј Sprite РёР· Resources
        Sprite portraitSprite = Resources.Load<Sprite>(portraitPath);

        if (portraitSprite != null)
        {
            avatarImage.sprite = portraitSprite;
        }
        else
        {
        }
    }

    // Р”РћР‘РђР’Р›Р•РќРћ: Update РґР»СЏ РѕР±РЅРѕРІР»РµРЅРёСЏ health bar РєР°Р¶РґС‹Р№ РєР°РґСЂ
    void Update()
    {
        if (characterIcons.Count > 0)
        {
            UpdateHealthBars();
        }
    }

    // Р”РћР‘РђР’Р›Р•РќРћ: РћР±РЅРѕРІР»РµРЅРёРµ health bar РґР»СЏ РІСЃРµС… РїРµСЂСЃРѕРЅР°Р¶РµР№
    void UpdateHealthBars()
    {
        foreach (var kvp in characterIcons)
        {
            Character character = kvp.Key;
            IconData iconData = kvp.Value;

            if (character != null && iconData.healthBarFill != null)
            {
                float healthPercent = character.GetHealthPercent();

                // РћР±РЅРѕРІР»СЏРµРј scale.x
                RectTransform healthBarRect = iconData.healthBarFill.GetComponent<RectTransform>();
                if (healthBarRect != null)
                {
                    Vector3 currentScale = healthBarRect.localScale;
                    healthBarRect.localScale = new Vector3(Mathf.Clamp01(healthPercent), currentScale.y, currentScale.z);
                }

                // РћР±РЅРѕРІР»СЏРµРј С†РІРµС‚ health bar
                if (healthPercent > 0.6f)
                {
                    iconData.healthBarFill.color = new Color(0.24913555f, 0.5849056f, 0, 1); // Р—РµР»РµРЅС‹Р№
                }
                else if (healthPercent > 0.3f)
                {
                    iconData.healthBarFill.color = Color.yellow;
                }
                else
                {
                    iconData.healthBarFill.color = Color.red;
                }
            }
        }
    }

    // Р”РћР‘РђР’Р›Р•РќРћ: РћР±СЂР°Р±РѕС‚С‡РёРє СЃРѕР±С‹С‚РёСЏ РїРѕР»СѓС‡РµРЅРёСЏ СѓСЂРѕРЅР°
    void OnCharacterDamaged(CharacterDamagedEvent evt)
    {

        if (evt.character != null && evt.character.IsPlayerCharacter())
        {

            // РџСЂРёРЅСѓРґРёС‚РµР»СЊРЅРѕ РѕР±РЅРѕРІР»СЏРµРј РёРєРѕРЅРєСѓ СЌС‚РѕРіРѕ РїРµСЂСЃРѕРЅР°Р¶Р°
            if (characterIcons.ContainsKey(evt.character))
            {
                IconData iconData = characterIcons[evt.character];
                if (iconData.healthBarFill != null)
                {
                    float healthPercent = evt.character.GetHealthPercent();

                    RectTransform healthBarRect = iconData.healthBarFill.GetComponent<RectTransform>();
                    if (healthBarRect != null)
                    {
                        Vector3 currentScale = healthBarRect.localScale;
                        healthBarRect.localScale = new Vector3(healthPercent, currentScale.y, currentScale.z);
                    }

                    // РћР±РЅРѕРІР»СЏРµРј С†РІРµС‚
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

                }
            }
        }
    }

    void OnDestroy()
    {
        if (selectionManager != null)
        {
            selectionManager.OnSelectionChanged -= OnSelectionChanged;
        }

        // Р”РћР‘РђР’Р›Р•РќРћ: РћС‚РїРёСЃРєР° РѕС‚ EventBus
        EventBus.Unsubscribe<CharacterDamagedEvent>(OnCharacterDamaged);
    }
}
