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
    public GameObject wallCornerPrefab;   // Угловая стена SM_Wall_L (внешние углы)
    public GameObject wallInnerCornerPrefab; // Внутренняя угловая стена SM_Wall_L_Undo (внутренние углы в вырезах)

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

        // Загружаем префаб внутренней угловой стены если он не назначен
        if (wallInnerCornerPrefab == null)
        {
            FileLogger.Log("[INIT] Attempting to load SM_Wall_L_Undo from Resources/Prefabs/SM_Wall_L_Undo");
            wallInnerCornerPrefab = Resources.Load<GameObject>("Prefabs/SM_Wall_L_Undo");
            if (wallInnerCornerPrefab == null)
            {
                FileLogger.LogError("[ERROR] SM_Wall_L_Undo prefab not found in Resources/Prefabs/! Using regular corner wall as fallback");
                wallInnerCornerPrefab = wallCornerPrefab; // Fallback на обычный угол
            }
            else
            {
                FileLogger.Log($"[INIT] ✓ SM_Wall_L_Undo prefab loaded successfully: {wallInnerCornerPrefab.name}");
            }
        }
        else
        {
            FileLogger.Log($"[INIT] SM_Wall_L_Undo prefab already assigned: {wallInnerCornerPrefab.name}");
        }
    }

    /// <summary>
    /// Построить комнату с кастомным силуэтом (после удаления клеток)
    /// </summary>
    public GameObject BuildCustomRoom(Vector2Int gridPosition, Vector2Int roomSize, string roomName, List<Vector2Int> wallPositions, List<Vector2Int> floorPositions, List<Vector2Int> innerCornerPositions = null)
    {
        FileLogger.Log($"DEBUG: BuildCustomRoom called - name: {roomName}, position: {gridPosition}, size: {roomSize}, walls: {wallPositions.Count}, floor: {floorPositions.Count}, innerCorners: {(innerCornerPositions != null ? innerCornerPositions.Count : 0)}");

        GameObject roomGO = new GameObject($"Room_{roomName}_{gridPosition.x}_{gridPosition.y}");

        // Добавляем компонент информации о комнате
        RoomInfo roomInfo = roomGO.AddComponent<RoomInfo>();
        roomInfo.gridPosition = gridPosition;
        roomInfo.roomSize = roomSize;
        roomInfo.roomName = roomName;
        roomInfo.roomRotation = 0;

        FileLogger.Log($"DEBUG: RoomInfo added to {roomGO.name}");

        // Создаем пол из отдельных плиток
        CreateCustomFloor(roomGO, floorPositions);

        // Создаем стены по списку позиций, передавая внутренние углы
        CreateCustomWalls(roomGO, wallPositions, floorPositions, innerCornerPositions);

        return roomGO;
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
    /// Создать кастомный пол из отдельных плиток
    /// </summary>
    void CreateCustomFloor(GameObject parent, List<Vector2Int> floorPositions)
    {
        FileLogger.Log($"DEBUG: CreateCustomFloor called with {floorPositions.Count} floor tiles");

        if (floorPositions.Count == 0)
        {
            FileLogger.Log("DEBUG: No floor positions provided, skipping floor creation");
            return;
        }

        // Создаем контейнер для пола
        GameObject floorContainer = new GameObject("FloorTiles");
        floorContainer.transform.SetParent(parent.transform);

        // Создаем отдельную плитку для каждой позиции пола
        foreach (Vector2Int floorPos in floorPositions)
        {
            GameObject floorTile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floorTile.name = $"FloorTile_{floorPos.x}_{floorPos.y}";
            floorTile.transform.SetParent(floorContainer.transform);

            // Позиция плитки (центр клетки)
            Vector3 worldPos = GridToWorldPosition(floorPos);
            worldPos.y = -floorThickness * 0.5f;
            floorTile.transform.position = worldPos;

            // Размер плитки
            floorTile.transform.localScale = new Vector3(cellSize * 0.95f, floorThickness, cellSize * 0.95f);

            // Материал
            Renderer renderer = floorTile.GetComponent<Renderer>();
            if (renderer != null && floorMaterial != null)
            {
                renderer.material = floorMaterial;
            }

            // Убираем коллайдер у плиток (коллайдер выделения будет отдельно)
            Collider tileCollider = floorTile.GetComponent<Collider>();
            if (tileCollider != null)
            {
                Destroy(tileCollider);
            }
        }

        // Создаем коллайдер для выделения комнаты
        CreateCustomSelectionFloor(parent, floorPositions);

        FileLogger.Log($"DEBUG: Created {floorPositions.Count} floor tiles");
    }

    /// <summary>
    /// Создать коллайдер для выделения кастомной комнаты
    /// </summary>
    void CreateCustomSelectionFloor(GameObject parent, List<Vector2Int> floorPositions)
    {
        if (floorPositions.Count == 0) return;

        FileLogger.Log($"DEBUG: CreateCustomSelectionFloor for {parent.name}");

        // Находим центр пола
        Vector3 center = Vector3.zero;
        foreach (Vector2Int pos in floorPositions)
        {
            center += GridToWorldPosition(pos);
        }
        center /= floorPositions.Count;

        GameObject selectionFloor = new GameObject("SelectionFloor");
        selectionFloor.transform.SetParent(parent.transform);
        selectionFloor.transform.position = new Vector3(center.x, -floorThickness * 2f, center.z);

        // Создаем коллайдеры для каждой плитки пола (для выделения)
        foreach (Vector2Int pos in floorPositions)
        {
            GameObject colliderTile = new GameObject($"SelectionTile_{pos.x}_{pos.y}");
            colliderTile.transform.SetParent(selectionFloor.transform);

            Vector3 worldPos = GridToWorldPosition(pos);
            worldPos.y = -floorThickness * 2f;
            colliderTile.transform.position = worldPos;

            BoxCollider tileCollider = colliderTile.AddComponent<BoxCollider>();
            tileCollider.size = new Vector3(cellSize, floorThickness, cellSize);
            tileCollider.isTrigger = false;
        }

        // Добавляем компонент для выделения к главному объекту
        LocationObjectInfo locationInfo = selectionFloor.AddComponent<LocationObjectInfo>();
        RoomInfo roomInfo = parent.GetComponent<RoomInfo>();
        if (roomInfo != null)
        {
            locationInfo.objectName = roomInfo.roomName;
            locationInfo.objectType = roomInfo.roomType;
            locationInfo.health = 500f;
            locationInfo.isDestructible = true;
        }

        RoomFloorMarker floorMarker = selectionFloor.AddComponent<RoomFloorMarker>();
        floorMarker.parentRoom = parent;

        FileLogger.Log($"DEBUG: Custom SelectionFloor created with {floorPositions.Count} tiles");
    }

    /// <summary>
    /// Создать кастомные стены по списку позиций
    /// </summary>
    void CreateCustomWalls(GameObject parent, List<Vector2Int> wallPositions, List<Vector2Int> floorPositions, List<Vector2Int> innerCornerPositions = null)
    {
        FileLogger.Log($"DEBUG: CreateCustomWalls called with {wallPositions.Count} wall positions and {floorPositions.Count} floor positions");

        if (wallPositions.Count == 0)
        {
            FileLogger.Log("DEBUG: No wall positions provided, skipping wall creation");
            return;
        }

        // Создаем HashSet для быстрой проверки наличия стен и пола
        HashSet<Vector2Int> wallSet = new HashSet<Vector2Int>(wallPositions);
        HashSet<Vector2Int> floorSet = new HashSet<Vector2Int>(floorPositions);

        // Используем переданные внутренние углы или ищем их сами (fallback)
        if (innerCornerPositions == null)
        {
            FileLogger.Log("DEBUG: No inner corners provided, using fallback FindInnerCornerPositions");
            innerCornerPositions = FindInnerCornerPositions(wallSet, floorSet);
        }
        FileLogger.Log($"DEBUG: Using {innerCornerPositions.Count} inner corner positions");

        // НЕ добавляем внутренние углы в wallSet - они уже должны быть в roomPerimeter
        // wallSet уже содержит все нужные позиции, включая внутренние углы

        // Создаем стены для каждой позиции (включая внутренние углы)
        List<WallData> wallsToCreate = new List<WallData>();

        // Используем только wallPositions - внутренние углы уже в нем
        foreach (Vector2Int wallPos in wallPositions)
        {
            // Определяем тип стены (угловая или прямая) и её ориентацию с учетом пола
            WallSide wallSide = DetermineWallSideFromNeighbors(wallPos, wallSet, floorSet);

            // Проверяем является ли это внутренним углом
            bool isInnerCornerWall = innerCornerPositions.Contains(wallPos);
            WallType wallType = isInnerCornerWall ? WallType.InnerCorner : DetermineWallType(wallSide);

            WallData wallData = new WallData(
                wallPos,
                WallDirection.Vertical,
                Vector2Int.zero, // roomPosition - не используется для кастомных стен
                Vector2Int.zero, // roomSize - не используется
                wallSide,
                wallType,
                0 // rotation
            );

            wallsToCreate.Add(wallData);
            FileLogger.Log($"DEBUG: Wall at {wallPos}: side={wallSide}, type={wallType}");
        }

        // Добавляем стены в глобальный реестр
        foreach (WallData wall in wallsToCreate)
        {
            AddWallToGlobal(wall);
        }

        UpdateWallVisuals();

        FileLogger.Log($"DEBUG: Created {wallsToCreate.Count} custom walls (including {innerCornerPositions.Count} inner corners)");
    }

    /// <summary>
    /// Найти позиции для внутренних углов (в местах вырезов)
    /// </summary>
    List<Vector2Int> FindInnerCornerPositions(HashSet<Vector2Int> wallSet, HashSet<Vector2Int> floorSet)
    {
        List<Vector2Int> innerCorners = new List<Vector2Int>();

        // Находим границы области для поиска
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (Vector2Int pos in wallSet)
        {
            minX = Mathf.Min(minX, pos.x);
            maxX = Mathf.Max(maxX, pos.x);
            minY = Mathf.Min(minY, pos.y);
            maxY = Mathf.Max(maxY, pos.y);
        }

        foreach (Vector2Int pos in floorSet)
        {
            minX = Mathf.Min(minX, pos.x);
            maxX = Mathf.Max(maxX, pos.x);
            minY = Mathf.Min(minY, pos.y);
            maxY = Mathf.Max(maxY, pos.y);
        }

        // Расширяем область поиска на 1 клетку
        minX--; maxX++; minY--; maxY++;

        // Проверяем все клетки в области
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);

                // Пропускаем если это уже стена или пол
                if (wallSet.Contains(pos) || floorSet.Contains(pos))
                    continue;

                // Проверяем является ли это внутренним углом
                if (IsInnerCorner(pos, wallSet, floorSet))
                {
                    innerCorners.Add(pos);
                    FileLogger.Log($"DEBUG: Inner corner detected at {pos}");
                }
            }
        }

        return innerCorners;
    }

    /// <summary>
    /// Проверить является ли позиция внутренним углом
    /// </summary>
    bool IsInnerCorner(Vector2Int pos, HashSet<Vector2Int> wallSet, HashSet<Vector2Int> floorSet)
    {
        // Проверяем соседние клетки
        bool hasWallTop = wallSet.Contains(pos + Vector2Int.up);
        bool hasWallBottom = wallSet.Contains(pos + Vector2Int.down);
        bool hasWallLeft = wallSet.Contains(pos + Vector2Int.left);
        bool hasWallRight = wallSet.Contains(pos + Vector2Int.right);

        // Проверяем диагональные клетки (где должен быть пол)
        bool hasFloorTopLeft = floorSet.Contains(pos + new Vector2Int(-1, 1));
        bool hasFloorTopRight = floorSet.Contains(pos + new Vector2Int(1, 1));
        bool hasFloorBottomLeft = floorSet.Contains(pos + new Vector2Int(-1, -1));
        bool hasFloorBottomRight = floorSet.Contains(pos + new Vector2Int(1, -1));

        // Внутренний угол: 2 стены под прямым углом + пол в противоположной диагонали

        // TopLeft конфигурация: стены справа и снизу, пол слева сверху
        if (hasWallRight && hasWallBottom && hasFloorTopLeft)
            return true;

        // TopRight конфигурация: стены слева и снизу, пол справа сверху
        if (hasWallLeft && hasWallBottom && hasFloorTopRight)
            return true;

        // BottomLeft конфигурация: стены справа и сверху, пол слева снизу
        if (hasWallRight && hasWallTop && hasFloorBottomLeft)
            return true;

        // BottomRight конфигурация: стены слева и сверху, пол справа снизу
        if (hasWallLeft && hasWallTop && hasFloorBottomRight)
            return true;

        return false;
    }

    /// <summary>
    /// Определить сторону/тип стены на основе соседних клеток
    /// Учитывает позиции пола для правильного определения внутренних/внешних углов
    /// </summary>
    WallSide DetermineWallSideFromNeighbors(Vector2Int wallPos, HashSet<Vector2Int> wallSet, HashSet<Vector2Int> floorSet)
    {
        // Проверяем соседние клетки (4 стороны)
        bool hasWallTop = wallSet.Contains(wallPos + Vector2Int.up);
        bool hasWallBottom = wallSet.Contains(wallPos + Vector2Int.down);
        bool hasWallLeft = wallSet.Contains(wallPos + Vector2Int.left);
        bool hasWallRight = wallSet.Contains(wallPos + Vector2Int.right);

        // Проверяем диагональные клетки (для определения внутренних углов)
        bool hasFloorTopLeft = floorSet.Contains(wallPos + new Vector2Int(-1, 1));
        bool hasFloorTopRight = floorSet.Contains(wallPos + new Vector2Int(1, 1));
        bool hasFloorBottomLeft = floorSet.Contains(wallPos + new Vector2Int(-1, -1));
        bool hasFloorBottomRight = floorSet.Contains(wallPos + new Vector2Int(1, -1));

        // Проверяем где находится пол (комната) относительно стены
        bool hasFloorTop = floorSet.Contains(wallPos + Vector2Int.up);
        bool hasFloorBottom = floorSet.Contains(wallPos + Vector2Int.down);
        bool hasFloorLeft = floorSet.Contains(wallPos + Vector2Int.left);
        bool hasFloorRight = floorSet.Contains(wallPos + Vector2Int.right);

        // УГЛЫ: проверяем угловые конфигурации
        // Угол появляется когда есть стены с двух перпендикулярных сторон

        // TopLeft: стены сверху и слева
        if (hasWallTop && hasWallLeft)
        {
            // Внешний угол: пол справа снизу (комната в BottomRight диагонали)
            if (hasFloorBottomRight || hasFloorBottom || hasFloorRight)
                return WallSide.TopLeft;
            // Внутренний угол (вырез): пол слева сверху
            if (hasFloorTopLeft)
                return WallSide.BottomRight; // Инвертированный угол для выреза
        }

        // TopRight: стены сверху и справа
        if (hasWallTop && hasWallRight)
        {
            // Внешний угол: пол слева снизу (комната в BottomLeft диагонали)
            if (hasFloorBottomLeft || hasFloorBottom || hasFloorLeft)
                return WallSide.TopRight;
            // Внутренний угол (вырез): пол справа сверху
            if (hasFloorTopRight)
                return WallSide.BottomLeft; // Инвертированный угол для выреза
        }

        // BottomLeft: стены снизу и слева
        if (hasWallBottom && hasWallLeft)
        {
            // Внешний угол: пол справа сверху (комната в TopRight диагонали)
            if (hasFloorTopRight || hasFloorTop || hasFloorRight)
                return WallSide.BottomLeft;
            // Внутренний угол (вырез): пол слева снизу
            if (hasFloorBottomLeft)
                return WallSide.TopRight; // Инвертированный угол для выреза
        }

        // BottomRight: стены снизу и справа
        if (hasWallBottom && hasWallRight)
        {
            // Внешний угол: пол слева сверху (комната в TopLeft диагонали)
            if (hasFloorTopLeft || hasFloorTop || hasFloorLeft)
                return WallSide.BottomRight;
            // Внутренний угол (вырез): пол справа снизу
            if (hasFloorBottomRight)
                return WallSide.TopLeft; // Инвертированный угол для выреза
        }

        // ВНУТРЕННИЕ УГЛЫ (вырезы): стены с двух сторон под прямым углом, пол в противоположной диагонали
        // Эти углы "смотрят" внутрь выреза

        // Вырез TopLeft: стены справа и снизу, пол слева сверху
        if (hasWallRight && hasWallBottom && hasFloorTopLeft)
            return WallSide.BottomRight;

        // Вырез TopRight: стены слева и снизу, пол справа сверху
        if (hasWallLeft && hasWallBottom && hasFloorTopRight)
            return WallSide.BottomLeft;

        // Вырез BottomLeft: стены справа и сверху, пол слева снизу
        if (hasWallRight && hasWallTop && hasFloorBottomLeft)
            return WallSide.TopRight;

        // Вырез BottomRight: стены слева и сверху, пол справа снизу
        if (hasWallLeft && hasWallTop && hasFloorBottomRight)
            return WallSide.TopLeft;

        // ПРЯМЫЕ СТЕНЫ: определяем ориентацию по соседним стенам или полу

        // Вертикальная стена (стены сверху/снизу)
        if (hasWallTop || hasWallBottom)
        {
            // Определяем в какую сторону смотрит стена (где комната)
            if (hasFloorLeft)
                return WallSide.Right; // Смотрит влево (комната слева)
            if (hasFloorRight)
                return WallSide.Left;  // Смотрит вправо (комната справа)
            return WallSide.Left; // По умолчанию
        }

        // Горизонтальная стена (стены слева/справа)
        if (hasWallLeft || hasWallRight)
        {
            // Определяем в какую сторону смотрит стена (где комната)
            if (hasFloorTop)
                return WallSide.Bottom; // Смотрит вниз (комната сверху)
            if (hasFloorBottom)
                return WallSide.Top;    // Смотрит вверх (комната снизу)
            return WallSide.Top; // По умолчанию
        }

        // Одиночная стена - определяем по полу
        if (hasFloorLeft || hasFloorRight)
            return WallSide.Left;   // Вертикальная
        if (hasFloorTop || hasFloorBottom)
            return WallSide.Top;    // Горизонтальная

        // По умолчанию вертикальная стена
        return WallSide.Left;
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
    /// Обновить визуальное отображение всех стен (ОПТИМИЗИРОВАНО - инкрементальное обновление)
    /// </summary>
    void UpdateWallVisuals()
    {
        FileLogger.Log($"DEBUG: UpdateWallVisuals started (INCREMENTAL). Current activeWalls count: {activeWalls.Count}, globalWalls count: {globalWalls.Count}");

        // ИНКРЕМЕНТАЛЬНОЕ ОБНОВЛЕНИЕ: Находим стены которые нужно удалить
        List<Vector2Int> wallsToRemove = new List<Vector2Int>();
        foreach (var kvp in activeWalls)
        {
            if (!globalWalls.ContainsKey(kvp.Key))
            {
                wallsToRemove.Add(kvp.Key);
            }
        }

        // Удаляем только стены которых больше нет
        int destroyedWalls = 0;
        foreach (Vector2Int key in wallsToRemove)
        {
            if (activeWalls[key] != null)
            {
                DestroyImmediate(activeWalls[key]);
                destroyedWalls++;
            }
            activeWalls.Remove(key);
        }
        FileLogger.Log($"DEBUG: Destroyed {destroyedWalls} wall GameObjects (removed from global)");

        // ИНКРЕМЕНТАЛЬНОЕ ОБНОВЛЕНИЕ: Создаем только новые стены
        int createdWalls = 0;
        int updatedWalls = 0;
        foreach (var kvp in globalWalls)
        {
            Vector2Int key = kvp.Key;
            WallData wallData = kvp.Value.wallData;

            if (!activeWalls.ContainsKey(key))
            {
                // Новая стена - создаем
                GameObject wallGO = CreateWallGameObject(wallData);
                activeWalls[key] = wallGO;
                createdWalls++;
            }
            else
            {
                // Стена уже существует - просто обновляем счетчик
                updatedWalls++;
            }
        }
        FileLogger.Log($"DEBUG: Created {createdWalls} new walls, kept {updatedWalls} existing walls");

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

        FileLogger.Log($"[DEBUG] CreateWallGameObject: wallType={wallData.wallType}, position={wallData.position}");
        FileLogger.Log($"[DEBUG] Prefab availability: wallInnerCornerPrefab={(wallInnerCornerPrefab != null ? "OK" : "NULL")}, wallCornerPrefab={(wallCornerPrefab != null ? "OK" : "NULL")}, wallPrefab={(wallPrefab != null ? "OK" : "NULL")}");

        if (wallData.wallType == WallType.InnerCorner && wallInnerCornerPrefab != null)
        {
            prefabToUse = wallInnerCornerPrefab;
            FileLogger.Log($"[DEBUG] ✓ Using INNER CORNER prefab (SM_Wall_L_Undo) at {wallData.position}");
        }
        else if (wallData.wallType == WallType.InnerCorner && wallInnerCornerPrefab == null)
        {
            FileLogger.LogError($"[ERROR] Inner corner requested but wallInnerCornerPrefab is NULL! Using fallback.");
            prefabToUse = wallCornerPrefab;
        }
        else if (wallData.wallType == WallType.Corner && wallCornerPrefab != null)
        {
            prefabToUse = wallCornerPrefab;
            FileLogger.Log($"[DEBUG] Using OUTER CORNER prefab (SM_Wall_L) at {wallData.position}");
        }
        else if (wallPrefab != null)
        {
            prefabToUse = wallPrefab;
            FileLogger.Log($"[DEBUG] Using STRAIGHT WALL prefab (SM_Wall) at {wallData.position}");
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

        // Создаем пол под стеной
        CreateFloorUnderWall(wall, wallData);

        return wall;
    }

    /// <summary>
    /// Создать пол под стеной
    /// </summary>
    void CreateFloorUnderWall(GameObject wallParent, WallData wallData)
    {
        // Создаем пол как дочерний объект стены
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "FloorUnderWall";
        floor.transform.SetParent(wallParent.transform);

        // Позиционируем пол под стеной в той же клетке
        Vector3 worldPos = GridToWorldPosition(wallData.position);
        worldPos.y = -floorThickness * 0.5f;  // На уровне пола
        floor.transform.position = worldPos;

        // Размер пола - полная клетка
        floor.transform.localScale = new Vector3(cellSize * 0.95f, floorThickness, cellSize * 0.95f);

        // Применяем материал пола
        Renderer renderer = floor.GetComponent<Renderer>();
        if (renderer != null && floorMaterial != null)
        {
            renderer.material = floorMaterial;
        }

        // Убираем коллайдер у пола под стеной (коллайдер стены достаточен)
        Collider floorCollider = floor.GetComponent<Collider>();
        if (floorCollider != null)
        {
            Destroy(floorCollider);
        }

        FileLogger.Log($"DEBUG: Created floor under wall at {wallData.position}");
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

        // КРИТИЧНО: Для внутренних углов (SM_Wall_L_Undo) ротация НЕ инвертируется
        // wallSide уже правильно установлен в DetermineWallSideFromNeighbors()
        // (для внутренних углов wallSide инвертирован относительно внешних углов там же)
        if (wallType == WallType.InnerCorner)
        {
            FileLogger.Log($"[DEBUG] INNER CORNER using standard rotation: {baseRotation}° (no adjustment)");
        }

        // Компенсируем поворот комнаты: вычитаем roomRotation из базового поворота
        float finalRotation = (baseRotation - roomRotation) % 360f;
        if (finalRotation < 0) finalRotation += 360f;

        // Debug лог для отслеживания поворотов
        string wallTypeStr = wallType == WallType.InnerCorner ? "InnerCorner" : (wallType == WallType.Corner ? "OuterCorner" : "Straight");
        FileLogger.Log($"[DEBUG] Wall at {position} - type: {wallTypeStr}, wallSide: {wallSide}, roomRotation: {roomRotation}, baseRotation: {baseRotation}°, finalRotation: {finalRotation}°");

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
    Straight = 0,      // Обычная прямая стена (SM_Wall)
    Corner = 1,        // Угловая стена внешняя (SM_Wall_L)
    InnerCorner = 2    // Угловая стена внутренняя для вырезов (SM_Wall_L_Undo)
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