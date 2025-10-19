using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoomData
{
    public string roomName;
    public string roomType;
    public Vector2Int size; // СЂР°Р·РјРµСЂ РІ РєР»РµС‚РєР°С…
    public int cost;
    public GameObject prefab;
    public Color previewColor = Color.green;
}

public class ShipBuildingSystem : MonoBehaviour
{
    [Header("Building Settings")]
    public GridManager gridManager;
    public Camera playerCamera;

    [Header("Room Types")]
    public List<RoomData> availableRooms = new List<RoomData>();

    [Header("Main Objects")]
    public List<MainObjectData> availableMainObjects = new List<MainObjectData>();

    [Header("Preview Settings")]
    public Material previewMaterial;
    public LayerMask groundLayerMask = 1;

    // РЎРѕСЃС‚РѕСЏРЅРёСЏ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
    public enum BuildingPhase
    {
        None,           // Р РµР¶РёРј СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР° РЅРµР°РєС‚РёРІРµРЅ
        PlacingRoom,    // Р­С‚Р°Рї 1: Р Р°Р·РјРµС‰РµРЅРёРµ РїСЂРёР·СЂР°РєР° РєРѕРјРЅР°С‚С‹
        PlacingDoor     // Р­С‚Р°Рї 2: Р Р°Р·РјРµС‰РµРЅРёРµ РґРІРµСЂРё
    }

    // Р’РЅСѓС‚СЂРµРЅРЅРёРµ РїРµСЂРµРјРµРЅРЅС‹Рµ
    private bool buildingMode = false;
    private bool deletionMode = false;
    private BuildingPhase currentPhase = BuildingPhase.None;
    private int selectedRoomIndex = 0;
    private GameObject previewObject;
    private GameObject doorPreviewObject; // РџСЂРёР·СЂР°Рє РґРІРµСЂРё
    private Vector2Int pendingRoomPosition; // РџРѕР·РёС†РёСЏ СЂР°Р·РјРµС‰Р°РµРјРѕР№ РєРѕРјРЅР°С‚С‹
    private Vector2Int pendingRoomSize; // Р Р°Р·РјРµСЂ СЂР°Р·РјРµС‰Р°РµРјРѕР№ РєРѕРјРЅР°С‚С‹
    private int pendingRoomRotation; // РџРѕРІРѕСЂРѕС‚ СЂР°Р·РјРµС‰Р°РµРјРѕР№ РєРѕРјРЅР°С‚С‹
    private List<Vector2Int> straightWallPositions = new List<Vector2Int>(); // РџРѕР·РёС†РёРё РїСЂСЏРјС‹С… СЃС‚РµРЅ РґР»СЏ РґРІРµСЂРё
    private Vector2Int doorPosition = Vector2Int.zero; // РўРµРєСѓС‰Р°СЏ РїРѕР·РёС†РёСЏ РґРІРµСЂРё
    private List<GameObject> previewCells = new List<GameObject>();
    private List<GameObject> builtRooms = new List<GameObject>();

    // РђРІС‚РѕРјР°С‚РёС‡РµСЃРєРѕРµ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІРѕ РїСЂРё РІС‹Р±РѕСЂРµ РґРІРµСЂРё (РћРўРљР›Р®Р§Р•РќРћ)
    private float doorSelectionTimer = 0f;
    private const float AUTO_BUILD_DELAY = float.MaxValue; // Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРѕРµ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІРѕ РѕС‚РєР»СЋС‡РµРЅРѕ
    private Vector2Int lastDoorPosition = Vector2Int.zero;
    private bool roomBuilt = false;
    private GameObject highlightedRoom = null;
    private Material originalMaterial = null;
    private int roomRotation = 0; // РџРѕРІРѕСЂРѕС‚ РєРѕРјРЅР°С‚С‹ РІ РіСЂР°РґСѓСЃР°С… (0, 90, 180, 270)
    private bool scrollWheelUsedThisFrame = false; // Р¤Р»Р°Рі РёСЃРїРѕР»СЊР·РѕРІР°РЅРёСЏ СЂРѕР»РёРєР° РІ СЌС‚РѕРј РєР°РґСЂРµ

    // РЎРѕР±С‹С‚РёСЏ
    public System.Action<GameObject> OnRoomBuilt;
    public System.Action<GameObject> OnRoomDeleted;
    public System.Action OnBuildingModeChanged;
    public System.Action OnDeletionModeChanged;

    void Start()
    {
        InitializeBuildingSystem();
    }

    void Update()
    {
        // РЎР±СЂР°СЃС‹РІР°РµРј С„Р»Р°Рі РёСЃРїРѕР»СЊР·РѕРІР°РЅРёСЏ СЂРѕР»РёРєР° РІ РЅР°С‡Р°Р»Рµ РєР°РґСЂР°
        scrollWheelUsedThisFrame = false;

        // РџСЂРѕРІРµСЂСЏРµРј РїР°СѓР·Сѓ, РЅРѕ СЂР°Р·СЂРµС€Р°РµРј СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІРѕ РІРѕ РІСЂРµРјСЏ РїР°СѓР·С‹ СЃС‚СЂРѕР№РєРё
        bool isPaused = GamePauseManager.Instance.IsPaused();
        bool isBuildModePause = GamePauseManager.Instance.IsBuildModePause();

        if (buildingMode && (!isPaused || isBuildModePause))
        {
            UpdatePreview();
            HandleBuildingInput();
        }
        else if (deletionMode && !isPaused)
        {
            UpdateDeletionHighlight();
            HandleDeletionInput();
        }
    }

    /// <summary>
    /// РРЅРёС†РёР°Р»РёР·Р°С†РёСЏ СЃРёСЃС‚РµРјС‹ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
    /// </summary>
    void InitializeBuildingSystem()
    {
        if (gridManager == null)
            gridManager = FindObjectOfType<GridManager>();

        if (playerCamera == null)
            playerCamera = Camera.main;

        CreateDefaultRooms();
    }

    /// <summary>
    /// РЎРѕР·РґР°РЅРёРµ СЃС‚Р°РЅРґР°СЂС‚РЅС‹С… С‚РёРїРѕРІ РєРѕРјРЅР°С‚ (С‚РµРїРµСЂСЊ С‚РѕР»СЊРєРѕ С„РѕСЂРјС‹/СЂР°Р·РјРµСЂС‹)
    /// </summary>
    void CreateDefaultRooms()
    {
        if (availableRooms.Count == 0)
        {
            // РњР°Р»РµРЅСЊРєРёР№ РјРѕРґСѓР»СЊ 4x4
            RoomData smallModule = new RoomData();
            smallModule.roomName = "РњР°Р»С‹Р№ РјРѕРґСѓР»СЊ";
            smallModule.roomType = "Module";
            smallModule.size = new Vector2Int(4, 4);
            smallModule.cost = 40;
            smallModule.previewColor = Color.cyan;
            smallModule.prefab = null;
            availableRooms.Add(smallModule);

            // РЎСЂРµРґРЅРёР№ РјРѕРґСѓР»СЊ 6x6
            RoomData mediumModule = new RoomData();
            mediumModule.roomName = "РЎСЂРµРґРЅРёР№ РјРѕРґСѓР»СЊ";
            mediumModule.roomType = "Module";
            mediumModule.size = new Vector2Int(6, 6);
            mediumModule.cost = 80;
            mediumModule.previewColor = Color.blue;
            mediumModule.prefab = null;
            availableRooms.Add(mediumModule);

            // Р‘РѕР»СЊС€РѕР№ РјРѕРґСѓР»СЊ 8x8
            RoomData largeModule = new RoomData();
            largeModule.roomName = "Р‘РѕР»СЊС€РѕР№ РјРѕРґСѓР»СЊ";
            largeModule.roomType = "Module";
            largeModule.size = new Vector2Int(8, 8);
            largeModule.cost = 120;
            largeModule.previewColor = Color.green;
            largeModule.prefab = null;
            availableRooms.Add(largeModule);
        }

        // РЎРѕР·РґР°РµРј СЃРїРёСЃРѕРє РґРѕСЃС‚СѓРїРЅС‹С… РіР»Р°РІРЅС‹С… РѕР±СЉРµРєС‚РѕРІ
        if (availableMainObjects.Count == 0)
        {
            // РЎРёСЃС‚РµРјР° Р¶РёР·РЅРµРѕР±РµСЃРїРµС‡РµРЅРёСЏ
            MainObjectData lifeSupport = new MainObjectData(
                "РЎРёСЃС‚РµРјР° Р¶РёР·РЅРµРѕР±РµСЃРїРµС‡РµРЅРёСЏ",
                MainObjectType.LifeSupport,
                200f,
                100
            );
            availableMainObjects.Add(lifeSupport);

            // Р СѓРєР°-РјР°РЅРёРїСѓР»СЏС‚РѕСЂ
            MainObjectData manipulatorArm = new MainObjectData(
                "Р СѓРєР°-РјР°РЅРёРїСѓР»СЏС‚РѕСЂ",
                MainObjectType.ManipulatorArm,
                150f,
                80
            );
            availableMainObjects.Add(manipulatorArm);

            // Р РµР°РєС‚РѕСЂРЅР°СЏ СѓСЃС‚Р°РЅРѕРІРєР°
            MainObjectData reactor = new MainObjectData(
                "Р РµР°РєС‚РѕСЂРЅР°СЏ СѓСЃС‚Р°РЅРѕРІРєР°",
                MainObjectType.ReactorInstallation,
                300f,
                200
            );
            availableMainObjects.Add(reactor);

            FileLogger.Log($"[ShipBuildingSystem] Created {availableMainObjects.Count} main object types");
        }
    }


    /// <summary>
    /// РЎРѕР·РґР°РЅРёРµ РїСЂРµС„Р°Р±Р° РєРѕРјРЅР°С‚С‹
    /// </summary>
    GameObject CreateRoomPrefab(RoomData roomData)
    {
        GameObject roomPrefab = new GameObject($"{roomData.roomName}_Prefab");

        // РЎРѕР·РґР°РµРј РІРёР·СѓР°Р»СЊРЅРѕРµ РїСЂРµРґСЃС‚Р°РІР»РµРЅРёРµ
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "RoomVisual";
        visual.transform.SetParent(roomPrefab.transform);
        visual.transform.localPosition = Vector3.zero;

        // РњР°СЃС€С‚Р°Р±РёСЂСѓРµРј РїРѕ СЂР°Р·РјРµСЂСѓ РєРѕРјРЅР°С‚С‹
        float width = roomData.size.x * gridManager.cellSize;
        float height = roomData.size.y * gridManager.cellSize;
        visual.transform.localScale = new Vector3(width, 2f, height);

        // РќР°СЃС‚СЂР°РёРІР°РµРј РјР°С‚РµСЂРёР°Р»
        Renderer renderer = visual.GetComponent<Renderer>();
        Material roomMaterial = new Material(Shader.Find("Standard"));
        roomMaterial.color = new Color(0.8f, 0.8f, 1f, 1f);
        renderer.material = roomMaterial;

        // Р”РѕР±Р°РІР»СЏРµРј РёРЅС„РѕСЂРјР°С†РёСЋ РѕР± РѕР±СЉРµРєС‚Рµ
        LocationObjectInfo objectInfo = roomPrefab.AddComponent<LocationObjectInfo>();
        objectInfo.objectName = roomData.roomName;
        objectInfo.objectType = roomData.roomType;
        objectInfo.health = 300f;
        objectInfo.isDestructible = false;

        roomPrefab.SetActive(false);
        return roomPrefab;
    }

    /// <summary>
    /// Р’РєР»СЋС‡РёС‚СЊ/РІС‹РєР»СЋС‡РёС‚СЊ СЂРµР¶РёРј СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
    /// </summary>
    public void ToggleBuildingMode()
    {
        buildingMode = !buildingMode;

        if (buildingMode)
        {
            StartBuildingMode();
        }
        else
        {
            StopBuildingMode();
        }

        OnBuildingModeChanged?.Invoke();
    }

    /// <summary>
    /// РЈСЃС‚Р°РЅРѕРІРёС‚СЊ СЂРµР¶РёРј СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР° (РёСЃРїРѕР»СЊР·СѓРµС‚СЃСЏ РёР· GameUI)
    /// </summary>
    public void SetBuildMode(bool enabled)
    {
        if (buildingMode == enabled) return;

        buildingMode = enabled;

        if (buildingMode)
        {
            currentPhase = BuildingPhase.PlacingRoom;
            StartBuildingMode();
            FileLogger.Log("Build mode activated - Phase 1: Placing room");
        }
        else
        {
            currentPhase = BuildingPhase.None;
            StopBuildingMode();
            FileLogger.Log("Build mode deactivated");
        }

        OnBuildingModeChanged?.Invoke();
    }

    /// <summary>
    /// Р—Р°РїСѓСЃС‚РёС‚СЊ СЂРµР¶РёРј СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
    /// </summary>
    void StartBuildingMode()
    {
        CreatePreviewObject();
    }

    /// <summary>
    /// РћСЃС‚Р°РЅРѕРІРёС‚СЊ СЂРµР¶РёРј СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
    /// </summary>
    void StopBuildingMode()
    {
        if (previewObject != null)
        {
            DestroyImmediate(previewObject);
            previewObject = null;
        }

        if (doorPreviewObject != null)
        {
            DestroyImmediate(doorPreviewObject);
            doorPreviewObject = null;
        }

        ClearPreviewCells();
        currentPhase = BuildingPhase.None;
    }

    /// <summary>
    /// РЎРѕР·РґР°С‚СЊ РѕР±СЉРµРєС‚ РїСЂРµРґРІР°СЂРёС‚РµР»СЊРЅРѕРіРѕ РїСЂРѕСЃРјРѕС‚СЂР°
    /// </summary>
    void CreatePreviewObject()
    {
        if (availableRooms.Count == 0 || selectedRoomIndex >= availableRooms.Count)
            return;

        RoomData currentRoom = availableRooms[selectedRoomIndex];

        if (previewObject != null)
            DestroyImmediate(previewObject);

        ClearPreviewCells();

        // РЎРѕР·РґР°РµРј РїСЂРёР·СЂР°Рє РєРѕРјРЅР°С‚С‹ СЃ РЅР°СЃС‚РѕСЏС‰РёРјРё СЃС‚РµРЅР°РјРё (РёСЃРїРѕР»СЊР·СѓРµРј РІСЂРµРјРµРЅРЅСѓСЋ РїРѕР·РёС†РёСЋ, РѕР±РЅРѕРІРёС‚СЃСЏ РІ UpdatePreview)
        Vector2Int rotatedSize = GetRotatedRoomSize(currentRoom.size, roomRotation);
        previewObject = CreateGhostRoom(Vector2Int.zero, rotatedSize, currentRoom.roomName + "_Preview", roomRotation);
    }

    /// <summary>
    /// РЎРѕР·РґР°С‚СЊ РїСЂРёР·СЂР°Рє РєРѕРјРЅР°С‚С‹ СЃ РїРѕР»СѓРїСЂРѕР·СЂР°С‡РЅС‹РјРё СЃС‚РµРЅР°РјРё
    /// </summary>
    GameObject CreateGhostRoom(Vector2Int gridPosition, Vector2Int roomSize, string roomName, int rotation = 0)
    {
        GameObject ghostRoom = new GameObject(roomName);

        // РџРѕР» РЅРµ СЃРѕР·РґР°РµРј - РІ СЂРµР°Р»СЊРЅС‹С… РєРѕРјРЅР°С‚Р°С… РµРіРѕ РЅРµС‚

        // РЎРѕР·РґР°РµРј РїСЂРёР·СЂР°С‡РЅС‹Рµ СЃС‚РµРЅС‹ СЃ СѓС‡РµС‚РѕРј РїРѕРІРѕСЂРѕС‚Р°
        CreateGhostWalls(ghostRoom, Vector2Int.zero, roomSize, rotation);

        return ghostRoom;
    }

    /// <summary>
    /// РЎРѕР·РґР°С‚СЊ РїСЂРёР·СЂР°С‡РЅС‹Р№ РїРѕР»
    /// </summary>
    void CreateGhostFloor(GameObject parent, Vector2Int gridPosition, Vector2Int roomSize)
    {
        // РЎРѕР·РґР°РµРј РїРѕР» С‚РѕР»СЊРєРѕ РґР»СЏ РІРЅСѓС‚СЂРµРЅРЅРёС… РєР»РµС‚РѕРє (Р±РµР· СЃС‚РµРЅ РїРѕ РїРµСЂРёРјРµС‚СЂСѓ)
        int innerWidth = Mathf.Max(1, roomSize.x - 2);
        int innerHeight = Mathf.Max(1, roomSize.y - 2);

        if (innerWidth > 0 && innerHeight > 0)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "GhostFloor";
            floor.transform.SetParent(parent.transform);

            // Р Р°Р·РјРµСЂС‹ РІРЅСѓС‚СЂРµРЅРЅРµРіРѕ РїРѕР»Р°
            float width = innerWidth * gridManager.cellSize;
            float height = innerHeight * gridManager.cellSize;
            float floorThickness = 0.1f;

            // РџРѕР·РёС†РёСЏ - С†РµРЅС‚СЂ РІРЅСѓС‚СЂРµРЅРЅРµР№ РѕР±Р»Р°СЃС‚Рё
            Vector3 centerOffset = new Vector3(
                (roomSize.x - 1) * gridManager.cellSize * 0.5f,
                -floorThickness * 0.5f,
                (roomSize.y - 1) * gridManager.cellSize * 0.5f
            );

            floor.transform.localPosition = centerOffset;
            floor.transform.localScale = new Vector3(width, floorThickness, height);

            // РЈР±РёСЂР°РµРј РєРѕР»Р»Р°Р№РґРµСЂ
            Destroy(floor.GetComponent<Collider>());

            // РџСЂРёРјРµРЅСЏРµРј РїСЂРёР·СЂР°С‡РЅС‹Р№ РјР°С‚РµСЂРёР°Р»
            ApplyGhostMaterial(floor.GetComponent<Renderer>(), true);
        }
    }

    /// <summary>
    /// РЎРѕР·РґР°С‚СЊ РїСЂРёР·СЂР°С‡РЅС‹Рµ СЃС‚РµРЅС‹
    /// </summary>
    void CreateGhostWalls(GameObject parent, Vector2Int gridPosition, Vector2Int roomSize, int rotation = 0)
    {
        // РСЃРїРѕР»СЊР·СѓРµРј С‚Сѓ Р¶Рµ Р»РѕРіРёРєСѓ С‡С‚Рѕ Рё РІ RoomBuilder РґР»СЏ РїРѕР»СѓС‡РµРЅРёСЏ СЃС‚РµРЅ
        List<WallData> walls = GetGhostRoomWalls(gridPosition, roomSize, rotation);

        foreach (WallData wallData in walls)
        {
            CreateGhostWall(parent, wallData, rotation);
        }
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ СЃРїРёСЃРѕРє СЃС‚РµРЅ РґР»СЏ РїСЂРёР·СЂР°С‡РЅРѕР№ РєРѕРјРЅР°С‚С‹ (РєРѕРїРёСЏ Р»РѕРіРёРєРё РёР· RoomBuilder)
    /// </summary>
    List<WallData> GetGhostRoomWalls(Vector2Int gridPosition, Vector2Int roomSize, int rotation = 0)
    {
        List<WallData> walls = new List<WallData>();

        for (int x = 0; x < roomSize.x; x++)
        {
            for (int y = 0; y < roomSize.y; y++)
            {
                Vector2Int cellPos = new Vector2Int(gridPosition.x + x, gridPosition.y + y);

                // РџСЂРѕРІРµСЂСЏРµРј, СЏРІР»СЏРµС‚СЃСЏ Р»Рё РєР»РµС‚РєР° С‡Р°СЃС‚СЊСЋ РїРµСЂРёРјРµС‚СЂР°
                bool isPerimeter = (x == 0 || x == roomSize.x - 1 || y == 0 || y == roomSize.y - 1);

                if (isPerimeter)
                {
                    // РћРїСЂРµРґРµР»СЏРµРј СЃС‚РѕСЂРѕРЅСѓ РєРѕРјРЅР°С‚С‹ РґР»СЏ СЃС‚РµРЅС‹ СЃ СѓС‡РµС‚РѕРј РїРѕРІРѕСЂРѕС‚Р°
                    WallSide wallSide = DetermineGhostWallSide(x, y, roomSize, rotation);
                    WallType wallType = DetermineGhostWallType(wallSide);

                    walls.Add(new WallData(cellPos, WallDirection.Vertical, gridPosition, roomSize, wallSide, wallType, rotation));
                }
            }
        }

        return walls;
    }

    /// <summary>
    /// РћРїСЂРµРґРµР»РёС‚СЊ СЃС‚РѕСЂРѕРЅСѓ РєРѕРјРЅР°С‚С‹ РґР»СЏ РїСЂРёР·СЂР°С‡РЅРѕР№ СЃС‚РµРЅС‹
    /// </summary>
    WallSide DetermineGhostWallSide(int relativeX, int relativeY, Vector2Int roomSize, int rotation = 0)
    {
        bool isLeftEdge = (relativeX == 0);
        bool isRightEdge = (relativeX == roomSize.x - 1);
        bool isTopEdge = (relativeY == roomSize.y - 1);
        bool isBottomEdge = (relativeY == 0);

        WallSide baseSide = WallSide.None;

        // РЎРЅР°С‡Р°Р»Р° РїСЂРѕРІРµСЂСЏРµРј СѓРіР»С‹
        if (isTopEdge && isLeftEdge) baseSide = WallSide.TopLeft;
        else if (isTopEdge && isRightEdge) baseSide = WallSide.TopRight;
        else if (isBottomEdge && isLeftEdge) baseSide = WallSide.BottomLeft;
        else if (isBottomEdge && isRightEdge) baseSide = WallSide.BottomRight;
        // Р—Р°С‚РµРј РѕР±С‹С‡РЅС‹Рµ СЃС‚РѕСЂРѕРЅС‹
        else if (isTopEdge) baseSide = WallSide.Top;
        else if (isBottomEdge) baseSide = WallSide.Bottom;
        else if (isLeftEdge) baseSide = WallSide.Left;
        else if (isRightEdge) baseSide = WallSide.Right;

        // РџСЂРёРјРµРЅСЏРµРј РїРѕРІРѕСЂРѕС‚ Рє РѕРїСЂРµРґРµР»РµРЅРЅРѕР№ СЃС‚РѕСЂРѕРЅРµ
        return RotateWallSide(baseSide, rotation);
    }

    /// <summary>
    /// РџРѕРІРµСЂРЅСѓС‚СЊ СЃС‚РѕСЂРѕРЅСѓ СЃС‚РµРЅС‹ РЅР° Р·Р°РґР°РЅРЅС‹Р№ СѓРіРѕР»
    /// </summary>
    WallSide RotateWallSide(WallSide originalSide, int rotation)
    {
        if (rotation == 0) return originalSide;

        int rotationSteps = (rotation / 90) % 4;
        if (rotationSteps < 0) rotationSteps += 4;

        switch (originalSide)
        {
            case WallSide.Top:
                switch (rotationSteps)
                {
                    case 1: return WallSide.Right;
                    case 2: return WallSide.Bottom;
                    case 3: return WallSide.Left;
                    default: return WallSide.Top;
                }
            case WallSide.Right:
                switch (rotationSteps)
                {
                    case 1: return WallSide.Bottom;
                    case 2: return WallSide.Left;
                    case 3: return WallSide.Top;
                    default: return WallSide.Right;
                }
            case WallSide.Bottom:
                switch (rotationSteps)
                {
                    case 1: return WallSide.Left;
                    case 2: return WallSide.Top;
                    case 3: return WallSide.Right;
                    default: return WallSide.Bottom;
                }
            case WallSide.Left:
                switch (rotationSteps)
                {
                    case 1: return WallSide.Top;
                    case 2: return WallSide.Right;
                    case 3: return WallSide.Bottom;
                    default: return WallSide.Left;
                }
            // РЈРіР»С‹
            case WallSide.TopLeft:
                switch (rotationSteps)
                {
                    case 1: return WallSide.TopRight;
                    case 2: return WallSide.BottomRight;
                    case 3: return WallSide.BottomLeft;
                    default: return WallSide.TopLeft;
                }
            case WallSide.TopRight:
                switch (rotationSteps)
                {
                    case 1: return WallSide.BottomRight;
                    case 2: return WallSide.BottomLeft;
                    case 3: return WallSide.TopLeft;
                    default: return WallSide.TopRight;
                }
            case WallSide.BottomRight:
                switch (rotationSteps)
                {
                    case 1: return WallSide.BottomLeft;
                    case 2: return WallSide.TopLeft;
                    case 3: return WallSide.TopRight;
                    default: return WallSide.BottomRight;
                }
            case WallSide.BottomLeft:
                switch (rotationSteps)
                {
                    case 1: return WallSide.TopLeft;
                    case 2: return WallSide.TopRight;
                    case 3: return WallSide.BottomRight;
                    default: return WallSide.BottomLeft;
                }
            default:
                return originalSide;
        }
    }

    /// <summary>
    /// РћРїСЂРµРґРµР»РёС‚СЊ С‚РёРї РїСЂРёР·СЂР°С‡РЅРѕР№ СЃС‚РµРЅС‹
    /// </summary>
    WallType DetermineGhostWallType(WallSide wallSide)
    {
        switch (wallSide)
        {
            case WallSide.TopLeft:
            case WallSide.TopRight:
            case WallSide.BottomLeft:
            case WallSide.BottomRight:
                return WallType.Corner;
            default:
                return WallType.Straight;
        }
    }

    /// <summary>
    /// РЎРѕР·РґР°С‚СЊ РїСЂРёР·СЂР°С‡РЅСѓСЋ СЃС‚РµРЅСѓ
    /// </summary>
    void CreateGhostWall(GameObject parent, WallData wallData, int roomRotation = 0)
    {
        // Р’С‹Р±РёСЂР°РµРј РїСЂР°РІРёР»СЊРЅС‹Р№ РїСЂРµС„Р°Р±
        GameObject prefabToUse = null;
        if (wallData.wallType == WallType.Corner && RoomBuilder.Instance.wallCornerPrefab != null)
        {
            prefabToUse = RoomBuilder.Instance.wallCornerPrefab;
        }
        else if (RoomBuilder.Instance.wallPrefab != null)
        {
            prefabToUse = RoomBuilder.Instance.wallPrefab;
        }

        if (prefabToUse != null)
        {
            GameObject ghostWall = Instantiate(prefabToUse, parent.transform);

            // РСЃРїРѕР»СЊР·СѓРµРј РѕС‚РЅРѕСЃРёС‚РµР»СЊРЅС‹Рµ РєРѕРѕСЂРґРёРЅР°С‚С‹ РІ РёРјРµРЅРё (wallData.position - СЌС‚Рѕ Р°Р±СЃРѕР»СЋС‚РЅС‹Рµ РєРѕРѕСЂРґРёРЅР°С‚С‹)
            int relativeX = wallData.position.x - wallData.roomPosition.x;
            int relativeY = wallData.position.y - wallData.roomPosition.y;
            ghostWall.name = $"GhostWall_{relativeX}_{relativeY}_{wallData.wallSide}";

            // РџРѕР·РёС†РёРѕРЅРёСЂРѕРІР°РЅРёРµ РІСЂРµРјРµРЅРЅРѕРµ - Р±СѓРґРµС‚ РѕР±РЅРѕРІР»РµРЅРѕ РІ UpdateGhostRoomPosition
            Vector3 worldPos = GridToWorldPosition(wallData.position);
            ghostWall.transform.position = worldPos;

            // РџРѕРІРѕСЂРѕС‚ СЃ СѓС‡РµС‚РѕРј РїРѕРІРѕСЂРѕС‚Р° РєРѕРјРЅР°С‚С‹ - РёСЃРїРѕР»СЊР·СѓРµРј С‚РѕС‚ Р¶Рµ РјРµС‚РѕРґ С‡С‚Рѕ Рё РґР»СЏ РѕР±С‹С‡РЅС‹С… СЃС‚РµРЅ
            float wallRotation = wallData.GetRotationTowardRoom();
            ghostWall.transform.localRotation = Quaternion.Euler(0, wallRotation, 0);

            // РЈР±РёСЂР°РµРј РєРѕР»Р»Р°Р№РґРµСЂС‹
            Collider[] colliders = ghostWall.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                Destroy(col);
            }

            // РџСЂРёРјРµРЅСЏРµРј РїСЂРёР·СЂР°С‡РЅС‹Р№ РјР°С‚РµСЂРёР°Р» РєРѕ РІСЃРµРј СЂРµРЅРґРµСЂРµСЂР°Рј
            Renderer[] renderers = ghostWall.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                ApplyGhostMaterial(renderer, true);
            }
        }
    }


    /// <summary>
    /// РџСЂРµРѕР±СЂР°Р·РѕРІР°РЅРёРµ РєРѕРѕСЂРґРёРЅР°С‚ СЃРµС‚РєРё РІ РјРёСЂРѕРІС‹Рµ (РґР»СЏ РїСЂРёР·СЂР°РєРѕРІ)
    /// </summary>
    Vector3 GridToWorldPosition(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x * gridManager.cellSize + gridManager.cellSize * 0.5f, 0,
                          gridPos.y * gridManager.cellSize + gridManager.cellSize * 0.5f);
    }

    /// <summary>
    /// РћР±РЅРѕРІРёС‚СЊ РїРѕР·РёС†РёРё РІСЃРµС… СЌР»РµРјРµРЅС‚РѕРІ РїСЂРёР·СЂР°С‡РЅРѕР№ РєРѕРјРЅР°С‚С‹
    /// </summary>
    void UpdateGhostRoomPosition(GameObject ghostRoom, Vector2Int gridPos, Vector2Int roomSize)
    {
        if (ghostRoom == null) return;

        // РќР°С…РѕРґРёРј РІСЃРµ РїСЂРёР·СЂР°С‡РЅС‹Рµ СЃС‚РµРЅС‹ Рё РѕР±РЅРѕРІР»СЏРµРј РёС… РїРѕР·РёС†РёРё
        Transform[] children = ghostRoom.GetComponentsInChildren<Transform>();
        foreach (Transform child in children)
        {
            if (child.name.StartsWith("GhostWall_"))
            {
                // РР·РІР»РµРєР°РµРј РѕС‚РЅРѕСЃРёС‚РµР»СЊРЅС‹Рµ РєРѕРѕСЂРґРёРЅР°С‚С‹ РёР· РёРјРµРЅРё СЃС‚РµРЅС‹
                string[] nameParts = child.name.Split('_');
                if (nameParts.Length >= 3)
                {
                    int relativeX = int.Parse(nameParts[1]);
                    int relativeY = int.Parse(nameParts[2]);

                    // Р’С‹С‡РёСЃР»СЏРµРј Р°Р±СЃРѕР»СЋС‚РЅС‹Рµ РєРѕРѕСЂРґРёРЅР°С‚С‹ СЃС‚РµРЅС‹
                    Vector2Int absolutePos = new Vector2Int(gridPos.x + relativeX, gridPos.y + relativeY);

                    // РћР±РЅРѕРІР»СЏРµРј РїРѕР·РёС†РёСЋ СЃС‚РµРЅС‹
                    Vector3 worldPos = GridToWorldPosition(absolutePos);
                    child.transform.position = worldPos;
                }
            }
        }

        // РЈСЃС‚Р°РЅР°РІР»РёРІР°РµРј Р±Р°Р·РѕРІСѓСЋ РїРѕР·РёС†РёСЋ СЂРѕРґРёС‚РµР»СЊСЃРєРѕРіРѕ РѕР±СЉРµРєС‚Р°
        ghostRoom.transform.position = Vector3.zero;
    }

    /// <summary>
    /// РџСЂРёРјРµРЅРёС‚СЊ РїСЂРёР·СЂР°С‡РЅС‹Р№ РјР°С‚РµСЂРёР°Р» Рє СЂРµРЅРґРµСЂРµСЂСѓ
    /// </summary>
    void ApplyGhostMaterial(Renderer renderer, bool canPlace)
    {
        if (renderer == null) return;

        Material ghostMaterial = null;
        if (canPlace)
        {
            ghostMaterial = Resources.Load<Material>("Materials/GhostGreen");
        }
        else
        {
            ghostMaterial = Resources.Load<Material>("Materials/GhostRed");
        }

        if (ghostMaterial != null)
        {
            renderer.material = ghostMaterial;
        }
    }

    /// <summary>
    /// РћР±РЅРѕРІР»РµРЅРёРµ РїСЂРµРґРІР°СЂРёС‚РµР»СЊРЅРѕРіРѕ РїСЂРѕСЃРјРѕС‚СЂР°
    /// </summary>
    void UpdatePreview()
    {
        if (previewObject == null || playerCamera == null)
            return;

        // Р’ С„Р°Р·Рµ СЂР°Р·РјРµС‰РµРЅРёСЏ РґРІРµСЂРё РїСЂРёР·СЂР°Рє РєРѕРјРЅР°С‚С‹ РґРѕР»Р¶РµРЅ РѕСЃС‚Р°РІР°С‚СЊСЃСЏ Р·Р°С„РёРєСЃРёСЂРѕРІР°РЅРЅС‹Рј
        if (currentPhase == BuildingPhase.PlacingDoor)
        {
            // РћР±РЅРѕРІР»СЏРµРј С‚РѕР»СЊРєРѕ С†РІРµС‚ РїСЂРёР·СЂР°РєР° РєРѕРјРЅР°С‚С‹ (Р·РµР»РµРЅС‹Р№, С‚Р°Рє РєР°Рє РїРѕР·РёС†РёСЏ СѓР¶Рµ РІС‹Р±СЂР°РЅР°)
            UpdatePreviewColor(true);
            return;
        }

        // РўРѕР»СЊРєРѕ РІ С„Р°Р·Рµ СЂР°Р·РјРµС‰РµРЅРёСЏ РєРѕРјРЅР°С‚С‹ РѕР±РЅРѕРІР»СЏРµРј РїРѕР·РёС†РёСЋ РїСЂРёР·СЂР°РєР°
        if (currentPhase != BuildingPhase.PlacingRoom)
            return;

        // РџРѕР»СѓС‡Р°РµРј РїРѕР·РёС†РёСЋ РјС‹С€Рё РІ РјРёСЂРµ
        Vector3 mousePosition = Input.mousePosition;
        Ray ray = playerCamera.ScreenPointToRay(mousePosition);

        Vector3 worldPos;
        Vector2Int gridPos;

        // РџРѕРїСЂРѕР±СѓРµРј raycast РЅР° Р·РµРјР»СЋ/РѕР±СЉРµРєС‚С‹
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            worldPos = hit.point;
            gridPos = gridManager.WorldToGrid(worldPos);
        }
        else
        {
            // Р•СЃР»Рё raycast РЅРµ РїРѕРїР°Р», РёСЃРїРѕР»СЊР·СѓРµРј РїР»РѕСЃРєРѕСЃС‚СЊ Y=0
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float distance))
            {
                worldPos = ray.GetPoint(distance);
                gridPos = gridManager.WorldToGrid(worldPos);
            }
            else
            {
                return; // РќРµ РјРѕР¶РµРј РѕРїСЂРµРґРµР»РёС‚СЊ РїРѕР·РёС†РёСЋ
            }
        }

        Vector3 snapPosition = gridManager.GridToWorld(gridPos);

        // РћР±РЅРѕРІР»СЏРµРј РїСЂРёР·СЂР°С‡РЅСѓСЋ РєРѕРјРЅР°С‚Сѓ СЃ РїСЂР°РІРёР»СЊРЅС‹РјРё РєРѕРѕСЂРґРёРЅР°С‚Р°РјРё
        RoomData currentRoom = availableRooms[selectedRoomIndex];
        Vector2Int rotatedSize = GetRotatedRoomSize(currentRoom.size, roomRotation);
        UpdateGhostRoomPosition(previewObject, gridPos, rotatedSize);

        // РЈР±РёСЂР°РµРј РёРЅРґРёРєР°С‚РѕСЂС‹ РєР»РµС‚РѕРє - С‚РµРїРµСЂСЊ РёСЃРїРѕР»СЊР·СѓРµРј РїРѕР»РЅРѕС†РµРЅРЅС‹Р№ РїСЂРёР·СЂР°Рє РєРѕРјРЅР°С‚С‹
        // UpdatePreviewCells(gridPos, currentRoom);

        // РџСЂРѕРІРµСЂСЏРµРј, РјРѕР¶РЅРѕ Р»Рё СЂР°Р·РјРµСЃС‚РёС‚СЊ РєРѕРјРЅР°С‚Сѓ Рё РѕР±РЅРѕРІР»СЏРµРј С†РІРµС‚ РїСЂРёР·СЂР°РєР°
        bool canPlace = CanPlaceRoom(gridPos, currentRoom, roomRotation);
        UpdatePreviewColor(canPlace);

    }

    /// <summary>
    /// РћР±РЅРѕРІРёС‚СЊ С†РІРµС‚ РїСЂРёР·СЂР°РєР° Р·РґР°РЅРёСЏ РІ Р·Р°РІРёСЃРёРјРѕСЃС‚Рё РѕС‚ РІРѕР·РјРѕР¶РЅРѕСЃС‚Рё СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
    /// </summary>
    void UpdatePreviewColor(bool canPlace)
    {
        if (previewObject == null) return;

        // РћР±РЅРѕРІР»СЏРµРј РјР°С‚РµСЂРёР°Р»С‹ РІСЃРµС… СЂРµРЅРґРµСЂРµСЂРѕРІ РІ РїСЂРёР·СЂР°С‡РЅРѕР№ РєРѕРјРЅР°С‚Рµ
        Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            ApplyGhostMaterial(renderer, canPlace);
        }
    }

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚РєР° РІРІРѕРґР° РІ СЂРµР¶РёРјРµ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
    /// </summary>
    void HandleBuildingInput()
    {
        // РџСЂРѕРІРµСЂСЏРµРј, РЅРµ РЅР°С…РѕРґРёС‚СЃСЏ Р»Рё РјС‹С€СЊ РЅР°Рґ UI СЌР»РµРјРµРЅС‚РѕРј
        bool isPointerOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

        // РћР±СЂР°Р±Р°С‚С‹РІР°РµРј РІРІРѕРґ РІ Р·Р°РІРёСЃРёРјРѕСЃС‚Рё РѕС‚ С‚РµРєСѓС‰РµР№ С„Р°Р·С‹ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
        switch (currentPhase)
        {
            case BuildingPhase.PlacingRoom:
                HandleRoomPlacementInput(isPointerOverUI);
                break;
            case BuildingPhase.PlacingDoor:
                HandleDoorPlacementInput(isPointerOverUI);
                break;
        }

        // РћР±С‰РёРµ РєР»Р°РІРёС€Рё РґР»СЏ РІСЃРµС… С„Р°Р·
        HandleCommonBuildingInput();
    }

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚РєР° РІРІРѕРґР° РґР»СЏ С„Р°Р·С‹ СЂР°Р·РјРµС‰РµРЅРёСЏ РєРѕРјРЅР°С‚С‹
    /// </summary>
    void HandleRoomPlacementInput(bool isPointerOverUI)
    {
        // Р›РљРњ - Р·Р°С„РёРєСЃРёСЂРѕРІР°С‚СЊ РїРѕР·РёС†РёСЋ РєРѕРјРЅР°С‚С‹ Рё РїРµСЂРµР№С‚Рё Рє СЂР°Р·РјРµС‰РµРЅРёСЋ РґРІРµСЂРё
        if (Input.GetMouseButtonDown(0) && !isPointerOverUI)
        {
            TryPlaceRoomGhost();
        }

        // РџРљРњ - РѕС‚РјРµРЅРёС‚СЊ РІС‹Р±РѕСЂ РєРѕРјРЅР°С‚С‹ РёР»Рё РІС‹Р№С‚Рё РёР· СЂРµР¶РёРјР° СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
        if (Input.GetMouseButtonDown(1) && !isPointerOverUI)
        {
            if (selectedRoomIndex >= 0)
            {
                ClearRoomSelection();
            }
            else
            {
                SetBuildMode(false);
            }
        }
    }

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚РєР° РІРІРѕРґР° РґР»СЏ С„Р°Р·С‹ СЂР°Р·РјРµС‰РµРЅРёСЏ РґРІРµСЂРё
    /// </summary>
    void HandleDoorPlacementInput(bool isPointerOverUI)
    {
        FileLogger.Log($"DEBUG: HandleDoorPlacementInput called - doorPosition: {doorPosition}, timer: {doorSelectionTimer:F2}, roomBuilt: {roomBuilt}");

        // РћР±РЅРѕРІР»СЏРµРј РїРѕР·РёС†РёСЋ РґРІРµСЂРё РїРѕ РґРІРёР¶РµРЅРёСЋ РјС‹С€Рё
        UpdateDoorPosition();

        // Р›РљРњ - РјРіРЅРѕРІРµРЅРЅРѕ С„РёРЅР°Р»РёР·РёСЂРѕРІР°С‚СЊ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІРѕ
        if (Input.GetMouseButtonDown(0) && !isPointerOverUI && !roomBuilt)
        {
            FileLogger.Log("DEBUG: Manual build triggered by LEFT CLICK");
            TryFinalizeBuildRoom();
        }

        // РђРІС‚РѕРјР°С‚РёС‡РµСЃРєРѕРµ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІРѕ РћРўРљР›Р®Р§Р•РќРћ - С‚РѕР»СЊРєРѕ СЂСѓС‡РЅРѕРµ РїРѕРґС‚РІРµСЂР¶РґРµРЅРёРµ Р›РљРњ

        // РџРљРњ - РІРµСЂРЅСѓС‚СЊСЃСЏ Рє СЂР°Р·РјРµС‰РµРЅРёСЋ РєРѕРјРЅР°С‚С‹
        if (Input.GetMouseButtonDown(1) && !isPointerOverUI)
        {
            ReturnToRoomPlacement();
        }
    }

    /// <summary>
    /// РћР±РЅРѕРІРёС‚СЊ РїРѕР·РёС†РёСЋ РґРІРµСЂРё РІ Р·Р°РІРёСЃРёРјРѕСЃС‚Рё РѕС‚ РїРѕР·РёС†РёРё РјС‹С€Рё
    /// </summary>
    void UpdateDoorPosition()
    {
        Vector2Int mouseGridPos = GetGridPositionFromMouse();
        FileLogger.Log($"DEBUG: UpdateDoorPosition - mouseGridPos: {mouseGridPos}, current doorPosition: {doorPosition}");

        // РќР°С…РѕРґРёРј Р±Р»РёР¶Р°Р№С€СѓСЋ РїСЂСЏРјСѓСЋ СЃС‚РµРЅСѓ Рє РїРѕР·РёС†РёРё РјС‹С€Рё
        Vector2Int closestWallPos = doorPosition; // РїРѕ СѓРјРѕР»С‡Р°РЅРёСЋ С‚РµРєСѓС‰Р°СЏ РїРѕР·РёС†РёСЏ
        float minDistance = float.MaxValue;

        foreach (Vector2Int wallPos in straightWallPositions)
        {
            float distance = Vector2Int.Distance(mouseGridPos, wallPos);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestWallPos = wallPos;
            }
        }

        // РћР±РЅРѕРІР»СЏРµРј РїРѕР·РёС†РёСЋ РґРІРµСЂРё РµСЃР»Рё РѕРЅР° РёР·РјРµРЅРёР»Р°СЃСЊ
        if (closestWallPos != doorPosition)
        {
            FileLogger.Log($"DEBUG: Door position changed from {doorPosition} to {closestWallPos}");
            doorPosition = closestWallPos;
            UpdateDoorPreviewPosition();
        }
        else
        {
            FileLogger.Log($"DEBUG: Door position unchanged: {doorPosition}");
        }
    }

    /// <summary>
    /// РћР±С‰РёРµ РєР»Р°РІРёС€Рё РґР»СЏ РІСЃРµС… С„Р°Р· СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
    /// </summary>
    void HandleCommonBuildingInput()
    {
        // Q Рё E - РїРѕРІРѕСЂРѕС‚ РєРѕРјРЅР°С‚С‹ (С‚РѕР»СЊРєРѕ РІ С„Р°Р·Рµ СЂР°Р·РјРµС‰РµРЅРёСЏ РєРѕРјРЅР°С‚С‹)
        if (currentPhase == BuildingPhase.PlacingRoom)
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                RotateRoom(-90);
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                RotateRoom(90);
            }

            // Р РѕР»РёРє РјС‹С€РєРё - РїРѕРІРѕСЂРѕС‚ (С‚РѕР»СЊРєРѕ РІ С„Р°Р·Рµ СЂР°Р·РјРµС‰РµРЅРёСЏ РєРѕРјРЅР°С‚С‹)
            float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scrollWheel) > 0.0001f)
            {
                scrollWheelUsedThisFrame = true;
                if (scrollWheel > 0)
                {
                    RotateRoom(90);
                }
                else
                {
                    RotateRoom(-90);
                }
            }
        }
    }

    /// <summary>
    /// РџРѕРїС‹С‚РєР° Р·Р°С„РёРєСЃРёСЂРѕРІР°С‚СЊ РїРѕР·РёС†РёСЋ РєРѕРјРЅР°С‚С‹ Рё РїРµСЂРµР№С‚Рё Рє СЂР°Р·РјРµС‰РµРЅРёСЋ РґРІРµСЂРё
    /// </summary>
    void TryPlaceRoomGhost()
    {
        Vector2Int gridPosition = GetGridPositionFromMouse();
        if (selectedRoomIndex < 0 || selectedRoomIndex >= availableRooms.Count) return;

        RoomData roomData = availableRooms[selectedRoomIndex];
        if (!CanPlaceRoom(gridPosition, roomData, roomRotation))
        {
            FileLogger.Log("Cannot place room at this position");
            return;
        }

        // РЎРѕС…СЂР°РЅСЏРµРј РґР°РЅРЅС‹Рµ Рѕ СЂР°Р·РјРµС‰Р°РµРјРѕР№ РєРѕРјРЅР°С‚Рµ
        pendingRoomPosition = gridPosition;
        pendingRoomSize = GetRotatedRoomSize(roomData.size, roomRotation);
        pendingRoomRotation = roomRotation;

        // РџРµСЂРµС…РѕРґРёРј Рє С„Р°Р·Рµ СЂР°Р·РјРµС‰РµРЅРёСЏ РґРІРµСЂРё
        currentPhase = BuildingPhase.PlacingDoor;

        // РЎР±СЂР°СЃС‹РІР°РµРј С„Р»Р°РіРё Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРѕРіРѕ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
        doorSelectionTimer = 0f;
        roomBuilt = false;
        lastDoorPosition = Vector2Int.zero;

        // Р¤РёРєСЃРёСЂСѓРµРј РїСЂРёР·СЂР°Рє РєРѕРјРЅР°С‚С‹ РІ РІС‹Р±СЂР°РЅРЅРѕР№ РїРѕР·РёС†РёРё
        UpdateGhostRoomPosition(previewObject, pendingRoomPosition, pendingRoomSize);

        // РќР°С…РѕРґРёРј РІСЃРµ РїСЂСЏРјС‹Рµ СЃС‚РµРЅС‹ РґР»СЏ СЂР°Р·РјРµС‰РµРЅРёСЏ РґРІРµСЂРё
        FindStraightWallPositions();

        // РЎРѕР·РґР°РµРј РїСЂРёР·СЂР°Рє РґРІРµСЂРё
        CreateDoorPreview();

        FileLogger.Log("Phase 2: Placing door - room ghost locked");
    }

    /// <summary>
    /// РќР°Р№С‚Рё РїРѕР·РёС†РёРё РїСЂСЏРјС‹С… СЃС‚РµРЅ РґР»СЏ СЂР°Р·РјРµС‰РµРЅРёСЏ РґРІРµСЂРё
    /// </summary>
    void FindStraightWallPositions()
    {
        FileLogger.Log($"DEBUG: FindStraightWallPositions - room at {pendingRoomPosition}, size {pendingRoomSize}");
        straightWallPositions.Clear();

        for (int x = 0; x < pendingRoomSize.x; x++)
        {
            for (int y = 0; y < pendingRoomSize.y; y++)
            {
                Vector2Int cellPos = new Vector2Int(pendingRoomPosition.x + x, pendingRoomPosition.y + y);

                // РџСЂРѕРІРµСЂСЏРµРј, СЏРІР»СЏРµС‚СЃСЏ Р»Рё РєР»РµС‚РєР° С‡Р°СЃС‚СЊСЋ РїРµСЂРёРјРµС‚СЂР°
                bool isPerimeter = (x == 0 || x == pendingRoomSize.x - 1 || y == 0 || y == pendingRoomSize.y - 1);

                if (isPerimeter)
                {
                    // РџСЂРѕРІРµСЂСЏРµРј, СЏРІР»СЏРµС‚СЃСЏ Р»Рё СЌС‚Рѕ РїСЂСЏРјРѕР№ СЃС‚РµРЅРѕР№ (РЅРµ СѓРіРѕР»)
                    bool isCorner = (x == 0 && y == 0) ||
                                   (x == 0 && y == pendingRoomSize.y - 1) ||
                                   (x == pendingRoomSize.x - 1 && y == 0) ||
                                   (x == pendingRoomSize.x - 1 && y == pendingRoomSize.y - 1);

                    if (!isCorner)
                    {
                        straightWallPositions.Add(cellPos);
                        FileLogger.Log($"DEBUG: Added straight wall position: {cellPos}");
                    }
                    else
                    {
                        FileLogger.Log($"DEBUG: Skipped corner position: {cellPos}");
                    }
                }
            }
        }

        // РЈСЃС‚Р°РЅР°РІР»РёРІР°РµРј РЅР°С‡Р°Р»СЊРЅСѓСЋ РїРѕР·РёС†РёСЋ РґРІРµСЂРё РЅР° РїРµСЂРІРѕР№ РґРѕСЃС‚СѓРїРЅРѕР№ РїСЂСЏРјРѕР№ СЃС‚РµРЅРµ
        if (straightWallPositions.Count > 0)
        {
            doorPosition = straightWallPositions[0];
            FileLogger.Log($"DEBUG: Set initial door position to: {doorPosition}");
        }
        else
        {
            FileLogger.Log("ERROR: No straight wall positions found for door placement!");
        }

        FileLogger.Log($"DEBUG: Found {straightWallPositions.Count} straight wall positions for door placement");
    }

    /// <summary>
    /// РЎРѕР·РґР°С‚СЊ РїСЂРёР·СЂР°Рє РґРІРµСЂРё
    /// </summary>
    void CreateDoorPreview()
    {
        if (doorPreviewObject != null)
            DestroyImmediate(doorPreviewObject);

        // Р—Р°РіСЂСѓР¶Р°РµРј РїСЂРµС„Р°Р± РґРІРµСЂРё
        GameObject doorPrefab = Resources.Load<GameObject>("Prefabs/SM_Door");
        if (doorPrefab == null)
        {
            // Fallback: СЃРѕР·РґР°РµРј РїСЂРѕСЃС‚РѕР№ РєСѓР± РµСЃР»Рё РїСЂРµС„Р°Р± РЅРµ РЅР°Р№РґРµРЅ
            doorPreviewObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorPreviewObject.name = "DoorPreview_Fallback";
            DestroyImmediate(doorPreviewObject.GetComponent<Collider>());
        }
        else
        {
            // РЎРѕР·РґР°РµРј СЌРєР·РµРјРїР»СЏСЂ РїСЂРµС„Р°Р±Р° РґРІРµСЂРё
            doorPreviewObject = Instantiate(doorPrefab);
            doorPreviewObject.name = "DoorPreview";

            // РЈР±РёСЂР°РµРј РєРѕР»Р»Р°Р№РґРµСЂС‹ Сѓ РїСЂРёР·СЂР°РєР°
            Collider[] colliders = doorPreviewObject.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
                DestroyImmediate(col);
        }

        // РЈР±РёСЂР°РµРј РєСЂР°СЃРЅСѓСЋ РїРѕРґСЃРІРµС‚РєСѓ - РґРІРµСЂСЊ РѕСЃС‚Р°РµС‚СЃСЏ РІ РѕСЂРёРіРёРЅР°Р»СЊРЅРѕРј РІРёРґРµ
        // РџСЂРѕСЃС‚Рѕ РґРµР»Р°РµРј РґРІРµСЂРё РїРѕР»СѓРїСЂРѕР·СЂР°С‡РЅС‹РјРё С‡С‚РѕР±С‹ РїРѕРєР°Р·Р°С‚СЊ С‡С‚Рѕ СЌС‚Рѕ РїСЂРёР·СЂР°Рє
        Renderer[] renderers = doorPreviewObject.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer.material != null)
            {
                Material ghostMaterial = new Material(renderer.material);
                Color originalColor = ghostMaterial.color;
                ghostMaterial.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.7f);

                // РќР°СЃС‚СЂРѕР№РєР° РїСЂРѕР·СЂР°С‡РЅРѕСЃС‚Рё
                if (ghostMaterial.HasProperty("_Mode"))
                    ghostMaterial.SetFloat("_Mode", 3); // Transparent mode
                if (ghostMaterial.HasProperty("_SrcBlend"))
                    ghostMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                if (ghostMaterial.HasProperty("_DstBlend"))
                    ghostMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                if (ghostMaterial.HasProperty("_ZWrite"))
                    ghostMaterial.SetInt("_ZWrite", 0);
                ghostMaterial.renderQueue = 3000;

                renderer.material = ghostMaterial;
            }
        }

        // РџРѕР·РёС†РёРѕРЅРёСЂСѓРµРј РїСЂРёР·СЂР°Рє РґРІРµСЂРё
        UpdateDoorPreviewPosition();
    }

    /// <summary>
    /// РћР±РЅРѕРІРёС‚СЊ РїРѕР·РёС†РёСЋ РїСЂРёР·СЂР°РєР° РґРІРµСЂРё
    /// </summary>
    void UpdateDoorPreviewPosition()
    {
        if (doorPreviewObject == null) return;

        Vector3 worldPos = gridManager.GridToWorld(doorPosition);
        doorPreviewObject.transform.position = worldPos;

        // РџРѕР»СѓС‡Р°РµРј РѕСЂРёРµРЅС‚Р°С†РёСЋ СЃС‚РµРЅС‹ РІ СЌС‚РѕР№ РїРѕР·РёС†РёРё РґР»СЏ РїСЂР°РІРёР»СЊРЅРѕРіРѕ РїРѕРІРѕСЂРѕС‚Р° РґРІРµСЂРё
        float doorRotation = GetWallRotationAtPosition(doorPosition);
        doorPreviewObject.transform.rotation = Quaternion.Euler(0, doorRotation, 0);

        // Р•СЃР»Рё СЌС‚Рѕ fallback РєСѓР±, СѓСЃС‚Р°РЅР°РІР»РёРІР°РµРј СЂР°Р·РјРµСЂ
        if (doorPreviewObject.name.Contains("Fallback"))
        {
            doorPreviewObject.transform.localScale = new Vector3(gridManager.cellSize * 0.8f, 2f, gridManager.cellSize * 0.8f);
        }
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РѕСЂРёРµРЅС‚Р°С†РёСЋ СЃС‚РµРЅС‹ РІ СѓРєР°Р·Р°РЅРЅРѕР№ РїРѕР·РёС†РёРё
    /// </summary>
    float GetWallRotationAtPosition(Vector2Int position)
    {
        // РџРѕР»СѓС‡Р°РµРј РѕСЂРёРіРёРЅР°Р»СЊРЅС‹Р№ СЂР°Р·РјРµСЂ РєРѕРјРЅР°С‚С‹ (РґРѕ РїРѕРІРѕСЂРѕС‚Р°)
        RoomData currentRoom = availableRooms[selectedRoomIndex];
        Vector2Int originalRoomSize = currentRoom.size;

        // РћРїСЂРµРґРµР»СЏРµРј РіРґРµ РЅР°С…РѕРґРёС‚СЃСЏ РїРѕР·РёС†РёСЏ РѕС‚РЅРѕСЃРёС‚РµР»СЊРЅРѕ РєРѕРјРЅР°С‚С‹
        Vector2Int relativePos = position - pendingRoomPosition;

        // РћРїСЂРµРґРµР»СЏРµРј СЃС‚РѕСЂРѕРЅСѓ СЃС‚РµРЅС‹ РІ РїРѕРІРµСЂРЅСѓС‚РѕР№ РєРѕРјРЅР°С‚Рµ РёСЃРїРѕР»СЊР·СѓСЏ С‚Сѓ Р¶Рµ Р»РѕРіРёРєСѓ С‡С‚Рѕ Рё РІ RoomBuilder
        WallSide wallSide = DetermineWallSideInRotatedRoom(relativePos, pendingRoomSize, pendingRoomRotation);

        // РџРѕР»СѓС‡Р°РµРј РїРѕРІРѕСЂРѕС‚ СЃС‚РµРЅС‹ РёСЃРїРѕР»СЊР·СѓСЏ С‚РѕС‚ Р¶Рµ Р°Р»РіРѕСЂРёС‚Рј С‡С‚Рѕ Рё РІ RoomBuilder
        return GetWallRotationFromSide(wallSide, pendingRoomRotation);
    }

    /// <summary>
    /// РћРїСЂРµРґРµР»РёС‚СЊ СЃС‚РѕСЂРѕРЅСѓ СЃС‚РµРЅС‹ РІ РїРѕРІРµСЂРЅСѓС‚РѕР№ РєРѕРјРЅР°С‚Рµ (РєРѕРїРёСЏ Р»РѕРіРёРєРё РёР· RoomBuilder)
    /// </summary>
    WallSide DetermineWallSideInRotatedRoom(Vector2Int relativePos, Vector2Int roomSize, int rotation)
    {
        bool isLeftEdge = (relativePos.x == 0);
        bool isRightEdge = (relativePos.x == roomSize.x - 1);
        bool isTopEdge = (relativePos.y == roomSize.y - 1);
        bool isBottomEdge = (relativePos.y == 0);

        WallSide baseSide = WallSide.None;

        // РЎРЅР°С‡Р°Р»Р° РїСЂРѕРІРµСЂСЏРµРј СѓРіР»С‹ (РєРѕРјР±РёРЅР°С†РёРё СЃС‚РѕСЂРѕРЅ)
        if (isTopEdge && isLeftEdge) baseSide = WallSide.TopLeft;
        else if (isTopEdge && isRightEdge) baseSide = WallSide.TopRight;
        else if (isBottomEdge && isLeftEdge) baseSide = WallSide.BottomLeft;
        else if (isBottomEdge && isRightEdge) baseSide = WallSide.BottomRight;
        // Р—Р°С‚РµРј РїСЂРѕРІРµСЂСЏРµРј РѕР±С‹С‡РЅС‹Рµ СЃС‚РѕСЂРѕРЅС‹
        else if (isTopEdge) baseSide = WallSide.Top;
        else if (isBottomEdge) baseSide = WallSide.Bottom;
        else if (isLeftEdge) baseSide = WallSide.Left;
        else if (isRightEdge) baseSide = WallSide.Right;

        // РџСЂРёРјРµРЅСЏРµРј РїРѕРІРѕСЂРѕС‚ Рє РѕРїСЂРµРґРµР»РµРЅРЅРѕР№ СЃС‚РѕСЂРѕРЅРµ (С‚Р° Р¶Рµ Р»РѕРіРёРєР° С‡С‚Рѕ РІ RoomBuilder)
        return RotateWallSide(baseSide, rotation);
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РїРѕРІРѕСЂРѕС‚ СЃС‚РµРЅС‹ РѕС‚ СЃС‚РѕСЂРѕРЅС‹ (РєРѕРїРёСЏ Р»РѕРіРёРєРё РёР· RoomBuilder.GetRotationTowardRoom)
    /// </summary>
    float GetWallRotationFromSide(WallSide wallSide, int roomRotation)
    {
        // РџРѕР»СѓС‡Р°РµРј Р±Р°Р·РѕРІС‹Р№ РїРѕРІРѕСЂРѕС‚ РґР»СЏ СЃС‚РµРЅС‹ РєР°Рє Р±СѓРґС‚Рѕ РєРѕРјРЅР°С‚Р° РїРѕРІРµСЂРЅСѓС‚Р° РЅР° 0В°
        float baseRotation;
        switch (wallSide)
        {
            // РџСЂСЏРјС‹Рµ СЃС‚РµРЅС‹ - СЃРјРѕС‚СЂСЏС‚ РІРЅСѓС‚СЂСЊ РєРѕРјРЅР°С‚С‹
            case WallSide.Top:    baseRotation = 180f; break; // СЃРјРѕС‚СЂРёС‚ РІРЅРёР·
            case WallSide.Bottom: baseRotation = 0f; break;   // СЃРјРѕС‚СЂРёС‚ РІРІРµСЂС…
            case WallSide.Left:   baseRotation = 90f; break;  // СЃРјРѕС‚СЂРёС‚ РІРїСЂР°РІРѕ
            case WallSide.Right:  baseRotation = 270f; break; // СЃРјРѕС‚СЂРёС‚ РІР»РµРІРѕ

            // РЈРіР»РѕРІС‹Рµ СЃС‚РµРЅС‹ (L-РѕР±СЂР°Р·РЅС‹Рµ) - С‚РѕС‡РЅРѕРµ СЃРѕРІРїР°РґРµРЅРёРµ РєРѕРЅРЅРµРєС‚РѕСЂРѕРІ
            case WallSide.TopLeft:     baseRotation = 90f; break;
            case WallSide.TopRight:    baseRotation = 180f; break;
            case WallSide.BottomLeft:  baseRotation = 0f; break;
            case WallSide.BottomRight: baseRotation = 270f; break;

            default: baseRotation = 0f; break;
        }

        // РљРѕРјРїРµРЅСЃРёСЂСѓРµРј РїРѕРІРѕСЂРѕС‚ РєРѕРјРЅР°С‚С‹: РІС‹С‡РёС‚Р°РµРј roomRotation РёР· Р±Р°Р·РѕРІРѕРіРѕ РїРѕРІРѕСЂРѕС‚Р°
        float finalRotation = (baseRotation - roomRotation) % 360f;
        if (finalRotation < 0) finalRotation += 360f;

        return finalRotation;
    }

    /// <summary>
    /// Р¤РёРЅР°Р»РёР·РёСЂРѕРІР°С‚СЊ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІРѕ РєРѕРјРЅР°С‚С‹ СЃ РґРІРµСЂСЊСЋ
    /// </summary>
    void TryFinalizeBuildRoom()
    {
        if (selectedRoomIndex < 0 || selectedRoomIndex >= availableRooms.Count) return;

        RoomData roomData = availableRooms[selectedRoomIndex];

        FileLogger.Log("=== DEBUG: STARTING ROOM FINALIZATION ===");
        FileLogger.Log($"Building room at {pendingRoomPosition}, size {pendingRoomSize}, rotation {pendingRoomRotation}");
        FileLogger.Log($"Door will be placed at {doorPosition}");

        // Р›РѕРіРёСЂСѓРµРј РІСЃРµ СЃСѓС‰РµСЃС‚РІСѓСЋС‰РёРµ РґРІРµСЂРё РїРµСЂРµРґ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІРѕРј
        LogExistingDoors();

        // РЎС‚СЂРѕРёРј РєРѕРјРЅР°С‚Сѓ Р‘Р•Р— СЃС‚РµРЅС‹ РІ РїРѕР·РёС†РёРё РґРІРµСЂРё
        FileLogger.Log("Building room walls (excluding door position)...");
        BuildRoomWithDoor(pendingRoomPosition, roomData, pendingRoomRotation, doorPosition);

        // РЎРѕР·РґР°РµРј РґРІРµСЂСЊ РІ РІС‹Р±СЂР°РЅРЅРѕР№ РїРѕР·РёС†РёРё
        FileLogger.Log($"Creating door at {doorPosition}...");
        CreateDoorAtPosition(doorPosition);

        // РћСЃРІРѕР±РѕР¶РґР°РµРј РєР»РµС‚РєСѓ РіРґРµ СЃС‚РѕРёС‚ РґРІРµСЂСЊ
        FileLogger.Log($"Freeing door cell at {doorPosition}...");
        gridManager.FreeCell(doorPosition);

        // Р›РѕРіРёСЂСѓРµРј РІСЃРµ РґРІРµСЂРё РїРѕСЃР»Рµ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
        LogExistingDoors();

        // РћС‡РёС‰Р°РµРј РїСЂРёР·СЂР°РєРё
        if (previewObject != null)
            DestroyImmediate(previewObject);
        if (doorPreviewObject != null)
            DestroyImmediate(doorPreviewObject);

        // РџРѕРјРµС‡Р°РµРј С‡С‚Рѕ РєРѕРјРЅР°С‚Р° РїРѕСЃС‚СЂРѕРµРЅР°
        roomBuilt = true;

        // Р’РѕР·РІСЂР°С‰Р°РµРјСЃСЏ Рє С„Р°Р·Рµ СЂР°Р·РјРµС‰РµРЅРёСЏ РєРѕРјРЅР°С‚С‹ РґР»СЏ СЃР»РµРґСѓСЋС‰РµРіРѕ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
        currentPhase = BuildingPhase.PlacingRoom;
        roomRotation = 0; // РЎР±СЂР°СЃС‹РІР°РµРј РїРѕРІРѕСЂРѕС‚

        // РЎР±СЂР°СЃС‹РІР°РµРј С‚Р°Р№РјРµСЂ Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРѕРіРѕ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
        doorSelectionTimer = 0f;
        roomBuilt = false;

        CreatePreviewObject(); // РЎРѕР·РґР°РµРј РЅРѕРІС‹Р№ РїСЂРёР·СЂР°Рє РєРѕРјРЅР°С‚С‹

        FileLogger.Log($"=== DEBUG: ROOM FINALIZATION COMPLETE ===");
    }

    /// <summary>
    /// Р›РѕРіРёСЂРѕРІР°С‚СЊ РІСЃРµ СЃСѓС‰РµСЃС‚РІСѓСЋС‰РёРµ РґРІРµСЂРё РЅР° СЃС†РµРЅРµ
    /// </summary>
    void LogExistingDoors()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int doorCount = 0;
        FileLogger.Log("--- Existing doors on scene ---");
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Door") && obj.activeInHierarchy)
            {
                Vector3 objWorldPos = obj.transform.position;
                Vector2Int objGridPos = gridManager.WorldToGrid(objWorldPos);
                FileLogger.Log($"Door found: {obj.name} at world {objWorldPos} grid {objGridPos}");
                doorCount++;
            }
        }
        FileLogger.Log($"Total doors found: {doorCount}");
        FileLogger.Log("--- End of door list ---");
    }

    /// <summary>
    /// РЎС‚СЂРѕРёС‚СЊ РєРѕРјРЅР°С‚Сѓ СЃ РґРІРµСЂСЊСЋ (РёСЃРєР»СЋС‡Р°СЏ РїРѕР·РёС†РёСЋ РґРІРµСЂРё РёР· СЃС‚РµРЅ)
    /// </summary>
    void BuildRoomWithDoor(Vector2Int gridPosition, RoomData roomData, int rotation, Vector2Int doorPosition)
    {
        // РЎРЅР°С‡Р°Р»Р° РіРѕРІРѕСЂРёРј RoomBuilder РёСЃРєР»СЋС‡РёС‚СЊ РїРѕР·РёС†РёСЋ РґРІРµСЂРё
        RoomBuilder.Instance.SetDoorExclusion(doorPosition);

        // РЎС‚СЂРѕРёРј РєРѕРјРЅР°С‚Сѓ РєР°Рє РѕР±С‹С‡РЅРѕ
        BuildRoom(gridPosition, roomData, rotation);

        // РћС‡РёС‰Р°РµРј РёСЃРєР»СЋС‡РµРЅРёРµ
        RoomBuilder.Instance.ClearDoorExclusion();
    }

    /// <summary>
    /// РЎРѕР·РґР°С‚СЊ РґРІРµСЂСЊ РІ СѓРєР°Р·Р°РЅРЅРѕР№ РїРѕР·РёС†РёРё
    /// </summary>
    void CreateDoorAtPosition(Vector2Int position)
    {
        // Р—Р°РіСЂСѓР¶Р°РµРј РїСЂРµС„Р°Р± РґРІРµСЂРё
        GameObject doorPrefab = Resources.Load<GameObject>("Prefabs/SM_Door");
        if (doorPrefab == null)
        {
            FileLogger.Log("ERROR: SM_Door prefab not found in Resources/Prefabs/!");
            return;
        }

        // Р’С‹С‡РёСЃР»СЏРµРј РїРѕР·РёС†РёСЋ Рё РїРѕРІРѕСЂРѕС‚ РґРІРµСЂРё
        Vector3 worldPos = gridManager.GridToWorld(position);
        float doorRotation = GetWallRotationAtPosition(position);
        Quaternion rotation = Quaternion.Euler(0, doorRotation, 0);

        // РЎРѕР·РґР°РµРј РґРІРµСЂСЊ
        GameObject door = Instantiate(doorPrefab, worldPos, rotation);
        door.name = $"Door_{position.x}_{position.y}";

        FileLogger.Log($"DEBUG: Door created: {door.name} at {door.transform.position} with rotation {doorRotation}В°");
        FileLogger.Log($"SUCCESS: Created door at {position}");
    }

    /// <summary>
    /// Р’РµСЂРЅСѓС‚СЊСЃСЏ Рє СЂР°Р·РјРµС‰РµРЅРёСЋ РєРѕРјРЅР°С‚С‹
    /// </summary>
    void ReturnToRoomPlacement()
    {
        // РћС‡РёС‰Р°РµРј РїСЂРёР·СЂР°Рє РґРІРµСЂРё
        if (doorPreviewObject != null)
            DestroyImmediate(doorPreviewObject);

        // Р’РѕР·РІСЂР°С‰Р°РµРјСЃСЏ Рє С„Р°Р·Рµ СЂР°Р·РјРµС‰РµРЅРёСЏ РєРѕРјРЅР°С‚С‹
        currentPhase = BuildingPhase.PlacingRoom;

        FileLogger.Log("Returned to Phase 1: Placing room");
    }

    /// <summary>
    /// РџРѕРІРµСЂРЅСѓС‚СЊ РєРѕРјРЅР°С‚Сѓ РЅР° Р·Р°РґР°РЅРЅС‹Р№ СѓРіРѕР»
    /// </summary>
    void RotateRoom(int degrees)
    {
        roomRotation = (roomRotation + degrees) % 360;
        if (roomRotation < 0) roomRotation += 360;

        // РћР±РЅРѕРІР»СЏРµРј РїСЂРµРґРїСЂРѕСЃРјРѕС‚СЂ РІ Р·Р°РІРёСЃРёРјРѕСЃС‚Рё РѕС‚ С„Р°Р·С‹
        if (buildingMode)
        {
            if (currentPhase == BuildingPhase.PlacingRoom && previewObject != null)
            {
                // Р’ С„Р°Р·Рµ СЂР°Р·РјРµС‰РµРЅРёСЏ РєРѕРјРЅР°С‚С‹ - РїРµСЂРµСЃРѕР·РґР°РµРј РїСЂРёР·СЂР°Рє РєРѕРјРЅР°С‚С‹
                CreatePreviewObject();
            }
            else if (currentPhase == BuildingPhase.PlacingDoor)
            {
                // Р’ С„Р°Р·Рµ СЂР°Р·РјРµС‰РµРЅРёСЏ РґРІРµСЂРё - РїРѕР»РЅРѕСЃС‚СЊСЋ РїРµСЂРµСЃРѕР·РґР°РµРј РїСЂРёР·СЂР°Рє РєРѕРјРЅР°С‚С‹
                RoomData roomData = availableRooms[selectedRoomIndex];
                pendingRoomSize = GetRotatedRoomSize(roomData.size, roomRotation);
                pendingRoomRotation = roomRotation;

                // РџРµСЂРµСЃРѕР·РґР°РµРј РїСЂРёР·СЂР°Рє РєРѕРјРЅР°С‚С‹ СЃ РїСЂР°РІРёР»СЊРЅС‹РјРё РїРѕРІРѕСЂРѕС‚Р°РјРё СЃС‚РµРЅ
                if (previewObject != null)
                    DestroyImmediate(previewObject);

                previewObject = CreateGhostRoom(pendingRoomPosition, pendingRoomSize, roomData.roomName + "_Preview", roomRotation);

                // РџРµСЂРµСЃС‡РёС‚С‹РІР°РµРј РїРѕР·РёС†РёРё РїСЂСЏРјС‹С… СЃС‚РµРЅ
                FindStraightWallPositions();

                // РћР±РЅРѕРІР»СЏРµРј РїСЂРёР·СЂР°Рє РґРІРµСЂРё
                if (doorPreviewObject != null)
                {
                    UpdateDoorPreviewPosition();
                }
            }
        }

        FileLogger.Log($"Room rotated to {roomRotation} degrees");
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ СЂР°Р·РјРµСЂ РєРѕРјРЅР°С‚С‹ СЃ СѓС‡РµС‚РѕРј РїРѕРІРѕСЂРѕС‚Р°
    /// </summary>
    Vector2Int GetRotatedRoomSize(Vector2Int originalSize, int rotation)
    {
        if (rotation == 90 || rotation == 270)
        {
            // РџСЂРё РїРѕРІРѕСЂРѕС‚Рµ РЅР° 90 РёР»Рё 270 РіСЂР°РґСѓСЃРѕРІ РјРµРЅСЏРµРј РјРµСЃС‚Р°РјРё X Рё Y
            return new Vector2Int(originalSize.y, originalSize.x);
        }
        return originalSize;
    }

    /// <summary>
    /// РћС‚РјРµРЅРёС‚СЊ РІС‹Р±РѕСЂ РєРѕРјРЅР°С‚С‹
    /// </summary>
    public void ClearRoomSelection()
    {
        selectedRoomIndex = -1;
        roomRotation = 0; // РЎР±СЂР°СЃС‹РІР°РµРј РїРѕРІРѕСЂРѕС‚ РїСЂРё СЃРјРµРЅРµ РІС‹Р±РѕСЂР°

        // РЈР±РёСЂР°РµРј РїСЂРµРґРїСЂРѕСЃРјРѕС‚СЂ
        if (previewObject != null)
        {
            DestroyImmediate(previewObject);
            previewObject = null;
        }

        ClearPreviewCells();

        FileLogger.Log("Room selection cleared");
    }

    /// <summary>
    /// РЈСЃС‚Р°РЅРѕРІРёС‚СЊ РІС‹Р±СЂР°РЅРЅС‹Р№ С‚РёРї РєРѕРјРЅР°С‚С‹
    /// </summary>
    public void SetSelectedRoomType(int index)
    {
        if (index >= 0 && index < availableRooms.Count)
        {
            selectedRoomIndex = index;
            roomRotation = 0; // РЎР±СЂР°СЃС‹РІР°РµРј РїРѕРІРѕСЂРѕС‚ РїСЂРё СЃРјРµРЅРµ С‚РёРїР° РєРѕРјРЅР°С‚С‹
            FileLogger.Log($"[SetSelectedRoomType] Changed to room {index} ({availableRooms[index].roomName}), buildingMode: {buildingMode}");
            if (buildingMode)
            {
                CreatePreviewObject();
            }
        }
    }

    /// <summary>
    /// РџРµСЂРµРєР»СЋС‡РёС‚СЊСЃСЏ Рє СЃР»РµРґСѓСЋС‰РµРјСѓ С‚РёРїСѓ РєРѕРјРЅР°С‚С‹
    /// </summary>
    public void CycleRoomType()
    {
        selectedRoomIndex = (selectedRoomIndex + 1) % availableRooms.Count;
        roomRotation = 0; // РЎР±СЂР°СЃС‹РІР°РµРј РїРѕРІРѕСЂРѕС‚ РїСЂРё СЃРјРµРЅРµ С‚РёРїР° РєРѕРјРЅР°С‚С‹
        if (buildingMode)
        {
            CreatePreviewObject();
        }
    }

    /// <summary>
    /// РџРѕРїС‹С‚РєР° РїРѕСЃС‚СЂРѕРёС‚СЊ РєРѕРјРЅР°С‚Сѓ
    /// </summary>
    void TryBuildRoom()
    {
        if (previewObject == null || playerCamera == null)
            return;

        // РСЃРїРѕР»СЊР·СѓРµРј С‚Сѓ Р¶Рµ Р»РѕРіРёРєСѓ РѕРїСЂРµРґРµР»РµРЅРёСЏ РїРѕР·РёС†РёРё, С‡С‚Рѕ Рё РІ UpdatePreview
        Vector3 mousePosition = Input.mousePosition;
        Ray ray = playerCamera.ScreenPointToRay(mousePosition);

        Vector3 worldPos;
        Vector2Int gridPos;

        // РџРѕРїСЂРѕР±СѓРµРј raycast РЅР° Р·РµРјР»СЋ/РѕР±СЉРµРєС‚С‹
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            worldPos = hit.point;
            gridPos = gridManager.WorldToGrid(worldPos);
        }
        else
        {
            // Р•СЃР»Рё raycast РЅРµ РїРѕРїР°Р», РёСЃРїРѕР»СЊР·СѓРµРј РїР»РѕСЃРєРѕСЃС‚СЊ Y=0
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float distance))
            {
                worldPos = ray.GetPoint(distance);
                gridPos = gridManager.WorldToGrid(worldPos);
            }
            else
            {
                return; // РќРµ РјРѕР¶РµРј РѕРїСЂРµРґРµР»РёС‚СЊ РїРѕР·РёС†РёСЋ
            }
        }

        RoomData currentRoom = availableRooms[selectedRoomIndex];

        Vector3 snapPosition = gridManager.GridToWorld(gridPos);

        if (CanPlaceRoom(gridPos, currentRoom, roomRotation))
        {
            BuildRoom(gridPos, currentRoom, roomRotation);
        }
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРєР° РІРѕР·РјРѕР¶РЅРѕСЃС‚Рё СЂР°Р·РјРµС‰РµРЅРёСЏ РєРѕРјРЅР°С‚С‹
    /// </summary>
    bool CanPlaceRoom(Vector2Int gridPosition, RoomData roomData, int rotation = 0)
    {
        Vector2Int rotatedSize = GetRotatedRoomSize(roomData.size, rotation);
        return gridManager.CanPlacePerimeterAt(gridPosition, rotatedSize.x, rotatedSize.y);
    }

    /// <summary>
    /// РџРѕСЃС‚СЂРѕРёС‚СЊ РєРѕРјРЅР°С‚Сѓ (РёСЃРїРѕР»СЊР·СѓСЏ РЅРѕРІСѓСЋ СЃРёСЃС‚РµРјСѓ BuildCustomRoom)
    /// </summary>
    void BuildRoom(Vector2Int gridPosition, RoomData roomData, int rotation = 0)
    {
        FileLogger.Log($"DEBUG: BuildRoom called for {roomData.roomName} at position {gridPosition}, rotation {rotation}");

        GameObject room;
        Vector2Int rotatedSize = GetRotatedRoomSize(roomData.size, rotation);

        FileLogger.Log($"DEBUG: Rotated size: {rotatedSize}");

        // РСЃРїРѕР»СЊР·СѓРµРј РЅРѕРІСѓСЋ СЃРёСЃС‚РµРјСѓ BuildCustomRoom РґР»СЏ СЃРѕР·РґР°РЅРёСЏ РєРѕРјРЅР°С‚
        if (roomData.prefab == null)
        {
            FileLogger.Log($"DEBUG: Building custom room for {roomData.roomName} using BuildCustomRoom");

            // Р“РµРЅРµСЂРёСЂСѓРµРј СЃРїРёСЃРєРё РїРѕР·РёС†РёР№ СЃС‚РµРЅ Рё РїРѕР»Р° РґР»СЏ РїСЂСЏРјРѕСѓРіРѕР»СЊРЅРѕР№ РєРѕРјРЅР°С‚С‹
            List<Vector2Int> wallPositions = new List<Vector2Int>();
            List<Vector2Int> floorPositions = new List<Vector2Int>();

            for (int x = 0; x < rotatedSize.x; x++)
            {
                for (int y = 0; y < rotatedSize.y; y++)
                {
                    Vector2Int cellPos = new Vector2Int(gridPosition.x + x, gridPosition.y + y);

                    // РџСЂРѕРІРµСЂСЏРµРј, СЏРІР»СЏРµС‚СЃСЏ Р»Рё РєР»РµС‚РєР° С‡Р°СЃС‚СЊСЋ РїРµСЂРёРјРµС‚СЂР° (СЃС‚РµРЅР°) РёР»Рё РІРЅСѓС‚СЂРµРЅРЅРµР№ С‡Р°СЃС‚СЊСЋ (РїРѕР»)
                    bool isPerimeter = (x == 0 || x == rotatedSize.x - 1 || y == 0 || y == rotatedSize.y - 1);

                    if (isPerimeter)
                    {
                        wallPositions.Add(cellPos);
                    }
                    else
                    {
                        floorPositions.Add(cellPos);
                    }
                }
            }

            // Р’С‹Р·С‹РІР°РµРј BuildCustomRoom СЃ СЃРіРµРЅРµСЂРёСЂРѕРІР°РЅРЅС‹РјРё РїРѕР·РёС†РёСЏРјРё
            room = RoomBuilder.Instance.BuildCustomRoom(gridPosition, rotatedSize, roomData.roomName, wallPositions, floorPositions);
            room.name = $"{roomData.roomName}_{builtRooms.Count + 1}";

            FileLogger.Log($"DEBUG: BuildCustomRoom completed, room created: {room.name}");

            // Р”РѕР±Р°РІР»СЏРµРј RoomInfo РєРѕРјРїРѕРЅРµРЅС‚ РґР»СЏ С…СЂР°РЅРµРЅРёСЏ РёРЅС„РѕСЂРјР°С†РёРё Рѕ РєРѕРјРЅР°С‚Рµ
            RoomInfo roomInfo = room.GetComponent<RoomInfo>();
            if (roomInfo != null)
            {
                roomInfo.gridPosition = gridPosition;
                roomInfo.roomSize = roomData.size;
                roomInfo.roomRotation = rotation;
                roomInfo.roomName = roomData.roomName;
                roomInfo.roomType = roomData.roomType;
            }
        }
        else
        {
            // РЎС‚Р°СЂС‹Р№ СЃРїРѕСЃРѕР± СЃ РїСЂРµС„Р°Р±Р°РјРё (РґР»СЏ СЃРѕРІРјРµСЃС‚РёРјРѕСЃС‚Рё)
            Vector3 centerOffset = new Vector3(
                (rotatedSize.x - 1) * 0.5f * gridManager.cellSize,
                0,
                (rotatedSize.y - 1) * 0.5f * gridManager.cellSize
            );
            Vector3 roomPosition = gridManager.GridToWorld(gridPosition) + centerOffset;

            room = Instantiate(roomData.prefab, roomPosition, Quaternion.Euler(0, rotation, 0));
            room.SetActive(true);
            room.name = $"{roomData.roomName}_{builtRooms.Count + 1}";
        }

        // Р—Р°РЅРёРјР°РµРј С‚РѕР»СЊРєРѕ РїРµСЂРёРјРµС‚СЂ РєРѕРјРЅР°С‚С‹ (СЃС‚РµРЅС‹), РІРЅСѓС‚СЂРµРЅРЅРёРµ РєР»РµС‚РєРё РѕСЃС‚Р°СЋС‚СЃСЏ СЃРІРѕР±РѕРґРЅС‹РјРё
        gridManager.OccupyCellPerimeter(gridPosition, rotatedSize.x, rotatedSize.y, room, roomData.roomType);

        // Р РµРіРёСЃС‚СЂРёСЂСѓРµРј СЃС‚РµРЅС‹ РєР°Рє РЅРµРїСЂРѕС…РѕРґРёРјС‹Рµ
        RegisterRoomWalls(gridPosition, rotatedSize);

        // Р”РѕР±Р°РІР»СЏРµРј РІ СЃРїРёСЃРѕРє РїРѕСЃС‚СЂРѕРµРЅРЅС‹С… РєРѕРјРЅР°С‚
        builtRooms.Add(room);

        OnRoomBuilt?.Invoke(room);
    }

    /// <summary>
    /// Р—Р°СЂРµРіРёСЃС‚СЂРёСЂРѕРІР°С‚СЊ СЃС‚РµРЅС‹ РєРѕРјРЅР°С‚С‹ РєР°Рє РїСЂРµРїСЏС‚СЃС‚РІРёСЏ РІ GridManager
    /// </summary>
    void RegisterRoomWalls(Vector2Int gridPosition, Vector2Int roomSize)
    {
        List<GameObject> walls = RoomBuilder.Instance.GetActiveWalls();

        foreach (GameObject wall in walls)
        {
            WallComponent wallComp = wall.GetComponent<WallComponent>();
            if (wallComp != null)
            {
                Vector2Int wallGridPos = wallComp.wallData.position;

                // РџСЂРѕРІРµСЂСЏРµРј, РѕС‚РЅРѕСЃРёС‚СЃСЏ Р»Рё СЃС‚РµРЅР° Рє РЅР°С€РµР№ РєРѕРјРЅР°С‚Рµ
                if (IsWallPartOfRoom(wallGridPos, gridPosition, roomSize))
                {
                    // Р—Р°РЅРёРјР°РµРј РєР»РµС‚РєСѓ СЃС‚РµРЅС‹ РІ GridManager
                    gridManager.OccupyCell(wallGridPos, wall, "Wall");
                }
            }
        }
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ, РѕС‚РЅРѕСЃРёС‚СЃСЏ Р»Рё СЃС‚РµРЅР° Рє РґР°РЅРЅРѕР№ РєРѕРјРЅР°С‚Рµ
    /// </summary>
    bool IsWallPartOfRoom(Vector2Int wallPos, Vector2Int roomPos, Vector2Int roomSize)
    {
        // РџСЂРѕРІРµСЂСЏРµРј, РЅР°С…РѕРґРёС‚СЃСЏ Р»Рё СЃС‚РµРЅР° РІ РїСЂРµРґРµР»Р°С… РєРѕРјРЅР°С‚С‹
        bool inBoundsX = (wallPos.x >= roomPos.x && wallPos.x < roomPos.x + roomSize.x);
        bool inBoundsY = (wallPos.y >= roomPos.y && wallPos.y < roomPos.y + roomSize.y);

        if (!inBoundsX || !inBoundsY)
            return false;

        // РџСЂРѕРІРµСЂСЏРµРј, СЏРІР»СЏРµС‚СЃСЏ Р»Рё РїРѕР·РёС†РёСЏ С‡Р°СЃС‚СЊСЋ РїРµСЂРёРјРµС‚СЂР°
        int relX = wallPos.x - roomPos.x;
        int relY = wallPos.y - roomPos.y;

        bool isPerimeter = (relX == 0 || relX == roomSize.x - 1 || relY == 0 || relY == roomSize.y - 1);

        return isPerimeter;
    }

    /// <summary>
    /// РћСЃРІРѕР±РѕРґРёС‚СЊ РєР»РµС‚РєРё СЃС‚РµРЅ РєРѕРјРЅР°С‚С‹ РІ GridManager
    /// </summary>
    void FreeRoomWallCells(Vector2Int gridPosition, Vector2Int roomSize)
    {
        // РћСЃРІРѕР±РѕР¶РґР°РµРј РєР»РµС‚РєРё РїРµСЂРёРјРµС‚СЂР° РєРѕРјРЅР°С‚С‹ (СЃС‚РµРЅС‹)
        for (int x = 0; x < roomSize.x; x++)
        {
            for (int y = 0; y < roomSize.y; y++)
            {
                Vector2Int cellPos = new Vector2Int(gridPosition.x + x, gridPosition.y + y);

                // РџСЂРѕРІРµСЂСЏРµРј, СЏРІР»СЏРµС‚СЃСЏ Р»Рё РєР»РµС‚РєР° С‡Р°СЃС‚СЊСЋ РїРµСЂРёРјРµС‚СЂР°
                bool isPerimeter = (x == 0 || x == roomSize.x - 1 || y == 0 || y == roomSize.y - 1);

                if (isPerimeter)
                {
                    gridManager.FreeCell(cellPos);
                }
            }
        }
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ С‚РµРєСѓС‰РёР№ С‚РёРї РєРѕРјРЅР°С‚С‹
    /// </summary>
    public RoomData GetCurrentRoomType()
    {
        if (availableRooms.Count > 0 && selectedRoomIndex < availableRooms.Count)
            return availableRooms[selectedRoomIndex];
        return null;
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ, Р°РєС‚РёРІРµРЅ Р»Рё СЂРµР¶РёРј СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
    /// </summary>
    public bool IsBuildingModeActive()
    {
        return buildingMode;
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ, Р±С‹Р» Р»Рё СЂРѕР»РёРє РјС‹С€Рё РёСЃРїРѕР»СЊР·РѕРІР°РЅ РґР»СЏ РїРѕРІРѕСЂРѕС‚Р° РІ СЌС‚РѕРј РєР°РґСЂРµ
    /// </summary>
    public bool IsScrollWheelUsedThisFrame()
    {
        return scrollWheelUsedThisFrame;
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ СЃРїРёСЃРѕРє РїРѕСЃС‚СЂРѕРµРЅРЅС‹С… РєРѕРјРЅР°С‚
    /// </summary>
    public List<GameObject> GetBuiltRooms()
    {
        return new List<GameObject>(builtRooms);
    }

    /// <summary>
    /// Р’РєР»СЋС‡РёС‚СЊ/РІС‹РєР»СЋС‡РёС‚СЊ СЂРµР¶РёРј СѓРґР°Р»РµРЅРёСЏ
    /// </summary>
    public void ToggleDeletionMode()
    {
        deletionMode = !deletionMode;
        FileLogger.Log($"Deletion mode toggled: {deletionMode}");

        if (deletionMode)
        {
            // Р’С‹РєР»СЋС‡Р°РµРј СЂРµР¶РёРј СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР° РµСЃР»Рё РѕРЅ Р±С‹Р» Р°РєС‚РёРІРµРЅ
            if (buildingMode)
            {
                buildingMode = false;
                StopBuildingMode();
                OnBuildingModeChanged?.Invoke();
            }
        }
        else
        {
            // РџСЂРё РІС‹С…РѕРґРµ РёР· СЂРµР¶РёРјР° СЂР°Р·СЂСѓС€РµРЅРёСЏ СѓР±РёСЂР°РµРј РїРѕРґСЃРІРµС‚РєСѓ
            if (highlightedRoom != null)
            {
                RemoveRoomHighlight(highlightedRoom);
                highlightedRoom = null;
            }
        }

        OnDeletionModeChanged?.Invoke();
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ, Р°РєС‚РёРІРµРЅ Р»Рё СЂРµР¶РёРј СѓРґР°Р»РµРЅРёСЏ
    /// </summary>
    public bool IsDeletionModeActive()
    {
        return deletionMode;
    }

    /// <summary>
    /// РћР±РЅРѕРІР»РµРЅРёРµ РїРѕРґСЃРІРµС‚РєРё РєРѕРјРЅР°С‚ РІ СЂРµР¶РёРјРµ СЂР°Р·СЂСѓС€РµРЅРёСЏ
    /// </summary>
    void UpdateDeletionHighlight()
    {
        Vector3 mousePosition = Input.mousePosition;
        Ray ray = playerCamera.ScreenPointToRay(mousePosition);

        GameObject newHighlightedRoom = null;

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            // РџСЂРѕРІРµСЂСЏРµРј, СЏРІР»СЏРµС‚СЃСЏ Р»Рё РѕР±СЉРµРєС‚ РєРѕРјРЅР°С‚РѕР№
            GameObject hitObject = hit.collider.gameObject;
            FileLogger.Log($"Raycast hit object: {hitObject.name}");

            RoomInfo roomInfo = hitObject.GetComponentInParent<RoomInfo>();

            if (roomInfo != null)
            {
                newHighlightedRoom = roomInfo.gameObject;
                FileLogger.Log($"Found room for highlighting: {newHighlightedRoom.name}");
            }
            else
            {
                FileLogger.Log($"No RoomInfo found on {hitObject.name} or its parents");
            }
        }

        // РЈР±РёСЂР°РµРј СЃС‚Р°СЂСѓСЋ РїРѕРґСЃРІРµС‚РєСѓ
        if (highlightedRoom != null && highlightedRoom != newHighlightedRoom)
        {
            RemoveRoomHighlight(highlightedRoom);
        }

        // Р”РѕР±Р°РІР»СЏРµРј РЅРѕРІСѓСЋ РїРѕРґСЃРІРµС‚РєСѓ
        if (newHighlightedRoom != null && newHighlightedRoom != highlightedRoom)
        {
            AddRoomHighlight(newHighlightedRoom);
        }

        highlightedRoom = newHighlightedRoom;
    }

    /// <summary>
    /// Р”РѕР±Р°РІРёС‚СЊ РєСЂР°СЃРЅСѓСЋ РїРѕРґСЃРІРµС‚РєСѓ РєРѕРјРЅР°С‚Рµ
    /// </summary>
    void AddRoomHighlight(GameObject room)
    {
        Renderer[] renderers = room.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                // РЎРѕС…СЂР°РЅСЏРµРј РѕСЂРёРіРёРЅР°Р»СЊРЅС‹Р№ РјР°С‚РµСЂРёР°Р» РµСЃР»Рё РµС‰Рµ РЅРµ СЃРѕС…СЂР°РЅРµРЅ
                if (originalMaterial == null)
                {
                    originalMaterial = renderer.material;
                }

                // РЎРѕР·РґР°РµРј РєСЂР°СЃРЅС‹Р№ РјР°С‚РµСЂРёР°Р» РґР»СЏ РїРѕРґСЃРІРµС‚РєРё
                Material highlightMaterial = new Material(renderer.material);
                highlightMaterial.color = Color.red;
                renderer.material = highlightMaterial;
            }
        }
    }

    /// <summary>
    /// РЈР±СЂР°С‚СЊ РїРѕРґСЃРІРµС‚РєСѓ СЃ РєРѕРјРЅР°С‚С‹
    /// </summary>
    void RemoveRoomHighlight(GameObject room)
    {
        if (room == null || originalMaterial == null) return;

        Renderer[] renderers = room.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.material = originalMaterial;
            }
        }
    }

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚РєР° РІРІРѕРґР° РІ СЂРµР¶РёРјРµ СѓРґР°Р»РµРЅРёСЏ
    /// </summary>
    void HandleDeletionInput()
    {
        // РџСЂРѕРІРµСЂСЏРµРј, РЅРµ РЅР°С…РѕРґРёС‚СЃСЏ Р»Рё РјС‹С€СЊ РЅР°Рґ UI СЌР»РµРјРµРЅС‚РѕРј
        bool isPointerOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

        // Р›РљРњ - СѓРґР°Р»РёС‚СЊ РєРѕРјРЅР°С‚Сѓ (С‚РѕР»СЊРєРѕ РµСЃР»Рё РјС‹С€СЊ РќР• РЅР°Рґ UI)
        if (Input.GetMouseButtonDown(0) && !isPointerOverUI)
        {
            TryDeleteRoom();
        }

        // РџРљРњ - РІС‹Р№С‚Рё РёР· СЂРµР¶РёРјР° СѓРґР°Р»РµРЅРёСЏ (С‚РѕР»СЊРєРѕ РµСЃР»Рё РјС‹С€СЊ РќР• РЅР°Рґ UI)
        if (Input.GetMouseButtonDown(1) && !isPointerOverUI)
        {
            ToggleDeletionMode();
        }
    }

    /// <summary>
    /// РџРѕРїС‹С‚РєР° СѓРґР°Р»РёС‚СЊ РєРѕРјРЅР°С‚Сѓ
    /// </summary>
    void TryDeleteRoom()
    {
        Vector3 mousePosition = Input.mousePosition;
        Ray ray = playerCamera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            // РџСЂРѕРІРµСЂСЏРµРј, СЏРІР»СЏРµС‚СЃСЏ Р»Рё РѕР±СЉРµРєС‚ РєРѕРјРЅР°С‚РѕР№
            GameObject hitObject = hit.collider.gameObject;

            // РџСЂРѕРІРµСЂСЏРµРј СЂРѕРґРёС‚РµР»СЊСЃРєРёРµ РѕР±СЉРµРєС‚С‹ РЅР° РЅР°Р»РёС‡РёРµ RoomInfo
            RoomInfo roomInfo = hitObject.GetComponentInParent<RoomInfo>();
            if (roomInfo != null)
            {
                DeleteRoom(roomInfo.gameObject);
            }
        }
    }

    /// <summary>
    /// РЈРґР°Р»РёС‚СЊ РєРѕРјРЅР°С‚Сѓ РїРѕ РїРѕР·РёС†РёРё РЅР° СЃРµС‚РєРµ
    /// </summary>
    public bool DeleteRoom(Vector2Int position)
    {
        // РС‰РµРј РєРѕРјРЅР°С‚Сѓ РІ СѓРєР°Р·Р°РЅРЅРѕР№ РїРѕР·РёС†РёРё
        foreach (GameObject room in builtRooms)
        {
            RoomInfo roomInfo = room.GetComponent<RoomInfo>();
            if (roomInfo != null && roomInfo.gridPosition == position)
            {
                DeleteRoom(room);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// РЈРґР°Р»РёС‚СЊ РєРѕРјРЅР°С‚Сѓ
    /// </summary>
    public void DeleteRoom(GameObject room)
    {
        RoomInfo roomInfo = room.GetComponent<RoomInfo>();
        if (roomInfo == null)
        {
            FileLogger.Log("ERROR: DeleteRoom called on object without RoomInfo component");
            return;
        }

        FileLogger.Log($"DEBUG: Starting DeleteRoom for {roomInfo.roomName} at position {roomInfo.gridPosition}, size {roomInfo.roomSize}, rotation {roomInfo.roomRotation}");

        // РџРѕР»СѓС‡Р°РµРј СЂР°Р·РјРµСЂ СЃ СѓС‡РµС‚РѕРј РїРѕРІРѕСЂРѕС‚Р° РєРѕРјРЅР°С‚С‹
        Vector2Int rotatedSize = GetRotatedRoomSize(roomInfo.roomSize, roomInfo.roomRotation);
        FileLogger.Log($"DEBUG: Rotated size calculated as {rotatedSize}");

        // РћСЃРІРѕР±РѕР¶РґР°РµРј РєР»РµС‚РєРё СЃС‚РµРЅ РІ GridManager
        FileLogger.Log("DEBUG: Freeing room wall cells");
        FreeRoomWallCells(roomInfo.gridPosition, rotatedSize);

        // РћСЃРІРѕР±РѕР¶РґР°РµРј С‚РѕР»СЊРєРѕ РїРµСЂРёРјРµС‚СЂ РєРѕРјРЅР°С‚С‹ (СЃС‚РµРЅС‹) РІ GridManager
        FileLogger.Log("DEBUG: Freeing cell perimeter in GridManager");
        gridManager.FreeCellPerimeter(roomInfo.gridPosition, rotatedSize.x, rotatedSize.y);

        // РЈРґР°Р»СЏРµРј СЃС‚РµРЅС‹ С‡РµСЂРµР· RoomBuilder
        FileLogger.Log("DEBUG: Calling RoomBuilder.RemoveRoom");
        RoomBuilder.Instance.RemoveRoom(roomInfo.gridPosition, roomInfo.roomSize, roomInfo.roomRotation);

        // РЈРґР°Р»СЏРµРј РёР· СЃРїРёСЃРєР° РїРѕСЃС‚СЂРѕРµРЅРЅС‹С… РєРѕРјРЅР°С‚
        if (builtRooms.Contains(room))
        {
            FileLogger.Log("DEBUG: Removing room from builtRooms list");
            builtRooms.Remove(room);
        }
        else
        {
            FileLogger.Log("WARNING: Room not found in builtRooms list");
        }

        // РЈРІРµРґРѕРјР»СЏРµРј Рѕ СѓРґР°Р»РµРЅРёРё
        FileLogger.Log("DEBUG: Invoking OnRoomDeleted event");
        OnRoomDeleted?.Invoke(room);

        // РЈРЅРёС‡С‚РѕР¶Р°РµРј РѕР±СЉРµРєС‚
        FileLogger.Log("DEBUG: Destroying room GameObject");
        DestroyImmediate(room);

        FileLogger.Log("DEBUG: DeleteRoom completed");
    }

    /// <summary>
    /// РћР±РЅРѕРІРёС‚СЊ РёРЅРґРёРєР°С‚РѕСЂС‹ РєР»РµС‚РѕРє РґР»СЏ РїСЂРµРґРІР°СЂРёС‚РµР»СЊРЅРѕРіРѕ РїСЂРѕСЃРјРѕС‚СЂР°
    /// </summary>
    void UpdatePreviewCells(Vector2Int gridPos, RoomData roomData)
    {
        // РћС‡РёС‰Р°РµРј СЃС‚Р°СЂС‹Рµ РёРЅРґРёРєР°С‚РѕСЂС‹
        ClearPreviewCells();

        // РЎРѕР·РґР°РµРј РЅРѕРІС‹Рµ РёРЅРґРёРєР°С‚РѕСЂС‹ РґР»СЏ РєР°Р¶РґРѕР№ РєР»РµС‚РєРё РєРѕРјРЅР°С‚С‹
        for (int x = 0; x < roomData.size.x; x++)
        {
            for (int y = 0; y < roomData.size.y; y++)
            {
                Vector2Int cellPos = new Vector2Int(gridPos.x + x, gridPos.y + y);

                // РџСЂРѕРІРµСЂСЏРµРј, СЃРІРѕР±РѕРґРЅР° Р»Рё РєР»РµС‚РєР°
                bool isValidPosition = gridManager.IsValidGridPosition(cellPos);
                bool isCellFree = isValidPosition && gridManager.IsCellFree(cellPos);

                // Р”РѕРїРѕР»РЅРёС‚РµР»СЊРЅР°СЏ РїСЂРѕРІРµСЂРєР° - РµСЃР»Рё РїРѕР·РёС†РёСЏ Р·Р° РїСЂРµРґРµР»Р°РјРё СЃРµС‚РєРё, СЃС‡РёС‚Р°РµРј РЅРµРґРѕСЃС‚СѓРїРЅРѕР№
                if (!isValidPosition)
                {
                    isCellFree = false;
                }


                // РЎРѕР·РґР°РµРј РІРёР·СѓР°Р»СЊРЅС‹Р№ РёРЅРґРёРєР°С‚РѕСЂ
                GameObject cellIndicator = CreateCellIndicator(cellPos, isCellFree);
                previewCells.Add(cellIndicator);
            }
        }
    }

    /// <summary>
    /// РЎРѕР·РґР°С‚СЊ РёРЅРґРёРєР°С‚РѕСЂ РґР»СЏ РѕРґРЅРѕР№ РєР»РµС‚РєРё
    /// </summary>
    GameObject CreateCellIndicator(Vector2Int gridPos, bool isAvailable)
    {
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
        indicator.name = $"CellIndicator_{gridPos.x}_{gridPos.y}";

        // РџРѕР·РёС†РёРѕРЅРёСЂСѓРµРј РёРЅРґРёРєР°С‚РѕСЂ
        Vector3 worldPos = gridManager.GridToWorld(gridPos);
        worldPos.y = 0.5f; // Р•С‰Рµ РІС‹С€Рµ РґР»СЏ Р»СѓС‡С€РµР№ РІРёРґРёРјРѕСЃС‚Рё
        indicator.transform.position = worldPos;

        // Р Р°Р·РјРµСЂ РёРЅРґРёРєР°С‚РѕСЂР° - РїРѕРєСЂС‹РІР°РµС‚ Р±РѕР»СЊС€СѓСЋ С‡Р°СЃС‚СЊ РєР»РµС‚РєРё
        float indicatorSize = gridManager.cellSize * 0.9f;
        indicator.transform.localScale = new Vector3(indicatorSize, 0.5f, indicatorSize);

        // РЈР±РёСЂР°РµРј РєРѕР»Р»Р°Р№РґРµСЂ
        Collider collider = indicator.GetComponent<Collider>();
        if (collider != null)
            DestroyImmediate(collider);

        // РќР°СЃС‚СЂР°РёРІР°РµРј РјР°С‚РµСЂРёР°Р»
        Renderer renderer = indicator.GetComponent<Renderer>();

        // РџРѕРїСЂРѕР±СѓРµРј Legacy С€РµР№РґРµСЂС‹ РєРѕС‚РѕСЂС‹Рµ С‚РѕС‡РЅРѕ РµСЃС‚СЊ РІ Unity
        Material mat = new Material(Shader.Find("Legacy Shaders/Transparent/Diffuse"));
        if (mat.shader == null)
        {
            mat = new Material(Shader.Find("Standard"));
            mat.SetFloat("_Mode", 2); // Fade mode
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }

        if (isAvailable)
        {
            // РћС‡РµРЅСЊ СЏСЂРєРёР№ Р·РµР»РµРЅС‹Р№ РґР»СЏ РґРѕСЃС‚СѓРїРЅС‹С… РєР»РµС‚РѕРє
            mat.color = new Color(0f, 1f, 0f, 1f);
        }
        else
        {
            // РћС‡РµРЅСЊ СЏСЂРєРёР№ РєСЂР°СЃРЅС‹Р№ РґР»СЏ РЅРµРґРѕСЃС‚СѓРїРЅС‹С… РєР»РµС‚РѕРє
            mat.color = new Color(1f, 0f, 0f, 1f);
        }

        renderer.material = mat;

        return indicator;
    }

    /// <summary>
    /// РћС‡РёСЃС‚РёС‚СЊ РІСЃРµ РёРЅРґРёРєР°С‚РѕСЂС‹ РєР»РµС‚РѕРє
    /// </summary>
    void ClearPreviewCells()
    {
        foreach (GameObject cell in previewCells)
        {
            if (cell != null)
                DestroyImmediate(cell);
        }
        previewCells.Clear();
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ СЃРїРёСЃРѕРє РґРѕСЃС‚СѓРїРЅС‹С… РєРѕРјРЅР°С‚ РґР»СЏ UI
    /// </summary>
    public List<RoomData> GetAvailableRooms()
    {
        return availableRooms;
    }

    private Vector2Int GetGridPositionFromMouse()
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
            if (ray.direction.y != 0)
            {
                float t = -ray.origin.y / ray.direction.y;
                Vector3 worldPos = ray.origin + ray.direction * t;
                return gridManager.WorldToGrid(worldPos);
            }
        }

        return Vector2Int.zero;
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ С‚РµРєСѓС‰РёР№ РІС‹Р±СЂР°РЅРЅС‹Р№ РёРЅРґРµРєСЃ РєРѕРјРЅР°С‚С‹
    /// </summary>
    public int GetSelectedRoomIndex()
    {
        return selectedRoomIndex;
    }
}
