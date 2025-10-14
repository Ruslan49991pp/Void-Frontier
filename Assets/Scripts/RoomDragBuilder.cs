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

    // Режим активен
    private bool isDragModeActive = false;

    // Режим удаления
    private bool isDeletionModeActive = false;
    private List<Vector2Int> deletedCells = new List<Vector2Int>();   // Удаленные клетки
    private List<GameObject> delGhostBlocks = new List<GameObject>(); // Del_Build_Ghost блоки
    private GameObject delPreviewBlock = null;                         // Preview блок при наведении
    private Vector2Int lastPreviewPos = Vector2Int.zero;              // Последняя позиция preview

    // Сохраненные внутренние углы
    private List<Vector2Int> savedInnerCorners = new List<Vector2Int>(); // Внутренние углы найденные в RecalculatePerimeter

    // Drag удаление
    private bool isDeletionDragActive = false;                        // Активен ли drag режим удаления
    private Vector2Int deletionDragStart = Vector2Int.zero;           // Начальная позиция drag
    private Vector2Int deletionDragEnd = Vector2Int.zero;             // Конечная позиция drag
    private List<GameObject> delDragPreviewBlocks = new List<GameObject>(); // Preview блоки для drag

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
        if (!isDragModeActive) return;

        // Проверяем, не над UI ли мышь
        bool isPointerOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

        if (isPointerOverUI) return;

        switch (currentState)
        {
            case DragState.Idle:
                HandleIdleState();
                break;

            case DragState.Dragging:
                HandleDraggingState();
                break;

            case DragState.PreviewReady:
                // Ожидание подтверждения через кнопку AddBuild
                break;

            case DragState.Confirmed:
                // Ожидание выхода из режима строительства
                break;
        }

        // Обработка режима удаления
        if (isDeletionModeActive && (currentState == DragState.PreviewReady || currentState == DragState.Confirmed))
        {
            HandleDeletionMode();
        }
    }

    /// <summary>
    /// Активировать режим drag строительства
    /// </summary>
    public void ActivateDragMode()
    {
        isDragModeActive = true;
        currentState = DragState.Idle;
        Debug.Log("[RoomDragBuilder] Drag mode activated - waiting for mouse input");
        Debug.Log($"[RoomDragBuilder] GridManager: {(gridManager != null ? "OK" : "NULL")}, Camera: {(playerCamera != null ? "OK" : "NULL")}");
    }

    /// <summary>
    /// Деактивировать режим drag строительства
    /// </summary>
    public void DeactivateDragMode()
    {
        isDragModeActive = false;
        ClearAllGhosts();
        Debug.Log("[RoomDragBuilder] Drag mode deactivated");
    }

    /// <summary>
    /// Активировать режим удаления
    /// </summary>
    public void ActivateDeletionMode()
    {
        isDeletionModeActive = true;
        Debug.Log("[RoomDragBuilder] Deletion mode activated");
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
        Debug.Log("[RoomDragBuilder] Deletion mode deactivated");
    }

    /// <summary>
    /// Обработка состояния Idle - ожидание начала drag
    /// </summary>
    void HandleIdleState()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("[RoomDragBuilder] Mouse button down detected");

            // Начинаем drag
            Vector2Int gridPos = GetGridPositionFromMouse();
            Debug.Log($"[RoomDragBuilder] Grid position from mouse: {gridPos}");

            if (gridPos != Vector2Int.zero)
            {
                dragStartGridPos = gridPos;
                dragEndGridPos = gridPos;
                currentState = DragState.Dragging;
                Debug.Log($"[RoomDragBuilder] Started dragging from {dragStartGridPos}");
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
            currentState = DragState.PreviewReady;
            Debug.Log($"[RoomDragBuilder] Drag complete: from {dragStartGridPos} to {dragEndGridPos}");
        }
    }

    /// <summary>
    /// Обновить превью во время drag
    /// </summary>
    void UpdateDragPreview()
    {
        // Очищаем старые блоки
        ClearGhostBlocks();

        // Вычисляем прямоугольник
        int minX = Mathf.Min(dragStartGridPos.x, dragEndGridPos.x);
        int maxX = Mathf.Max(dragStartGridPos.x, dragEndGridPos.x);
        int minY = Mathf.Min(dragStartGridPos.y, dragEndGridPos.y);
        int maxY = Mathf.Max(dragStartGridPos.y, dragEndGridPos.y);

        // Создаем периметр комнаты и пол
        roomPerimeter.Clear();
        roomFloor.Clear();

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int cellPos = new Vector2Int(x, y);

                // Проверяем, является ли клетка частью периметра
                bool isPerimeter = (x == minX || x == maxX || y == minY || y == maxY);

                if (isPerimeter)
                {
                    // Стена (периметр)
                    roomPerimeter.Add(cellPos);

                    // Создаем Build_Ghost блок (стена)
                    GameObject ghostBlock = CreateGhostBlock(cellPos, buildGhostPrefab);
                    ghostBlocks.Add(ghostBlock);
                }
                else
                {
                    // Пол (внутри)
                    roomFloor.Add(cellPos);

                    // Создаем Floor_Ghost блок (пол)
                    if (floorGhostPrefab != null)
                    {
                        GameObject floorBlock = CreateGhostBlock(cellPos, floorGhostPrefab);
                        ghostFloorBlocks.Add(floorBlock);
                    }
                }
            }
        }

        Debug.Log($"[RoomDragBuilder] Updated preview with {ghostBlocks.Count} wall blocks and {ghostFloorBlocks.Count} floor blocks");
    }

    /// <summary>
    /// Создать призрачный блок в указанной позиции
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
    /// Подтвердить постройку - заменить Build_Ghost на Add_Build_Ghost
    /// </summary>
    public void ConfirmBuild()
    {
        if (currentState != DragState.PreviewReady)
        {
            Debug.LogWarning("[RoomDragBuilder] Cannot confirm - not in PreviewReady state");
            return;
        }

        Debug.Log("[RoomDragBuilder] Confirming build - replacing ghosts with confirmed ghosts");

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
        Debug.Log($"[RoomDragBuilder] Build confirmed with {confirmedBlocks.Count} wall blocks and {confirmedFloorBlocks.Count} floor blocks");
    }

    /// <summary>
    /// Финализировать постройку - заменить Add_Build_Ghost на SM_Wall/SM_Wall_L
    /// </summary>
    public void FinalizeBuild()
    {
        if (currentState != DragState.Confirmed)
        {
            Debug.LogWarning("[RoomDragBuilder] Cannot finalize - not in Confirmed state");
            return;
        }

        Debug.Log("[RoomDragBuilder] Finalizing build - replacing Add_Build_Ghost with walls");

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
        Debug.Log($"[RoomDragBuilder] FinalizeBuild - Using {savedInnerCorners.Count} saved inner corners to pass to RoomBuilder");
        foreach (Vector2Int corner in savedInnerCorners)
        {
            Debug.Log($"[RoomDragBuilder] FinalizeBuild - Inner corner at {corner}");
        }

        // Создаем комнату с кастомным силуэтом, передавая внутренние углы
        GameObject room = RoomBuilder.Instance.BuildCustomRoom(roomGridPos, roomSize, "DraggedRoom", roomPerimeter, roomFloor, savedInnerCorners);
        room.name = $"DraggedRoom_{roomGridPos.x}_{roomGridPos.y}";

        Debug.Log($"[RoomDragBuilder] Created custom room at {roomGridPos} with {roomPerimeter.Count} walls and {roomFloor.Count} floor tiles");

        // Регистрируем в GridManager
        gridManager.OccupyCellPerimeter(roomGridPos, roomSize.x, roomSize.y, room, "Room");

        // Очищаем Add_Build_Ghost и Add_Floor_Ghost блоки
        ClearConfirmedBlocks();

        // Сбрасываем состояние
        currentState = DragState.Idle;
        roomPerimeter.Clear();
        roomFloor.Clear();
        deletedCells.Clear();
        savedInnerCorners.Clear();

        Debug.Log("[RoomDragBuilder] Build finalized successfully");
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
                    Debug.Log($"[RoomDragBuilder] Started deletion drag from {deletionDragStart}");
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
                Debug.Log($"[RoomDragBuilder] Deletion drag complete: from {deletionDragStart} to {deletionDragEnd}");
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
    /// Обновить preview блок при наведении курсора
    /// </summary>
    void UpdateDeletionPreview(Vector2Int gridPos)
    {
        if (gridPos == Vector2Int.zero)
        {
            ClearDelPreview();
            return;
        }

        // Если позиция изменилась - обновляем preview
        if (gridPos != lastPreviewPos)
        {
            ClearDelPreview();

            // Создаем новый preview блок (показываем всегда, даже если не над комнатой)
            if (delBuildGhostPrefab != null)
            {
                delPreviewBlock = CreateGhostBlock(gridPos, delBuildGhostPrefab);
                delPreviewBlock.name = "Del_Preview";
                lastPreviewPos = gridPos;
            }
        }
    }

    /// <summary>
    /// Очистить preview блок
    /// </summary>
    void ClearDelPreview()
    {
        if (delPreviewBlock != null)
        {
            Destroy(delPreviewBlock);
            delPreviewBlock = null;
        }
        lastPreviewPos = Vector2Int.zero;
    }

    /// <summary>
    /// Обновить preview для drag удаления
    /// </summary>
    void UpdateDeletionDragPreview()
    {
        // Очищаем старые preview блоки
        ClearDelDragPreview();

        // Вычисляем прямоугольник
        int minX = Mathf.Min(deletionDragStart.x, deletionDragEnd.x);
        int maxX = Mathf.Max(deletionDragStart.x, deletionDragEnd.x);
        int minY = Mathf.Min(deletionDragStart.y, deletionDragEnd.y);
        int maxY = Mathf.Max(deletionDragStart.y, deletionDragEnd.y);

        // Создаем preview блоки для всех клеток в прямоугольнике (показываем везде)
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int cellPos = new Vector2Int(x, y);

                if (delBuildGhostPrefab != null)
                {
                    GameObject previewBlock = CreateGhostBlock(cellPos, delBuildGhostPrefab);
                    previewBlock.name = $"Del_DragPreview_{x}_{y}";
                    delDragPreviewBlocks.Add(previewBlock);
                }
            }
        }

        Debug.Log($"[RoomDragBuilder] Deletion drag preview updated: {delDragPreviewBlocks.Count} cells");
    }

    /// <summary>
    /// Очистить drag preview блоки
    /// </summary>
    void ClearDelDragPreview()
    {
        foreach (GameObject block in delDragPreviewBlocks)
        {
            if (block != null)
                Destroy(block);
        }
        delDragPreviewBlocks.Clear();
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

        Debug.Log($"[RoomDragBuilder] Deleting {cellsToDelete.Count} cells in rectangle");

        // Удаляем все клетки
        foreach (Vector2Int cellPos in cellsToDelete)
        {
            DeleteCellInternal(cellPos);
        }

        // Пересчитываем периметр один раз после всех удалений
        RecalculatePerimeter();
        UpdateGhostsAfterDeletion();
    }

    /// <summary>
    /// Удалить клетку и пересчитать периметр (для одиночного удаления)
    /// </summary>
    void DeleteCell(Vector2Int cellPos)
    {
        DeleteCellInternal(cellPos);
        RecalculatePerimeter();
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

        Debug.Log($"[RoomDragBuilder] Deleting cell {cellPos}");

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
    /// Пересчитать периметр после удаления
    /// </summary>
    void RecalculatePerimeter()
    {
        Debug.Log($"[RoomDragBuilder] ========== RECALCULATING PERIMETER ==========");
        Debug.Log($"[RoomDragBuilder] Before recalculation: {roomPerimeter.Count} walls, {roomFloor.Count} floor");

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
                if (!allCells.Contains(neighbor) || deletedCells.Contains(neighbor))
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

        Debug.Log($"[RoomDragBuilder] After initial recalculation: {roomPerimeter.Count} walls, {roomFloor.Count} floor");

        // СОХРАНЯЕМ исходный пол ПЕРЕД замыканием периметра
        // Эти клетки должны ВСЕГДА оставаться полом, даже после добавления стен
        HashSet<Vector2Int> originalFloor = new HashSet<Vector2Int>(roomFloor);
        Debug.Log($"[RoomDragBuilder] Saved {originalFloor.Count} original floor cells that must stay as floor");

        // Замыкаем периметр ортогонально - заполняем диагональные пробелы
        int addedWalls = ClosePerimeterOrthogonally();
        Debug.Log($"[RoomDragBuilder] Added {addedWalls} walls to close perimeter orthogonally");

        // ВАЖНО: После добавления новых стен, некоторые клетки пола могли стать стенами
        // Пересчитываем периметр/пол еще раз, НО сохраняем исходный пол
        if (addedWalls > 0)
        {
            Debug.Log($"[RoomDragBuilder] Re-classifying floor/wall after orthogonal closure");
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
                    Debug.Log($"[RoomDragBuilder] Position {pos} stays as FLOOR (was original floor)");
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
                    if (!allCellsAfter.Contains(neighbor) || deletedCells.Contains(neighbor))
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

            Debug.Log($"[RoomDragBuilder] Re-classified: {finalPerimeter.Count} walls, {finalFloor.Count} floor");
        }

        // Находим и добавляем внутренние углы (в местах вырезов)
        List<Vector2Int> innerCorners = FindInnerCorners();

        // СОХРАНЯЕМ найденные углы в переменную класса для использования в FinalizeBuild
        savedInnerCorners.Clear();
        savedInnerCorners.AddRange(innerCorners);
        Debug.Log($"[RoomDragBuilder] SAVED {savedInnerCorners.Count} inner corners for later use");

        foreach (Vector2Int corner in innerCorners)
        {
            if (!roomPerimeter.Contains(corner))
            {
                roomPerimeter.Add(corner);
                Debug.Log($"[RoomDragBuilder] Added inner corner at {corner} to perimeter");
            }

            // КРИТИЧНО: Удаляем эту позицию из пола, если она там была!
            if (roomFloor.Contains(corner))
            {
                roomFloor.Remove(corner);
                Debug.Log($"[RoomDragBuilder] Removed inner corner at {corner} from floor - it's now a wall!");
            }
        }

        Debug.Log($"[RoomDragBuilder] Perimeter recalculated: {roomPerimeter.Count} walls (including {innerCorners.Count} inner corners), {roomFloor.Count} floor cells");
    }

    /// <summary>
    /// Замыкает периметр ортогонально, заполняя диагональные пробелы
    /// Работает итеративно, пока все диагональные соединения не будут устранены
    /// Возвращает количество добавленных стен
    /// </summary>
    int ClosePerimeterOrthogonally()
    {
        int totalAddedWalls = 0;
        int iteration = 0;
        int maxIterations = 100; // Защита от бесконечного цикла

        HashSet<Vector2Int> deletedSet = new HashSet<Vector2Int>(deletedCells);

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
                                    Debug.Log($"[RoomDragBuilder] Iteration {iteration}: Diagonal at {wall}-{diagonalPos} is OUTER corner, skipping");
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
                                Debug.Log($"[RoomDragBuilder] Iteration {iteration}: Diagonal connection between {wall} and {diagonalPos}, adding wall at {posToAdd}");
                            }
                        }
                    }
                }
            }

            // Если ничего не добавлено - все соединения ортогональные
            if (wallsToAdd.Count == 0)
            {
                Debug.Log($"[RoomDragBuilder] ClosePerimeterOrthogonally completed in {iteration} iterations. Total walls added: {totalAddedWalls}");
                break;
            }

            // Добавляем новые стены в периметр
            foreach (Vector2Int newWall in wallsToAdd)
            {
                roomPerimeter.Add(newWall);
                totalAddedWalls++;
            }

            Debug.Log($"[RoomDragBuilder] Iteration {iteration}: Added {wallsToAdd.Count} walls to close gaps");
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
    /// Найти позиции внутренних углов (в местах вырезов)
    /// Внутренние углы могут быть как пустыми позициями, так и позициями ПОЛА которые окружены стенами под прямым углом
    /// </summary>
    List<Vector2Int> FindInnerCorners()
    {
        List<Vector2Int> innerCorners = new List<Vector2Int>();

        Debug.Log($"[RoomDragBuilder] FindInnerCorners - Starting search. Walls: {roomPerimeter.Count}, Floor: {roomFloor.Count}, Deleted: {deletedCells.Count}");

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

        Debug.Log($"[RoomDragBuilder] FindInnerCorners - Checking {candidatePositions.Count} candidate positions (empty + floor near deleted cells)");

        // Проверяем каждого кандидата - является ли он внутренним углом
        foreach (Vector2Int pos in candidatePositions)
        {
            if (IsPositionAnInnerCorner(pos, wallSet, floorSet, deletedSet))
            {
                innerCorners.Add(pos);
                string posType = floorSet.Contains(pos) ? "FLOOR" : "EMPTY";
                Debug.Log($"[RoomDragBuilder] FindInnerCorners - FOUND inner corner at {posType} position {pos}");
            }
        }

        Debug.Log($"[RoomDragBuilder] FindInnerCorners - Complete. Found {innerCorners.Count} inner corners");
        return innerCorners;
    }

    /// <summary>
    /// Проверить является ли ПУСТАЯ позиция внутренним углом
    /// Внутренний угол - ПУСТАЯ клетка где встречаются ДВЕ стены под прямым углом и ПОЛ на противоположной диагонали
    /// КРИТИЧНО: Внутренний угол должен быть РЯДОМ с удаленной клеткой (вырез), иначе это внешний угол комнаты
    /// </summary>
    bool IsPositionAnInnerCorner(Vector2Int pos, HashSet<Vector2Int> wallSet, HashSet<Vector2Int> floorSet, HashSet<Vector2Int> deletedSet)
    {
        Debug.Log($"[RoomDragBuilder] === Checking position {pos} for inner corner ===");

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
        {
            Debug.Log($"[RoomDragBuilder]   REJECT: No deleted neighbor - this is OUTER corner of room, not inner corner of cutout");
            return false;
        }

        Debug.Log($"[RoomDragBuilder]   ✓ Has deleted neighbor - could be inner corner");

        // Проверяем соседние клетки
        Vector2Int topPos = pos + Vector2Int.up;
        Vector2Int bottomPos = pos + Vector2Int.down;
        Vector2Int leftPos = pos + Vector2Int.left;
        Vector2Int rightPos = pos + Vector2Int.right;

        bool hasWallTop = wallSet.Contains(topPos);
        bool hasWallBottom = wallSet.Contains(bottomPos);
        bool hasWallLeft = wallSet.Contains(leftPos);
        bool hasWallRight = wallSet.Contains(rightPos);

        Debug.Log($"[RoomDragBuilder]   Wall neighbors: Top={hasWallTop}, Bottom={hasWallBottom}, Left={hasWallLeft}, Right={hasWallRight}");

        // Внутренний угол должен иметь РОВНО 2 стены-соседа под прямым углом
        int wallNeighborCount = (hasWallTop ? 1 : 0) + (hasWallBottom ? 1 : 0) + (hasWallLeft ? 1 : 0) + (hasWallRight ? 1 : 0);
        Debug.Log($"[RoomDragBuilder]   Total wall neighbors: {wallNeighborCount}");

        if (wallNeighborCount != 2)
        {
            Debug.Log($"[RoomDragBuilder]   REJECT: Need exactly 2 wall neighbors, has {wallNeighborCount}");
            return false;
        }

        // Проверяем, что 2 стены находятся под прямым углом (не напротив друг друга)
        bool hasVerticalPair = hasWallTop && hasWallBottom;
        bool hasHorizontalPair = hasWallLeft && hasWallRight;

        if (hasVerticalPair || hasHorizontalPair)
        {
            Debug.Log($"[RoomDragBuilder]   REJECT: Walls are opposite (not perpendicular)");
            return false;
        }

        // Проверяем диагонали - внутренний угол должен иметь ПОЛ на противоположной диагонали
        Vector2Int topLeftDiag = pos + new Vector2Int(-1, 1);
        Vector2Int topRightDiag = pos + new Vector2Int(1, 1);
        Vector2Int bottomLeftDiag = pos + new Vector2Int(-1, -1);
        Vector2Int bottomRightDiag = pos + new Vector2Int(1, -1);

        bool hasFloorTopLeft = floorSet.Contains(topLeftDiag);
        bool hasFloorTopRight = floorSet.Contains(topRightDiag);
        bool hasFloorBottomLeft = floorSet.Contains(bottomLeftDiag);
        bool hasFloorBottomRight = floorSet.Contains(bottomRightDiag);

        Debug.Log($"[RoomDragBuilder]   Diagonal floor: TL={hasFloorTopLeft}, TR={hasFloorTopRight}, BL={hasFloorBottomLeft}, BR={hasFloorBottomRight}");

        // Проверяем конфигурации: стены Top+Right → пол должен быть BottomLeft
        if (hasWallTop && hasWallRight)
        {
            if (hasFloorBottomLeft)
            {
                Debug.Log($"[RoomDragBuilder]   ✓✓✓ CONFIRMED inner corner at {pos} (walls: Top+Right, floor at BottomLeft, near deleted cell)");
                return true;
            }
        }

        // Стены Top+Left → пол должен быть BottomRight
        if (hasWallTop && hasWallLeft)
        {
            if (hasFloorBottomRight)
            {
                Debug.Log($"[RoomDragBuilder]   ✓✓✓ CONFIRMED inner corner at {pos} (walls: Top+Left, floor at BottomRight, near deleted cell)");
                return true;
            }
        }

        // Стены Bottom+Right → пол должен быть TopLeft
        if (hasWallBottom && hasWallRight)
        {
            if (hasFloorTopLeft)
            {
                Debug.Log($"[RoomDragBuilder]   ✓✓✓ CONFIRMED inner corner at {pos} (walls: Bottom+Right, floor at TopLeft, near deleted cell)");
                return true;
            }
        }

        // Стены Bottom+Left → пол должен быть TopRight
        if (hasWallBottom && hasWallLeft)
        {
            if (hasFloorTopRight)
            {
                Debug.Log($"[RoomDragBuilder]   ✓✓✓ CONFIRMED inner corner at {pos} (walls: Bottom+Left, floor at TopRight, near deleted cell)");
                return true;
            }
        }

        Debug.Log($"[RoomDragBuilder]   REJECT: No matching floor pattern for inner corner");
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
        Debug.Log($"[RoomDragBuilder] === Checking wall at {wallPos} for inner corner ===");

        // Проверяем соседние клетки
        Vector2Int topPos = wallPos + Vector2Int.up;
        Vector2Int bottomPos = wallPos + Vector2Int.down;
        Vector2Int leftPos = wallPos + Vector2Int.left;
        Vector2Int rightPos = wallPos + Vector2Int.right;

        bool hasWallTop = wallSet.Contains(topPos);
        bool hasWallBottom = wallSet.Contains(bottomPos);
        bool hasWallLeft = wallSet.Contains(leftPos);
        bool hasWallRight = wallSet.Contains(rightPos);

        Debug.Log($"[RoomDragBuilder]   Wall neighbors: Top={hasWallTop}, Bottom={hasWallBottom}, Left={hasWallLeft}, Right={hasWallRight}");

        // ВАЖНО: Угол должен иметь РОВНО 2 стены-соседа под прямым углом
        int wallNeighborCount = (hasWallTop ? 1 : 0) + (hasWallBottom ? 1 : 0) + (hasWallLeft ? 1 : 0) + (hasWallRight ? 1 : 0);
        Debug.Log($"[RoomDragBuilder]   Total wall neighbors: {wallNeighborCount}");
        if (wallNeighborCount != 2)
        {
            Debug.Log($"[RoomDragBuilder]   REJECT: Need exactly 2 wall neighbors, has {wallNeighborCount}");
            return false;
        }

        // Проверяем, что 2 стены находятся под прямым углом (не напротив друг друга)
        bool hasVerticalPair = hasWallTop && hasWallBottom;
        bool hasHorizontalPair = hasWallLeft && hasWallRight;
        if (hasVerticalPair || hasHorizontalPair)
        {
            Debug.Log($"[RoomDragBuilder]   REJECT: Walls are opposite (not perpendicular). Vertical={hasVerticalPair}, Horizontal={hasHorizontalPair}");
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

        Debug.Log($"[RoomDragBuilder]   Deleted neighbors: Top={hasDeletedTop}, Bottom={hasDeletedBottom}, Left={hasDeletedLeft}, Right={hasDeletedRight}");
        Debug.Log($"[RoomDragBuilder]   Total deleted cells nearby: {deletedCells.Count}");

        if (!hasDeletedNeighbor)
        {
            Debug.Log($"[RoomDragBuilder]   REJECT: No deleted neighbor - this is outer corner, not inner corner");
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

        Debug.Log($"[RoomDragBuilder]   Diagonal floor: TopLeft={hasFloorTopLeft}, TopRight={hasFloorTopRight}, BottomLeft={hasFloorBottomLeft}, BottomRight={hasFloorBottomRight}");

        // Проверяем конфигурации внутренних углов
        // Внутренний угол имеет пол на противоположной диагонали от места соединения стен
        if (hasWallTop && hasWallRight)
        {
            Debug.Log($"[RoomDragBuilder]   Config: walls Top+Right -> need floor at BottomLeft. Has floor? {hasFloorBottomLeft}");
            // Стены сверху и справа -> пол должен быть слева снизу
            if (hasFloorBottomLeft)
            {
                Debug.Log($"[RoomDragBuilder]   ✓ MATCH inner corner at {wallPos} (walls: Top+Right, floor at BottomLeft)");
                return true;
            }
        }

        if (hasWallTop && hasWallLeft)
        {
            Debug.Log($"[RoomDragBuilder]   Config: walls Top+Left -> need floor at BottomRight. Has floor? {hasFloorBottomRight}");
            // Стены сверху и слева -> пол должен быть справа снизу
            if (hasFloorBottomRight)
            {
                Debug.Log($"[RoomDragBuilder]   ✓ MATCH inner corner at {wallPos} (walls: Top+Left, floor at BottomRight)");
                return true;
            }
        }

        if (hasWallBottom && hasWallRight)
        {
            Debug.Log($"[RoomDragBuilder]   Config: walls Bottom+Right -> need floor at TopLeft. Has floor? {hasFloorTopLeft}");
            // Стены снизу и справа -> пол должен быть слева сверху
            if (hasFloorTopLeft)
            {
                Debug.Log($"[RoomDragBuilder]   ✓ MATCH inner corner at {wallPos} (walls: Bottom+Right, floor at TopLeft)");
                return true;
            }
        }

        if (hasWallBottom && hasWallLeft)
        {
            Debug.Log($"[RoomDragBuilder]   Config: walls Bottom+Left -> need floor at TopRight. Has floor? {hasFloorTopRight}");
            // Стены снизу и слева -> пол должен быть справа сверху
            if (hasFloorTopRight)
            {
                Debug.Log($"[RoomDragBuilder]   ✓ MATCH inner corner at {wallPos} (walls: Bottom+Left, floor at TopRight)");
                return true;
            }
        }

        Debug.Log($"[RoomDragBuilder]   REJECT: No matching floor pattern for inner corner");
        return false;
    }

    /// <summary>
    /// Обновить ghost блоки после удаления
    /// </summary>
    void UpdateGhostsAfterDeletion()
    {
        // Очищаем ВСЕ старые блоки
        ClearGhostBlocks();
        ClearConfirmedBlocks();

        Debug.Log($"[RoomDragBuilder] ========== UPDATING GHOSTS AFTER DELETION ==========");
        Debug.Log($"[RoomDragBuilder] Total walls (perimeter): {roomPerimeter.Count}");
        Debug.Log($"[RoomDragBuilder] Total floor cells: {roomFloor.Count}");
        Debug.Log($"[RoomDragBuilder] Total deleted cells: {deletedCells.Count}");

        Debug.Log($"[RoomDragBuilder] === DELETED CELLS (these are EMPTY now) ===");
        foreach (Vector2Int deleted in deletedCells)
        {
            Debug.Log($"[RoomDragBuilder] DELETED: {deleted}");
        }

        int blockID = 1;

        // Создаем новые блоки в зависимости от состояния
        // Блоки создаются только на позициях из roomPerimeter и roomFloor
        // Удаленные клетки уже убраны из этих списков, поэтому на них блоки не создаются
        if (currentState == DragState.PreviewReady)
        {
            Debug.Log($"[RoomDragBuilder] === CREATING WALL BLOCKS (Build_Ghost) ===");
            // Если еще не подтверждено, создаем Build_Ghost
            foreach (Vector2Int cellPos in roomPerimeter)
            {
                GameObject ghostBlock = CreateGhostBlock(cellPos, buildGhostPrefab);
                ghostBlock.name = $"[WALL-{blockID}] Wall at {cellPos}";
                ghostBlocks.Add(ghostBlock);
                Debug.Log($"[RoomDragBuilder] [WALL-{blockID}] Position: {cellPos}");
                blockID++;
            }

            Debug.Log($"[RoomDragBuilder] === CREATING FLOOR BLOCKS (Floor_Ghost) ===");
            foreach (Vector2Int cellPos in roomFloor)
            {
                if (floorGhostPrefab != null)
                {
                    GameObject floorBlock = CreateGhostBlock(cellPos, floorGhostPrefab);
                    floorBlock.name = $"[FLOOR-{blockID}] Floor at {cellPos}";
                    ghostFloorBlocks.Add(floorBlock);
                    Debug.Log($"[RoomDragBuilder] [FLOOR-{blockID}] Position: {cellPos} ← ЕСЛИ ТУТ ДОЛЖЕН БЫТЬ ВНУТРЕННИЙ УГОЛ, СКАЖИ МНЕ ЭТОТ НОМЕР!");
                    blockID++;
                }
            }
        }
        else if (currentState == DragState.Confirmed)
        {
            Debug.Log($"[RoomDragBuilder] === CREATING WALL BLOCKS (Add_Build_Ghost) ===");
            // Если подтверждено, создаем Add_Build_Ghost
            foreach (Vector2Int cellPos in roomPerimeter)
            {
                GameObject confirmedBlock = CreateGhostBlock(cellPos, addBuildGhostPrefab);
                confirmedBlock.name = $"[WALL-{blockID}] Wall at {cellPos}";
                confirmedBlocks.Add(confirmedBlock);
                Debug.Log($"[RoomDragBuilder] [WALL-{blockID}] Position: {cellPos}");
                blockID++;
            }

            Debug.Log($"[RoomDragBuilder] === CREATING FLOOR BLOCKS (Add_Floor_Ghost) ===");
            if (addFloorGhostPrefab != null)
            {
                foreach (Vector2Int cellPos in roomFloor)
                {
                    GameObject confirmedFloorBlock = CreateGhostBlock(cellPos, addFloorGhostPrefab);
                    confirmedFloorBlock.name = $"[FLOOR-{blockID}] Floor at {cellPos}";
                    confirmedFloorBlocks.Add(confirmedFloorBlock);
                    Debug.Log($"[RoomDragBuilder] [FLOOR-{blockID}] Position: {cellPos} ← ЕСЛИ ТУТ ДОЛЖЕН БЫТЬ ВНУТРЕННИЙ УГОЛ, СКАЖИ МНЕ ЭТОТ НОМЕР!");
                    blockID++;
                }
            }
        }

        Debug.Log($"[RoomDragBuilder] ========== GHOSTS UPDATE COMPLETE ==========");
        Debug.Log($"[RoomDragBuilder] Total blocks created: {blockID - 1}");
        Debug.Log($"[RoomDragBuilder] Walls: {ghostBlocks.Count + confirmedBlocks.Count}, Floor: {ghostFloorBlocks.Count + confirmedFloorBlocks.Count}");
        Debug.Log($"[RoomDragBuilder] ====================================================");
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
}
