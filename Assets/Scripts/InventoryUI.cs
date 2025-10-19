using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI РґР»СЏ РѕС‚РѕР±СЂР°Р¶РµРЅРёСЏ Рё СѓРїСЂР°РІР»РµРЅРёСЏ РёРЅРІРµРЅС‚Р°СЂРµРј
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    public Canvas mainCanvas;
    public RectTransform inventoryPanel;
    public Button inventoryToggleButton;

    [Header("Inventory Display")]
    public RectTransform slotsContainer;
    public Text inventoryStatsText;
    public Text characterNameText;
    public TMP_Text characterNameTextTMP; // TextMeshPro РІРµСЂСЃРёСЏ

    [Header("Equipment Display")]
    public Dictionary<EquipmentSlot, InventorySlotUI> equipmentSlotUIs;

    [Header("Settings")]
    public bool showInventoryOnStart = false;
    public KeyCode toggleKey = KeyCode.I;
    public int slotsPerRow = 5;
    public Vector2 slotSize = new Vector2(60, 60);
    public Vector2 slotSpacing = new Vector2(5, 5);
    public int defaultInventorySlots = 24; // РљРѕР»РёС‡РµСЃС‚РІРѕ СЃР»РѕС‚РѕРІ РїРѕ СѓРјРѕР»С‡Р°РЅРёСЋ

    [Header("Prefabs")]
    public GameObject itemSlotPrefab; // РџСЂРµС„Р°Р± ItemSlot

    // Р’РЅСѓС‚СЂРµРЅРЅРёРµ РїРµСЂРµРјРµРЅРЅС‹Рµ
    private bool isInventoryVisible = false;
    private Inventory currentInventory;
    private List<InventorySlotUI> slotUIElements = new List<InventorySlotUI>();
    private SelectionManager selectionManager;
    private InventorySlotUI selectedSlotUI;
    private RectTransform contentPanel; // Content РїР°РЅРµР»СЊ РґР»СЏ СЃРїР°РІРЅР° СЃР»РѕС‚РѕРІ

    // РЎС‚Р°С‚РёС‡РµСЃРєРѕРµ СЃРІРѕР№СЃС‚РІРѕ РґР»СЏ РїСЂРѕРІРµСЂРєРё РѕС‚РєСЂС‹С‚РѕРіРѕ РёРЅРІРµРЅС‚Р°СЂСЏ
    public static bool IsAnyInventoryOpen { get; private set; } = false;

    // РџСЂРµС„Р°Р±С‹ РґР»СЏ СЃРѕР·РґР°РЅРёСЏ UI СЌР»РµРјРµРЅС‚РѕРІ
    private GameObject slotPrefab;

    void Awake()
    {
        equipmentSlotUIs = new Dictionary<EquipmentSlot, InventorySlotUI>();
        FindSelectionManager();
        CreateSlotPrefab();
        InitializeUI();

        // РЎРєСЂС‹РІР°РµРј РёРЅРІРµРЅС‚Р°СЂСЊ СЃСЂР°Р·Сѓ РїРѕСЃР»Рµ СЃРѕР·РґР°РЅРёСЏ
        if (!showInventoryOnStart && inventoryPanel != null)
        {
            inventoryPanel.gameObject.SetActive(false);
            isInventoryVisible = false;
        }
    }

    void Start()
    {
        if (selectionManager != null)
        {
            selectionManager.OnSelectionChanged += OnSelectionChanged;
        }
        else
        {

        }

        if (showInventoryOnStart)
        {
            ShowInventory();
        }
        else
        {
            HideInventory();
        }
    }

    void Update()
    {
        // РћР±СЂР°Р±РѕС‚РєР° РєР»РёРєР° РјС‹С€Рё РґР»СЏ СЃРЅСЏС‚РёСЏ РІС‹РґРµР»РµРЅРёСЏ СЃР»РѕС‚Р°
        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }
    }

    /// <summary>
    /// РџРѕРёСЃРє SelectionManager РІ СЃС†РµРЅРµ
    /// </summary>
    void FindSelectionManager()
    {
        selectionManager = FindObjectOfType<SelectionManager>();
    }

    /// <summary>
    /// РЎРѕР·РґР°РЅРёРµ РїСЂРµС„Р°Р±Р° РґР»СЏ СЃР»РѕС‚Р° РёРЅРІРµРЅС‚Р°СЂСЏ
    /// </summary>
    void CreateSlotPrefab()
    {
        GameObject prefab = new GameObject("InventorySlot");

        // РљРѕРјРїРѕРЅРµРЅС‚С‹ СЃР»РѕС‚Р°
        RectTransform rectTransform = prefab.AddComponent<RectTransform>();
        rectTransform.sizeDelta = slotSize;

        Image background = prefab.AddComponent<Image>();
        background.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        Button button = prefab.AddComponent<Button>();

        // РРєРѕРЅРєР° РїСЂРµРґРјРµС‚Р°
        GameObject iconGO = new GameObject("ItemIcon");
        iconGO.transform.SetParent(prefab.transform, false);

        RectTransform iconRect = iconGO.AddComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(5, 5);
        iconRect.offsetMax = new Vector2(-5, -5);

        Image iconImage = iconGO.AddComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.color = Color.white;

        // РўРµРєСЃС‚ РєРѕР»РёС‡РµСЃС‚РІР°
        GameObject quantityGO = new GameObject("QuantityText");
        quantityGO.transform.SetParent(prefab.transform, false);

        RectTransform quantityRect = quantityGO.AddComponent<RectTransform>();
        quantityRect.anchorMin = new Vector2(0.7f, 0);
        quantityRect.anchorMax = new Vector2(1, 0.3f);
        quantityRect.offsetMin = Vector2.zero;
        quantityRect.offsetMax = Vector2.zero;

        Text quantityText = quantityGO.AddComponent<Text>();
        quantityText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        quantityText.fontSize = 12;
        quantityText.color = Color.white;
        quantityText.alignment = TextAnchor.LowerRight;
        quantityText.text = "";

        // Р”РѕР±Р°РІР»СЏРµРј РєРѕРјРїРѕРЅРµРЅС‚ InventorySlotUI
        InventorySlotUI slotUI = prefab.AddComponent<InventorySlotUI>();
        slotUI.Initialize(background, iconImage, quantityText, button);

        // РћС‚РєР»СЋС‡Р°РµРј РїРѕ СѓРјРѕР»С‡Р°РЅРёСЋ
        prefab.SetActive(false);
        slotPrefab = prefab;
    }

    /// <summary>
    /// РРЅРёС†РёР°Р»РёР·Р°С†РёСЏ UI
    /// </summary>
    void InitializeUI()
    {
        // РС‰РµРј СЃСѓС‰РµСЃС‚РІСѓСЋС‰РёР№ Canvas_MainUI Рё Inventory РІРЅСѓС‚СЂРё РЅРµРіРѕ
        GameObject mainUICanvas = GameObject.Find("Canvas_MainUI");
        if (mainUICanvas != null)
        {
            mainCanvas = mainUICanvas.GetComponent<Canvas>();

            // РС‰РµРј СЃСѓС‰РµСЃС‚РІСѓСЋС‰РёР№ Inventory РѕР±СЉРµРєС‚ РїРѕ РїРѕР»РЅРѕРјСѓ РїСѓС‚Рё
            Transform windowsTransform = mainUICanvas.transform.Find("Windows");
            if (windowsTransform != null)
            {
                Transform inventoryTransform = windowsTransform.Find("Inventory");
                if (inventoryTransform != null)
                {
                    inventoryPanel = inventoryTransform.GetComponent<RectTransform>();

                    // РС‰РµРј Content РїР°РЅРµР»СЊ РґР»СЏ СЃРїР°РІРЅР° ItemSlot
                    Transform itemsAreaTransform = inventoryTransform.Find("ItemsArea");
                    if (itemsAreaTransform != null)
                    {
                        Transform viewportTransform = itemsAreaTransform.Find("Viewport");
                        if (viewportTransform != null)
                        {
                            Transform contentTransform = viewportTransform.Find("Content");
                            if (contentTransform != null)
                            {
                                contentPanel = contentTransform.GetComponent<RectTransform>();
                            }
                        }
                    }

                    // РС‰РµРј Character name Text Рё CloseButton РІРЅСѓС‚СЂРё Inventory
                    Transform headerTransform = inventoryTransform.Find("Header ");
                    if (headerTransform != null)
                    {
                        // РќР°С…РѕРґРёРј Character name
                        Transform characterNameTransform = headerTransform.Find("Character name");
                        if (characterNameTransform != null)
                        {
                            // РџСЂРѕР±СѓРµРј РЅР°Р№С‚Рё TextMeshPro РєРѕРјРїРѕРЅРµРЅС‚ (РїСЂРёРѕСЂРёС‚РµС‚)
                            characterNameTextTMP = characterNameTransform.GetComponent<TMP_Text>();
                            if (characterNameTextTMP == null)
                            {
                                // Р•СЃР»Рё РЅРµС‚ TMP, РїСЂРѕР±СѓРµРј РѕР±С‹С‡РЅС‹Р№ Text
                                characterNameText = characterNameTransform.GetComponent<Text>();
                            }
                        }

                        // РќР°С…РѕРґРёРј Рё РїСЂРёРІСЏР·С‹РІР°РµРј CloseButton
                        Transform closeButtonTransform = headerTransform.Find("CloseButton");
                        if (closeButtonTransform != null)
                        {
                            Button closeButton = closeButtonTransform.GetComponent<Button>();
                            if (closeButton != null)
                            {
                                // РћС‡РёС‰Р°РµРј СЃСѓС‰РµСЃС‚РІСѓСЋС‰РёРµ РѕР±СЂР°Р±РѕС‚С‡РёРєРё (РµСЃР»Рё РµСЃС‚СЊ) Рё РґРѕР±Р°РІР»СЏРµРј РЅР°С€
                                closeButton.onClick.RemoveAllListeners();
                                closeButton.onClick.AddListener(HideInventory);
                            }
                        }
                    }

                    // РС‰РµРј Рё РёРЅРёС†РёР°Р»РёР·РёСЂСѓРµРј EquipmentPanel
                    InitializeEquipmentPanel(inventoryTransform);

                    // РЎРєСЂС‹РІР°РµРј РїР°РЅРµР»СЊ РїРѕ СѓРјРѕР»С‡Р°РЅРёСЋ
                    if (!showInventoryOnStart)
                    {
                        inventoryPanel.gameObject.SetActive(false);
                    }
                }
                else
                {
                }
            }
            else
            {
            }
        }
        else
        {
        }

        // РРЅРёС†РёР°Р»РёР·РёСЂСѓРµРј СЃРёСЃС‚РµРјСѓ tooltips
        TooltipSystem.Instance.gameObject.transform.SetParent(transform, false);
    }

    /// <summary>
    /// РРЅРёС†РёР°Р»РёР·Р°С†РёСЏ EquipmentPanel Рё РїСЂРёРІСЏР·РєР° Рє СЃСѓС‰РµСЃС‚РІСѓСЋС‰РёРј СЃР»РѕС‚Р°Рј
    /// </summary>
    void InitializeEquipmentPanel(Transform inventoryTransform)
    {
        // РС‰РµРј СЃР»РѕС‚С‹ СЌРєРёРїРёСЂРѕРІРєРё РІ Canvas_MainUI (СЃ СЂРѕРґРёС‚РµР»СЏРјРё)
        GameObject canvasMainUI = GameObject.Find("Canvas_MainUI");
        if (canvasMainUI == null)
        {
            return;
        }

        // РС‰РµРј СЃР»РѕС‚С‹ РїРѕ РёС… СЂРµР°Р»СЊРЅС‹Рј РёРјРµРЅР°Рј Рё РїСЂРёРІСЏР·С‹РІР°РµРј Рє СЃРѕРѕС‚РІРµС‚СЃС‚РІСѓСЋС‰РµРјСѓ EquipmentSlot
        BindEquipmentSlotInHierarchy(canvasMainUI.transform, "EquipmentSlot_Helmet", EquipmentSlot.Head);
        BindEquipmentSlotInHierarchy(canvasMainUI.transform, "EquipmentSlot_Armor", EquipmentSlot.Chest);
        BindEquipmentSlotInHierarchy(canvasMainUI.transform, "EquipmentSlot_Weapon", EquipmentSlot.RightHand);
        BindEquipmentSlotInHierarchy(canvasMainUI.transform, "EquipmentSlot_Pants", EquipmentSlot.Legs);
        BindEquipmentSlotInHierarchy(canvasMainUI.transform, "EquipmentSlot_Boots", EquipmentSlot.Feet);
    }

    /// <summary>
    /// РџСЂРёРІСЏР·Р°С‚СЊ СЃСѓС‰РµСЃС‚РІСѓСЋС‰РёР№ СЃР»РѕС‚ СЌРєРёРїРёСЂРѕРІРєРё Рє СЃРёСЃС‚РµРјРµ (СЂРµРєСѓСЂСЃРёРІРЅС‹Р№ РїРѕРёСЃРє)
    /// </summary>
    void BindEquipmentSlotInHierarchy(Transform parent, string slotName, EquipmentSlot equipmentSlot)
    {
        // Р РµРєСѓСЂСЃРёРІРЅС‹Р№ РїРѕРёСЃРє СЃР»РѕС‚Р° РІ РёРµСЂР°СЂС…РёРё
        Transform slotTransform = FindTransformRecursive(parent, slotName);

        if (slotTransform == null)
        {
            return;
        }

        GameObject slotGO = slotTransform.gameObject;

        // РџРѕР»СѓС‡Р°РµРј РёР»Рё РґРѕР±Р°РІР»СЏРµРј InventorySlotUI РєРѕРјРїРѕРЅРµРЅС‚
        InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
        if (slotUI == null)
        {
            // РќР°С…РѕРґРёРј РєРѕРјРїРѕРЅРµРЅС‚С‹ РґР»СЏ РёРЅРёС†РёР°Р»РёР·Р°С†РёРё InventorySlotUI
            Image background = slotGO.GetComponent<Image>();

            // РС‰РµРј Icon РІРЅСѓС‚СЂРё СЃР»РѕС‚Р°
            Transform iconTransform = slotTransform.Find("Icon");
            Image iconImage = iconTransform != null ? iconTransform.GetComponent<Image>() : null;

            // Р•СЃР»Рё Icon РЅРµ РЅР°Р№РґРµРЅ, Р»РѕРіРёСЂСѓРµРј РІСЃРµ РґРѕС‡РµСЂРЅРёРµ РѕР±СЉРµРєС‚С‹
            if (iconTransform == null)
            {
                foreach (Transform child in slotTransform)
                {
                }
            }

            // Р”Р»СЏ Text РёСЃРїРѕР»СЊР·СѓРµРј РїСѓСЃС‚СѓСЋ СЃСЃС‹Р»РєСѓ (РјРѕР¶РЅРѕ РґРѕР±Р°РІРёС‚СЊ QuantityText РµСЃР»Рё РЅСѓР¶РЅРѕ)
            Text quantityText = null;

            // РџРѕР»СѓС‡Р°РµРј РёР»Рё РґРѕР±Р°РІР»СЏРµРј Button РєРѕРјРїРѕРЅРµРЅС‚
            Button button = slotGO.GetComponent<Button>();
            if (button == null)
            {
                button = slotGO.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
            }

            // Р”РѕР±Р°РІР»СЏРµРј Рё РёРЅРёС†РёР°Р»РёР·РёСЂСѓРµРј InventorySlotUI
            slotUI = slotGO.AddComponent<InventorySlotUI>();
            slotUI.Initialize(background, iconImage, quantityText, button);
        }

        // РќР°СЃС‚СЂР°РёРІР°РµРј СЃР»РѕС‚ РґР»СЏ СЌРєРёРїРёСЂРѕРІРєРё
        slotUI.SetEquipmentSlot(equipmentSlot);
        slotUI.SetSlotIndex((int)equipmentSlot);

        // РџРѕРґРїРёСЃС‹РІР°РµРјСЃСЏ РЅР° СЃРѕР±С‹С‚РёСЏ
        slotUI.OnSlotClicked += OnEquipmentSlotClicked;
        slotUI.OnSlotRightClicked += OnEquipmentSlotRightClicked;
        slotUI.OnSlotDoubleClicked += OnEquipmentSlotDoubleClicked;
        slotUI.OnSlotDragAndDrop += OnSlotDragAndDrop;

        // РЎРѕС…СЂР°РЅСЏРµРј РІ СЃР»РѕРІР°СЂСЊ
        equipmentSlotUIs[equipmentSlot] = slotUI;
    }

    /// <summary>
    /// Р РµРєСѓСЂСЃРёРІРЅС‹Р№ РїРѕРёСЃРє Transform РїРѕ РёРјРµРЅРё
    /// </summary>
    Transform FindTransformRecursive(Transform parent, string name)
    {
        // РџСЂРѕРІРµСЂСЏРµРј С‚РµРєСѓС‰РёР№ РѕР±СЉРµРєС‚
        if (parent.name == name)
            return parent;

        // РџСЂРѕРІРµСЂСЏРµРј РІСЃРµС… РґРµС‚РµР№
        foreach (Transform child in parent)
        {
            Transform result = FindTransformRecursive(child, name);
            if (result != null)
                return result;
        }

        return null;
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РїРѕР»РЅС‹Р№ РїСѓС‚СЊ GameObject РІ РёРµСЂР°СЂС…РёРё
    /// </summary>
    string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚С‡РёРє РёР·РјРµРЅРµРЅРёСЏ РІС‹РґРµР»РµРЅРёСЏ
    /// </summary>
    void OnSelectionChanged(List<GameObject> selectedObjects)
    {
        if (selectedObjects.Count == 1)
        {
            Character character = selectedObjects[0].GetComponent<Character>();
            if (character != null && character.IsPlayerCharacter())
            {
                SetCurrentInventory(character.GetInventory(), character);
                return;
            }
        }

        // Р•СЃР»Рё РЅРµ РІС‹РґРµР»РµРЅ СЃРѕСЋР·РЅС‹Р№ РїРµСЂСЃРѕРЅР°Р¶, СЃРєСЂС‹РІР°РµРј РёРЅРІРµРЅС‚Р°СЂСЊ
        SetCurrentInventory(null, null);
    }

    /// <summary>
    /// РЈСЃС‚Р°РЅРѕРІРёС‚СЊ С‚РµРєСѓС‰РёР№ РёРЅРІРµРЅС‚Р°СЂСЊ РґР»СЏ РѕС‚РѕР±СЂР°Р¶РµРЅРёСЏ
    /// </summary>
    public void SetCurrentInventory(Inventory inventory, Character character = null)
    {
        // РћС‚РїРёСЃС‹РІР°РµРјСЃСЏ РѕС‚ СЃРѕР±С‹С‚РёР№ РїСЂРµРґС‹РґСѓС‰РµРіРѕ РёРЅРІРµРЅС‚Р°СЂСЏ
        if (currentInventory != null)
        {
            currentInventory.OnInventoryChanged -= UpdateInventoryDisplay;
            currentInventory.OnEquipmentChanged -= UpdateEquipmentDisplay;
        }

        currentInventory = inventory;

        // РћР±РЅРѕРІР»СЏРµРј РёРјСЏ РїРµСЂСЃРѕРЅР°Р¶Р° (РёСЃРїРѕР»СЊР·СѓРµРј TMP_Text РµСЃР»Рё РґРѕСЃС‚СѓРїРµРЅ, РёРЅР°С‡Рµ РѕР±С‹С‡РЅС‹Р№ Text)
        if (characterNameTextTMP != null)
        {
            characterNameTextTMP.text = character != null ? character.GetFullName() : "РџРµСЂСЃРѕРЅР°Р¶";
        }
        else if (characterNameText != null)
        {
            characterNameText.text = character != null ? character.GetFullName() : "РџРµСЂСЃРѕРЅР°Р¶";
        }

        // РџРѕРґРїРёСЃС‹РІР°РµРјСЃСЏ РЅР° СЃРѕР±С‹С‚РёСЏ РЅРѕРІРѕРіРѕ РёРЅРІРµРЅС‚Р°СЂСЏ
        if (currentInventory != null)
        {
            currentInventory.OnInventoryChanged += UpdateInventoryDisplay;
            currentInventory.OnEquipmentChanged += UpdateEquipmentDisplay;
            UpdateInventoryDisplay();
            UpdateEquipmentDisplay();
        }
        else
        {
            ClearInventoryDisplay();
        }
    }

    /// <summary>
    /// РЎРѕР·РґР°С‚СЊ СЃР»РѕС‚С‹ РґР»СЏ РёРЅРІРµРЅС‚Р°СЂСЏ
    /// </summary>
    void CreateInventorySlots()
    {
        // РСЃРїРѕР»СЊР·СѓРµРј Content РїР°РЅРµР»СЊ РµСЃР»Рё РѕРЅР° РЅР°Р№РґРµРЅР°, РёРЅР°С‡Рµ fallback РЅР° slotsContainer
        RectTransform targetContainer = contentPanel != null ? contentPanel : slotsContainer;

        if (targetContainer == null)
        {
            return;
        }

        // РЈРґР°Р»СЏРµРј СЃСѓС‰РµСЃС‚РІСѓСЋС‰РёРµ СЃР»РѕС‚С‹
        ClearSlotUIElements();

        // Р’СЃРµРіРґР° СЃРѕР·РґР°РµРј 24 СЃР»РѕС‚Р° РІ Content (РЅРµР·Р°РІРёСЃРёРјРѕ РѕС‚ maxSlots РІ Inventory)
        int slotCount = defaultInventorySlots;

        // Р—Р°РіСЂСѓР¶Р°РµРј РїСЂРµС„Р°Р± ItemSlot РµСЃР»Рё РЅРµ РЅР°Р·РЅР°С‡РµРЅ
        GameObject prefabToUse = itemSlotPrefab;
        if (prefabToUse == null)
        {
            prefabToUse = Resources.Load<GameObject>("Prefabs/UI/ItemSlot");
            if (prefabToUse == null)
            {
                // Fallback РЅР° РїСЂРѕРіСЂР°РјРјРЅРѕ СЃРѕР·РґР°РЅРЅС‹Р№ РїСЂРµС„Р°Р±
                prefabToUse = slotPrefab;
            }
        }

        if (prefabToUse == null)
        {
            return;
        }

        // РЎРѕР·РґР°РµРј РЅРѕРІС‹Рµ СЃР»РѕС‚С‹
        for (int i = 0; i < slotCount; i++)
        {
            GameObject slotGO = Instantiate(prefabToUse, targetContainer);
            slotGO.SetActive(true);
            slotGO.name = $"ItemSlot_{i}";

            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            if (slotUI != null)
            {
                // РџСЂРѕРІРµСЂСЏРµРј, РЅСѓР¶РЅР° Р»Рё РёРЅРёС†РёР°Р»РёР·Р°С†РёСЏ (РµСЃР»Рё РїСЂРµС„Р°Р± РЅРµ Р±С‹Р» РёРЅРёС†РёР°Р»РёР·РёСЂРѕРІР°РЅ)
                if (slotUI.backgroundImage == null || slotUI.itemIcon == null)
                {
                    // РС‰РµРј РєРѕРјРїРѕРЅРµРЅС‚С‹ РґР»СЏ РёРЅРёС†РёР°Р»РёР·Р°С†РёРё
                    Image background = slotGO.GetComponent<Image>();
                    Transform iconTransform = slotGO.transform.Find("Icon");
                    if (iconTransform == null)
                        iconTransform = slotGO.transform.Find("ItemIcon");

                    Image iconImage = iconTransform != null ? iconTransform.GetComponent<Image>() : null;

                    Transform quantityTransform = slotGO.transform.Find("QuantityText");
                    Text quantityText = quantityTransform != null ? quantityTransform.GetComponent<Text>() : null;

                    Button button = slotGO.GetComponent<Button>();

                    // РРЅРёС†РёР°Р»РёР·РёСЂСѓРµРј СЃР»РѕС‚
                    slotUI.Initialize(background, iconImage, quantityText, button);
                }

                // РЈСЃС‚Р°РЅР°РІР»РёРІР°РµРј Canvas РґР»СЏ drag & drop РѕРїРµСЂР°С†РёР№
                slotUI.SetDragCanvas(mainCanvas);

                slotUI.SetSlotIndex(i);
                slotUI.OnSlotClicked += OnSlotClicked;
                slotUI.OnSlotRightClicked += OnSlotRightClicked;
                slotUI.OnSlotDoubleClicked += OnSlotDoubleClicked;
                slotUI.OnSlotDragAndDrop += OnSlotDragAndDrop;
                slotUIElements.Add(slotUI);
            }
        }
    }

    /// <summary>
    /// РћС‡РёСЃС‚РёС‚СЊ UI СЌР»РµРјРµРЅС‚С‹ СЃР»РѕС‚РѕРІ
    /// </summary>
    void ClearSlotUIElements()
    {
        foreach (InventorySlotUI slotUI in slotUIElements)
        {
            if (slotUI != null)
            {
                slotUI.OnSlotClicked -= OnSlotClicked;
                slotUI.OnSlotRightClicked -= OnSlotRightClicked;
                slotUI.OnSlotDoubleClicked -= OnSlotDoubleClicked;
                slotUI.OnSlotDragAndDrop -= OnSlotDragAndDrop;
                DestroyImmediate(slotUI.gameObject);
            }
        }
        slotUIElements.Clear();

        // РќР• РѕС‡РёС‰Р°РµРј СЃР»РѕС‚С‹ СЌРєРёРїРёСЂРѕРІРєРё Р·РґРµСЃСЊ, С‚Р°Рє РєР°Рє РѕРЅРё СЃРѕР·РґР°СЋС‚СЃСЏ С‚РѕР»СЊРєРѕ РїСЂРё РёРЅРёС†РёР°Р»РёР·Р°С†РёРё
        // Рё РґРѕР»Р¶РЅС‹ СЃРѕС…СЂР°РЅСЏС‚СЊСЃСЏ РјРµР¶РґСѓ РѕР±РЅРѕРІР»РµРЅРёСЏРјРё РёРЅРІРµРЅС‚Р°СЂСЏ

    }

    /// <summary>
    /// РћР±РЅРѕРІРёС‚СЊ РѕС‚РѕР±СЂР°Р¶РµРЅРёРµ РёРЅРІРµРЅС‚Р°СЂСЏ
    /// </summary>
    void UpdateInventoryDisplay()
    {
        if (currentInventory == null) return;

        // РЎРѕР·РґР°РµРј СЃР»РѕС‚С‹ РµСЃР»Рё РёС… РµС‰Рµ РЅРµС‚
        if (slotUIElements.Count == 0)
        {
            CreateInventorySlots();
        }

        // РћР±РЅРѕРІР»СЏРµРј РєР°Р¶РґС‹Р№ СЃР»РѕС‚
        var allSlots = currentInventory.GetAllSlots();
        for (int i = 0; i < slotUIElements.Count && i < allSlots.Count; i++)
        {
            InventorySlotUI slotUI = slotUIElements[i];
            InventorySlot slot = allSlots[i];

            if (slotUI != null)
            {
                slotUI.UpdateSlot(slot);
            }
        }

        // РћР±РЅРѕРІР»СЏРµРј СЃС‚Р°С‚РёСЃС‚РёРєСѓ
        UpdateInventoryStats();

        // РћР±РЅРѕРІР»СЏРµРј РІРёР·СѓР°Р»СЊРЅРѕРµ СЃРѕСЃС‚РѕСЏРЅРёРµ СЃР»РѕС‚РѕРІ СЌРєРёРїРёСЂРѕРІРєРё

        foreach (var kvp in equipmentSlotUIs)
        {
            InventorySlotUI slotUI = kvp.Value;
            if (slotUI != null)
            {

                if (slotUI.backgroundImage != null)
                {

                }
            }
        }
        UpdateEquipmentSlotVisuals();

        foreach (var kvp in equipmentSlotUIs)
        {
            InventorySlotUI slotUI = kvp.Value;
            if (slotUI != null)
            {

                if (slotUI.backgroundImage != null)
                {

                }
            }
        }
    }

    /// <summary>
    /// РћР±РЅРѕРІРёС‚СЊ РѕС‚РѕР±СЂР°Р¶РµРЅРёРµ СЌРєРёРїРёСЂРѕРІРєРё
    /// </summary>
    void UpdateEquipmentDisplay()
    {
        if (currentInventory == null) return;

        var equipmentSlots = currentInventory.GetAllEquipmentSlots();

        foreach (var kvp in equipmentSlotUIs)
        {
            EquipmentSlot slotType = kvp.Key;
            InventorySlotUI slotUI = kvp.Value;

            if (equipmentSlots.ContainsKey(slotType))
            {
                InventorySlot slot = equipmentSlots[slotType];

                slotUI.UpdateSlot(slot);

                // РџСЂРёРЅСѓРґРёС‚РµР»СЊРЅРѕ РґРµР»Р°РµРј СЃР»РѕС‚ РІРёРґРёРјС‹Рј РїРѕСЃР»Рµ РѕР±РЅРѕРІР»РµРЅРёСЏ
                if (slotUI.backgroundImage != null)
                {
                    slotUI.backgroundImage.enabled = true;
                    slotUI.backgroundImage.color = new Color(0.8f, 0.8f, 0.9f, 1.0f);
                }

                // РўР°РєР¶Рµ РѕР±РµСЃРїРµС‡РёРІР°РµРј РІРёРґРёРјРѕСЃС‚СЊ GameObject
                slotUI.gameObject.SetActive(true);

            }
        }
    }

    /// <summary>
    /// РћР±РЅРѕРІРёС‚СЊ СЃС‚Р°С‚РёСЃС‚РёРєСѓ РёРЅРІРµРЅС‚Р°СЂСЏ
    /// </summary>
    void UpdateInventoryStats()
    {
        if (currentInventory == null || inventoryStatsText == null) return;

        string stats = $"РЎР»РѕС‚С‹: {currentInventory.GetUsedSlots()}/{currentInventory.maxSlots}\n";
        stats += $"Р’РµСЃ: {currentInventory.GetCurrentWeight():F1}/{currentInventory.maxWeight:F1}\n";
        stats += $"Р—Р°РїРѕР»РЅРµРЅРЅРѕСЃС‚СЊ: {(currentInventory.GetWeightPercent() * 100):F0}%";

        if (currentInventory.autoPickupEnabled)
        {
            stats += "\n\nРђРІС‚РѕРїРѕРґР±РѕСЂ: Р’РєР»СЋС‡РµРЅ";
        }

        inventoryStatsText.text = stats;
    }

    /// <summary>
    /// РћС‡РёСЃС‚РёС‚СЊ РѕС‚РѕР±СЂР°Р¶РµРЅРёРµ РёРЅРІРµРЅС‚Р°СЂСЏ
    /// </summary>
    void ClearInventoryDisplay()
    {
        ClearSlotUIElements();

        if (inventoryStatsText != null)
        {
            inventoryStatsText.text = "РќРµС‚ РґРѕСЃС‚СѓРїРЅРѕРіРѕ РёРЅРІРµРЅС‚Р°СЂСЏ";
        }

    }

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚С‡РёРє РєР»РёРєР° РїРѕ СЃР»РѕС‚Сѓ
    /// </summary>
    void OnSlotClicked(int slotIndex)
    {
        if (currentInventory == null) return;

        InventorySlot slot = currentInventory.GetSlot(slotIndex);
        if (slot != null && !slot.IsEmpty())
        {
            // Р’С‹РґРµР»СЏРµРј СЃР»РѕС‚
            SelectSlot(slotIndex);
        }
        else
        {
            // РЎРЅРёРјР°РµРј РІС‹РґРµР»РµРЅРёРµ
            ClearSlotSelection();
        }
    }

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚С‡РёРє РїСЂР°РІРѕРіРѕ РєР»РёРєР° РїРѕ СЃР»РѕС‚Сѓ (РІС‹Р±СЂРѕСЃ РїСЂРµРґРјРµС‚Р°)
    /// </summary>
    void OnSlotRightClicked(int slotIndex)
    {
        if (currentInventory == null) return;

        InventorySlot slot = currentInventory.GetSlot(slotIndex);
        if (slot != null && !slot.IsEmpty())
        {
            // Р’С‹Р±СЂР°СЃС‹РІР°РµРј РѕРґРёРЅ РїСЂРµРґРјРµС‚
            currentInventory.DropItem(slotIndex, 1);
        }
    }

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚С‡РёРє РєР»РёРєР° РїРѕ СЃР»РѕС‚Сѓ СЌРєРёРїРёСЂРѕРІРєРё
    /// </summary>
    void OnEquipmentSlotClicked(int slotIndex)
    {
        // Р”Р»СЏ СЃР»РѕС‚РѕРІ СЌРєРёРїРёСЂРѕРІРєРё РёСЃРїРѕР»СЊР·СѓРµРј slotIndex РєР°Рє С‚РёРї EquipmentSlot
        EquipmentSlot equipSlot = (EquipmentSlot)slotIndex;

        if (currentInventory == null) return;

        // РРЅС„РѕСЂРјР°С†РёСЏ Рѕ РїСЂРµРґРјРµС‚Рµ РѕС‚РѕР±СЂР°Р¶Р°РµС‚СЃСЏ РІ tooltip РїСЂРё РЅР°РІРµРґРµРЅРёРё РєСѓСЂСЃРѕСЂР°
    }

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚С‡РёРє РїСЂР°РІРѕРіРѕ РєР»РёРєР° РїРѕ СЃР»РѕС‚Сѓ СЌРєРёРїРёСЂРѕРІРєРё (СЃРЅСЏС‚РёРµ СЌРєРёРїРёСЂРѕРІРєРё)
    /// </summary>
    void OnEquipmentSlotRightClicked(int slotIndex)
    {
        EquipmentSlot equipSlot = (EquipmentSlot)slotIndex;

        if (currentInventory == null) return;

        // РЎРЅРёРјР°РµРј СЌРєРёРїРёСЂРѕРІРєСѓ
        currentInventory.UnequipItem(equipSlot);
    }

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚С‡РёРє РґРІРѕР№РЅРѕРіРѕ РєР»РёРєР° РїРѕ СЃР»РѕС‚Сѓ (СЌРєРёРїРёСЂРѕРІРєР° РїСЂРµРґРјРµС‚Р°)
    /// </summary>
    void OnSlotDoubleClicked(int slotIndex)
    {
        if (currentInventory == null) return;

        InventorySlot slot = currentInventory.GetSlot(slotIndex);
        if (slot != null && !slot.IsEmpty())
        {
            ItemData item = slot.itemData;



            // РџСЂРѕРІРµСЂСЏРµРј, РјРѕР¶РЅРѕ Р»Рё СЌРєРёРїРёСЂРѕРІР°С‚СЊ РїСЂРµРґРјРµС‚
            if (item.CanBeEquipped() && item.equipmentSlot != EquipmentSlot.None)
            {
                // Р”Р»СЏ РѕСЂСѓР¶РёСЏ РїСЂРѕРІРµСЂСЏРµРј РґРѕСЃС‚СѓРїРЅС‹Рµ СЃР»РѕС‚С‹ СЂСѓРє
                if (item.itemType == ItemType.Weapon)
                {
                    // РџР РћР’Р•Р РљРђ: СЃРЅР°С‡Р°Р»Р° РїСЂРѕРІРµСЂСЏРµРј Р’РЎР• СЃР»РѕС‚С‹ СЂСѓРє РЅР° РЅР°Р»РёС‡РёРµ С‚Р°РєРѕРіРѕ Р¶Рµ РїСЂРµРґРјРµС‚Р°
                    ItemData rightHandItem = currentInventory.GetEquippedItem(EquipmentSlot.RightHand);
                    ItemData leftHandItem = currentInventory.GetEquippedItem(EquipmentSlot.LeftHand);

                    if ((rightHandItem != null && rightHandItem.itemName == item.itemName) ||
                        (leftHandItem != null && leftHandItem.itemName == item.itemName))
                    {
                        return;
                    }

                    EquipmentSlot targetSlot = FindAvailableWeaponSlot();
                    if (targetSlot != EquipmentSlot.None)
                    {
                        // Р’СЂРµРјРµРЅРЅРѕ РјРµРЅСЏРµРј СЃР»РѕС‚ РїСЂРµРґРјРµС‚Р° РґР»СЏ СЌРєРёРїРёСЂРѕРІРєРё
                        EquipmentSlot originalSlot = item.equipmentSlot;
                        item.equipmentSlot = targetSlot;

                        if (currentInventory.EquipItem(item))
                        {
                            // Р’РђР–РќРћ: СѓРґР°Р»СЏРµРј РїСЂРµРґРјРµС‚ РёР· РєРѕРЅРєСЂРµС‚РЅРѕРіРѕ СЃР»РѕС‚Р°, Р° РЅРµ РїРµСЂРІС‹Р№ РЅР°Р№РґРµРЅРЅС‹Р№!
                            currentInventory.RemoveItemFromSlot(slotIndex, 1);

                            // РћР±РЅРѕРІР»СЏРµРј РІРёР·СѓР°Р»С‹ Р·Р°Р±Р»РѕРєРёСЂРѕРІР°РЅРЅС‹С… СЃР»РѕС‚РѕРІ
                            UpdateEquipmentSlotVisuals();
                            // РЎРЅРёРјР°РµРј РІС‹РґРµР»РµРЅРёРµ СЃ РїСѓСЃС‚РѕРіРѕ СЃР»РѕС‚Р°
                            ClearSlotSelection();
                        }
                        else
                        {
                            // Р’РѕСЃСЃС‚Р°РЅР°РІР»РёРІР°РµРј РѕСЂРёРіРёРЅР°Р»СЊРЅС‹Р№ СЃР»РѕС‚ РїСЂРё РЅРµСѓРґР°С‡Рµ
                            item.equipmentSlot = originalSlot;

                        }
                    }
                    else
                    {
                        // РћР±Р° СЃР»РѕС‚Р° СЂСѓРє Р·Р°РЅСЏС‚С‹
                    }
                }
                else
                {
                    // Р”Р»СЏ Р±СЂРѕРЅРё РїСЂРѕРІРµСЂСЏРµРј СЃРѕРѕС‚РІРµС‚СЃС‚РІСѓСЋС‰РёР№ СЃР»РѕС‚
                    if (currentInventory.IsEquipmentSlotBlocked(item.equipmentSlot))
                    {

                        return;
                    }

                    // РџР РћР’Р•Р РљРђ: РЅРµ СЌРєРёРїРёСЂРѕРІР°РЅ Р»Рё СѓР¶Рµ С‚Р°РєРѕР№ Р¶Рµ РїСЂРµРґРјРµС‚ (РїРѕ РёРјРµРЅРё) РІ С†РµР»РµРІРѕРј СЃР»РѕС‚Рµ
                    ItemData equippedItem = currentInventory.GetEquippedItem(item.equipmentSlot);
                    if (equippedItem != null && equippedItem.itemName == item.itemName)
                    {
                        return;
                    }

                    if (currentInventory.EquipItem(item))
                    {
                        // Р’РђР–РќРћ: СѓРґР°Р»СЏРµРј РїСЂРµРґРјРµС‚ РёР· РєРѕРЅРєСЂРµС‚РЅРѕРіРѕ СЃР»РѕС‚Р°, Р° РЅРµ РїРµСЂРІС‹Р№ РЅР°Р№РґРµРЅРЅС‹Р№!
                        currentInventory.RemoveItemFromSlot(slotIndex, 1);

                        // РЎРЅРёРјР°РµРј РІС‹РґРµР»РµРЅРёРµ СЃ РїСѓСЃС‚РѕРіРѕ СЃР»РѕС‚Р°
                        ClearSlotSelection();
                    }
                    else
                    {

                    }
                }
            }
            else
            {

            }
        }
    }

    /// <summary>
    /// РќР°Р№С‚Рё РґРѕСЃС‚СѓРїРЅС‹Р№ СЃР»РѕС‚ РґР»СЏ РѕСЂСѓР¶РёСЏ
    /// </summary>
    EquipmentSlot FindAvailableWeaponSlot()
    {
        // РџСЂРѕРІРµСЂСЏРµРј РїСЂР°РІСѓСЋ СЂСѓРєСѓ СЃРЅР°С‡Р°Р»Р° (РїСЂРёРѕСЂРёС‚РµС‚)
        if (!currentInventory.IsEquipped(EquipmentSlot.RightHand))
        {
            return EquipmentSlot.RightHand;
        }

        // РџРѕС‚РѕРј Р»РµРІСѓСЋ СЂСѓРєСѓ
        if (!currentInventory.IsEquipped(EquipmentSlot.LeftHand))
        {
            return EquipmentSlot.LeftHand;
        }

        return EquipmentSlot.None; // РќРµС‚ РґРѕСЃС‚СѓРїРЅС‹С… СЃР»РѕС‚РѕРІ
    }

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚С‡РёРє РґРІРѕР№РЅРѕРіРѕ РєР»РёРєР° РїРѕ СЃР»РѕС‚Сѓ СЌРєРёРїРёСЂРѕРІРєРё (СЃРЅСЏС‚РёРµ РїСЂРµРґРјРµС‚Р°)
    /// </summary>
    void OnEquipmentSlotDoubleClicked(int slotIndex)
    {
        if (currentInventory == null) return;

        // РћРїСЂРµРґРµР»СЏРµРј СЃР»РѕС‚ СЌРєРёРїРёСЂРѕРІРєРё РїРѕ РёРЅРґРµРєСЃСѓ
        EquipmentSlot equipmentSlot = (EquipmentSlot)slotIndex;



        // РџСЂРѕРІРµСЂСЏРµРј, РµСЃС‚СЊ Р»Рё РїСЂРµРґРјРµС‚ РІ СЌС‚РѕРј СЃР»РѕС‚Рµ
        if (currentInventory.IsEquipped(equipmentSlot))
        {
            // РџРѕР»СѓС‡Р°РµРј СЌРєРёРїРёСЂРѕРІР°РЅРЅС‹Р№ РїСЂРµРґРјРµС‚ РґР»СЏ Р»РѕРіРёСЂРѕРІР°РЅРёСЏ
            ItemData equippedItem = currentInventory.GetEquippedItem(equipmentSlot);
            if (equippedItem != null)
            {
                // РџСЂРѕРІРµСЂСЏРµРј, РµСЃС‚СЊ Р»Рё РјРµСЃС‚Рѕ РІ РёРЅРІРµРЅС‚Р°СЂРµ
                int freeSlotIndex = FindNearestFreeInventorySlot();
                if (freeSlotIndex != -1)
                {
                    // РЎРЅРёРјР°РµРј РїСЂРµРґРјРµС‚ СЃ СЌРєРёРїРёСЂРѕРІРєРё (UnequipItem СѓР¶Рµ РґРѕР±Р°РІР»СЏРµС‚ РїСЂРµРґРјРµС‚ РІ РёРЅРІРµРЅС‚Р°СЂСЊ)
                    if (currentInventory.UnequipItem(equipmentSlot))
                    {

                        // РћР±РЅРѕРІР»СЏРµРј РІРёР·СѓР°Р»С‹ Р·Р°Р±Р»РѕРєРёСЂРѕРІР°РЅРЅС‹С… СЃР»РѕС‚РѕРІ
                        UpdateEquipmentSlotVisuals();
                        // РЎРЅРёРјР°РµРј РІС‹РґРµР»РµРЅРёРµ
                        ClearSlotSelection();
                    }
                    else
                    {

                    }
                }
                else
                {

                }
            }
            else
            {

            }
        }
        else
        {

        }
    }

    /// <summary>
    /// РќР°Р№С‚Рё Р±Р»РёР¶Р°Р№С€РёР№ СЃРІРѕР±РѕРґРЅС‹Р№ СЃР»РѕС‚ РІ РѕСЃРЅРѕРІРЅРѕРј РёРЅРІРµРЅС‚Р°СЂРµ
    /// </summary>
    int FindNearestFreeInventorySlot()
    {
        if (currentInventory == null) return -1;

        var allSlots = currentInventory.GetAllSlots();
        for (int i = 0; i < allSlots.Count; i++)
        {
            if (allSlots[i].IsEmpty())
            {
                return i;
            }
        }

        return -1; // РќРµС‚ СЃРІРѕР±РѕРґРЅС‹С… СЃР»РѕС‚РѕРІ
    }

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚С‡РёРє drag and drop РјРµР¶РґСѓ СЃР»РѕС‚Р°РјРё
    /// </summary>
    void OnSlotDragAndDrop(int fromDragDropId, int toDragDropId)
    {
        if (currentInventory == null) return;



        // РћРїСЂРµРґРµР»СЏРµРј С‚РёРїС‹ СЃР»РѕС‚РѕРІ РїРѕ ID
        bool isFromEquipment = fromDragDropId >= 1000;
        bool isToEquipment = toDragDropId >= 1000;

        if (isFromEquipment && isToEquipment)
        {
            // РџРµСЂРµРјРµС‰РµРЅРёРµ РјРµР¶РґСѓ СЃР»РѕС‚Р°РјРё СЌРєРёРїРёСЂРѕРІРєРё
            HandleEquipmentToEquipmentDrag(fromDragDropId - 1000, toDragDropId - 1000);
        }
        else if (isFromEquipment && !isToEquipment)
        {
            // РР· СЃР»РѕС‚Р° СЌРєРёРїРёСЂРѕРІРєРё РІ РѕР±С‹С‡РЅС‹Р№ РёРЅРІРµРЅС‚Р°СЂСЊ
            HandleEquipmentToInventoryDrag(fromDragDropId - 1000, toDragDropId);
        }
        else if (!isFromEquipment && isToEquipment)
        {
            // РР· РѕР±С‹С‡РЅРѕРіРѕ РёРЅРІРµРЅС‚Р°СЂСЏ РІ СЃР»РѕС‚ СЌРєРёРїРёСЂРѕРІРєРё
            HandleInventoryToEquipmentDrag(fromDragDropId, toDragDropId - 1000);
        }
        else
        {
            // РћР±С‹С‡РЅРѕРµ РїРµСЂРµРјРµС‰РµРЅРёРµ РјРµР¶РґСѓ СЃР»РѕС‚Р°РјРё РёРЅРІРµРЅС‚Р°СЂСЏ
            HandleInventoryToInventoryDrag(fromDragDropId, toDragDropId);
        }
    }

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚РєР° РїРµСЂРµС‚Р°СЃРєРёРІР°РЅРёСЏ РјРµР¶РґСѓ СЃР»РѕС‚Р°РјРё РёРЅРІРµРЅС‚Р°СЂСЏ
    /// </summary>
    void HandleInventoryToInventoryDrag(int fromSlot, int toSlot)
    {

        bool success = currentInventory.MoveItem(fromSlot, toSlot);

        if (success)
        {

        }
        else
        {

        }
    }

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚РєР° РїРµСЂРµС‚Р°СЃРєРёРІР°РЅРёСЏ РёР· РёРЅРІРµРЅС‚Р°СЂСЏ РІ СЌРєРёРїРёСЂРѕРІРєСѓ
    /// </summary>
    void HandleInventoryToEquipmentDrag(int fromSlot, int equipSlotId)
    {
        EquipmentSlot equipSlot = (EquipmentSlot)equipSlotId;

        InventorySlot fromInventorySlot = currentInventory.GetSlot(fromSlot);
        if (fromInventorySlot != null && !fromInventorySlot.IsEmpty())
        {
            ItemData item = fromInventorySlot.itemData;

            // РџСЂРѕРІРµСЂСЏРµРј СЃРѕРІРјРµСЃС‚РёРјРѕСЃС‚СЊ
            bool canEquipToSlot = false;

            if (item.CanBeEquipped() && item.equipmentSlot != EquipmentSlot.None)
            {
                if (item.itemType == ItemType.Weapon)
                {
                    // Weapons can be equipped in either hand slot
                    canEquipToSlot = (equipSlot == EquipmentSlot.LeftHand || equipSlot == EquipmentSlot.RightHand);
                }
                else
                {
                    // Other items must match their specific equipment slot
                    canEquipToSlot = (item.equipmentSlot == equipSlot);
                }
            }

            if (canEquipToSlot)
            {
                // Check if the slot is blocked (e.g., second hand when weapon is already equipped)
                if (currentInventory.IsEquipmentSlotBlocked(equipSlot))
                {
                    return;
                }

                // РџР РћР’Р•Р РљРђ: РґР»СЏ РѕСЂСѓР¶РёСЏ РїСЂРѕРІРµСЂСЏРµРј Р’РЎР• СЃР»РѕС‚С‹ СЂСѓРє, РґР»СЏ РѕСЃС‚Р°Р»СЊРЅС‹С… - С†РµР»РµРІРѕР№ СЃР»РѕС‚
                if (item.itemType == ItemType.Weapon)
                {
                    ItemData rightHandItem = currentInventory.GetEquippedItem(EquipmentSlot.RightHand);
                    ItemData leftHandItem = currentInventory.GetEquippedItem(EquipmentSlot.LeftHand);

                    if ((rightHandItem != null && rightHandItem.itemName == item.itemName) ||
                        (leftHandItem != null && leftHandItem.itemName == item.itemName))
                    {
                        return;
                    }
                }
                else
                {
                    // Р”Р»СЏ РЅРµ-РѕСЂСѓР¶РёСЏ РїСЂРѕРІРµСЂСЏРµРј С‚РѕР»СЊРєРѕ С†РµР»РµРІРѕР№ СЃР»РѕС‚
                    ItemData equippedItem = currentInventory.GetEquippedItem(equipSlot);
                    if (equippedItem != null && equippedItem.itemName == item.itemName)
                    {
                        return;
                    }
                }

                // Temporarily change the item's equipment slot to match the target
                EquipmentSlot originalSlot = item.equipmentSlot;
                if (item.itemType == ItemType.Weapon)
                {
                    item.equipmentSlot = equipSlot; // Set to the slot we're equipping to
                }

                // Р­РєРёРїРёСЂСѓРµРј РїСЂРµРґРјРµС‚
                if (currentInventory.EquipItem(item))
                {
                    // Р’РђР–РќРћ: СѓРґР°Р»СЏРµРј РїСЂРµРґРјРµС‚ РёР· РєРѕРЅРєСЂРµС‚РЅРѕРіРѕ СЃР»РѕС‚Р°, Р° РЅРµ РїРµСЂРІС‹Р№ РЅР°Р№РґРµРЅРЅС‹Р№!
                    currentInventory.RemoveItemFromSlot(fromSlot, 1);

                    // Update UI to show blocked slots
                    UpdateEquipmentSlotVisuals();
                }
                else
                {
                    // Restore original slot if equipping failed
                    if (item.itemType == ItemType.Weapon)
                    {
                        item.equipmentSlot = originalSlot;
                    }
                }
            }
        }
    }

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚РєР° РїРµСЂРµС‚Р°СЃРєРёРІР°РЅРёСЏ РёР· СЌРєРёРїРёСЂРѕРІРєРё РІ РёРЅРІРµРЅС‚Р°СЂСЊ
    /// </summary>
    void HandleEquipmentToInventoryDrag(int equipSlotId, int toSlot)
    {
        EquipmentSlot equipSlot = (EquipmentSlot)equipSlotId;

        // UnequipItem Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРё РґРѕР±Р°РІР»СЏРµС‚ РїСЂРµРґРјРµС‚ РІ РёРЅРІРµРЅС‚Р°СЂСЊ, РїРѕСЌС‚РѕРјСѓ РґРѕРїРѕР»РЅРёС‚РµР»СЊРЅРѕ РґРѕР±Р°РІР»СЏС‚СЊ РЅРµ РЅСѓР¶РЅРѕ
        if (currentInventory.UnequipItem(equipSlot))
        {
            // Update UI to show unblocked slots
            UpdateEquipmentSlotVisuals();
        }
    }

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚РєР° РїРµСЂРµС‚Р°СЃРєРёРІР°РЅРёСЏ РјРµР¶РґСѓ СЃР»РѕС‚Р°РјРё СЌРєРёРїРёСЂРѕРІРєРё
    /// </summary>
    void HandleEquipmentToEquipmentDrag(int fromEquipSlotId, int toEquipSlotId)
    {
        EquipmentSlot fromEquipSlot = (EquipmentSlot)fromEquipSlotId;
        EquipmentSlot toEquipSlot = (EquipmentSlot)toEquipSlotId;

        ItemData fromItem = currentInventory.GetEquippedItem(fromEquipSlot);
        ItemData toItem = currentInventory.GetEquippedItem(toEquipSlot);

        if (fromItem == null)
        {
            return;
        }

        // Р’РђР–РќРћ: UnequipItem Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРё РґРѕР±Р°РІР»СЏРµС‚ РїСЂРµРґРјРµС‚С‹ РІ РёРЅРІРµРЅС‚Р°СЂСЊ!
        // РџРѕСЌС‚РѕРјСѓ РЅР°Рј РќР• РЅСѓР¶РЅРѕ РІС‹Р·С‹РІР°С‚СЊ AddItem РµСЃР»Рё СЌРєРёРїРёСЂРѕРІРєР° РЅРµ СѓРґР°Р»Р°СЃСЊ

        // РџСЂРѕРІРµСЂСЏРµРј, РјРѕР¶РЅРѕ Р»Рё РІС‹РїРѕР»РЅРёС‚СЊ РѕР±РјРµРЅ РџР•Р Р•Р” СЃРЅСЏС‚РёРµРј РїСЂРµРґРјРµС‚РѕРІ
        bool canEquipFromToTarget = (fromItem.equipmentSlot == toEquipSlot);
        bool canEquipToToSource = (toItem != null && toItem.equipmentSlot == fromEquipSlot);

        // Р•СЃР»Рё РЅРё РѕРґРёРЅ РїСЂРµРґРјРµС‚ РЅРµ РјРѕР¶РµС‚ Р±С‹С‚СЊ СЌРєРёРїРёСЂРѕРІР°РЅ РІ С†РµР»РµРІРѕР№ СЃР»РѕС‚, РѕС‚РјРµРЅСЏРµРј РѕРїРµСЂР°С†РёСЋ
        if (!canEquipFromToTarget && !canEquipToToSource)
        {
            return; // РќРµ СЃРЅРёРјР°РµРј РїСЂРµРґРјРµС‚С‹, РµСЃР»Рё РѕР±РјРµРЅ РЅРµРІРѕР·РјРѕР¶РµРЅ
        }

        // РЎРЅРёРјР°РµРј РїСЂРµРґРјРµС‚С‹ (РѕРЅРё Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРё РїРѕРїР°РґСѓС‚ РІ РёРЅРІРµРЅС‚Р°СЂСЊ)
        currentInventory.UnequipItem(fromEquipSlot);
        if (toItem != null)
        {
            currentInventory.UnequipItem(toEquipSlot);
        }

        // РџС‹С‚Р°РµРјСЃСЏ СЌРєРёРїРёСЂРѕРІР°С‚СЊ РІ РЅРѕРІС‹Рµ СЃР»РѕС‚С‹
        if (canEquipFromToTarget)
        {
            if (currentInventory.EquipItem(fromItem))
            {
                // РЈРґР°Р»СЏРµРј РёР· РёРЅРІРµРЅС‚Р°СЂСЏ, С‚Р°Рє РєР°Рє СЌРєРёРїРёСЂРѕРІРєР° СѓРґР°Р»Р°СЃСЊ
                currentInventory.RemoveItem(fromItem, 1);
            }
        }

        if (toItem != null && canEquipToToSource)
        {
            if (currentInventory.EquipItem(toItem))
            {
                // РЈРґР°Р»СЏРµРј РёР· РёРЅРІРµРЅС‚Р°СЂСЏ, С‚Р°Рє РєР°Рє СЌРєРёРїРёСЂРѕРІРєР° СѓРґР°Р»Р°СЃСЊ
                currentInventory.RemoveItem(toItem, 1);
            }
        }
    }


    /// <summary>
    /// Р’С‹РґРµР»РёС‚СЊ СЃР»РѕС‚
    /// </summary>
    void SelectSlot(int slotIndex)
    {
        // РЎРЅРёРјР°РµРј РІС‹РґРµР»РµРЅРёРµ СЃ РїСЂРµРґС‹РґСѓС‰РµРіРѕ СЃР»РѕС‚Р°
        if (selectedSlotUI != null)
        {
            selectedSlotUI.SetSelected(false);
        }

        // Р’С‹РґРµР»СЏРµРј РЅРѕРІС‹Р№ СЃР»РѕС‚
        if (slotIndex >= 0 && slotIndex < slotUIElements.Count)
        {
            selectedSlotUI = slotUIElements[slotIndex];
            selectedSlotUI.SetSelected(true);
        }
    }

    /// <summary>
    /// РЎРЅСЏС‚СЊ РІС‹РґРµР»РµРЅРёРµ СЃР»РѕС‚Р°
    /// </summary>
    void ClearSlotSelection()
    {
        if (selectedSlotUI != null)
        {
            selectedSlotUI.SetSelected(false);
            selectedSlotUI = null;
        }

    }


    /// <summary>
    /// РћР±СЂР°Р±РѕС‚РєР° РєР»РёРєР° РјС‹С€Рё
    /// </summary>
    void HandleMouseClick()
    {
        // РџСЂРѕРІРµСЂСЏРµРј, РєР»РёРєРЅСѓР»Рё Р»Рё РІРЅРµ UI РёРЅРІРµРЅС‚Р°СЂСЏ
        if (isInventoryVisible && !RectTransformUtility.RectangleContainsScreenPoint(inventoryPanel, Input.mousePosition, mainCanvas.worldCamera))
        {
            // Р•СЃР»Рё РєР»РёРєРЅСѓР»Рё РІРЅРµ РїР°РЅРµР»Рё, СЃРЅРёРјР°РµРј РІС‹РґРµР»РµРЅРёРµ СЃР»РѕС‚Р°
            ClearSlotSelection();
        }
    }

    /// <summary>
    /// РџРµСЂРµРєР»СЋС‡РёС‚СЊ РІРёРґРёРјРѕСЃС‚СЊ РёРЅРІРµРЅС‚Р°СЂСЏ
    /// </summary>
    public void ToggleInventory()
    {
        // Р•СЃР»Рё РёРЅРІРµРЅС‚Р°СЂСЊ РѕС‚РєСЂС‹С‚, РІСЃРµРіРґР° РїРѕР·РІРѕР»СЏРµРј РµРіРѕ Р·Р°РєСЂС‹С‚СЊ
        if (isInventoryVisible)
        {
            HideInventory();
            return;
        }

        // Р”Р»СЏ РѕС‚РєСЂС‹С‚РёСЏ РёРЅРІРµРЅС‚Р°СЂСЏ РЅСѓР¶РµРЅ РІС‹РґРµР»РµРЅРЅС‹Р№ РїРµСЂСЃРѕРЅР°Р¶
        if (selectionManager == null)
        {
            selectionManager = FindObjectOfType<SelectionManager>();
        }

        if (selectionManager != null)
        {
            List<GameObject> selectedObjects = selectionManager.GetSelectedObjects();

            // РџСЂРѕРІРµСЂСЏРµРј, РµСЃС‚СЊ Р»Рё РІС‹РґРµР»РµРЅРЅС‹Р№ РїРµСЂСЃРѕРЅР°Р¶ РёРіСЂРѕРєР°
            Character selectedCharacter = null;
            foreach (GameObject obj in selectedObjects)
            {
                Character character = obj.GetComponent<Character>();
                if (character != null && character.IsPlayerCharacter())
                {
                    selectedCharacter = character;
                    break;
                }
            }

            // Р•СЃР»Рё РµСЃС‚СЊ РІС‹РґРµР»РµРЅРЅС‹Р№ РїРµСЂСЃРѕРЅР°Р¶ РёРіСЂРѕРєР°, РѕС‚РєСЂС‹РІР°РµРј РµРіРѕ РёРЅРІРµРЅС‚Р°СЂСЊ
            if (selectedCharacter != null)
            {
                SetCurrentInventory(selectedCharacter.GetInventory(), selectedCharacter);
                ShowInventory();
            }
            else
            {
                // РќРµС‚ РІС‹РґРµР»РµРЅРЅРѕРіРѕ РїРµСЂСЃРѕРЅР°Р¶Р° - РЅРµ РѕС‚РєСЂС‹РІР°РµРј РёРЅРІРµРЅС‚Р°СЂСЊ
            }
        }
    }

    /// <summary>
    /// РџРѕРєР°Р·Р°С‚СЊ РёРЅРІРµРЅС‚Р°СЂСЊ
    /// </summary>
    public void ShowInventory()
    {
        // РђРєС‚РёРІРёСЂСѓРµРј С‚РѕР»СЊРєРѕ РїР°РЅРµР»СЊ Inventory, РЅРµ РІРµСЃСЊ Canvas
        if (inventoryPanel != null)
        {
            inventoryPanel.gameObject.SetActive(true);
            isInventoryVisible = true;
            IsAnyInventoryOpen = true;


            foreach (var kvp in equipmentSlotUIs)
            {
                InventorySlotUI slotUI = kvp.Value;
                if (slotUI != null)
                {
                    // РЎР»РѕС‚ СЃСѓС‰РµСЃС‚РІСѓРµС‚ - РѕР±РЅРѕРІР»СЏРµРј СЃРѕСЃС‚РѕСЏРЅРёРµ
                }
            }

            // РћР±РЅРѕРІР»СЏРµРј РѕС‚РѕР±СЂР°Р¶РµРЅРёРµ
            UpdateInventoryDisplay();
        }
        else
        {

        }
    }

    /// <summary>
    /// РЎРєСЂС‹С‚СЊ РёРЅРІРµРЅС‚Р°СЂСЊ
    /// </summary>
    public void HideInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.gameObject.SetActive(false);
            isInventoryVisible = false;
            IsAnyInventoryOpen = false;

            // РЎРЅРёРјР°РµРј РІС‹РґРµР»РµРЅРёРµ
            ClearSlotSelection();

            // РЎРєСЂС‹РІР°РµРј tooltip РїСЂРё Р·Р°РєСЂС‹С‚РёРё РёРЅРІРµРЅС‚Р°СЂСЏ
            if (TooltipSystem.Instance != null)
            {
                TooltipSystem.Instance.HideTooltip();
            }
        }
        else
        {

        }
    }

    void OnDestroy()
    {
        if (selectionManager != null)
        {
            selectionManager.OnSelectionChanged -= OnSelectionChanged;
        }

        if (currentInventory != null)
        {
            currentInventory.OnInventoryChanged -= UpdateInventoryDisplay;
        }

        ClearSlotUIElements();

        // РћС‡РёС‰Р°РµРј СЃР»РѕС‚С‹ СЌРєРёРїРёСЂРѕРІРєРё С‚РѕР»СЊРєРѕ РїСЂРё СѓРЅРёС‡С‚РѕР¶РµРЅРёРё
        // Р’РђР–РќРћ: РџСЂРѕРІРµСЂСЏРµРј С‡С‚Рѕ СЃР»РѕРІР°СЂСЊ РЅРµ null (РјРѕР¶РµС‚ Р±С‹С‚СЊ СѓРЅРёС‡С‚РѕР¶РµРЅ РїСЂРё РІС‹С…РѕРґРµ РёР· Play Mode)
        if (equipmentSlotUIs != null)
        {
            try
            {
                foreach (var kvp in equipmentSlotUIs)
                {
                    InventorySlotUI slotUI = kvp.Value;
                    if (slotUI != null)
                    {
                        slotUI.OnSlotClicked -= OnEquipmentSlotClicked;
                        slotUI.OnSlotRightClicked -= OnEquipmentSlotRightClicked;
                        slotUI.OnSlotDoubleClicked -= OnEquipmentSlotDoubleClicked;
                        slotUI.OnSlotDragAndDrop -= OnSlotDragAndDrop;
                        DestroyImmediate(slotUI.gameObject);
                    }
                }
                equipmentSlotUIs.Clear();
            }
            catch (System.Exception)
            {
                // РРіРЅРѕСЂРёСЂСѓРµРј РѕС€РёР±РєРё РїСЂРё СѓРЅРёС‡С‚РѕР¶РµРЅРёРё РѕР±СЉРµРєС‚РѕРІ
                // РЎР»РѕРІР°СЂСЊ РјРѕР¶РµС‚ Р±С‹С‚СЊ С‡Р°СЃС‚РёС‡РЅРѕ СѓРЅРёС‡С‚РѕР¶РµРЅ
            }
        }
    }

    /// <summary>
    /// РћР±РЅРѕРІРёС‚СЊ РІРёР·СѓР°Р»СЊРЅРѕРµ РѕС‚РѕР±СЂР°Р¶РµРЅРёРµ СЃР»РѕС‚РѕРІ СЌРєРёРїРёСЂРѕРІРєРё (РїРѕРєР°Р·Р°С‚СЊ Р·Р°Р±Р»РѕРєРёСЂРѕРІР°РЅРЅС‹Рµ)
    /// </summary>
    void UpdateEquipmentSlotVisuals()
    {
        if (currentInventory == null || equipmentSlotUIs == null) return;

        foreach (var kvp in equipmentSlotUIs)
        {
            EquipmentSlot slot = kvp.Key;
            InventorySlotUI slotUI = kvp.Value;

            if (slotUI != null && slotUI.backgroundImage != null)
            {
                // Check if this slot should be blocked
                bool isBlocked = currentInventory.IsEquipmentSlotBlocked(slot);
                bool hasItem = currentInventory.IsEquipped(slot);

                if (isBlocked && !hasItem)
                {
                    // Show blocked state (gray/darker) but still visible
                    slotUI.backgroundImage.color = new Color(0.4f, 0.4f, 0.4f, 1.0f); // Visible blocked state
                }
                else if (hasItem)
                {
                    // Show equipped state (bright for visibility)
                    slotUI.backgroundImage.color = new Color(0.6f, 0.8f, 1.0f, 1.0f); // Bright equipped color
                }
                else
                {
                    // Show empty state (still visible!)
                    slotUI.backgroundImage.color = new Color(0.8f, 0.8f, 0.9f, 1.0f); // Bright empty state
                }

                // РџСЂРёРЅСѓРґРёС‚РµР»СЊРЅРѕ РІРєР»СЋС‡Р°РµРј РІРёРґРёРјРѕСЃС‚СЊ
                slotUI.backgroundImage.enabled = true;

            }
        }
    }
}
