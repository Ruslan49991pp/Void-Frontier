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

    // Внутренние переменные
    private bool buildingMode = false;
    private bool deletionMode = false;
    private int selectedRoomIndex = 0;
    private GameObject previewObject;
    private List<GameObject> previewCells = new List<GameObject>();
    private GameUI gameUI;
    private List<GameObject> builtRooms = new List<GameObject>();
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
            StartBuildingMode();
            FileLogger.Log("Build mode activated via UI");
        }
        else
        {
            StopBuildingMode();
            FileLogger.Log("Build mode deactivated via UI");
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

        ClearPreviewCells();
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

        // ЛКМ - построить комнату (только если мышь НЕ над UI)
        if (Input.GetMouseButtonDown(0) && !isPointerOverUI)
        {
            TryBuildRoom();
        }

        // ПКМ - отменить выбор комнаты или выйти из режима строительства (только если мышь НЕ над UI)
        if (Input.GetMouseButtonDown(1) && !isPointerOverUI)
        {
            if (selectedRoomIndex >= 0)
            {
                // Если комната выбрана, отменяем выбор
                ClearRoomSelection();
            }
            else
            {
                // Если комната не выбрана, выходим из режима строительства
                SetBuildMode(false);
            }
        }

        // ESC - выйти из режима строительства
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetBuildMode(false);
        }

        // Q - поворот против часовой стрелки
        if (Input.GetKeyDown(KeyCode.Q))
        {
            RotateRoom(-90);
        }

        // E - поворот по часовой стрелке
        if (Input.GetKeyDown(KeyCode.E))
        {
            RotateRoom(90);
        }

        // Ролик мышки - поворот
        float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollWheel) > 0.0001f)
        {
            if (scrollWheel > 0f)
            {
                RotateRoom(90); // Вверх - по часовой стрелке
            }
            else if (scrollWheel < 0f)
            {
                RotateRoom(-90); // Вниз - против часовой стрелки
            }

            // Помечаем что ввод ролика обработан в этом кадре
            scrollWheelUsedThisFrame = true;
            FileLogger.Log($"Scroll wheel used for room rotation: {scrollWheel}");
        }

        // Клавиши 1,2,3 и Tab больше не используются - выбор идет через UI
    }

    /// <summary>
    /// Повернуть комнату на заданный угол
    /// </summary>
    void RotateRoom(int degrees)
    {
        roomRotation = (roomRotation + degrees) % 360;
        if (roomRotation < 0) roomRotation += 360;

        // Пересоздаем предпросмотр с новым поворотом
        if (buildingMode && previewObject != null)
        {
            CreatePreviewObject();
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
        return gridManager.CanPlaceObjectAt(gridPosition, rotatedSize.x, rotatedSize.y);
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

        // Занимаем клетки в сетке с учетом поворота
        gridManager.OccupyCellArea(gridPosition, rotatedSize.x, rotatedSize.y, room, roomData.roomType);

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

        // Освобождаем клетки стен в GridManager
        FreeRoomWallCells(roomInfo.gridPosition, roomInfo.roomSize);

        // Освобождаем клетки в GridManager
        gridManager.FreeCellArea(roomInfo.gridPosition, roomInfo.roomSize.x, roomInfo.roomSize.y);

        // Удаляем стены через RoomBuilder
        RoomBuilder.Instance.RemoveRoom(roomInfo.gridPosition, roomInfo.roomSize);

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

    /// <summary>
    /// Получить текущий выбранный индекс комнаты
    /// </summary>
    public int GetSelectedRoomIndex()
    {
        return selectedRoomIndex;
    }

}