using UnityEngine;

/// <summary>
/// Компонент для ограничения движения объекта границами занимаемых им клеток сетки
/// </summary>
public class GridBounds : MonoBehaviour
{
    [Header("Grid Bounds Settings")]
    public bool enableBounds = true;
    public bool showDebugBounds = true;
    public Color boundsColor = Color.yellow;
    
    [Header("Bounds Info (Read Only)")]
    [SerializeField] private Vector2Int gridPosition;
    [SerializeField] private int gridWidth = 1;
    [SerializeField] private int gridHeight = 1;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector3 boundsCenter;
    [SerializeField] private Vector3 boundsSize;
    
    // Кэшированные компоненты
    private GridManager gridManager;
    private Transform cachedTransform;
    private Vector3 initialPosition;
    private bool isInitialized = false;
    
    // Границы движения
    private Bounds movementBounds;
    
    void Awake()
    {
        cachedTransform = transform;
        initialPosition = cachedTransform.position;
    }
    
    void Start()
    {
        InitializeBounds();
    }
    
    void Update()
    {
        if (!enableBounds || !isInitialized) return;
        
        // Проверяем и корректируем позицию объекта
        EnforceBounds();
    }
    
    /// <summary>
    /// Инициализация границ на основе занимаемых клеток сетки
    /// </summary>
    void InitializeBounds()
    {
        // Находим GridManager
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
            if (gridManager == null)
            {
                return;
            }
        }
        
        // Определяем какие клетки занимает объект
        DiscoverOccupiedCells();
        
        // Вычисляем границы движения
        CalculateMovementBounds();
        
        isInitialized = true;
        
    }
    
    /// <summary>
    /// Определение занимаемых объектом клеток
    /// </summary>
    void DiscoverOccupiedCells()
    {
        // Получаем текущую позицию в координатах сетки
        Vector2Int currentGridPos = gridManager.WorldToGrid(cachedTransform.position);
        
        // Ищем все ячейки, занятые этим объектом
        var occupiedCells = gridManager.GetCellsByObjectType(GetObjectType());
        
        Vector2Int minPos = currentGridPos;
        Vector2Int maxPos = currentGridPos;
        bool foundCells = false;
        
        foreach (var cell in occupiedCells)
        {
            if (cell.occupyingObject == gameObject)
            {
                if (!foundCells)
                {
                    minPos = cell.gridPosition;
                    maxPos = cell.gridPosition;
                    foundCells = true;
                }
                else
                {
                    minPos.x = Mathf.Min(minPos.x, cell.gridPosition.x);
                    minPos.y = Mathf.Min(minPos.y, cell.gridPosition.y);
                    maxPos.x = Mathf.Max(maxPos.x, cell.gridPosition.x);
                    maxPos.y = Mathf.Max(maxPos.y, cell.gridPosition.y);
                }
            }
        }
        
        if (foundCells)
        {
            gridPosition = minPos;
            gridWidth = maxPos.x - minPos.x + 1;
            gridHeight = maxPos.y - minPos.y + 1;
            cellSize = gridManager.cellSize;
        }
        else
        {
            // Если не нашли занятые ячейки, используем текущую позицию как единственную ячейку
            gridPosition = currentGridPos;
            gridWidth = 1;
            gridHeight = 1;
            cellSize = gridManager.cellSize;
            
        }
    }
    
    /// <summary>
    /// Вычисление границ движения
    /// </summary>
    void CalculateMovementBounds()
    {
        // Преобразуем координаты сетки в мировые координаты
        Vector3 worldMin = gridManager.GridToWorld(gridPosition);
        Vector3 worldMax = gridManager.GridToWorld(new Vector2Int(gridPosition.x + gridWidth - 1, gridPosition.y + gridHeight - 1));
        
        // Вычисляем центр и размер области
        boundsCenter = new Vector3(
            (worldMin.x + worldMax.x) * 0.5f,
            cachedTransform.position.y, // Y координата остается текущей
            (worldMin.z + worldMax.z) * 0.5f
        );
        
        boundsSize = new Vector3(
            (worldMax.x - worldMin.x) + cellSize,
            float.MaxValue, // Не ограничиваем по высоте
            (worldMax.z - worldMin.z) + cellSize
        );
        
        // Создаем Bounds для проверки
        movementBounds = new Bounds(boundsCenter, boundsSize);
    }
    
    /// <summary>
    /// Принуждение объекта оставаться в границах
    /// </summary>
    void EnforceBounds()
    {
        Vector3 currentPos = cachedTransform.position;
        Vector3 clampedPos = currentPos;
        
        // Ограничиваем X координату
        float halfSizeX = boundsSize.x * 0.5f;
        clampedPos.x = Mathf.Clamp(currentPos.x, boundsCenter.x - halfSizeX, boundsCenter.x + halfSizeX);
        
        // Ограничиваем Z координату
        float halfSizeZ = boundsSize.z * 0.5f;
        clampedPos.z = Mathf.Clamp(currentPos.z, boundsCenter.z - halfSizeZ, boundsCenter.z + halfSizeZ);
        
        // Применяем исправленную позицию если она изменилась
        if (Vector3.Distance(currentPos, clampedPos) > 0.01f)
        {
            cachedTransform.position = clampedPos;
        }
    }
    
    /// <summary>
    /// Получение типа объекта из LocationObjectInfo
    /// </summary>
    string GetObjectType()
    {
        var objectInfo = GetComponent<LocationObjectInfo>();
        return objectInfo != null ? objectInfo.objectType : "Unknown";
    }
    
    /// <summary>
    /// Ручная установка параметров сетки (для объектов, созданных программно)
    /// </summary>
    public void SetGridBounds(Vector2Int position, int width, int height, float size)
    {
        gridPosition = position;
        gridWidth = width;
        gridHeight = height;
        cellSize = size;
        
        if (gridManager != null)
        {
            CalculateMovementBounds();
            isInitialized = true;
        }
    }
    
    /// <summary>
    /// Включить/выключить ограничения
    /// </summary>
    public void SetBoundsEnabled(bool enabled)
    {
        enableBounds = enabled;
    }
    
    /// <summary>
    /// Получить текущие границы движения
    /// </summary>
    public Bounds GetMovementBounds()
    {
        return movementBounds;
    }
    
    void OnDrawGizmos()
    {
        if (!showDebugBounds || !isInitialized) return;
        
        // Рисуем границы движения
        Gizmos.color = boundsColor;
        Gizmos.DrawWireCube(boundsCenter, boundsSize);
        
        // Рисуем центр
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(boundsCenter, 0.5f);
        
        // Показываем информацию о сетке
        Gizmos.color = Color.white;
        Vector3 textPos = boundsCenter + Vector3.up * 2f;
        
#if UNITY_EDITOR
        UnityEditor.Handles.Label(textPos, $"Grid: {gridWidth}x{gridHeight}\nPos: {gridPosition}");
#endif
    }
    
    void OnDrawGizmosSelected()
    {
        if (!isInitialized) return;
        
        // Подробная визуализация при выделении
        Gizmos.color = Color.green;
        Gizmos.DrawCube(boundsCenter, boundsSize * 0.02f);
        
        // Рисуем клетки сетки
        Gizmos.color = Color.cyan;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector2Int cellPos = new Vector2Int(gridPosition.x + x, gridPosition.y + z);
                if (gridManager != null)
                {
                    Vector3 cellCenter = gridManager.GridToWorld(cellPos);
                    cellCenter.y = cachedTransform.position.y;
                    Gizmos.DrawWireCube(cellCenter, new Vector3(cellSize, 0.1f, cellSize));
                }
            }
        }
    }
}