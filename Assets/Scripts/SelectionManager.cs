using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectionManager : MonoBehaviour
{
    [Header("Selection Settings")]
    public Camera playerCamera;
    public LayerMask selectableLayerMask = -1;
    public Color selectionBoxColor = new Color(0.8f, 0.8f, 1f, 0.25f);
    public Color selectionBoxBorderColor = new Color(0.8f, 0.8f, 1f, 1f);
    public float selectionIndicatorHeight = 2f;

    [Header("Item Pickup Settings")]
    [Tooltip("Р’РєР»СЋС‡РёС‚СЊ РїРѕРґР±РѕСЂ РїСЂРµРґРјРµС‚РѕРІ РїСЂРё РєР»РёРєРµ РџРљРњ РЅР° СЂРµСЃСѓСЂСЃ")]
    public bool enableRightClickPickup = true;

    [Header("Hover Highlight")]
    public Color hoverColor = Color.cyan;
    
    [Header("Visual Indicators")]
    public GameObject selectionIndicatorPrefab;
    public Color selectionIndicatorColor = Color.yellow;
    
    [Header("UI")]
    public RectTransform selectionInfoPanel;
    public Text selectionInfoText;
    public Canvas uiCanvas;
    
    // Р’РЅСѓС‚СЂРµРЅРЅРёРµ РїРµСЂРµРјРµРЅРЅС‹Рµ
    private List<GameObject> selectedObjects = new List<GameObject>();
    private Dictionary<GameObject, GameObject> selectionIndicators = new Dictionary<GameObject, GameObject>();
    
    // РџРµСЂРµРјРµРЅРЅС‹Рµ РґР»СЏ box selection Рё РєР»РёРєРѕРІ
    private bool isBoxSelecting = false;
    private bool isMousePressed = false;
    private Vector3 boxStartPosition;
    private Vector3 boxEndPosition;
    private Vector3 mouseDownPosition;
    private float clickThreshold = 5f; // РџРёРєСЃРµР»РµР№ РґР»СЏ РѕРїСЂРµРґРµР»РµРЅРёСЏ РєР»РёРєР° vs СЂР°РјРєРё

    // UI РґР»СЏ СЂР°РјРєРё РІС‹РґРµР»РµРЅРёСЏ
    private GameObject selectionBoxUI;
    private Image selectionBoxImage;
    
    // РЎРѕР±С‹С‚РёСЏ
    public System.Action<List<GameObject>> OnSelectionChanged;

    // Р¤Р»Р°Рі РґР»СЏ РїСЂРµРґРѕС‚РІСЂР°С‰РµРЅРёСЏ РѕР±СЂР°Р±РѕС‚РєРё РєР»РёРєР° РґСЂСѓРіРёРјРё СЃРёСЃС‚РµРјР°РјРё
    private bool rightClickHandledThisFrame = false;

    // Hover СЃРёСЃС‚РµРјР°
    private GameObject currentHoveredObject = null;
    private Dictionary<MeshRenderer, Material> originalMaterials = new Dictionary<MeshRenderer, Material>();
    private Dictionary<MeshRenderer, Material> hoverMaterials = new Dictionary<MeshRenderer, Material>();
    private List<MeshRenderer> currentHighlightedRenderers = new List<MeshRenderer>();

    // ARCHITECTURE: РљСЌС€РёСЂРѕРІР°РЅРЅС‹Рµ СЃСЃС‹Р»РєРё РЅР° РјРµРЅРµРґР¶РµСЂС‹ С‡РµСЂРµР· ServiceLocator
    private GridManager gridManager;
    private MiningManager miningManager;

    // РџСѓР±Р»РёС‡РЅС‹Рµ СЃРІРѕР№СЃС‚РІР°
    public bool IsBoxSelecting => isBoxSelecting;
    public bool RightClickHandledThisFrame => rightClickHandledThisFrame;
    
    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        // ARCHITECTURE: РџРѕР»СѓС‡Р°РµРј РјРµРЅРµРґР¶РµСЂС‹ С‡РµСЂРµР· ServiceLocator РІРјРµСЃС‚Рѕ FindObjectOfType
        // Р­С‚Рѕ РЅР°РјРЅРѕРіРѕ Р±С‹СЃС‚СЂРµРµ Рё Р±РѕР»РµРµ РЅР°РґРµР¶РЅРѕ
        if (ServiceLocator.IsInitialized)
        {
            gridManager = ServiceLocator.Get<GridManager>();
            // MiningManager СЃРѕР·РґР°РµС‚СЃСЏ РґРёРЅР°РјРёС‡РµСЃРєРё, РїРѕСЌС‚РѕРјСѓ РјРѕР¶РµС‚ РѕС‚СЃСѓС‚СЃС‚РІРѕРІР°С‚СЊ
            ServiceLocator.TryGet<MiningManager>(out miningManager);
        }
        else
        {
            gridManager = FindObjectOfType<GridManager>();
        }

        InitializeUI();
        CreateSelectionIndicatorPrefab();

        // РћС‚Р»РѕР¶РµРЅРЅР°СЏ РґРёР°РіРЅРѕСЃС‚РёРєР° (РґР°РµРј РІСЂРµРјСЏ РѕР±СЉРµРєС‚Р°Рј СЃРѕР·РґР°С‚СЊСЃСЏ)
        Invoke("DiagnoseSelectableObjects", 1f);
    }
    
    void Update()
    {
        // РЎР±СЂР°СЃС‹РІР°РµРј С„Р»Р°Рі РІ РЅР°С‡Р°Р»Рµ РєР°Р¶РґРѕРіРѕ РєР°РґСЂР°
        rightClickHandledThisFrame = false;

        // Р‘Р»РѕРєРёСЂСѓРµРј РІРІРѕРґ РµСЃР»Рё РѕС‚РєСЂС‹С‚ РёРЅРІРµРЅС‚Р°СЂСЊ РёР»Рё РјРµРЅСЋ РїР°СѓР·С‹
        if (!InventoryUI.IsAnyInventoryOpen && !IsGamePaused())
        {
            HandleMouseInput();
            UpdateSelectionBox();
            HandleHover();
        }

        UpdateSelectionIndicatorPositions();
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ РЅР°С…РѕРґРёС‚СЃСЏ Р»Рё РёРіСЂР° РЅР° РїР°СѓР·Рµ
    /// </summary>
    bool IsGamePaused()
    {
        return GamePauseManager.Instance != null && GamePauseManager.Instance.IsPaused();
    }
    
    /// <summary>
    /// РРЅРёС†РёР°Р»РёР·Р°С†РёСЏ UI СЌР»РµРјРµРЅС‚РѕРІ
    /// </summary>
    void InitializeUI()
    {
        if (uiCanvas == null)
            uiCanvas = FindObjectOfType<Canvas>();
            
        if (uiCanvas == null)
        {
            GameObject canvasGO = new GameObject("SelectionCanvas");
            uiCanvas = canvasGO.AddComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        
        // РЈР±СЂР°Р»Рё РїР°РЅРµР»СЊ РёРЅС„РѕСЂРјР°С†РёРё Рѕ РІС‹РґРµР»РµРЅРёРё СЃРІРµСЂС…Сѓ СЃР»РµРІР° - С‚РµРїРµСЂСЊ Р±СѓРґРµС‚ С‚РѕР»СЊРєРѕ СЃРЅРёР·Сѓ

        // РЎРѕР·РґР°РµРј UI СЌР»РµРјРµРЅС‚ РґР»СЏ СЂР°РјРєРё РІС‹РґРµР»РµРЅРёСЏ
        GameObject boxGO = new GameObject("SelectionBox");
        boxGO.transform.SetParent(uiCanvas.transform, false);

        selectionBoxUI = boxGO;
        selectionBoxImage = boxGO.AddComponent<Image>();
        selectionBoxImage.color = selectionBoxColor;

        RectTransform boxRect = boxGO.GetComponent<RectTransform>();
        boxRect.anchorMin = Vector2.zero;
        boxRect.anchorMax = Vector2.zero;
        boxRect.pivot = Vector2.zero;

        selectionBoxUI.SetActive(false);
    }
    
    /// <summary>
    /// РЎРѕР·РґР°РЅРёРµ РїСЂРµС„Р°Р±Р° РёРЅРґРёРєР°С‚РѕСЂР° РІС‹РґРµР»РµРЅРёСЏ РµСЃР»Рё РµРіРѕ РЅРµС‚
    /// </summary>
    void CreateSelectionIndicatorPrefab()
    {
        if (selectionIndicatorPrefab == null)
        {
            GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            prefab.name = "SelectionIndicator";
            prefab.transform.localScale = Vector3.one * 0.6f;
            
            // РЈР±РёСЂР°РµРј РєРѕР»Р»Р°Р№РґРµСЂ
            DestroyImmediate(prefab.GetComponent<Collider>());
            
            // РќР°СЃС‚СЂР°РёРІР°РµРј РјР°С‚РµСЂРёР°Р»
            Renderer renderer = prefab.GetComponent<Renderer>();
            Material material = new Material(Shader.Find("Standard"));
            material.color = selectionIndicatorColor;
            material.SetFloat("_Mode", 3); // Transparent mode
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            renderer.material = material;
            
            prefab.SetActive(false);
            selectionIndicatorPrefab = prefab;
        }
    }
    
    /// <summary>
    /// РћР±СЂР°Р±РѕС‚РєР° РІРІРѕРґР° РјС‹С€Рё
    /// </summary>
    void HandleMouseInput()
    {
        // РќР°Р¶Р°С‚РёРµ Р›РљРњ - Р·Р°РїРѕРјРёРЅР°РµРј РїРѕР·РёС†РёСЋ
        if (Input.GetMouseButtonDown(0))
        {

            isMousePressed = true;
            mouseDownPosition = Input.mousePosition;
            boxStartPosition = Input.mousePosition;
        }

        // Р”РІРёР¶РµРЅРёРµ РјС‹С€Рё РїСЂРё Р·Р°Р¶Р°С‚РѕР№ Р›РљРњ
        if (isMousePressed && Input.GetMouseButton(0))
        {
            boxEndPosition = Input.mousePosition;
            float distance = Vector3.Distance(mouseDownPosition, Input.mousePosition);

            // РќР°С‡РёРЅР°РµРј box selection С‚РѕР»СЊРєРѕ РµСЃР»Рё РјС‹С€СЊ РґРІРёРЅСѓР»Р°СЃСЊ РґРѕСЃС‚Р°С‚РѕС‡РЅРѕ РґР°Р»РµРєРѕ
            if (distance > clickThreshold && !isBoxSelecting)
            {
                // РџСЂРѕРІРµСЂСЏРµРј, РµСЃС‚СЊ Р»Рё РїРѕРґ РєСѓСЂСЃРѕСЂРѕРј СЋРЅРёС‚С‹ (РїРµСЂСЃРѕРЅР°Р¶Рё)
                if (HasCharactersInArea())
                {
                    isBoxSelecting = true;
                    selectionBoxUI.SetActive(true);
                }
            }
        }

        // РћС‚РїСѓСЃРєР°РЅРёРµ Р›РљРњ
        if (Input.GetMouseButtonUp(0) && isMousePressed)
        {
            // РџСЂРѕРІРµСЂСЏРµРј, Р±С‹Р» Р»Рё РєР»РёРє РїРѕ UI СЌР»РµРјРµРЅС‚Сѓ
            bool isPointerOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                                   UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

            if (isBoxSelecting)
            {

                // Р’С‹РїРѕР»РЅСЏРµРј box selection С‚РѕР»СЊРєРѕ РґР»СЏ СЋРЅРёС‚РѕРІ
                PerformBoxSelection();
                isBoxSelecting = false;
                selectionBoxUI.SetActive(false);
            }
            else if (!isPointerOverUI)
            {
                // РћР±С‹С‡РЅРѕРµ РєР»РёРє-РІС‹РґРµР»РµРЅРёРµ - С‚РѕР»СЊРєРѕ РµСЃР»Рё РќР• РєР»РёРєРЅСѓР»Рё РїРѕ UI
                PerformClickSelection(mouseDownPosition);
            }
            isMousePressed = false;
        }

        // РџРљРњ - РІР·Р°РёРјРѕРґРµР№СЃС‚РІРёРµ СЃ РїСЂРµРґРјРµС‚Р°РјРё (РїРѕРґР±РѕСЂ СЂРµСЃСѓСЂСЃРѕРІ)
        if (Input.GetMouseButtonDown(1))
        {
            bool isPointerOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                                   UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

            if (!isPointerOverUI)
            {
                HandleRightClick();
            }
        }
    }
    
    /// <summary>
    /// РџСЂРѕРІРµСЂРєР° РЅР°Р»РёС‡РёСЏ СЋРЅРёС‚РѕРІ (РїРµСЂСЃРѕРЅР°Р¶РµР№) РІ РѕР±Р»Р°СЃС‚Рё
    /// </summary>
    bool HasCharactersInArea()
    {
        // РџСЂРѕСЃС‚РѕРµ РѕРїСЂРµРґРµР»РµРЅРёРµ - РІРѕР·РІСЂР°С‰Р°РµРј true РµСЃР»Рё РІ СЃС†РµРЅРµ РµСЃС‚СЊ Character'С‹
        Character[] characters = FindObjectsOfType<Character>();
        return characters.Length > 0;
    }

    /// <summary>
    /// РћР±РЅРѕРІР»РµРЅРёРµ РІРёР·СѓР°Р»СЊРЅРѕР№ СЂР°РјРєРё РІС‹РґРµР»РµРЅРёСЏ
    /// </summary>
    void UpdateSelectionBox()
    {
        if (!isBoxSelecting || selectionBoxUI == null) return;

        Vector2 start = boxStartPosition;
        Vector2 end = boxEndPosition;

        Vector2 min = Vector2.Min(start, end);
        Vector2 max = Vector2.Max(start, end);

        RectTransform boxRect = selectionBoxUI.GetComponent<RectTransform>();
        boxRect.anchoredPosition = min;
        boxRect.sizeDelta = max - min;
    }

    /// <summary>
    /// Р’С‹РїРѕР»РЅРµРЅРёРµ box selection С‚РѕР»СЊРєРѕ РґР»СЏ СЋРЅРёС‚РѕРІ
    /// </summary>
    void PerformBoxSelection()
    {
        Vector2 start = playerCamera.ScreenToWorldPoint(boxStartPosition);
        Vector2 end = playerCamera.ScreenToWorldPoint(boxEndPosition);

        Vector2 min = Vector2.Min(start, end);
        Vector2 max = Vector2.Max(start, end);

        List<GameObject> newSelections = new List<GameObject>();

        // РќР°С…РѕРґРёРј С‚РѕР»СЊРєРѕ Character'РѕРІ РІ РѕР±Р»Р°СЃС‚Рё
        Character[] allCharacters = FindObjectsOfType<Character>();

        foreach (Character character in allCharacters)
        {
            // Р Р°РјРєРѕР№ РјРѕР¶РЅРѕ РІС‹РґРµР»СЏС‚СЊ С‚РѕР»СЊРєРѕ СЃРѕСЋР·РЅРёРєРѕРІ
            if (!character.IsPlayerCharacter())
            {
                continue;
            }

            Vector3 worldPos = character.transform.position;
            Vector2 screenPos = playerCamera.WorldToScreenPoint(worldPos);

            Vector2 boxMin = Vector2.Min(boxStartPosition, boxEndPosition);
            Vector2 boxMax = Vector2.Max(boxStartPosition, boxEndPosition);

            if (screenPos.x >= boxMin.x && screenPos.x <= boxMax.x &&
                screenPos.y >= boxMin.y && screenPos.y <= boxMax.y)
            {
                newSelections.Add(character.gameObject);
            }
        }

        // Р›РѕРіРёРєР° РІС‹РґРµР»РµРЅРёСЏ
        if (newSelections.Count > 0)
        {
            // Р•СЃС‚СЊ СЋРЅРёС‚С‹ РІ СЂР°РјРєРµ
            if (!Input.GetKey(KeyCode.LeftControl))
            {
                // Р•СЃР»Рё Ctrl РЅРµ Р·Р°Р¶Р°С‚, Р·Р°РјРµРЅСЏРµРј РІС‹РґРµР»РµРЅРёРµ
                ClearSelection();
            }

            // Р”РѕР±Р°РІР»СЏРµРј РЅРѕРІС‹С… СЋРЅРёС‚РѕРІ
            foreach (GameObject obj in newSelections)
            {
                if (!selectedObjects.Contains(obj))
                {
                    AddToSelection(obj);
                }
            }
        }
        else
        {
            // Р’ СЂР°РјРєСѓ РЅРёС‡РµРіРѕ РЅРµ РїРѕРїР°Р»Рѕ
            if (!Input.GetKey(KeyCode.LeftControl))
            {
                ClearSelection();
            }
        }

        UpdateSelectionInfo();
    }

    /// <summary>
    /// Р’С‹РїРѕР»РЅРµРЅРёРµ РІС‹РґРµР»РµРЅРёСЏ РїРѕ РєР»РёРєСѓ (РґР»СЏ Р·РґР°РЅРёР№ Рё РјРѕРґСѓР»РµР№)
    /// </summary>
    void PerformClickSelection(Vector3 mousePosition)
    {
        Ray ray = playerCamera.ScreenPointToRay(mousePosition);

        // РСЃРїРѕР»СЊР·СѓРµРј RaycastAll С‡С‚РѕР±С‹ РїРѕР»СѓС‡РёС‚СЊ РІСЃРµ РѕР±СЉРµРєС‚С‹ РЅР° Р»СѓС‡Рµ, РІРєР»СЋС‡Р°СЏ С‚СЂРёРіРіРµСЂС‹
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, selectableLayerMask, QueryTriggerInteraction.Collide);

        if (hits.Length > 0)
        {
            // РС‰РµРј РїРµСЂРІС‹Р№ РїРѕРґС…РѕРґСЏС‰РёР№ РѕР±СЉРµРєС‚ РґР»СЏ РІС‹РґРµР»РµРЅРёСЏ
            foreach (RaycastHit rayHit in hits)
            {
                GameObject hitObject = rayHit.collider.gameObject;

                // РСЃРєР»СЋС‡Р°РµРј СЃРёСЃС‚РµРјРЅС‹Рµ РѕР±СЉРµРєС‚С‹ РёР· РІС‹РґРµР»РµРЅРёСЏ
                if (hitObject.name.Contains("Bounds") || hitObject.name.Contains("Grid") ||
                    hitObject.name.Contains("Location") && !hitObject.name.Contains("Test") ||
                    hitObject.name.Contains("Plane"))
                {
                    continue;
                }

                // РџСЂРѕРІРµСЂСЏРµРј, СЏРІР»СЏРµС‚СЃСЏ Р»Рё СЌС‚Рѕ РїРµСЂСЃРѕРЅР°Р¶РѕРј
                Character character = hitObject.GetComponent<Character>();
                GameObject characterObject = hitObject;

                if (character == null)
                {
                    // РС‰РµРј РєРѕРјРїРѕРЅРµРЅС‚ Character РІ СЂРѕРґРёС‚РµР»СЊСЃРєРёС… РѕР±СЉРµРєС‚Р°С…
                    character = hitObject.GetComponentInParent<Character>();
                    if (character != null)
                    {
                        characterObject = character.gameObject;
                    }
                }

                if (character != null)
                {

                    // Р”Р»СЏ РїРµСЂСЃРѕРЅР°Р¶РµР№ РїРѕРґРґРµСЂР¶РёРІР°РµРј Ctrl+РєР»РёРє РґР»СЏ РјРЅРѕР¶РµСЃС‚РІРµРЅРЅРѕРіРѕ РІС‹РґРµР»РµРЅРёСЏ С‚РѕР»СЊРєРѕ Сѓ СЃРѕСЋР·РЅРёРєРѕРІ
                    // Р’СЂР°РіРѕРІ РјРѕР¶РЅРѕ РІС‹РґРµР»СЏС‚СЊ С‚РѕР»СЊРєРѕ РїРѕ РѕРґРЅРѕРјСѓ РґР»СЏ РїСЂРѕСЃРјРѕС‚СЂР° РёРЅС„РѕСЂРјР°С†РёРё
                    if (character.IsPlayerCharacter())
                    {

                        // РЎРѕСЋР·РЅРёРєРѕРІ РјРѕР¶РЅРѕ РІС‹РґРµР»СЏС‚СЊ РіСЂСѓРїРїР°РјРё
                        if (!Input.GetKey(KeyCode.LeftControl))
                        {
                            ClearSelection();
                        }
                    }
                    else
                    {
                        // Р’СЂР°РіРѕРІ РІСЃРµРіРґР° РІС‹РґРµР»СЏРµРј РїРѕ РѕРґРЅРѕРјСѓ (РѕС‡РёС‰Р°РµРј РїСЂРµРґС‹РґСѓС‰РµРµ РІС‹РґРµР»РµРЅРёРµ)
                        ClearSelection();
                    }


                    ToggleSelection(characterObject);
                    return;
                }

                // РџСЂРѕРІРµСЂСЏРµРј, СЏРІР»СЏРµС‚СЃСЏ Р»Рё СЌС‚Рѕ РїСЂРµРґРјРµС‚РѕРј РёРЅРІРµРЅС‚Р°СЂСЏ
                Item item = hitObject.GetComponent<Item>();
                if (item != null)
                {
                    // РџСЂРµРґРјРµС‚С‹ РІС‹РґРµР»СЏРµРј РїРѕ РѕРґРЅРѕРјСѓ
                    ClearSelection();
                    ToggleSelection(hitObject);
                    return;
                }

                LocationObjectInfo objectInfo = hitObject.GetComponent<LocationObjectInfo>();
                if (objectInfo != null)
                {
                    GameObject targetObject = hitObject;

                    // РџСЂРѕРІРµСЂСЏРµРј, СЏРІР»СЏРµС‚СЃСЏ Р»Рё СЌС‚Рѕ РїРѕР»РѕРј РєРѕРјРЅР°С‚С‹
                    RoomInfo roomInfo = hitObject.GetComponentInParent<RoomInfo>();
                    if (roomInfo != null)
                    {
                        // Р•СЃР»Рё СЌС‚Рѕ РїРѕР» РєРѕРјРЅР°С‚С‹, РІС‹РґРµР»СЏРµРј СЂРѕРґРёС‚РµР»СЊСЃРєСѓСЋ РєРѕРјРЅР°С‚Сѓ
                        targetObject = roomInfo.gameObject;
                    }

                    // РќР°Р№РґРµРЅ РїРѕРґС…РѕРґСЏС‰РёР№ РѕР±СЉРµРєС‚ РґР»СЏ РІС‹РґРµР»РµРЅРёСЏ
                    // Р”Р»СЏ Р·РґР°РЅРёР№/РјРѕРґСѓР»РµР№ РІСЃРµРіРґР° РІС‹РґРµР»СЏРµРј С‚РѕР»СЊРєРѕ РѕРґРёРЅ РѕР±СЉРµРєС‚
                    ClearSelection();
                    ToggleSelection(targetObject);
                    return;
                }
            }
        }

        // Р•СЃР»Рё РјС‹ РґРѕС€Р»Рё РґРѕ СЌС‚РѕРіРѕ РјРµСЃС‚Р°, Р·РЅР°С‡РёС‚ РЅРµ РЅР°Р№РґРµРЅРѕ РїРѕРґС…РѕРґСЏС‰РёС… РѕР±СЉРµРєС‚РѕРІ
        // РџРѕРїСЂРѕР±СѓРµРј РёСЃРїСЂР°РІРёС‚СЊ РєРѕРјРїРѕРЅРµРЅС‚С‹ РєРѕРјРЅР°С‚ РµСЃР»Рё РёС… РЅРµ Р±С‹Р»Рѕ РЅР°Р№РґРµРЅРѕ
        CheckAndFixRoomComponents();

        // Р­С‚Рѕ СЃС‡РёС‚Р°РµС‚СЃСЏ РєР»РёРєРѕРј РІ РїСѓСЃС‚РѕРµ РјРµСЃС‚Рѕ - РІСЃРµРіРґР° РѕС‡РёС‰Р°РµРј РІС‹РґРµР»РµРЅРёРµ
        ClearSelection();
    }
    
    
    
    /// <summary>
    /// РџРµСЂРµРєР»СЋС‡РµРЅРёРµ РІС‹РґРµР»РµРЅРёСЏ РѕР±СЉРµРєС‚Р°
    /// </summary>
    public void ToggleSelection(GameObject obj)
    {


        if (selectedObjects.Contains(obj))
        {

            RemoveFromSelection(obj);
        }
        else
        {

            AddToSelection(obj);
        }
    }
    
    /// <summary>
    /// Р”РѕР±Р°РІР»РµРЅРёРµ РѕР±СЉРµРєС‚Р° Рє РІС‹РґРµР»РµРЅРёСЋ
    /// </summary>
    public void AddToSelection(GameObject obj)
    {
        if (!selectedObjects.Contains(obj))
        {
            // РЈР±РёСЂР°РµРј hover РїРѕРґСЃРІРµС‚РєСѓ РµСЃР»Рё РѕР±СЉРµРєС‚ Р±С‹Р» РїРѕРґСЃРІРµС‡РµРЅ
            if (currentHoveredObject == obj)
            {
                EndHover(obj);
            }

            // РћС‡РёС‰Р°РµРј Р»СЋР±С‹Рµ СЃРѕС…СЂР°РЅРµРЅРЅС‹Рµ hover РјР°С‚РµСЂРёР°Р»С‹ РґР»СЏ СЌС‚РѕРіРѕ РѕР±СЉРµРєС‚Р°
            ClearHoverMaterialsForObject(obj);

            selectedObjects.Add(obj);
            CreateSelectionIndicator(obj);
            SetObjectSelectionState(obj, true);
            UpdateSelectionInfo();
            OnSelectionChanged?.Invoke(selectedObjects);
        }
    }
    
    /// <summary>
    /// РЈРґР°Р»РµРЅРёРµ РѕР±СЉРµРєС‚Р° РёР· РІС‹РґРµР»РµРЅРёСЏ
    /// </summary>
    public void RemoveFromSelection(GameObject obj)
    {
        if (selectedObjects.Remove(obj))
        {
            RemoveSelectionIndicator(obj);
            if (obj != null) // РџСЂРѕРІРµСЂСЏРµРј РїРµСЂРµРґ РѕР±СЂР°С‰РµРЅРёРµРј Рє РѕР±СЉРµРєС‚Сѓ
            {
                SetObjectSelectionState(obj, false);

                // Р•СЃР»Рё РєСѓСЂСЃРѕСЂ РЅР°С…РѕРґРёС‚СЃСЏ РЅР°Рґ СЌС‚РёРј РѕР±СЉРµРєС‚РѕРј, РїСЂРёРјРµРЅСЏРµРј hover РїРѕРґСЃРІРµС‚РєСѓ
                if (currentHoveredObject == obj)
                {
                    StartHover(obj);
                }
            }
            UpdateSelectionInfo();
            OnSelectionChanged?.Invoke(selectedObjects);
        }
    }
    
    /// <summary>
    /// РћС‡РёСЃС‚РєР° РІСЃРµРіРѕ РІС‹РґРµР»РµРЅРёСЏ
    /// </summary>
    public void ClearSelection()
    {
        // РЎРѕР·РґР°РµРј РєРѕРїРёСЋ СЃРїРёСЃРєР° РґР»СЏ Р±РµР·РѕРїР°СЃРЅРѕР№ РёС‚РµСЂР°С†РёРё
        var objectsToProcess = new List<GameObject>(selectedObjects);

        foreach (GameObject obj in objectsToProcess)
        {
            if (obj != null) // РџСЂРѕРІРµСЂСЏРµРј, С‡С‚Рѕ РѕР±СЉРµРєС‚ РЅРµ Р±С‹Р» СѓРЅРёС‡С‚РѕР¶РµРЅ
            {
                RemoveSelectionIndicator(obj);
                SetObjectSelectionState(obj, false);

                // Р•СЃР»Рё РєСѓСЂСЃРѕСЂ РЅР°С…РѕРґРёС‚СЃСЏ РЅР°Рґ СЌС‚РёРј РѕР±СЉРµРєС‚РѕРј, РїСЂРёРјРµРЅСЏРµРј hover РїРѕРґСЃРІРµС‚РєСѓ
                if (currentHoveredObject == obj)
                {
                    StartHover(obj);
                }
            }
            else
            {
                // РЈРґР°Р»СЏРµРј null-СЃСЃС‹Р»РєРё РёР· СЃР»РѕРІР°СЂСЏ РёРЅРґРёРєР°С‚РѕСЂРѕРІ
                if (selectionIndicators.ContainsKey(obj))
                {
                    selectionIndicators.Remove(obj);
                }
            }
        }

        selectedObjects.Clear();
        UpdateSelectionInfo();
        OnSelectionChanged?.Invoke(selectedObjects);
    }
    
    /// <summary>
    /// РЎРѕР·РґР°РЅРёРµ РІРёР·СѓР°Р»СЊРЅРѕРіРѕ РёРЅРґРёРєР°С‚РѕСЂР° РІС‹РґРµР»РµРЅРёСЏ
    /// </summary>
    void CreateSelectionIndicator(GameObject targetObject)
    {
        if (selectionIndicators.ContainsKey(targetObject))
            return;
            
        GameObject indicator = Instantiate(selectionIndicatorPrefab);
        indicator.SetActive(true);
        
        // РџРѕР·РёС†РёРѕРЅРёСЂСѓРµРј РЅР°Рґ РѕР±СЉРµРєС‚РѕРј
        Bounds bounds = GetObjectBounds(targetObject);
        Vector3 position = bounds.center + Vector3.up * (bounds.size.y * 0.5f + selectionIndicatorHeight);
        indicator.transform.position = position;
        
        selectionIndicators[targetObject] = indicator;
    }
    
    /// <summary>
    /// РЈРґР°Р»РµРЅРёРµ РІРёР·СѓР°Р»СЊРЅРѕРіРѕ РёРЅРґРёРєР°С‚РѕСЂР° РІС‹РґРµР»РµРЅРёСЏ
    /// </summary>
    void RemoveSelectionIndicator(GameObject targetObject)
    {
        if (selectionIndicators.TryGetValue(targetObject, out GameObject indicator))
        {
            if (indicator != null)
            {
                try
                {
                    DestroyImmediate(indicator);
                }
                catch (System.Exception)
                {
                    // Error handled silently
                }
            }
            selectionIndicators.Remove(targetObject);
        }
    }

    /// <summary>
    /// РћР±РЅРѕРІР»РµРЅРёРµ РїРѕР·РёС†РёР№ РёРЅРґРёРєР°С‚РѕСЂРѕРІ РІС‹РґРµР»РµРЅРёСЏ
    /// </summary>
    void UpdateSelectionIndicatorPositions()
    {
        // РЎРѕР·РґР°РµРј СЃРїРёСЃРѕРє СѓРЅРёС‡С‚РѕР¶РµРЅРЅС‹С… РѕР±СЉРµРєС‚РѕРІ РґР»СЏ СѓРґР°Р»РµРЅРёСЏ РёР· СЃР»РѕРІР°СЂСЏ
        List<GameObject> destroyedObjects = new List<GameObject>();

        try
        {
            foreach (var kvp in selectionIndicators)
            {
                GameObject targetObject = kvp.Key;
                GameObject indicator = kvp.Value;

                // РљР РРўРР§Р•РЎРљР Р’РђР–РќРћ: РџСЂРѕРІРµСЂСЏРµРј С‡С‚Рѕ РѕР±СЉРµРєС‚ РЅРµ Р±С‹Р» СѓРЅРёС‡С‚РѕР¶РµРЅ
                // Unity СѓРЅРёС‡С‚РѕР¶РµРЅРЅС‹Рµ РѕР±СЉРµРєС‚С‹ != null, РЅРѕ ReferenceEquals(obj, null) == true
                bool isDestroyed = ReferenceEquals(targetObject, null);

                if (isDestroyed)
                {
                    destroyedObjects.Add(targetObject);

                    // РЈРґР°Р»СЏРµРј РёРЅРґРёРєР°С‚РѕСЂ РµСЃР»Рё РѕРЅ СЃСѓС‰РµСЃС‚РІСѓРµС‚
                    if (indicator != null)
                    {
                        DestroyImmediate(indicator);
                    }
                    continue;
                }

                if (targetObject != null && indicator != null)
                {
                    // РћР±РЅРѕРІР»СЏРµРј РїРѕР·РёС†РёСЋ РёРЅРґРёРєР°С‚РѕСЂР° РЅР°Рґ РґРІРёР¶СѓС‰РёРјСЃСЏ РѕР±СЉРµРєС‚РѕРј
                    Bounds bounds = GetObjectBounds(targetObject);
                    Vector3 newPosition = bounds.center + Vector3.up * (bounds.size.y * 0.5f + selectionIndicatorHeight);
                    indicator.transform.position = newPosition;
                }
                else if (targetObject == null)
                {
                    destroyedObjects.Add(targetObject);
                }
            }
        }
        catch (System.Exception ex)
        {
        }

        // РЈРґР°Р»СЏРµРј СѓРЅРёС‡С‚РѕР¶РµРЅРЅС‹Рµ РѕР±СЉРµРєС‚С‹ РёР· СЃР»РѕРІР°СЂСЏ
        foreach (GameObject destroyedObj in destroyedObjects)
        {
            if (selectionIndicators.ContainsKey(destroyedObj))
            {
                selectionIndicators.Remove(destroyedObj);
            }
        }
    }

    /// <summary>
    /// РџРѕР»СѓС‡РµРЅРёРµ РіСЂР°РЅРёС† РѕР±СЉРµРєС‚Р°
    /// </summary>
    Bounds GetObjectBounds(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
            return renderer.bounds;
            
        Collider collider = obj.GetComponent<Collider>();
        if (collider == null)
        {
            collider = obj.GetComponentInChildren<Collider>();
        }
        if (collider != null)
            return collider.bounds;
            
        return new Bounds(obj.transform.position, Vector3.one);
    }
    
    /// <summary>
    /// РЈСЃС‚Р°РЅРѕРІРєР° СЃРѕСЃС‚РѕСЏРЅРёСЏ РІС‹РґРµР»РµРЅРёСЏ РґР»СЏ РѕР±СЉРµРєС‚Р°
    /// </summary>
    void SetObjectSelectionState(GameObject obj, bool selected)
    {
        // РџСЂРѕРІРµСЂСЏРµРј, С‡С‚Рѕ РѕР±СЉРµРєС‚ РЅРµ Р±С‹Р» СѓРЅРёС‡С‚РѕР¶РµРЅ
        if (obj == null)
        {
            return;
        }

        // РћР±РЅРѕРІР»СЏРµРј СЃРѕСЃС‚РѕСЏРЅРёРµ РїРµСЂСЃРѕРЅР°Р¶Р° РµСЃР»Рё СЌС‚Рѕ РїРµСЂСЃРѕРЅР°Р¶
        Character character = obj.GetComponent<Character>();
        if (character != null)
        {
            character.SetSelected(selected);
            return;
        }
        
        // Р›РѕРіРёСЂРѕРІР°РЅРёРµ РґР»СЏ РѕСЃС‚Р°Р»СЊРЅС‹С… РѕР±СЉРµРєС‚РѕРІ
        LocationObjectInfo objectInfo = obj.GetComponent<LocationObjectInfo>();
        if (objectInfo != null)
        {
        }
    }
    
    /// <summary>
    /// РћР±РЅРѕРІР»РµРЅРёРµ РёРЅС„РѕСЂРјР°С†РёРё Рѕ РІС‹РґРµР»РµРЅРёРё РІ UI - С‚РµРїРµСЂСЊ С‚РѕР»СЊРєРѕ РґР»СЏ GameUI
    /// </summary>
    void UpdateSelectionInfo()
    {
        // РћС‡РёС‰Р°РµРј СЃРїРёСЃРѕРє РѕС‚ СѓРЅРёС‡С‚РѕР¶РµРЅРЅС‹С… РѕР±СЉРµРєС‚РѕРІ
        selectedObjects.RemoveAll(obj => obj == null);

        // РРЅС„РѕСЂРјР°С†РёСЏ Рѕ РІС‹РґРµР»РµРЅРёРё С‚РµРїРµСЂСЊ РѕС‚РѕР±СЂР°Р¶Р°РµС‚СЃСЏ С‚РѕР»СЊРєРѕ РІ РЅРёР¶РЅРµР№ РїР°РЅРµР»Рё GameUI
        // Р—РґРµСЃСЊ Р±РѕР»СЊС€Рµ РЅРёС‡РµРіРѕ РЅРµ РґРµР»Р°РµРј
    }
    
    /// <summary>
    /// РџРѕР»СѓС‡РµРЅРёРµ СЃРїРёСЃРєР° РІС‹РґРµР»РµРЅРЅС‹С… РѕР±СЉРµРєС‚РѕРІ
    /// </summary>
    public List<GameObject> GetSelectedObjects()
    {
        return new List<GameObject>(selectedObjects);
    }
    
    /// <summary>
    /// РџСЂРѕРІРµСЂРєР°, РІС‹РґРµР»РµРЅ Р»Рё РѕР±СЉРµРєС‚
    /// </summary>
    public bool IsSelected(GameObject obj)
    {
        return selectedObjects.Contains(obj);
    }
    
    /// <summary>
    /// Р”РёР°РіРЅРѕСЃС‚РёРєР° РІСЃРµС… РѕР±СЉРµРєС‚РѕРІ СЃ РєРѕР»Р»Р°Р№РґРµСЂР°РјРё РІ СЃС†РµРЅРµ
    /// </summary>
    void DiagnoseSelectableObjects()
    {


        // РџСЂРѕРІРµСЂСЏРµРј РїРµСЂСЃРѕРЅР°Р¶РµР№
        Character[] allCharacters = FindObjectsOfType<Character>();


        foreach (Character character in allCharacters)
        {
            GameObject obj = character.gameObject;
            Collider collider = obj.GetComponent<Collider>();
            if (collider == null)
            {
                collider = obj.GetComponentInChildren<Collider>();
            }
            bool inMask = (selectableLayerMask & (1 << obj.layer)) != 0;

            // Debug logging disabled
        }

        LocationObjectInfo[] allObjects = FindObjectsOfType<LocationObjectInfo>();

        foreach (LocationObjectInfo objectInfo in allObjects)
        {
            GameObject obj = objectInfo.gameObject;

            Collider collider = obj.GetComponent<Collider>();
            if (collider != null)
            {
                // РџСЂРѕРІРµСЂСЏРµРј, РїРѕРїР°РґР°РµС‚ Р»Рё РІ LayerMask
                bool inMask = (selectableLayerMask & (1 << obj.layer)) != 0;
            }
            else
            {
                // Object has no collider
            }
        }

        // РџСЂРѕРІРµСЂСЏРµРј РєРѕРјРЅР°С‚С‹ Р±РµР· LocationObjectInfo Рё РґРѕР±Р°РІР»СЏРµРј РёС…
        CheckAndFixRoomComponents();


    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ Рё РёСЃРїСЂР°РІРёС‚СЊ РєРѕРјРїРѕРЅРµРЅС‚С‹ РєРѕРјРЅР°С‚
    /// </summary>
    void CheckAndFixRoomComponents()
    {
        // РС‰РµРј РІСЃРµ РѕР±СЉРµРєС‚С‹ СЃ RoomInfo
        RoomInfo[] allRooms = FindObjectsOfType<RoomInfo>();

        foreach (RoomInfo roomInfo in allRooms)
        {
            GameObject roomObj = roomInfo.gameObject;

            // РџСЂРѕРІРµСЂСЏРµРј, РµСЃС‚СЊ Р»Рё Сѓ РєРѕРјРЅР°С‚С‹ РїРѕР» СЃ РєРѕРјРїРѕРЅРµРЅС‚Р°РјРё РІС‹РґРµР»РµРЅРёСЏ
            Transform floorTransform = roomObj.transform.Find("Floor");
            Transform selectionFloorTransform = roomObj.transform.Find("SelectionFloor");

            // Р”Р»СЏ Р±РѕР»СЊС€РёС… РєРѕРјРЅР°С‚ - РїСЂРѕРІРµСЂСЏРµРј РѕР±С‹С‡РЅС‹Р№ РїРѕР»
            if (floorTransform != null)
            {
                GameObject floor = floorTransform.gameObject;

                // РџСЂРѕРІРµСЂСЏРµРј РЅР°Р»РёС‡РёРµ LocationObjectInfo РЅР° РїРѕР»Сѓ
                LocationObjectInfo locationInfo = floor.GetComponent<LocationObjectInfo>();
                if (locationInfo == null)
                {
                    // Р”РѕР±Р°РІР»СЏРµРј РЅРµРґРѕСЃС‚Р°СЋС‰РёР№ РєРѕРјРїРѕРЅРµРЅС‚ Рє РїРѕР»Сѓ
                    locationInfo = floor.AddComponent<LocationObjectInfo>();
                    locationInfo.objectName = roomInfo.roomName;
                    locationInfo.objectType = roomInfo.roomType;
                    locationInfo.health = 500f;
                    locationInfo.isDestructible = true;
                }

                // РџСЂРѕРІРµСЂСЏРµРј РЅР°Р»РёС‡РёРµ РєРѕР»Р»Р°Р№РґРµСЂР° РЅР° РїРѕР»Сѓ
                BoxCollider floorCollider = floor.GetComponent<BoxCollider>();
                if (floorCollider == null)
                {
                    floorCollider = floor.AddComponent<BoxCollider>();
                    floorCollider.isTrigger = false; // РќРµ С‚СЂРёРіРіРµСЂ РґР»СЏ raycast
                }
            }
            // Р”Р»СЏ РјР°Р»РµРЅСЊРєРёС… РєРѕРјРЅР°С‚ - РїСЂРѕРІРµСЂСЏРµРј SelectionFloor
            else if (selectionFloorTransform != null)
            {
                GameObject selectionFloor = selectionFloorTransform.gameObject;

                // РџСЂРѕРІРµСЂСЏРµРј РЅР°Р»РёС‡РёРµ LocationObjectInfo
                LocationObjectInfo locationInfo = selectionFloor.GetComponent<LocationObjectInfo>();
                if (locationInfo == null)
                {
                    locationInfo = selectionFloor.AddComponent<LocationObjectInfo>();
                    locationInfo.objectName = roomInfo.roomName;
                    locationInfo.objectType = roomInfo.roomType;
                    locationInfo.health = 500f;
                    locationInfo.isDestructible = true;
                }

                // РџСЂРѕРІРµСЂСЏРµРј РЅР°Р»РёС‡РёРµ РєРѕР»Р»Р°Р№РґРµСЂР°
                BoxCollider selectionCollider = selectionFloor.GetComponent<BoxCollider>();
                if (selectionCollider == null)
                {
                    selectionCollider = selectionFloor.AddComponent<BoxCollider>();
                    selectionCollider.isTrigger = false;
                }
            }
        }
    }

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚РєР° РїРѕРґСЃРІРµС‚РєРё РѕР±СЉРµРєС‚РѕРІ РїСЂРё hover
    /// </summary>
    void HandleHover()
    {
        if (isBoxSelecting) return; // РќРµ РѕР±СЂР°Р±Р°С‚С‹РІР°РµРј hover РІРѕ РІСЂРµРјСЏ РІС‹РґРµР»РµРЅРёСЏ СЂР°РјРєРѕР№

        try
        {
            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, selectableLayerMask, QueryTriggerInteraction.Collide);

            GameObject hoveredObject = null;

            // РС‰РµРј РѕР±СЉРµРєС‚ РґР»СЏ РїРѕРґСЃРІРµС‚РєРё
            foreach (RaycastHit hit in hits)
            {
                GameObject hitObject = hit.collider.gameObject;

                // Р—РђР©РРўРђ: РџСЂРѕРІРµСЂСЏРµРј С‡С‚Рѕ РѕР±СЉРµРєС‚ РЅРµ Р±С‹Р» СѓРЅРёС‡С‚РѕР¶РµРЅ
                if (ReferenceEquals(hitObject, null) || hitObject == null)
                {
                    continue;
                }

                // РСЃРєР»СЋС‡Р°РµРј СЃРёСЃС‚РµРјРЅС‹Рµ РѕР±СЉРµРєС‚С‹
                if (hitObject.name.Contains("Bounds") || hitObject.name.Contains("Grid") ||
                    hitObject.name.Contains("Location") && !hitObject.name.Contains("Test") ||
                    hitObject.name.Contains("Plane"))
                {
                    continue;
                }

                // РС‰РµРј РєРѕСЂРЅРµРІРѕР№ РѕР±СЉРµРєС‚ РґР»СЏ РїРѕРґСЃРІРµС‚РєРё (РїСЂРµС„Р°Р±)
                GameObject rootObject = FindHoverableRoot(hitObject);
                if (rootObject != null && !ReferenceEquals(rootObject, null))
                {
                    // РџСЂРѕРІРµСЂСЏРµРј, СЏРІР»СЏРµС‚СЃСЏ Р»Рё СЌС‚Рѕ РїРѕР»РѕРј РєРѕРјРЅР°С‚С‹
                    RoomInfo roomInfo = rootObject.GetComponentInParent<RoomInfo>();
                    if (roomInfo != null)
                    {
                        // Р•СЃР»Рё СЌС‚Рѕ РїРѕР» РєРѕРјРЅР°С‚С‹, РїРѕРґСЃРІРµС‡РёРІР°РµРј СЂРѕРґРёС‚РµР»СЊСЃРєСѓСЋ РєРѕРјРЅР°С‚Сѓ
                        hoveredObject = roomInfo.gameObject;
                    }
                    else
                    {
                        hoveredObject = rootObject;
                    }
                    break;
                }
            }

            // РџСЂРѕРІРµСЂСЏРµРј С‡С‚Рѕ currentHoveredObject РЅРµ Р±С‹Р» СѓРЅРёС‡С‚РѕР¶РµРЅ
            if (!ReferenceEquals(currentHoveredObject, null) && currentHoveredObject == null)
            {
                currentHoveredObject = null;
            }

            // РћР±РЅРѕРІР»СЏРµРј РїРѕРґСЃРІРµС‚РєСѓ
            if (hoveredObject != currentHoveredObject)
            {
                // РЈР±РёСЂР°РµРј РїРѕРґСЃРІРµС‚РєСѓ СЃ РїСЂРµРґС‹РґСѓС‰РµРіРѕ РѕР±СЉРµРєС‚Р°
                if (currentHoveredObject != null && !ReferenceEquals(currentHoveredObject, null))
                {
                    EndHover(currentHoveredObject);
                }

                // Р”РѕР±Р°РІР»СЏРµРј РїРѕРґСЃРІРµС‚РєСѓ РЅРѕРІРѕРјСѓ РѕР±СЉРµРєС‚Сѓ, С‚РѕР»СЊРєРѕ РµСЃР»Рё РѕРЅ РќР• РІС‹РґРµР»РµРЅ
                if (hoveredObject != null && !ReferenceEquals(hoveredObject, null) && !IsSelected(hoveredObject))
                {
                    StartHover(hoveredObject);
                }

                currentHoveredObject = hoveredObject;
            }
        }
        catch (System.Exception ex)
        {
        }
    }

    /// <summary>
    /// РќР°Р№С‚Рё РєРѕСЂРЅРµРІРѕР№ РѕР±СЉРµРєС‚ РґР»СЏ РїРѕРґСЃРІРµС‚РєРё (РїСЂРµС„Р°Р±)
    /// </summary>
    GameObject FindHoverableRoot(GameObject hitObject)
    {
        // РџСЂРѕРІРµСЂСЏРµРј, РјРѕР¶РµС‚ Р»Рё СЃР°Рј РѕР±СЉРµРєС‚ Р±С‹С‚СЊ РїРѕРґСЃРІРµС‡РµРЅ
        if (CanObjectBeHighlighted(hitObject))
        {
            return hitObject;
        }

        // РС‰РµРј РІ СЂРѕРґРёС‚РµР»СЊСЃРєРёС… РѕР±СЉРµРєС‚Р°С…
        Transform current = hitObject.transform.parent;
        while (current != null)
        {
            if (CanObjectBeHighlighted(current.gameObject))
            {
                return current.gameObject;
            }
            current = current.parent;
        }

        return null;
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ, РјРѕР¶РµС‚ Р»Рё РѕР±СЉРµРєС‚ Р±С‹С‚СЊ РїРѕРґСЃРІРµС‡РµРЅ
    /// </summary>
    bool CanObjectBeHighlighted(GameObject obj)
    {
        // РџСЂРѕРІРµСЂСЏРµРј РЅР°Р»РёС‡РёРµ MeshRenderer РІ РѕР±СЉРµРєС‚Рµ РёР»Рё РµРіРѕ РґРµС‚СЏС…
        MeshRenderer[] renderers = obj.GetComponentsInChildren<MeshRenderer>();
        if (renderers.Length == 0) return false;

        // РџР•Р РЎРћРќРђР–Р РќР• РЈР§РђРЎРўР’РЈР®Рў Р’ HOVER РЎРРЎРўР•РњР• - Сѓ РЅРёС… СЃРІРѕСЏ СЃРёСЃС‚РµРјР° С†РІРµС‚РѕРІ
        if (obj.GetComponent<Character>() != null)
        {
            return false;
        }

        // РЎРїРµС†РёР°Р»СЊРЅР°СЏ Р»РѕРіРёРєР° РґР»СЏ SM_Cockpit - РѕРЅ РґРѕР»Р¶РµРЅ РїРѕРґСЃРІРµС‡РёРІР°С‚СЊСЃСЏ С†РµР»РёРєРѕРј
        if (obj.name == "SM_Cockpit")
        {
            return true;
        }

        // Р”Р»СЏ РґСЂСѓРіРёС… РѕР±СЉРµРєС‚РѕРІ - РїСЂРѕРІРµСЂСЏРµРј РЅР°Р»РёС‡РёРµ LocationObjectInfo
        if (obj.GetComponent<LocationObjectInfo>() != null)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// РќР°С‡Р°С‚СЊ РїРѕРґСЃРІРµС‚РєСѓ РѕР±СЉРµРєС‚Р° Рё РІСЃРµС… РµРіРѕ РґРѕС‡РµСЂРЅРёС… MeshRenderer'РѕРІ
    /// PERFORMANCE FIX: РџРµСЂРµРёСЃРїРѕР»СЊР·СѓРµРј РјР°С‚РµСЂРёР°Р»С‹ РІРјРµСЃС‚Рѕ СЃРѕР·РґР°РЅРёСЏ/СѓРЅРёС‡С‚РѕР¶РµРЅРёСЏ
    /// </summary>
    void StartHover(GameObject obj)
    {
        // РџСЂРѕРІРµСЂСЏРµРј, С‡С‚Рѕ РѕР±СЉРµРєС‚ РЅРµ РІС‹РґРµР»РµРЅ (РґРѕРїРѕР»РЅРёС‚РµР»СЊРЅР°СЏ РїСЂРѕРІРµСЂРєР°)
        if (IsSelected(obj)) return;

        // РџРѕР»СѓС‡Р°РµРј РІСЃРµ MeshRenderer РІ РѕР±СЉРµРєС‚Рµ Рё РµРіРѕ РґРµС‚СЏС…
        MeshRenderer[] renderers = obj.GetComponentsInChildren<MeshRenderer>();

        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer == null) continue;

            // РЎРѕС…СЂР°РЅСЏРµРј РѕСЂРёРіРёРЅР°Р»СЊРЅС‹Р№ РјР°С‚РµСЂРёР°Р» РµСЃР»Рё РµС‰Рµ РЅРµ СЃРѕС…СЂР°РЅРёР»Рё
            if (!originalMaterials.ContainsKey(renderer))
            {
                originalMaterials[renderer] = renderer.material;
            }

            // PERFORMANCE: РЎРѕР·РґР°РµРј hover РјР°С‚РµСЂРёР°Р» С‚РѕР»СЊРєРѕ РѕРґРёРЅ СЂР°Р· Рё РїРµСЂРµРёСЃРїРѕР»СЊР·СѓРµРј
            if (!hoverMaterials.ContainsKey(renderer))
            {
                Material hoverMat = new Material(originalMaterials[renderer]);
                hoverMat.color = hoverColor;
                hoverMaterials[renderer] = hoverMat;
            }

            renderer.material = hoverMaterials[renderer];
            currentHighlightedRenderers.Add(renderer);
        }
    }

    /// <summary>
    /// Р—Р°РІРµСЂС€РёС‚СЊ РїРѕРґСЃРІРµС‚РєСѓ РѕР±СЉРµРєС‚Р° Рё РІСЃРµС… РµРіРѕ РґРѕС‡РµСЂРЅРёС… MeshRenderer'РѕРІ
    /// </summary>
    void EndHover(GameObject obj)
    {
        // Р’РѕСЃСЃС‚Р°РЅР°РІР»РёРІР°РµРј РјР°С‚РµСЂРёР°Р»С‹ РІСЃРµС… РїРѕРґСЃРІРµС‡РµРЅРЅС‹С… СЂРµРЅРґРµСЂРµСЂРѕРІ
        foreach (MeshRenderer renderer in currentHighlightedRenderers)
        {
            if (renderer != null && originalMaterials.ContainsKey(renderer))
            {
                renderer.material = originalMaterials[renderer];
            }
        }

        currentHighlightedRenderers.Clear();
    }

    /// <summary>
    /// РћС‡РёСЃС‚РёС‚СЊ hover РјР°С‚РµСЂРёР°Р»С‹ РґР»СЏ РєРѕРЅРєСЂРµС‚РЅРѕРіРѕ РѕР±СЉРµРєС‚Р°
    /// PERFORMANCE FIX: РќР• СѓРЅРёС‡С‚РѕР¶Р°РµРј РјР°С‚РµСЂРёР°Р»С‹, С‚РѕР»СЊРєРѕ РІРѕСЃСЃС‚Р°РЅР°РІР»РёРІР°РµРј РѕСЂРёРіРёРЅР°Р»СЊРЅС‹Р№
    /// Hover РјР°С‚РµСЂРёР°Р»С‹ РѕСЃС‚Р°СЋС‚СЃСЏ РІ РєСЌС€Рµ РґР»СЏ РїРµСЂРµРёСЃРїРѕР»СЊР·РѕРІР°РЅРёСЏ
    /// </summary>
    void ClearHoverMaterialsForObject(GameObject obj)
    {
        MeshRenderer[] renderers = obj.GetComponentsInChildren<MeshRenderer>();

        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer == null) continue;

            // Р’РѕСЃСЃС‚Р°РЅР°РІР»РёРІР°РµРј РѕСЂРёРіРёРЅР°Р»СЊРЅС‹Р№ РјР°С‚РµСЂРёР°Р»
            if (originalMaterials.ContainsKey(renderer))
            {
                renderer.material = originalMaterials[renderer];
            }

            // PERFORMANCE: РќР• СѓРґР°Р»СЏРµРј Р·Р°РїРёСЃРё РёР· СЃР»РѕРІР°СЂРµР№ - РѕСЃС‚Р°РІР»СЏРµРј РґР»СЏ РїРµСЂРµРёСЃРїРѕР»СЊР·РѕРІР°РЅРёСЏ
            // РњР°С‚РµСЂРёР°Р»С‹ Р±СѓРґСѓС‚ СѓРЅРёС‡С‚РѕР¶РµРЅС‹ С‚РѕР»СЊРєРѕ РІ OnDestroy()

            // РЈР±РёСЂР°РµРј РёР· СЃРїРёСЃРєР° РїРѕРґСЃРІРµС‡РµРЅРЅС‹С…
            currentHighlightedRenderers.Remove(renderer);
        }
    }


    /// <summary>
    /// РћР±СЂР°Р±РѕС‚РєР° РџРљРњ - РІР·Р°РёРјРѕРґРµР№СЃС‚РІРёРµ СЃ РїСЂРµРґРјРµС‚Р°РјРё Рё РѕР±СЉРµРєС‚Р°РјРё
    /// </summary>
    void HandleRightClick()
    {
        // РџСЂРѕРІРµСЂСЏРµРј, РµСЃС‚СЊ Р»Рё РІС‹РґРµР»РµРЅРЅС‹Рµ РїРµСЂСЃРѕРЅР°Р¶Рё
        if (selectedObjects.Count == 0)
        {
            return;
        }

        // РџРѕР»СѓС‡Р°РµРј Р’РЎР•РҐ РІС‹РґРµР»РµРЅРЅС‹С… РїРµСЂСЃРѕРЅР°Р¶РµР№ РёРіСЂРѕРєР°
        List<Character> selectedCharacters = new List<Character>();
        foreach (GameObject obj in selectedObjects)
        {
            Character character = obj.GetComponent<Character>();
            if (character != null && character.IsPlayerCharacter())
            {
                selectedCharacters.Add(character);
            }
        }

        if (selectedCharacters.Count == 0)
        {
            return;
        }

        // РџРµСЂРІС‹Р№ РїРµСЂСЃРѕРЅР°Р¶ (РґР»СЏ СЃРѕРІРјРµСЃС‚РёРјРѕСЃС‚Рё СЃРѕ СЃС‚Р°СЂС‹Рј РєРѕРґРѕРј РїРѕРґР±РѕСЂР° РїСЂРµРґРјРµС‚РѕРІ)
        Character selectedCharacter = selectedCharacters[0];

        // Raycast РґР»СЏ РїРѕРёСЃРєР° РѕР±СЉРµРєС‚Р° РїРѕРґ РєСѓСЂСЃРѕСЂРѕРј
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, selectableLayerMask, QueryTriggerInteraction.Collide);

        foreach (RaycastHit hit in hits)
        {
            GameObject hitObject = hit.collider.gameObject;

            // РџСЂРѕРІРµСЂСЏРµРј, СЏРІР»СЏРµС‚СЃСЏ Р»Рё СЌС‚Рѕ Р°СЃС‚РµСЂРѕРёРґРѕРј РґР»СЏ РґРѕР±С‹С‡Рё
            LocationObjectInfo locationInfo = hitObject.GetComponent<LocationObjectInfo>();
            if (locationInfo != null && locationInfo.IsOfType("Asteroid"))
            {
                if (locationInfo.metalAmount > 0)
                {
                    // ARCHITECTURE: РСЃРїРѕР»СЊР·СѓРµРј РєСЌС€РёСЂРѕРІР°РЅРЅСѓСЋ СЃСЃС‹Р»РєСѓ РёР»Рё СЃРѕР·РґР°РµРј РЅРѕРІС‹Р№ MiningManager
                    if (miningManager == null)
                    {
                        // РџС‹С‚Р°РµРјСЃСЏ РїРѕР»СѓС‡РёС‚СЊ С‡РµСЂРµР· ServiceLocator
                        if (!ServiceLocator.TryGet<MiningManager>(out miningManager))
                        {
                            // РЎРѕР·РґР°РµРј РЅРѕРІС‹Р№ РµСЃР»Рё РЅРµ РЅР°Р№РґРµРЅ
                            GameObject miningManagerObj = new GameObject("MiningManager");
                            miningManager = miningManagerObj.AddComponent<MiningManager>();

                            // Р РµРіРёСЃС‚СЂРёСЂСѓРµРј РІ ServiceLocator РґР»СЏ Р±СѓРґСѓС‰РµРіРѕ РёСЃРїРѕР»СЊР·РѕРІР°РЅРёСЏ
                            if (ServiceLocator.IsInitialized)
                            {
                                ServiceLocator.Register<MiningManager>(miningManager);
                            }
                        }
                    }

                    // РќР°С‡РёРЅР°РµРј РґРѕР±С‹С‡Сѓ РґР»СЏ Р’РЎР•РҐ РІС‹РґРµР»РµРЅРЅС‹С… РїРµСЂСЃРѕРЅР°Р¶РµР№
                    foreach (Character character in selectedCharacters)
                    {
                        miningManager.StartMining(character, hitObject);
                    }

                    // Р’РђР–РќРћ: РЈСЃС‚Р°РЅР°РІР»РёРІР°РµРј С„Р»Р°Рі С‡С‚РѕР±С‹ РґСЂСѓРіРёРµ СЃРёСЃС‚РµРјС‹ РЅРµ РѕР±СЂР°Р±Р°С‚С‹РІР°Р»Рё СЌС‚РѕС‚ РєР»РёРє
                    rightClickHandledThisFrame = true;
                    return;
                }
                else
                {
                    return;
                }
            }

            // РџСЂРѕРІРµСЂСЏРµРј, СЏРІР»СЏРµС‚СЃСЏ Р»Рё СЌС‚Рѕ РїСЂРµРґРјРµС‚РѕРј
            Item item = hitObject.GetComponent<Item>();
            if (item != null && !ReferenceEquals(item, null))
            {
                // РџСЂРѕРІРµСЂСЏРµРј, РІРєР»СЋС‡РµРЅ Р»Рё РїРѕРґР±РѕСЂ РїРѕ РџРљРњ
                if (!enableRightClickPickup)
                {
                    continue; // РџСЂРѕРїСѓСЃРєР°РµРј СЌС‚РѕС‚ РїСЂРµРґРјРµС‚ Рё РїСЂРѕРґРѕР»Р¶Р°РµРј РїРѕРёСЃРє РґСЂСѓРіРёС… РѕР±СЉРµРєС‚РѕРІ
                }

                if (item.canBePickedUp)
                {
                    // РћС‚РїСЂР°РІР»СЏРµРј РїРµСЂСЃРѕРЅР°Р¶Р° Рє РїСЂРµРґРјРµС‚Сѓ
                    CharacterMovement movement = selectedCharacter.GetComponent<CharacterMovement>();
                    if (movement != null)
                    {
                        Vector3 itemPosition = item.transform.position;
                        float distance = Vector3.Distance(selectedCharacter.transform.position, itemPosition);

                        // Р•СЃР»Рё РїРµСЂСЃРѕРЅР°Р¶ СѓР¶Рµ СЂСЏРґРѕРј - РїРѕРґР±РёСЂР°РµРј СЃСЂР°Р·Сѓ
                        if (distance <= item.pickupRange)
                        {
                            PickupItem(selectedCharacter, item);
                        }
                        else
                        {
                            // РћС‚РїСЂР°РІР»СЏРµРј РїРµСЂСЃРѕРЅР°Р¶Р° Рє РїСЂРµРґРјРµС‚Сѓ
                            movement.MoveTo(itemPosition);

                            // Р—Р°РїСѓСЃРєР°РµРј РєРѕСЂСѓС‚РёРЅСѓ РґР»СЏ РїРѕРґР±РѕСЂР° РїСЂРµРґРјРµС‚Р° РїРѕСЃР»Рµ РґРІРёР¶РµРЅРёСЏ
                            StartCoroutine(WaitForMovementAndPickup(movement, selectedCharacter, item));
                        }
                    }
                    else
                    {
                    }

                    // Р’РђР–РќРћ: РЈСЃС‚Р°РЅР°РІР»РёРІР°РµРј С„Р»Р°Рі С‡С‚РѕР±С‹ РґСЂСѓРіРёРµ СЃРёСЃС‚РµРјС‹ РЅРµ РѕР±СЂР°Р±Р°С‚С‹РІР°Р»Рё СЌС‚РѕС‚ РєР»РёРє
                    rightClickHandledThisFrame = true;
                    return;
                }
            }
        }

        // ============================================================================
        // РљРћРњРђРќР”Рђ Р”Р’РР–Р•РќРРЇ: Р•СЃР»Рё РЅРёС‡РµРіРѕ РёРЅС‚РµСЂРµСЃРЅРѕРіРѕ РїРѕРґ РєСѓСЂСЃРѕСЂРѕРј РЅРµС‚ - РѕС‚РїСЂР°РІР»СЏРµРј РєРѕРјР°РЅРґСѓ РґРІРёР¶РµРЅРёСЏ
        // ============================================================================
        if (gridManager != null)
        {
            // Р”РµР»Р°РµРј raycast РІ РїР»РѕСЃРєРѕСЃС‚СЊ Y=0 (СѓСЂРѕРІРµРЅСЊ Р·РµРјР»Рё)
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            Ray groundRay = playerCamera.ScreenPointToRay(Input.mousePosition);

            float rayDistance;
            if (groundPlane.Raycast(groundRay, out rayDistance))
            {
                Vector3 targetWorldPosition = groundRay.GetPoint(rayDistance);
                Vector2Int targetGridPosition = gridManager.WorldToGrid(targetWorldPosition);

                // РџСЂРѕРІРµСЂСЏРµРј РІР°Р»РёРґРЅРѕСЃС‚СЊ РїРѕР·РёС†РёРё
                if (gridManager.IsValidGridPosition(targetGridPosition))
                {
                    // Р’РђР–РќРћ: РћС‚РїСЂР°РІР»СЏРµРј РєРѕРјР°РЅРґСѓ РґРІРёР¶РµРЅРёСЏ Р’РЎР•Рњ РІС‹РґРµР»РµРЅРЅС‹Рј РїРµСЂСЃРѕРЅР°Р¶Р°Рј
                    // FIX: Track used positions for group movement
                    List<Vector2Int> usedPositions = new List<Vector2Int>();

                    for (int i = 0; i < selectedCharacters.Count; i++)
                    {
                        Character character = selectedCharacters[i];
                        CharacterMovement movement = character.GetComponent<CharacterMovement>();
                        CharacterAI characterAI = character.GetComponent<CharacterAI>();

                        if (movement != null && characterAI != null)
                        {
                            characterAI.OnPlayerInitiatedMovement();

                            // FIX: First character goes to target, others find free positions nearby
                            Vector2Int assignedGridPos = (i == 0) ? targetGridPosition : FindNearbyFreePosition(targetGridPosition, usedPositions);
                            usedPositions.Add(assignedGridPos);

                            Vector3 assignedWorldPos = gridManager.GridToWorld(assignedGridPos);
                            movement.MoveTo(assignedWorldPos);
                        }
                    }

                    // РЈСЃС‚Р°РЅР°РІР»РёРІР°РµРј С„Р»Р°Рі С‡С‚Рѕ РєР»РёРє РѕР±СЂР°Р±РѕС‚Р°РЅ
                    rightClickHandledThisFrame = true;
                }
            }
        }
    }

    /// <summary>
    /// РљРѕСЂСѓС‚РёРЅР° РѕР¶РёРґР°РЅРёСЏ Р·Р°РІРµСЂС€РµРЅРёСЏ РґРІРёР¶РµРЅРёСЏ Рё РїРѕРґР±РѕСЂР° РїСЂРµРґРјРµС‚Р°
    /// </summary>
    System.Collections.IEnumerator WaitForMovementAndPickup(CharacterMovement movement, Character character, Item item)
    {
        // Р”Р°РµРј РїРµСЂСЃРѕРЅР°Р¶Сѓ РІСЂРµРјСЏ РЅР°С‡Р°С‚СЊ РґРІРёР¶РµРЅРёРµ (CharacterMovement РёСЃРїРѕР»СЊР·СѓРµС‚ StartMovementAfterDelay)
        yield return null;
        yield return null;

        // Р—РђР©РРўРђ: РџСЂРѕРІРµСЂСЏРµРј С‡С‚Рѕ item РЅРµ Р±С‹Р» СѓРЅРёС‡С‚РѕР¶РµРЅ
        if (ReferenceEquals(item, null) || item == null || item.gameObject == null)
        {
            yield break;
        }

        // Р–РґРµРј РїРѕРєР° РїРµСЂСЃРѕРЅР°Р¶ РґРІРёР¶РµС‚СЃСЏ
        while (movement != null && movement.IsMoving())
        {
            // Р—РђР©РРўРђ: РџСЂРѕРІРµСЂСЏРµРј РєР°Р¶РґС‹Р№ РєР°РґСЂ С‡С‚Рѕ item РµС‰Рµ СЃСѓС‰РµСЃС‚РІСѓРµС‚
            if (ReferenceEquals(item, null) || item == null || item.gameObject == null)
            {
                yield break;
            }
            yield return null;
        }

        // РџСЂРѕРІРµСЂСЏРµРј С‡С‚Рѕ РїРµСЂСЃРѕРЅР°Р¶ Рё РїСЂРµРґРјРµС‚ РІСЃРµ РµС‰Рµ СЃСѓС‰РµСЃС‚РІСѓСЋС‚
        if (character != null && !ReferenceEquals(item, null) && item != null && item.gameObject != null)
        {
            // РџСЂРѕРІРµСЂСЏРµРј РґРёСЃС‚Р°РЅС†РёСЋ РїРѕСЃР»Рµ РґРІРёР¶РµРЅРёСЏ
            float finalDistance = Vector3.Distance(character.transform.position, item.transform.position);

            if (finalDistance <= item.pickupRange)
            {
                PickupItem(character, item);
                // Р’РђР–РќРћ: РћСЃС‚Р°РЅР°РІР»РёРІР°РµРј РєРѕСЂСѓС‚РёРЅСѓ РїРѕСЃР»Рµ РїРѕРґР±РѕСЂР°, С‚.Рє. РїСЂРµРґРјРµС‚ Р±СѓРґРµС‚ СѓРЅРёС‡С‚РѕР¶РµРЅ
                yield break;
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
    /// РџРѕРґРѕР±СЂР°С‚СЊ РїСЂРµРґРјРµС‚ РїРµСЂСЃРѕРЅР°Р¶РµРј
    /// </summary>
    void PickupItem(Character character, Item item)
    {
        if (character == null || item == null)
            return;

        // Р’РђР–РќРћ: РЎРЅРёРјР°РµРј РІС‹РґРµР»РµРЅРёРµ СЃ РїСЂРµРґРјРµС‚Р° РџР•Р Р•Р” РїРѕРґР±РѕСЂРѕРј
        // С‡С‚РѕР±С‹ UpdateSelectionIndicatorPositions() РЅРµ РїС‹С‚Р°Р»СЃСЏ РѕР±СЂР°С‚РёС‚СЊСЃСЏ Рє СѓРЅРёС‡С‚РѕР¶РµРЅРЅРѕРјСѓ РѕР±СЉРµРєС‚Сѓ
        if (IsSelected(item.gameObject))
        {
            RemoveFromSelection(item.gameObject);
        }

        // ARCHITECTURE: РћСЃРІРѕР±РѕР¶РґР°РµРј РєР»РµС‚РєСѓ РІ GridManager РёСЃРїРѕР»СЊР·СѓСЏ РєСЌС€РёСЂРѕРІР°РЅРЅСѓСЋ СЃСЃС‹Р»РєСѓ
        if (gridManager != null)
        {
            Vector2Int gridPos = gridManager.WorldToGrid(item.transform.position);
            gridManager.FreeCell(gridPos);
        }
        else
        {
        }

        // РџРѕРґР±РёСЂР°РµРј РїСЂРµРґРјРµС‚ (Р’РђР–РќРћ: РїРѕСЃР»Рµ СЌС‚РѕРіРѕ item Р±СѓРґРµС‚ СѓРЅРёС‡С‚РѕР¶РµРЅ!)
        item.PickUp(character);
    }

    void OnDestroy()
    {
        try
        {
            // Р‘РµР·РѕРїР°СЃРЅР°СЏ РѕС‡РёСЃС‚РєР° РІС‹РґРµР»РµРЅРёСЏ РїСЂРё СѓРЅРёС‡С‚РѕР¶РµРЅРёРё (Р±РµР· СѓРІРµРґРѕРјР»РµРЅРёСЏ UI)
            if (selectedObjects != null)
            {
                foreach (var obj in selectedObjects)
                {
                    if (obj != null)
                    {
                        RemoveSelectionIndicator(obj);
                    }
                }
                selectedObjects.Clear();
            }

            // РћС‡РёСЃС‚РєР° hover СЃРѕСЃС‚РѕСЏРЅРёСЏ
            if (currentHoveredObject != null)
            {
                EndHover(currentHoveredObject);
            }

            // РЈРЅРёС‡С‚РѕР¶РµРЅРёРµ СЃРѕР·РґР°РЅРЅС‹С… hover РјР°С‚РµСЂРёР°Р»РѕРІ
            foreach (var material in hoverMaterials.Values)
            {
                if (material != null)
                {
                    DestroyImmediate(material);
                }
            }
        }
        catch (System.Exception)
        {
            // Error handled silently during cleanup
        }

        // РџСЂРёРЅСѓРґРёС‚РµР»СЊРЅР°СЏ РѕС‡РёСЃС‚РєР° СЃР»РѕРІР°СЂРµР№
        if (selectionIndicators != null)
        {
            selectionIndicators.Clear();
        }

        if (originalMaterials != null)
        {
            originalMaterials.Clear();
        }

        if (hoverMaterials != null)
        {
            hoverMaterials.Clear();
        }

        if (currentHighlightedRenderers != null)
        {
            currentHighlightedRenderers.Clear();
        }

        // РћС‡РёСЃС‚РєР° СЃРїРёСЃРєР° РІС‹РґРµР»РµРЅРЅС‹С… РѕР±СЉРµРєС‚РѕРІ
        if (selectedObjects != null)
        {
            selectedObjects.Clear();
        }
    }

    /// <summary>
    /// FIX: Find nearby free position for group movement
    /// </summary>
    Vector2Int FindNearbyFreePosition(Vector2Int targetPos, List<Vector2Int> usedPositions)
    {
        // Check target cell first
        if (!usedPositions.Contains(targetPos))
        {
            var cell = gridManager.GetCell(targetPos);
            if (cell == null || !cell.isOccupied)
            {
                return targetPos;
            }
        }

        // Search for free cells in radius
        for (int radius = 1; radius <= 5; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    // Check only cells on current radius border
                    if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        continue;

                    Vector2Int checkPos = new Vector2Int(targetPos.x + x, targetPos.y + y);

                    if (usedPositions.Contains(checkPos))
                        continue;

                    if (gridManager.IsValidGridPosition(checkPos))
                    {
                        var checkCell = gridManager.GetCell(checkPos);
                        if (checkCell == null || !checkCell.isOccupied)
                        {
                            return checkPos;
                        }
                    }
                }
            }
        }

        // If no free cell found, return initial target
        return targetPos;
    }
}
