using System.Collections;
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
    public bool showGridInGame = true; // Показывать грид в игре
    public Color gridColor = Color.white;
    public Color occupiedCellColor = Color.red;
    
    [Header("Characters")]
    public int numberOfCharacters = 3;
    public GameObject characterPrefab;

    [Header("Cockpit")]
    public GameObject cockpitPrefab;
    
    
    // Сетка центрирована в (0,0,0)
    private GridCell[,] grid;
    private Dictionary<Vector2Int, GridCell> gridLookup;
    
    // Визуализация грида в игре
    private GameObject gridLinesParent;
    private GameObject occupiedCellsParent;
    private Dictionary<Vector2Int, GameObject> occupiedCellVisuals = new Dictionary<Vector2Int, GameObject>();
    
    // События
    public System.Action<GridCell, GameObject> OnCellOccupied;
    public System.Action<GridCell> OnCellFreed;
    
    void Awake()
    {
        InitializeGrid();
    }
    
    void Start()
    {
        // Задержка для того чтобы LocationManager успел создать объекты
        StartCoroutine(DelayedStart());
    }
    
    System.Collections.IEnumerator DelayedStart()
    {
        yield return new WaitForEndOfFrame(); // Ждем конца кадра
        
        SpawnCharacters();
        EnsureMovementController();
        CreateGridVisualization();
    }
    
    /// <summary>
    /// Обновить и пересоздать сетку с новыми параметрами
    /// </summary>
    public void UpdateGridSettings(int width, int height, float cellSizeNew)
    {
        gridWidth = width;
        gridHeight = height;
        cellSize = cellSizeNew;
        
        
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
            
            // Обновляем визуализацию только для объектов-препятствий (не персонажей и не кокпита)
            if (showGridInGame && showOccupiedCells && objectType != "Character" && objectType != "Cockpit")
            {
                CreateOccupiedCellVisual(gridPosition);
            }
            
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
            cell.ClearOccupied();
            OnCellFreed?.Invoke(cell);
            
            // Обновляем визуализацию - убираем кубик при освобождении любой клетки
            if (showGridInGame && showOccupiedCells)
            {
                RemoveOccupiedCellVisual(gridPosition);
            }
            
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
    /// Получить все клетки сетки
    /// </summary>
    public Dictionary<Vector2Int, GridCell> GetAllCells()
    {
        return gridLookup;
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
                // НЕ очищаем клетки кокпита и персонажей - они должны сохраняться
                if (cell.objectType != "Cockpit" && cell.objectType != "Character")
                {
                    cell.ClearOccupied();
                }
            }
        }
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
        
        
        foreach (var kvp in typeCount)
        {
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
                
                
                // Занимаем ячейку
                OccupyCell(spawnPosition, character, "Character");
                
            }
            else
            {
                Debug.LogWarning($"Не удалось создать персонажа {i + 1} в позиции {spawnPosition} - ячейка занята или не существует");
            }
        }

        // Создаем кокпит рядом с персонажами
        SpawnCockpit(startPosition);

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
    /// Создать кокпит рядом с персонажами (размер 5x3 клетки)
    /// </summary>
    void SpawnCockpit(Vector2Int charactersStartPosition)
    {
        if (cockpitPrefab == null)
        {
            // Пытаемся загрузить префаб из папки Prefabs
            cockpitPrefab = Resources.Load<GameObject>("Prefabs/SM_Cockpit");

            if (cockpitPrefab == null)
            {
                Debug.LogWarning("Префаб кокпита не установлен и не найден в Resources/Prefabs/SM_Cockpit");
                return;
            }

        }

        // Размер кокпита: 5 в длину x 3 в ширину
        Vector2Int cockpitSize = new Vector2Int(5, 3);

        // Пытаемся разместить кокпит подальше от персонажей
        Vector2Int[] possiblePositions = new Vector2Int[]
        {
            new Vector2Int(charactersStartPosition.x - cockpitSize.x - 2, charactersStartPosition.y - 2),  // слева с отступом
            new Vector2Int(charactersStartPosition.x + numberOfCharacters + 2, charactersStartPosition.y - 2), // справа с отступом
            new Vector2Int(charactersStartPosition.x - 1, charactersStartPosition.y + 3),   // сверху с отступом
            new Vector2Int(charactersStartPosition.x - 1, charactersStartPosition.y - cockpitSize.y - 1),   // снизу с отступом
        };

        foreach (Vector2Int position in possiblePositions)
        {
            if (CanPlaceCockpit(position, cockpitSize))
            {
                GridCell centerCell = GetCell(position + new Vector2Int(cockpitSize.x / 2, cockpitSize.y / 2));
                if (centerCell != null)
                {
                    GameObject cockpit = Instantiate(cockpitPrefab, centerCell.worldPosition, Quaternion.identity);
                    cockpit.name = "SM_Cockpit";

                    // Занимаем все клетки под кокпитом
                    OccupyCells(position, cockpitSize, cockpit, "Cockpit");

                    return;
                }
            }
        }

        Debug.LogWarning("Не удалось найти подходящее место для размещения кокпита (5x3 клетки) рядом с персонажами");
    }

    /// <summary>
    /// Проверить, можно ли разместить кокпит в указанной позиции
    /// </summary>
    bool CanPlaceCockpit(Vector2Int startPosition, Vector2Int size)
    {
        // Проверяем все клетки в области размещения
        for (int x = startPosition.x; x < startPosition.x + size.x; x++)
        {
            for (int y = startPosition.y; y < startPosition.y + size.y; y++)
            {
                Vector2Int checkPos = new Vector2Int(x, y);
                if (!IsValidGridPosition(checkPos) || !IsCellFree(checkPos))
                {
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary>
    /// Занять несколько клеток под объектом (все клетки кокпита теперь непроходимы)
    /// </summary>
    void OccupyCells(Vector2Int startPosition, Vector2Int size, GameObject obj, string objectType)
    {
        int occupiedCount = 0;
        for (int x = startPosition.x; x < startPosition.x + size.x; x++)
        {
            for (int y = startPosition.y; y < startPosition.y + size.y; y++)
            {
                Vector2Int cellPos = new Vector2Int(x, y);
                if (IsValidGridPosition(cellPos))
                {
                    bool success = OccupyCell(cellPos, obj, objectType);
                    if (success)
                    {
                        occupiedCount++;
                    }
                    else
                    {
                        Debug.LogWarning($"Не удалось занять клетку {cellPos} для {objectType}");
                    }
                }
                else
                {
                    Debug.LogWarning($"Клетка {cellPos} недействительна для {objectType}");
                }
            }
        }
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

        // Скрываем префаб-шаблон, переместив его далеко от игровой области
        tempCharacter.transform.position = new Vector3(10000, 10000, 10000);

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
        }
    }
    
    /// <summary>
    /// Создать видимую визуализацию грида в игре
    /// </summary>
    void CreateGridVisualization()
    {
        if (!showGridInGame) return;
        
        // Удаляем старую визуализацию если есть
        if (gridLinesParent != null)
        {
            DestroyImmediate(gridLinesParent);
        }
        
        gridLinesParent = new GameObject("GridLines");
        gridLinesParent.transform.SetParent(transform);
        
        int halfWidth = gridWidth / 2;
        int halfHeight = gridHeight / 2;
        
        // Создаем вертикальные линии
        for (int x = -halfWidth; x <= halfWidth; x++)
        {
            CreateGridLine(
                new Vector3(x * cellSize, 0.01f, -halfHeight * cellSize),
                new Vector3(x * cellSize, 0.01f, halfHeight * cellSize),
                $"VerticalLine_{x}"
            );
        }
        
        // Создаем горизонтальные линии
        for (int z = -halfHeight; z <= halfHeight; z++)
        {
            CreateGridLine(
                new Vector3(-halfWidth * cellSize, 0.01f, z * cellSize),
                new Vector3(halfWidth * cellSize, 0.01f, z * cellSize),
                $"HorizontalLine_{z}"
            );
        }
        
        
        // Создаем визуализацию занятых клеток
        CreateOccupiedCellsVisualization();
    }
    
    /// <summary>
    /// Создать одну линию грида
    /// </summary>
    void CreateGridLine(Vector3 start, Vector3 end, string lineName)
    {
        GameObject lineObj = new GameObject(lineName);
        lineObj.transform.SetParent(gridLinesParent.transform);
        
        LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.material.color = gridColor;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.sortingOrder = -1; // Позади других объектов
        
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }
    
    /// <summary>
    /// Создать визуализацию занятых клеток в игре
    /// </summary>
    void CreateOccupiedCellsVisualization()
    {
        if (!showOccupiedCells || !showGridInGame) return;
        
        // Создаем родительский объект для занятых клеток
        if (occupiedCellsParent != null)
        {
            DestroyImmediate(occupiedCellsParent);
        }
        
        occupiedCellsParent = new GameObject("OccupiedCells");
        occupiedCellsParent.transform.SetParent(transform);
        
        // Очищаем словарь визуалов
        occupiedCellVisuals.Clear();
        
        // Создаем кубики для всех занятых клеток (кроме персонажей и кокпита)
        foreach (var cell in gridLookup.Values)
        {
            if (cell.isOccupied && cell.objectType != "Character" && cell.objectType != "Cockpit")
            {
                CreateOccupiedCellVisual(cell.gridPosition);
            }
        }
        
    }
    
    /// <summary>
    /// Создать визуализацию для одной занятой клетки
    /// </summary>
    void CreateOccupiedCellVisual(Vector2Int gridPos)
    {
        if (occupiedCellsParent == null || occupiedCellVisuals.ContainsKey(gridPos))
            return;
            
        var cell = GetCell(gridPos);
        if (cell == null || !cell.isOccupied) return;
        
        // Создаем куб
        GameObject cellVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cellVisual.name = $"OccupiedCell_{gridPos.x}_{gridPos.y}";
        cellVisual.transform.SetParent(occupiedCellsParent.transform);
        
        // Позиционируем и масштабируем
        cellVisual.transform.position = new Vector3(cell.worldPosition.x, 0.25f, cell.worldPosition.z);
        cellVisual.transform.localScale = new Vector3(cellSize * 0.8f, 0.5f, cellSize * 0.8f);
        
        // Убираем коллайдер чтобы не мешал
        Collider collider = cellVisual.GetComponent<Collider>();
        if (collider != null)
        {
            DestroyImmediate(collider);
        }
        
        // Настраиваем материал - красный полупрозрачный
        Renderer renderer = cellVisual.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = new Color(occupiedCellColor.r, occupiedCellColor.g, occupiedCellColor.b, 0.7f);
            
            // Настройки для прозрачности
            material.SetFloat("_Mode", 3); // Transparent mode
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            
            renderer.material = material;
        }
        
        occupiedCellVisuals[gridPos] = cellVisual;
    }
    
    /// <summary>
    /// Убрать визуализацию занятой клетки
    /// </summary>
    void RemoveOccupiedCellVisual(Vector2Int gridPos)
    {
        if (occupiedCellVisuals.ContainsKey(gridPos))
        {
            GameObject visual = occupiedCellVisuals[gridPos];
            if (visual != null)
            {
                DestroyImmediate(visual);
            }
            occupiedCellVisuals.Remove(gridPos);
        }
    }
    
    /// <summary>
    /// Скрыть/показать визуализацию грида в игре
    /// </summary>
    public void ToggleGridVisualization(bool show)
    {
        showGridInGame = show;
        
        if (gridLinesParent != null)
        {
            gridLinesParent.SetActive(show);
        }
        
        if (occupiedCellsParent != null)
        {
            occupiedCellsParent.SetActive(show && showOccupiedCells);
        }
        
        if (show && gridLinesParent == null)
        {
            CreateGridVisualization();
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