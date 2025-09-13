using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Контроллер движения персонажей по ПКМ
/// </summary>
public class MovementController : MonoBehaviour
{
    private GridManager gridManager;
    private SelectionManager selectionManager;
    
    // Группы персонажей, движущихся к одной цели
    private Dictionary<Vector2Int, MovementGroup> movingGroups = new Dictionary<Vector2Int, MovementGroup>();
    
    // Защита от спама кликов
    private float lastClickTime = 0f;
    private const float CLICK_COOLDOWN = 0.1f;
    
    // Класс для отслеживания группы персонажей
    private class MovementGroup
    {
        public List<CharacterMovement> allCharacters = new List<CharacterMovement>();
        public List<CharacterMovement> arrivedCharacters = new List<CharacterMovement>();
        public bool targetOccupied = false;
    }
    
    void Awake()
    {
        gridManager = FindObjectOfType<GridManager>();
        selectionManager = FindObjectOfType<SelectionManager>();
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
        if (Input.GetMouseButtonDown(1)) // ПКМ
        {
            // Защита от спама кликов
            if (Time.time - lastClickTime < CLICK_COOLDOWN)
            {
                Debug.Log("MovementController: клик проигнорирован - слишком быстро");
                return;
            }
            lastClickTime = Time.time;
            // Получаем выделенных персонажей
            List<Character> selectedCharacters = GetSelectedCharacters();
            
            if (selectedCharacters.Count > 0)
            {
                // Получаем позицию клика на сетке
                Vector3 clickWorldPos = GetMouseWorldPosition();
                if (clickWorldPos != Vector3.zero)
                {
                    Vector2Int targetGridPos = gridManager.WorldToGrid(clickWorldPos);
                    
                    // Проверяем, свободна ли целевая клетка
                    Vector2Int finalTargetPos = GetNearestFreeCell(targetGridPos, selectedCharacters);
                    Vector3 targetWorldPos = gridManager.GridToWorld(finalTargetPos);
                    
                    Debug.Log($"ПКМ клик в клетку {targetGridPos}, финальная цель {finalTargetPos}");
                    
                    // ПОЛНАЯ ОЧИСТКА перед новой командой
                    ClearAllMovingGroups();
                    
                    // Дополнительно останавливаем движение у всех выделенных персонажей
                    foreach (var character in selectedCharacters)
                    {
                        CharacterMovement movement = character.GetComponent<CharacterMovement>();
                        if (movement != null)
                        {
                            movement.StopMovement();
                        }
                    }
                    
                    // Запускаем движение персонажей к цели
                    MoveCharactersToTarget(selectedCharacters, finalTargetPos);
                }
            }
        }
    }
    
    /// <summary>
    /// Получение мировой позиции мыши на плоскости сетки
    /// </summary>
    Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        Camera camera = Camera.main;
        
        if (camera == null) return Vector3.zero;
        
        Ray ray = camera.ScreenPointToRay(mouseScreenPos);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        
        if (groundPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        
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
    /// Запустить движение персонажей к цели
    /// </summary>
    void MoveCharactersToTarget(List<Character> characters, Vector2Int targetGridPos)
    {
        // Добавляем компонент движения персонажам если его нет
        List<CharacterMovement> movements = new List<CharacterMovement>();
        
        foreach (var character in characters)
        {
            CharacterMovement movement = character.GetComponent<CharacterMovement>();
            if (movement == null)
            {
                movement = character.gameObject.AddComponent<CharacterMovement>();
            }
            movements.Add(movement);
        }
        
        // Создаем новую группу движения (предыдущие уже очищены в ClearAllMovingGroups)
        MovementGroup group = new MovementGroup();
        group.allCharacters = movements;
        movingGroups[targetGridPos] = group;
        
        // Назначаем каждому персонажу свою финальную позицию
        for (int i = 0; i < movements.Count; i++)
        {
            Vector2Int finalPos;
            Vector3 finalWorldPos;
            
            if (i == 0)
            {
                // Первый персонаж идет к основной цели
                finalPos = targetGridPos;
                finalWorldPos = gridManager.GridToWorld(targetGridPos);
                group.targetOccupied = true;
                Debug.Log($"Персонаж {movements[i].name} назначен к основной цели {targetGridPos}");
            }
            else
            {
                // Остальные идут к соседним позициям
                finalPos = GetNextNearbyPosition(targetGridPos, i - 1);
                finalWorldPos = gridManager.GridToWorld(finalPos);
                Debug.Log($"Персонаж {movements[i].name} назначен к соседней позиции {finalPos}");
            }
            
            // Подписываемся на событие завершения движения с учетом финальной позиции
            var movement = movements[i];
            var targetPos = finalPos;
            movement.OnMovementComplete += (completedMovement) => OnCharacterArrivedAtFinalPosition(completedMovement, targetPos, targetGridPos);
            
            // Отправляем к финальной позиции
            movement.MoveTo(finalWorldPos);
        }
        
        Debug.Log($"Запущено движение {movements.Count} персонажей: 1 к основной цели {targetGridPos}, {movements.Count - 1} к соседним позициям");
    }
    
    /// <summary>
    /// Обработчик прибытия персонажа к финальной позиции
    /// </summary>
    void OnCharacterArrivedAtFinalPosition(CharacterMovement arrivedCharacter, Vector2Int arrivedPosition, Vector2Int originalTargetGridPos)
    {
        if (!movingGroups.ContainsKey(originalTargetGridPos))
            return;
            
        MovementGroup group = movingGroups[originalTargetGridPos];
        
        // Добавляем персонажа в список прибывших
        if (!group.arrivedCharacters.Contains(arrivedCharacter))
        {
            group.arrivedCharacters.Add(arrivedCharacter);
            Debug.Log($"Персонаж {arrivedCharacter.name} прибыл к финальной позиции {arrivedPosition}");
        }
        
        // Если все прибыли, очищаем группу
        if (group.arrivedCharacters.Count >= group.allCharacters.Count)
        {
            // Отписываемся от событий
            foreach (var movement in group.allCharacters)
            {
                if (movement != null)
                {
                    movement.OnMovementComplete = null; // Полная очистка событий
                }
            }
            movingGroups.Remove(originalTargetGridPos);
            Debug.Log($"Группа движения к {originalTargetGridPos} завершена, все {group.arrivedCharacters.Count} персонажей прибыли");
        }
    }
    
    /// <summary>
    /// Получить следующую соседнюю позицию по индексу
    /// </summary>
    Vector2Int GetNextNearbyPosition(Vector2Int center, int index)
    {
        // Порядок размещения: право, низ, лево, верх, затем по диагоналям
        Vector2Int[] offsets = {
            new Vector2Int(1, 0),   // право
            new Vector2Int(0, -1),  // низ
            new Vector2Int(-1, 0),  // лево
            new Vector2Int(0, 1),   // верх
            new Vector2Int(1, -1),  // право-низ
            new Vector2Int(-1, -1), // лево-низ
            new Vector2Int(-1, 1),  // лево-верх
            new Vector2Int(1, 1),   // право-верх
        };
        
        // Если нужно больше позиций, расширяем радиус
        if (index < offsets.Length)
        {
            Vector2Int pos = center + offsets[index];
            if (gridManager.IsValidGridPosition(pos))
            {
                return pos;
            }
        }
        
        // Для большего количества персонажей - расширяем поиск
        int radius = 2;
        int currentIndex = index - offsets.Length;
        
        while (radius <= 5)
        {
            var expandedPositions = GetPositionsAtRadius(center, radius);
            if (currentIndex < expandedPositions.Count)
            {
                return expandedPositions[currentIndex];
            }
            currentIndex -= expandedPositions.Count;
            radius++;
        }
        
        // Если ничего не найдено, возвращаем позицию справа
        return center + Vector2Int.right;
    }
    
    /// <summary>
    /// Получить позиции на определенном радиусе от центра
    /// </summary>
    List<Vector2Int> GetPositionsAtRadius(Vector2Int center, int radius)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                // Проверяем, что это граница текущего радиуса
                if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius) continue;
                
                Vector2Int pos = new Vector2Int(center.x + x, center.y + y);
                
                if (gridManager.IsValidGridPosition(pos))
                {
                    positions.Add(pos);
                }
            }
        }
        
        // Сортируем по расстоянию до центра
        return positions.OrderBy(pos => Vector2Int.Distance(pos, center)).ToList();
    }
    
    /// <summary>
    /// Найти ближайшую свободную клетку к целевой позиции
    /// </summary>
    Vector2Int GetNearestFreeCell(Vector2Int targetPos, List<Character> characters)
    {
        // Если целевая клетка свободна или содержит только персонажей, используем её
        if (IsCellPassableForTarget(targetPos))
        {
            return targetPos;
        }
        
        Debug.Log($"Клетка {targetPos} занята, ищем ближайшую свободную...");
        
        // Определяем среднюю позицию персонажей для выбора направления
        Vector2 avgCharacterPos = Vector2.zero;
        foreach (var character in characters)
        {
            Vector2Int charGridPos = gridManager.WorldToGrid(character.transform.position);
            avgCharacterPos += new Vector2(charGridPos.x, charGridPos.y);
        }
        avgCharacterPos /= characters.Count;
        
        // Ищем ближайшую свободную клетку, предпочитая сторону от персонажей
        for (int radius = 1; radius <= 10; radius++)
        {
            var positionsAtRadius = GetPositionsAtRadius(targetPos, radius);
            
            // Сортируем позиции по близости к персонажам (ближе к персонажам = выше приоритет)
            var sortedPositions = positionsAtRadius.OrderBy(pos => 
            {
                Vector2 posVector = new Vector2(pos.x, pos.y);
                return Vector2.Distance(posVector, avgCharacterPos);
            }).ToList();
            
            foreach (var pos in sortedPositions)
            {
                // Проверяем что клетка свободна или содержит только персонажей
                if (IsCellPassableForTarget(pos))
                {
                    Debug.Log($"Найдена свободная клетка {pos} на расстоянии {radius} от {targetPos}");
                    return pos;
                }
            }
        }
        
        Debug.LogWarning($"Не удалось найти свободную клетку рядом с {targetPos}, используем исходную позицию");
        return targetPos;
    }
    
    /// <summary>
    /// Проверить, подходит ли клетка для размещения цели (свободна или содержит персонажей)
    /// </summary>
    bool IsCellPassableForTarget(Vector2Int pos)
    {
        if (!gridManager.IsValidGridPosition(pos))
            return false;
            
        var cell = gridManager.GetCell(pos);
        if (cell == null || !cell.isOccupied)
            return true;
            
        // Для целевых позиций персонажи не являются препятствиями
        return cell.objectType == "Character";
    }
    
    /// <summary>
    /// Очистить предыдущие движения для указанных персонажей
    /// </summary>
    void CleanupMovingCharacters(List<CharacterMovement> characters)
    {
        // Находим и удаляем эти персонажи из всех активных групп движения
        List<Vector2Int> groupsToRemove = new List<Vector2Int>();
        
        foreach (var kvp in movingGroups)
        {
            Vector2Int targetPos = kvp.Key;
            MovementGroup group = kvp.Value;
            
            // Отписываем события и останавливаем движение для персонажей из нового списка
            foreach (var character in characters)
            {
                if (character != null && group.allCharacters.Contains(character))
                {
                    try
                    {
                        // Отписываемся от событий
                        character.OnMovementComplete = null;
                        
                        // Останавливаем движение
                        character.StopMovement();
                        
                        // Убираем из группы
                        group.allCharacters.Remove(character);
                        group.arrivedCharacters.Remove(character);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Ошибка при очистке движения {character.name}: {e.Message}");
                    }
                }
            }
            
            // Если группа стала пустой, помечаем для удаления
            if (group.allCharacters.Count == 0)
            {
                groupsToRemove.Add(targetPos);
            }
        }
        
        // Удаляем пустые группы
        foreach (var targetPos in groupsToRemove)
        {
            movingGroups.Remove(targetPos);
            Debug.Log($"Удалена пустая группа движения к {targetPos}");
        }
        
        Debug.Log($"Очищены предыдущие движения для {characters.Count} персонажей");
    }
    
    /// <summary>
    /// Полная очистка всех активных групп движения
    /// </summary>
    void ClearAllMovingGroups()
    {
        Debug.Log("Полная очистка всех групп движения...");
        
        // Останавливаем все движения и отписываемся от событий
        foreach (var kvp in movingGroups)
        {
            Vector2Int targetPos = kvp.Key;
            MovementGroup group = kvp.Value;
            
            foreach (var character in group.allCharacters)
            {
                if (character != null && character.gameObject != null)
                {
                    // Отписываемся от ВСЕХ возможных событий
                    character.OnMovementComplete = null;
                    
                    // Останавливаем движение только если объект еще существует
                    try
                    {
                        character.StopMovement();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Ошибка при остановке движения {character.name}: {e.Message}");
                    }
                }
            }
        }
        
        // Полностью очищаем словарь групп
        movingGroups.Clear();
        
        Debug.Log("Все группы движения очищены");
    }
}