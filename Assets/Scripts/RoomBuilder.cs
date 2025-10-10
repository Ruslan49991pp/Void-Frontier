using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Система строительства комнат из примитивов
/// </summary>
public class RoomBuilder : MonoBehaviour
{
    [Header("Room Materials")]
    public Material floorMaterial;
    public Material wallMaterial;

    [Header("Wall Prefabs")]
    public GameObject wallPrefab;         // Обычная стена SM_Wall
    public GameObject wallCornerPrefab;   // Угловая стена SM_Wall_L

    [Header("Room Settings")]
    public float cellSize = 1f;
    public float wallHeight = 3f;
    public float floorThickness = 0.1f;
    public float wallThickness = 0.2f;

    private static RoomBuilder instance;
    private Dictionary<Vector2Int, WallInfo> globalWalls = new Dictionary<Vector2Int, WallInfo>();
    private Dictionary<Vector2Int, GameObject> activeWalls = new Dictionary<Vector2Int, GameObject>();
    private Vector2Int? excludedDoorPosition = null; // Позиция где не нужно создавать стену (для двери)

    public static RoomBuilder Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("RoomBuilder");
                instance = go.AddComponent<RoomBuilder>();
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            InitializeWithGridManager();
            InitializeMaterials();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Инициализация с параметрами GridManager
    /// </summary>
    void InitializeWithGridManager()
    {
        GridManager gridManager = FindObjectOfType<GridManager>();
        if (gridManager != null)
        {
            cellSize = gridManager.cellSize;
        }
    }

    void InitializeMaterials()
    {
        if (floorMaterial == null)
        {
            // Используем встроенный Lit материал для пола
            floorMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (floorMaterial.shader == null)
            {
                // Fallback на стандартный шейдер если URP недоступен
                floorMaterial = new Material(Shader.Find("Standard"));
            }
            floorMaterial.color = new Color(0.6f, 0.6f, 0.7f, 1f);
            floorMaterial.SetFloat("_Metallic", 0.1f);
            floorMaterial.SetFloat("_Smoothness", 0.3f);
            floorMaterial.name = "FloorMaterial_Lit";
        }

        if (wallMaterial == null)
        {
            // Используем встроенный Lit материал для стен
            wallMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (wallMaterial.shader == null)
            {
                // Fallback на стандартный шейдер если URP недоступен
                wallMaterial = new Material(Shader.Find("Standard"));
            }
            wallMaterial.color = new Color(0.85f, 0.85f, 0.9f, 1f);
            wallMaterial.SetFloat("_Metallic", 0.05f);
            wallMaterial.SetFloat("_Smoothness", 0.6f);
            wallMaterial.name = "WallMaterial_Lit";
        }

        // Загружаем префаб стены если он не назначен
        if (wallPrefab == null)
        {
            wallPrefab = Resources.Load<GameObject>("Prefabs/SM_Wall");
            if (wallPrefab == null)
            {
                FileLogger.Log("Warning: SM_Wall prefab not found in Resources/Prefabs/, using primitive cube");
            }
            else
            {
                FileLogger.Log("SM_Wall prefab loaded successfully");
            }
        }

        // Загружаем префаб угловой стены если он не назначен
        if (wallCornerPrefab == null)
        {
            wallCornerPrefab = Resources.Load<GameObject>("Prefabs/SM_Wall_L");
            if (wallCornerPrefab == null)
            {
                FileLogger.Log("Warning: SM_Wall_L prefab not found in Resources/Prefabs/, using regular wall for corners");
            }
            else
            {
                FileLogger.Log("SM_Wall_L prefab loaded successfully");
            }
        }
    }

    /// <summary>
    /// Построить комнату заданного размера
    /// </summary>
    public GameObject BuildRoom(Vector2Int gridPosition, Vector2Int roomSize, string roomName, Transform parent = null)
    {
        return BuildRoom(gridPosition, roomSize, roomName, 0, parent);
    }

    public GameObject BuildRoom(Vector2Int gridPosition, Vector2Int roomSize, string roomName, int rotation, Transform parent = null)
    {
        FileLogger.Log($"DEBUG: BuildRoom called - name: {roomName}, position: {gridPosition}, size: {roomSize}, rotation: {rotation}");

        GameObject roomGO = new GameObject($"Room_{roomName}_{gridPosition.x}_{gridPosition.y}");
        if (parent != null)
            roomGO.transform.SetParent(parent);

        FileLogger.Log($"DEBUG: Room GameObject created: {roomGO.name}");

        // СНАЧАЛА добавляем компонент информации о комнате, чтобы пол мог его использовать
        RoomInfo roomInfo = roomGO.AddComponent<RoomInfo>();
        roomInfo.gridPosition = gridPosition;
        roomInfo.roomSize = roomSize;
        roomInfo.roomName = roomName;
        roomInfo.roomRotation = rotation;

        FileLogger.Log($"DEBUG: RoomInfo added to {roomGO.name}");

        // Создаем пол (теперь RoomInfo уже есть)
        FileLogger.Log($"DEBUG: About to call CreateFloor...");
        CreateFloor(roomGO, gridPosition, roomSize);

        // Создаем стены с учетом поворота
        FileLogger.Log($"DEBUG: About to call CreateWalls...");
        CreateWalls(roomGO, gridPosition, roomSize, rotation);

        FileLogger.Log($"DEBUG: RoomInfo added to {roomGO.name}");

        return roomGO;
    }

    /// <summary>
    /// Создать пол комнаты (только внутренние клетки без стен)
    /// </summary>
    void CreateFloor(GameObject parent, Vector2Int gridPosition, Vector2Int roomSize)
    {
        FileLogger.Log($"DEBUG: CreateFloor called for room {parent.name}, size: {roomSize}, gridPos: {gridPosition}");

        // Создаем пол только для внутренних клеток (без стен по периметру)
        int innerWidth = Mathf.Max(1, roomSize.x - 2);  // убираем левую и правую стены
        int innerHeight = Mathf.Max(1, roomSize.y - 2); // убираем верхнюю и нижнюю стены

        FileLogger.Log($"DEBUG: Inner floor dimensions: {innerWidth}x{innerHeight}");

        if (innerWidth > 0 && innerHeight > 0)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(parent.transform);

            FileLogger.Log($"DEBUG: Floor object created: {floor.name}");

            // Размеры внутреннего пола
            float width = innerWidth * cellSize;
            float height = innerHeight * cellSize;

            // Позиция - центр внутренней области
            Vector3 centerOffset = new Vector3(
                (roomSize.x - 1) * cellSize * 0.5f,  // центр комнаты по X
                -floorThickness * 0.5f,              // чуть ниже уровня земли
                (roomSize.y - 1) * cellSize * 0.5f   // центр комнаты по Z
            );

            floor.transform.localPosition = centerOffset;
            floor.transform.localScale = new Vector3(width, floorThickness, height);

            // Материал
            floor.GetComponent<Renderer>().material = floorMaterial;

            // Настраиваем коллайдер пола для выделения комнаты
            BoxCollider floorCollider = floor.GetComponent<BoxCollider>();
            if (floorCollider != null)
            {
                // Коллайдер становится не-триггером для работы с raycast
                floorCollider.isTrigger = false;

                FileLogger.Log($"DEBUG: Floor collider configured as non-trigger");

                // Добавляем компонент для выделения к полу
                LocationObjectInfo locationInfo = floor.AddComponent<LocationObjectInfo>();
                RoomInfo roomInfo = parent.GetComponent<RoomInfo>();
                if (roomInfo != null)
                {
                    locationInfo.objectName = roomInfo.roomName;
                    locationInfo.objectType = roomInfo.roomType;
                    locationInfo.health = 500f;
                    locationInfo.isDestructible = true;

                    FileLogger.Log($"DEBUG: LocationObjectInfo added to floor with room name: {roomInfo.roomName}");
                }
                else
                {
                    locationInfo.objectName = "Room";
                    locationInfo.objectType = "Room";
                    locationInfo.health = 500f;
                    locationInfo.isDestructible = true;

                    FileLogger.Log($"DEBUG: LocationObjectInfo added to floor with default name (no RoomInfo found)");
                }

                // Добавляем ссылку на родительскую комнату для удобства
                RoomFloorMarker floorMarker = floor.AddComponent<RoomFloorMarker>();
                floorMarker.parentRoom = parent;

                FileLogger.Log($"DEBUG: RoomFloorMarker added to floor, parent: {parent.name}");
                FileLogger.Log($"DEBUG: Floor layer: {floor.layer}, position: {floor.transform.position}");
            }
            else
            {
                FileLogger.Log($"ERROR: Floor collider is null!");
            }
        }

        // Для очень маленьких комнат (например 2x2) создаем минимальный пол
        if (roomSize.x <= 2 || roomSize.y <= 2)
        {
            FileLogger.Log($"DEBUG: Creating small room floor for {roomSize.x}x{roomSize.y} room");

            // Создаем отдельные плитки пола для проходимых клеток
            CreateFloorTiles(parent, gridPosition, roomSize);

            // Добавляем общий коллайдер для выделения маленькой комнаты
            CreateSmallRoomSelectionFloor(parent, gridPosition, roomSize);
        }
    }

    /// <summary>
    /// Создать отдельные плитки пола для маленьких комнат
    /// </summary>
    void CreateFloorTiles(GameObject parent, Vector2Int gridPosition, Vector2Int roomSize)
    {
        for (int x = 1; x < roomSize.x - 1; x++) // пропускаем края (стены)
        {
            for (int y = 1; y < roomSize.y - 1; y++) // пропускаем края (стены)
            {
                GameObject floorTile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floorTile.name = $"FloorTile_{x}_{y}";
                floorTile.transform.SetParent(parent.transform);

                // Позиция плитки (центр клетки)
                Vector3 tilePosition = new Vector3(
                    x * cellSize + cellSize * 0.5f,
                    -floorThickness * 0.5f,
                    y * cellSize + cellSize * 0.5f
                );

                floorTile.transform.localPosition = tilePosition;
                floorTile.transform.localScale = new Vector3(cellSize * 0.9f, floorThickness, cellSize * 0.9f);

                // Материал
                floorTile.GetComponent<Renderer>().material = floorMaterial;

                // Убираем коллайдер (коллайдер выделения будет отдельно)
                Destroy(floorTile.GetComponent<Collider>());
            }
        }
    }

    /// <summary>
    /// Создать невидимый коллайдер для выделения маленькой комнаты
    /// </summary>
    void CreateSmallRoomSelectionFloor(GameObject parent, Vector2Int gridPosition, Vector2Int roomSize)
    {
        FileLogger.Log($"DEBUG: CreateSmallRoomSelectionFloor for {parent.name}");

        GameObject selectionFloor = new GameObject("SelectionFloor");
        selectionFloor.transform.SetParent(parent.transform);

        // Позиция - центр комнаты, ниже уровня земли
        Vector3 centerOffset = new Vector3(
            (roomSize.x - 1) * cellSize * 0.5f,  // центр комнаты по X
            -floorThickness * 2f,                // еще ниже визуального пола
            (roomSize.y - 1) * cellSize * 0.5f   // центр комнаты по Z
        );

        selectionFloor.transform.localPosition = centerOffset;

        // Добавляем коллайдер для выделения
        BoxCollider selectionCollider = selectionFloor.AddComponent<BoxCollider>();
        float width = roomSize.x * cellSize;
        float height = roomSize.y * cellSize;
        selectionCollider.size = new Vector3(width, floorThickness, height);
        selectionCollider.isTrigger = false; // Не триггер для raycast

        // Добавляем компонент для выделения
        LocationObjectInfo locationInfo = selectionFloor.AddComponent<LocationObjectInfo>();
        RoomInfo roomInfo = parent.GetComponent<RoomInfo>();
        if (roomInfo != null)
        {
            locationInfo.objectName = roomInfo.roomName;
            locationInfo.objectType = roomInfo.roomType;
            locationInfo.health = 500f;
            locationInfo.isDestructible = true;
        }
        else
        {
            locationInfo.objectName = "Small Room";
            locationInfo.objectType = "Room";
            locationInfo.health = 500f;
            locationInfo.isDestructible = true;
        }

        // Добавляем ссылку на родительскую комнату
        RoomFloorMarker floorMarker = selectionFloor.AddComponent<RoomFloorMarker>();
        floorMarker.parentRoom = parent;

        FileLogger.Log($"DEBUG: SmallRoom SelectionFloor created, layer: {selectionFloor.layer}, position: {selectionFloor.transform.position}");
    }

    /// <summary>
    /// Создать стены комнаты
    /// </summary>
    void CreateWalls(GameObject parent, Vector2Int gridPosition, Vector2Int roomSize)
    {
        CreateWalls(parent, gridPosition, roomSize, 0);
    }

    void CreateWalls(GameObject parent, Vector2Int gridPosition, Vector2Int roomSize, int rotation)
    {
        List<WallData> wallsToCreate = GetRoomWalls(gridPosition, roomSize, rotation);

        foreach (WallData wall in wallsToCreate)
        {
            AddWallToGlobal(wall);
        }

        UpdateWallVisuals();
    }

    /// <summary>
    /// Получить список стен для комнаты
    /// Стены располагаются НА клетках сетки по периметру комнаты
    /// </summary>
    List<WallData> GetRoomWalls(Vector2Int gridPosition, Vector2Int roomSize)
    {
        return GetRoomWalls(gridPosition, roomSize, 0);
    }

    List<WallData> GetRoomWalls(Vector2Int gridPosition, Vector2Int roomSize, int rotation)
    {
        FileLogger.Log($"DEBUG: GetRoomWalls called with position {gridPosition}, size {roomSize}, rotation {rotation}");

        List<WallData> walls = new List<WallData>();
        HashSet<Vector2Int> wallPositions = new HashSet<Vector2Int>();

        // Добавляем все клетки по периметру комнаты
        for (int x = 0; x < roomSize.x; x++)
        {
            for (int y = 0; y < roomSize.y; y++)
            {
                Vector2Int cellPos = new Vector2Int(gridPosition.x + x, gridPosition.y + y);

                // Проверяем, является ли клетка частью периметра
                bool isPerimeter = (x == 0 || x == roomSize.x - 1 || y == 0 || y == roomSize.y - 1);

                if (isPerimeter && !wallPositions.Contains(cellPos))
                {
                    wallPositions.Add(cellPos);

                    // Определяем сторону комнаты для стены с учетом поворота
                    WallSide wallSide = DetermineWallSide(x, y, roomSize, rotation);
                    WallType wallType = DetermineWallType(wallSide);

                    WallData wallData = new WallData(cellPos, WallDirection.Vertical, gridPosition, roomSize, wallSide, wallType, rotation);
                    walls.Add(wallData);

                    FileLogger.Log($"DEBUG: Added wall at position {cellPos}, side {wallSide}, type {wallType}");
                }
            }
        }

        FileLogger.Log($"DEBUG: GetRoomWalls returning {walls.Count} walls");
        return walls;
    }

    /// <summary>
    /// Определить сторону комнаты для стены по её относительной позиции
    /// </summary>
    WallSide DetermineWallSide(int relativeX, int relativeY, Vector2Int roomSize)
    {
        return DetermineWallSide(relativeX, relativeY, roomSize, 0);
    }

    WallSide DetermineWallSide(int relativeX, int relativeY, Vector2Int roomSize, int rotation)
    {
        bool isLeftEdge = (relativeX == 0);
        bool isRightEdge = (relativeX == roomSize.x - 1);
        bool isTopEdge = (relativeY == roomSize.y - 1);
        bool isBottomEdge = (relativeY == 0);

        WallSide baseSide = WallSide.None;

        // Сначала проверяем углы (комбинации сторон)
        if (isTopEdge && isLeftEdge) baseSide = WallSide.TopLeft;
        else if (isTopEdge && isRightEdge) baseSide = WallSide.TopRight;
        else if (isBottomEdge && isLeftEdge) baseSide = WallSide.BottomLeft;
        else if (isBottomEdge && isRightEdge) baseSide = WallSide.BottomRight;
        // Затем проверяем обычные стороны
        else if (isTopEdge) baseSide = WallSide.Top;
        else if (isBottomEdge) baseSide = WallSide.Bottom;
        else if (isLeftEdge) baseSide = WallSide.Left;
        else if (isRightEdge) baseSide = WallSide.Right;

        // Применяем поворот к определенной стороне
        return RotateWallSide(baseSide, rotation);
    }

    /// <summary>
    /// Повернуть сторону стены на заданный угол
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
            // Углы
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
    /// Определить тип стены (прямая или угловая) по её стороне
    /// </summary>
    WallType DetermineWallType(WallSide wallSide)
    {
        switch (wallSide)
        {
            case WallSide.TopLeft:
            case WallSide.TopRight:
            case WallSide.BottomLeft:
            case WallSide.BottomRight:
                return WallType.Corner;

            case WallSide.Top:
            case WallSide.Bottom:
            case WallSide.Left:
            case WallSide.Right:
                return WallType.Straight;

            default:
                return WallType.Straight;
        }
    }

    /// <summary>
    /// Добавить стену в глобальный реестр
    /// </summary>
    void AddWallToGlobal(WallData wallData)
    {
        Vector2Int key = wallData.GetKey();

        // Проверяем исключение для двери
        if (excludedDoorPosition.HasValue && excludedDoorPosition.Value == key)
        {
            FileLogger.Log($"DEBUG: SKIPPING wall creation at {key} - door exclusion is active!");
            return;
        }

        // DEBUG: Проверяем есть ли уже дверь в этой позиции
        if (IsDoorAtPosition(key))
        {
            FileLogger.Log($"DEBUG: SKIPPING wall creation at {key} - door already exists!");
            return; // НЕ добавляем стену если там уже есть дверь
        }

        FileLogger.Log($"DEBUG: Adding wall to global registry at {key}");

        if (globalWalls.ContainsKey(key))
        {
            globalWalls[key].referenceCount++;
            FileLogger.Log($"DEBUG: Incremented wall reference count at {key} to {globalWalls[key].referenceCount}");
        }
        else
        {
            globalWalls[key] = new WallInfo
            {
                wallData = wallData,
                referenceCount = 1
            };
            FileLogger.Log($"DEBUG: Created new wall entry at {key}");
        }
    }

    /// <summary>
    /// Проверить есть ли дверь в указанной позиции
    /// </summary>
    bool IsDoorAtPosition(Vector2Int position)
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Door") && obj.activeInHierarchy && !obj.name.Contains("Preview"))
            {
                Vector3 objWorldPos = obj.transform.position;
                Vector2Int objGridPos = WorldToGrid(objWorldPos);
                if (objGridPos == position)
                {
                    FileLogger.Log($"DEBUG: Found door at {position}: {obj.name}");
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Удалить дверь в указанной позиции
    /// </summary>
    void RemoveDoorAtPosition(Vector2Int position)
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Door") && obj.activeInHierarchy && !obj.name.Contains("Preview"))
            {
                Vector3 objWorldPos = obj.transform.position;
                Vector2Int objGridPos = WorldToGrid(objWorldPos);
                if (objGridPos == position)
                {
                    FileLogger.Log($"DEBUG: Destroying door GameObject: {obj.name} at position {position}");
                    DestroyImmediate(obj);
                    return;
                }
            }
        }
        FileLogger.Log($"WARNING: No door found at position {position} to remove");
    }

    /// <summary>
    /// Преобразовать мировую позицию в координаты сетки
    /// </summary>
    Vector2Int WorldToGrid(Vector3 worldPos)
    {
        // Используем тот же алгоритм что и в GridManager
        int x = Mathf.FloorToInt(worldPos.x / cellSize);
        int z = Mathf.FloorToInt(worldPos.z / cellSize);
        return new Vector2Int(x, z);
    }

    /// <summary>
    /// Удалить комнату и обновить стены
    /// </summary>
    public void RemoveRoom(Vector2Int gridPosition, Vector2Int roomSize)
    {
        RemoveRoom(gridPosition, roomSize, 0);
    }

    /// <summary>
    /// Удалить комнату с учетом поворота
    /// </summary>
    public void RemoveRoom(Vector2Int gridPosition, Vector2Int roomSize, int rotation)
    {
        FileLogger.Log($"DEBUG: RemoveRoom called for position {gridPosition}, size {roomSize}, rotation {rotation}");

        // Сначала покажем что есть в глобальном реестре
        FileLogger.Log($"DEBUG: Current globalWalls registry has {globalWalls.Count} entries:");
        foreach (var kvp in globalWalls)
        {
            FileLogger.Log($"DEBUG: globalWalls contains key {kvp.Key} with refCount {kvp.Value.referenceCount}");
        }

        List<WallData> allPotentialWalls = GetRoomWalls(gridPosition, roomSize, rotation);

        // Фильтруем только те стены, которые реально существуют в globalWalls
        List<WallData> wallsToRemove = new List<WallData>();
        foreach (WallData wall in allPotentialWalls)
        {
            Vector2Int key = wall.GetKey();
            if (globalWalls.ContainsKey(key))
            {
                wallsToRemove.Add(wall);
            }
            else
            {
                FileLogger.Log($"DEBUG: Skipping wall at {wall.position} - not found in globalWalls (likely door position)");
            }
        }

        FileLogger.Log($"DEBUG: Found {wallsToRemove.Count} actual walls to remove (from {allPotentialWalls.Count} potential)");

        foreach (WallData wall in wallsToRemove)
        {
            FileLogger.Log($"DEBUG: Removing wall at {wall.position}");
            RemoveWallFromGlobal(wall);
        }

        // Отдельно удаляем двери в позициях где не было стен (исключенные позиции)
        foreach (WallData potentialWall in allPotentialWalls)
        {
            Vector2Int key = potentialWall.GetKey();
            if (!globalWalls.ContainsKey(key) && IsDoorAtPosition(key))
            {
                FileLogger.Log($"DEBUG: Found door at excluded position {key}, removing it");
                RemoveDoorAtPosition(key);
            }
        }

        UpdateWallVisuals();
    }

    /// <summary>
    /// Удалить стену из глобального реестра
    /// </summary>
    void RemoveWallFromGlobal(WallData wallData)
    {
        Vector2Int key = wallData.GetKey();
        FileLogger.Log($"DEBUG: RemoveWallFromGlobal called for key {key}");

        if (globalWalls.ContainsKey(key))
        {
            int oldRefCount = globalWalls[key].referenceCount;
            globalWalls[key].referenceCount--;
            FileLogger.Log($"DEBUG: Wall reference count reduced from {oldRefCount} to {globalWalls[key].referenceCount}");

            if (globalWalls[key].referenceCount <= 0)
            {
                FileLogger.Log($"DEBUG: Removing wall from globalWalls registry at {key}");
                globalWalls.Remove(key);

                // Также удаляем дверь, если она есть в этой позиции
                if (IsDoorAtPosition(key))
                {
                    FileLogger.Log($"DEBUG: Found and removing door at position {key}");

                    // Найдем дверь и удалим её
                    GameObject[] allObjects = FindObjectsOfType<GameObject>();
                    foreach (GameObject obj in allObjects)
                    {
                        if (obj.name.Contains("Door") && obj.activeInHierarchy && !obj.name.Contains("Preview"))
                        {
                            Vector3 objWorldPos = obj.transform.position;
                            Vector2Int objGridPos = new Vector2Int(
                                Mathf.RoundToInt(objWorldPos.x / cellSize),
                                Mathf.RoundToInt(objWorldPos.z / cellSize)
                            );

                            if (objGridPos == key)
                            {
                                FileLogger.Log($"DEBUG: Destroying door GameObject: {obj.name}");
                                DestroyImmediate(obj);
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                FileLogger.Log($"DEBUG: Wall still has {globalWalls[key].referenceCount} references, keeping it");
            }
        }
        else
        {
            FileLogger.Log($"DEBUG: Wall at {key} not found in globalWalls registry (likely shared wall already removed)");
        }
    }

    /// <summary>
    /// Обновить визуальное отображение всех стен
    /// </summary>
    void UpdateWallVisuals()
    {
        FileLogger.Log($"DEBUG: UpdateWallVisuals started. Current activeWalls count: {activeWalls.Count}, globalWalls count: {globalWalls.Count}");

        // Удаляем старые стены
        int destroyedWalls = 0;
        foreach (var kvp in activeWalls)
        {
            if (kvp.Value != null)
            {
                DestroyImmediate(kvp.Value);
                destroyedWalls++;
            }
        }
        FileLogger.Log($"DEBUG: Destroyed {destroyedWalls} old wall GameObjects");
        activeWalls.Clear();

        // Создаем новые стены
        int createdWalls = 0;
        foreach (var kvp in globalWalls)
        {
            Vector2Int key = kvp.Key;
            WallData wallData = kvp.Value.wallData;

            GameObject wallGO = CreateWallGameObject(wallData);
            activeWalls[key] = wallGO;
            createdWalls++;
        }
        FileLogger.Log($"DEBUG: Created {createdWalls} new wall GameObjects");

        FileLogger.Log($"DEBUG: UpdateWallVisuals completed. Final activeWalls count: {activeWalls.Count}");
    }

    /// <summary>
    /// Создать GameObject стены (стена занимает полную клетку)
    /// </summary>
    GameObject CreateWallGameObject(WallData wallData)
    {
        GameObject wall;

        // Выбираем правильный префаб в зависимости от типа стены
        GameObject prefabToUse = null;
        if (wallData.wallType == WallType.Corner && wallCornerPrefab != null)
        {
            prefabToUse = wallCornerPrefab;
        }
        else if (wallPrefab != null)
        {
            prefabToUse = wallPrefab;
        }

        // Используем выбранный префаб если доступен, иначе примитив
        if (prefabToUse != null)
        {
            wall = Instantiate(prefabToUse);
            wall.name = $"Wall_{wallData.position.x}_{wallData.position.y}_{wallData.wallSide}_{wallData.wallType}";

            // Позиционирование стены в центре клетки
            Vector3 worldPos = GridToWorldPosition(wallData.position);
            wall.transform.position = worldPos;

            // Поворачиваем стену так, чтобы она смотрела внутрь комнаты
            float rotationY = wallData.GetRotationTowardRoom();
            wall.transform.rotation = Quaternion.Euler(0, rotationY, 0);

            // Устанавливаем стандартный размер для префаба
            wall.transform.localScale = Vector3.one;

            // Убеждаемся что есть коллайдер для блокировки прохода
            BoxCollider collider = wall.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = wall.AddComponent<BoxCollider>();
            }
            collider.isTrigger = false;

            // Применяем материал к рендереру если есть
            Renderer renderer = wall.GetComponent<Renderer>();
            if (renderer != null && wallMaterial != null)
            {
                renderer.material = wallMaterial;
            }

            // Логируем для отладки
            string prefabName = (wallData.wallType == WallType.Corner) ? "SM_Wall_L" : "SM_Wall";
            string connectionInfo = "";
            if (wallData.wallType == WallType.Corner)
            {
                connectionInfo = " (connector-aligned)";
            }
            else
            {
                connectionInfo = " (room-facing)";
            }
            FileLogger.Log($"{prefabName} created at {wallData.position} on {wallData.wallSide} side, rotation: {rotationY}°{connectionInfo}");
        }
        else
        {
            // Fallback на примитив куб если префаб недоступен
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = $"Wall_{wallData.position.x}_{wallData.position.y}";

            // Позиционирование стены в центре клетки
            Vector3 worldPos = GridToWorldPosition(wallData.position);
            worldPos.y = wallHeight * 0.5f; // центр стены по высоте
            wall.transform.position = worldPos;

            // Стена занимает полную клетку по размеру
            wall.transform.localScale = new Vector3(cellSize, wallHeight, cellSize);

            // Материал
            wall.GetComponent<Renderer>().material = wallMaterial;

            // Настраиваем коллайдер для блокировки прохода
            BoxCollider collider = wall.GetComponent<BoxCollider>();
            collider.isTrigger = false;
        }

        // Добавляем компонент для идентификации
        WallComponent wallComp = wall.AddComponent<WallComponent>();
        wallComp.wallData = wallData;

        return wall;
    }

    /// <summary>
    /// Преобразование координат сетки в мировые координаты (центр клетки)
    /// </summary>
    Vector3 GridToWorldPosition(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x * cellSize + cellSize * 0.5f, 0, gridPos.y * cellSize + cellSize * 0.5f);
    }

    /// <summary>
    /// Получить все активные стены (для GridManager)
    /// </summary>
    public List<GameObject> GetActiveWalls()
    {
        List<GameObject> walls = new List<GameObject>();
        foreach (var kvp in activeWalls)
        {
            if (kvp.Value != null)
                walls.Add(kvp.Value);
        }
        return walls;
    }

    /// <summary>
    /// Проверить, есть ли стена в указанной позиции
    /// </summary>
    public bool HasWallAt(Vector2Int gridPos, WallDirection direction)
    {
        WallData wallData = new WallData(gridPos, direction);
        return globalWalls.ContainsKey(wallData.GetKey());
    }

    /// <summary>
    /// Установить позицию где не нужно создавать стену (для двери)
    /// </summary>
    public void SetDoorExclusion(Vector2Int doorPosition)
    {
        excludedDoorPosition = doorPosition;
        FileLogger.Log($"DEBUG: RoomBuilder - door exclusion set at {doorPosition}");
    }

    /// <summary>
    /// Очистить исключение двери
    /// </summary>
    public void ClearDoorExclusion()
    {
        if (excludedDoorPosition.HasValue)
        {
            FileLogger.Log($"DEBUG: RoomBuilder - door exclusion cleared from {excludedDoorPosition.Value}");
        }
        excludedDoorPosition = null;
    }
}

/// <summary>
/// Данные о стене
/// </summary>
[System.Serializable]
public class WallData
{
    public Vector2Int position;
    public WallDirection direction;
    public Vector2Int roomPosition;  // позиция комнаты
    public Vector2Int roomSize;      // размер комнаты
    public WallSide wallSide;        // с какой стороны комнаты находится стена
    public WallType wallType;        // тип стены (прямая или угловая)
    public int roomRotation;         // поворот комнаты в градусах (0, 90, 180, 270)

    public WallData(Vector2Int pos, WallDirection dir)
    {
        position = pos;
        direction = dir;
        roomPosition = Vector2Int.zero;
        roomSize = Vector2Int.zero;
        wallSide = WallSide.None;
        wallType = WallType.Straight;
        roomRotation = 0;
    }

    public WallData(Vector2Int pos, WallDirection dir, Vector2Int roomPos, Vector2Int roomSz, WallSide side, WallType type, int rotation = 0)
    {
        position = pos;
        direction = dir;
        roomPosition = roomPos;
        roomSize = roomSz;
        wallSide = side;
        wallType = type;
        roomRotation = rotation;
    }

    public Vector2Int GetKey()
    {
        // Создаем уникальный ключ для стены на основе только позиции
        // (направление больше не важно, так как стена занимает полную клетку)
        return new Vector2Int(position.x, position.y);
    }

    /// <summary>
    /// Получить поворот стены для правильного соединения коннекторов
    /// wallSide уже учитывает поворот комнаты, поэтому просто возвращаем нужную ориентацию
    /// </summary>
    public float GetRotationTowardRoom()
    {
        // wallSide уже "повернут" при создании WallData, но нам нужно компенсировать поворот комнаты
        // Получаем базовый поворот для стены как будто комната повернута на 0°
        float baseRotation;
        switch (wallSide)
        {
            // Прямые стены - смотрят внутрь комнаты
            case WallSide.Top:    baseRotation = 180f; break; // смотрит вниз
            case WallSide.Bottom: baseRotation = 0f; break;   // смотрит вверх
            case WallSide.Left:   baseRotation = 90f; break;  // смотрит вправо
            case WallSide.Right:  baseRotation = 270f; break; // смотрит влево

            // Угловые стены (L-образные) - точное совпадение коннекторов
            case WallSide.TopLeft:     baseRotation = 90f; break;
            case WallSide.TopRight:    baseRotation = 180f; break;
            case WallSide.BottomLeft:  baseRotation = 0f; break;
            case WallSide.BottomRight: baseRotation = 270f; break;

            default: baseRotation = 0f; break;
        }

        // Компенсируем поворот комнаты: вычитаем roomRotation из базового поворота
        float finalRotation = (baseRotation - roomRotation) % 360f;
        if (finalRotation < 0) finalRotation += 360f;

        // Debug лог для отслеживания поворотов
        FileLogger.Log($"[DEBUG] Wall at {position} - wallSide: {wallSide}, roomRotation: {roomRotation}, baseRotation: {baseRotation}°, finalRotation: {finalRotation}°");

        return finalRotation;
    }
}

/// <summary>
/// Информация о стене в глобальном реестре
/// </summary>
public class WallInfo
{
    public WallData wallData;
    public int referenceCount;
}

/// <summary>
/// Направление стены
/// </summary>
public enum WallDirection
{
    Horizontal = 0,
    Vertical = 1
}

/// <summary>
/// Сторона комнаты, где расположена стена
/// </summary>
public enum WallSide
{
    None = 0,
    Top = 1,        // верхняя сторона комнаты
    Bottom = 2,     // нижняя сторона комнаты
    Left = 3,       // левая сторона комнаты
    Right = 4,      // правая сторона комнаты
    TopLeft = 5,    // верхний левый угол
    TopRight = 6,   // верхний правый угол
    BottomLeft = 7, // нижний левый угол
    BottomRight = 8 // нижний правый угол
}

/// <summary>
/// Тип стены
/// </summary>
public enum WallType
{
    Straight = 0,   // Обычная прямая стена (SM_Wall)
    Corner = 1      // Угловая стена (SM_Wall_L)
}

/// <summary>
/// Компонент для идентификации стен
/// </summary>
public class WallComponent : MonoBehaviour
{
    public WallData wallData;
}

/// <summary>
/// Информация о комнате
/// </summary>
public class RoomInfo : MonoBehaviour
{
    public Vector2Int gridPosition;
    public Vector2Int roomSize;
    public string roomName;
    public string roomType;
    public int roomRotation; // поворот комнаты в градусах (0, 90, 180, 270)

    [Header("Main Object")]
    public ModuleMainObject mainObject; // Ссылка на главный объект в комнате
    public MainObjectType currentMainObjectType = MainObjectType.None; // Тип текущего главного объекта

    [Header("Room Health")]
    public float maxWallHealth = 500f;
    public float currentWallHealth;

    void Start()
    {
        currentWallHealth = maxWallHealth;
    }

    /// <summary>
    /// Проверить, можно ли установить главный объект
    /// </summary>
    public bool CanPlaceMainObject()
    {
        return mainObject == null && currentMainObjectType == MainObjectType.None;
    }

    /// <summary>
    /// Установить главный объект
    /// </summary>
    public bool SetMainObject(ModuleMainObject obj)
    {
        if (!CanPlaceMainObject())
        {
            FileLogger.Log($"[RoomInfo] Cannot place main object in room {roomName} - already has one");
            return false;
        }

        mainObject = obj;
        currentMainObjectType = obj.objectType;
        obj.parentRoom = gameObject;
        obj.roomGridPosition = gridPosition;

        FileLogger.Log($"[RoomInfo] Main object {obj.objectName} placed in room {roomName}");
        return true;
    }

    /// <summary>
    /// Нанести урон стенам комнаты
    /// </summary>
    public void TakeDamage(float damage)
    {
        currentWallHealth -= damage;
        FileLogger.Log($"[RoomInfo] Room {roomName} walls took {damage} damage. Health: {currentWallHealth}/{maxWallHealth}");

        if (currentWallHealth <= 0)
        {
            DestroyRoom();
        }
    }

    /// <summary>
    /// Уничтожить комнату
    /// </summary>
    void DestroyRoom()
    {
        FileLogger.Log($"[RoomInfo] Room {roomName} walls destroyed!");

        // Уничтожаем главный объект если он есть
        if (mainObject != null)
        {
            Destroy(mainObject.gameObject);
        }

        // Уничтожаем саму комнату через систему строительства
        ShipBuildingSystem buildingSystem = FindObjectOfType<ShipBuildingSystem>();
        if (buildingSystem != null)
        {
            buildingSystem.DeleteRoom(gameObject);
        }
    }
}