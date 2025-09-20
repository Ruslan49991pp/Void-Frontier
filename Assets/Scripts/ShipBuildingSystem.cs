using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoomData
{
    public string roomName;
    public string roomType;
    public Vector2Int size; // размер в клетках
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

    [Header("Preview Settings")]
    public Material previewMaterial;
    public LayerMask groundLayerMask = 1;

    // Состояния строительства
    public enum BuildingPhase
    {
        None,           // Режим строительства неактивен
        PlacingRoom,    // Этап 1: Размещение призрака комнаты
        PlacingDoor     // Этап 2: Размещение двери
    }

    // Внутренние переменные
    private bool buildingMode = false;
    private bool deletionMode = false;
    private BuildingPhase currentPhase = BuildingPhase.None;
    private int selectedRoomIndex = 0;
    private GameObject previewObject;
    private GameObject doorPreviewObject; // Призрак двери
    private Vector2Int pendingRoomPosition; // Позиция размещаемой комнаты
    private Vector2Int pendingRoomSize; // Размер размещаемой комнаты
    private int pendingRoomRotation; // Поворот размещаемой комнаты
    private List<Vector2Int> straightWallPositions = new List<Vector2Int>(); // Позиции прямых стен для двери
    private Vector2Int doorPosition = Vector2Int.zero; // Текущая позиция двери
    private List<GameObject> previewCells = new List<GameObject>();
    private GameUI gameUI;
    private List<GameObject> builtRooms = new List<GameObject>();

    // Автоматическое строительство при выборе двери (ОТКЛЮЧЕНО)
    private float doorSelectionTimer = 0f;
    private const float AUTO_BUILD_DELAY = float.MaxValue; // автоматическое строительство отключено
    private Vector2Int lastDoorPosition = Vector2Int.zero;
    private bool roomBuilt = false;
    private GameObject highlightedRoom = null;
    private Material originalMaterial = null;
    private int roomRotation = 0; // Поворот комнаты в градусах (0, 90, 180, 270)
    private bool scrollWheelUsedThisFrame = false; // Флаг использования ролика в этом кадре

    // События
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
        // Сбрасываем флаг использования ролика в начале кадра
        scrollWheelUsedThisFrame = false;

        // Проверяем паузу, но разрешаем строительство во время паузы стройки
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
    /// Инициализация системы строительства
    /// </summary>
    void InitializeBuildingSystem()
    {
        if (gridManager == null)
            gridManager = FindObjectOfType<GridManager>();

        if (playerCamera == null)
            playerCamera = Camera.main;

        gameUI = FindObjectOfType<GameUI>();

        CreateDefaultRooms();
    }

    /// <summary>
    /// Создание стандартных типов комнат
    /// </summary>
    void CreateDefaultRooms()
    {
        if (availableRooms.Count == 0)
        {
            // Коридор 4x10
            RoomData corridor = new RoomData();
            corridor.roomName = "Коридор";
            corridor.roomType = "Corridor";
            corridor.size = new Vector2Int(4, 10);
            corridor.cost = 80;
            corridor.previewColor = Color.cyan;
            corridor.prefab = null; // Будем создавать через RoomBuilder
            availableRooms.Add(corridor);

            // Ангар 10x10
            RoomData hangar = new RoomData();
            hangar.roomName = "Ангар";
            hangar.roomType = "Hangar";
            hangar.size = new Vector2Int(10, 10);
            hangar.cost = 200;
            hangar.previewColor = Color.blue;
            hangar.prefab = null; // Будем создавать через RoomBuilder
            availableRooms.Add(hangar);

            // Жилой модуль 6x10
            RoomData livingRoom = new RoomData();
            livingRoom.roomName = "Жилой модуль";
            livingRoom.roomType = "Living";
            livingRoom.size = new Vector2Int(6, 10);
            livingRoom.cost = 120;
            livingRoom.previewColor = Color.green;
            livingRoom.prefab = null; // Будем создавать через RoomBuilder
            availableRooms.Add(livingRoom);
        }
    }


    /// <summary>
    /// Создание префаба комнаты
    /// </summary>
    GameObject CreateRoomPrefab(RoomData roomData)
    {
        GameObject roomPrefab = new GameObject($"{roomData.roomName}_Prefab");

        // Создаем визуальное представление
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "RoomVisual";
        visual.transform.SetParent(roomPrefab.transform);
        visual.transform.localPosition = Vector3.zero;

        // Масштабируем по размеру комнаты
        float width = roomData.size.x * gridManager.cellSize;
        float height = roomData.size.y * gridManager.cellSize;
        visual.transform.localScale = new Vector3(width, 2f, height);

        // Настраиваем материал
        Renderer renderer = visual.GetComponent<Renderer>();
        Material roomMaterial = new Material(Shader.Find("Standard"));
        roomMaterial.color = new Color(0.8f, 0.8f, 1f, 1f);
        renderer.material = roomMaterial;

        // Добавляем информацию об объекте
        LocationObjectInfo objectInfo = roomPrefab.AddComponent<LocationObjectInfo>();
        objectInfo.objectName = roomData.roomName;
        objectInfo.objectType = roomData.roomType;
        objectInfo.health = 300f;
        objectInfo.isDestructible = false;

        roomPrefab.SetActive(false);
        return roomPrefab;
    }

    /// <summary>
    /// Включить/выключить режим строительства
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
    /// Установить режим строительства (используется из GameUI)
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
    /// Запустить режим строительства
    /// </summary>
    void StartBuildingMode()
    {
        CreatePreviewObject();
    }

    /// <summary>
    /// Остановить режим строительства
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
    /// Создать объект предварительного просмотра
    /// </summary>
    void CreatePreviewObject()
    {
        if (availableRooms.Count == 0 || selectedRoomIndex >= availableRooms.Count)
            return;

        RoomData currentRoom = availableRooms[selectedRoomIndex];

        if (previewObject != null)
            DestroyImmediate(previewObject);

        ClearPreviewCells();

        // Создаем призрак комнаты с настоящими стенами (используем временную позицию, обновится в UpdatePreview)
        Vector2Int rotatedSize = GetRotatedRoomSize(currentRoom.size, roomRotation);
        previewObject = CreateGhostRoom(Vector2Int.zero, rotatedSize, currentRoom.roomName + "_Preview", roomRotation);
    }

    /// <summary>
    /// Создать призрак комнаты с полупрозрачными стенами
    /// </summary>
    GameObject CreateGhostRoom(Vector2Int gridPosition, Vector2Int roomSize, string roomName, int rotation = 0)
    {
        GameObject ghostRoom = new GameObject(roomName);

        // Пол не создаем - в реальных комнатах его нет

        // Создаем призрачные стены с учетом поворота
        CreateGhostWalls(ghostRoom, Vector2Int.zero, roomSize, rotation);

        return ghostRoom;
    }

    /// <summary>
    /// Создать призрачный пол
    /// </summary>
    void CreateGhostFloor(GameObject parent, Vector2Int gridPosition, Vector2Int roomSize)
    {
        // Создаем пол только для внутренних клеток (без стен по периметру)
        int innerWidth = Mathf.Max(1, roomSize.x - 2);
        int innerHeight = Mathf.Max(1, roomSize.y - 2);

        if (innerWidth > 0 && innerHeight > 0)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "GhostFloor";
            floor.transform.SetParent(parent.transform);

            // Размеры внутреннего пола
            float width = innerWidth * gridManager.cellSize;
            float height = innerHeight * gridManager.cellSize;
            float floorThickness = 0.1f;

            // Позиция - центр внутренней области
            Vector3 centerOffset = new Vector3(
                (roomSize.x - 1) * gridManager.cellSize * 0.5f,
                -floorThickness * 0.5f,
                (roomSize.y - 1) * gridManager.cellSize * 0.5f
            );

            floor.transform.localPosition = centerOffset;
            floor.transform.localScale = new Vector3(width, floorThickness, height);

            // Убираем коллайдер
            Destroy(floor.GetComponent<Collider>());

            // Применяем призрачный материал
            ApplyGhostMaterial(floor.GetComponent<Renderer>(), true);
        }
    }

    /// <summary>
    /// Создать призрачные стены
    /// </summary>
    void CreateGhostWalls(GameObject parent, Vector2Int gridPosition, Vector2Int roomSize, int rotation = 0)
    {
        // Используем ту же логику что и в RoomBuilder для получения стен
        List<WallData> walls = GetGhostRoomWalls(gridPosition, roomSize, rotation);

        foreach (WallData wallData in walls)
        {
            CreateGhostWall(parent, wallData, rotation);
        }
    }

    /// <summary>
    /// Получить список стен для призрачной комнаты (копия логики из RoomBuilder)
    /// </summary>
    List<WallData> GetGhostRoomWalls(Vector2Int gridPosition, Vector2Int roomSize, int rotation = 0)
    {
        List<WallData> walls = new List<WallData>();

        for (int x = 0; x < roomSize.x; x++)
        {
            for (int y = 0; y < roomSize.y; y++)
            {
                Vector2Int cellPos = new Vector2Int(gridPosition.x + x, gridPosition.y + y);

                // Проверяем, является ли клетка частью периметра
                bool isPerimeter = (x == 0 || x == roomSize.x - 1 || y == 0 || y == roomSize.y - 1);

                if (isPerimeter)
                {
                    // Определяем сторону комнаты для стены с учетом поворота
                    WallSide wallSide = DetermineGhostWallSide(x, y, roomSize, rotation);
                    WallType wallType = DetermineGhostWallType(wallSide);

                    walls.Add(new WallData(cellPos, WallDirection.Vertical, gridPosition, roomSize, wallSide, wallType, rotation));
                }
            }
        }

        return walls;
    }

    /// <summary>
    /// Определить сторону комнаты для призрачной стены
    /// </summary>
    WallSide DetermineGhostWallSide(int relativeX, int relativeY, Vector2Int roomSize, int rotation = 0)
    {
        bool isLeftEdge = (relativeX == 0);
        bool isRightEdge = (relativeX == roomSize.x - 1);
        bool isTopEdge = (relativeY == roomSize.y - 1);
        bool isBottomEdge = (relativeY == 0);

        WallSide baseSide = WallSide.None;

        // Сначала проверяем углы
        if (isTopEdge && isLeftEdge) baseSide = WallSide.TopLeft;
        else if (isTopEdge && isRightEdge) baseSide = WallSide.TopRight;
        else if (isBottomEdge && isLeftEdge) baseSide = WallSide.BottomLeft;
        else if (isBottomEdge && isRightEdge) baseSide = WallSide.BottomRight;
        // Затем обычные стороны
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
    /// Определить тип призрачной стены
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
    /// Создать призрачную стену
    /// </summary>
    void CreateGhostWall(GameObject parent, WallData wallData, int roomRotation = 0)
    {
        // Выбираем правильный префаб
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

            // Используем относительные координаты в имени (wallData.position - это абсолютные координаты)
            int relativeX = wallData.position.x - wallData.roomPosition.x;
            int relativeY = wallData.position.y - wallData.roomPosition.y;
            ghostWall.name = $"GhostWall_{relativeX}_{relativeY}_{wallData.wallSide}";

            // Позиционирование временное - будет обновлено в UpdateGhostRoomPosition
            Vector3 worldPos = GridToWorldPosition(wallData.position);
            ghostWall.transform.position = worldPos;

            // Поворот с учетом поворота комнаты - используем тот же метод что и для обычных стен
            float wallRotation = wallData.GetRotationTowardRoom();
            ghostWall.transform.localRotation = Quaternion.Euler(0, wallRotation, 0);

            // Убираем коллайдеры
            Collider[] colliders = ghostWall.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                Destroy(col);
            }

            // Применяем призрачный материал ко всем рендерерам
            Renderer[] renderers = ghostWall.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                ApplyGhostMaterial(renderer, true);
            }
        }
    }


    /// <summary>
    /// Преобразование координат сетки в мировые (для призраков)
    /// </summary>
    Vector3 GridToWorldPosition(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x * gridManager.cellSize + gridManager.cellSize * 0.5f, 0,
                          gridPos.y * gridManager.cellSize + gridManager.cellSize * 0.5f);
    }

    /// <summary>
    /// Обновить позиции всех элементов призрачной комнаты
    /// </summary>
    void UpdateGhostRoomPosition(GameObject ghostRoom, Vector2Int gridPos, Vector2Int roomSize)
    {
        if (ghostRoom == null) return;

        // Находим все призрачные стены и обновляем их позиции
        Transform[] children = ghostRoom.GetComponentsInChildren<Transform>();
        foreach (Transform child in children)
        {
            if (child.name.StartsWith("GhostWall_"))
            {
                // Извлекаем относительные координаты из имени стены
                string[] nameParts = child.name.Split('_');
                if (nameParts.Length >= 3)
                {
                    int relativeX = int.Parse(nameParts[1]);
                    int relativeY = int.Parse(nameParts[2]);

                    // Вычисляем абсолютные координаты стены
                    Vector2Int absolutePos = new Vector2Int(gridPos.x + relativeX, gridPos.y + relativeY);

                    // Обновляем позицию стены
                    Vector3 worldPos = GridToWorldPosition(absolutePos);
                    child.transform.position = worldPos;
                }
            }
        }

        // Устанавливаем базовую позицию родительского объекта
        ghostRoom.transform.position = Vector3.zero;
    }

    /// <summary>
    /// Применить призрачный материал к рендереру
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
    /// Обновление предварительного просмотра
    /// </summary>
    void UpdatePreview()
    {
        if (previewObject == null || playerCamera == null)
            return;

        // В фазе размещения двери призрак комнаты должен оставаться зафиксированным
        if (currentPhase == BuildingPhase.PlacingDoor)
        {
            // Обновляем только цвет призрака комнаты (зеленый, так как позиция уже выбрана)
            UpdatePreviewColor(true);
            return;
        }

        // Только в фазе размещения комнаты обновляем позицию призрака
        if (currentPhase != BuildingPhase.PlacingRoom)
            return;

        // Получаем позицию мыши в мире
        Vector3 mousePosition = Input.mousePosition;
        Ray ray = playerCamera.ScreenPointToRay(mousePosition);

        Vector3 worldPos;
        Vector2Int gridPos;

        // Попробуем raycast на землю/объекты
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            worldPos = hit.point;
            gridPos = gridManager.WorldToGrid(worldPos);
        }
        else
        {
            // Если raycast не попал, используем плоскость Y=0
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float distance))
            {
                worldPos = ray.GetPoint(distance);
                gridPos = gridManager.WorldToGrid(worldPos);
            }
            else
            {
                return; // Не можем определить позицию
            }
        }

        Vector3 snapPosition = gridManager.GridToWorld(gridPos);

        // Обновляем призрачную комнату с правильными координатами
        RoomData currentRoom = availableRooms[selectedRoomIndex];
        Vector2Int rotatedSize = GetRotatedRoomSize(currentRoom.size, roomRotation);
        UpdateGhostRoomPosition(previewObject, gridPos, rotatedSize);

        // Убираем индикаторы клеток - теперь используем полноценный призрак комнаты
        // UpdatePreviewCells(gridPos, currentRoom);

        // Проверяем, можно ли разместить комнату и обновляем цвет призрака
        bool canPlace = CanPlaceRoom(gridPos, currentRoom, roomRotation);
        UpdatePreviewColor(canPlace);

    }

    /// <summary>
    /// Обновить цвет призрака здания в зависимости от возможности строительства
    /// </summary>
    void UpdatePreviewColor(bool canPlace)
    {
        if (previewObject == null) return;

        // Обновляем материалы всех рендереров в призрачной комнате
        Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            ApplyGhostMaterial(renderer, canPlace);
        }
    }

    /// <summary>
    /// Обработка ввода в режиме строительства
    /// </summary>
    void HandleBuildingInput()
    {
        // Проверяем, не находится ли мышь над UI элементом
        bool isPointerOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

        // Обрабатываем ввод в зависимости от текущей фазы строительства
        switch (currentPhase)
        {
            case BuildingPhase.PlacingRoom:
                HandleRoomPlacementInput(isPointerOverUI);
                break;
            case BuildingPhase.PlacingDoor:
                HandleDoorPlacementInput(isPointerOverUI);
                break;
        }

        // Общие клавиши для всех фаз
        HandleCommonBuildingInput();
    }

    /// <summary>
    /// Обработка ввода для фазы размещения комнаты
    /// </summary>
    void HandleRoomPlacementInput(bool isPointerOverUI)
    {
        // ЛКМ - зафиксировать позицию комнаты и перейти к размещению двери
        if (Input.GetMouseButtonDown(0) && !isPointerOverUI)
        {
            TryPlaceRoomGhost();
        }

        // ПКМ - отменить выбор комнаты или выйти из режима строительства
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
    /// Обработка ввода для фазы размещения двери
    /// </summary>
    void HandleDoorPlacementInput(bool isPointerOverUI)
    {
        FileLogger.Log($"DEBUG: HandleDoorPlacementInput called - doorPosition: {doorPosition}, timer: {doorSelectionTimer:F2}, roomBuilt: {roomBuilt}");

        // Обновляем позицию двери по движению мыши
        UpdateDoorPosition();

        // ЛКМ - мгновенно финализировать строительство
        if (Input.GetMouseButtonDown(0) && !isPointerOverUI && !roomBuilt)
        {
            FileLogger.Log("DEBUG: Manual build triggered by LEFT CLICK");
            TryFinalizeBuildRoom();
        }

        // Автоматическое строительство ОТКЛЮЧЕНО - только ручное подтверждение ЛКМ

        // ПКМ - вернуться к размещению комнаты
        if (Input.GetMouseButtonDown(1) && !isPointerOverUI)
        {
            ReturnToRoomPlacement();
        }
    }

    /// <summary>
    /// Обновить позицию двери в зависимости от позиции мыши
    /// </summary>
    void UpdateDoorPosition()
    {
        Vector2Int mouseGridPos = GetGridPositionFromMouse();
        FileLogger.Log($"DEBUG: UpdateDoorPosition - mouseGridPos: {mouseGridPos}, current doorPosition: {doorPosition}");

        // Находим ближайшую прямую стену к позиции мыши
        Vector2Int closestWallPos = doorPosition; // по умолчанию текущая позиция
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

        // Обновляем позицию двери если она изменилась
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
    /// Общие клавиши для всех фаз строительства
    /// </summary>
    void HandleCommonBuildingInput()
    {
        // ESC - выйти из режима строительства
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetBuildMode(false);
        }

        // Q и E - поворот комнаты (только в фазе размещения комнаты)
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

            // Ролик мышки - поворот (только в фазе размещения комнаты)
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
    /// Попытка зафиксировать позицию комнаты и перейти к размещению двери
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

        // Сохраняем данные о размещаемой комнате
        pendingRoomPosition = gridPosition;
        pendingRoomSize = GetRotatedRoomSize(roomData.size, roomRotation);
        pendingRoomRotation = roomRotation;

        // Переходим к фазе размещения двери
        currentPhase = BuildingPhase.PlacingDoor;

        // Сбрасываем флаги автоматического строительства
        doorSelectionTimer = 0f;
        roomBuilt = false;
        lastDoorPosition = Vector2Int.zero;

        // Фиксируем призрак комнаты в выбранной позиции
        UpdateGhostRoomPosition(previewObject, pendingRoomPosition, pendingRoomSize);

        // Находим все прямые стены для размещения двери
        FindStraightWallPositions();

        // Создаем призрак двери
        CreateDoorPreview();

        FileLogger.Log("Phase 2: Placing door - room ghost locked");
    }

    /// <summary>
    /// Найти позиции прямых стен для размещения двери
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

                // Проверяем, является ли клетка частью периметра
                bool isPerimeter = (x == 0 || x == pendingRoomSize.x - 1 || y == 0 || y == pendingRoomSize.y - 1);

                if (isPerimeter)
                {
                    // Проверяем, является ли это прямой стеной (не угол)
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

        // Устанавливаем начальную позицию двери на первой доступной прямой стене
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
    /// Создать призрак двери
    /// </summary>
    void CreateDoorPreview()
    {
        if (doorPreviewObject != null)
            DestroyImmediate(doorPreviewObject);

        // Загружаем префаб двери
        GameObject doorPrefab = Resources.Load<GameObject>("Prefabs/SM_Door");
        if (doorPrefab == null)
        {
            // Fallback: создаем простой куб если префаб не найден
            doorPreviewObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorPreviewObject.name = "DoorPreview_Fallback";
            DestroyImmediate(doorPreviewObject.GetComponent<Collider>());
        }
        else
        {
            // Создаем экземпляр префаба двери
            doorPreviewObject = Instantiate(doorPrefab);
            doorPreviewObject.name = "DoorPreview";

            // Убираем коллайдеры у призрака
            Collider[] colliders = doorPreviewObject.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
                DestroyImmediate(col);
        }

        // Убираем красную подсветку - дверь остается в оригинальном виде
        // Просто делаем двери полупрозрачными чтобы показать что это призрак
        Renderer[] renderers = doorPreviewObject.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer.material != null)
            {
                Material ghostMaterial = new Material(renderer.material);
                Color originalColor = ghostMaterial.color;
                ghostMaterial.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.7f);

                // Настройка прозрачности
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

        // Позиционируем призрак двери
        UpdateDoorPreviewPosition();
    }

    /// <summary>
    /// Обновить позицию призрака двери
    /// </summary>
    void UpdateDoorPreviewPosition()
    {
        if (doorPreviewObject == null) return;

        Vector3 worldPos = gridManager.GridToWorld(doorPosition);
        doorPreviewObject.transform.position = worldPos;

        // Получаем ориентацию стены в этой позиции для правильного поворота двери
        float doorRotation = GetWallRotationAtPosition(doorPosition);
        doorPreviewObject.transform.rotation = Quaternion.Euler(0, doorRotation, 0);

        // Если это fallback куб, устанавливаем размер
        if (doorPreviewObject.name.Contains("Fallback"))
        {
            doorPreviewObject.transform.localScale = new Vector3(gridManager.cellSize * 0.8f, 2f, gridManager.cellSize * 0.8f);
        }
    }

    /// <summary>
    /// Получить ориентацию стены в указанной позиции
    /// </summary>
    float GetWallRotationAtPosition(Vector2Int position)
    {
        // Получаем оригинальный размер комнаты (до поворота)
        RoomData currentRoom = availableRooms[selectedRoomIndex];
        Vector2Int originalRoomSize = currentRoom.size;

        // Определяем где находится позиция относительно комнаты
        Vector2Int relativePos = position - pendingRoomPosition;

        // Определяем сторону стены в повернутой комнате используя ту же логику что и в RoomBuilder
        WallSide wallSide = DetermineWallSideInRotatedRoom(relativePos, pendingRoomSize, pendingRoomRotation);

        // Получаем поворот стены используя тот же алгоритм что и в RoomBuilder
        return GetWallRotationFromSide(wallSide, pendingRoomRotation);
    }

    /// <summary>
    /// Определить сторону стены в повернутой комнате (копия логики из RoomBuilder)
    /// </summary>
    WallSide DetermineWallSideInRotatedRoom(Vector2Int relativePos, Vector2Int roomSize, int rotation)
    {
        bool isLeftEdge = (relativePos.x == 0);
        bool isRightEdge = (relativePos.x == roomSize.x - 1);
        bool isTopEdge = (relativePos.y == roomSize.y - 1);
        bool isBottomEdge = (relativePos.y == 0);

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

        // Применяем поворот к определенной стороне (та же логика что в RoomBuilder)
        return RotateWallSide(baseSide, rotation);
    }

    /// <summary>
    /// Получить поворот стены от стороны (копия логики из RoomBuilder.GetRotationTowardRoom)
    /// </summary>
    float GetWallRotationFromSide(WallSide wallSide, int roomRotation)
    {
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

        return finalRotation;
    }

    /// <summary>
    /// Финализировать строительство комнаты с дверью
    /// </summary>
    void TryFinalizeBuildRoom()
    {
        if (selectedRoomIndex < 0 || selectedRoomIndex >= availableRooms.Count) return;

        RoomData roomData = availableRooms[selectedRoomIndex];

        FileLogger.Log("=== DEBUG: STARTING ROOM FINALIZATION ===");
        FileLogger.Log($"Building room at {pendingRoomPosition}, size {pendingRoomSize}, rotation {pendingRoomRotation}");
        FileLogger.Log($"Door will be placed at {doorPosition}");

        // Логируем все существующие двери перед строительством
        LogExistingDoors();

        // Строим комнату БЕЗ стены в позиции двери
        FileLogger.Log("Building room walls (excluding door position)...");
        BuildRoomWithDoor(pendingRoomPosition, roomData, pendingRoomRotation, doorPosition);

        // Создаем дверь в выбранной позиции
        FileLogger.Log($"Creating door at {doorPosition}...");
        CreateDoorAtPosition(doorPosition);

        // Освобождаем клетку где стоит дверь
        FileLogger.Log($"Freeing door cell at {doorPosition}...");
        gridManager.FreeCell(doorPosition);

        // Логируем все двери после строительства
        LogExistingDoors();

        // Очищаем призраки
        if (previewObject != null)
            DestroyImmediate(previewObject);
        if (doorPreviewObject != null)
            DestroyImmediate(doorPreviewObject);

        // Помечаем что комната построена
        roomBuilt = true;

        // Возвращаемся к фазе размещения комнаты для следующего строительства
        currentPhase = BuildingPhase.PlacingRoom;
        roomRotation = 0; // Сбрасываем поворот

        // Сбрасываем таймер автоматического строительства
        doorSelectionTimer = 0f;
        roomBuilt = false;

        CreatePreviewObject(); // Создаем новый призрак комнаты

        FileLogger.Log($"=== DEBUG: ROOM FINALIZATION COMPLETE ===");
    }

    /// <summary>
    /// Логировать все существующие двери на сцене
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
    /// Строить комнату с дверью (исключая позицию двери из стен)
    /// </summary>
    void BuildRoomWithDoor(Vector2Int gridPosition, RoomData roomData, int rotation, Vector2Int doorPosition)
    {
        // Сначала говорим RoomBuilder исключить позицию двери
        RoomBuilder.Instance.SetDoorExclusion(doorPosition);

        // Строим комнату как обычно
        BuildRoom(gridPosition, roomData, rotation);

        // Очищаем исключение
        RoomBuilder.Instance.ClearDoorExclusion();
    }

    /// <summary>
    /// Создать дверь в указанной позиции
    /// </summary>
    void CreateDoorAtPosition(Vector2Int position)
    {
        // Загружаем префаб двери
        GameObject doorPrefab = Resources.Load<GameObject>("Prefabs/SM_Door");
        if (doorPrefab == null)
        {
            FileLogger.Log("ERROR: SM_Door prefab not found in Resources/Prefabs/!");
            return;
        }

        // Вычисляем позицию и поворот двери
        Vector3 worldPos = gridManager.GridToWorld(position);
        float doorRotation = GetWallRotationAtPosition(position);
        Quaternion rotation = Quaternion.Euler(0, doorRotation, 0);

        // Создаем дверь
        GameObject door = Instantiate(doorPrefab, worldPos, rotation);
        door.name = $"Door_{position.x}_{position.y}";

        FileLogger.Log($"DEBUG: Door created: {door.name} at {door.transform.position} with rotation {doorRotation}°");
        FileLogger.Log($"SUCCESS: Created door at {position}");
    }

    /// <summary>
    /// Заменить стену на дверь в указанной позиции (СТАРЫЙ МЕТОД - НЕ ИСПОЛЬЗУЕТСЯ)
    /// </summary>
    void ReplaceWallWithDoor(Vector2Int position)
    {
        FileLogger.Log($"DEBUG: Starting wall replacement at {position}");

        // Получаем RoomBuilder для доступа к стенам
        RoomBuilder roomBuilder = RoomBuilder.Instance;
        if (roomBuilder == null)
        {
            FileLogger.Log("ERROR: RoomBuilder not found!");
            return;
        }

        // Загружаем префаб двери
        GameObject doorPrefab = Resources.Load<GameObject>("Prefabs/SM_Door");
        if (doorPrefab == null)
        {
            FileLogger.Log("ERROR: SM_Door prefab not found in Resources/Prefabs/!");
            return;
        }

        // Ищем стену в указанной позиции через RoomBuilder
        FileLogger.Log($"DEBUG: Looking for wall at position {position}");
        GameObject wallToReplace = FindWallAtPosition(position);
        if (wallToReplace == null)
        {
            FileLogger.Log($"ERROR: Wall not found at position {position}");
            LogAllWallsOnScene(); // Дополнительный лог всех стен
            return;
        }

        // Проверяем что объект стены все еще существует
        if (wallToReplace == null)
        {
            FileLogger.Log($"ERROR: Wall object is null after finding it at {position}");
            return;
        }

        FileLogger.Log($"DEBUG: Found wall to replace: {wallToReplace.name} at {wallToReplace.transform.position}");

        // Сохраняем позицию и поворот старой стены
        Vector3 wallPosition;
        Quaternion wallRotation;
        string wallName;

        try
        {
            wallPosition = wallToReplace.transform.position;
            wallRotation = wallToReplace.transform.rotation;
            wallName = wallToReplace.name;
            FileLogger.Log($"DEBUG: Wall position: {wallPosition}, rotation: {wallRotation.eulerAngles}");
        }
        catch (System.Exception e)
        {
            FileLogger.Log($"ERROR: Failed to get wall properties: {e.Message}");
            return;
        }

        // Удаляем старую стену
        try
        {
            DestroyImmediate(wallToReplace);
            FileLogger.Log($"DEBUG: Wall {wallName} destroyed");
        }
        catch (System.Exception e)
        {
            FileLogger.Log($"ERROR: Failed to destroy wall: {e.Message}");
            return;
        }

        // Создаем дверь
        GameObject door = Instantiate(doorPrefab, wallPosition, wallRotation);
        door.name = $"Door_{position.x}_{position.y}";

        FileLogger.Log($"DEBUG: Door created: {door.name} at {door.transform.position}");
        FileLogger.Log($"SUCCESS: Replaced wall with door at {position}");
    }

    /// <summary>
    /// Логировать все стены на сцене для отладки
    /// </summary>
    void LogAllWallsOnScene()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int wallCount = 0;
        FileLogger.Log("--- All walls on scene ---");
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Wall") && !obj.name.Contains("Preview") && obj.activeInHierarchy)
            {
                Vector3 objWorldPos = obj.transform.position;
                Vector2Int objGridPos = gridManager.WorldToGrid(objWorldPos);
                FileLogger.Log($"Wall found: {obj.name} at world {objWorldPos} grid {objGridPos}");
                wallCount++;
            }
        }
        FileLogger.Log($"Total walls found: {wallCount}");
        FileLogger.Log("--- End of wall list ---");
    }

    /// <summary>
    /// Найти стену в указанной позиции
    /// </summary>
    GameObject FindWallAtPosition(Vector2Int position)
    {
        FileLogger.Log($"DEBUG: FindWallAtPosition looking for wall at {position}");

        // Ищем среди всех объектов с тегом или именем стены
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int wallsFound = 0;
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Wall") && !obj.name.Contains("Preview"))
            {
                wallsFound++;
                Vector3 objWorldPos = obj.transform.position;
                Vector2Int objGridPos = gridManager.WorldToGrid(objWorldPos);
                FileLogger.Log($"DEBUG: Checking wall {obj.name} at world {objWorldPos} grid {objGridPos}");
                if (objGridPos == position)
                {
                    FileLogger.Log($"DEBUG: FOUND target wall: {obj.name} at {position}");
                    return obj;
                }
            }
        }
        FileLogger.Log($"DEBUG: No wall found at {position}. Total walls checked: {wallsFound}");
        return null;
    }

    /// <summary>
    /// Вернуться к размещению комнаты
    /// </summary>
    void ReturnToRoomPlacement()
    {
        // Очищаем призрак двери
        if (doorPreviewObject != null)
            DestroyImmediate(doorPreviewObject);

        // Возвращаемся к фазе размещения комнаты
        currentPhase = BuildingPhase.PlacingRoom;

        FileLogger.Log("Returned to Phase 1: Placing room");
    }

    /// <summary>
    /// Повернуть комнату на заданный угол
    /// </summary>
    void RotateRoom(int degrees)
    {
        roomRotation = (roomRotation + degrees) % 360;
        if (roomRotation < 0) roomRotation += 360;

        // Обновляем предпросмотр в зависимости от фазы
        if (buildingMode)
        {
            if (currentPhase == BuildingPhase.PlacingRoom && previewObject != null)
            {
                // В фазе размещения комнаты - пересоздаем призрак комнаты
                CreatePreviewObject();
            }
            else if (currentPhase == BuildingPhase.PlacingDoor)
            {
                // В фазе размещения двери - полностью пересоздаем призрак комнаты
                RoomData roomData = availableRooms[selectedRoomIndex];
                pendingRoomSize = GetRotatedRoomSize(roomData.size, roomRotation);
                pendingRoomRotation = roomRotation;

                // Пересоздаем призрак комнаты с правильными поворотами стен
                if (previewObject != null)
                    DestroyImmediate(previewObject);

                previewObject = CreateGhostRoom(pendingRoomPosition, pendingRoomSize, roomData.roomName + "_Preview", roomRotation);

                // Пересчитываем позиции прямых стен
                FindStraightWallPositions();

                // Обновляем призрак двери
                if (doorPreviewObject != null)
                {
                    UpdateDoorPreviewPosition();
                }
            }
        }

        FileLogger.Log($"Room rotated to {roomRotation} degrees");
    }

    /// <summary>
    /// Получить размер комнаты с учетом поворота
    /// </summary>
    Vector2Int GetRotatedRoomSize(Vector2Int originalSize, int rotation)
    {
        if (rotation == 90 || rotation == 270)
        {
            // При повороте на 90 или 270 градусов меняем местами X и Y
            return new Vector2Int(originalSize.y, originalSize.x);
        }
        return originalSize;
    }

    /// <summary>
    /// Отменить выбор комнаты
    /// </summary>
    public void ClearRoomSelection()
    {
        selectedRoomIndex = -1;
        roomRotation = 0; // Сбрасываем поворот при смене выбора

        // Убираем предпросмотр
        if (previewObject != null)
        {
            DestroyImmediate(previewObject);
            previewObject = null;
        }

        ClearPreviewCells();

        // Обновляем UI - убираем выбор здания
        if (gameUI != null)
        {
            gameUI.ClearBuildingSelection();
        }

        FileLogger.Log("Room selection cleared");
    }

    /// <summary>
    /// Установить выбранный тип комнаты
    /// </summary>
    public void SetSelectedRoomType(int index)
    {
        if (index >= 0 && index < availableRooms.Count)
        {
            selectedRoomIndex = index;
            roomRotation = 0; // Сбрасываем поворот при смене типа комнаты
            FileLogger.Log($"[SetSelectedRoomType] Changed to room {index} ({availableRooms[index].roomName}), buildingMode: {buildingMode}");
            if (buildingMode)
            {
                CreatePreviewObject();
            }
        }
    }

    /// <summary>
    /// Переключиться к следующему типу комнаты
    /// </summary>
    public void CycleRoomType()
    {
        selectedRoomIndex = (selectedRoomIndex + 1) % availableRooms.Count;
        roomRotation = 0; // Сбрасываем поворот при смене типа комнаты
        if (buildingMode)
        {
            CreatePreviewObject();
        }
    }

    /// <summary>
    /// Попытка построить комнату
    /// </summary>
    void TryBuildRoom()
    {
        if (previewObject == null || playerCamera == null)
            return;

        // Используем ту же логику определения позиции, что и в UpdatePreview
        Vector3 mousePosition = Input.mousePosition;
        Ray ray = playerCamera.ScreenPointToRay(mousePosition);

        Vector3 worldPos;
        Vector2Int gridPos;

        // Попробуем raycast на землю/объекты
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            worldPos = hit.point;
            gridPos = gridManager.WorldToGrid(worldPos);
        }
        else
        {
            // Если raycast не попал, используем плоскость Y=0
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float distance))
            {
                worldPos = ray.GetPoint(distance);
                gridPos = gridManager.WorldToGrid(worldPos);
            }
            else
            {
                return; // Не можем определить позицию
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
    /// Проверка возможности размещения комнаты
    /// </summary>
    bool CanPlaceRoom(Vector2Int gridPosition, RoomData roomData, int rotation = 0)
    {
        Vector2Int rotatedSize = GetRotatedRoomSize(roomData.size, rotation);
        return gridManager.CanPlacePerimeterAt(gridPosition, rotatedSize.x, rotatedSize.y);
    }

    /// <summary>
    /// Построить комнату
    /// </summary>
    void BuildRoom(Vector2Int gridPosition, RoomData roomData, int rotation = 0)
    {
        GameObject room;
        Vector2Int rotatedSize = GetRotatedRoomSize(roomData.size, rotation);

        // Используем RoomBuilder для создания комнат из примитивов
        if (roomData.prefab == null)
        {
            room = RoomBuilder.Instance.BuildRoom(gridPosition, rotatedSize, roomData.roomName, rotation);
            room.name = $"{roomData.roomName}_{builtRooms.Count + 1}";

            // Добавляем информацию об объекте для системы выделения
            LocationObjectInfo objectInfo = room.GetComponent<LocationObjectInfo>();
            if (objectInfo == null)
            {
                objectInfo = room.AddComponent<LocationObjectInfo>();
            }
            objectInfo.objectName = roomData.roomName;
            objectInfo.objectType = roomData.roomType;
            objectInfo.health = 500f;
            objectInfo.isDestructible = true; // Теперь комнаты можно разрушать

            // Добавляем коллайдер для возможности выделения
            BoxCollider roomCollider = room.GetComponent<BoxCollider>();
            if (roomCollider == null)
            {
                roomCollider = room.AddComponent<BoxCollider>();
                // Настраиваем размер коллайдера на всю комнату с учетом поворота
                float width = rotatedSize.x * gridManager.cellSize;
                float height = rotatedSize.y * gridManager.cellSize;
                roomCollider.size = new Vector3(width, 2f, height);
                roomCollider.center = new Vector3(
                    (rotatedSize.x - 1) * 0.5f * gridManager.cellSize,
                    1f,
                    (rotatedSize.y - 1) * 0.5f * gridManager.cellSize
                );
                roomCollider.isTrigger = true; // Триггер чтобы не мешать движению
            }
        }
        else
        {
            // Старый способ с префабами (для совместимости)
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

        // Занимаем только периметр комнаты (стены), внутренние клетки остаются свободными
        gridManager.OccupyCellPerimeter(gridPosition, rotatedSize.x, rotatedSize.y, room, roomData.roomType);

        // Регистрируем стены как непроходимые
        RegisterRoomWalls(gridPosition, rotatedSize);

        // Добавляем в список построенных комнат
        builtRooms.Add(room);

        OnRoomBuilt?.Invoke(room);
    }

    /// <summary>
    /// Зарегистрировать стены комнаты как препятствия в GridManager
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

                // Проверяем, относится ли стена к нашей комнате
                if (IsWallPartOfRoom(wallGridPos, gridPosition, roomSize))
                {
                    // Занимаем клетку стены в GridManager
                    gridManager.OccupyCell(wallGridPos, wall, "Wall");
                }
            }
        }
    }

    /// <summary>
    /// Проверить, относится ли стена к данной комнате
    /// </summary>
    bool IsWallPartOfRoom(Vector2Int wallPos, Vector2Int roomPos, Vector2Int roomSize)
    {
        // Проверяем, находится ли стена в пределах комнаты
        bool inBoundsX = (wallPos.x >= roomPos.x && wallPos.x < roomPos.x + roomSize.x);
        bool inBoundsY = (wallPos.y >= roomPos.y && wallPos.y < roomPos.y + roomSize.y);

        if (!inBoundsX || !inBoundsY)
            return false;

        // Проверяем, является ли позиция частью периметра
        int relX = wallPos.x - roomPos.x;
        int relY = wallPos.y - roomPos.y;

        bool isPerimeter = (relX == 0 || relX == roomSize.x - 1 || relY == 0 || relY == roomSize.y - 1);

        return isPerimeter;
    }

    /// <summary>
    /// Освободить клетки стен комнаты в GridManager
    /// </summary>
    void FreeRoomWallCells(Vector2Int gridPosition, Vector2Int roomSize)
    {
        // Освобождаем клетки периметра комнаты (стены)
        for (int x = 0; x < roomSize.x; x++)
        {
            for (int y = 0; y < roomSize.y; y++)
            {
                Vector2Int cellPos = new Vector2Int(gridPosition.x + x, gridPosition.y + y);

                // Проверяем, является ли клетка частью периметра
                bool isPerimeter = (x == 0 || x == roomSize.x - 1 || y == 0 || y == roomSize.y - 1);

                if (isPerimeter)
                {
                    gridManager.FreeCell(cellPos);
                }
            }
        }
    }

    /// <summary>
    /// Получить текущий тип комнаты
    /// </summary>
    public RoomData GetCurrentRoomType()
    {
        if (availableRooms.Count > 0 && selectedRoomIndex < availableRooms.Count)
            return availableRooms[selectedRoomIndex];
        return null;
    }

    /// <summary>
    /// Проверить, активен ли режим строительства
    /// </summary>
    public bool IsBuildingModeActive()
    {
        return buildingMode;
    }

    /// <summary>
    /// Проверить, был ли ролик мыши использован для поворота в этом кадре
    /// </summary>
    public bool IsScrollWheelUsedThisFrame()
    {
        return scrollWheelUsedThisFrame;
    }

    /// <summary>
    /// Получить список построенных комнат
    /// </summary>
    public List<GameObject> GetBuiltRooms()
    {
        return new List<GameObject>(builtRooms);
    }

    /// <summary>
    /// Включить/выключить режим удаления
    /// </summary>
    public void ToggleDeletionMode()
    {
        deletionMode = !deletionMode;
        FileLogger.Log($"Deletion mode toggled: {deletionMode}");

        if (deletionMode)
        {
            // Выключаем режим строительства если он был активен
            if (buildingMode)
            {
                buildingMode = false;
                StopBuildingMode();
                OnBuildingModeChanged?.Invoke();
            }
        }
        else
        {
            // При выходе из режима разрушения убираем подсветку
            if (highlightedRoom != null)
            {
                RemoveRoomHighlight(highlightedRoom);
                highlightedRoom = null;
            }
        }

        OnDeletionModeChanged?.Invoke();
    }

    /// <summary>
    /// Проверить, активен ли режим удаления
    /// </summary>
    public bool IsDeletionModeActive()
    {
        return deletionMode;
    }

    /// <summary>
    /// Обновление подсветки комнат в режиме разрушения
    /// </summary>
    void UpdateDeletionHighlight()
    {
        Vector3 mousePosition = Input.mousePosition;
        Ray ray = playerCamera.ScreenPointToRay(mousePosition);

        GameObject newHighlightedRoom = null;

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            // Проверяем, является ли объект комнатой
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

        // Убираем старую подсветку
        if (highlightedRoom != null && highlightedRoom != newHighlightedRoom)
        {
            RemoveRoomHighlight(highlightedRoom);
        }

        // Добавляем новую подсветку
        if (newHighlightedRoom != null && newHighlightedRoom != highlightedRoom)
        {
            AddRoomHighlight(newHighlightedRoom);
        }

        highlightedRoom = newHighlightedRoom;
    }

    /// <summary>
    /// Добавить красную подсветку комнате
    /// </summary>
    void AddRoomHighlight(GameObject room)
    {
        Renderer[] renderers = room.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                // Сохраняем оригинальный материал если еще не сохранен
                if (originalMaterial == null)
                {
                    originalMaterial = renderer.material;
                }

                // Создаем красный материал для подсветки
                Material highlightMaterial = new Material(renderer.material);
                highlightMaterial.color = Color.red;
                renderer.material = highlightMaterial;
            }
        }
    }

    /// <summary>
    /// Убрать подсветку с комнаты
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
    /// Обработка ввода в режиме удаления
    /// </summary>
    void HandleDeletionInput()
    {
        // Проверяем, не находится ли мышь над UI элементом
        bool isPointerOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

        // ЛКМ - удалить комнату (только если мышь НЕ над UI)
        if (Input.GetMouseButtonDown(0) && !isPointerOverUI)
        {
            TryDeleteRoom();
        }

        // ПКМ или ESC - выйти из режима удаления (только если мышь НЕ над UI для ПКМ)
        if ((Input.GetMouseButtonDown(1) && !isPointerOverUI) || Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleDeletionMode();
        }
    }

    /// <summary>
    /// Попытка удалить комнату
    /// </summary>
    void TryDeleteRoom()
    {
        Vector3 mousePosition = Input.mousePosition;
        Ray ray = playerCamera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            // Проверяем, является ли объект комнатой
            GameObject hitObject = hit.collider.gameObject;

            // Проверяем родительские объекты на наличие RoomInfo
            RoomInfo roomInfo = hitObject.GetComponentInParent<RoomInfo>();
            if (roomInfo != null)
            {
                DeleteRoom(roomInfo.gameObject);
            }
        }
    }

    /// <summary>
    /// Удалить комнату
    /// </summary>
    public void DeleteRoom(GameObject room)
    {
        RoomInfo roomInfo = room.GetComponent<RoomInfo>();
        if (roomInfo == null) return;

        // Получаем размер с учетом поворота комнаты
        Vector2Int rotatedSize = GetRotatedRoomSize(roomInfo.roomSize, roomInfo.roomRotation);

        // Освобождаем клетки стен в GridManager
        FreeRoomWallCells(roomInfo.gridPosition, rotatedSize);

        // Освобождаем только периметр комнаты (стены) в GridManager
        gridManager.FreeCellPerimeter(roomInfo.gridPosition, rotatedSize.x, rotatedSize.y);

        // Удаляем стены через RoomBuilder
        RoomBuilder.Instance.RemoveRoom(roomInfo.gridPosition, rotatedSize);

        // Удаляем из списка построенных комнат
        if (builtRooms.Contains(room))
        {
            builtRooms.Remove(room);
        }

        // Уведомляем о удалении
        OnRoomDeleted?.Invoke(room);

        // Уничтожаем объект
        DestroyImmediate(room);
    }

    /// <summary>
    /// Обновить индикаторы клеток для предварительного просмотра
    /// </summary>
    void UpdatePreviewCells(Vector2Int gridPos, RoomData roomData)
    {
        // Очищаем старые индикаторы
        ClearPreviewCells();

        // Создаем новые индикаторы для каждой клетки комнаты
        for (int x = 0; x < roomData.size.x; x++)
        {
            for (int y = 0; y < roomData.size.y; y++)
            {
                Vector2Int cellPos = new Vector2Int(gridPos.x + x, gridPos.y + y);

                // Проверяем, свободна ли клетка
                bool isValidPosition = gridManager.IsValidGridPosition(cellPos);
                bool isCellFree = isValidPosition && gridManager.IsCellFree(cellPos);

                // Дополнительная проверка - если позиция за пределами сетки, считаем недоступной
                if (!isValidPosition)
                {
                    isCellFree = false;
                }


                // Создаем визуальный индикатор
                GameObject cellIndicator = CreateCellIndicator(cellPos, isCellFree);
                previewCells.Add(cellIndicator);
            }
        }
    }

    /// <summary>
    /// Создать индикатор для одной клетки
    /// </summary>
    GameObject CreateCellIndicator(Vector2Int gridPos, bool isAvailable)
    {
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
        indicator.name = $"CellIndicator_{gridPos.x}_{gridPos.y}";

        // Позиционируем индикатор
        Vector3 worldPos = gridManager.GridToWorld(gridPos);
        worldPos.y = 0.5f; // Еще выше для лучшей видимости
        indicator.transform.position = worldPos;

        // Размер индикатора - покрывает большую часть клетки
        float indicatorSize = gridManager.cellSize * 0.9f;
        indicator.transform.localScale = new Vector3(indicatorSize, 0.5f, indicatorSize);

        // Убираем коллайдер
        Collider collider = indicator.GetComponent<Collider>();
        if (collider != null)
            DestroyImmediate(collider);

        // Настраиваем материал
        Renderer renderer = indicator.GetComponent<Renderer>();

        // Попробуем Legacy шейдеры которые точно есть в Unity
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
            // Очень яркий зеленый для доступных клеток
            mat.color = new Color(0f, 1f, 0f, 1f);
        }
        else
        {
            // Очень яркий красный для недоступных клеток
            mat.color = new Color(1f, 0f, 0f, 1f);
        }

        renderer.material = mat;

        return indicator;
    }

    /// <summary>
    /// Очистить все индикаторы клеток
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
    /// Получить список доступных комнат для UI
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
    /// Получить текущий выбранный индекс комнаты
    /// </summary>
    public int GetSelectedRoomIndex()
    {
        return selectedRoomIndex;
    }
}