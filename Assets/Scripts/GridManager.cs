using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GridCell
{
    public Vector2Int gridPosition;
    public Vector3 worldPosition;
    public bool isOccupied;
    public GameObject occupyingObject;
    public string objectType;
    
    public GridCell(Vector2Int gridPos, Vector3 worldPos)
    {
        gridPosition = gridPos;
        worldPosition = worldPos;
        isOccupied = false;
        occupyingObject = null;
        objectType = "";
    }
    
    public void SetOccupied(GameObject obj, string type)
    {
        isOccupied = true;
        occupyingObject = obj;
        objectType = type;
    }
    
    public void ClearOccupied()
    {
        isOccupied = false;
        occupyingObject = null;
        objectType = "";
    }
}

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 100;
    public int gridHeight = 100;
    public float cellSize = 10f;
    
    [Header("Visual")]
    public bool showGrid = true;
    public bool showOccupiedCells = true;
    public Color gridColor = Color.white;
    public Color occupiedCellColor = Color.red;
    
    [Header("Characters")]
    public int numberOfCharacters = 3;
    public GameObject characterPrefab;
    
    // Сетка центрирована в (0,0,0)
    private GridCell[,] grid;
    private Dictionary<Vector2Int, GridCell> gridLookup;
    
    // События
    public System.Action<GridCell, GameObject> OnCellOccupied;
    public System.Action<GridCell> OnCellFreed;
    
    void Awake()
    {
        InitializeGrid();
    }
    
    void Start()
    {
        SpawnCharacters();
        EnsureMovementController();
    }
    
    /// <summary>
    /// Обновить и пересоздать сетку с новыми параметрами
    /// </summary>
    public void UpdateGridSettings(int width, int height, float cellSizeNew)
    {
        gridWidth = width;
        gridHeight = height;
        cellSize = cellSizeNew;
        
        Debug.Log($"Обновление настроек сетки: {width}x{height}, размер ячейки: {cellSizeNew}");
        
        // Пересоздаем сетку
        InitializeGrid();
    }
    
    /// <summary>
    /// Инициализация сетки с центром в (0,0,0)
    /// </summary>
    void InitializeGrid()
    {
        grid = new GridCell[gridWidth, gridHeight];
        gridLookup = new Dictionary<Vector2Int, GridCell>();
        
        // Вычисляем смещение для центрирования сетки
        int halfWidth = gridWidth / 2;
        int halfHeight = gridHeight / 2;
        
        Debug.Log($"Инициализация сетки {gridWidth}x{gridHeight}, размер ячейки: {cellSize}");
        Debug.Log($"Диапазон X: {-halfWidth} до {halfWidth-1}, диапазон Z: {-halfHeight} до {halfHeight-1}");
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                // Координаты сетки относительно центра
                Vector2Int gridPos = new Vector2Int(x - halfWidth, z - halfHeight);
                
                // Мировые координаты ячейки (центр ячейки)
                Vector3 worldPos = new Vector3(gridPos.x * cellSize + cellSize * 0.5f, 0f, gridPos.y * cellSize + cellSize * 0.5f);
                
                GridCell cell = new GridCell(gridPos, worldPos);
                grid[x, z] = cell;
                gridLookup[gridPos] = cell;
            }
        }
        
        Debug.Log($"Сетка инициализирована: {gridLookup.Count} ячеек");
    }
    
    /// <summary>
    /// Получить ячейку по координатам сетки
    /// </summary>
    public GridCell GetCell(Vector2Int gridPosition)
    {
        if (gridLookup.TryGetValue(gridPosition, out GridCell cell))
        {
            return cell;
        }
        return null;
    }
    
    /// <summary>
    /// Получить ячейку по мировым координатам
    /// </summary>
    public GridCell GetCellFromWorldPosition(Vector3 worldPosition)
    {
        Vector2Int gridPos = WorldToGrid(worldPosition);
        return GetCell(gridPos);
    }
    
    /// <summary>
    /// Преобразование мировых координат в координаты сетки
    /// </summary>
    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt((worldPosition.x - cellSize * 0.5f) / cellSize);
        int z = Mathf.RoundToInt((worldPosition.z - cellSize * 0.5f) / cellSize);
        return new Vector2Int(x, z);
    }
    
    /// <summary>
    /// Преобразование координат сетки в мировые координаты (центр ячейки)
    /// </summary>
    public Vector3 GridToWorld(Vector2Int gridPosition)
    {
        return new Vector3(gridPosition.x * cellSize + cellSize * 0.5f, 0f, gridPosition.y * cellSize + cellSize * 0.5f);
    }
    
    /// <summary>
    /// Проверка валидности координат сетки
    /// </summary>
    public bool IsValidGridPosition(Vector2Int gridPosition)
    {
        return gridLookup.ContainsKey(gridPosition);
    }
    
    /// <summary>
    /// Проверка, свободна ли ячейка
    /// </summary>
    public bool IsCellFree(Vector2Int gridPosition)
    {
        GridCell cell = GetCell(gridPosition);
        return cell != null && !cell.isOccupied;
    }
    
    /// <summary>
    /// Занять ячейку объектом
    /// </summary>
    public bool OccupyCell(Vector2Int gridPosition, GameObject obj, string objectType)
    {
        GridCell cell = GetCell(gridPosition);
        if (cell != null && !cell.isOccupied)
        {
            cell.SetOccupied(obj, objectType);
            OnCellOccupied?.Invoke(cell, obj);
            Debug.Log($"Ячейка {gridPosition} занята объектом {obj.name} ({objectType})");
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Освободить ячейку
    /// </summary>
    public bool FreeCell(Vector2Int gridPosition)
    {
        GridCell cell = GetCell(gridPosition);
        if (cell != null && cell.isOccupied)
        {
            Debug.Log($"Ячейка {gridPosition} освобождена от {cell.objectType}");
            cell.ClearOccupied();
            OnCellFreed?.Invoke(cell);
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Получить случайную свободную ячейку
    /// </summary>
    public GridCell GetRandomFreeCell()
    {
        List<GridCell> freeCells = new List<GridCell>();
        
        foreach (var cell in gridLookup.Values)
        {
            if (!cell.isOccupied)
            {
                freeCells.Add(cell);
            }
        }
        
        if (freeCells.Count > 0)
        {
            return freeCells[Random.Range(0, freeCells.Count)];
        }
        
        return null;
    }
    
    /// <summary>
    /// Найти область для размещения объекта заданного размера
    /// </summary>
    public GridCell GetRandomFreeCellArea(int width, int height)
    {
        List<GridCell> validCells = new List<GridCell>();
        
        foreach (var cell in gridLookup.Values)
        {
            if (CanPlaceObjectAt(cell.gridPosition, width, height))
            {
                validCells.Add(cell);
            }
        }
        
        if (validCells.Count > 0)
        {
            return validCells[Random.Range(0, validCells.Count)];
        }
        
        return null;
    }
    
    /// <summary>
    /// Проверить, можно ли разместить объект заданного размера в указанной позиции
    /// </summary>
    public bool CanPlaceObjectAt(Vector2Int startPosition, int width, int height)
    {
        // Проверяем все ячейки в области размещения
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector2Int checkPos = new Vector2Int(startPosition.x + x, startPosition.y + z);
                
                // Проверяем валидность позиции
                if (!IsValidGridPosition(checkPos))
                    return false;
                
                // Проверяем занятость ячейки
                GridCell cell = GetCell(checkPos);
                if (cell == null || cell.isOccupied)
                    return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Занять область ячеек объектом
    /// </summary>
    public bool OccupyCellArea(Vector2Int startPosition, int width, int height, GameObject obj, string objectType)
    {
        // Сначала проверяем, можно ли разместить
        if (!CanPlaceObjectAt(startPosition, width, height))
        {
            return false;
        }
        
        // Занимаем все ячейки в области
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector2Int cellPos = new Vector2Int(startPosition.x + x, startPosition.y + z);
                GridCell cell = GetCell(cellPos);
                if (cell != null)
                {
                    cell.SetOccupied(obj, objectType);
                }
            }
        }
        
        Debug.Log($"Область {width}x{height} занята объектом {obj.name} начиная с ячейки {startPosition}");
        return true;
    }
    
    /// <summary>
    /// Освободить область ячеек
    /// </summary>
    public bool FreeCellArea(Vector2Int startPosition, int width, int height)
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector2Int cellPos = new Vector2Int(startPosition.x + x, startPosition.y + z);
                GridCell cell = GetCell(cellPos);
                if (cell != null)
                {
                    cell.ClearOccupied();
                }
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Получить свободные ячейки в радиусе от позиции
    /// </summary>
    public List<GridCell> GetFreeCellsInRadius(Vector2Int centerGrid, int radius)
    {
        List<GridCell> freeCells = new List<GridCell>();
        
        for (int x = centerGrid.x - radius; x <= centerGrid.x + radius; x++)
        {
            for (int z = centerGrid.y - radius; z <= centerGrid.y + radius; z++)
            {
                Vector2Int gridPos = new Vector2Int(x, z);
                GridCell cell = GetCell(gridPos);
                
                if (cell != null && !cell.isOccupied)
                {
                    freeCells.Add(cell);
                }
            }
        }
        
        return freeCells;
    }
    
    /// <summary>
    /// Получить все ячейки определенного типа объектов
    /// </summary>
    public List<GridCell> GetCellsByObjectType(string objectType)
    {
        List<GridCell> cells = new List<GridCell>();
        
        foreach (var cell in gridLookup.Values)
        {
            if (cell.isOccupied && cell.objectType == objectType)
            {
                cells.Add(cell);
            }
        }
        
        return cells;
    }
    
    /// <summary>
    /// Очистить всю сетку
    /// </summary>
    public void ClearGrid()
    {
        foreach (var cell in gridLookup.Values)
        {
            if (cell.isOccupied)
            {
                cell.ClearOccupied();
            }
        }
        Debug.Log("Сетка очищена");
    }
    
    /// <summary>
    /// Получить статистику сетки
    /// </summary>
    public void LogGridStats()
    {
        int occupiedCount = 0;
        Dictionary<string, int> typeCount = new Dictionary<string, int>();
        
        foreach (var cell in gridLookup.Values)
        {
            if (cell.isOccupied)
            {
                occupiedCount++;
                
                if (typeCount.ContainsKey(cell.objectType))
                    typeCount[cell.objectType]++;
                else
                    typeCount[cell.objectType] = 1;
            }
        }
        
        Debug.Log($"=== Статистика сетки ===");
        Debug.Log($"Всего ячеек: {gridLookup.Count}");
        Debug.Log($"Занято ячеек: {occupiedCount}");
        Debug.Log($"Свободно ячеек: {gridLookup.Count - occupiedCount}");
        
        foreach (var kvp in typeCount)
        {
            Debug.Log($"  {kvp.Key}: {kvp.Value} ячеек");
        }
    }
    
    /// <summary>
    /// Создать персонажей на сетке
    /// </summary>
    public void SpawnCharacters()
    {
        if (characterPrefab == null)
        {
            // Создаем префаб персонажа программно
            CreateCharacterPrefab();
        }
        
        // Очищаем список использованных имен для нового спавна
        Character.ClearUsedNames();
        
        Debug.Log($"Генерация {numberOfCharacters} персонажей с уникальными именами...");
        
        // Найдем область для размещения персонажей рядом друг с другом
        Vector2Int startPosition = FindSpawnAreaForCharacters();
        
        for (int i = 0; i < numberOfCharacters; i++)
        {
            // Размещаем персонажей в ряд
            Vector2Int spawnPosition = new Vector2Int(startPosition.x + i, startPosition.y);
            GridCell cell = GetCell(spawnPosition);
            
            if (cell != null && !cell.isOccupied)
            {
                // Создаем персонажа
                GameObject character = Instantiate(characterPrefab, cell.worldPosition, Quaternion.identity);
                character.name = $"Character_{i + 1}";
                
                // Добавляем компонент движения
                CharacterMovement movement = character.GetComponent<CharacterMovement>();
                if (movement == null)
                {
                    movement = character.AddComponent<CharacterMovement>();
                }
                
                // Занимаем ячейку
                OccupyCell(spawnPosition, character, "Character");
                
                Debug.Log($"Персонаж {i + 1} создан в позиции {spawnPosition} (мир: {cell.worldPosition})");
            }
            else
            {
                Debug.LogWarning($"Не удалось создать персонажа {i + 1} в позиции {spawnPosition} - ячейка занята или не существует");
            }
        }
        
        // Логируем статистику имен
        Character.LogNameStatistics();
    }
    
    /// <summary>
    /// Найти подходящую область для размещения персонажей
    /// </summary>
    Vector2Int FindSpawnAreaForCharacters()
    {
        // Пытаемся найти область возле центра сетки
        for (int attempt = 0; attempt < 100; attempt++)
        {
            Vector2Int testPosition = new Vector2Int(
                Random.Range(-10, 11 - numberOfCharacters),
                Random.Range(-10, 11)
            );
            
            // Проверяем, можем ли разместить всех персонажей в ряд
            bool canPlace = true;
            for (int i = 0; i < numberOfCharacters; i++)
            {
                Vector2Int checkPos = new Vector2Int(testPosition.x + i, testPosition.y);
                if (!IsValidGridPosition(checkPos) || !IsCellFree(checkPos))
                {
                    canPlace = false;
                    break;
                }
            }
            
            if (canPlace)
            {
                return testPosition;
            }
        }
        
        Debug.LogWarning("Не удалось найти подходящее место для размещения персонажей, используем случайную позицию");
        return Vector2Int.zero;
    }
    
    /// <summary>
    /// Создать префаб персонажа программно
    /// </summary>
    void CreateCharacterPrefab()
    {
        // Создаем временный объект для создания префаба
        GameObject tempCharacter = new GameObject("Character_Template");
        
        // Добавляем компоненты
        var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.transform.SetParent(tempCharacter.transform);
        capsule.transform.localPosition = Vector3.zero;
        capsule.name = "CharacterMesh";
        
        // Настраиваем размер капсулы
        capsule.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
        
        // Добавляем скрипт Character
        var characterScript = tempCharacter.AddComponent<Character>();
        characterScript.characterRenderer = capsule.GetComponent<Renderer>();
        
        // Добавляем Collider если его нет
        if (tempCharacter.GetComponent<Collider>() == null)
        {
            var collider = tempCharacter.AddComponent<CapsuleCollider>();
            collider.height = 2f;
            collider.radius = 0.4f;
        }
        
        characterPrefab = tempCharacter;
        
        Debug.Log("Префаб персонажа создан программно");
    }
    
    /// <summary>
    /// Обеспечение наличия MovementController в сцене
    /// </summary>
    void EnsureMovementController()
    {
        MovementController movementController = FindObjectOfType<MovementController>();
        if (movementController == null)
        {
            GameObject controllerGO = new GameObject("MovementController");
            movementController = controllerGO.AddComponent<MovementController>();
            Debug.Log("MovementController создан автоматически");
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showGrid || gridLookup == null) return;
        
        // Рисуем сетку
        Gizmos.color = gridColor;
        
        int halfWidth = gridWidth / 2;
        int halfHeight = gridHeight / 2;
        
        // Вертикальные линии
        for (int x = -halfWidth; x <= halfWidth; x++)
        {
            Vector3 start = new Vector3(x * cellSize, 0f, -halfHeight * cellSize);
            Vector3 end = new Vector3(x * cellSize, 0f, halfHeight * cellSize);
            Gizmos.DrawLine(start, end);
        }
        
        // Горизонтальные линии
        for (int z = -halfHeight; z <= halfHeight; z++)
        {
            Vector3 start = new Vector3(-halfWidth * cellSize, 0f, z * cellSize);
            Vector3 end = new Vector3(halfWidth * cellSize, 0f, z * cellSize);
            Gizmos.DrawLine(start, end);
        }
        
        // Рисуем занятые ячейки
        if (showOccupiedCells)
        {
            Gizmos.color = occupiedCellColor;
            foreach (var cell in gridLookup.Values)
            {
                if (cell.isOccupied)
                {
                    Vector3 cellCenter = cell.worldPosition;
                    Vector3 cellSize3D = new Vector3(cellSize * 0.8f, 0.5f, cellSize * 0.8f);
                    Gizmos.DrawCube(cellCenter, cellSize3D);
                }
            }
        }
        
        // Рисуем центр сетки
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Vector3.zero, 2f);
    }
}