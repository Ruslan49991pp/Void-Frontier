using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Контроллер для управления движением группы персонажей
/// </summary>
public class MovementController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float formationRadius = 2f;
    public bool showTargetIndicators = true;
    public GameObject targetIndicatorPrefab;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    // Компоненты
    private GridManager gridManager;
    private SelectionManager selectionManager;
    
    // Управление индикаторами
    private List<GameObject> targetIndicators = new List<GameObject>();
    
    // События
    public System.Action<List<Character>, Vector2Int> OnMovementCommand;
    
    void Awake()
    {
        gridManager = FindObjectOfType<GridManager>();
        selectionManager = FindObjectOfType<SelectionManager>();
        
        CreateTargetIndicatorPrefab();
    }
    
    void Start()
    {
        // Подписываемся на события SelectionManager если он есть
        if (selectionManager != null)
        {
            // Подписка на изменения выделения для очистки индикаторов
            selectionManager.OnSelectionChanged += OnSelectionChanged;
        }
    }
    
    void Update()
    {
        HandleMovementInput();
    }
    
    /// <summary>
    /// Обработка ввода для движения
    /// </summary>
    void HandleMovementInput()
    {
        // Проверяем нажатие ПКМ
        if (Input.GetMouseButtonDown(1))
        {
            // Получаем выделенных персонажей
            List<Character> selectedCharacters = GetSelectedCharacters();
            
            if (selectedCharacters.Count > 0)
            {
                // Получаем позицию клика точно на плоскости Y=0 (уровень сетки)
                Vector3 clickWorldPos = GetMouseWorldPosition();
                if (clickWorldPos != Vector3.zero)
                {
                    Vector2Int targetGridPos = gridManager.WorldToGrid(clickWorldPos);
                    
                    Debug.Log($"MovementController: ПКМ клик в мировую позицию {clickWorldPos}, клетка сетки {targetGridPos}");
                    
                    // Выполняем команду движения
                    ExecuteMovementCommand(selectedCharacters, targetGridPos);
                }
            }
        }
    }
    
    /// <summary>
    /// Получение мировой позиции мыши на плоскости сетки (Y=0)
    /// </summary>
    Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        Camera camera = Camera.main;
        
        if (camera == null)
        {
            Debug.LogError("MovementController: основная камера не найдена!");
            return Vector3.zero;
        }
        
        // Создаем луч от камеры через позицию мыши
        Ray ray = camera.ScreenPointToRay(mouseScreenPos);
        
        // Создаем плоскость на уровне Y=0 (уровень сетки)
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        
        // Находим пересечение луча с плоскостью
        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 worldPosition = ray.GetPoint(distance);
            Debug.Log($"GetMouseWorldPosition: мышь {mouseScreenPos} -> мир {worldPosition}");
            return worldPosition;
        }
        
        Debug.LogWarning("MovementController: не удалось определить позицию мыши на плоскости сетки");
        return Vector3.zero;
    }
    
    /// <summary>
    /// Получение списка выделенных персонажей
    /// </summary>
    List<Character> GetSelectedCharacters()
    {
        List<Character> characters = new List<Character>();
        
        if (selectionManager != null)
        {
            var selectedObjects = selectionManager.GetSelectedObjects();
            foreach (var obj in selectedObjects)
            {
                Character character = obj.GetComponent<Character>();
                if (character != null)
                {
                    characters.Add(character);
                }
            }
        }
        
        return characters;
    }
    
    /// <summary>
    /// Выполнение команды движения группы персонажей
    /// </summary>
    public void ExecuteMovementCommand(List<Character> characters, Vector2Int targetGridPosition)
    {
        if (characters == null || characters.Count == 0)
        {
            Debug.LogWarning("MovementController: нет персонажей для перемещения");
            return;
        }
        
        Debug.Log($"MovementController: команда движения для {characters.Count} персонажей к клетке {targetGridPosition}");
        
        // Очищаем предыдущие индикаторы
        ClearTargetIndicators();
        
        // Находим оптимальные позиции для всех персонажей
        var assignments = AssignCharactersToPositions(characters, targetGridPosition);
        
        // Перемещаем персонажей к назначенным позициям
        foreach (var assignment in assignments)
        {
            MoveCharacterToPosition(assignment.Key, assignment.Value);
        }
        
        // Создаем визуальные индикаторы
        if (showTargetIndicators)
        {
            CreateTargetIndicators(assignments.Values.ToList());
        }
        
        // Вызываем событие
        OnMovementCommand?.Invoke(characters, targetGridPosition);
    }
    
    /// <summary>
    /// Назначение персонажей к оптимальным позициям
    /// </summary>
    Dictionary<Character, Vector2Int> AssignCharactersToPositions(List<Character> characters, Vector2Int targetGridPosition)
    {
        var assignments = new Dictionary<Character, Vector2Int>();
        var occupiedPositions = new HashSet<Vector2Int>();
        
        // 1. Находим ближайшего персонажа к целевой позиции
        Character closestCharacter = FindClosestCharacter(characters, targetGridPosition);
        
        // 2. Назначаем ближайшего персонажа в целевую клетку (если она свободна)
        if (gridManager.IsCellFree(targetGridPosition))
        {
            assignments[closestCharacter] = targetGridPosition;
            occupiedPositions.Add(targetGridPosition);
            Debug.Log($"  Ближайший персонаж {closestCharacter.GetFullName()} назначен в целевую клетку {targetGridPosition}");
        }
        else
        {
            Debug.LogWarning($"  Целевая клетка {targetGridPosition} занята, ищем альтернативу для {closestCharacter.GetFullName()}");
        }
        
        // 3. Назначаем остальных персонажей в соседние свободные клетки
        var remainingCharacters = characters.Where(c => !assignments.ContainsKey(c)).ToList();
        var availablePositions = GetAvailablePositionsAroundTarget(targetGridPosition, occupiedPositions, remainingCharacters.Count + (assignments.Count == 0 ? 1 : 0));
        
        // Если целевая клетка была занята, добавляем ближайшего персонажа в очередь
        if (!assignments.ContainsKey(closestCharacter))
        {
            remainingCharacters.Insert(0, closestCharacter);
        }
        
        // Сортируем оставшихся персонажей по расстоянию до центра
        remainingCharacters = remainingCharacters
            .OrderBy(c => Vector2Int.Distance(gridManager.WorldToGrid(c.transform.position), targetGridPosition))
            .ToList();
        
        // Назначаем оставшихся персонажей
        for (int i = 0; i < remainingCharacters.Count && i < availablePositions.Count; i++)
        {
            assignments[remainingCharacters[i]] = availablePositions[i];
            Debug.Log($"  Персонаж {remainingCharacters[i].GetFullName()} назначен в соседнюю клетку {availablePositions[i]}");
        }
        
        return assignments;
    }
    
    /// <summary>
    /// Поиск ближайшего персонажа к целевой позиции
    /// </summary>
    Character FindClosestCharacter(List<Character> characters, Vector2Int targetGridPosition)
    {
        Character closest = null;
        float minDistance = float.MaxValue;
        var charactersAtSameDistance = new List<Character>();
        
        foreach (var character in characters)
        {
            Vector2Int characterGridPos = gridManager.WorldToGrid(character.transform.position);
            float distance = Vector2Int.Distance(characterGridPos, targetGridPosition);
            
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = character;
                charactersAtSameDistance.Clear();
                charactersAtSameDistance.Add(character);
            }
            else if (Mathf.Approximately(distance, minDistance))
            {
                charactersAtSameDistance.Add(character);
            }
        }
        
        // Если несколько персонажей на одинаковом расстоянии, выбираем случайного
        if (charactersAtSameDistance.Count > 1)
        {
            closest = charactersAtSameDistance[Random.Range(0, charactersAtSameDistance.Count)];
            Debug.Log($"  Несколько персонажей на расстоянии {minDistance}, выбран случайно: {closest.GetFullName()}");
        }
        
        return closest;
    }
    
    /// <summary>
    /// Получение доступных позиций вокруг цели
    /// </summary>
    List<Vector2Int> GetAvailablePositionsAroundTarget(Vector2Int centerPosition, HashSet<Vector2Int> occupiedPositions, int maxPositions)
    {
        var availablePositions = new List<Vector2Int>();
        var checkedPositions = new HashSet<Vector2Int>(occupiedPositions);
        
        // Поиск в расширяющихся кругах
        for (int radius = 1; radius <= 10 && availablePositions.Count < maxPositions; radius++)
        {
            var positionsAtRadius = GetPositionsAtRadius(centerPosition, radius);
            
            foreach (var pos in positionsAtRadius)
            {
                if (checkedPositions.Contains(pos)) continue;
                checkedPositions.Add(pos);
                
                if (gridManager.IsValidGridPosition(pos) && gridManager.IsCellFree(pos))
                {
                    availablePositions.Add(pos);
                    if (availablePositions.Count >= maxPositions) break;
                }
            }
        }
        
        // Сортируем по расстоянию до центра для лучшего формирования
        availablePositions = availablePositions
            .OrderBy(pos => Vector2Int.Distance(pos, centerPosition))
            .ToList();
        
        return availablePositions;
    }
    
    /// <summary>
    /// Получение позиций на определенном радиусе от центра
    /// </summary>
    List<Vector2Int> GetPositionsAtRadius(Vector2Int center, int radius)
    {
        var positions = new List<Vector2Int>();
        
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                // Проверяем, что позиция находится примерно на нужном радиусе
                float distance = Mathf.Sqrt(x * x + y * y);
                if (distance >= radius - 0.5f && distance <= radius + 0.5f)
                {
                    positions.Add(new Vector2Int(center.x + x, center.y + y));
                }
            }
        }
        
        return positions;
    }
    
    /// <summary>
    /// Перемещение персонажа к позиции
    /// </summary>
    void MoveCharacterToPosition(Character character, Vector2Int gridPosition)
    {
        // Добавляем или получаем компонент движения
        CharacterMovement movement = character.GetComponent<CharacterMovement>();
        if (movement == null)
        {
            movement = character.gameObject.AddComponent<CharacterMovement>();
        }
        
        // Запускаем движение
        movement.MoveToGridCell(gridPosition);
    }
    
    /// <summary>
    /// Создание префаба индикатора цели
    /// </summary>
    void CreateTargetIndicatorPrefab()
    {
        if (targetIndicatorPrefab == null)
        {
            targetIndicatorPrefab = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            targetIndicatorPrefab.name = "TargetIndicator";
            targetIndicatorPrefab.transform.localScale = new Vector3(0.8f, 0.1f, 0.8f);
            
            // Убираем коллайдер
            DestroyImmediate(targetIndicatorPrefab.GetComponent<Collider>());
            
            // Настраиваем материал
            var renderer = targetIndicatorPrefab.GetComponent<Renderer>();
            var material = new Material(Shader.Find("Standard"));
            material.color = new Color(0f, 1f, 0f, 0.7f); // Зеленый полупрозрачный
            material.SetFloat("_Mode", 3); // Transparent
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            renderer.material = material;
            
            targetIndicatorPrefab.SetActive(false);
        }
    }
    
    /// <summary>
    /// Создание визуальных индикаторов цели
    /// </summary>
    void CreateTargetIndicators(List<Vector2Int> positions)
    {
        foreach (var gridPos in positions)
        {
            // Получаем точный центр клетки через GridManager
            Vector3 worldPos = gridManager.GridToWorld(gridPos);
            worldPos.y += 0.1f; // Немного приподнимаем над землей
            
            GameObject indicator = Instantiate(targetIndicatorPrefab, worldPos, Quaternion.identity);
            indicator.SetActive(true);
            targetIndicators.Add(indicator);
            
            Debug.Log($"Создан индикатор цели в клетке {gridPos}, мировая позиция {worldPos}");
        }
        
        // Автоматически удаляем индикаторы через некоторое время
        StartCoroutine(ClearIndicatorsAfterDelay(3f));
    }
    
    /// <summary>
    /// Очистка индикаторов цели
    /// </summary>
    void ClearTargetIndicators()
    {
        foreach (var indicator in targetIndicators)
        {
            if (indicator != null)
            {
                DestroyImmediate(indicator);
            }
        }
        targetIndicators.Clear();
    }
    
    /// <summary>
    /// Очистка индикаторов через задержку
    /// </summary>
    System.Collections.IEnumerator ClearIndicatorsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ClearTargetIndicators();
    }
    
    /// <summary>
    /// Обработчик изменения выделения
    /// </summary>
    void OnSelectionChanged(List<GameObject> selectedObjects)
    {
        // При смене выделения очищаем индикаторы
        ClearTargetIndicators();
    }
    
    void OnDestroy()
    {
        ClearTargetIndicators();
        
        if (selectionManager != null)
        {
            selectionManager.OnSelectionChanged -= OnSelectionChanged;
        }
    }
}