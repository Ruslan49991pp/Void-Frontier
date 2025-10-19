using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// РЎРёСЃС‚РµРјР° drag-and-drop РґР»СЏ СЂРёСЃРѕРІР°РЅРёСЏ РєРѕРјРЅР°С‚
/// 1. Р—Р°Р¶Р°С‚СЊ Р›РљРњ Рё С‚СЏРЅСѓС‚СЊ - СЂРёСЃРѕРІР°С‚СЊ СЂР°РјРєСѓ РёР· Build_Ghost
/// 2. РћС‚РїСѓСЃС‚РёС‚СЊ Р›РљРњ - Р·Р°С„РёРєСЃРёСЂРѕРІР°С‚СЊ СЃРёР»СѓСЌС‚
/// 3. AddBuild - РїРѕРґС‚РІРµСЂРґРёС‚СЊ (Build_Ghost в†’ Add_Build_Ghost)
/// 4. РџСЂРё РІС‹С…РѕРґРµ РёР· СЂРµР¶РёРјР° СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР° - Add_Build_Ghost в†’ SM_Wall/SM_Wall_L
/// </summary>
public class RoomDragBuilder : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject buildGhostPrefab;      // Build_Ghost РїСЂРµС„Р°Р± (СЃС‚РµРЅС‹)
    public GameObject addBuildGhostPrefab;  // Add_Build_Ghost РїСЂРµС„Р°Р± (СЃС‚РµРЅС‹)
    public GameObject floorGhostPrefab;      // Floor_Ghost РїСЂРµС„Р°Р± (РїРѕР»)
    public GameObject addFloorGhostPrefab;  // Add_Floor_Ghost РїСЂРµС„Р°Р± (РїРѕР»)
    public GameObject delBuildGhostPrefab;   // Del_Build_Ghost РїСЂРµС„Р°Р± (СѓРґР°Р»РµРЅРёРµ)

    [Header("References")]
    public GridManager gridManager;
    public Camera playerCamera;

    // РЎРѕСЃС‚РѕСЏРЅРёСЏ
    private enum DragState
    {
        Idle,           // РћР¶РёРґР°РЅРёРµ РЅР°С‡Р°Р»Р° drag
        Dragging,       // РџСЂРѕС†РµСЃСЃ drag
        PreviewReady,   // РЎРёР»СѓСЌС‚ РіРѕС‚РѕРІ, РѕР¶РёРґР°РЅРёРµ РїРѕРґС‚РІРµСЂР¶РґРµРЅРёСЏ
        Confirmed       // РџРѕРґС‚РІРµСЂР¶РґРµРЅРѕ, РѕР¶РёРґР°РЅРёРµ РІС‹С…РѕРґР° РёР· СЂРµР¶РёРјР° СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
    }

    private DragState currentState = DragState.Idle;

    // Drag РґР°РЅРЅС‹Рµ
    private Vector2Int dragStartGridPos;
    private Vector2Int dragEndGridPos;
    private List<GameObject> ghostBlocks = new List<GameObject>();      // Build_Ghost Р±Р»РѕРєРё (СЃС‚РµРЅС‹)
    private List<GameObject> ghostFloorBlocks = new List<GameObject>(); // Floor_Ghost Р±Р»РѕРєРё (РїРѕР»)
    private List<GameObject> confirmedBlocks = new List<GameObject>(); // Add_Build_Ghost Р±Р»РѕРєРё (СЃС‚РµРЅС‹)
    private List<GameObject> confirmedFloorBlocks = new List<GameObject>(); // Add_Floor_Ghost Р±Р»РѕРєРё (РїРѕР»)
    private List<Vector2Int> roomPerimeter = new List<Vector2Int>();  // РџРѕР·РёС†РёРё РїРµСЂРёРјРµС‚СЂР°
    private List<Vector2Int> roomFloor = new List<Vector2Int>();      // РџРѕР·РёС†РёРё РїРѕР»Р°

    // РљСЌС€ РґР»СЏ РёРЅРєСЂРµРјРµРЅС‚Р°Р»СЊРЅРѕРіРѕ РѕР±РЅРѕРІР»РµРЅРёСЏ
    private HashSet<Vector2Int> cachedPerimeter = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> cachedFloor = new HashSet<Vector2Int>();
    private Dictionary<Vector2Int, GameObject> activeGhostBlocks = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, GameObject> activeFloorBlocks = new Dictionary<Vector2Int, GameObject>();

    // Р РµР¶РёРј Р°РєС‚РёРІРµРЅ
    private bool isDragModeActive = false;

    // Р РµР¶РёРј РґРѕР±Р°РІР»РµРЅРёСЏ РєР»РµС‚РѕРє Рє СЃСѓС‰РµСЃС‚РІСѓСЋС‰РµРјСѓ СЃРёР»СѓСЌС‚Сѓ
    private bool isAddingMoreCells = false;
    private List<Vector2Int> existingPerimeter = new List<Vector2Int>(); // РЎРѕС…СЂР°РЅРµРЅРЅС‹Р№ РїРµСЂРёРјРµС‚СЂ РґРѕ РЅР°С‡Р°Р»Р° РґРѕР±Р°РІР»РµРЅРёСЏ
    private List<Vector2Int> existingFloor = new List<Vector2Int>();     // РЎРѕС…СЂР°РЅРµРЅРЅС‹Р№ РїРѕР» РґРѕ РЅР°С‡Р°Р»Р° РґРѕР±Р°РІР»РµРЅРёСЏ

    // Р РµР¶РёРј СѓРґР°Р»РµРЅРёСЏ
    private bool isDeletionModeActive = false;
    private List<Vector2Int> deletedCells = new List<Vector2Int>();   // РЈРґР°Р»РµРЅРЅС‹Рµ РєР»РµС‚РєРё
    private List<GameObject> delGhostBlocks = new List<GameObject>(); // Del_Build_Ghost Р±Р»РѕРєРё
    private GameObject delPreviewBlock = null;                         // Preview Р±Р»РѕРє РїСЂРё РЅР°РІРµРґРµРЅРёРё (deletion)
    private Vector2Int lastDelPreviewPos = Vector2Int.zero;           // РџРѕСЃР»РµРґРЅСЏСЏ РїРѕР·РёС†РёСЏ preview (deletion)

    // Cursor preview РґР»СЏ build mode
    private GameObject buildCursorPreview = null;                      // Preview Р±Р»РѕРє РїРѕРґ РєСѓСЂСЃРѕСЂРѕРј (build)
    private Vector2Int lastBuildPreviewPos = Vector2Int.zero;         // РџРѕСЃР»РµРґРЅСЏСЏ РїРѕР·РёС†РёСЏ preview (build)

    // РЎРѕС…СЂР°РЅРµРЅРЅС‹Рµ РІРЅСѓС‚СЂРµРЅРЅРёРµ СѓРіР»С‹
    private List<Vector2Int> savedInnerCorners = new List<Vector2Int>(); // Р’РЅСѓС‚СЂРµРЅРЅРёРµ СѓРіР»С‹ РЅР°Р№РґРµРЅРЅС‹Рµ РІ RecalculatePerimeter

    // Drag СѓРґР°Р»РµРЅРёРµ
    private bool isDeletionDragActive = false;                        // РђРєС‚РёРІРµРЅ Р»Рё drag СЂРµР¶РёРј СѓРґР°Р»РµРЅРёСЏ
    private Vector2Int deletionDragStart = Vector2Int.zero;           // РќР°С‡Р°Р»СЊРЅР°СЏ РїРѕР·РёС†РёСЏ drag
    private Vector2Int deletionDragEnd = Vector2Int.zero;             // РљРѕРЅРµС‡РЅР°СЏ РїРѕР·РёС†РёСЏ drag
    private List<GameObject> delDragPreviewBlocks = new List<GameObject>(); // Preview Р±Р»РѕРєРё РґР»СЏ drag

    // РљСЌС€ РґР»СЏ drag СѓРґР°Р»РµРЅРёСЏ
    private HashSet<Vector2Int> cachedDelDragPositions = new HashSet<Vector2Int>();
    private Dictionary<Vector2Int, GameObject> activeDelDragBlocks = new Dictionary<Vector2Int, GameObject>();

    // Р¤Р»Р°Рі РґРµС‚Р°Р»СЊРЅРѕРіРѕ Р»РѕРіРёСЂРѕРІР°РЅРёСЏ (Р’РљР›Р®Р§Р•Рќ РґР»СЏ РѕС‚Р»Р°РґРєРё)
    private const bool DEBUG_LOGGING = true;

    void Start()
    {
        // Р—Р°РіСЂСѓР¶Р°РµРј РїСЂРµС„Р°Р±С‹ РµСЃР»Рё РЅРµ РЅР°Р·РЅР°С‡РµРЅС‹
        if (buildGhostPrefab == null)
        {
            buildGhostPrefab = Resources.Load<GameObject>("Prefabs/Build_Ghost");
            if (buildGhostPrefab == null)
            {
            }
        }

        if (addBuildGhostPrefab == null)
        {
            addBuildGhostPrefab = Resources.Load<GameObject>("Prefabs/Add_Build_Ghost");
            if (addBuildGhostPrefab == null)
            {
            }
        }

        if (floorGhostPrefab == null)
        {
            floorGhostPrefab = Resources.Load<GameObject>("Prefabs/Floor_Ghost");
            if (floorGhostPrefab == null)
            {
            }
        }

        if (addFloorGhostPrefab == null)
        {
            addFloorGhostPrefab = Resources.Load<GameObject>("Prefabs/Add_Floor_Ghost");
            if (addFloorGhostPrefab == null)
            {
            }
        }

        if (delBuildGhostPrefab == null)
        {
            delBuildGhostPrefab = Resources.Load<GameObject>("Prefabs/Del_Build_Ghost");
            if (delBuildGhostPrefab == null)
            {
            }
        }

        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    void Update()
    {
        // Р’РђР–РќРћ: Update СЂР°Р±РѕС‚Р°РµС‚ РµСЃР»Рё Р°РєС‚РёРІРµРЅ drag СЂРµР¶РёРј РР›Р deletion СЂРµР¶РёРј
        if (!isDragModeActive && !isDeletionModeActive) return;

        // РџСЂРѕРІРµСЂСЏРµРј, РЅРµ РЅР°Рґ UI Р»Рё РјС‹С€СЊ
        bool isPointerOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

        if (isPointerOverUI) return;

        // Р’РђР–РќРћ: РџСЂРѕРІРµСЂСЏРµРј СЂРµР¶РёРј deletion РџР•Р Р•Р” РѕР±СЂР°Р±РѕС‚РєРѕР№ СЃРѕСЃС‚РѕСЏРЅРёР№
        // Р•СЃР»Рё deletion mode Р°РєС‚РёРІРµРЅ - РѕР±СЂР°Р±Р°С‚С‹РІР°РµРј РўРћР›Р¬РљРћ deletion, РёРіРЅРѕСЂРёСЂСѓРµРј build Р»РѕРіРёРєСѓ
        if (isDeletionModeActive)
        {
            HandleDeletionMode();
        }
        else
        {
            // Build СЂРµР¶РёРј - РѕР±СЂР°Р±Р°С‚С‹РІР°РµРј СЃРѕСЃС‚РѕСЏРЅРёСЏ РѕР±С‹С‡РЅС‹Рј РѕР±СЂР°Р·РѕРј
            switch (currentState)
            {
                case DragState.Idle:
                    HandleIdleState();
                    break;

                case DragState.Dragging:
                    HandleDraggingState();
                    break;

                case DragState.PreviewReady:
                    // Р’ СЌС‚РѕРј СЃРѕСЃС‚РѕСЏРЅРёРё РјРѕР¶РЅРѕ РЅР°С‡Р°С‚СЊ РЅРѕРІС‹Р№ drag (РґР»СЏ РґРѕР±Р°РІР»РµРЅРёСЏ РєР»РµС‚РѕРє)
                    // РћР±СЂР°Р±Р°С‚С‹РІР°РµРј РєР»РёРєРё С‚Р°Рє Р¶Рµ РєР°Рє РІ Idle СЃРѕСЃС‚РѕСЏРЅРёРё
                    if (isDragModeActive)
                    {
                        // РљР РРўРР§РќРћ: РџСЂРё РєР»РёРєРµ РІ PreviewReady РЅСѓР¶РЅРѕ Р°РєС‚РёРІРёСЂРѕРІР°С‚СЊ СЂРµР¶РёРј РґРѕР±Р°РІР»РµРЅРёСЏ РєР»РµС‚РѕРє!
                        if (Input.GetMouseButtonDown(0) && !isAddingMoreCells)
                        {
                            // РђРєС‚РёРІРёСЂСѓРµРј СЂРµР¶РёРј РґРѕР±Р°РІР»РµРЅРёСЏ Рё СЃРѕС…СЂР°РЅСЏРµРј СЃСѓС‰РµСЃС‚РІСѓСЋС‰РёРµ РєР»РµС‚РєРё
                            isAddingMoreCells = true;
                            existingPerimeter.Clear();
                            existingPerimeter.AddRange(roomPerimeter);
                            existingFloor.Clear();
                            existingFloor.AddRange(roomFloor);

                            if (DEBUG_LOGGING)
                            {
                            }
                        }

                        HandleIdleState();
                    }
                    break;

                case DragState.Confirmed:
                    // Р’ СЌС‚РѕРј СЃРѕСЃС‚РѕСЏРЅРёРё С‚Р°РєР¶Рµ РјРѕР¶РЅРѕ РЅР°С‡Р°С‚СЊ РЅРѕРІС‹Р№ drag (РґР»СЏ РґРѕР±Р°РІР»РµРЅРёСЏ РєР»РµС‚РѕРє)
                    if (isDragModeActive)
                    {
                        // РљР РРўРР§РќРћ: РџСЂРё РєР»РёРєРµ РІ Confirmed РЅСѓР¶РЅРѕ Р°РєС‚РёРІРёСЂРѕРІР°С‚СЊ СЂРµР¶РёРј РґРѕР±Р°РІР»РµРЅРёСЏ РєР»РµС‚РѕРє!
                        if (Input.GetMouseButtonDown(0) && !isAddingMoreCells)
                        {
                            // РђРєС‚РёРІРёСЂСѓРµРј СЂРµР¶РёРј РґРѕР±Р°РІР»РµРЅРёСЏ Рё СЃРѕС…СЂР°РЅСЏРµРј СЃСѓС‰РµСЃС‚РІСѓСЋС‰РёРµ РєР»РµС‚РєРё
                            isAddingMoreCells = true;
                            existingPerimeter.Clear();
                            existingPerimeter.AddRange(roomPerimeter);
                            existingFloor.Clear();
                            existingFloor.AddRange(roomFloor);

                            if (DEBUG_LOGGING)
                            {
                            }

                            // РњРµРЅСЏРµРј СЃРѕСЃС‚РѕСЏРЅРёРµ РЅР° Idle С‡С‚РѕР±С‹ РЅР°С‡Р°С‚СЊ РЅРѕРІС‹Р№ drag
                            currentState = DragState.Idle;
                        }

                        HandleIdleState();
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// РђРєС‚РёРІРёСЂРѕРІР°С‚СЊ СЂРµР¶РёРј drag СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
    /// РџРѕСЃР»Рµ Р°РєС‚РёРІР°С†РёРё РјРѕР¶РЅРѕ СЂРёСЃРѕРІР°С‚СЊ РќР•РћР“Р РђРќРР§Р•РќРќРћР• РєРѕР»РёС‡РµСЃС‚РІРѕ РїСЂСЏРјРѕСѓРіРѕР»СЊРЅРёРєРѕРІ РїРѕРґСЂСЏРґ Р±РµР· РїРѕРІС‚РѕСЂРЅРѕРіРѕ РЅР°Р¶Р°С‚РёСЏ РєРЅРѕРїРєРё
    /// РљР°Р¶РґС‹Р№ РЅРѕРІС‹Р№ РїСЂСЏРјРѕСѓРіРѕР»СЊРЅРёРє РґРѕР±Р°РІР»СЏРµС‚СЃСЏ Рє СЃСѓС‰РµСЃС‚РІСѓСЋС‰РµРјСѓ СЃРёР»СѓСЌС‚Сѓ
    /// </summary>
    public void ActivateDragMode()
    {
        if (DEBUG_LOGGING)
        {
        }

        isDragModeActive = true;

        // Р—Р°РїРѕРјРёРЅР°РµРј, Р±С‹Р» Р»Рё Р°РєС‚РёРІРµРЅ deletion mode Р”Рћ РґРµР°РєС‚РёРІР°С†РёРё
        bool wasInDeletionMode = isDeletionModeActive;

        // Р”РµР°РєС‚РёРІРёСЂСѓРµРј deletion СЂРµР¶РёРј РµСЃР»Рё РѕРЅ Р±С‹Р» Р°РєС‚РёРІРµРЅ
        if (isDeletionModeActive)
        {
            if (DEBUG_LOGGING)
            {
            }
            DeactivateDeletionMode();
        }

        // РџСЂРѕРІРµСЂСЏРµРј, РµСЃС‚СЊ Р»Рё СѓР¶Рµ РїРѕСЃС‚СЂРѕРµРЅРЅС‹Р№ СЃРёР»СѓСЌС‚
        // Р’РђР–РќРћ: РџСЂРѕРІРµСЂСЏРµРј РЅРµ С‚РѕР»СЊРєРѕ currentState, РЅРѕ Рё РЅР°Р»РёС‡РёРµ РґР°РЅРЅС‹С…!
        // РџРѕС‚РѕРјСѓ С‡С‚Рѕ РїСЂРё РїРѕРІС‚РѕСЂРЅРѕРј РєР»РёРєРµ РЅР° BuildSlot state РјРѕР¶РµС‚ Р±С‹С‚СЊ Idle, РЅРѕ РґР°РЅРЅС‹Рµ РµСЃС‚СЊ
        bool hasExistingRoom = (currentState == DragState.PreviewReady || currentState == DragState.Confirmed) ||
                                (roomPerimeter.Count > 0 || roomFloor.Count > 0);

        if (hasExistingRoom)
        {
            // Р РµР¶РёРј РґРѕР±Р°РІР»РµРЅРёСЏ РєР»РµС‚РѕРє Рє СЃСѓС‰РµСЃС‚РІСѓСЋС‰РµРјСѓ СЃРёР»СѓСЌС‚Сѓ
            isAddingMoreCells = true;

            // РЎРѕС…СЂР°РЅСЏРµРј СЃСѓС‰РµСЃС‚РІСѓСЋС‰РёР№ РїРµСЂРёРјРµС‚СЂ Рё РїРѕР»
            existingPerimeter.Clear();
            existingPerimeter.AddRange(roomPerimeter);
            existingFloor.Clear();
            existingFloor.AddRange(roomFloor);

            if (DEBUG_LOGGING)
            {
            }

            // РљР РРўРР§РќРћ: РћС‡РёС‰Р°РµРј СЃС‚Р°СЂС‹Рµ ghost Р±Р»РѕРєРё РўРћР›Р¬РљРћ РµСЃР»Рё РїРµСЂРµРєР»СЋС‡Р°РµРјСЃСЏ СЃ deletion mode РР›Р РёР· Confirmed state
            // РџСЂРё СѓРґР°Р»РµРЅРёРё Р±С‹Р»Рё СЃРѕР·РґР°РЅС‹ РЅРѕРІС‹Рµ ghost Р±Р»РѕРєРё РґР»СЏ РїРµСЂРёРјРµС‚СЂР°
            // Р•СЃР»Рё РїСЂРѕСЃС‚Рѕ РЅР°Р¶Р°Р»Рё BuildSlot РІС‚РѕСЂРѕР№ СЂР°Р· - РќР• РѕС‡РёС‰Р°РµРј, ghost Р±Р»РѕРєРё РґРѕР»Р¶РЅС‹ РѕСЃС‚Р°С‚СЊСЃСЏ!
            // Р’РђР–РќРћ: Р•СЃР»Рё РІ Confirmed state, РЅСѓР¶РЅРѕ РѕС‡РёСЃС‚РёС‚СЊ confirmedBlocks С‡С‚РѕР±С‹ РѕРЅРё РјРѕРіР»Рё Р±С‹С‚СЊ РїРµСЂРµСЃС‚СЂРѕРµРЅС‹!
            if (wasInDeletionMode || currentState == DragState.Confirmed)
            {
                if (DEBUG_LOGGING)
                {
                }

                // Р’РѕР·РІСЂР°С‰Р°РµРј РІСЃРµ Р°РєС‚РёРІРЅС‹Рµ ghost Р±Р»РѕРєРё РІ pool
                foreach (var kvp in activeGhostBlocks)
                {
                    if (kvp.Value != null)
                    {
                        GhostBlockPool.Instance.Return(kvp.Value);
                    }
                }
                activeGhostBlocks.Clear();
                ghostBlocks.Clear();
                cachedPerimeter.Clear();

                foreach (var kvp in activeFloorBlocks)
                {
                    if (kvp.Value != null)
                    {
                        GhostBlockPool.Instance.Return(kvp.Value);
                    }
                }
                activeFloorBlocks.Clear();
                ghostFloorBlocks.Clear();
                cachedFloor.Clear();

                // РљР РРўРР§РќРћ: РўР°РєР¶Рµ РѕС‡РёС‰Р°РµРј confirmed Р±Р»РѕРєРё РµСЃР»Рё РѕРЅРё РµСЃС‚СЊ
                // РћРЅРё Р±СѓРґСѓС‚ Р·Р°РЅРѕРІРѕ СЃРѕР·РґР°РЅС‹ РїРѕСЃР»Рµ merge СЃ РїСЂР°РІРёР»СЊРЅРѕР№ РєР»Р°СЃСЃРёС„РёРєР°С†РёРµР№ (СЃС‚РµРЅР°/РїРѕР»)
                // Р’РђР–РќРћ: РСЃРїРѕР»СЊР·СѓРµРј DestroyImmediate() С‡С‚РѕР±С‹ СѓРґР°Р»РёС‚СЊ Р±Р»РѕРєРё РЎР РђР—РЈ, РёРЅР°С‡Рµ РѕРЅРё Р±СѓРґСѓС‚ РґСѓР±Р»РёСЂРѕРІР°С‚СЊСЃСЏ!
                if (DEBUG_LOGGING)
                {
                }
                foreach (GameObject block in confirmedBlocks)
                {
                    if (block != null)
                    {
                        if (DEBUG_LOGGING)
                        {
                        }
                        DestroyImmediate(block);
                    }
                }
                confirmedBlocks.Clear();

                if (DEBUG_LOGGING)
                {
                }
                foreach (GameObject block in confirmedFloorBlocks)
                {
                    if (block != null)
                    {
                        if (DEBUG_LOGGING)
                        {
                        }
                        DestroyImmediate(block);
                    }
                }
                confirmedFloorBlocks.Clear();

                // РљР РРўРР§РќРћ: РўР°РєР¶Рµ РѕС‡РёС‰Р°РµРј deletion preview Р±Р»РѕРєРё
                // Р­С‚Рё Р±Р»РѕРєРё РјРѕРіСѓС‚ РѕСЃС‚Р°С‚СЊСЃСЏ РµСЃР»Рё РїРѕР»СЊР·РѕРІР°С‚РµР»СЊ РґРµР»Р°Р» СѓРґР°Р»РµРЅРёРµ РїРµСЂРµРґ РґРѕР±Р°РІР»РµРЅРёРµРј
                if (DEBUG_LOGGING)
                {
                }
                foreach (var kvp in activeDelDragBlocks)
                {
                    if (kvp.Value != null)
                    {
                        if (DEBUG_LOGGING)
                        {
                        }
                        GhostBlockPool.Instance.Return(kvp.Value);
                    }
                }
                activeDelDragBlocks.Clear();
                cachedDelDragPositions.Clear();

                if (DEBUG_LOGGING)
                {
                }
                foreach (GameObject block in delDragPreviewBlocks)
                {
                    if (block != null)
                    {
                        if (DEBUG_LOGGING)
                        {
                        }
                        GhostBlockPool.Instance.Return(block);
                    }
                }
                delDragPreviewBlocks.Clear();

                if (DEBUG_LOGGING)
                {

                    // Р”РѕРїРѕР»РЅРёС‚РµР»СЊРЅР°СЏ РїСЂРѕРІРµСЂРєР° - СЃС‡РёС‚Р°РµРј СЃРєРѕР»СЊРєРѕ ghost Р±Р»РѕРєРѕРІ РѕСЃС‚Р°Р»РѕСЃСЊ РЅР° СЃС†РµРЅРµ
                    int sceneGhostCount = 0;
                    GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
                    foreach (GameObject obj in allObjects)
                    {
                        if (obj.name.Contains("GhostBlock_") || obj.name.Contains("Add_Build_Ghost") || obj.name.Contains("Add_Floor_Ghost"))
                        {
                            sceneGhostCount++;
                        }
                    }
                }

                // РљР РРўРР§РќРћ: РџРѕСЃР»Рµ РѕС‡РёСЃС‚РєРё РЅСѓР¶РЅРѕ Р·Р°РЅРѕРІРѕ СЃРѕР·РґР°С‚СЊ ghost Р±Р»РѕРєРё РґР»СЏ СЃСѓС‰РµСЃС‚РІСѓСЋС‰РµРіРѕ РїРµСЂРёРјРµС‚СЂР°!
                // РРЅР°С‡Рµ РєРѕРјРЅР°С‚Р° РёСЃС‡РµР·РЅРµС‚ РґРѕ РЅР°С‡Р°Р»Р° drag'Р°
                if (DEBUG_LOGGING)
                {
                }

                // РЎРѕР·РґР°РµРј ghost Р±Р»РѕРєРё РґР»СЏ РїРµСЂРёРјРµС‚СЂР°
                foreach (Vector2Int pos in roomPerimeter)
                {
                    GameObject ghostBlock = CreateGhostBlockPooled(pos, buildGhostPrefab);
                    activeGhostBlocks[pos] = ghostBlock;
                    ghostBlocks.Add(ghostBlock);
                    cachedPerimeter.Add(pos);
                }

                // РЎРѕР·РґР°РµРј ghost Р±Р»РѕРєРё РґР»СЏ РїРѕР»Р°
                if (floorGhostPrefab != null)
                {
                    foreach (Vector2Int pos in roomFloor)
                    {
                        GameObject floorBlock = CreateGhostBlockPooled(pos, floorGhostPrefab);
                        activeFloorBlocks[pos] = floorBlock;
                        ghostFloorBlocks.Add(floorBlock);
                        cachedFloor.Add(pos);
                    }
                }

                if (DEBUG_LOGGING)
                {
                }
            }
            else
            {
                if (DEBUG_LOGGING)
                {
                }
            }
        }
        else
        {
            // РџРµСЂРІРѕРЅР°С‡Р°Р»СЊРЅРѕРµ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІРѕ - РЅР°С‡РёРЅР°РµРј СЃ РЅСѓР»СЏ
            isAddingMoreCells = false;
            if (DEBUG_LOGGING)
            {
            }
        }

        currentState = DragState.Idle;
    }

    /// <summary>
    /// Р”РµР°РєС‚РёРІРёСЂРѕРІР°С‚СЊ СЂРµР¶РёРј drag СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
    /// Р’РђР–РќРћ: Р•СЃР»Рё РјС‹ РІ СЃРѕСЃС‚РѕСЏРЅРёРё PreviewReady РёР»Рё Confirmed - РќР• РѕС‡РёС‰Р°РµРј РєР»РµС‚РєРё!
    /// РћС‡РёС‰Р°РµРј С‚РѕР»СЊРєРѕ РµСЃР»Рё РІ СЃРѕСЃС‚РѕСЏРЅРёРё Idle РёР»Рё Dragging (РЅРµР·Р°РІРµСЂС€РµРЅРЅС‹Р№ drag)
    /// </summary>
    public void DeactivateDragMode()
    {

        isDragModeActive = false;
        isAddingMoreCells = false; // РЎР±СЂР°СЃС‹РІР°РµРј С„Р»Р°Рі РґРѕР±Р°РІР»РµРЅРёСЏ

        // РћС‡РёС‰Р°РµРј build cursor preview
        if (buildCursorPreview != null)
        {
            GhostBlockPool.Instance.Return(buildCursorPreview);
            buildCursorPreview = null;
        }

        // Р’РђР–РќРћ: РћС‡РёС‰Р°РµРј РґР°РЅРЅС‹Рµ РўРћР›Р¬РљРћ РµСЃР»Рё:
        // 1. Drag РЅРµ Р±С‹Р» Р·Р°РІРµСЂС€РµРЅ (Idle РёР»Рё Dragging state)
        // 2. Р РґР°РЅРЅС‹С… РєРѕРјРЅР°С‚С‹ РќР•Рў (roomPerimeter Рё roomFloor РїСѓСЃС‚С‹)
        // Р­С‚Рѕ Р·Р°С‰РёС‰Р°РµС‚ РѕС‚ СЃР»СѓС‡Р°Р№РЅРѕРіРѕ СѓРґР°Р»РµРЅРёСЏ РґР°РЅРЅС‹С… РїСЂРё РїРµСЂРµРєР»СЋС‡РµРЅРёРё РёРЅСЃС‚СЂСѓРјРµРЅС‚РѕРІ
        bool hasRoomData = roomPerimeter.Count > 0 || roomFloor.Count > 0;
        bool isIncompleteDrag = currentState == DragState.Idle || currentState == DragState.Dragging;


        if (isIncompleteDrag && !hasRoomData)
        {
            // РќРµР·Р°РІРµСЂС€РµРЅРЅС‹Р№ drag Р‘Р•Р— РґР°РЅРЅС‹С… - РѕС‡РёС‰Р°РµРј РІСЃРµ
            ClearGhostBlocks();
            ClearDelPreview();
            ClearDelDragPreview();
            roomPerimeter.Clear();
            roomFloor.Clear();
            currentState = DragState.Idle;
        }
        else
        {
            // PreviewReady, Confirmed РР›Р РµСЃС‚СЊ РґР°РЅРЅС‹Рµ - РѕСЃС‚Р°РІР»СЏРµРј РєР»РµС‚РєРё РЅР° РјРµСЃС‚Рµ
        }

    }

    /// <summary>
    /// РђРєС‚РёРІРёСЂРѕРІР°С‚СЊ СЂРµР¶РёРј СѓРґР°Р»РµРЅРёСЏ
    /// </summary>
    public void ActivateDeletionMode()
    {

        isDeletionModeActive = true;

    }

    /// <summary>
    /// Р”РµР°РєС‚РёРІРёСЂРѕРІР°С‚СЊ СЂРµР¶РёРј СѓРґР°Р»РµРЅРёСЏ
    /// </summary>
    public void DeactivateDeletionMode()
    {

        isDeletionModeActive = false;
        isDeletionDragActive = false;
        ClearDelGhostBlocks();
        ClearDelPreview();
        ClearDelDragPreview();

    }

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚РєР° СЃРѕСЃС‚РѕСЏРЅРёСЏ Idle - РѕР¶РёРґР°РЅРёРµ РЅР°С‡Р°Р»Р° drag
    /// </summary>
    void HandleIdleState()
    {
        // РџРѕРєР°Р·С‹РІР°РµРј cursor preview РїРѕРґ РјС‹С€РєРѕР№ (С‚РѕР»СЊРєРѕ РґР»СЏ build mode, РґР»СЏ deletion РµСЃС‚СЊ РѕС‚РґРµР»СЊРЅР°СЏ Р»РѕРіРёРєР°)
        if (isDragModeActive && !isDeletionModeActive)
        {
            Vector2Int currentGridPos = GetGridPositionFromMouse();

            if (currentGridPos != Vector2Int.zero && currentGridPos != lastBuildPreviewPos)
            {
                // РЈРґР°Р»СЏРµРј СЃС‚Р°СЂС‹Р№ preview
                if (buildCursorPreview != null)
                {
                    GhostBlockPool.Instance.Return(buildCursorPreview);
                    buildCursorPreview = null;
                }

                // РЎРѕР·РґР°РµРј РЅРѕРІС‹Р№ preview РїРѕРґ РєСѓСЂСЃРѕСЂРѕРј
                buildCursorPreview = CreateGhostBlockPooled(currentGridPos, buildGhostPrefab);
                lastBuildPreviewPos = currentGridPos;
            }
        }

        if (Input.GetMouseButtonDown(0))
        {

            // РЈРґР°Р»СЏРµРј cursor preview РїСЂРё РЅР°С‡Р°Р»Рµ drag
            if (buildCursorPreview != null)
            {
                GhostBlockPool.Instance.Return(buildCursorPreview);
                buildCursorPreview = null;
            }

            // РќР°С‡РёРЅР°РµРј drag
            Vector2Int gridPos = GetGridPositionFromMouse();

            if (gridPos != Vector2Int.zero)
            {
                dragStartGridPos = gridPos;
                dragEndGridPos = gridPos;
                currentState = DragState.Dragging;
            }
            else
            {
            }
        }
    }

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚РєР° СЃРѕСЃС‚РѕСЏРЅРёСЏ Dragging - РїСЂРѕС†РµСЃСЃ СЂРёСЃРѕРІР°РЅРёСЏ СЂР°РјРєРё
    /// </summary>
    void HandleDraggingState()
    {
        if (Input.GetMouseButton(0))
        {
            // РћР±РЅРѕРІР»СЏРµРј РєРѕРЅРµС‡РЅСѓСЋ РїРѕР·РёС†РёСЋ
            Vector2Int currentGridPos = GetGridPositionFromMouse();
            if (currentGridPos != dragEndGridPos)
            {
                dragEndGridPos = currentGridPos;
                UpdateDragPreview();
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            // Р—Р°РєРѕРЅС‡РёР»Рё drag - С„РёРєСЃРёСЂСѓРµРј СЃРёР»СѓСЌС‚
            if (DEBUG_LOGGING)
            {
            }

            // Р•СЃР»Рё СЌС‚Рѕ СЂРµР¶РёРј РґРѕР±Р°РІР»РµРЅРёСЏ РєР»РµС‚РѕРє - РѕР±СЉРµРґРёРЅСЏРµРј СЃ СЃСѓС‰РµСЃС‚РІСѓСЋС‰РёРјРё
            if (isAddingMoreCells)
            {
                MergeNewCellsWithExisting();
                isAddingMoreCells = false; // РЎР±СЂР°СЃС‹РІР°РµРј С„Р»Р°Рі РїРѕСЃР»Рµ merge
            }

            currentState = DragState.PreviewReady;
        }
    }

    /// <summary>
    /// РћР±РЅРѕРІРёС‚СЊ РїСЂРµРІСЊСЋ РІРѕ РІСЂРµРјСЏ drag (РћРџРўРРњРР—РР РћР’РђРќРћ - РёРЅРєСЂРµРјРµРЅС‚Р°Р»СЊРЅРѕРµ РѕР±РЅРѕРІР»РµРЅРёРµ)
    /// </summary>
    void UpdateDragPreview()
    {
        if (DEBUG_LOGGING && isAddingMoreCells)
        {
        }

        // Р’С‹С‡РёСЃР»СЏРµРј РїСЂСЏРјРѕСѓРіРѕР»СЊРЅРёРє
        int minX = Mathf.Min(dragStartGridPos.x, dragEndGridPos.x);
        int maxX = Mathf.Max(dragStartGridPos.x, dragEndGridPos.x);
        int minY = Mathf.Min(dragStartGridPos.y, dragEndGridPos.y);
        int maxY = Mathf.Max(dragStartGridPos.y, dragEndGridPos.y);

        // РќРѕРІС‹Рµ РјРЅРѕР¶РµСЃС‚РІР° РїРѕР·РёС†РёР№ РґР»СЏ РќРћР’РћР“Рћ РЅР°СЂРёСЃРѕРІР°РЅРЅРѕРіРѕ РїСЂСЏРјРѕСѓРіРѕР»СЊРЅРёРєР°
        HashSet<Vector2Int> newPerimeter = new HashSet<Vector2Int>();
        HashSet<Vector2Int> newFloor = new HashSet<Vector2Int>();

        // Р’РђР–РќРћ: Р•СЃР»Рё СЂРµР¶РёРј РґРѕР±Р°РІР»РµРЅРёСЏ - СЃРЅР°С‡Р°Р»Р° РґРѕР±Р°РІР»СЏРµРј СЃСѓС‰РµСЃС‚РІСѓСЋС‰РёРµ РєР»РµС‚РєРё!
        if (isAddingMoreCells)
        {
            // Р”РѕР±Р°РІР»СЏРµРј СЃСѓС‰РµСЃС‚РІСѓСЋС‰РёРµ РєР»РµС‚РєРё РІ РЅРѕРІС‹Рµ РјРЅРѕР¶РµСЃС‚РІР°
            foreach (Vector2Int pos in existingPerimeter)
            {
                newPerimeter.Add(pos);
            }
            foreach (Vector2Int pos in existingFloor)
            {
                newFloor.Add(pos);
            }

            if (DEBUG_LOGGING)
            {
            }
        }

        // РћР±РЅРѕРІР»СЏРµРј СЃРїРёСЃРєРё РїРµСЂРёРјРµС‚СЂР° Рё РїРѕР»Р°
        roomPerimeter.Clear();
        roomFloor.Clear();

        // Р”РѕР±Р°РІР»СЏРµРј РќРћР’Р«Р™ РЅР°СЂРёСЃРѕРІР°РЅРЅС‹Р№ РїСЂСЏРјРѕСѓРіРѕР»СЊРЅРёРє
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int cellPos = new Vector2Int(x, y);

                // РџСЂРѕРІРµСЂСЏРµРј, СЏРІР»СЏРµС‚СЃСЏ Р»Рё РєР»РµС‚РєР° С‡Р°СЃС‚СЊСЋ РїРµСЂРёРјРµС‚СЂР°
                bool isPerimeter = (x == minX || x == maxX || y == minY || y == maxY);

                if (isPerimeter)
                {
                    newPerimeter.Add(cellPos);
                }
                else
                {
                    newFloor.Add(cellPos);
                }
            }
        }

        // Р’РђР–РќРћ: РўРµРїРµСЂСЊ РєРѕРїРёСЂСѓРµРј Р’РЎР• РєР»РµС‚РєРё РёР· newPerimeter/newFloor РІ roomPerimeter/roomFloor
        // Р­С‚Рѕ РІРєР»СЋС‡Р°РµС‚ РєР°Рє СЃСѓС‰РµСЃС‚РІСѓСЋС‰РёРµ РєР»РµС‚РєРё (РµСЃР»Рё isAddingMoreCells), С‚Р°Рє Рё РЅРѕРІС‹Рµ
        roomPerimeter.AddRange(newPerimeter);
        roomFloor.AddRange(newFloor);

        if (DEBUG_LOGGING && isAddingMoreCells)
        {
        }

        // РРќРљР Р•РњР•РќРўРђР›Р¬РќРћР• РћР‘РќРћР’Р›Р•РќРР•: РЈРґР°Р»СЏРµРј Р±Р»РѕРєРё РєРѕС‚РѕСЂС‹С… Р±РѕР»СЊС€Рµ РЅРµС‚
        List<Vector2Int> toRemovePerimeter = new List<Vector2Int>();
        foreach (var pos in cachedPerimeter)
        {
            if (!newPerimeter.Contains(pos))
            {
                toRemovePerimeter.Add(pos);
            }
        }

        foreach (var pos in toRemovePerimeter)
        {
            if (activeGhostBlocks.TryGetValue(pos, out GameObject block))
            {
                GhostBlockPool.Instance.Return(block);
                activeGhostBlocks.Remove(pos);
                ghostBlocks.Remove(block);
            }
            cachedPerimeter.Remove(pos);
        }

        List<Vector2Int> toRemoveFloor = new List<Vector2Int>();
        foreach (var pos in cachedFloor)
        {
            if (!newFloor.Contains(pos))
            {
                toRemoveFloor.Add(pos);
            }
        }

        foreach (var pos in toRemoveFloor)
        {
            if (activeFloorBlocks.TryGetValue(pos, out GameObject block))
            {
                GhostBlockPool.Instance.Return(block);
                activeFloorBlocks.Remove(pos);
                ghostFloorBlocks.Remove(block);
            }
            cachedFloor.Remove(pos);
        }

        // РРќРљР Р•РњР•РќРўРђР›Р¬РќРћР• РћР‘РќРћР’Р›Р•РќРР•: Р”РѕР±Р°РІР»СЏРµРј РЅРѕРІС‹Рµ Р±Р»РѕРєРё
        foreach (var pos in newPerimeter)
        {
            if (!cachedPerimeter.Contains(pos))
            {
                GameObject ghostBlock = CreateGhostBlockPooled(pos, buildGhostPrefab);
                activeGhostBlocks[pos] = ghostBlock;
                ghostBlocks.Add(ghostBlock);
                cachedPerimeter.Add(pos);
            }
        }

        foreach (var pos in newFloor)
        {
            if (!cachedFloor.Contains(pos))
            {
                if (floorGhostPrefab != null)
                {
                    GameObject floorBlock = CreateGhostBlockPooled(pos, floorGhostPrefab);
                    activeFloorBlocks[pos] = floorBlock;
                    ghostFloorBlocks.Add(floorBlock);
                    cachedFloor.Add(pos);
                }
            }
        }
    }

    /// <summary>
    /// РћР±СЉРµРґРёРЅРёС‚СЊ РЅРѕРІС‹Рµ РЅР°СЂРёСЃРѕРІР°РЅРЅС‹Рµ РєР»РµС‚РєРё СЃ СЃСѓС‰РµСЃС‚РІСѓСЋС‰РёРјРё
    /// РџРµСЂРµСЃС‡РёС‚С‹РІР°РµС‚ РїРµСЂРёРјРµС‚СЂ Рё РїРѕР» РґР»СЏ РѕР±СЉРµРґРёРЅРµРЅРЅРѕР№ С„РёРіСѓСЂС‹
    /// </summary>
    void MergeNewCellsWithExisting()
    {
        if (DEBUG_LOGGING)
        {
        }

        // РћР±СЉРµРґРёРЅСЏРµРј РІСЃРµ РєР»РµС‚РєРё (СЃСѓС‰РµСЃС‚РІСѓСЋС‰РёРµ + РЅРѕРІС‹Рµ)
        HashSet<Vector2Int> allCells = new HashSet<Vector2Int>();

        // Р”РѕР±Р°РІР»СЏРµРј СЃСѓС‰РµСЃС‚РІСѓСЋС‰РёРµ РєР»РµС‚РєРё
        foreach (Vector2Int pos in existingPerimeter)
        {
            allCells.Add(pos);
        }
        foreach (Vector2Int pos in existingFloor)
        {
            allCells.Add(pos);
        }

        // Р”РѕР±Р°РІР»СЏРµРј РЅРѕРІС‹Рµ РєР»РµС‚РєРё
        foreach (Vector2Int pos in roomPerimeter)
        {
            allCells.Add(pos);
        }
        foreach (Vector2Int pos in roomFloor)
        {
            allCells.Add(pos);
        }

        if (DEBUG_LOGGING)
        {
        }

        // РўРµРїРµСЂСЊ РїРµСЂРµСЃС‡РёС‚С‹РІР°РµРј РїРµСЂРёРјРµС‚СЂ Рё РїРѕР» РґР»СЏ РѕР±СЉРµРґРёРЅРµРЅРЅРѕР№ С„РёРіСѓСЂС‹
        List<Vector2Int> newPerimeter = new List<Vector2Int>();
        List<Vector2Int> newFloor = new List<Vector2Int>();

        foreach (Vector2Int pos in allCells)
        {
            // РџСЂРѕРІРµСЂСЏРµРј, Р±С‹Р»Р° Р»Рё СЌС‚Р° РєР»РµС‚РєР° С‡Р°СЃС‚СЊСЋ СЃС‚Р°СЂРѕРіРѕ РїРµСЂРёРјРµС‚СЂР° (РёР· СѓРґР°Р»РµРЅРёСЏ)
            bool wasOldPerimeterWall = existingPerimeter.Contains(pos);

            // РљР»РµС‚РєР° - РїРµСЂРёРјРµС‚СЂ, РµСЃР»Рё С…РѕС‚СЏ Р±С‹ РѕРґРёРЅ РѕСЂС‚РѕРіРѕРЅР°Р»СЊРЅС‹Р№ СЃРѕСЃРµРґ РїСѓСЃС‚РѕР№
            bool hasEmptyNeighbor = false;

            Vector2Int[] neighbors = new Vector2Int[]
            {
                pos + Vector2Int.left,
                pos + Vector2Int.right,
                pos + Vector2Int.down,
                pos + Vector2Int.up
            };

            foreach (Vector2Int neighbor in neighbors)
            {
                if (!allCells.Contains(neighbor))
                {
                    hasEmptyNeighbor = true;
                    break;
                }
            }

            // DEBUG: Р›РѕРіРёСЂСѓРµРј РєР»РµС‚РєРё СЃС‚Р°СЂРѕРіРѕ РїРµСЂРёРјРµС‚СЂР°, РєРѕС‚РѕСЂС‹Рµ С‚РµРїРµСЂСЊ РІРЅСѓС‚СЂРё
            if (DEBUG_LOGGING && wasOldPerimeterWall && !hasEmptyNeighbor)
            {
            }

            // РљР РРўРР§РќРћ: Р”РѕРїРѕР»РЅРёС‚РµР»СЊРЅР°СЏ РїСЂРѕРІРµСЂРєР° РЅР° РІРЅСѓС‚СЂРµРЅРЅРёРµ СѓРіР»С‹
            // Р’РђР–РќРћ: Р­С‚Р° РїСЂРѕРІРµСЂРєР° СЂР°Р±РѕС‚Р°РµС‚ РўРћР›Р¬РљРћ РґР»СЏ СЂРµР¶РёРјР° РѕР±СЉРµРґРёРЅРµРЅРёСЏ Р‘Р•Р— СѓРґР°Р»РµРЅРёР№!
            // РљРѕРіРґР° РµСЃС‚СЊ СѓРґР°Р»РµРЅРёСЏ (deletedCells.Count > 0), РІРЅСѓС‚СЂРµРЅРЅРёРµ СѓРіР»С‹ РёС‰СѓС‚СЃСЏ С‡РµСЂРµР· FindInnerCorners()
            if (!hasEmptyNeighbor && deletedCells.Count == 0)
            {
                // РџСЂРѕРІРµСЂСЏРµРј РєР°Р¶РґСѓСЋ РґРёР°РіРѕРЅР°Р»СЊ: РµСЃР»Рё РґРёР°РіРѕРЅР°Р»СЊ РїСѓСЃС‚Р°СЏ Р РѕР±Р° РѕСЂС‚РѕРіРѕРЅР°Р»СЊРЅС‹С… СЃРѕСЃРµРґР° Р·Р°РЅСЏС‚С‹ - СЌС‚Рѕ РІРЅСѓС‚СЂРµРЅРЅРёР№ СѓРіРѕР»
                Vector2Int left = pos + Vector2Int.left;
                Vector2Int right = pos + Vector2Int.right;
                Vector2Int down = pos + Vector2Int.down;
                Vector2Int up = pos + Vector2Int.up;
                Vector2Int topLeft = pos + new Vector2Int(-1, 1);
                Vector2Int topRight = pos + new Vector2Int(1, 1);
                Vector2Int bottomLeft = pos + new Vector2Int(-1, -1);
                Vector2Int bottomRight = pos + new Vector2Int(1, -1);

                bool hasLeft = allCells.Contains(left);
                bool hasRight = allCells.Contains(right);
                bool hasDown = allCells.Contains(down);
                bool hasUp = allCells.Contains(up);

                // РџСЂРѕРІРµСЂРєР° 1: TopLeft РґРёР°РіРѕРЅР°Р»СЊ РїСѓСЃС‚Р°СЏ, РЅРѕ Up Рё Left Р·Р°РЅСЏС‚С‹ -> РІРЅСѓС‚СЂРµРЅРЅРёР№ СѓРіРѕР»
                if (!allCells.Contains(topLeft) && hasUp && hasLeft)
                {
                    hasEmptyNeighbor = true;
                }
                // РџСЂРѕРІРµСЂРєР° 2: TopRight РґРёР°РіРѕРЅР°Р»СЊ РїСѓСЃС‚Р°СЏ, РЅРѕ Up Рё Right Р·Р°РЅСЏС‚С‹ -> РІРЅСѓС‚СЂРµРЅРЅРёР№ СѓРіРѕР»
                else if (!allCells.Contains(topRight) && hasUp && hasRight)
                {
                    hasEmptyNeighbor = true;
                }
                // РџСЂРѕРІРµСЂРєР° 3: BottomLeft РґРёР°РіРѕРЅР°Р»СЊ РїСѓСЃС‚Р°СЏ, РЅРѕ Down Рё Left Р·Р°РЅСЏС‚С‹ -> РІРЅСѓС‚СЂРµРЅРЅРёР№ СѓРіРѕР»
                else if (!allCells.Contains(bottomLeft) && hasDown && hasLeft)
                {
                    hasEmptyNeighbor = true;
                }
                // РџСЂРѕРІРµСЂРєР° 4: BottomRight РґРёР°РіРѕРЅР°Р»СЊ РїСѓСЃС‚Р°СЏ, РЅРѕ Down Рё Right Р·Р°РЅСЏС‚С‹ -> РІРЅСѓС‚СЂРµРЅРЅРёР№ СѓРіРѕР»
                else if (!allCells.Contains(bottomRight) && hasDown && hasRight)
                {
                    hasEmptyNeighbor = true;
                }
            }

            if (hasEmptyNeighbor)
            {
                newPerimeter.Add(pos);
            }
            else
            {
                newFloor.Add(pos);
            }
        }

        // РћР±РЅРѕРІР»СЏРµРј СЃРїРёСЃРєРё
        roomPerimeter.Clear();
        roomPerimeter.AddRange(newPerimeter);
        roomFloor.Clear();
        roomFloor.AddRange(newFloor);

        if (DEBUG_LOGGING)
        {
        }

        // Р’РђР–РќРћ: Р’С‹Р·С‹РІР°РµРј RecalculatePerimeter() РґР»СЏ С„РёРЅР°Р»СЊРЅРѕР№ РѕР±СЂР°Р±РѕС‚РєРё
        // Р­С‚Рѕ РѕРїРµСЂР°С†РёСЏ Р”РћР‘РђР’Р›Р•РќРРЇ (РЅРµ СѓРґР°Р»РµРЅРёСЏ), РїРѕСЌС‚РѕРјСѓ isDeletion=false
        RecalculatePerimeter(isDeletion: false);

        if (DEBUG_LOGGING)
        {
        }

        // РћР±РЅРѕРІР»СЏРµРј ghost Р±Р»РѕРєРё РґР»СЏ РѕС‚РѕР±СЂР°Р¶РµРЅРёСЏ РѕР±СЉРµРґРёРЅРµРЅРЅРѕР№ С„РёРіСѓСЂС‹
        UpdateGhostsAfterMerge();
    }

    /// <summary>
    /// РћР±РЅРѕРІРёС‚СЊ ghost Р±Р»РѕРєРё РїРѕСЃР»Рµ РѕР±СЉРµРґРёРЅРµРЅРёСЏ РєР»РµС‚РѕРє
    /// </summary>
    void UpdateGhostsAfterMerge()
    {
        if (DEBUG_LOGGING)
        {
        }

        // РћС‡РёС‰Р°РµРј С‚РµРєСѓС‰РёРµ ghost Р±Р»РѕРєРё
        int wallsReturned = 0;
        foreach (var kvp in activeGhostBlocks)
        {
            if (kvp.Value != null)
            {
                GhostBlockPool.Instance.Return(kvp.Value);
                wallsReturned++;
            }
        }
        activeGhostBlocks.Clear();
        ghostBlocks.Clear();
        cachedPerimeter.Clear();

        int floorsReturned = 0;
        foreach (var kvp in activeFloorBlocks)
        {
            if (kvp.Value != null)
            {
                GhostBlockPool.Instance.Return(kvp.Value);
                floorsReturned++;
            }
        }
        activeFloorBlocks.Clear();
        ghostFloorBlocks.Clear();
        cachedFloor.Clear();

        if (DEBUG_LOGGING)
        {
        }

        // РЎРѕР·РґР°РµРј ghost Р±Р»РѕРєРё РґР»СЏ РЅРѕРІРѕРіРѕ РїРµСЂРёРјРµС‚СЂР°
        if (DEBUG_LOGGING)
        {
        }
        foreach (Vector2Int pos in roomPerimeter)
        {
            GameObject ghostBlock = CreateGhostBlockPooled(pos, buildGhostPrefab);
            activeGhostBlocks[pos] = ghostBlock;
            ghostBlocks.Add(ghostBlock);
            cachedPerimeter.Add(pos);

            if (DEBUG_LOGGING)
            {
            }
        }

        // РЎРѕР·РґР°РµРј ghost Р±Р»РѕРєРё РґР»СЏ РЅРѕРІРѕРіРѕ РїРѕР»Р°
        if (floorGhostPrefab != null)
        {
            if (DEBUG_LOGGING)
            {
            }
            foreach (Vector2Int pos in roomFloor)
            {
                GameObject floorBlock = CreateGhostBlockPooled(pos, floorGhostPrefab);
                activeFloorBlocks[pos] = floorBlock;
                ghostFloorBlocks.Add(floorBlock);
                cachedFloor.Add(pos);

                if (DEBUG_LOGGING)
                {
                }
            }
        }

        if (DEBUG_LOGGING)
        {

            // РџСЂРѕРІРµСЂСЏРµРј СЃРєРѕР»СЊРєРѕ РђРљРўРР’РќР«РҐ ghost Р±Р»РѕРєРѕРІ РЅР° СЃС†РµРЅРµ РїРѕСЃР»Рµ СЃРѕР·РґР°РЅРёСЏ
            int activeSceneCount = 0;
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj.activeInHierarchy && (obj.name.Contains("GhostBlock_") || obj.name.Contains("Build_Ghost")))
                {
                    activeSceneCount++;
                }
            }
        }
    }

    /// <summary>
    /// РЎРѕР·РґР°С‚СЊ РїСЂРёР·СЂР°С‡РЅС‹Р№ Р±Р»РѕРє РІ СѓРєР°Р·Р°РЅРЅРѕР№ РїРѕР·РёС†РёРё (РЈРЎРўРђР Р•Р’РЁРР™ - РґР»СЏ СЃРѕРІРјРµСЃС‚РёРјРѕСЃС‚Рё)
    /// </summary>
    GameObject CreateGhostBlock(Vector2Int gridPos, GameObject prefab)
    {
        Vector3 worldPos = gridManager.GridToWorld(gridPos);
        GameObject block = Instantiate(prefab, worldPos, Quaternion.identity);
        block.name = $"GhostBlock_{gridPos.x}_{gridPos.y}";

        // РЈР±РёСЂР°РµРј РєРѕР»Р»Р°Р№РґРµСЂС‹
        Collider[] colliders = block.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            Destroy(col);
        }

        return block;
    }

    /// <summary>
    /// РЎРѕР·РґР°С‚СЊ РїСЂРёР·СЂР°С‡РЅС‹Р№ Р±Р»РѕРє РёСЃРїРѕР»СЊР·СѓСЏ Object Pool (РћРџРўРРњРР—РР РћР’РђРќРћ)
    /// </summary>
    GameObject CreateGhostBlockPooled(Vector2Int gridPos, GameObject prefab)
    {
        Vector3 worldPos = gridManager.GridToWorld(gridPos);
        GameObject block = GhostBlockPool.Instance.Get(prefab, worldPos, Quaternion.identity);

        if (block != null)
        {
            block.name = $"GhostBlock_{gridPos.x}_{gridPos.y}";

            // РЈР±РёСЂР°РµРј РєРѕР»Р»Р°Р№РґРµСЂС‹ РїСЂРё РїРµСЂРІРѕРј СЃРѕР·РґР°РЅРёРё
            Collider[] colliders = block.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                if (col != null && col.enabled)
                {
                    Destroy(col);
                }
            }
        }

        return block;
    }

    /// <summary>
    /// РџРѕРґС‚РІРµСЂРґРёС‚СЊ РїРѕСЃС‚СЂРѕР№РєСѓ - РїСЂРёРјРµРЅРёС‚СЊ РјР°С‚РµСЂРёР°Р» M_Add_Build_Ghost Рё Р·Р°РїСѓСЃС‚РёС‚СЊ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІРѕ
    /// </summary>
    public void ConfirmBuild()
    {
        // Р’РђР–РќРћ: РџСЂРѕРІРµСЂСЏРµРј РЅРµ state, Р° РЅР°Р»РёС‡РёРµ РґР°РЅРЅС‹С…!
        // РњРѕР¶РЅРѕ РїРѕРґС‚РІРµСЂР¶РґР°С‚СЊ РІ Р»СЋР±РѕРј СЃРѕСЃС‚РѕСЏРЅРёРё РµСЃР»Рё РµСЃС‚СЊ РґР°РЅРЅС‹Рµ
        if (!HasRoomData())
        {
            return;
        }

        // Р—Р°РіСЂСѓР¶Р°РµРј РјР°С‚РµСЂРёР°Р» M_Add_Build_Ghost
        Material addBuildMaterial = Resources.Load<Material>("Materials/M_Add_Build_Ghost");
        if (addBuildMaterial == null)
        {
            return;
        }

        // РЎРїРёСЃРѕРє Р±Р»РѕРєРѕРІ РґР»СЏ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
        List<ConstructionBlock> constructionBlocks = new List<ConstructionBlock>();

        // РџСЂРёРјРµРЅСЏРµРј РјР°С‚РµСЂРёР°Р» Рє СЃСѓС‰РµСЃС‚РІСѓСЋС‰РёРј Build_Ghost Р±Р»РѕРєР°Рј (СЃС‚РµРЅС‹)
        foreach (var kvp in activeGhostBlocks)
        {
            GameObject ghostBlock = kvp.Value;
            Vector2Int gridPos = kvp.Key;

            // РџСЂРёРјРµРЅСЏРµРј РјР°С‚РµСЂРёР°Р» M_Add_Build_Ghost
            Renderer renderer = ghostBlock.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = addBuildMaterial;
            }

            // Р”РѕР±Р°РІР»СЏРµРј Р±Р»РѕРє РІ РѕС‡РµСЂРµРґСЊ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
            Vector3 worldPos = ghostBlock.transform.position;
            ConstructionBlock constructionBlock = new ConstructionBlock(gridPos, worldPos, ghostBlock, ConstructionBlock.BlockType.Wall);

            // Callback РґР»СЏ Р·Р°РјРµРЅС‹ РЅР° С„РёРЅР°Р»СЊРЅС‹Р№ РїСЂРµС„Р°Р± РїРѕСЃР»Рµ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
            constructionBlock.OnConstructionComplete = () => ReplaceGhostWithWall(gridPos, ghostBlock);

            constructionBlocks.Add(constructionBlock);
            confirmedBlocks.Add(ghostBlock);
        }

        // РџСЂРёРјРµРЅСЏРµРј РјР°С‚РµСЂРёР°Р» Рє Floor_Ghost Р±Р»РѕРєР°Рј (РїРѕР»)
        foreach (var kvp in activeFloorBlocks)
        {
            GameObject ghostFloorBlock = kvp.Value;
            Vector2Int gridPos = kvp.Key;

            // РџСЂРёРјРµРЅСЏРµРј РјР°С‚РµСЂРёР°Р» M_Add_Build_Ghost (РёР»Рё РґСЂСѓРіРѕР№ РґР»СЏ РїРѕР»Р°)
            Renderer renderer = ghostFloorBlock.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = addBuildMaterial;
            }

            // Р”РѕР±Р°РІР»СЏРµРј Р±Р»РѕРє РІ РѕС‡РµСЂРµРґСЊ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
            Vector3 worldPos = ghostFloorBlock.transform.position;
            ConstructionBlock constructionBlock = new ConstructionBlock(gridPos, worldPos, ghostFloorBlock, ConstructionBlock.BlockType.Floor);

            // Callback РґР»СЏ Р·Р°РјРµРЅС‹ РЅР° С„РёРЅР°Р»СЊРЅС‹Р№ РїСЂРµС„Р°Р± РїРѕСЃР»Рµ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
            constructionBlock.OnConstructionComplete = () => ReplaceGhostWithFloor(gridPos, ghostFloorBlock);

            constructionBlocks.Add(constructionBlock);
            confirmedFloorBlocks.Add(ghostFloorBlock);
        }



        // Р РµРіРёСЃС‚СЂРёСЂСѓРµРј Р±Р»РѕРєРё РІ ConstructionManager РґР»СЏ Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРѕРіРѕ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
        if (ConstructionManager.Instance != null)
        {
            ConstructionManager.Instance.AddConstructionBlocks(constructionBlocks);
        }
        else
        {
        }

        currentState = DragState.Confirmed;
    }

    /// <summary>
    /// Р—Р°РјРµРЅРёС‚СЊ ghost-СЃС‚РµРЅСѓ РЅР° С„РёРЅР°Р»СЊРЅС‹Р№ РїСЂРµС„Р°Р± РїРѕСЃР»Рµ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
    /// </summary>
    void ReplaceGhostWithWall(Vector2Int gridPos, GameObject ghostBlock)
    {


        // РЈРЅРёС‡С‚РѕР¶Р°РµРј ghost Р±Р»РѕРє
        if (ghostBlock != null)
        {
            Destroy(ghostBlock);
        }

        // РЈРґР°Р»СЏРµРј РёР· confirmedBlocks
        confirmedBlocks.Remove(ghostBlock);

        // Р”РѕР±Р°РІР»СЏРµРј СЃС‚РµРЅСѓ РІ РіР»РѕР±Р°Р»СЊРЅС‹Р№ СЂРµРµСЃС‚СЂ RoomBuilder
        // RoomBuilder Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРё СЃРѕР·РґР°СЃС‚ РІРёР·СѓР°Р»СЊРЅСѓСЋ СЃС‚РµРЅСѓ РїСЂРё СЃР»РµРґСѓСЋС‰РµРј UpdateWallVisuals
        if (RoomBuilder.Instance != null)
        {
            // РЎРѕР·РґР°РµРј HashSet РґР»СЏ Р±С‹СЃС‚СЂРѕР№ РїСЂРѕРІРµСЂРєРё
            HashSet<Vector2Int> wallSet = new HashSet<Vector2Int>(roomPerimeter);
            HashSet<Vector2Int> floorSet = new HashSet<Vector2Int>(roomFloor);

            // РћРїСЂРµРґРµР»СЏРµРј С‚РёРї СЃС‚РµРЅС‹ РЅР° РѕСЃРЅРѕРІРµ СЃРѕСЃРµРґРµР№
            WallSide wallSide = RoomBuilder.Instance.DetermineWallSideFromNeighbors(gridPos, wallSet, floorSet);
            bool isInnerCorner = savedInnerCorners.Contains(gridPos);
            WallType wallType = isInnerCorner ? WallType.InnerCorner : RoomBuilder.Instance.DetermineWallType(wallSide);

            // РЎРѕР·РґР°РµРј WallData
            WallData wallData = new WallData(
                gridPos,
                WallDirection.Vertical,
                Vector2Int.zero,
                Vector2Int.zero,
                wallSide,
                wallType,
                0
            );

            // Р”РѕР±Р°РІР»СЏРµРј РІ РіР»РѕР±Р°Р»СЊРЅС‹Р№ СЂРµРµСЃС‚СЂ СЃС‚РµРЅ
            RoomBuilder.Instance.AddWallToGlobal(wallData);

            // РћР±РЅРѕРІР»СЏРµРј РІРёР·СѓР°Р»РёР·Р°С†РёСЋ СЃС‚РµРЅ
            RoomBuilder.Instance.UpdateWallVisuals();


        }
        else
        {
        }
    }

    /// <summary>
    /// Р—Р°РјРµРЅРёС‚СЊ ghost-РїРѕР» РЅР° С„РёРЅР°Р»СЊРЅС‹Р№ РїСЂРµС„Р°Р± РїРѕСЃР»Рµ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
    /// </summary>
    void ReplaceGhostWithFloor(Vector2Int gridPos, GameObject ghostFloorBlock)
    {


        // РЈРЅРёС‡С‚РѕР¶Р°РµРј ghost Р±Р»РѕРє
        if (ghostFloorBlock != null)
        {
            Destroy(ghostFloorBlock);
        }

        // РЈРґР°Р»СЏРµРј РёР· confirmedFloorBlocks
        confirmedFloorBlocks.Remove(ghostFloorBlock);

        // РЎРѕР·РґР°РµРј РЅР°СЃС‚РѕСЏС‰СѓСЋ РїР»РёС‚РєСѓ РїРѕР»Р°
        GameObject floorTile = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floorTile.name = $"ConstructedFloor_{gridPos.x}_{gridPos.y}";

        // РџРѕР·РёС†РёСЏ РїР»РёС‚РєРё (С†РµРЅС‚СЂ РєР»РµС‚РєРё)
        Vector3 worldPos = gridManager.GridToWorld(gridPos);
        worldPos.y = -0.05f; // РќРµР±РѕР»СЊС€РѕРµ СЃРјРµС‰РµРЅРёРµ РІРЅРёР· РґР»СЏ РїРѕР»Р°
        floorTile.transform.position = worldPos;

        // Р Р°Р·РјРµСЂ РїР»РёС‚РєРё (С‡СѓС‚СЊ РјРµРЅСЊС€Рµ РєР»РµС‚РєРё С‡С‚РѕР±С‹ РІРёРґРµС‚СЊ С€РІС‹)
        float cellSize = gridManager.cellSize;
        floorTile.transform.localScale = new Vector3(cellSize * 0.95f, 0.1f, cellSize * 0.95f);

        // РџСЂРёРјРµРЅСЏРµРј РјР°С‚РµСЂРёР°Р» РїРѕР»Р°
        Material floorMaterial = Resources.Load<Material>("Materials/M_Floor");
        Renderer renderer = floorTile.GetComponent<Renderer>();
        if (renderer != null && floorMaterial != null)
        {
            renderer.material = floorMaterial;
        }
        else if (renderer != null)
        {
            // Р•СЃР»Рё РјР°С‚РµСЂРёР°Р» РЅРµ РЅР°Р№РґРµРЅ, РёСЃРїРѕР»СЊР·СѓРµРј Р±Р°Р·РѕРІС‹Р№ СЃРµСЂС‹Р№ С†РІРµС‚
            renderer.material.color = new Color(0.5f, 0.5f, 0.5f);
        }

        // РЈР±РёСЂР°РµРј РєРѕР»Р»Р°Р№РґРµСЂ Сѓ РїР»РёС‚РєРё (РєРѕР»Р»Р°Р№РґРµСЂ РІС‹РґРµР»РµРЅРёСЏ Р±СѓРґРµС‚ РѕС‚РґРµР»СЊРЅРѕ РµСЃР»Рё РЅСѓР¶РµРЅ)
        Collider tileCollider = floorTile.GetComponent<Collider>();
        if (tileCollider != null)
        {
            Destroy(tileCollider);
        }


    }

    /// <summary>
    /// Р¤РёРЅР°Р»РёР·РёСЂРѕРІР°С‚СЊ РїРѕСЃС‚СЂРѕР№РєСѓ - СЃРѕР·РґР°С‚СЊ РЅР°СЃС‚РѕСЏС‰РёРµ СЃС‚РµРЅС‹
    /// РўРµРїРµСЂСЊ СЂР°Р±РѕС‚Р°РµС‚ РЎР РђР—РЈ, РјРёРЅСѓСЏ Confirmed state (green ghost)
    /// </summary>
    public void FinalizeBuild()
    {
        // Р’РђР–РќРћ: РџСЂРѕРІРµСЂСЏРµРј РЅРµ state, Р° РЅР°Р»РёС‡РёРµ РґР°РЅРЅС‹С…!
        if (!HasRoomData())
        {
            return;
        }


        // Р’С‹С‡РёСЃР»СЏРµРј РіСЂР°РЅРёС†С‹ РєРѕРјРЅР°С‚С‹ РґР»СЏ СЃРѕР·РґР°РЅРёСЏ GameObject
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (Vector2Int pos in roomPerimeter)
        {
            minX = Mathf.Min(minX, pos.x);
            maxX = Mathf.Max(maxX, pos.x);
            minY = Mathf.Min(minY, pos.y);
            maxY = Mathf.Max(maxY, pos.y);
        }

        // РўР°РєР¶Рµ СѓС‡РёС‚С‹РІР°РµРј РєР»РµС‚РєРё РїРѕР»Р°
        foreach (Vector2Int pos in roomFloor)
        {
            minX = Mathf.Min(minX, pos.x);
            maxX = Mathf.Max(maxX, pos.x);
            minY = Mathf.Min(minY, pos.y);
            maxY = Mathf.Max(maxY, pos.y);
        }

        Vector2Int roomGridPos = new Vector2Int(minX, minY);
        Vector2Int roomSize = new Vector2Int(maxX - minX + 1, maxY - minY + 1);

        // РСЃРїРѕР»СЊР·СѓРµРј СЃРѕС…СЂР°РЅРµРЅРЅС‹Рµ РІРЅСѓС‚СЂРµРЅРЅРёРµ СѓРіР»С‹ РёР· RecalculatePerimeter
        foreach (Vector2Int corner in savedInnerCorners)
        {
        }

        // РЎРѕР·РґР°РµРј РєРѕРјРЅР°С‚Сѓ СЃ РєР°СЃС‚РѕРјРЅС‹Рј СЃРёР»СѓСЌС‚РѕРј, РїРµСЂРµРґР°РІР°СЏ РІРЅСѓС‚СЂРµРЅРЅРёРµ СѓРіР»С‹
        GameObject room = RoomBuilder.Instance.BuildCustomRoom(roomGridPos, roomSize, "DraggedRoom", roomPerimeter, roomFloor, savedInnerCorners);
        room.name = $"DraggedRoom_{roomGridPos.x}_{roomGridPos.y}";


        // Р РµРіРёСЃС‚СЂРёСЂСѓРµРј РІ GridManager
        gridManager.OccupyCellPerimeter(roomGridPos, roomSize.x, roomSize.y, room, "Room");

        // РћС‡РёС‰Р°РµРј Add_Build_Ghost Рё Add_Floor_Ghost Р±Р»РѕРєРё
        ClearConfirmedBlocks();

        // РљР РРўРР§РќРћ: РўР°РєР¶Рµ РѕС‡РёС‰Р°РµРј activeGhostBlocks Рё activeFloorBlocks
        // Р­С‚Рѕ ghost Р±Р»РѕРєРё РёР· СЂРµР¶РёРјР° preview, РєРѕС‚РѕСЂС‹Рµ РјРѕРіР»Рё РѕСЃС‚Р°С‚СЊСЃСЏ РїРѕСЃР»Рµ СѓРґР°Р»РµРЅРёСЏ
        if (DEBUG_LOGGING)
        {
        }

        foreach (var kvp in activeGhostBlocks)
        {
            if (kvp.Value != null)
            {
                GhostBlockPool.Instance.Return(kvp.Value);
            }
        }
        activeGhostBlocks.Clear();
        ghostBlocks.Clear();
        cachedPerimeter.Clear();

        foreach (var kvp in activeFloorBlocks)
        {
            if (kvp.Value != null)
            {
                GhostBlockPool.Instance.Return(kvp.Value);
            }
        }
        activeFloorBlocks.Clear();
        ghostFloorBlocks.Clear();
        cachedFloor.Clear();

        if (DEBUG_LOGGING)
        {
        }

        // РЎР±СЂР°СЃС‹РІР°РµРј СЃРѕСЃС‚РѕСЏРЅРёРµ
        currentState = DragState.Idle;
        roomPerimeter.Clear();
        roomFloor.Clear();
        deletedCells.Clear();
        savedInnerCorners.Clear();

    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РїРѕР·РёС†РёСЋ СЃРµС‚РєРё РёР· РїРѕР·РёС†РёРё РјС‹С€Рё
    /// </summary>
    Vector2Int GetGridPositionFromMouse()
    {
        if (playerCamera == null || gridManager == null)
            return Vector2Int.zero;

        Vector3 mousePosition = Input.mousePosition;
        Ray ray = playerCamera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            Vector3 worldPos = hit.point;
            return gridManager.WorldToGrid(worldPos);
        }
        else
        {
            // Р•СЃР»Рё raycast РЅРµ РїРѕРїР°Р», РёСЃРїРѕР»СЊР·СѓРµРј РїР»РѕСЃРєРѕСЃС‚СЊ Y=0
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 worldPos = ray.GetPoint(distance);
                return gridManager.WorldToGrid(worldPos);
            }
        }

        return Vector2Int.zero;
    }

    /// <summary>
    /// РћС‡РёСЃС‚РёС‚СЊ Build_Ghost Р±Р»РѕРєРё (СЃС‚РµРЅС‹ Рё РїРѕР»)
    /// </summary>
    void ClearGhostBlocks()
    {
        foreach (GameObject block in ghostBlocks)
        {
            if (block != null)
                Destroy(block);
        }
        ghostBlocks.Clear();

        foreach (GameObject block in ghostFloorBlocks)
        {
            if (block != null)
                Destroy(block);
        }
        ghostFloorBlocks.Clear();
    }

    /// <summary>
    /// РћС‡РёСЃС‚РёС‚СЊ Add_Build_Ghost Р±Р»РѕРєРё (СЃС‚РµРЅС‹ Рё РїРѕР»)
    /// </summary>
    void ClearConfirmedBlocks()
    {
        foreach (GameObject block in confirmedBlocks)
        {
            if (block != null)
                Destroy(block);
        }
        confirmedBlocks.Clear();

        foreach (GameObject block in confirmedFloorBlocks)
        {
            if (block != null)
                Destroy(block);
        }
        confirmedFloorBlocks.Clear();
    }

    /// <summary>
    /// РћС‡РёСЃС‚РёС‚СЊ РІСЃРµ РїСЂРёР·СЂР°РєРё
    /// </summary>
    void ClearAllGhosts()
    {
        ClearGhostBlocks();
        ClearConfirmedBlocks();
        ClearDelGhostBlocks();
        ClearDelPreview();
        ClearDelDragPreview();
        roomPerimeter.Clear();
        roomFloor.Clear();
        savedInnerCorners.Clear();
        currentState = DragState.Idle;
        isDeletionDragActive = false;
    }

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚РєР° СЂРµР¶РёРјР° СѓРґР°Р»РµРЅРёСЏ - preview Рё drag СѓРґР°Р»РµРЅРёРµ
    /// </summary>
    void HandleDeletionMode()
    {
        Vector2Int currentGridPos = GetGridPositionFromMouse();

        if (!isDeletionDragActive)
        {
            // РћР±РЅРѕРІР»СЏРµРј preview РїСЂРё РґРІРёР¶РµРЅРёРё РјС‹С€Рё
            UpdateDeletionPreview(currentGridPos);

            // РќР°С‡РёРЅР°РµРј drag СѓРґР°Р»РµРЅРёРµ РїСЂРё Р·Р°Р¶Р°С‚РёРё Р›РљРњ (РјРѕР¶РЅРѕ РЅР°С‡Р°С‚СЊ РёР· Р»СЋР±РѕРіРѕ РјРµСЃС‚Р°)
            if (Input.GetMouseButtonDown(0))
            {
                if (currentGridPos != Vector2Int.zero)
                {
                    isDeletionDragActive = true;
                    deletionDragStart = currentGridPos;
                    deletionDragEnd = currentGridPos;
                    ClearDelPreview(); // РЈР±РёСЂР°РµРј РѕР±С‹С‡РЅС‹Р№ preview
                    if (DEBUG_LOGGING)
                    {
                    }
                }
            }
        }
        else
        {
            // РћР±РЅРѕРІР»СЏРµРј drag РѕР±Р»Р°СЃС‚СЊ
            if (Input.GetMouseButton(0))
            {
                if (currentGridPos != deletionDragEnd)
                {
                    deletionDragEnd = currentGridPos;
                    UpdateDeletionDragPreview();
                }
            }
            // Р—Р°РІРµСЂС€Р°РµРј drag СѓРґР°Р»РµРЅРёРµ
            else if (Input.GetMouseButtonUp(0))
            {
                if (DEBUG_LOGGING)
                {
                }
                DeleteCellsInRectangle(deletionDragStart, deletionDragEnd);
                ClearDelDragPreview();
                isDeletionDragActive = false;
            }
        }
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ, СЏРІР»СЏРµС‚СЃСЏ Р»Рё РєР»РµС‚РєР° С‡Р°СЃС‚СЊСЋ РєРѕРјРЅР°С‚С‹
    /// </summary>
    bool IsCellPartOfRoom(Vector2Int cellPos)
    {
        return roomPerimeter.Contains(cellPos) || roomFloor.Contains(cellPos);
    }

    /// <summary>
    /// РћР±РЅРѕРІРёС‚СЊ preview Р±Р»РѕРє РїСЂРё РЅР°РІРµРґРµРЅРёРё РєСѓСЂСЃРѕСЂР° (deletion mode)
    /// </summary>
    void UpdateDeletionPreview(Vector2Int gridPos)
    {
        if (gridPos == Vector2Int.zero)
        {
            ClearDelPreview();
            return;
        }

        // Р•СЃР»Рё РїРѕР·РёС†РёСЏ РёР·РјРµРЅРёР»Р°СЃСЊ - РѕР±РЅРѕРІР»СЏРµРј preview
        if (gridPos != lastDelPreviewPos)
        {
            ClearDelPreview();

            // РЎРѕР·РґР°РµРј РЅРѕРІС‹Р№ preview Р±Р»РѕРє (РїРѕРєР°Р·С‹РІР°РµРј РІСЃРµРіРґР°, РґР°Р¶Рµ РµСЃР»Рё РЅРµ РЅР°Рґ РєРѕРјРЅР°С‚РѕР№)
            if (delBuildGhostPrefab != null)
            {
                delPreviewBlock = CreateGhostBlockPooled(gridPos, delBuildGhostPrefab);
                delPreviewBlock.name = "Del_Preview";
                lastDelPreviewPos = gridPos;
            }
        }
    }

    /// <summary>
    /// РћС‡РёСЃС‚РёС‚СЊ deletion preview Р±Р»РѕРє
    /// </summary>
    void ClearDelPreview()
    {
        if (delPreviewBlock != null)
        {
            GhostBlockPool.Instance.Return(delPreviewBlock);
            delPreviewBlock = null;
        }
        lastDelPreviewPos = Vector2Int.zero;
    }

    /// <summary>
    /// РћР±РЅРѕРІРёС‚СЊ preview РґР»СЏ drag СѓРґР°Р»РµРЅРёСЏ (РћРџРўРРњРР—РР РћР’РђРќРћ - РёРЅРєСЂРµРјРµРЅС‚Р°Р»СЊРЅРѕРµ РѕР±РЅРѕРІР»РµРЅРёРµ)
    /// </summary>
    void UpdateDeletionDragPreview()
    {
        // Р’С‹С‡РёСЃР»СЏРµРј РїСЂСЏРјРѕСѓРіРѕР»СЊРЅРёРє
        int minX = Mathf.Min(deletionDragStart.x, deletionDragEnd.x);
        int maxX = Mathf.Max(deletionDragStart.x, deletionDragEnd.x);
        int minY = Mathf.Min(deletionDragStart.y, deletionDragEnd.y);
        int maxY = Mathf.Max(deletionDragStart.y, deletionDragEnd.y);

        // РќРѕРІС‹Рµ РїРѕР·РёС†РёРё preview
        HashSet<Vector2Int> newPositions = new HashSet<Vector2Int>();

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                newPositions.Add(new Vector2Int(x, y));
            }
        }

        // РРќРљР Р•РњР•РќРўРђР›Р¬РќРћР• РћР‘РќРћР’Р›Р•РќРР•: РЈРґР°Р»СЏРµРј Р±Р»РѕРєРё РєРѕС‚РѕСЂС‹С… Р±РѕР»СЊС€Рµ РЅРµС‚
        List<Vector2Int> toRemove = new List<Vector2Int>();
        foreach (var pos in cachedDelDragPositions)
        {
            if (!newPositions.Contains(pos))
            {
                toRemove.Add(pos);
            }
        }

        foreach (var pos in toRemove)
        {
            if (activeDelDragBlocks.TryGetValue(pos, out GameObject block))
            {
                GhostBlockPool.Instance.Return(block);
                activeDelDragBlocks.Remove(pos);
                delDragPreviewBlocks.Remove(block);
            }
            cachedDelDragPositions.Remove(pos);
        }

        // РРќРљР Р•РњР•РќРўРђР›Р¬РќРћР• РћР‘РќРћР’Р›Р•РќРР•: Р”РѕР±Р°РІР»СЏРµРј РЅРѕРІС‹Рµ Р±Р»РѕРєРё
        foreach (var pos in newPositions)
        {
            if (!cachedDelDragPositions.Contains(pos))
            {
                if (delBuildGhostPrefab != null)
                {
                    GameObject previewBlock = CreateGhostBlockPooled(pos, delBuildGhostPrefab);
                    previewBlock.name = $"Del_DragPreview_{pos.x}_{pos.y}";
                    activeDelDragBlocks[pos] = previewBlock;
                    delDragPreviewBlocks.Add(previewBlock);
                    cachedDelDragPositions.Add(pos);
                }
            }
        }
    }

    /// <summary>
    /// РћС‡РёСЃС‚РёС‚СЊ drag preview Р±Р»РѕРєРё
    /// </summary>
    void ClearDelDragPreview()
    {
        // Р’РђР–РќРћ: РСЃРїРѕР»СЊР·СѓРµРј Object Pool РґР»СЏ РІРѕР·РІСЂР°С‚Р° Р±Р»РѕРєРѕРІ
        foreach (var kvp in activeDelDragBlocks)
        {
            if (kvp.Value != null)
            {
                GhostBlockPool.Instance.Return(kvp.Value);
            }
        }
        activeDelDragBlocks.Clear();
        cachedDelDragPositions.Clear();
        delDragPreviewBlocks.Clear();

        if (DEBUG_LOGGING)
        {
        }
    }

    /// <summary>
    /// РЈРґР°Р»РёС‚СЊ РєР»РµС‚РєРё РІ РїСЂСЏРјРѕСѓРіРѕР»СЊРЅРёРєРµ
    /// </summary>
    void DeleteCellsInRectangle(Vector2Int startPos, Vector2Int endPos)
    {
        int minX = Mathf.Min(startPos.x, endPos.x);
        int maxX = Mathf.Max(startPos.x, endPos.x);
        int minY = Mathf.Min(startPos.y, endPos.y);
        int maxY = Mathf.Max(startPos.y, endPos.y);

        List<Vector2Int> cellsToDelete = new List<Vector2Int>();

        // РЎРѕР±РёСЂР°РµРј РІСЃРµ РєР»РµС‚РєРё РґР»СЏ СѓРґР°Р»РµРЅРёСЏ
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int cellPos = new Vector2Int(x, y);
                if (IsCellPartOfRoom(cellPos))
                {
                    cellsToDelete.Add(cellPos);
                }
            }
        }

        if (DEBUG_LOGGING)
        {
        }

        // РЈРґР°Р»СЏРµРј РІСЃРµ РєР»РµС‚РєРё
        foreach (Vector2Int cellPos in cellsToDelete)
        {
            DeleteCellInternal(cellPos);
        }

        if (DEBUG_LOGGING)
        {
        }

        // РџРµСЂРµСЃС‡РёС‚С‹РІР°РµРј РїРµСЂРёРјРµС‚СЂ РѕРґРёРЅ СЂР°Р· РїРѕСЃР»Рµ РІСЃРµС… СѓРґР°Р»РµРЅРёР№
        // Р’РђР–РќРћ: РџРµСЂРµРґР°РµРј isDeletion=true С‡С‚РѕР±С‹ РќР• РІС‹Р·С‹РІР°С‚СЊ ClosePerimeterOrthogonally
        RecalculatePerimeter(isDeletion: true);
        UpdateGhostsAfterDeletion();

        if (DEBUG_LOGGING)
        {
        }
    }

    /// <summary>
    /// РЈРґР°Р»РёС‚СЊ РєР»РµС‚РєСѓ Рё РїРµСЂРµСЃС‡РёС‚Р°С‚СЊ РїРµСЂРёРјРµС‚СЂ (РґР»СЏ РѕРґРёРЅРѕС‡РЅРѕРіРѕ СѓРґР°Р»РµРЅРёСЏ)
    /// </summary>
    void DeleteCell(Vector2Int cellPos)
    {
        DeleteCellInternal(cellPos);
        RecalculatePerimeter(isDeletion: true);
        UpdateGhostsAfterDeletion();
    }

    /// <summary>
    /// Р’РЅСѓС‚СЂРµРЅРЅРёР№ РјРµС‚РѕРґ СѓРґР°Р»РµРЅРёСЏ РєР»РµС‚РєРё (Р±РµР· РїРµСЂРµСЃС‡РµС‚Р° РїРµСЂРёРјРµС‚СЂР°)
    /// </summary>
    void DeleteCellInternal(Vector2Int cellPos)
    {
        // РџСЂРѕРІРµСЂСЏРµРј, РµСЃС‚СЊ Р»Рё СЌС‚Р° РєР»РµС‚РєР° РІ РїРµСЂРёРјРµС‚СЂРµ РёР»Рё РїРѕР»Сѓ
        bool wasInPerimeter = roomPerimeter.Contains(cellPos);
        bool wasInFloor = roomFloor.Contains(cellPos);

        if (!wasInPerimeter && !wasInFloor)
        {
            return;
        }


        // Р”РѕР±Р°РІР»СЏРµРј РІ СЃРїРёСЃРѕРє СѓРґР°Р»РµРЅРЅС‹С… РґР»СЏ РѕС‚СЃР»РµР¶РёРІР°РЅРёСЏ
        if (!deletedCells.Contains(cellPos))
        {
            deletedCells.Add(cellPos);
        }

        // РЈРґР°Р»СЏРµРј РёР· СЃРїРёСЃРєРѕРІ РїРµСЂРёРјРµС‚СЂР° Рё РїРѕР»Р°
        // РџСЂРµС„Р°Р±С‹ СѓРґР°Р»СЏСЋС‚СЃСЏ Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРё РїСЂРё РІС‹Р·РѕРІРµ UpdateGhostsAfterDeletion
        roomPerimeter.Remove(cellPos);
        roomFloor.Remove(cellPos);
    }

    /// <summary>
    /// РџРµСЂРµСЃС‡РёС‚Р°С‚СЊ РїРµСЂРёРјРµС‚СЂ РїРѕСЃР»Рµ РґРѕР±Р°РІР»РµРЅРёСЏ/СѓРґР°Р»РµРЅРёСЏ РєР»РµС‚РѕРє
    /// </summary>
    /// <param name="isDeletion">True РµСЃР»Рё СЌС‚Рѕ РѕРїРµСЂР°С†РёСЏ СѓРґР°Р»РµРЅРёСЏ (РќР• РІС‹Р·С‹РІР°С‚СЊ ClosePerimeterOrthogonally)</param>
    void RecalculatePerimeter(bool isDeletion = false)
    {
        // РЎРѕР±РёСЂР°РµРј РІСЃРµ РѕСЃС‚Р°РІС€РёРµСЃСЏ РєР»РµС‚РєРё (РїРµСЂРёРјРµС‚СЂ + РїРѕР»)
        HashSet<Vector2Int> allCells = new HashSet<Vector2Int>();
        foreach (Vector2Int pos in roomPerimeter)
        {
            allCells.Add(pos);
        }
        foreach (Vector2Int pos in roomFloor)
        {
            allCells.Add(pos);
        }

        // Р’СЂРµРјРµРЅРЅС‹Рµ СЃРїРёСЃРєРё РґР»СЏ РЅРѕРІРѕРіРѕ РїРµСЂРёРјРµС‚СЂР° Рё РїРѕР»Р°
        List<Vector2Int> newPerimeter = new List<Vector2Int>();
        List<Vector2Int> newFloor = new List<Vector2Int>();

        // Р”Р»СЏ РєР°Р¶РґРѕР№ РєР»РµС‚РєРё РѕРїСЂРµРґРµР»СЏРµРј, СЏРІР»СЏРµС‚СЃСЏ Р»Рё РѕРЅР° РїРµСЂРёРјРµС‚СЂРѕРј
        foreach (Vector2Int pos in allCells)
        {
            // РљР»РµС‚РєР° - РїРµСЂРёРјРµС‚СЂ, РµСЃР»Рё С…РѕС‚СЏ Р±С‹ РѕРґРЅР° РёР· СЃРѕСЃРµРґРЅРёС… РєР»РµС‚РѕРє РїСѓСЃС‚Р°СЏ РёР»Рё СѓРґР°Р»РµРЅР°
            bool hasEmptyNeighbor = false;

            Vector2Int[] neighbors = new Vector2Int[]
            {
                new Vector2Int(pos.x - 1, pos.y),     // left
                new Vector2Int(pos.x + 1, pos.y),     // right
                new Vector2Int(pos.x, pos.y - 1),     // down
                new Vector2Int(pos.x, pos.y + 1)      // up
            };

            foreach (Vector2Int neighbor in neighbors)
            {
                // РљР»РµС‚РєР° - РїРµСЂРёРјРµС‚СЂ, РµСЃР»Рё С…РѕС‚СЏ Р±С‹ РѕРґРёРЅ СЃРѕСЃРµРґ РЅРµ РІС…РѕРґРёС‚ РІ РєРѕРјРЅР°С‚Сѓ
                // (СѓРґР°Р»РµРЅРЅС‹Рµ РєР»РµС‚РєРё СѓР¶Рµ РЅРµ РІ allCells, РїРѕСЌС‚РѕРјСѓ РїСЂРѕРІРµСЂРєР° deletedCells РёР·Р±С‹С‚РѕС‡РЅР°)
                if (!allCells.Contains(neighbor))
                {
                    hasEmptyNeighbor = true;
                    break;
                }
            }

            if (hasEmptyNeighbor)
            {
                newPerimeter.Add(pos);
            }
            else
            {
                newFloor.Add(pos);
            }
        }

        // РћР±РЅРѕРІР»СЏРµРј СЃРїРёСЃРєРё
        roomPerimeter.Clear();
        roomPerimeter.AddRange(newPerimeter);

        roomFloor.Clear();
        roomFloor.AddRange(newFloor);

        if (DEBUG_LOGGING)
        {
        }

        // РЎРћРҐР РђРќРЇР•Рњ РёСЃС…РѕРґРЅС‹Р№ РїРѕР» РџР•Р Р•Р” Р·Р°РјС‹РєР°РЅРёРµРј РїРµСЂРёРјРµС‚СЂР°
        // Р­С‚Рё РєР»РµС‚РєРё РґРѕР»Р¶РЅС‹ Р’РЎР•Р“Р”Рђ РѕСЃС‚Р°РІР°С‚СЊСЃСЏ РїРѕР»РѕРј, РґР°Р¶Рµ РїРѕСЃР»Рµ РґРѕР±Р°РІР»РµРЅРёСЏ СЃС‚РµРЅ
        HashSet<Vector2Int> originalFloor = new HashSet<Vector2Int>(roomFloor);

        int addedWalls = 0;

        // РљР РРўРР§РќРћ: ClosePerimeterOrthogonally() РІС‹Р·С‹РІР°РµРј РўРћР›Р¬РљРћ РїСЂРё РґРѕР±Р°РІР»РµРЅРёРё РєР»РµС‚РѕРє!
        // РџСЂРё СѓРґР°Р»РµРЅРёРё РєР»РµС‚РѕРє РќР• РЅСѓР¶РЅРѕ Р·Р°РјС‹РєР°С‚СЊ РґРёР°РіРѕРЅР°Р»СЊРЅС‹Рµ СЃРѕРµРґРёРЅРµРЅРёСЏ - СЌС‚Рѕ Р»РѕРјР°РµС‚ РїРµСЂРёРјРµС‚СЂ!
        if (!isDeletion && deletedCells.Count == 0)
        {
            if (DEBUG_LOGGING)
            {
            }
            addedWalls = ClosePerimeterOrthogonally();

            if (DEBUG_LOGGING)
            {
            }
        }
        else
        {
            if (DEBUG_LOGGING)
            {
            }
        }

        // Р’РђР–РќРћ: РџРѕСЃР»Рµ РґРѕР±Р°РІР»РµРЅРёСЏ РЅРѕРІС‹С… СЃС‚РµРЅ, РЅРµРєРѕС‚РѕСЂС‹Рµ РєР»РµС‚РєРё РїРѕР»Р° РјРѕРіР»Рё СЃС‚Р°С‚СЊ СЃС‚РµРЅР°РјРё
        // РџРµСЂРµСЃС‡РёС‚С‹РІР°РµРј РїРµСЂРёРјРµС‚СЂ/РїРѕР» РµС‰Рµ СЂР°Р·, РќРћ СЃРѕС…СЂР°РЅСЏРµРј РёСЃС…РѕРґРЅС‹Р№ РїРѕР»
        if (addedWalls > 0)
        {
            HashSet<Vector2Int> allCellsAfter = new HashSet<Vector2Int>();
            foreach (Vector2Int pos in roomPerimeter)
            {
                allCellsAfter.Add(pos);
            }
            foreach (Vector2Int pos in roomFloor)
            {
                allCellsAfter.Add(pos);
            }

            List<Vector2Int> finalPerimeter = new List<Vector2Int>();
            List<Vector2Int> finalFloor = new List<Vector2Int>();

            foreach (Vector2Int pos in allCellsAfter)
            {
                // РљР РРўРР§РќРћ: Р•СЃР»Рё РєР»РµС‚РєР° Р±С‹Р»Р° РїРѕР»РѕРј Р”Рћ Р·Р°РјС‹РєР°РЅРёСЏ РїРµСЂРёРјРµС‚СЂР° - РѕРЅР° РћРЎРўРђР•РўРЎРЇ РїРѕР»РѕРј
                if (originalFloor.Contains(pos))
                {
                    finalFloor.Add(pos);
                    continue;
                }

                // РљР»РµС‚РєР° - СЃС‚РµРЅР°, РµСЃР»Рё С…РѕС‚СЏ Р±С‹ РѕРґРёРЅ СЃРѕСЃРµРґ РїСѓСЃС‚РѕР№ РёР»Рё СѓРґР°Р»РµРЅ
                bool hasEmptyNeighbor = false;
                Vector2Int[] neighbors = new Vector2Int[]
                {
                    pos + Vector2Int.left,
                    pos + Vector2Int.right,
                    pos + Vector2Int.down,
                    pos + Vector2Int.up
                };

                foreach (Vector2Int neighbor in neighbors)
                {
                    // РљР»РµС‚РєР° - РїРµСЂРёРјРµС‚СЂ, РµСЃР»Рё С…РѕС‚СЏ Р±С‹ РѕРґРёРЅ СЃРѕСЃРµРґ РЅРµ РІС…РѕРґРёС‚ РІ РєРѕРјРЅР°С‚Сѓ
                    // (СѓРґР°Р»РµРЅРЅС‹Рµ РєР»РµС‚РєРё СѓР¶Рµ РЅРµ РІ allCellsAfter, РїСЂРѕРІРµСЂРєР° deletedCells РёР·Р±С‹С‚РѕС‡РЅР°)
                    if (!allCellsAfter.Contains(neighbor))
                    {
                        hasEmptyNeighbor = true;
                        break;
                    }
                }

                if (hasEmptyNeighbor)
                {
                    finalPerimeter.Add(pos);
                }
                else
                {
                    finalFloor.Add(pos);
                }
            }

            roomPerimeter.Clear();
            roomPerimeter.AddRange(finalPerimeter);
            roomFloor.Clear();
            roomFloor.AddRange(finalFloor);

        }

        // РљР РРўРР§РќРћ: РќР°С…РѕРґРёРј РІРЅСѓС‚СЂРµРЅРЅРёРµ СѓРіР»С‹ РІ Р·Р°РІРёСЃРёРјРѕСЃС‚Рё РѕС‚ С‚РёРїР° РѕРїРµСЂР°С†РёРё
        List<Vector2Int> innerCorners = new List<Vector2Int>();

        if (isDeletion && deletedCells.Count > 0)
        {
            // РЈРґР°Р»РµРЅРёРµ - Р’РђР–РќРћ: РЅСѓР¶РЅРѕ РЅР°Р№С‚Рё РћР‘Рђ С‚РёРїР° СѓРіР»РѕРІ!
            // 1. РЎС‚Р°СЂС‹Рµ СѓРіР»С‹ РѕС‚ РѕР±СЉРµРґРёРЅРµРЅРЅС‹С… РѕР±Р»Р°СЃС‚РµР№ (С‡С‚РѕР±С‹ РЅРµ РїРѕС‚РµСЂСЏС‚СЊ РёС…)
            // 2. РќРѕРІС‹Рµ СѓРіР»С‹ РІ РјРµСЃС‚Р°С… РІС‹СЂРµР·РѕРІ
            if (DEBUG_LOGGING)
            {
            }

            HashSet<Vector2Int> allInnerCorners = new HashSet<Vector2Int>();

            // РќР°С…РѕРґРёРј СѓРіР»С‹ РѕС‚ РѕР±СЉРµРґРёРЅРµРЅРЅС‹С… РѕР±Р»Р°СЃС‚РµР№
            List<Vector2Int> mergedAreaCorners = FindInnerCornersForMergedAreas();
            if (DEBUG_LOGGING)
            {
            }
            foreach (var corner in mergedAreaCorners)
            {
                allInnerCorners.Add(corner);
            }

            // РќР°С…РѕРґРёРј СѓРіР»С‹ СЂСЏРґРѕРј СЃ РІС‹СЂРµР·Р°РјРё
            List<Vector2Int> cutoutCorners = FindInnerCorners();
            if (DEBUG_LOGGING)
            {
            }
            foreach (var corner in cutoutCorners)
            {
                allInnerCorners.Add(corner);
            }

            innerCorners.AddRange(allInnerCorners);
            if (DEBUG_LOGGING)
            {
            }
        }
        else if (!isDeletion)
        {
            // Р”РѕР±Р°РІР»РµРЅРёРµ - РёС‰РµРј СѓРіР»С‹ РїРѕ РїСЂРёР·РЅР°РєСѓ РїСѓСЃС‚РѕР№ РґРёР°РіРѕРЅР°Р»Рё
            if (DEBUG_LOGGING)
            {
            }
            innerCorners = FindInnerCornersForMergedAreas();
        }
        else
        {
            if (DEBUG_LOGGING)
            {
            }
        }

        // Р”РѕР±Р°РІР»СЏРµРј РЅР°Р№РґРµРЅРЅС‹Рµ РІРЅСѓС‚СЂРµРЅРЅРёРµ СѓРіР»С‹
        if (innerCorners.Count > 0)
        {
            // РЎРћРҐР РђРќРЇР•Рњ РЅР°Р№РґРµРЅРЅС‹Рµ СѓРіР»С‹ РІ РїРµСЂРµРјРµРЅРЅСѓСЋ РєР»Р°СЃСЃР° РґР»СЏ РёСЃРїРѕР»СЊР·РѕРІР°РЅРёСЏ РІ FinalizeBuild
            savedInnerCorners.Clear();
            savedInnerCorners.AddRange(innerCorners);
            if (DEBUG_LOGGING)
            {
            }

            foreach (Vector2Int corner in innerCorners)
            {
                if (!roomPerimeter.Contains(corner))
                {
                    roomPerimeter.Add(corner);
                    if (DEBUG_LOGGING)
                    {
                    }
                }

                // РљР РРўРР§РќРћ: РЈРґР°Р»СЏРµРј СЌС‚Сѓ РїРѕР·РёС†РёСЋ РёР· РїРѕР»Р°, РµСЃР»Рё РѕРЅР° С‚Р°Рј Р±С‹Р»Р°!
                if (roomFloor.Contains(corner))
                {
                    roomFloor.Remove(corner);
                    if (DEBUG_LOGGING)
                    {
                    }
                }
            }

            if (DEBUG_LOGGING)
            {
            }
        }


        // Р’РђР–РќРћ: РћС‡РёС‰Р°РµРј deletedCells РїРѕСЃР»Рµ РїРµСЂРµСЃС‡РµС‚Р° РїРµСЂРёРјРµС‚СЂР°
        // РџРѕСЃР»Рµ РїРµСЂРµСЃС‡РµС‚Р° РїРµСЂРёРјРµС‚СЂР° РёРЅС„РѕСЂРјР°С†РёСЏ Рѕ СѓРґР°Р»РµРЅРёСЏС… РЈР–Р• СѓС‡С‚РµРЅР° РІ РЅРѕРІРѕРј РїРµСЂРёРјРµС‚СЂРµ
        // РќРµ РЅСѓР¶РЅРѕ С…СЂР°РЅРёС‚СЊ РёСЃС‚РѕСЂРёСЋ - СЌС‚Рѕ РїСЂРёРІРѕРґРёС‚ Рє РѕС€РёР±РєР°Рј РїСЂРё РїРѕРІС‚РѕСЂРЅС‹С… РѕРїРµСЂР°С†РёСЏС…
        deletedCells.Clear();
        if (DEBUG_LOGGING)
        {
        }
    }

    /// <summary>
    /// Р—Р°РјС‹РєР°РµС‚ РїРµСЂРёРјРµС‚СЂ РѕСЂС‚РѕРіРѕРЅР°Р»СЊРЅРѕ, Р·Р°РїРѕР»РЅСЏСЏ РґРёР°РіРѕРЅР°Р»СЊРЅС‹Рµ РїСЂРѕР±РµР»С‹ (РћРџРўРРњРР—РР РћР’РђРќРћ)
    /// Р Р°Р±РѕС‚Р°РµС‚ РёС‚РµСЂР°С‚РёРІРЅРѕ, РїРѕРєР° РІСЃРµ РґРёР°РіРѕРЅР°Р»СЊРЅС‹Рµ СЃРѕРµРґРёРЅРµРЅРёСЏ РЅРµ Р±СѓРґСѓС‚ СѓСЃС‚СЂР°РЅРµРЅС‹
    /// Р’РѕР·РІСЂР°С‰Р°РµС‚ РєРѕР»РёС‡РµСЃС‚РІРѕ РґРѕР±Р°РІР»РµРЅРЅС‹С… СЃС‚РµРЅ
    /// Р’РђР–РќРћ: deletedCells СЃРѕРґРµСЂР¶РёС‚ РўРћР›Р¬РљРћ РєР»РµС‚РєРё РёР· С‚РµРєСѓС‰РµР№ РѕРїРµСЂР°С†РёРё (РѕС‡РёС‰Р°РµС‚СЃСЏ РїРѕСЃР»Рµ РєР°Р¶РґРѕРіРѕ RecalculatePerimeter)
    /// </summary>
    int ClosePerimeterOrthogonally()
    {
        HashSet<Vector2Int> deletedSet = new HashSet<Vector2Int>(deletedCells);

        if (DEBUG_LOGGING)
        {
            if (deletedSet.Count > 0)
            {
                foreach (var del in deletedSet)
                {
                }
            }
        }

        int totalAddedWalls = 0;
        int iteration = 0;
        int maxIterations = 20; // РћРџРўРРњРР—РђР¦РРЇ: РЎРЅРёР¶РµРЅРѕ СЃ 100 РґРѕ 20 РґР»СЏ РїСЂРѕРёР·РІРѕРґРёС‚РµР»СЊРЅРѕСЃС‚Рё

        while (iteration < maxIterations)
        {
            iteration++;
            HashSet<Vector2Int> wallSet = new HashSet<Vector2Int>(roomPerimeter);
            HashSet<Vector2Int> floorSet = new HashSet<Vector2Int>(roomFloor);
            HashSet<Vector2Int> allRoomCells = new HashSet<Vector2Int>(wallSet);
            allRoomCells.UnionWith(floorSet);

            List<Vector2Int> wallsToAdd = new List<Vector2Int>();

            // РџСЂРѕРІРµСЂСЏРµРј РєР°Р¶РґСѓСЋ СЃС‚РµРЅСѓ РЅР° РґРёР°РіРѕРЅР°Р»СЊРЅС‹Рµ СЃРѕРµРґРёРЅРµРЅРёСЏ
            foreach (Vector2Int wall in roomPerimeter)
            {
                // РџСЂРѕРІРµСЂСЏРµРј 4 РґРёР°РіРѕРЅР°Р»СЊРЅС‹С… РЅР°РїСЂР°РІР»РµРЅРёСЏ
                Vector2Int[] diagonals = new Vector2Int[]
                {
                    new Vector2Int(-1, 1),   // TopLeft
                    new Vector2Int(1, 1),    // TopRight
                    new Vector2Int(-1, -1),  // BottomLeft
                    new Vector2Int(1, -1)    // BottomRight
                };

                Vector2Int[][] orthogonalPairs = new Vector2Int[][]
                {
                    new Vector2Int[] { Vector2Int.up, Vector2Int.left },           // РґР»СЏ TopLeft
                    new Vector2Int[] { Vector2Int.up, Vector2Int.right },          // РґР»СЏ TopRight
                    new Vector2Int[] { Vector2Int.down, Vector2Int.left },         // РґР»СЏ BottomLeft
                    new Vector2Int[] { Vector2Int.down, Vector2Int.right }         // РґР»СЏ BottomRight
                };

                for (int i = 0; i < diagonals.Length; i++)
                {
                    Vector2Int diagonalPos = wall + diagonals[i];

                    // РџСЂРѕРІРµСЂСЏРµРј РўРћР›Р¬РљРћ РЅР° СЃС‚РµРЅСѓ РЅР° РґРёР°РіРѕРЅР°Р»Рё (РЅРµ РїРѕР»!)
                    // Р­С‚Рѕ РІР°Р¶РЅРѕ РґР»СЏ РІС‹СЂРµР·РѕРІ, РіРґРµ РјРµР¶РґСѓ СЃС‚РµРЅР°РјРё РјРѕР¶РµС‚ Р±С‹С‚СЊ СѓРґР°Р»РµРЅРЅР°СЏ РєР»РµС‚РєР°
                    if (wallSet.Contains(diagonalPos))
                    {
                        Vector2Int ortho1 = wall + orthogonalPairs[i][0];
                        Vector2Int ortho2 = wall + orthogonalPairs[i][1];

                        // РџСЂРѕРІРµСЂСЏРµРј РµСЃС‚СЊ Р»Рё РѕСЂС‚РѕРіРѕРЅР°Р»СЊРЅС‹Р№ РїСѓС‚СЊ РјРµР¶РґСѓ СЃС‚РµРЅР°РјРё
                        bool hasOrtho1Wall = wallSet.Contains(ortho1);
                        bool hasOrtho2Wall = wallSet.Contains(ortho2);

                        // Р•СЃР»Рё РЅРё РѕРґРЅРѕР№ РёР· РїСЂРѕРјРµР¶СѓС‚РѕС‡РЅС‹С… СЃС‚РµРЅ РЅРµС‚ - РґРёР°РіРѕРЅР°Р»СЊРЅРѕРµ СЃРѕРµРґРёРЅРµРЅРёРµ!
                        if (!hasOrtho1Wall && !hasOrtho2Wall)
                        {
                            // РљР РРўРР§Р•РЎРљРђРЇ РџР РћР’Р•Р РљРђ: Р—Р°РјС‹РєР°С‚СЊ РўРћР›Р¬РљРћ РµСЃР»Рё СЌС‚Рѕ РґРёР°РіРѕРЅР°Р»СЊРЅРѕРµ СЃРѕРµРґРёРЅРµРЅРёРµ СЂСЏРґРѕРј СЃ Р’Р«Р Р•Р—РћРњ
                            // Р•СЃР»Рё РґР°Р»РµРєРѕ РѕС‚ СѓРґР°Р»РµРЅРЅС‹С… РєР»РµС‚РѕРє - СЌС‚Рѕ РІРЅРµС€РЅРёР№ СѓРіРѕР» РёСЃС…РѕРґРЅРѕР№ С„РѕСЂРјС‹, РќР• Р·Р°РјС‹РєР°РµРј!
                            bool isWallNearDeletion = IsPositionNearDeletion(wall, deletedSet, 2);
                            bool isDiagNearDeletion = IsPositionNearDeletion(diagonalPos, deletedSet, 2);
                            bool isNearDeletion = isWallNearDeletion || isDiagNearDeletion;

                            if (DEBUG_LOGGING)
                            {
                            }

                            if (!isNearDeletion)
                            {
                                // Р”РёР°РіРѕРЅР°Р»СЊРЅРѕРµ СЃРѕРµРґРёРЅРµРЅРёРµ РґР°Р»РµРєРѕ РѕС‚ РІС‹СЂРµР·РѕРІ - СЌС‚Рѕ РІРЅРµС€РЅРёР№ СѓРіРѕР», РќР• Р·Р°РјС‹РєР°РµРј
                                if (DEBUG_LOGGING)
                                {
                                }
                                continue;
                            }

                            if (DEBUG_LOGGING)
                            {
                            }

                            // РќСѓР¶РЅРѕ РґРѕР±Р°РІРёС‚СЊ СЃС‚РµРЅСѓ "РїРѕ С…РѕРґСѓ РЅР°РїСЂР°РІР»РµРЅРёСЏ" РѕРґРЅРѕР№ РёР· СЃС‚РµРЅ
                            // РћРїСЂРµРґРµР»СЏРµРј РєР°РєР°СЏ РёР· РґРІСѓС… РїРѕР·РёС†РёР№ Р»СѓС‡С€Рµ РїРѕРґС…РѕРґРёС‚
                            bool ortho1IsDeleted = deletedSet.Contains(ortho1);
                            bool ortho2IsDeleted = deletedSet.Contains(ortho2);
                            bool ortho1IsFloor = floorSet.Contains(ortho1);
                            bool ortho2IsFloor = floorSet.Contains(ortho2);

                            Vector2Int posToAdd;

                            // РџР РђР’РР›Рћ: РґРѕР±Р°РІР»СЏРµРј СЃС‚РµРЅСѓ РІ РїРѕР·РёС†РёСЋ, РєРѕС‚РѕСЂР°СЏ РќР• СѓРґР°Р»РµРЅР° Рё РќР• СЏРІР»СЏРµС‚СЃСЏ РїРѕР»РѕРј
                            // Р­С‚Рѕ СЃРѕР·РґР°СЃС‚ "РїСЂРѕРґРѕР»Р¶РµРЅРёРµ" СЃС‚РµРЅС‹ РїРѕ РµС‘ РЅР°РїСЂР°РІР»РµРЅРёСЋ
                            if (ortho1IsDeleted || ortho1IsFloor)
                            {
                                // РџРµСЂРІР°СЏ РїРѕР·РёС†РёСЏ Р·Р°РЅСЏС‚Р° - РёСЃРїРѕР»СЊР·СѓРµРј РІС‚РѕСЂСѓСЋ
                                if (!ortho2IsDeleted && !ortho2IsFloor)
                                {
                                    posToAdd = ortho2;
                                }
                                else
                                {
                                    // РћР±Рµ РїРѕР·РёС†РёРё Р·Р°РЅСЏС‚С‹ - РїСЂРѕРїСѓСЃРєР°РµРј (СЌС‚Рѕ СѓРіРѕР» РєРѕРјРЅР°С‚С‹)
                                    continue;
                                }
                            }
                            else if (ortho2IsDeleted || ortho2IsFloor)
                            {
                                // Р’С‚РѕСЂР°СЏ РїРѕР·РёС†РёСЏ Р·Р°РЅСЏС‚Р° - РёСЃРїРѕР»СЊР·СѓРµРј РїРµСЂРІСѓСЋ
                                posToAdd = ortho1;
                            }
                            else
                            {
                                // РћР±Рµ РїРѕР·РёС†РёРё СЃРІРѕР±РѕРґРЅС‹
                                // РџР РћР’Р•Р РљРђ: РµСЃР»Рё РѕР±Рµ РїРѕР·РёС†РёРё РќР• С‡Р°СЃС‚СЊ РєРѕРјРЅР°С‚С‹ (РЅРµ СЃС‚РµРЅС‹, РЅРµ РїРѕР») - СЌС‚Рѕ РІРЅРµС€РЅРёР№ СѓРіРѕР»
                                // РќР° РІРЅРµС€РЅРёС… СѓРіР»Р°С… РґРёР°РіРѕРЅР°Р»СЊРЅРѕРµ СЃРѕРµРґРёРЅРµРЅРёРµ - СЌС‚Рѕ РќРћР РњРђР›Р¬РќРћ, РЅРµ Р·Р°РїРѕР»РЅСЏРµРј
                                bool ortho1IsInRoom = allRoomCells.Contains(ortho1);
                                bool ortho2IsInRoom = allRoomCells.Contains(ortho2);

                                if (!ortho1IsInRoom && !ortho2IsInRoom)
                                {
                                    // РћР±Рµ РїРѕР·РёС†РёРё Р·Р° РїСЂРµРґРµР»Р°РјРё РєРѕРјРЅР°С‚С‹ - СЌС‚Рѕ РІРЅРµС€РЅРёР№ СѓРіРѕР» РєРѕРјРЅР°С‚С‹
                                    // РџСЂРѕРїСѓСЃРєР°РµРј! Р”РёР°РіРѕРЅР°Р»СЊРЅС‹Рµ СЃРѕРµРґРёРЅРµРЅРёСЏ РЅР° РІРЅРµС€РЅРёС… СѓРіР»Р°С… - СЌС‚Рѕ РЅРѕСЂРјР°Р»СЊРЅРѕ
                                    continue;
                                }

                                // РҐРѕС‚СЏ Р±С‹ РѕРґРЅР° РїРѕР·РёС†РёСЏ РІРЅСѓС‚СЂРё РєРѕРјРЅР°С‚С‹ - СЌС‚Рѕ РІРЅСѓС‚СЂРµРЅРЅРёР№ РІС‹СЂРµР·
                                // Р’С‹Р±РёСЂР°РµРј С‚Сѓ, С‡С‚Рѕ РїСЂРѕРґРѕР»Р¶Р°РµС‚ РЅР°РїСЂР°РІР»РµРЅРёРµ СЃС‚РµРЅС‹
                                int ortho1WallCount = CountWallNeighbors(ortho1, wallSet);
                                int ortho2WallCount = CountWallNeighbors(ortho2, wallSet);

                                // РџСЂРµРґРїРѕС‡РёС‚Р°РµРј РїРѕР·РёС†РёСЋ СЃ Р±РѕР»СЊС€РёРј РєРѕР»РёС‡РµСЃС‚РІРѕРј СЃС‚РµРЅ-СЃРѕСЃРµРґРµР№
                                if (ortho1WallCount > ortho2WallCount)
                                    posToAdd = ortho1;
                                else if (ortho2WallCount > ortho1WallCount)
                                    posToAdd = ortho2;
                                else
                                    posToAdd = ortho1; // РџРѕ СѓРјРѕР»С‡Р°РЅРёСЋ РїРµСЂРІР°СЏ
                            }

                            // Р•СЃР»Рё РїРѕР·РёС†РёСЏ РµС‰Рµ РЅРµ РґРѕР±Р°РІР»РµРЅР°
                            if (!wallSet.Contains(posToAdd) && !wallsToAdd.Contains(posToAdd))
                            {
                                wallsToAdd.Add(posToAdd);
                            }
                        }
                    }
                }
            }

            // Р•СЃР»Рё РЅРёС‡РµРіРѕ РЅРµ РґРѕР±Р°РІР»РµРЅРѕ - РІСЃРµ СЃРѕРµРґРёРЅРµРЅРёСЏ РѕСЂС‚РѕРіРѕРЅР°Р»СЊРЅС‹Рµ
            if (wallsToAdd.Count == 0)
                break;

            // Р”РѕР±Р°РІР»СЏРµРј РЅРѕРІС‹Рµ СЃС‚РµРЅС‹ РІ РїРµСЂРёРјРµС‚СЂ
            foreach (Vector2Int newWall in wallsToAdd)
            {
                roomPerimeter.Add(newWall);
                totalAddedWalls++;
            }
        }

        if (iteration >= maxIterations)
        {
        }

        return totalAddedWalls;
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂСЏРµС‚, РµСЃС‚СЊ Р»Рё Сѓ РїРѕР·РёС†РёРё СЃРѕСЃРµРґРё РёР· РєРѕРјРЅР°С‚С‹ (СЃС‚РµРЅС‹ РёР»Рё РїРѕР»)
    /// </summary>
    bool HasRoomNeighbor(Vector2Int pos, HashSet<Vector2Int> roomCells)
    {
        Vector2Int[] neighbors = new Vector2Int[]
        {
            pos + Vector2Int.up,
            pos + Vector2Int.down,
            pos + Vector2Int.left,
            pos + Vector2Int.right
        };

        foreach (Vector2Int neighbor in neighbors)
        {
            if (roomCells.Contains(neighbor))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// РЎС‡РёС‚Р°РµС‚ РєРѕР»РёС‡РµСЃС‚РІРѕ СЃС‚РµРЅ-СЃРѕСЃРµРґРµР№ Сѓ РґР°РЅРЅРѕР№ РїРѕР·РёС†РёРё
    /// </summary>
    int CountWallNeighbors(Vector2Int pos, HashSet<Vector2Int> wallSet)
    {
        int count = 0;
        Vector2Int[] neighbors = new Vector2Int[]
        {
            pos + Vector2Int.up,
            pos + Vector2Int.down,
            pos + Vector2Int.left,
            pos + Vector2Int.right
        };

        foreach (Vector2Int neighbor in neighbors)
        {
            if (wallSet.Contains(neighbor))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂСЏРµС‚, РЅР°С…РѕРґРёС‚СЃСЏ Р»Рё РїРѕР·РёС†РёСЏ СЂСЏРґРѕРј СЃ СѓРґР°Р»РµРЅРЅС‹РјРё РєР»РµС‚РєР°РјРё (РІ РїСЂРµРґРµР»Р°С… radius)
    /// РСЃРїРѕР»СЊР·СѓРµС‚СЃСЏ РґР»СЏ РѕРїСЂРµРґРµР»РµРЅРёСЏ, СЏРІР»СЏРµС‚СЃСЏ Р»Рё РґРёР°РіРѕРЅР°Р»СЊРЅРѕРµ СЃРѕРµРґРёРЅРµРЅРёРµ С‡Р°СЃС‚СЊСЋ РІС‹СЂРµР·Р°
    /// </summary>
    bool IsPositionNearDeletion(Vector2Int pos, HashSet<Vector2Int> deletedSet, int radius)
    {
        // РџСЂРѕРІРµСЂСЏРµРј РІСЃРµ РїРѕР·РёС†РёРё РІ РїСЂРµРґРµР»Р°С… radius
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                Vector2Int checkPos = pos + new Vector2Int(dx, dy);
                if (deletedSet.Contains(checkPos))
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// РќР°Р№С‚Рё РїРѕР·РёС†РёРё РІРЅСѓС‚СЂРµРЅРЅРёС… СѓРіР»РѕРІ (РІ РјРµСЃС‚Р°С… РІС‹СЂРµР·РѕРІ)
    /// Р’РЅСѓС‚СЂРµРЅРЅРёРµ СѓРіР»С‹ РјРѕРіСѓС‚ Р±С‹С‚СЊ РєР°Рє РїСѓСЃС‚С‹РјРё РїРѕР·РёС†РёСЏРјРё, С‚Р°Рє Рё РїРѕР·РёС†РёСЏРјРё РџРћР›Рђ РєРѕС‚РѕСЂС‹Рµ РѕРєСЂСѓР¶РµРЅС‹ СЃС‚РµРЅР°РјРё РїРѕРґ РїСЂСЏРјС‹Рј СѓРіР»РѕРј
    /// </summary>
    List<Vector2Int> FindInnerCorners()
    {
        List<Vector2Int> innerCorners = new List<Vector2Int>();

        // РЎРѕР·РґР°РµРј HashSet РґР»СЏ Р±С‹СЃС‚СЂРѕР№ РїСЂРѕРІРµСЂРєРё
        HashSet<Vector2Int> wallSet = new HashSet<Vector2Int>(roomPerimeter);
        HashSet<Vector2Int> floorSet = new HashSet<Vector2Int>(roomFloor);
        HashSet<Vector2Int> deletedSet = new HashSet<Vector2Int>(deletedCells);

        HashSet<Vector2Int> candidatePositions = new HashSet<Vector2Int>();

        // 1. РЎРѕР±РёСЂР°РµРј РџРЈРЎРўР«Р• РїРѕР·РёС†РёРё СЂСЏРґРѕРј СЃ СѓРґР°Р»РµРЅРЅС‹РјРё РєР»РµС‚РєР°РјРё
        foreach (Vector2Int deleted in deletedCells)
        {
            Vector2Int[] neighbors = new Vector2Int[]
            {
                deleted + Vector2Int.up,
                deleted + Vector2Int.down,
                deleted + Vector2Int.left,
                deleted + Vector2Int.right,
                deleted + new Vector2Int(-1, 1),   // РґРёР°РіРѕРЅР°Р»Рё С‚РѕР¶Рµ
                deleted + new Vector2Int(1, 1),
                deleted + new Vector2Int(-1, -1),
                deleted + new Vector2Int(1, -1)
            };

            foreach (Vector2Int neighbor in neighbors)
            {
                // Р•СЃР»Рё СЌС‚Рѕ РЅРµ СЃС‚РµРЅР° - РєР°РЅРґРёРґР°С‚ РЅР° РІРЅСѓС‚СЂРµРЅРЅРёР№ СѓРіРѕР» (РјРѕР¶РµС‚ Р±С‹С‚СЊ РїРѕР»РѕРј РёР»Рё РїСѓСЃС‚РѕР№)
                if (!wallSet.Contains(neighbor))
                {
                    candidatePositions.Add(neighbor);
                }
            }
        }

        // 2. Р’РђР–РќРћ: РўР°РєР¶Рµ РґРѕР±Р°РІР»СЏРµРј РІСЃРµ РєР»РµС‚РєРё РџРћР›Рђ РєР°Рє РєР°РЅРґРёРґР°С‚РѕРІ
        // РџРѕС‚РѕРјСѓ С‡С‚Рѕ РЅРµРєРѕС‚РѕСЂС‹Рµ РєР»РµС‚РєРё РїРѕР»Р° РЅР° СЃР°РјРѕРј РґРµР»Рµ РґРѕР»Р¶РЅС‹ Р±С‹С‚СЊ РІРЅСѓС‚СЂРµРЅРЅРёРјРё СѓРіР»Р°РјРё!
        foreach (Vector2Int floorPos in roomFloor)
        {
            candidatePositions.Add(floorPos);
        }

        // РџСЂРѕРІРµСЂСЏРµРј РєР°Р¶РґРѕРіРѕ РєР°РЅРґРёРґР°С‚Р° - СЏРІР»СЏРµС‚СЃСЏ Р»Рё РѕРЅ РІРЅСѓС‚СЂРµРЅРЅРёРј СѓРіР»РѕРј
        foreach (Vector2Int pos in candidatePositions)
        {
            if (IsPositionAnInnerCorner(pos, wallSet, floorSet, deletedSet))
            {
                innerCorners.Add(pos);
            }
        }

        return innerCorners;
    }

    /// <summary>
    /// РќР°Р№С‚Рё РїРѕР·РёС†РёРё РІРЅСѓС‚СЂРµРЅРЅРёС… СѓРіР»РѕРІ РґР»СЏ РѕР±СЉРµРґРёРЅРµРЅРЅС‹С… РѕР±Р»Р°СЃС‚РµР№ (Р‘Р•Р— СѓРґР°Р»РµРЅРёР№)
    /// Р’РЅСѓС‚СЂРµРЅРЅРёР№ СѓРіРѕР» РѕРїСЂРµРґРµР»СЏРµС‚СЃСЏ РїРѕ РїСЂРёР·РЅР°РєСѓ РїСѓСЃС‚РѕР№ РґРёР°РіРѕРЅР°Р»Рё РјРµР¶РґСѓ РґРІСѓРјСЏ РїРµСЂРїРµРЅРґРёРєСѓР»СЏСЂРЅС‹РјРё СЃС‚РµРЅР°РјРё
    /// РќР• С‚СЂРµР±СѓРµС‚ Р±Р»РёР·РѕСЃС‚Рё Рє СѓРґР°Р»РµРЅРЅС‹Рј РєР»РµС‚РєР°Рј - СЂР°Р±РѕС‚Р°РµС‚ РґР»СЏ СЃР»СѓС‡Р°СЏ РѕР±СЉРµРґРёРЅРµРЅРёСЏ РїСЂСЏРјРѕСѓРіРѕР»СЊРЅРёРєРѕРІ
    /// </summary>
    List<Vector2Int> FindInnerCornersForMergedAreas()
    {
        List<Vector2Int> innerCorners = new List<Vector2Int>();

        // РЎРѕР·РґР°РµРј HashSet РґР»СЏ Р±С‹СЃС‚СЂРѕР№ РїСЂРѕРІРµСЂРєРё
        HashSet<Vector2Int> wallSet = new HashSet<Vector2Int>(roomPerimeter);
        HashSet<Vector2Int> floorSet = new HashSet<Vector2Int>(roomFloor);
        HashSet<Vector2Int> allCells = new HashSet<Vector2Int>(wallSet);
        allCells.UnionWith(floorSet);

        if (DEBUG_LOGGING)
        {
        }

        // Р’РђР–РќРћ: РџСЂРѕРІРµСЂСЏРµРј РќР• РўРћР›Р¬РљРћ СЃС‚РµРЅС‹, РЅРѕ Рё РџРћР›!
        // РџСЂРё РѕР±СЉРµРґРёРЅРµРЅРёРё РѕР±Р»Р°СЃС‚РµР№ РІРЅСѓС‚СЂРµРЅРЅРёРµ СѓРіР»С‹ РјРѕРіСѓС‚ РѕРєР°Р·Р°С‚СЊСЃСЏ РЅР° РїРѕР»Сѓ
        List<Vector2Int> positionsToCheck = new List<Vector2Int>();
        positionsToCheck.AddRange(roomPerimeter);
        positionsToCheck.AddRange(roomFloor);

        // РџСЂРѕРІРµСЂСЏРµРј РєР°Р¶РґСѓСЋ РїРѕР·РёС†РёСЋ - РјРѕР¶РµС‚ Р»Рё РѕРЅР° Р±С‹С‚СЊ РІРЅСѓС‚СЂРµРЅРЅРёРј СѓРіР»РѕРј
        foreach (Vector2Int wallPos in positionsToCheck)
        {
            // РџРѕР»СѓС‡Р°РµРј СЃРѕСЃРµРґРµР№
            Vector2Int topPos = wallPos + Vector2Int.up;
            Vector2Int bottomPos = wallPos + Vector2Int.down;
            Vector2Int leftPos = wallPos + Vector2Int.left;
            Vector2Int rightPos = wallPos + Vector2Int.right;

            bool hasWallTop = wallSet.Contains(topPos);
            bool hasWallBottom = wallSet.Contains(bottomPos);
            bool hasWallLeft = wallSet.Contains(leftPos);
            bool hasWallRight = wallSet.Contains(rightPos);

            // РљР РРўРР§РќРћ: Р’РЅСѓС‚СЂРµРЅРЅРёР№ СѓРіРѕР» РґРѕР»Р¶РµРЅ РёРјРµС‚СЊ Р РћР’РќРћ 2 СЃС‚РµРЅС‹-СЃРѕСЃРµРґР° РїРѕРґ РїСЂСЏРјС‹Рј СѓРіР»РѕРј
            int wallNeighborCount = (hasWallTop ? 1 : 0) + (hasWallBottom ? 1 : 0) + (hasWallLeft ? 1 : 0) + (hasWallRight ? 1 : 0);

            if (wallNeighborCount != 2)
                continue;

            // РџСЂРѕРІРµСЂСЏРµРј, С‡С‚Рѕ 2 СЃС‚РµРЅС‹ РЅР°С…РѕРґСЏС‚СЃСЏ РїРѕРґ РїСЂСЏРјС‹Рј СѓРіР»РѕРј (РЅРµ РЅР°РїСЂРѕС‚РёРІ РґСЂСѓРі РґСЂСѓРіР°)
            bool hasVerticalPair = hasWallTop && hasWallBottom;
            bool hasHorizontalPair = hasWallLeft && hasWallRight;

            if (hasVerticalPair || hasHorizontalPair)
                continue;

            // Р•СЃР»Рё РґРѕС€Р»Рё СЃСЋРґР° - Сѓ РЅР°СЃ РµСЃС‚СЊ РєР°РЅРґРёРґР°С‚ РЅР° РІРЅСѓС‚СЂРµРЅРЅРёР№ СѓРіРѕР»
            if (DEBUG_LOGGING)
            {
            }

            // РўРµРїРµСЂСЊ РїСЂРѕРІРµСЂСЏРµРј РґРёР°РіРѕРЅР°Р»Рё - РІРЅСѓС‚СЂРµРЅРЅРёР№ СѓРіРѕР» РёРјРµРµС‚ РџРЈРЎРўРЈР® РґРёР°РіРѕРЅР°Р»СЊ РјРµР¶РґСѓ РґРІСѓРјСЏ РїРµСЂРїРµРЅРґРёРєСѓР»СЏСЂРЅС‹РјРё СЃС‚РµРЅР°РјРё
            Vector2Int topLeftDiag = wallPos + new Vector2Int(-1, 1);
            Vector2Int topRightDiag = wallPos + new Vector2Int(1, 1);
            Vector2Int bottomLeftDiag = wallPos + new Vector2Int(-1, -1);
            Vector2Int bottomRightDiag = wallPos + new Vector2Int(1, -1);

            bool isInnerCorner = false;

            // РљРѕРЅС„РёРіСѓСЂР°С†РёСЏ 1: РЎС‚РµРЅС‹ Top+Right в†’ РґРёР°РіРѕРЅР°Р»СЊ TopRight РґРѕР»Р¶РЅР° Р±С‹С‚СЊ РїСѓСЃС‚РѕР№
            if (hasWallTop && hasWallRight)
            {
                bool diagEmpty = !allCells.Contains(topRightDiag);
                if (DEBUG_LOGGING)
                {
                }
                // РџСЂРѕРІРµСЂСЏРµРј С‡С‚Рѕ TopRight РґРёР°РіРѕРЅР°Р»СЊ РќР• С‡Р°СЃС‚СЊ РєРѕРјРЅР°С‚С‹ (РїСѓСЃС‚Р°СЏ)
                if (diagEmpty)
                {
                    isInnerCorner = true;
                    if (DEBUG_LOGGING)
                    {
                    }
                }
            }
            // РљРѕРЅС„РёРіСѓСЂР°С†РёСЏ 2: РЎС‚РµРЅС‹ Top+Left в†’ РґРёР°РіРѕРЅР°Р»СЊ TopLeft РґРѕР»Р¶РЅР° Р±С‹С‚СЊ РїСѓСЃС‚РѕР№
            else if (hasWallTop && hasWallLeft)
            {
                bool diagEmpty = !allCells.Contains(topLeftDiag);
                if (DEBUG_LOGGING)
                {
                }
                if (diagEmpty)
                {
                    isInnerCorner = true;
                    if (DEBUG_LOGGING)
                    {
                    }
                }
            }
            // РљРѕРЅС„РёРіСѓСЂР°С†РёСЏ 3: РЎС‚РµРЅС‹ Bottom+Right в†’ РґРёР°РіРѕРЅР°Р»СЊ BottomRight РґРѕР»Р¶РЅР° Р±С‹С‚СЊ РїСѓСЃС‚РѕР№
            else if (hasWallBottom && hasWallRight)
            {
                bool diagEmpty = !allCells.Contains(bottomRightDiag);
                if (DEBUG_LOGGING)
                {
                }
                if (diagEmpty)
                {
                    isInnerCorner = true;
                    if (DEBUG_LOGGING)
                    {
                    }
                }
            }
            // РљРѕРЅС„РёРіСѓСЂР°С†РёСЏ 4: РЎС‚РµРЅС‹ Bottom+Left в†’ РґРёР°РіРѕРЅР°Р»СЊ BottomLeft РґРѕР»Р¶РЅР° Р±С‹С‚СЊ РїСѓСЃС‚РѕР№
            else if (hasWallBottom && hasWallLeft)
            {
                bool diagEmpty = !allCells.Contains(bottomLeftDiag);
                if (DEBUG_LOGGING)
                {
                }
                if (diagEmpty)
                {
                    isInnerCorner = true;
                    if (DEBUG_LOGGING)
                    {
                    }
                }
            }

            if (isInnerCorner)
            {
                innerCorners.Add(wallPos);
            }
        }

        if (DEBUG_LOGGING)
        {
        }

        return innerCorners;
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ СЏРІР»СЏРµС‚СЃСЏ Р»Рё РџРЈРЎРўРђРЇ РїРѕР·РёС†РёСЏ РІРЅСѓС‚СЂРµРЅРЅРёРј СѓРіР»РѕРј
    /// Р’РЅСѓС‚СЂРµРЅРЅРёР№ СѓРіРѕР» - РџРЈРЎРўРђРЇ РєР»РµС‚РєР° РіРґРµ РІСЃС‚СЂРµС‡Р°СЋС‚СЃСЏ Р”Р’Р• СЃС‚РµРЅС‹ РїРѕРґ РїСЂСЏРјС‹Рј СѓРіР»РѕРј Рё РџРћР› РЅР° РїСЂРѕС‚РёРІРѕРїРѕР»РѕР¶РЅРѕР№ РґРёР°РіРѕРЅР°Р»Рё
    /// РљР РРўРР§РќРћ: Р’РЅСѓС‚СЂРµРЅРЅРёР№ СѓРіРѕР» РґРѕР»Р¶РµРЅ Р±С‹С‚СЊ Р РЇР”РћРњ СЃ СѓРґР°Р»РµРЅРЅРѕР№ РєР»РµС‚РєРѕР№ (РІС‹СЂРµР·), РёРЅР°С‡Рµ СЌС‚Рѕ РІРЅРµС€РЅРёР№ СѓРіРѕР» РєРѕРјРЅР°С‚С‹
    /// </summary>
    bool IsPositionAnInnerCorner(Vector2Int pos, HashSet<Vector2Int> wallSet, HashSet<Vector2Int> floorSet, HashSet<Vector2Int> deletedSet)
    {
        // РљР РРўРР§Р•РЎРљРђРЇ РџР РћР’Р•Р РљРђ #1: Р’РЅСѓС‚СЂРµРЅРЅРёР№ СѓРіРѕР» РґРѕР»Р¶РµРЅ Р±С‹С‚СЊ Р РЇР”РћРњ СЃ СѓРґР°Р»РµРЅРЅРѕР№ РєР»РµС‚РєРѕР№
        // Р­С‚Рѕ РѕС‚Р»РёС‡Р°РµС‚ РІРЅСѓС‚СЂРµРЅРЅРёР№ СѓРіРѕР» (РІ РІС‹СЂРµР·Рµ) РѕС‚ РІРЅРµС€РЅРµРіРѕ СѓРіР»Р° РєРѕРјРЅР°С‚С‹
        bool hasDeletedNeighbor = false;
        Vector2Int[] allNeighbors = new Vector2Int[]
        {
            pos + Vector2Int.up,
            pos + Vector2Int.down,
            pos + Vector2Int.left,
            pos + Vector2Int.right,
            pos + new Vector2Int(-1, 1),   // РґРёР°РіРѕРЅР°Р»Рё С‚РѕР¶Рµ РїСЂРѕРІРµСЂСЏРµРј
            pos + new Vector2Int(1, 1),
            pos + new Vector2Int(-1, -1),
            pos + new Vector2Int(1, -1)
        };

        foreach (Vector2Int neighbor in allNeighbors)
        {
            if (deletedSet.Contains(neighbor))
            {
                hasDeletedNeighbor = true;
                break;
            }
        }

        if (!hasDeletedNeighbor)
            return false;

        // РџСЂРѕРІРµСЂСЏРµРј СЃРѕСЃРµРґРЅРёРµ РєР»РµС‚РєРё
        Vector2Int topPos = pos + Vector2Int.up;
        Vector2Int bottomPos = pos + Vector2Int.down;
        Vector2Int leftPos = pos + Vector2Int.left;
        Vector2Int rightPos = pos + Vector2Int.right;

        bool hasWallTop = wallSet.Contains(topPos);
        bool hasWallBottom = wallSet.Contains(bottomPos);
        bool hasWallLeft = wallSet.Contains(leftPos);
        bool hasWallRight = wallSet.Contains(rightPos);

        // Р’РЅСѓС‚СЂРµРЅРЅРёР№ СѓРіРѕР» РґРѕР»Р¶РµРЅ РёРјРµС‚СЊ Р РћР’РќРћ 2 СЃС‚РµРЅС‹-СЃРѕСЃРµРґР° РїРѕРґ РїСЂСЏРјС‹Рј СѓРіР»РѕРј
        int wallNeighborCount = (hasWallTop ? 1 : 0) + (hasWallBottom ? 1 : 0) + (hasWallLeft ? 1 : 0) + (hasWallRight ? 1 : 0);

        if (wallNeighborCount != 2)
            return false;

        // РџСЂРѕРІРµСЂСЏРµРј, С‡С‚Рѕ 2 СЃС‚РµРЅС‹ РЅР°С…РѕРґСЏС‚СЃСЏ РїРѕРґ РїСЂСЏРјС‹Рј СѓРіР»РѕРј (РЅРµ РЅР°РїСЂРѕС‚РёРІ РґСЂСѓРі РґСЂСѓРіР°)
        bool hasVerticalPair = hasWallTop && hasWallBottom;
        bool hasHorizontalPair = hasWallLeft && hasWallRight;

        if (hasVerticalPair || hasHorizontalPair)
            return false;

        // РџСЂРѕРІРµСЂСЏРµРј РґРёР°РіРѕРЅР°Р»Рё - РІРЅСѓС‚СЂРµРЅРЅРёР№ СѓРіРѕР» РґРѕР»Р¶РµРЅ РёРјРµС‚СЊ РџРћР› РЅР° РїСЂРѕС‚РёРІРѕРїРѕР»РѕР¶РЅРѕР№ РґРёР°РіРѕРЅР°Р»Рё
        Vector2Int topLeftDiag = pos + new Vector2Int(-1, 1);
        Vector2Int topRightDiag = pos + new Vector2Int(1, 1);
        Vector2Int bottomLeftDiag = pos + new Vector2Int(-1, -1);
        Vector2Int bottomRightDiag = pos + new Vector2Int(1, -1);

        bool hasFloorTopLeft = floorSet.Contains(topLeftDiag);
        bool hasFloorTopRight = floorSet.Contains(topRightDiag);
        bool hasFloorBottomLeft = floorSet.Contains(bottomLeftDiag);
        bool hasFloorBottomRight = floorSet.Contains(bottomRightDiag);

        // РџСЂРѕРІРµСЂСЏРµРј РєРѕРЅС„РёРіСѓСЂР°С†РёРё: СЃС‚РµРЅС‹ Top+Right в†’ РїРѕР» РґРѕР»Р¶РµРЅ Р±С‹С‚СЊ BottomLeft
        if (hasWallTop && hasWallRight && hasFloorBottomLeft)
            return true;

        // РЎС‚РµРЅС‹ Top+Left в†’ РїРѕР» РґРѕР»Р¶РµРЅ Р±С‹С‚СЊ BottomRight
        if (hasWallTop && hasWallLeft && hasFloorBottomRight)
            return true;

        // РЎС‚РµРЅС‹ Bottom+Right в†’ РїРѕР» РґРѕР»Р¶РµРЅ Р±С‹С‚СЊ TopLeft
        if (hasWallBottom && hasWallRight && hasFloorTopLeft)
            return true;

        // РЎС‚РµРЅС‹ Bottom+Left в†’ РїРѕР» РґРѕР»Р¶РµРЅ Р±С‹С‚СЊ TopRight
        if (hasWallBottom && hasWallLeft && hasFloorTopRight)
            return true;

        return false;
    }

    /// <summary>
    /// РЈРЎРўРђР Р•Р’РЁРР™ РњР•РўРћР” - РѕСЃС‚Р°РІР»РµРЅ РґР»СЏ СЃРѕРІРјРµСЃС‚РёРјРѕСЃС‚Рё
    /// РџСЂРѕРІРµСЂРёС‚СЊ СЏРІР»СЏРµС‚СЃСЏ Р»Рё СЃС‚РµРЅР° РІРЅСѓС‚СЂРµРЅРЅРёРј СѓРіР»РѕРј
    /// Р’РЅСѓС‚СЂРµРЅРЅРёР№ СѓРіРѕР» - СѓРіРѕР» Р’РќРЈРўР Р РІС‹СЂРµР·Р°, РіРґРµ РІСЃС‚СЂРµС‡Р°СЋС‚СЃСЏ РґРІРµ СЃС‚РµРЅС‹ РїРѕРґ РїСЂСЏРјС‹Рј СѓРіР»РѕРј
    /// РљР›Р®Р§Р•Р’РћР• РћРўР›РР§РР•: РІРЅСѓС‚СЂРµРЅРЅРёР№ СѓРіРѕР» РґРѕР»Р¶РµРЅ Р±С‹С‚СЊ СЂСЏРґРѕРј СЃ СѓРґР°Р»РµРЅРЅРѕР№ РєР»РµС‚РєРѕР№ (С‚Р°Рј РіРґРµ РІС‹СЂРµР·)
    /// </summary>
    bool IsWallAnInnerCorner(Vector2Int wallPos, HashSet<Vector2Int> wallSet, HashSet<Vector2Int> floorSet)
    {

        // РџСЂРѕРІРµСЂСЏРµРј СЃРѕСЃРµРґРЅРёРµ РєР»РµС‚РєРё
        Vector2Int topPos = wallPos + Vector2Int.up;
        Vector2Int bottomPos = wallPos + Vector2Int.down;
        Vector2Int leftPos = wallPos + Vector2Int.left;
        Vector2Int rightPos = wallPos + Vector2Int.right;

        bool hasWallTop = wallSet.Contains(topPos);
        bool hasWallBottom = wallSet.Contains(bottomPos);
        bool hasWallLeft = wallSet.Contains(leftPos);
        bool hasWallRight = wallSet.Contains(rightPos);


        // Р’РђР–РќРћ: РЈРіРѕР» РґРѕР»Р¶РµРЅ РёРјРµС‚СЊ Р РћР’РќРћ 2 СЃС‚РµРЅС‹-СЃРѕСЃРµРґР° РїРѕРґ РїСЂСЏРјС‹Рј СѓРіР»РѕРј
        int wallNeighborCount = (hasWallTop ? 1 : 0) + (hasWallBottom ? 1 : 0) + (hasWallLeft ? 1 : 0) + (hasWallRight ? 1 : 0);
        if (wallNeighborCount != 2)
        {
            return false;
        }

        // РџСЂРѕРІРµСЂСЏРµРј, С‡С‚Рѕ 2 СЃС‚РµРЅС‹ РЅР°С…РѕРґСЏС‚СЃСЏ РїРѕРґ РїСЂСЏРјС‹Рј СѓРіР»РѕРј (РЅРµ РЅР°РїСЂРѕС‚РёРІ РґСЂСѓРі РґСЂСѓРіР°)
        bool hasVerticalPair = hasWallTop && hasWallBottom;
        bool hasHorizontalPair = hasWallLeft && hasWallRight;
        if (hasVerticalPair || hasHorizontalPair)
        {
            return false;
        }

        // РљР РРўРР§Р•РЎРљРђРЇ РџР РћР’Р•Р РљРђ: РІРЅСѓС‚СЂРµРЅРЅРёР№ СѓРіРѕР» РґРѕР»Р¶РµРЅ Р±С‹С‚СЊ Р РЇР”РћРњ СЃ СѓРґР°Р»РµРЅРЅРѕР№ РєР»РµС‚РєРѕР№
        // Р­С‚Рѕ РѕС‚Р»РёС‡Р°РµС‚ РµРіРѕ РѕС‚ РІРЅРµС€РЅРµРіРѕ СѓРіР»Р° РєРѕРјРЅР°С‚С‹
        HashSet<Vector2Int> deletedSet = new HashSet<Vector2Int>(deletedCells);
        bool hasDeletedTop = deletedSet.Contains(topPos);
        bool hasDeletedBottom = deletedSet.Contains(bottomPos);
        bool hasDeletedLeft = deletedSet.Contains(leftPos);
        bool hasDeletedRight = deletedSet.Contains(rightPos);
        bool hasDeletedNeighbor = hasDeletedTop || hasDeletedBottom || hasDeletedLeft || hasDeletedRight;


        if (!hasDeletedNeighbor)
        {
            return false;
        }

        // РџСЂРѕРІРµСЂСЏРµРј РґРёР°РіРѕРЅР°Р»Рё - РІРЅСѓС‚СЂРµРЅРЅРёР№ СѓРіРѕР» РґРѕР»Р¶РµРЅ РёРјРµС‚СЊ РїРѕР» РЅР° РїСЂРѕС‚РёРІРѕРїРѕР»РѕР¶РЅРѕР№ РґРёР°РіРѕРЅР°Р»Рё
        Vector2Int topLeftDiag = wallPos + new Vector2Int(-1, 1);
        Vector2Int topRightDiag = wallPos + new Vector2Int(1, 1);
        Vector2Int bottomLeftDiag = wallPos + new Vector2Int(-1, -1);
        Vector2Int bottomRightDiag = wallPos + new Vector2Int(1, -1);

        bool hasFloorTopLeft = floorSet.Contains(topLeftDiag);
        bool hasFloorTopRight = floorSet.Contains(topRightDiag);
        bool hasFloorBottomLeft = floorSet.Contains(bottomLeftDiag);
        bool hasFloorBottomRight = floorSet.Contains(bottomRightDiag);


        // РџСЂРѕРІРµСЂСЏРµРј РєРѕРЅС„РёРіСѓСЂР°С†РёРё РІРЅСѓС‚СЂРµРЅРЅРёС… СѓРіР»РѕРІ
        // Р’РЅСѓС‚СЂРµРЅРЅРёР№ СѓРіРѕР» РёРјРµРµС‚ РїРѕР» РЅР° РїСЂРѕС‚РёРІРѕРїРѕР»РѕР¶РЅРѕР№ РґРёР°РіРѕРЅР°Р»Рё РѕС‚ РјРµСЃС‚Р° СЃРѕРµРґРёРЅРµРЅРёСЏ СЃС‚РµРЅ
        if (hasWallTop && hasWallRight)
        {
            // РЎС‚РµРЅС‹ СЃРІРµСЂС…Сѓ Рё СЃРїСЂР°РІР° -> РїРѕР» РґРѕР»Р¶РµРЅ Р±С‹С‚СЊ СЃР»РµРІР° СЃРЅРёР·Сѓ
            if (hasFloorBottomLeft)
            {
                return true;
            }
        }

        if (hasWallTop && hasWallLeft)
        {
            // РЎС‚РµРЅС‹ СЃРІРµСЂС…Сѓ Рё СЃР»РµРІР° -> РїРѕР» РґРѕР»Р¶РµРЅ Р±С‹С‚СЊ СЃРїСЂР°РІР° СЃРЅРёР·Сѓ
            if (hasFloorBottomRight)
            {
                return true;
            }
        }

        if (hasWallBottom && hasWallRight)
        {
            // РЎС‚РµРЅС‹ СЃРЅРёР·Сѓ Рё СЃРїСЂР°РІР° -> РїРѕР» РґРѕР»Р¶РµРЅ Р±С‹С‚СЊ СЃР»РµРІР° СЃРІРµСЂС…Сѓ
            if (hasFloorTopLeft)
            {
                return true;
            }
        }

        if (hasWallBottom && hasWallLeft)
        {
            // РЎС‚РµРЅС‹ СЃРЅРёР·Сѓ Рё СЃР»РµРІР° -> РїРѕР» РґРѕР»Р¶РµРЅ Р±С‹С‚СЊ СЃРїСЂР°РІР° СЃРІРµСЂС…Сѓ
            if (hasFloorTopRight)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// РћР±РЅРѕРІРёС‚СЊ ghost Р±Р»РѕРєРё РїРѕСЃР»Рµ СѓРґР°Р»РµРЅРёСЏ (РћРџРўРРњРР—РР РћР’РђРќРћ - РёРЅРєСЂРµРјРµРЅС‚Р°Р»СЊРЅРѕРµ РѕР±РЅРѕРІР»РµРЅРёРµ)
    /// </summary>
    void UpdateGhostsAfterDeletion()
    {
        // РРќРљР Р•РњР•РќРўРђР›Р¬РќРћР• РћР‘РќРћР’Р›Р•РќРР•: РѕР±РЅРѕРІР»СЏРµРј С‚РѕР»СЊРєРѕ РёР·РјРµРЅРµРЅРЅС‹Рµ Р±Р»РѕРєРё

        HashSet<Vector2Int> newPerimeter = new HashSet<Vector2Int>(roomPerimeter);
        HashSet<Vector2Int> newFloor = new HashSet<Vector2Int>(roomFloor);

        // Р’РђР–РќРћ: РћР±СЂР°Р±Р°С‚С‹РІР°РµРј Idle, PreviewReady - РёСЃРїРѕР»СЊР·СѓРµРј Р°РєС‚РёРІРЅС‹Рµ ghost Р±Р»РѕРєРё
        if (currentState == DragState.Idle || currentState == DragState.PreviewReady)
        {
            // РЈРґР°Р»СЏРµРј СЃС‚РµРЅС‹ РєРѕС‚РѕСЂС‹С… Р±РѕР»СЊС€Рµ РЅРµС‚
            List<Vector2Int> wallsToRemove = new List<Vector2Int>();
            foreach (var kvp in activeGhostBlocks)
            {
                if (!newPerimeter.Contains(kvp.Key))
                {
                    wallsToRemove.Add(kvp.Key);
                }
            }

            foreach (var pos in wallsToRemove)
            {
                if (activeGhostBlocks.TryGetValue(pos, out GameObject block))
                {
                    GhostBlockPool.Instance.Return(block);
                    ghostBlocks.Remove(block);
                }
                activeGhostBlocks.Remove(pos);
            }

            // Р”РѕР±Р°РІР»СЏРµРј РЅРѕРІС‹Рµ СЃС‚РµРЅС‹
            foreach (var pos in newPerimeter)
            {
                if (!activeGhostBlocks.ContainsKey(pos))
                {
                    GameObject ghostBlock = CreateGhostBlockPooled(pos, buildGhostPrefab);
                    activeGhostBlocks[pos] = ghostBlock;
                    ghostBlocks.Add(ghostBlock);
                }
            }

            // РЈРґР°Р»СЏРµРј РїРѕР» РєРѕС‚РѕСЂРѕРіРѕ Р±РѕР»СЊС€Рµ РЅРµС‚
            List<Vector2Int> floorToRemove = new List<Vector2Int>();
            foreach (var kvp in activeFloorBlocks)
            {
                if (!newFloor.Contains(kvp.Key))
                {
                    floorToRemove.Add(kvp.Key);
                }
            }

            foreach (var pos in floorToRemove)
            {
                if (activeFloorBlocks.TryGetValue(pos, out GameObject block))
                {
                    GhostBlockPool.Instance.Return(block);
                    ghostFloorBlocks.Remove(block);
                }
                activeFloorBlocks.Remove(pos);
            }

            // Р”РѕР±Р°РІР»СЏРµРј РЅРѕРІС‹Р№ РїРѕР»
            if (floorGhostPrefab != null)
            {
                foreach (var pos in newFloor)
                {
                    if (!activeFloorBlocks.ContainsKey(pos))
                    {
                        GameObject floorBlock = CreateGhostBlockPooled(pos, floorGhostPrefab);
                        activeFloorBlocks[pos] = floorBlock;
                        ghostFloorBlocks.Add(floorBlock);
                    }
                }
            }

            // РћР±РЅРѕРІР»СЏРµРј state РµСЃР»Рё РЅСѓР¶РЅРѕ
            if (currentState == DragState.Idle && (roomPerimeter.Count > 0 || roomFloor.Count > 0))
            {
                currentState = DragState.PreviewReady;
            }
        }
        else if (currentState == DragState.Confirmed)
        {
            // Р”Р»СЏ Confirmed СЃРѕСЃС‚РѕСЏРЅРёСЏ РёСЃРїРѕР»СЊР·СѓРµРј СЃС‚Р°СЂС‹Р№ РјРµС‚РѕРґ (РёСЃРїРѕР»СЊР·СѓРµС‚СЃСЏ СЂРµРґРєРѕ)
            ClearConfirmedBlocks();

            foreach (Vector2Int cellPos in roomPerimeter)
            {
                GameObject confirmedBlock = CreateGhostBlock(cellPos, addBuildGhostPrefab);
                confirmedBlocks.Add(confirmedBlock);
            }

            if (addFloorGhostPrefab != null)
            {
                foreach (Vector2Int cellPos in roomFloor)
                {
                    GameObject confirmedFloorBlock = CreateGhostBlock(cellPos, addFloorGhostPrefab);
                    confirmedFloorBlocks.Add(confirmedFloorBlock);
                }
            }
        }
    }

    /// <summary>
    /// РћС‡РёСЃС‚РёС‚СЊ Del_Build_Ghost Р±Р»РѕРєРё Рё СЃРїРёСЃРѕРє СѓРґР°Р»РµРЅРЅС‹С… РєР»РµС‚РѕРє
    /// </summary>
    void ClearDelGhostBlocks()
    {
        // РћС‡РёС‰Р°РµРј СЃРїРёСЃРѕРє СѓРґР°Р»РµРЅРЅС‹С… РєР»РµС‚РѕРє (РјР°СЂРєРµСЂС‹ Р±РѕР»СЊС€Рµ РЅРµ СЃРѕР·РґР°СЋС‚СЃСЏ)
        foreach (GameObject block in delGhostBlocks)
        {
            if (block != null)
                Destroy(block);
        }
        delGhostBlocks.Clear();
        deletedCells.Clear();
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ, РіРѕС‚РѕРІ Р»Рё СЃРёР»СѓСЌС‚ Рє РїРѕРґС‚РІРµСЂР¶РґРµРЅРёСЋ
    /// </summary>
    public bool IsReadyToConfirm()
    {
        return currentState == DragState.PreviewReady;
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ, РїРѕРґС‚РІРµСЂР¶РґРµРЅР° Р»Рё РїРѕСЃС‚СЂРѕР№РєР°
    /// </summary>
    public bool IsConfirmed()
    {
        return currentState == DragState.Confirmed;
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ, Р°РєС‚РёРІРµРЅ Р»Рё СЂРµР¶РёРј СѓРґР°Р»РµРЅРёСЏ
    /// </summary>
    public bool IsDeletionModeActive()
    {
        return isDeletionModeActive;
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ, Р°РєС‚РёРІРµРЅ Р»Рё drag СЂРµР¶РёРј
    /// </summary>
    public bool IsDragModeActive()
    {
        return isDragModeActive;
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ, РµСЃС‚СЊ Р»Рё РґР°РЅРЅС‹Рµ РєРѕРјРЅР°С‚С‹ (РїРµСЂРёРјРµС‚СЂ РёР»Рё РїРѕР»)
    /// </summary>
    public bool HasRoomData()
    {
        return roomPerimeter.Count > 0 || roomFloor.Count > 0;
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ, РјРѕР¶РЅРѕ Р»Рё РїРѕРґС‚РІРµСЂРґРёС‚СЊ РїРѕСЃС‚СЂРѕР№РєСѓ
    /// Р’РѕР·РІСЂР°С‰Р°РµС‚ true РµСЃР»Рё РµСЃС‚СЊ С…РѕС‚СЊ РєР°РєРёРµ-С‚Рѕ РґР°РЅРЅС‹Рµ РёР»Рё РёРґРµС‚ РїСЂРѕС†РµСЃСЃ СЂРёСЃРѕРІР°РЅРёСЏ
    /// </summary>
    public bool CanConfirmBuild()
    {
        // РљРЅРѕРїРєР° Р°РєС‚РёРІРЅР° РµСЃР»Рё:
        // 1. РРґРµС‚ РїСЂРѕС†РµСЃСЃ СЂРёСЃРѕРІР°РЅРёСЏ (Dragging state)
        // 2. Р•СЃС‚СЊ РіРѕС‚РѕРІС‹Р№ preview (PreviewReady state)
        // 3. РР›Р РїСЂРѕСЃС‚Рѕ РµСЃС‚СЊ РґР°РЅРЅС‹Рµ (РЅР° СЃР»СѓС‡Р°Р№ РµСЃР»Рё state СЃР±СЂРѕСЃРёР»СЃСЏ РЅРѕ РґР°РЅРЅС‹Рµ РѕСЃС‚Р°Р»РёСЃСЊ)
        return currentState == DragState.Dragging ||
               currentState == DragState.PreviewReady ||
               HasRoomData();
    }
}
