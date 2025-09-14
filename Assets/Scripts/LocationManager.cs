using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LocationData
{
    public string locationName;
    public Vector2Int gridSize;
    public Vector3 playerSpawnPoint;
    public List<Vector3> pointsOfInterest;
    public bool isGenerated;
}

[System.Serializable]
public class ObjectSpawnSettings
{
    [Header("Stations")]
    public GameObject[] stationPrefabs;
    public int minStations = 0;
    public int maxStations = 2;
    
    [Header("Asteroids")]
    public GameObject[] asteroidPrefabs;
    public int minAsteroids = 8;
    public int maxAsteroids = 12;
    
    [Header("Debris")]
    public GameObject[] debrisPrefabs;
    public int minDebris = 15;
    public int maxDebris = 25;
}

public class LocationManager : MonoBehaviour
{
    [Header("Location Settings")]
    public LocationData currentLocation;
    public Vector2Int defaultGridSize = new Vector2Int(150, 150); // Оптимизированный размер
    public float gridCellSize = 1f;
    
    [Header("Generation")]
    public ObjectSpawnSettings spawnSettings;
    public Transform contentParent;
    public bool autoGenerateOnStart = true;
    public float minDistanceBetweenObjects = 20f;
    public int maxPlacementAttempts = 100;
    
    [Header("Player")]
    public Transform playerShip;
    public float spawnSafeDistance = 50f;
    
    [Header("Grid System")]
    public GridManager gridManager;
    
    // Внутренние переменные
    private List<GameObject> spawnedObjects = new List<GameObject>();
    private Dictionary<Vector2Int, GameObject> gridObjects = new Dictionary<Vector2Int, GameObject>();
    
    // События
    public System.Action<LocationData> OnLocationGenerated;
    public System.Action OnLocationCleared;
    
    void Start()
    {
        InitializeLocation();
        
        if (autoGenerateOnStart)
        {
            GenerateLocation();
        }
    }
    
    /// <summary>
    /// Инициализация локации с базовыми настройками
    /// </summary>
    public void InitializeLocation()
    {
        if (currentLocation == null)
        {
            currentLocation = new LocationData();
            currentLocation.locationName = "VoidFrontier Location";
            currentLocation.gridSize = defaultGridSize;
            currentLocation.isGenerated = false;
            currentLocation.pointsOfInterest = new List<Vector3>();
        }
        
        // Убеждаемся что размер сетки не нулевой
        if (currentLocation.gridSize.x == 0 || currentLocation.gridSize.y == 0)
        {
            currentLocation.gridSize = defaultGridSize;
        }
        
        // Создаем родительский объект для содержимого если его нет
        if (contentParent == null)
        {
            GameObject contentGO = new GameObject("LocationContent");
            contentGO.transform.SetParent(transform);
            contentParent = contentGO.transform;
        }
        
        // Инициализируем GridManager если его нет
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
            if (gridManager == null)
            {
                GameObject gridGO = new GameObject("GridManager");
                gridGO.transform.SetParent(transform);
                gridManager = gridGO.AddComponent<GridManager>();
            }
        }
        
        // Синхронизируем размеры между GridManager и LocationManager
        if (gridManager != null)
        {
            gridManager.UpdateGridSettings(currentLocation.gridSize.x, currentLocation.gridSize.y, gridCellSize);
        }
    }
    
    /// <summary>
    /// Создание тестовых префабов если они не установлены
    /// </summary>
    void EnsureTestPrefabs()
    {
        // Создаем тестовые префабы если массивы пустые
        if (spawnSettings.stationPrefabs == null || spawnSettings.stationPrefabs.Length == 0)
        {
            GameObject stationPrefab = CreateTestPrefab("TestStation", Color.blue, new Vector3(8, 4, 8));
            spawnSettings.stationPrefabs = new GameObject[] { stationPrefab };
        }
        
        if (spawnSettings.asteroidPrefabs == null || spawnSettings.asteroidPrefabs.Length == 0)
        {
            GameObject asteroidPrefab = CreateTestPrefab("TestAsteroid", Color.gray, new Vector3(6, 3, 6));
            spawnSettings.asteroidPrefabs = new GameObject[] { asteroidPrefab };
        }
        
        if (spawnSettings.debrisPrefabs == null || spawnSettings.debrisPrefabs.Length == 0)
        {
            GameObject debrisPrefab = CreateTestPrefab("TestDebris", Color.yellow, new Vector3(0.8f, 0.8f, 0.8f));
            spawnSettings.debrisPrefabs = new GameObject[] { debrisPrefab };
        }
    }
    
    /// <summary>
    /// Создание тестового префаба в виде куба
    /// </summary>
    GameObject CreateTestPrefab(string name, Color color, Vector3 size)
    {
        GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        prefab.name = name;
        prefab.transform.localScale = size;
        
        // Убеждаемся что коллайдер есть и настроен правильно
        BoxCollider collider = prefab.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = prefab.AddComponent<BoxCollider>();
        }
        
        // Устанавливаем цвет
        Renderer renderer = prefab.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = color;
            renderer.material = material;
        }
        
        // Добавляем компонент LocationObjectInfo
        LocationObjectInfo objectInfo = prefab.GetComponent<LocationObjectInfo>();
        if (objectInfo == null)
        {
            objectInfo = prefab.AddComponent<LocationObjectInfo>();
        }
        
        // Настраиваем информацию об объекте
        if (name.Contains("Station"))
        {
            objectInfo.objectType = "Station";
            objectInfo.objectName = "Test Station";
            objectInfo.health = 500f;
            objectInfo.isDestructible = false;
        }
        else if (name.Contains("Asteroid"))
        {
            objectInfo.objectType = "Asteroid";
            objectInfo.objectName = "Test Asteroid";
            objectInfo.health = 200f;
            objectInfo.canBeScavenged = true;
        }
        else if (name.Contains("Debris"))
        {
            objectInfo.objectType = "Debris";
            objectInfo.objectName = "Test Debris";
            objectInfo.health = 50f;
            objectInfo.canBeScavenged = true;
        }
        
        // Скрываем префаб
        prefab.SetActive(false);
        
        
        return prefab;
    }
    
    
    /// <summary>
    /// Генерация всего содержимого локации
    /// </summary>
    public void GenerateLocation()
    {
        ClearLocation();
        
        currentLocation.isGenerated = true;
        
        
        // Диагностика префабов
        if (spawnSettings == null)
        {
            Debug.LogError("SpawnSettings не установлены!");
            return;
        }
        
        
        // Проверяем и создаем тестовые префабы если они не установлены
        EnsureTestPrefabs();
        
        // Генерируем объекты
        GenerateStations();
        GenerateAsteroids();
        GenerateDebris();
        
        // Определяем точку спавна игрока
        SetPlayerSpawnPoint();
        
        // Создаем точки интереса
        GeneratePointsOfInterest();
        
        
        // Выводим статистику сетки
        if (gridManager != null)
        {
            gridManager.LogGridStats();
        }
        
        // Диагностика созданных объектов
        DiagnoseCreatedObjects();
        
        OnLocationGenerated?.Invoke(currentLocation);
    }
    
    /// <summary>
    /// Генерация станций
    /// </summary>
    void GenerateStations()
    {
        
        if (spawnSettings.stationPrefabs == null || spawnSettings.stationPrefabs.Length == 0) 
        {
            Debug.LogWarning("Нет префабов станций для генерации");
            return;
        }
        
        int stationCount = Random.Range(spawnSettings.minStations, spawnSettings.maxStations + 1);
        
        int createdCount = 0;
        for (int i = 0; i < stationCount; i++)
        {
            
            // Станции занимают 20x20 клеток (20x20 метров)
            GridCell cell = gridManager.GetRandomFreeCellArea(20, 20);
            if (cell == null) 
            {
                Debug.LogWarning($"Не удалось найти свободную область 20x20 для станции {i+1}");
                continue;
            }
            
            GameObject stationPrefab = spawnSettings.stationPrefabs[Random.Range(0, spawnSettings.stationPrefabs.Length)];
            
            if (stationPrefab != null)
            {
                // Вычисляем точный центр области 20x20
                Vector3 areaCenterOffset = new Vector3(
                    (20 - 1) * 0.5f * gridCellSize,  // (количество клеток - 1) / 2 * размер клетки
                    0,
                    (20 - 1) * 0.5f * gridCellSize
                );
                Vector3 stationPosition = cell.worldPosition + areaCenterOffset;
                
                // Без поворота - используем поворот префаба
                GameObject station = Instantiate(stationPrefab, stationPosition, Quaternion.identity, contentParent);
                station.SetActive(true);
                
                // Занимаем область 20x20 в сетке
                gridManager.OccupyCellArea(cell.gridPosition, 20, 20, station, "Station");
                
                RegisterObject(station, "Station");
                
                currentLocation.pointsOfInterest.Add(stationPosition);
                createdCount++;
            }
            else
            {
                Debug.LogError($"stationPrefab[{i}] равен null!");
            }
        }
        
    }
    
    /// <summary>
    /// Генерация астероидов
    /// </summary>
    void GenerateAsteroids()
    {
        
        if (spawnSettings.asteroidPrefabs == null || spawnSettings.asteroidPrefabs.Length == 0) 
        {
            Debug.LogWarning("Нет префабов астероидов для генерации");
            return;
        }
        
        int asteroidCount = Random.Range(spawnSettings.minAsteroids, spawnSettings.maxAsteroids + 1);
        
        int createdCount = 0;
        for (int i = 0; i < asteroidCount; i++)
        {
            
            // Астероиды занимают 8x8 клеток (8x8 метров)
            GridCell cell = gridManager.GetRandomFreeCellArea(8, 8);
            if (cell == null) 
            {
                Debug.LogWarning($"Не удалось найти свободную область 8x8 для астероида {i+1}");
                continue;
            }
            
            GameObject asteroidPrefab = spawnSettings.asteroidPrefabs[Random.Range(0, spawnSettings.asteroidPrefabs.Length)];
            
            if (asteroidPrefab != null)
            {
                // Вычисляем точный центр области 8x8
                Vector3 areaCenterOffset = new Vector3(
                    (8 - 1) * 0.5f * gridCellSize,   // (количество клеток - 1) / 2 * размер клетки
                    0,
                    (8 - 1) * 0.5f * gridCellSize
                );
                Vector3 asteroidPosition = cell.worldPosition + areaCenterOffset;
                
                // Без поворота - используем поворот префаба
                GameObject asteroid = Instantiate(asteroidPrefab, asteroidPosition, Quaternion.identity, contentParent);
                asteroid.SetActive(true);
                
                // Занимаем область 8x8 в сетке
                gridManager.OccupyCellArea(cell.gridPosition, 8, 8, asteroid, "Asteroid");
                
                RegisterObject(asteroid, "Asteroid");
                
                createdCount++;
            }
            else
            {
                Debug.LogError($"asteroidPrefab[{i}] равен null!");
            }
        }
        
    }
    
    /// <summary>
    /// Генерация обломков
    /// </summary>
    void GenerateDebris()
    {
        if (spawnSettings.debrisPrefabs == null || spawnSettings.debrisPrefabs.Length == 0) 
        {
            return;
        }
        
        int debrisCount = Random.Range(spawnSettings.minDebris, spawnSettings.maxDebris + 1);
        
        int createdCount = 0;
        for (int i = 0; i < debrisCount; i++)
        {
            
            // Обломки занимают 1 клетку и размещаются в центре
            GridCell cell = GetRandomGridCell("Debris");
            if (cell == null) 
            {
                Debug.LogWarning($"Не удалось найти свободную ячейку для обломка {i+1}");
                continue;
            }
            
            GameObject debrisPrefab = spawnSettings.debrisPrefabs[Random.Range(0, spawnSettings.debrisPrefabs.Length)];
            
            if (debrisPrefab != null)
            {
                // Размещаем точно в центре единственной ячейки с ограниченным поворотом
                Quaternion limitedRotation = Quaternion.Euler(
                    Random.Range(-15f, 15f),    // Небольшой наклон по X
                    Random.Range(0f, 360f),     // Полный поворот по Y
                    Random.Range(-15f, 15f)     // Небольшой наклон по Z
                );
                GameObject debris = Instantiate(debrisPrefab, cell.worldPosition, limitedRotation, contentParent);
                debris.SetActive(true);
                
                // Занимаем ячейку в сетке
                gridManager.OccupyCell(cell.gridPosition, debris, "Debris");
                
                RegisterObject(debris, "Debris");
                
                createdCount++;
            }
            else
            {
                Debug.LogError($"debrisPrefab[{i}] равен null!");
            }
        }
        
    }
    
    /// <summary>
    /// Установка точки спавна игрока
    /// </summary>
    void SetPlayerSpawnPoint()
    {
        Vector3 spawnPoint = Vector3.zero; // Инициализируем значением по умолчанию
        int attempts = 0;
        int maxAttempts = 50;
        
        
        do
        {
            GridCell playerCell = GetRandomGridCell("Player");
            if (playerCell != null)
            {
                spawnPoint = playerCell.worldPosition;
                // Занимаем ячейку для игрока (только если playerShip не null)
                if (playerShip != null)
                {
                    gridManager.OccupyCell(playerCell.gridPosition, playerShip.gameObject, "Player");
                }
                break;
            }
            attempts++;
        }
        while (attempts < maxAttempts);
        
        currentLocation.playerSpawnPoint = spawnPoint;
        
        
        // Размещаем игрока если он есть
        if (playerShip != null)
        {
            playerShip.position = spawnPoint;
        }
        else
        {
        }
    }
    
    /// <summary>
    /// Генерация точек интереса
    /// </summary>
    void GeneratePointsOfInterest()
    {
        // Точки интереса уже добавляются при создании станций
        // Здесь можно добавить дополнительные точки интереса
    }
    
    /// <summary>
    /// Получение случайной позиции в пределах локации (старый метод, оставлен для совместимости)
    /// </summary>
    Vector3 GetRandomLocationPosition()
    {
        // Теперь используем сетку для получения позиции
        if (gridManager != null)
        {
            GridCell cell = gridManager.GetRandomFreeCell();
            if (cell != null)
            {
                return cell.worldPosition;
            }
        }
        
        // Fallback на старый метод если сетка недоступна
        float halfWidth = (currentLocation.gridSize.x * gridCellSize) * 0.5f;
        float halfHeight = (currentLocation.gridSize.y * gridCellSize) * 0.5f;
        
        float x = Random.Range(-halfWidth, halfWidth);
        float z = Random.Range(-halfHeight, halfHeight);
        
        Vector3 position = new Vector3(x, 0, z);
        
        return position;
    }
    
    /// <summary>
    /// Получение случайной свободной ячейки сетки для размещения объекта
    /// </summary>
    GridCell GetRandomGridCell(string objectType = "")
    {
        if (gridManager == null)
        {
            Debug.LogWarning("GridManager не найден!");
            return null;
        }
        
        GridCell cell = gridManager.GetRandomFreeCell();
        if (cell != null)
        {
        }
        else
        {
            Debug.LogWarning($"Не найдено свободных ячеек для {objectType}");
        }
        
        return cell;
    }
    
    /// <summary>
    /// Получение безопасной позиции с учетом минимального расстояния до других объектов
    /// </summary>
    Vector3 GetSafeSpawnPosition(float minDistance = 0f)
    {
        if (minDistance <= 0f)
            minDistance = minDistanceBetweenObjects;
            
        Vector3 position;
        int attempts = 0;
        
        do
        {
            position = GetRandomLocationPosition();
            attempts++;
            
            if (IsPositionSafe(position, minDistance))
                break;
                
        } while (attempts < maxPlacementAttempts);
        
        if (attempts >= maxPlacementAttempts)
        {
            Debug.LogWarning($"Не удалось найти безопасную позицию за {maxPlacementAttempts} попыток. Используется последняя сгенерированная позиция.");
        }
        
        return position;
    }
    
    /// <summary>
    /// Проверка безопасности позиции (нет объектов в заданном радиусе)
    /// </summary>
    bool IsPositionSafe(Vector3 position, float minDistance)
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null && Vector3.Distance(position, obj.transform.position) < minDistance)
            {
                return false;
            }
        }
        return true;
    }
    
    /// <summary>
    /// Проверка безопасности позиции для игрока
    /// </summary>
    bool IsPositionSafeForPlayer(Vector3 position)
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null && Vector3.Distance(position, obj.transform.position) < spawnSafeDistance)
            {
                return false;
            }
        }
        return true;
    }
    
    /// <summary>
    /// Регистрация объекта в системе
    /// </summary>
    void RegisterObject(GameObject obj, string objectType)
    {
        spawnedObjects.Add(obj);
        
        // Добавляем компонент для идентификации типа объекта вместо тега
        var objectInfo = obj.GetComponent<LocationObjectInfo>();
        if (objectInfo == null)
        {
            objectInfo = obj.AddComponent<LocationObjectInfo>();
            objectInfo.objectType = objectType;
            
            // Устанавливаем имя если оно не задано
            if (string.IsNullOrEmpty(objectInfo.objectName))
            {
                objectInfo.objectName = obj.name;
            }
        }
        
        // Проверяем наличие коллайдера для raycast
        Collider collider = obj.GetComponent<Collider>();
        if (collider == null)
        {
            Debug.LogWarning($"Объект {obj.name} не имеет коллайдера и не будет доступен для выделения!");
        }
        else
        {
        }
    }
    
    /// <summary>
    /// Очистка локации
    /// </summary>
    public void ClearLocation()
    {
        // Удаляем все созданные объекты, кроме персонажей
        List<GameObject> charactersToPreserve = new List<GameObject>();
        
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
            {
                // Проверяем, является ли объект персонажем
                Character character = obj.GetComponent<Character>();
                if (character != null)
                {
                    // Сохраняем персонажа
                    charactersToPreserve.Add(obj);
                }
                else
                {
                    // Удаляем все остальные объекты
                    DestroyImmediate(obj);
                }
            }
        }
        
        // Очищаем списки и восстанавливаем персонажей
        spawnedObjects.Clear();
        gridObjects.Clear();
        
        // Добавляем персонажей обратно в список
        foreach (GameObject character in charactersToPreserve)
        {
            spawnedObjects.Add(character);
            
            // Регистрируем персонажа в сетке заново
            Character charComponent = character.GetComponent<Character>();
            if (charComponent != null && gridManager != null)
            {
                Vector2Int gridPos = gridManager.WorldToGrid(character.transform.position);
                gridObjects[gridPos] = character;
            }
        }
        
        // Очищаем сетку
        if (gridManager != null)
        {
            gridManager.ClearGrid();
        }
        
        if (currentLocation != null)
        {
            currentLocation.pointsOfInterest.Clear();
            currentLocation.isGenerated = false;
        }
        
        OnLocationCleared?.Invoke();
    }
    
    /// <summary>
    /// Преобразование мировых координат в координаты сетки
    /// </summary>
    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt((worldPosition.x - gridCellSize * 0.5f) / gridCellSize);
        int z = Mathf.RoundToInt((worldPosition.z - gridCellSize * 0.5f) / gridCellSize);
        return new Vector2Int(x, z);
    }
    
    /// <summary>
    /// Преобразование координат сетки в мировые координаты (центр ячейки)
    /// </summary>
    public Vector3 GridToWorld(Vector2Int gridPosition)
    {
        return new Vector3(gridPosition.x * gridCellSize + gridCellSize * 0.5f, 0f, gridPosition.y * gridCellSize + gridCellSize * 0.5f);
    }
    
    /// <summary>
    /// Проверка валидности позиции сетки
    /// </summary>
    public bool IsValidGridPosition(Vector2Int gridPosition)
    {
        int halfWidth = currentLocation.gridSize.x / 2;
        int halfHeight = currentLocation.gridSize.y / 2;
        
        return gridPosition.x >= -halfWidth && gridPosition.x < halfWidth &&
               gridPosition.y >= -halfHeight && gridPosition.y < halfHeight;
    }
    
    /// <summary>
    /// Получение всех объектов определенного типа
    /// </summary>
    public List<GameObject> GetObjectsByType(string objectType)
    {
        List<GameObject> result = new List<GameObject>();
        
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
            {
                var objectInfo = obj.GetComponent<LocationObjectInfo>();
                if (objectInfo != null && objectInfo.objectType == objectType)
                {
                    result.Add(obj);
                }
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Сохранение состояния локации
    /// </summary>
    public string SaveLocationState()
    {
        return JsonUtility.ToJson(currentLocation, true);
    }
    
    /// <summary>
    /// Загрузка состояния локации
    /// </summary>
    public void LoadLocationState(string json)
    {
        if (!string.IsNullOrEmpty(json))
        {
            currentLocation = JsonUtility.FromJson<LocationData>(json);
            // После загрузки можно регенерировать локацию если нужно
        }
    }
    
    /// <summary>
    /// Диагностика созданных объектов для отладки выделения
    /// </summary>
    void DiagnoseCreatedObjects()
    {
        
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj == null) continue;
            
            
            // Проверяем коллайдеры
            Collider[] colliders = obj.GetComponents<Collider>();
            foreach (var collider in colliders)
            {
                if (collider is BoxCollider box)
                {
                }
            }
            
            // Проверяем LocationObjectInfo
            LocationObjectInfo objectInfo = obj.GetComponent<LocationObjectInfo>();
            if (objectInfo != null)
            {
            }
            else
            {
                Debug.LogWarning("LocationObjectInfo отсутствует!");
            }
            
            // Проверяем слой
        }
        
    }
    
    void OnDrawGizmos()
    {
        if (currentLocation != null)
        {
            // Рисуем границы локации
            Gizmos.color = Color.cyan;
            float halfWidth = (currentLocation.gridSize.x * gridCellSize) * 0.5f;
            float halfHeight = (currentLocation.gridSize.y * gridCellSize) * 0.5f;
            
            Vector3 center = transform.position;
            Vector3 size = new Vector3(halfWidth * 2, 1f, halfHeight * 2);
            Gizmos.DrawWireCube(center, size);
            
            // Рисуем точку спавна игрока
            if (currentLocation.isGenerated)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(currentLocation.playerSpawnPoint, 5f);
                
                // Рисуем точки интереса
                Gizmos.color = Color.yellow;
                foreach (Vector3 poi in currentLocation.pointsOfInterest)
                {
                    Gizmos.DrawWireSphere(poi, 3f);
                }
            }
        }
    }
}