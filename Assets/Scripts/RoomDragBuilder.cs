using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Система drag-and-drop для рисования комнат
/// 1. Зажать ЛКМ и тянуть - рисовать рамку из Build_Ghost
/// 2. Отпустить ЛКМ - зафиксировать силуэт
/// 3. AddBuild - подтвердить (Build_Ghost → Add_Build_Ghost)
/// 4. При выходе из режима строительства - Add_Build_Ghost → SM_Wall/SM_Wall_L
/// </summary>
public class RoomDragBuilder : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject buildGhostPrefab;      // Build_Ghost префаб (стены)
    public GameObject addBuildGhostPrefab;  // Add_Build_Ghost префаб (стены)
    public GameObject floorGhostPrefab;      // Floor_Ghost префаб (пол)
    public GameObject addFloorGhostPrefab;  // Add_Floor_Ghost префаб (пол)
    public GameObject delBuildGhostPrefab;   // Del_Build_Ghost префаб (удаление)

    [Header("References")]
    public GridManager gridManager;
    public Camera playerCamera;

    // Состояния
    private enum DragState
    {
        Idle,           // Ожидание начала drag
        Dragging,       // Процесс drag
        PreviewReady,   // Силуэт готов, ожидание подтверждения
        Confirmed       // Подтверждено, ожидание выхода из режима строительства
    }

    private DragState currentState = DragState.Idle;

    // Drag данные
    private Vector2Int dragStartGridPos;
    private Vector2Int dragEndGridPos;
    private List<GameObject> ghostBlocks = new List<GameObject>();      // Build_Ghost блоки (стены)
    private List<GameObject> ghostFloorBlocks = new List<GameObject>(); // Floor_Ghost блоки (пол)
    private List<GameObject> confirmedBlocks = new List<GameObject>(); // Add_Build_Ghost блоки (стены)
    private List<GameObject> confirmedFloorBlocks = new List<GameObject>(); // Add_Floor_Ghost блоки (пол)
    private List<Vector2Int> roomPerimeter = new List<Vector2Int>();  // Позиции периметра
    private List<Vector2Int> roomFloor = new List<Vector2Int>();      // Позиции пола

    // Кэш для инкрементального обновления
    private HashSet<Vector2Int> cachedPerimeter = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> cachedFloor = new HashSet<Vector2Int>();
    private Dictionary<Vector2Int, GameObject> activeGhostBlocks = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, GameObject> activeFloorBlocks = new Dictionary<Vector2Int, GameObject>();

    // Режим активен
    private bool isDragModeActive = false;

    // Режим добавления клеток к существующему силуэту
    private bool isAddingMoreCells = false;
    private List<Vector2Int> existingPerimeter = new List<Vector2Int>(); // Сохраненный периметр до начала добавления
    private List<Vector2Int> existingFloor = new List<Vector2Int>();     // Сохраненный пол до начала добавления

    // Режим удаления
    private bool isDeletionModeActive = false;
    private List<Vector2Int> deletedCells = new List<Vector2Int>();   // Удаленные клетки
    private List<GameObject> delGhostBlocks = new List<GameObject>(); // Del_Build_Ghost блоки
    private GameObject delPreviewBlock = null;                         // Preview блок при наведении (deletion)
    private Vector2Int lastDelPreviewPos = Vector2Int.zero;           // Последняя позиция preview (deletion)

    // Cursor preview для build mode
    private GameObject buildCursorPreview = null;                      // Preview блок под курсором (build)
    private Vector2Int lastBuildPreviewPos = Vector2Int.zero;         // Последняя позиция preview (build)

    // Сохраненные внутренние углы
    private List<Vector2Int> savedInnerCorners = new List<Vector2Int>(); // Внутренние углы найденные в RecalculatePerimeter

    // Drag удаление
    private bool isDeletionDragActive = false;                        // Активен ли drag режим удаления
    private Vector2Int deletionDragStart = Vector2Int.zero;           // Начальная позиция drag
    private Vector2Int deletionDragEnd = Vector2Int.zero;             // Конечная позиция drag
    private List<GameObject> delDragPreviewBlocks = new List<GameObject>(); // Preview блоки для drag

    // Кэш для drag удаления
    private HashSet<Vector2Int> cachedDelDragPositions = new HashSet<Vector2Int>();
    private Dictionary<Vector2Int, GameObject> activeDelDragBlocks = new Dictionary<Vector2Int, GameObject>();

    // Флаг детального логирования (ВКЛЮЧЕН для отладки)
    private const bool DEBUG_LOGGING = true;

    void Start()
    {
        // Загружаем префабы если не назначены
        if (buildGhostPrefab == null)
        {
            buildGhostPrefab = Resources.Load<GameObject>("Prefabs/Build_Ghost");
            if (buildGhostPrefab == null)
            {
                Debug.LogError("[RoomDragBuilder] Build_Ghost prefab not found in Resources/Prefabs/");
            }
        }

        if (addBuildGhostPrefab == null)
        {
            addBuildGhostPrefab = Resources.Load<GameObject>("Prefabs/Add_Build_Ghost");
            if (addBuildGhostPrefab == null)
            {
                Debug.LogError("[RoomDragBuilder] Add_Build_Ghost prefab not found in Resources/Prefabs/");
            }
        }

        if (floorGhostPrefab == null)
        {
            floorGhostPrefab = Resources.Load<GameObject>("Prefabs/Floor_Ghost");
            if (floorGhostPrefab == null)
            {
                Debug.LogError("[RoomDragBuilder] Floor_Ghost prefab not found in Resources/Prefabs/");
            }
        }

        if (addFloorGhostPrefab == null)
        {
            addFloorGhostPrefab = Resources.Load<GameObject>("Prefabs/Add_Floor_Ghost");
            if (addFloorGhostPrefab == null)
            {
                Debug.LogError("[RoomDragBuilder] Add_Floor_Ghost prefab not found in Resources/Prefabs/");
            }
        }

        if (delBuildGhostPrefab == null)
        {
            delBuildGhostPrefab = Resources.Load<GameObject>("Prefabs/Del_Build_Ghost");
            if (delBuildGhostPrefab == null)
            {
                Debug.LogError("[RoomDragBuilder] Del_Build_Ghost prefab not found in Resources/Prefabs/");
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
        // ВАЖНО: Update работает если активен drag режим ИЛИ deletion режим
        if (!isDragModeActive && !isDeletionModeActive) return;

        // Проверяем, не над UI ли мышь
        bool isPointerOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

        if (isPointerOverUI) return;

        // ВАЖНО: Проверяем режим deletion ПЕРЕД обработкой состояний
        // Если deletion mode активен - обрабатываем ТОЛЬКО deletion, игнорируем build логику
        if (isDeletionModeActive)
        {
            HandleDeletionMode();
        }
        else
        {
            // Build режим - обрабатываем состояния обычным образом
            switch (currentState)
            {
                case DragState.Idle:
                    HandleIdleState();
                    break;

                case DragState.Dragging:
                    HandleDraggingState();
                    break;

                case DragState.PreviewReady:
                    // В этом состоянии можно начать новый drag (для добавления клеток)
                    // Обрабатываем клики так же как в Idle состоянии
                    if (isDragModeActive)
                    {
                        // КРИТИЧНО: При клике в PreviewReady нужно активировать режим добавления клеток!
                        if (Input.GetMouseButtonDown(0) && !isAddingMoreCells)
                        {
                            // Активируем режим добавления и сохраняем существующие клетки
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
                    // В этом состоянии также можно начать новый drag (для добавления клеток)
                    if (isDragModeActive)
                    {
                        // КРИТИЧНО: При клике в Confirmed нужно активировать режим добавления клеток!
                        if (Input.GetMouseButtonDown(0) && !isAddingMoreCells)
                        {
                            // Активируем режим добавления и сохраняем существующие клетки
                            isAddingMoreCells = true;
                            existingPerimeter.Clear();
                            existingPerimeter.AddRange(roomPerimeter);
                            existingFloor.Clear();
                            existingFloor.AddRange(roomFloor);

                            if (DEBUG_LOGGING)
                            {
                            }

                            // Меняем состояние на Idle чтобы начать новый drag
                            currentState = DragState.Idle;
                        }

                        HandleIdleState();
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Активировать режим drag строительства
    /// После активации можно рисовать НЕОГРАНИЧЕННОЕ количество прямоугольников подряд без повторного нажатия кнопки
    /// Каждый новый прямоугольник добавляется к существующему силуэту
    /// </summary>
    public void ActivateDragMode()
    {
        if (DEBUG_LOGGING)
        {
        }

        isDragModeActive = true;

        // Запоминаем, был ли активен deletion mode ДО деактивации
        bool wasInDeletionMode = isDeletionModeActive;

        // Деактивируем deletion режим если он был активен
        if (isDeletionModeActive)
        {
            if (DEBUG_LOGGING)
            {
            }
            DeactivateDeletionMode();
        }

        // Проверяем, есть ли уже построенный силуэт
        // ВАЖНО: Проверяем не только currentState, но и наличие данных!
        // Потому что при повторном клике на BuildSlot state может быть Idle, но данные есть
        bool hasExistingRoom = (currentState == DragState.PreviewReady || currentState == DragState.Confirmed) ||
                                (roomPerimeter.Count > 0 || roomFloor.Count > 0);

        if (hasExistingRoom)
        {
            // Режим добавления клеток к существующему силуэту
            isAddingMoreCells = true;

            // Сохраняем существующий периметр и пол
            existingPerimeter.Clear();
            existingPerimeter.AddRange(roomPerimeter);
            existingFloor.Clear();
            existingFloor.AddRange(roomFloor);

            if (DEBUG_LOGGING)
            {
            }

            // КРИТИЧНО: Очищаем старые ghost блоки ТОЛЬКО если переключаемся с deletion mode ИЛИ из Confirmed state
            // При удалении были созданы новые ghost блоки для периметра
            // Если просто нажали BuildSlot второй раз - НЕ очищаем, ghost блоки должны остаться!
            // ВАЖНО: Если в Confirmed state, нужно очистить confirmedBlocks чтобы они могли быть перестроены!
            if (wasInDeletionMode || currentState == DragState.Confirmed)
            {
                if (DEBUG_LOGGING)
                {
                }

                // Возвращаем все активные ghost блоки в pool
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

                // КРИТИЧНО: Также очищаем confirmed блоки если они есть
                // Они будут заново созданы после merge с правильной классификацией (стена/пол)
                // ВАЖНО: Используем DestroyImmediate() чтобы удалить блоки СРАЗУ, иначе они будут дублироваться!
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

                // КРИТИЧНО: Также очищаем deletion preview блоки
                // Эти блоки могут остаться если пользователь делал удаление перед добавлением
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

                    // Дополнительная проверка - считаем сколько ghost блоков осталось на сцене
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

                // КРИТИЧНО: После очистки нужно заново создать ghost блоки для существующего периметра!
                // Иначе комната исчезнет до начала drag'а
                if (DEBUG_LOGGING)
                {
                }

                // Создаем ghost блоки для периметра
                foreach (Vector2Int pos in roomPerimeter)
                {
                    GameObject ghostBlock = CreateGhostBlockPooled(pos, buildGhostPrefab);
                    activeGhostBlocks[pos] = ghostBlock;
                    ghostBlocks.Add(ghostBlock);
                    cachedPerimeter.Add(pos);
                }

                // Создаем ghost блоки для пола
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
            // Первоначальное строительство - начинаем с нуля
            isAddingMoreCells = false;
            if (DEBUG_LOGGING)
            {
            }
        }

        currentState = DragState.Idle;
    }

    /// <summary>
    /// Деактивировать режим drag строительства
    /// ВАЖНО: Если мы в состоянии PreviewReady или Confirmed - НЕ очищаем клетки!
    /// Очищаем только если в состоянии Idle или Dragging (незавершенный drag)
    /// </summary>
    public void DeactivateDragMode()
    {

        isDragModeActive = false;
        isAddingMoreCells = false; // Сбрасываем флаг добавления

        // Очищаем build cursor preview
        if (buildCursorPreview != null)
        {
            GhostBlockPool.Instance.Return(buildCursorPreview);
            buildCursorPreview = null;
        }

        // ВАЖНО: Очищаем данные ТОЛЬКО если:
        // 1. Drag не был завершен (Idle или Dragging state)
        // 2. И данных комнаты НЕТ (roomPerimeter и roomFloor пусты)
        // Это защищает от случайного удаления данных при переключении инструментов
        bool hasRoomData = roomPerimeter.Count > 0 || roomFloor.Count > 0;
        bool isIncompleteDrag = currentState == DragState.Idle || currentState == DragState.Dragging;


        if (isIncompleteDrag && !hasRoomData)
        {
            // Незавершенный drag БЕЗ данных - очищаем все
            ClearGhostBlocks();
            ClearDelPreview();
            ClearDelDragPreview();
            roomPerimeter.Clear();
            roomFloor.Clear();
            currentState = DragState.Idle;
        }
        else
        {
            // PreviewReady, Confirmed ИЛИ есть данные - оставляем клетки на месте
        }

    }

    /// <summary>
    /// Активировать режим удаления
    /// </summary>
    public void ActivateDeletionMode()
    {

        isDeletionModeActive = true;

    }

    /// <summary>
    /// Деактивировать режим удаления
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
    /// Обработка состояния Idle - ожидание начала drag
    /// </summary>
    void HandleIdleState()
    {
        // Показываем cursor preview под мышкой (только для build mode, для deletion есть отдельная логика)
        if (isDragModeActive && !isDeletionModeActive)
        {
            Vector2Int currentGridPos = GetGridPositionFromMouse();

            if (currentGridPos != Vector2Int.zero && currentGridPos != lastBuildPreviewPos)
            {
                // Удаляем старый preview
                if (buildCursorPreview != null)
                {
                    GhostBlockPool.Instance.Return(buildCursorPreview);
                    buildCursorPreview = null;
                }

                // Создаем новый preview под курсором
                buildCursorPreview = CreateGhostBlockPooled(currentGridPos, buildGhostPrefab);
                lastBuildPreviewPos = currentGridPos;
            }
        }

        if (Input.GetMouseButtonDown(0))
        {

            // Удаляем cursor preview при начале drag
            if (buildCursorPreview != null)
            {
                GhostBlockPool.Instance.Return(buildCursorPreview);
                buildCursorPreview = null;
            }

            // Начинаем drag
            Vector2Int gridPos = GetGridPositionFromMouse();

            if (gridPos != Vector2Int.zero)
            {
                dragStartGridPos = gridPos;
                dragEndGridPos = gridPos;
                currentState = DragState.Dragging;
            }
            else
            {
                Debug.LogWarning("[RoomDragBuilder] Grid position is zero, cannot start drag");
            }
        }
    }

    /// <summary>
    /// Обработка состояния Dragging - процесс рисования рамки
    /// </summary>
    void HandleDraggingState()
    {
        if (Input.GetMouseButton(0))
        {
            // Обновляем конечную позицию
            Vector2Int currentGridPos = GetGridPositionFromMouse();
            if (currentGridPos != dragEndGridPos)
            {
                dragEndGridPos = currentGridPos;
                UpdateDragPreview();
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            // Закончили drag - фиксируем силуэт
            if (DEBUG_LOGGING)
            {
            }

            // Если это режим добавления клеток - объединяем с существующими
            if (isAddingMoreCells)
            {
                MergeNewCellsWithExisting();
                isAddingMoreCells = false; // Сбрасываем флаг после merge
            }

            currentState = DragState.PreviewReady;
        }
    }

    /// <summary>
    /// Обновить превью во время drag (ОПТИМИЗИРОВАНО - инкрементальное обновление)
    /// </summary>
    void UpdateDragPreview()
    {
        if (DEBUG_LOGGING && isAddingMoreCells)
        {
        }

        // Вычисляем прямоугольник
        int minX = Mathf.Min(dragStartGridPos.x, dragEndGridPos.x);
        int maxX = Mathf.Max(dragStartGridPos.x, dragEndGridPos.x);
        int minY = Mathf.Min(dragStartGridPos.y, dragEndGridPos.y);
        int maxY = Mathf.Max(dragStartGridPos.y, dragEndGridPos.y);

        // Новые множества позиций для НОВОГО нарисованного прямоугольника
        HashSet<Vector2Int> newPerimeter = new HashSet<Vector2Int>();
        HashSet<Vector2Int> newFloor = new HashSet<Vector2Int>();

        // ВАЖНО: Если режим добавления - сначала добавляем существующие клетки!
        if (isAddingMoreCells)
        {
            // Добавляем существующие клетки в новые множества
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

        // Обновляем списки периметра и пола
        roomPerimeter.Clear();
        roomFloor.Clear();

        // Добавляем НОВЫЙ нарисованный прямоугольник
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int cellPos = new Vector2Int(x, y);

                // Проверяем, является ли клетка частью периметра
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

        // ВАЖНО: Теперь копируем ВСЕ клетки из newPerimeter/newFloor в roomPerimeter/roomFloor
        // Это включает как существующие клетки (если isAddingMoreCells), так и новые
        roomPerimeter.AddRange(newPerimeter);
        roomFloor.AddRange(newFloor);

        if (DEBUG_LOGGING && isAddingMoreCells)
        {
        }

        // ИНКРЕМЕНТАЛЬНОЕ ОБНОВЛЕНИЕ: Удаляем блоки которых больше нет
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

        // ИНКРЕМЕНТАЛЬНОЕ ОБНОВЛЕНИЕ: Добавляем новые блоки
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
    /// Объединить новые нарисованные клетки с существующими
    /// Пересчитывает периметр и пол для объединенной фигуры
    /// </summary>
    void MergeNewCellsWithExisting()
    {
        if (DEBUG_LOGGING)
        {
        }

        // Объединяем все клетки (существующие + новые)
        HashSet<Vector2Int> allCells = new HashSet<Vector2Int>();

        // Добавляем существующие клетки
        foreach (Vector2Int pos in existingPerimeter)
        {
            allCells.Add(pos);
        }
        foreach (Vector2Int pos in existingFloor)
        {
            allCells.Add(pos);
        }

        // Добавляем новые клетки
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

        // Теперь пересчитываем периметр и пол для объединенной фигуры
        List<Vector2Int> newPerimeter = new List<Vector2Int>();
        List<Vector2Int> newFloor = new List<Vector2Int>();

        foreach (Vector2Int pos in allCells)
        {
            // Проверяем, была ли эта клетка частью старого периметра (из удаления)
            bool wasOldPerimeterWall = existingPerimeter.Contains(pos);

            // Клетка - периметр, если хотя бы один ортогональный сосед пустой
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

            // DEBUG: Логируем клетки старого периметра, которые теперь внутри
            if (DEBUG_LOGGING && wasOldPerimeterWall && !hasEmptyNeighbor)
            {
            }

            // КРИТИЧНО: Дополнительная проверка на внутренние углы
            // ВАЖНО: Эта проверка работает ТОЛЬКО для режима объединения БЕЗ удалений!
            // Когда есть удаления (deletedCells.Count > 0), внутренние углы ищутся через FindInnerCorners()
            if (!hasEmptyNeighbor && deletedCells.Count == 0)
            {
                // Проверяем каждую диагональ: если диагональ пустая И оба ортогональных соседа заняты - это внутренний угол
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

                // Проверка 1: TopLeft диагональ пустая, но Up и Left заняты -> внутренний угол
                if (!allCells.Contains(topLeft) && hasUp && hasLeft)
                {
                    hasEmptyNeighbor = true;
                }
                // Проверка 2: TopRight диагональ пустая, но Up и Right заняты -> внутренний угол
                else if (!allCells.Contains(topRight) && hasUp && hasRight)
                {
                    hasEmptyNeighbor = true;
                }
                // Проверка 3: BottomLeft диагональ пустая, но Down и Left заняты -> внутренний угол
                else if (!allCells.Contains(bottomLeft) && hasDown && hasLeft)
                {
                    hasEmptyNeighbor = true;
                }
                // Проверка 4: BottomRight диагональ пустая, но Down и Right заняты -> внутренний угол
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

        // Обновляем списки
        roomPerimeter.Clear();
        roomPerimeter.AddRange(newPerimeter);
        roomFloor.Clear();
        roomFloor.AddRange(newFloor);

        if (DEBUG_LOGGING)
        {
        }

        // ВАЖНО: Вызываем RecalculatePerimeter() для финальной обработки
        // Это операция ДОБАВЛЕНИЯ (не удаления), поэтому isDeletion=false
        RecalculatePerimeter(isDeletion: false);

        if (DEBUG_LOGGING)
        {
        }

        // Обновляем ghost блоки для отображения объединенной фигуры
        UpdateGhostsAfterMerge();
    }

    /// <summary>
    /// Обновить ghost блоки после объединения клеток
    /// </summary>
    void UpdateGhostsAfterMerge()
    {
        if (DEBUG_LOGGING)
        {
        }

        // Очищаем текущие ghost блоки
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

        // Создаем ghost блоки для нового периметра
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

        // Создаем ghost блоки для нового пола
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

            // Проверяем сколько АКТИВНЫХ ghost блоков на сцене после создания
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
    /// Создать призрачный блок в указанной позиции (УСТАРЕВШИЙ - для совместимости)
    /// </summary>
    GameObject CreateGhostBlock(Vector2Int gridPos, GameObject prefab)
    {
        Vector3 worldPos = gridManager.GridToWorld(gridPos);
        GameObject block = Instantiate(prefab, worldPos, Quaternion.identity);
        block.name = $"GhostBlock_{gridPos.x}_{gridPos.y}";

        // Убираем коллайдеры
        Collider[] colliders = block.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            Destroy(col);
        }

        return block;
    }

    /// <summary>
    /// Создать призрачный блок используя Object Pool (ОПТИМИЗИРОВАНО)
    /// </summary>
    GameObject CreateGhostBlockPooled(Vector2Int gridPos, GameObject prefab)
    {
        Vector3 worldPos = gridManager.GridToWorld(gridPos);
        GameObject block = GhostBlockPool.Instance.Get(prefab, worldPos, Quaternion.identity);

        if (block != null)
        {
            block.name = $"GhostBlock_{gridPos.x}_{gridPos.y}";

            // Убираем коллайдеры при первом создании
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
    /// Подтвердить постройку - заменить Build_Ghost на Add_Build_Ghost
    /// </summary>
    public void ConfirmBuild()
    {
        // ВАЖНО: Проверяем не state, а наличие данных!
        // Можно подтверждать в любом состоянии если есть данные
        if (!HasRoomData())
        {
            Debug.LogWarning("[RoomDragBuilder] Cannot confirm - no room data available");
            return;
        }


        // Заменяем все Build_Ghost (стены) на Add_Build_Ghost
        foreach (Vector2Int cellPos in roomPerimeter)
        {
            GameObject confirmedBlock = CreateGhostBlock(cellPos, addBuildGhostPrefab);
            confirmedBlocks.Add(confirmedBlock);
        }

        // Заменяем все Floor_Ghost (пол) на Add_Floor_Ghost
        if (addFloorGhostPrefab != null)
        {
            foreach (Vector2Int cellPos in roomFloor)
            {
                GameObject confirmedFloorBlock = CreateGhostBlock(cellPos, addFloorGhostPrefab);
                confirmedFloorBlocks.Add(confirmedFloorBlock);
            }
        }

        // Удаляем Build_Ghost и Floor_Ghost блоки
        ClearGhostBlocks();

        currentState = DragState.Confirmed;
    }

    /// <summary>
    /// Финализировать постройку - создать настоящие стены
    /// Теперь работает СРАЗУ, минуя Confirmed state (green ghost)
    /// </summary>
    public void FinalizeBuild()
    {
        // ВАЖНО: Проверяем не state, а наличие данных!
        if (!HasRoomData())
        {
            Debug.LogWarning("[RoomDragBuilder] Cannot finalize - no room data available");
            return;
        }


        // Вычисляем границы комнаты для создания GameObject
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (Vector2Int pos in roomPerimeter)
        {
            minX = Mathf.Min(minX, pos.x);
            maxX = Mathf.Max(maxX, pos.x);
            minY = Mathf.Min(minY, pos.y);
            maxY = Mathf.Max(maxY, pos.y);
        }

        // Также учитываем клетки пола
        foreach (Vector2Int pos in roomFloor)
        {
            minX = Mathf.Min(minX, pos.x);
            maxX = Mathf.Max(maxX, pos.x);
            minY = Mathf.Min(minY, pos.y);
            maxY = Mathf.Max(maxY, pos.y);
        }

        Vector2Int roomGridPos = new Vector2Int(minX, minY);
        Vector2Int roomSize = new Vector2Int(maxX - minX + 1, maxY - minY + 1);

        // Используем сохраненные внутренние углы из RecalculatePerimeter
        foreach (Vector2Int corner in savedInnerCorners)
        {
        }

        // Создаем комнату с кастомным силуэтом, передавая внутренние углы
        GameObject room = RoomBuilder.Instance.BuildCustomRoom(roomGridPos, roomSize, "DraggedRoom", roomPerimeter, roomFloor, savedInnerCorners);
        room.name = $"DraggedRoom_{roomGridPos.x}_{roomGridPos.y}";


        // Регистрируем в GridManager
        gridManager.OccupyCellPerimeter(roomGridPos, roomSize.x, roomSize.y, room, "Room");

        // Очищаем Add_Build_Ghost и Add_Floor_Ghost блоки
        ClearConfirmedBlocks();

        // КРИТИЧНО: Также очищаем activeGhostBlocks и activeFloorBlocks
        // Это ghost блоки из режима preview, которые могли остаться после удаления
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

        // Сбрасываем состояние
        currentState = DragState.Idle;
        roomPerimeter.Clear();
        roomFloor.Clear();
        deletedCells.Clear();
        savedInnerCorners.Clear();

    }

    /// <summary>
    /// Получить позицию сетки из позиции мыши
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
            // Если raycast не попал, используем плоскость Y=0
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
    /// Очистить Build_Ghost блоки (стены и пол)
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
    /// Очистить Add_Build_Ghost блоки (стены и пол)
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
    /// Очистить все призраки
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
    /// Обработка режима удаления - preview и drag удаление
    /// </summary>
    void HandleDeletionMode()
    {
        Vector2Int currentGridPos = GetGridPositionFromMouse();

        if (!isDeletionDragActive)
        {
            // Обновляем preview при движении мыши
            UpdateDeletionPreview(currentGridPos);

            // Начинаем drag удаление при зажатии ЛКМ (можно начать из любого места)
            if (Input.GetMouseButtonDown(0))
            {
                if (currentGridPos != Vector2Int.zero)
                {
                    isDeletionDragActive = true;
                    deletionDragStart = currentGridPos;
                    deletionDragEnd = currentGridPos;
                    ClearDelPreview(); // Убираем обычный preview
                    if (DEBUG_LOGGING)
                    {
                    }
                }
            }
        }
        else
        {
            // Обновляем drag область
            if (Input.GetMouseButton(0))
            {
                if (currentGridPos != deletionDragEnd)
                {
                    deletionDragEnd = currentGridPos;
                    UpdateDeletionDragPreview();
                }
            }
            // Завершаем drag удаление
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
    /// Проверить, является ли клетка частью комнаты
    /// </summary>
    bool IsCellPartOfRoom(Vector2Int cellPos)
    {
        return roomPerimeter.Contains(cellPos) || roomFloor.Contains(cellPos);
    }

    /// <summary>
    /// Обновить preview блок при наведении курсора (deletion mode)
    /// </summary>
    void UpdateDeletionPreview(Vector2Int gridPos)
    {
        if (gridPos == Vector2Int.zero)
        {
            ClearDelPreview();
            return;
        }

        // Если позиция изменилась - обновляем preview
        if (gridPos != lastDelPreviewPos)
        {
            ClearDelPreview();

            // Создаем новый preview блок (показываем всегда, даже если не над комнатой)
            if (delBuildGhostPrefab != null)
            {
                delPreviewBlock = CreateGhostBlockPooled(gridPos, delBuildGhostPrefab);
                delPreviewBlock.name = "Del_Preview";
                lastDelPreviewPos = gridPos;
            }
        }
    }

    /// <summary>
    /// Очистить deletion preview блок
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
    /// Обновить preview для drag удаления (ОПТИМИЗИРОВАНО - инкрементальное обновление)
    /// </summary>
    void UpdateDeletionDragPreview()
    {
        // Вычисляем прямоугольник
        int minX = Mathf.Min(deletionDragStart.x, deletionDragEnd.x);
        int maxX = Mathf.Max(deletionDragStart.x, deletionDragEnd.x);
        int minY = Mathf.Min(deletionDragStart.y, deletionDragEnd.y);
        int maxY = Mathf.Max(deletionDragStart.y, deletionDragEnd.y);

        // Новые позиции preview
        HashSet<Vector2Int> newPositions = new HashSet<Vector2Int>();

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                newPositions.Add(new Vector2Int(x, y));
            }
        }

        // ИНКРЕМЕНТАЛЬНОЕ ОБНОВЛЕНИЕ: Удаляем блоки которых больше нет
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

        // ИНКРЕМЕНТАЛЬНОЕ ОБНОВЛЕНИЕ: Добавляем новые блоки
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
    /// Очистить drag preview блоки
    /// </summary>
    void ClearDelDragPreview()
    {
        // ВАЖНО: Используем Object Pool для возврата блоков
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
    /// Удалить клетки в прямоугольнике
    /// </summary>
    void DeleteCellsInRectangle(Vector2Int startPos, Vector2Int endPos)
    {
        int minX = Mathf.Min(startPos.x, endPos.x);
        int maxX = Mathf.Max(startPos.x, endPos.x);
        int minY = Mathf.Min(startPos.y, endPos.y);
        int maxY = Mathf.Max(startPos.y, endPos.y);

        List<Vector2Int> cellsToDelete = new List<Vector2Int>();

        // Собираем все клетки для удаления
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

        // Удаляем все клетки
        foreach (Vector2Int cellPos in cellsToDelete)
        {
            DeleteCellInternal(cellPos);
        }

        if (DEBUG_LOGGING)
        {
        }

        // Пересчитываем периметр один раз после всех удалений
        // ВАЖНО: Передаем isDeletion=true чтобы НЕ вызывать ClosePerimeterOrthogonally
        RecalculatePerimeter(isDeletion: true);
        UpdateGhostsAfterDeletion();

        if (DEBUG_LOGGING)
        {
        }
    }

    /// <summary>
    /// Удалить клетку и пересчитать периметр (для одиночного удаления)
    /// </summary>
    void DeleteCell(Vector2Int cellPos)
    {
        DeleteCellInternal(cellPos);
        RecalculatePerimeter(isDeletion: true);
        UpdateGhostsAfterDeletion();
    }

    /// <summary>
    /// Внутренний метод удаления клетки (без пересчета периметра)
    /// </summary>
    void DeleteCellInternal(Vector2Int cellPos)
    {
        // Проверяем, есть ли эта клетка в периметре или полу
        bool wasInPerimeter = roomPerimeter.Contains(cellPos);
        bool wasInFloor = roomFloor.Contains(cellPos);

        if (!wasInPerimeter && !wasInFloor)
        {
            return;
        }


        // Добавляем в список удаленных для отслеживания
        if (!deletedCells.Contains(cellPos))
        {
            deletedCells.Add(cellPos);
        }

        // Удаляем из списков периметра и пола
        // Префабы удаляются автоматически при вызове UpdateGhostsAfterDeletion
        roomPerimeter.Remove(cellPos);
        roomFloor.Remove(cellPos);
    }

    /// <summary>
    /// Пересчитать периметр после добавления/удаления клеток
    /// </summary>
    /// <param name="isDeletion">True если это операция удаления (НЕ вызывать ClosePerimeterOrthogonally)</param>
    void RecalculatePerimeter(bool isDeletion = false)
    {
        // Собираем все оставшиеся клетки (периметр + пол)
        HashSet<Vector2Int> allCells = new HashSet<Vector2Int>();
        foreach (Vector2Int pos in roomPerimeter)
        {
            allCells.Add(pos);
        }
        foreach (Vector2Int pos in roomFloor)
        {
            allCells.Add(pos);
        }

        // Временные списки для нового периметра и пола
        List<Vector2Int> newPerimeter = new List<Vector2Int>();
        List<Vector2Int> newFloor = new List<Vector2Int>();

        // Для каждой клетки определяем, является ли она периметром
        foreach (Vector2Int pos in allCells)
        {
            // Клетка - периметр, если хотя бы одна из соседних клеток пустая или удалена
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
                // Клетка - периметр, если хотя бы один сосед не входит в комнату
                // (удаленные клетки уже не в allCells, поэтому проверка deletedCells избыточна)
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

        // Обновляем списки
        roomPerimeter.Clear();
        roomPerimeter.AddRange(newPerimeter);

        roomFloor.Clear();
        roomFloor.AddRange(newFloor);

        if (DEBUG_LOGGING)
        {
        }

        // СОХРАНЯЕМ исходный пол ПЕРЕД замыканием периметра
        // Эти клетки должны ВСЕГДА оставаться полом, даже после добавления стен
        HashSet<Vector2Int> originalFloor = new HashSet<Vector2Int>(roomFloor);

        int addedWalls = 0;

        // КРИТИЧНО: ClosePerimeterOrthogonally() вызываем ТОЛЬКО при добавлении клеток!
        // При удалении клеток НЕ нужно замыкать диагональные соединения - это ломает периметр!
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

        // ВАЖНО: После добавления новых стен, некоторые клетки пола могли стать стенами
        // Пересчитываем периметр/пол еще раз, НО сохраняем исходный пол
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
                // КРИТИЧНО: Если клетка была полом ДО замыкания периметра - она ОСТАЕТСЯ полом
                if (originalFloor.Contains(pos))
                {
                    finalFloor.Add(pos);
                    continue;
                }

                // Клетка - стена, если хотя бы один сосед пустой или удален
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
                    // Клетка - периметр, если хотя бы один сосед не входит в комнату
                    // (удаленные клетки уже не в allCellsAfter, проверка deletedCells избыточна)
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

        // КРИТИЧНО: Находим внутренние углы в зависимости от типа операции
        List<Vector2Int> innerCorners = new List<Vector2Int>();

        if (isDeletion && deletedCells.Count > 0)
        {
            // Удаление - ВАЖНО: нужно найти ОБА типа углов!
            // 1. Старые углы от объединенных областей (чтобы не потерять их)
            // 2. Новые углы в местах вырезов
            if (DEBUG_LOGGING)
            {
            }

            HashSet<Vector2Int> allInnerCorners = new HashSet<Vector2Int>();

            // Находим углы от объединенных областей
            List<Vector2Int> mergedAreaCorners = FindInnerCornersForMergedAreas();
            if (DEBUG_LOGGING)
            {
            }
            foreach (var corner in mergedAreaCorners)
            {
                allInnerCorners.Add(corner);
            }

            // Находим углы рядом с вырезами
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
            // Добавление - ищем углы по признаку пустой диагонали
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

        // Добавляем найденные внутренние углы
        if (innerCorners.Count > 0)
        {
            // СОХРАНЯЕМ найденные углы в переменную класса для использования в FinalizeBuild
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

                // КРИТИЧНО: Удаляем эту позицию из пола, если она там была!
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


        // ВАЖНО: Очищаем deletedCells после пересчета периметра
        // После пересчета периметра информация о удалениях УЖЕ учтена в новом периметре
        // Не нужно хранить историю - это приводит к ошибкам при повторных операциях
        deletedCells.Clear();
        if (DEBUG_LOGGING)
        {
        }
    }

    /// <summary>
    /// Замыкает периметр ортогонально, заполняя диагональные пробелы (ОПТИМИЗИРОВАНО)
    /// Работает итеративно, пока все диагональные соединения не будут устранены
    /// Возвращает количество добавленных стен
    /// ВАЖНО: deletedCells содержит ТОЛЬКО клетки из текущей операции (очищается после каждого RecalculatePerimeter)
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
        int maxIterations = 20; // ОПТИМИЗАЦИЯ: Снижено с 100 до 20 для производительности

        while (iteration < maxIterations)
        {
            iteration++;
            HashSet<Vector2Int> wallSet = new HashSet<Vector2Int>(roomPerimeter);
            HashSet<Vector2Int> floorSet = new HashSet<Vector2Int>(roomFloor);
            HashSet<Vector2Int> allRoomCells = new HashSet<Vector2Int>(wallSet);
            allRoomCells.UnionWith(floorSet);

            List<Vector2Int> wallsToAdd = new List<Vector2Int>();

            // Проверяем каждую стену на диагональные соединения
            foreach (Vector2Int wall in roomPerimeter)
            {
                // Проверяем 4 диагональных направления
                Vector2Int[] diagonals = new Vector2Int[]
                {
                    new Vector2Int(-1, 1),   // TopLeft
                    new Vector2Int(1, 1),    // TopRight
                    new Vector2Int(-1, -1),  // BottomLeft
                    new Vector2Int(1, -1)    // BottomRight
                };

                Vector2Int[][] orthogonalPairs = new Vector2Int[][]
                {
                    new Vector2Int[] { Vector2Int.up, Vector2Int.left },           // для TopLeft
                    new Vector2Int[] { Vector2Int.up, Vector2Int.right },          // для TopRight
                    new Vector2Int[] { Vector2Int.down, Vector2Int.left },         // для BottomLeft
                    new Vector2Int[] { Vector2Int.down, Vector2Int.right }         // для BottomRight
                };

                for (int i = 0; i < diagonals.Length; i++)
                {
                    Vector2Int diagonalPos = wall + diagonals[i];

                    // Проверяем ТОЛЬКО на стену на диагонали (не пол!)
                    // Это важно для вырезов, где между стенами может быть удаленная клетка
                    if (wallSet.Contains(diagonalPos))
                    {
                        Vector2Int ortho1 = wall + orthogonalPairs[i][0];
                        Vector2Int ortho2 = wall + orthogonalPairs[i][1];

                        // Проверяем есть ли ортогональный путь между стенами
                        bool hasOrtho1Wall = wallSet.Contains(ortho1);
                        bool hasOrtho2Wall = wallSet.Contains(ortho2);

                        // Если ни одной из промежуточных стен нет - диагональное соединение!
                        if (!hasOrtho1Wall && !hasOrtho2Wall)
                        {
                            // КРИТИЧЕСКАЯ ПРОВЕРКА: Замыкать ТОЛЬКО если это диагональное соединение рядом с ВЫРЕЗОМ
                            // Если далеко от удаленных клеток - это внешний угол исходной формы, НЕ замыкаем!
                            bool isWallNearDeletion = IsPositionNearDeletion(wall, deletedSet, 2);
                            bool isDiagNearDeletion = IsPositionNearDeletion(diagonalPos, deletedSet, 2);
                            bool isNearDeletion = isWallNearDeletion || isDiagNearDeletion;

                            if (DEBUG_LOGGING)
                            {
                            }

                            if (!isNearDeletion)
                            {
                                // Диагональное соединение далеко от вырезов - это внешний угол, НЕ замыкаем
                                if (DEBUG_LOGGING)
                                {
                                }
                                continue;
                            }

                            if (DEBUG_LOGGING)
                            {
                            }

                            // Нужно добавить стену "по ходу направления" одной из стен
                            // Определяем какая из двух позиций лучше подходит
                            bool ortho1IsDeleted = deletedSet.Contains(ortho1);
                            bool ortho2IsDeleted = deletedSet.Contains(ortho2);
                            bool ortho1IsFloor = floorSet.Contains(ortho1);
                            bool ortho2IsFloor = floorSet.Contains(ortho2);

                            Vector2Int posToAdd;

                            // ПРАВИЛО: добавляем стену в позицию, которая НЕ удалена и НЕ является полом
                            // Это создаст "продолжение" стены по её направлению
                            if (ortho1IsDeleted || ortho1IsFloor)
                            {
                                // Первая позиция занята - используем вторую
                                if (!ortho2IsDeleted && !ortho2IsFloor)
                                {
                                    posToAdd = ortho2;
                                }
                                else
                                {
                                    // Обе позиции заняты - пропускаем (это угол комнаты)
                                    continue;
                                }
                            }
                            else if (ortho2IsDeleted || ortho2IsFloor)
                            {
                                // Вторая позиция занята - используем первую
                                posToAdd = ortho1;
                            }
                            else
                            {
                                // Обе позиции свободны
                                // ПРОВЕРКА: если обе позиции НЕ часть комнаты (не стены, не пол) - это внешний угол
                                // На внешних углах диагональное соединение - это НОРМАЛЬНО, не заполняем
                                bool ortho1IsInRoom = allRoomCells.Contains(ortho1);
                                bool ortho2IsInRoom = allRoomCells.Contains(ortho2);

                                if (!ortho1IsInRoom && !ortho2IsInRoom)
                                {
                                    // Обе позиции за пределами комнаты - это внешний угол комнаты
                                    // Пропускаем! Диагональные соединения на внешних углах - это нормально
                                    continue;
                                }

                                // Хотя бы одна позиция внутри комнаты - это внутренний вырез
                                // Выбираем ту, что продолжает направление стены
                                int ortho1WallCount = CountWallNeighbors(ortho1, wallSet);
                                int ortho2WallCount = CountWallNeighbors(ortho2, wallSet);

                                // Предпочитаем позицию с большим количеством стен-соседей
                                if (ortho1WallCount > ortho2WallCount)
                                    posToAdd = ortho1;
                                else if (ortho2WallCount > ortho1WallCount)
                                    posToAdd = ortho2;
                                else
                                    posToAdd = ortho1; // По умолчанию первая
                            }

                            // Если позиция еще не добавлена
                            if (!wallSet.Contains(posToAdd) && !wallsToAdd.Contains(posToAdd))
                            {
                                wallsToAdd.Add(posToAdd);
                            }
                        }
                    }
                }
            }

            // Если ничего не добавлено - все соединения ортогональные
            if (wallsToAdd.Count == 0)
                break;

            // Добавляем новые стены в периметр
            foreach (Vector2Int newWall in wallsToAdd)
            {
                roomPerimeter.Add(newWall);
                totalAddedWalls++;
            }
        }

        if (iteration >= maxIterations)
        {
            Debug.LogWarning($"[RoomDragBuilder] ClosePerimeterOrthogonally reached max iterations ({maxIterations})");
        }

        return totalAddedWalls;
    }

    /// <summary>
    /// Проверяет, есть ли у позиции соседи из комнаты (стены или пол)
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
    /// Считает количество стен-соседей у данной позиции
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
    /// Проверяет, находится ли позиция рядом с удаленными клетками (в пределах radius)
    /// Используется для определения, является ли диагональное соединение частью выреза
    /// </summary>
    bool IsPositionNearDeletion(Vector2Int pos, HashSet<Vector2Int> deletedSet, int radius)
    {
        // Проверяем все позиции в пределах radius
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
    /// Найти позиции внутренних углов (в местах вырезов)
    /// Внутренние углы могут быть как пустыми позициями, так и позициями ПОЛА которые окружены стенами под прямым углом
    /// </summary>
    List<Vector2Int> FindInnerCorners()
    {
        List<Vector2Int> innerCorners = new List<Vector2Int>();

        // Создаем HashSet для быстрой проверки
        HashSet<Vector2Int> wallSet = new HashSet<Vector2Int>(roomPerimeter);
        HashSet<Vector2Int> floorSet = new HashSet<Vector2Int>(roomFloor);
        HashSet<Vector2Int> deletedSet = new HashSet<Vector2Int>(deletedCells);

        HashSet<Vector2Int> candidatePositions = new HashSet<Vector2Int>();

        // 1. Собираем ПУСТЫЕ позиции рядом с удаленными клетками
        foreach (Vector2Int deleted in deletedCells)
        {
            Vector2Int[] neighbors = new Vector2Int[]
            {
                deleted + Vector2Int.up,
                deleted + Vector2Int.down,
                deleted + Vector2Int.left,
                deleted + Vector2Int.right,
                deleted + new Vector2Int(-1, 1),   // диагонали тоже
                deleted + new Vector2Int(1, 1),
                deleted + new Vector2Int(-1, -1),
                deleted + new Vector2Int(1, -1)
            };

            foreach (Vector2Int neighbor in neighbors)
            {
                // Если это не стена - кандидат на внутренний угол (может быть полом или пустой)
                if (!wallSet.Contains(neighbor))
                {
                    candidatePositions.Add(neighbor);
                }
            }
        }

        // 2. ВАЖНО: Также добавляем все клетки ПОЛА как кандидатов
        // Потому что некоторые клетки пола на самом деле должны быть внутренними углами!
        foreach (Vector2Int floorPos in roomFloor)
        {
            candidatePositions.Add(floorPos);
        }

        // Проверяем каждого кандидата - является ли он внутренним углом
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
    /// Найти позиции внутренних углов для объединенных областей (БЕЗ удалений)
    /// Внутренний угол определяется по признаку пустой диагонали между двумя перпендикулярными стенами
    /// НЕ требует близости к удаленным клеткам - работает для случая объединения прямоугольников
    /// </summary>
    List<Vector2Int> FindInnerCornersForMergedAreas()
    {
        List<Vector2Int> innerCorners = new List<Vector2Int>();

        // Создаем HashSet для быстрой проверки
        HashSet<Vector2Int> wallSet = new HashSet<Vector2Int>(roomPerimeter);
        HashSet<Vector2Int> floorSet = new HashSet<Vector2Int>(roomFloor);
        HashSet<Vector2Int> allCells = new HashSet<Vector2Int>(wallSet);
        allCells.UnionWith(floorSet);

        if (DEBUG_LOGGING)
        {
        }

        // ВАЖНО: Проверяем НЕ ТОЛЬКО стены, но и ПОЛ!
        // При объединении областей внутренние углы могут оказаться на полу
        List<Vector2Int> positionsToCheck = new List<Vector2Int>();
        positionsToCheck.AddRange(roomPerimeter);
        positionsToCheck.AddRange(roomFloor);

        // Проверяем каждую позицию - может ли она быть внутренним углом
        foreach (Vector2Int wallPos in positionsToCheck)
        {
            // Получаем соседей
            Vector2Int topPos = wallPos + Vector2Int.up;
            Vector2Int bottomPos = wallPos + Vector2Int.down;
            Vector2Int leftPos = wallPos + Vector2Int.left;
            Vector2Int rightPos = wallPos + Vector2Int.right;

            bool hasWallTop = wallSet.Contains(topPos);
            bool hasWallBottom = wallSet.Contains(bottomPos);
            bool hasWallLeft = wallSet.Contains(leftPos);
            bool hasWallRight = wallSet.Contains(rightPos);

            // КРИТИЧНО: Внутренний угол должен иметь РОВНО 2 стены-соседа под прямым углом
            int wallNeighborCount = (hasWallTop ? 1 : 0) + (hasWallBottom ? 1 : 0) + (hasWallLeft ? 1 : 0) + (hasWallRight ? 1 : 0);

            if (wallNeighborCount != 2)
                continue;

            // Проверяем, что 2 стены находятся под прямым углом (не напротив друг друга)
            bool hasVerticalPair = hasWallTop && hasWallBottom;
            bool hasHorizontalPair = hasWallLeft && hasWallRight;

            if (hasVerticalPair || hasHorizontalPair)
                continue;

            // Если дошли сюда - у нас есть кандидат на внутренний угол
            if (DEBUG_LOGGING)
            {
            }

            // Теперь проверяем диагонали - внутренний угол имеет ПУСТУЮ диагональ между двумя перпендикулярными стенами
            Vector2Int topLeftDiag = wallPos + new Vector2Int(-1, 1);
            Vector2Int topRightDiag = wallPos + new Vector2Int(1, 1);
            Vector2Int bottomLeftDiag = wallPos + new Vector2Int(-1, -1);
            Vector2Int bottomRightDiag = wallPos + new Vector2Int(1, -1);

            bool isInnerCorner = false;

            // Конфигурация 1: Стены Top+Right → диагональ TopRight должна быть пустой
            if (hasWallTop && hasWallRight)
            {
                bool diagEmpty = !allCells.Contains(topRightDiag);
                if (DEBUG_LOGGING)
                {
                }
                // Проверяем что TopRight диагональ НЕ часть комнаты (пустая)
                if (diagEmpty)
                {
                    isInnerCorner = true;
                    if (DEBUG_LOGGING)
                    {
                    }
                }
            }
            // Конфигурация 2: Стены Top+Left → диагональ TopLeft должна быть пустой
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
            // Конфигурация 3: Стены Bottom+Right → диагональ BottomRight должна быть пустой
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
            // Конфигурация 4: Стены Bottom+Left → диагональ BottomLeft должна быть пустой
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
    /// Проверить является ли ПУСТАЯ позиция внутренним углом
    /// Внутренний угол - ПУСТАЯ клетка где встречаются ДВЕ стены под прямым углом и ПОЛ на противоположной диагонали
    /// КРИТИЧНО: Внутренний угол должен быть РЯДОМ с удаленной клеткой (вырез), иначе это внешний угол комнаты
    /// </summary>
    bool IsPositionAnInnerCorner(Vector2Int pos, HashSet<Vector2Int> wallSet, HashSet<Vector2Int> floorSet, HashSet<Vector2Int> deletedSet)
    {
        // КРИТИЧЕСКАЯ ПРОВЕРКА #1: Внутренний угол должен быть РЯДОМ с удаленной клеткой
        // Это отличает внутренний угол (в вырезе) от внешнего угла комнаты
        bool hasDeletedNeighbor = false;
        Vector2Int[] allNeighbors = new Vector2Int[]
        {
            pos + Vector2Int.up,
            pos + Vector2Int.down,
            pos + Vector2Int.left,
            pos + Vector2Int.right,
            pos + new Vector2Int(-1, 1),   // диагонали тоже проверяем
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

        // Проверяем соседние клетки
        Vector2Int topPos = pos + Vector2Int.up;
        Vector2Int bottomPos = pos + Vector2Int.down;
        Vector2Int leftPos = pos + Vector2Int.left;
        Vector2Int rightPos = pos + Vector2Int.right;

        bool hasWallTop = wallSet.Contains(topPos);
        bool hasWallBottom = wallSet.Contains(bottomPos);
        bool hasWallLeft = wallSet.Contains(leftPos);
        bool hasWallRight = wallSet.Contains(rightPos);

        // Внутренний угол должен иметь РОВНО 2 стены-соседа под прямым углом
        int wallNeighborCount = (hasWallTop ? 1 : 0) + (hasWallBottom ? 1 : 0) + (hasWallLeft ? 1 : 0) + (hasWallRight ? 1 : 0);

        if (wallNeighborCount != 2)
            return false;

        // Проверяем, что 2 стены находятся под прямым углом (не напротив друг друга)
        bool hasVerticalPair = hasWallTop && hasWallBottom;
        bool hasHorizontalPair = hasWallLeft && hasWallRight;

        if (hasVerticalPair || hasHorizontalPair)
            return false;

        // Проверяем диагонали - внутренний угол должен иметь ПОЛ на противоположной диагонали
        Vector2Int topLeftDiag = pos + new Vector2Int(-1, 1);
        Vector2Int topRightDiag = pos + new Vector2Int(1, 1);
        Vector2Int bottomLeftDiag = pos + new Vector2Int(-1, -1);
        Vector2Int bottomRightDiag = pos + new Vector2Int(1, -1);

        bool hasFloorTopLeft = floorSet.Contains(topLeftDiag);
        bool hasFloorTopRight = floorSet.Contains(topRightDiag);
        bool hasFloorBottomLeft = floorSet.Contains(bottomLeftDiag);
        bool hasFloorBottomRight = floorSet.Contains(bottomRightDiag);

        // Проверяем конфигурации: стены Top+Right → пол должен быть BottomLeft
        if (hasWallTop && hasWallRight && hasFloorBottomLeft)
            return true;

        // Стены Top+Left → пол должен быть BottomRight
        if (hasWallTop && hasWallLeft && hasFloorBottomRight)
            return true;

        // Стены Bottom+Right → пол должен быть TopLeft
        if (hasWallBottom && hasWallRight && hasFloorTopLeft)
            return true;

        // Стены Bottom+Left → пол должен быть TopRight
        if (hasWallBottom && hasWallLeft && hasFloorTopRight)
            return true;

        return false;
    }

    /// <summary>
    /// УСТАРЕВШИЙ МЕТОД - оставлен для совместимости
    /// Проверить является ли стена внутренним углом
    /// Внутренний угол - угол ВНУТРИ выреза, где встречаются две стены под прямым углом
    /// КЛЮЧЕВОЕ ОТЛИЧИЕ: внутренний угол должен быть рядом с удаленной клеткой (там где вырез)
    /// </summary>
    bool IsWallAnInnerCorner(Vector2Int wallPos, HashSet<Vector2Int> wallSet, HashSet<Vector2Int> floorSet)
    {

        // Проверяем соседние клетки
        Vector2Int topPos = wallPos + Vector2Int.up;
        Vector2Int bottomPos = wallPos + Vector2Int.down;
        Vector2Int leftPos = wallPos + Vector2Int.left;
        Vector2Int rightPos = wallPos + Vector2Int.right;

        bool hasWallTop = wallSet.Contains(topPos);
        bool hasWallBottom = wallSet.Contains(bottomPos);
        bool hasWallLeft = wallSet.Contains(leftPos);
        bool hasWallRight = wallSet.Contains(rightPos);


        // ВАЖНО: Угол должен иметь РОВНО 2 стены-соседа под прямым углом
        int wallNeighborCount = (hasWallTop ? 1 : 0) + (hasWallBottom ? 1 : 0) + (hasWallLeft ? 1 : 0) + (hasWallRight ? 1 : 0);
        if (wallNeighborCount != 2)
        {
            return false;
        }

        // Проверяем, что 2 стены находятся под прямым углом (не напротив друг друга)
        bool hasVerticalPair = hasWallTop && hasWallBottom;
        bool hasHorizontalPair = hasWallLeft && hasWallRight;
        if (hasVerticalPair || hasHorizontalPair)
        {
            return false;
        }

        // КРИТИЧЕСКАЯ ПРОВЕРКА: внутренний угол должен быть РЯДОМ с удаленной клеткой
        // Это отличает его от внешнего угла комнаты
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

        // Проверяем диагонали - внутренний угол должен иметь пол на противоположной диагонали
        Vector2Int topLeftDiag = wallPos + new Vector2Int(-1, 1);
        Vector2Int topRightDiag = wallPos + new Vector2Int(1, 1);
        Vector2Int bottomLeftDiag = wallPos + new Vector2Int(-1, -1);
        Vector2Int bottomRightDiag = wallPos + new Vector2Int(1, -1);

        bool hasFloorTopLeft = floorSet.Contains(topLeftDiag);
        bool hasFloorTopRight = floorSet.Contains(topRightDiag);
        bool hasFloorBottomLeft = floorSet.Contains(bottomLeftDiag);
        bool hasFloorBottomRight = floorSet.Contains(bottomRightDiag);


        // Проверяем конфигурации внутренних углов
        // Внутренний угол имеет пол на противоположной диагонали от места соединения стен
        if (hasWallTop && hasWallRight)
        {
            // Стены сверху и справа -> пол должен быть слева снизу
            if (hasFloorBottomLeft)
            {
                return true;
            }
        }

        if (hasWallTop && hasWallLeft)
        {
            // Стены сверху и слева -> пол должен быть справа снизу
            if (hasFloorBottomRight)
            {
                return true;
            }
        }

        if (hasWallBottom && hasWallRight)
        {
            // Стены снизу и справа -> пол должен быть слева сверху
            if (hasFloorTopLeft)
            {
                return true;
            }
        }

        if (hasWallBottom && hasWallLeft)
        {
            // Стены снизу и слева -> пол должен быть справа сверху
            if (hasFloorTopRight)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Обновить ghost блоки после удаления (ОПТИМИЗИРОВАНО - инкрементальное обновление)
    /// </summary>
    void UpdateGhostsAfterDeletion()
    {
        // ИНКРЕМЕНТАЛЬНОЕ ОБНОВЛЕНИЕ: обновляем только измененные блоки

        HashSet<Vector2Int> newPerimeter = new HashSet<Vector2Int>(roomPerimeter);
        HashSet<Vector2Int> newFloor = new HashSet<Vector2Int>(roomFloor);

        // ВАЖНО: Обрабатываем Idle, PreviewReady - используем активные ghost блоки
        if (currentState == DragState.Idle || currentState == DragState.PreviewReady)
        {
            // Удаляем стены которых больше нет
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

            // Добавляем новые стены
            foreach (var pos in newPerimeter)
            {
                if (!activeGhostBlocks.ContainsKey(pos))
                {
                    GameObject ghostBlock = CreateGhostBlockPooled(pos, buildGhostPrefab);
                    activeGhostBlocks[pos] = ghostBlock;
                    ghostBlocks.Add(ghostBlock);
                }
            }

            // Удаляем пол которого больше нет
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

            // Добавляем новый пол
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

            // Обновляем state если нужно
            if (currentState == DragState.Idle && (roomPerimeter.Count > 0 || roomFloor.Count > 0))
            {
                currentState = DragState.PreviewReady;
            }
        }
        else if (currentState == DragState.Confirmed)
        {
            // Для Confirmed состояния используем старый метод (используется редко)
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
    /// Очистить Del_Build_Ghost блоки и список удаленных клеток
    /// </summary>
    void ClearDelGhostBlocks()
    {
        // Очищаем список удаленных клеток (маркеры больше не создаются)
        foreach (GameObject block in delGhostBlocks)
        {
            if (block != null)
                Destroy(block);
        }
        delGhostBlocks.Clear();
        deletedCells.Clear();
    }

    /// <summary>
    /// Проверить, готов ли силуэт к подтверждению
    /// </summary>
    public bool IsReadyToConfirm()
    {
        return currentState == DragState.PreviewReady;
    }

    /// <summary>
    /// Проверить, подтверждена ли постройка
    /// </summary>
    public bool IsConfirmed()
    {
        return currentState == DragState.Confirmed;
    }

    /// <summary>
    /// Проверить, активен ли режим удаления
    /// </summary>
    public bool IsDeletionModeActive()
    {
        return isDeletionModeActive;
    }

    /// <summary>
    /// Проверить, активен ли drag режим
    /// </summary>
    public bool IsDragModeActive()
    {
        return isDragModeActive;
    }

    /// <summary>
    /// Проверить, есть ли данные комнаты (периметр или пол)
    /// </summary>
    public bool HasRoomData()
    {
        return roomPerimeter.Count > 0 || roomFloor.Count > 0;
    }

    /// <summary>
    /// Проверить, можно ли подтвердить постройку
    /// Возвращает true если есть хоть какие-то данные или идет процесс рисования
    /// </summary>
    public bool CanConfirmBuild()
    {
        // Кнопка активна если:
        // 1. Идет процесс рисования (Dragging state)
        // 2. Есть готовый preview (PreviewReady state)
        // 3. ИЛИ просто есть данные (на случай если state сбросился но данные остались)
        return currentState == DragState.Dragging ||
               currentState == DragState.PreviewReady ||
               HasRoomData();
    }
}
