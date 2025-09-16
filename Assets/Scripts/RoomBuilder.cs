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

    [Header("Room Settings")]
    public float cellSize = 1f;
    public float wallHeight = 3f;
    public float floorThickness = 0.1f;
    public float wallThickness = 0.2f;

    private static RoomBuilder instance;
    private Dictionary<Vector2Int, WallInfo> globalWalls = new Dictionary<Vector2Int, WallInfo>();
    private Dictionary<Vector2Int, GameObject> activeWalls = new Dictionary<Vector2Int, GameObject>();

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
    }

    /// <summary>
    /// Построить комнату заданного размера
    /// </summary>
    public GameObject BuildRoom(Vector2Int gridPosition, Vector2Int roomSize, string roomName, Transform parent = null)
    {
        GameObject roomGO = new GameObject($"Room_{roomName}_{gridPosition.x}_{gridPosition.y}");
        if (parent != null)
            roomGO.transform.SetParent(parent);

        // Создаем пол
        CreateFloor(roomGO, gridPosition, roomSize);

        // Создаем стены
        CreateWalls(roomGO, gridPosition, roomSize);

        // Добавляем компонент информации о комнате
        RoomInfo roomInfo = roomGO.AddComponent<RoomInfo>();
        roomInfo.gridPosition = gridPosition;
        roomInfo.roomSize = roomSize;
        roomInfo.roomName = roomName;

        return roomGO;
    }

    /// <summary>
    /// Создать пол комнаты (только внутренние клетки без стен)
    /// </summary>
    void CreateFloor(GameObject parent, Vector2Int gridPosition, Vector2Int roomSize)
    {
        // Создаем пол только для внутренних клеток (без стен по периметру)
        int innerWidth = Mathf.Max(1, roomSize.x - 2);  // убираем левую и правую стены
        int innerHeight = Mathf.Max(1, roomSize.y - 2); // убираем верхнюю и нижнюю стены

        if (innerWidth > 0 && innerHeight > 0)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(parent.transform);

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

            // Убираем коллайдер с пола, он не нужен для навигации
            Destroy(floor.GetComponent<Collider>());
        }

        // Для очень маленьких комнат (например 2x2) создаем минимальный пол
        if (roomSize.x <= 2 || roomSize.y <= 2)
        {
            // Создаем отдельные плитки пола для проходимых клеток
            CreateFloorTiles(parent, gridPosition, roomSize);
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

                // Убираем коллайдер
                Destroy(floorTile.GetComponent<Collider>());
            }
        }
    }

    /// <summary>
    /// Создать стены комнаты
    /// </summary>
    void CreateWalls(GameObject parent, Vector2Int gridPosition, Vector2Int roomSize)
    {
        List<WallData> wallsToCreate = GetRoomWalls(gridPosition, roomSize);

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
                    walls.Add(new WallData(cellPos, WallDirection.Vertical)); // направление не важно теперь
                }
            }
        }

        return walls;
    }

    /// <summary>
    /// Добавить стену в глобальный реестр
    /// </summary>
    void AddWallToGlobal(WallData wallData)
    {
        Vector2Int key = wallData.GetKey();

        if (globalWalls.ContainsKey(key))
        {
            globalWalls[key].referenceCount++;
        }
        else
        {
            globalWalls[key] = new WallInfo
            {
                wallData = wallData,
                referenceCount = 1
            };
        }
    }

    /// <summary>
    /// Удалить комнату и обновить стены
    /// </summary>
    public void RemoveRoom(Vector2Int gridPosition, Vector2Int roomSize)
    {
        List<WallData> wallsToRemove = GetRoomWalls(gridPosition, roomSize);

        foreach (WallData wall in wallsToRemove)
        {
            RemoveWallFromGlobal(wall);
        }

        UpdateWallVisuals();
    }

    /// <summary>
    /// Удалить стену из глобального реестра
    /// </summary>
    void RemoveWallFromGlobal(WallData wallData)
    {
        Vector2Int key = wallData.GetKey();

        if (globalWalls.ContainsKey(key))
        {
            globalWalls[key].referenceCount--;

            if (globalWalls[key].referenceCount <= 0)
            {
                globalWalls.Remove(key);
            }
        }
    }

    /// <summary>
    /// Обновить визуальное отображение всех стен
    /// </summary>
    void UpdateWallVisuals()
    {
        // Удаляем старые стены
        foreach (var kvp in activeWalls)
        {
            if (kvp.Value != null)
                DestroyImmediate(kvp.Value);
        }
        activeWalls.Clear();

        // Создаем новые стены
        foreach (var kvp in globalWalls)
        {
            Vector2Int key = kvp.Key;
            WallData wallData = kvp.Value.wallData;

            GameObject wallGO = CreateWallGameObject(wallData);
            activeWalls[key] = wallGO;
        }
    }

    /// <summary>
    /// Создать GameObject стены (стена занимает полную клетку)
    /// </summary>
    GameObject CreateWallGameObject(WallData wallData)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
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
}

/// <summary>
/// Данные о стене
/// </summary>
[System.Serializable]
public class WallData
{
    public Vector2Int position;
    public WallDirection direction;

    public WallData(Vector2Int pos, WallDirection dir)
    {
        position = pos;
        direction = dir;
    }

    public Vector2Int GetKey()
    {
        // Создаем уникальный ключ для стены на основе только позиции
        // (направление больше не важно, так как стена занимает полную клетку)
        return new Vector2Int(position.x, position.y);
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
}