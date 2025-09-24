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
        // Блокируем ввод если открыт инвентарь
        if (!InventoryUI.IsAnyInventoryOpen)
        {
            HandleMovementInput();
        }
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
                return;
            }
            lastClickTime = Time.time;

            // Получаем выделенных персонажей
            List<Character> selectedCharacters = GetSelectedCharacters();

            if (selectedCharacters.Count > 0)
            {
                // Проверяем, есть ли враг под курсором - если да, то не выполняем обычное движение
                Character enemyUnderMouse = GetEnemyUnderMouse();
                if (enemyUnderMouse != null)
                {

                    return; // Позволяем EnemyTargetingSystem обработать клик
                }
                // Получаем позицию клика на сетке
                Vector3 clickWorldPos = GetMouseWorldPosition();
                if (clickWorldPos != Vector3.zero)
                {
                    Vector2Int targetGridPos = gridManager.WorldToGrid(clickWorldPos);
                    
                    // Проверяем, свободна ли целевая клетка
                    Vector2Int finalTargetPos = GetNearestFreeCell(targetGridPos, selectedCharacters);
                    Vector3 targetWorldPos = gridManager.GridToWorld(finalTargetPos);
                    
                    
                    // Очистка движений только для выделенных персонажей
                    ClearMovingGroupsForSelectedCharacters(selectedCharacters);
                    
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
                if (character != null && character.IsPlayerCharacter())
                {
                    // Добавляем только персонажей игрока для управления
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
        
        // Создаем новую группу движения (предыдущие движения выделенных персонажей уже очищены)
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
                // Первый персонаж идет к основной цели, но только если она свободна
                if (IsCellPassableForTarget(targetGridPos) || IsOccupiedBySelectedCharacter(targetGridPos, characters))
                {
                    finalPos = targetGridPos;
                    finalWorldPos = gridManager.GridToWorld(targetGridPos);
                    group.targetOccupied = true;
                }
                else
                {
                    // Цель занята, находим ближайшую свободную клетку
                    finalPos = GetNextNearbyPosition(targetGridPos, 0);
                    finalWorldPos = gridManager.GridToWorld(finalPos);
                }
            }
            else
            {
                // Остальные идут к соседним позициям
                finalPos = GetNextNearbyPosition(targetGridPos, i - 1);
                finalWorldPos = gridManager.GridToWorld(finalPos);
            }
            
            // Подписываемся на событие завершения движения с учетом финальной позиции
            var movement = movements[i];
            var targetPos = finalPos;
            movement.OnMovementComplete += (completedMovement) => OnCharacterArrivedAtFinalPosition(completedMovement, targetPos, targetGridPos);

            // Уведомляем AI о том, что движение инициировано игроком
            var characterAI = movement.GetComponent<CharacterAI>();
            if (characterAI != null)
            {
                characterAI.OnPlayerInitiatedMovement();
            }

            // Отправляем к финальной позиции
            movement.MoveTo(finalWorldPos);
        }
        
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
        // Проверяем, свободна ли целевая клетка ИЛИ занята выделенным персонажем
        if (IsCellPassableForTarget(targetPos))
        {
            return targetPos;
        }

        // Если клетка занята выделенным персонажем, он освободит её
        if (IsOccupiedBySelectedCharacter(targetPos, characters))
        {
            return targetPos;
        }

        // Клетка занята другим персонажем или препятствием, ищем альтернативу
        
        
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
                // Проверяем что клетка свободна
                if (IsCellPassableForTarget(pos) || IsOccupiedBySelectedCharacter(pos, characters))
                {
                    return pos;
                }
            }
        }
        
        return targetPos;
    }
    
    /// <summary>
    /// Проверить, подходит ли клетка для размещения цели (полностью свободна)
    /// </summary>
    bool IsCellPassableForTarget(Vector2Int pos)
    {
        if (!gridManager.IsValidGridPosition(pos))
            return false;

        var cell = gridManager.GetCell(pos);

        // Клетка должна быть полностью свободна для размещения персонажей
        return cell == null || !cell.isOccupied;
    }

    /// <summary>
    /// Проверить, занята ли клетка одним из выделенных персонажей
    /// </summary>
    bool IsOccupiedBySelectedCharacter(Vector2Int pos, List<Character> selectedCharacters)
    {
        var cell = gridManager.GetCell(pos);
        if (cell == null || !cell.isOccupied || cell.objectType != "Character")
            return false;

        // Проверяем, является ли занимающий клетку персонаж одним из выделенных
        foreach (var character in selectedCharacters)
        {
            Vector2Int charCurrentPos = gridManager.WorldToGrid(character.transform.position);
            if (charCurrentPos == pos)
            {
                return true; // Этот персонаж освободит клетку при движении
            }
        }

        return false;
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
                    catch (System.Exception)
                    {
                        // Ignore cleanup errors
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
        }
        
    }
    
    /// <summary>
    /// Полная очистка всех активных групп движения
    /// </summary>
    void ClearAllMovingGroups()
    {
        
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
                    catch (System.Exception)
                    {
                        // Ignore movement stop errors
                    }
                }
            }
        }
        
        // Полностью очищаем словарь групп
        movingGroups.Clear();
        
    }

    /// <summary>
    /// Очистка групп движения только для выделенных персонажей
    /// </summary>
    void ClearMovingGroupsForSelectedCharacters(List<Character> selectedCharacters)
    {
        List<Vector2Int> groupsToRemove = new List<Vector2Int>();

        foreach (var kvp in movingGroups)
        {
            Vector2Int targetPos = kvp.Key;
            MovementGroup group = kvp.Value;
            List<CharacterMovement> charactersToRemove = new List<CharacterMovement>();

            // Находим выделенных персонажей в этой группе
            foreach (var character in group.allCharacters)
            {
                if (character != null && character.gameObject != null)
                {
                    Character charComponent = character.GetComponent<Character>();
                    if (charComponent != null && selectedCharacters.Contains(charComponent))
                    {
                        // Этот персонаж выделен, останавливаем его движение
                        charactersToRemove.Add(character);
                        character.OnMovementComplete = null;

                        try
                        {
                            character.StopMovement();
                        }
                        catch (System.Exception)
                        {
                            // Ignore movement stop errors
                        }
                    }
                }
            }

            // Удаляем выделенных персонажей из группы
            foreach (var charToRemove in charactersToRemove)
            {
                group.allCharacters.Remove(charToRemove);
                group.arrivedCharacters.Remove(charToRemove);
            }

            // Если в группе не осталось персонажей, помечаем её для удаления
            if (group.allCharacters.Count == 0)
            {
                groupsToRemove.Add(targetPos);
            }
        }

        // Удаляем пустые группы
        foreach (var targetPos in groupsToRemove)
        {
            movingGroups.Remove(targetPos);
        }
    }

    /// <summary>
    /// Получить врага под курсором мыши
    /// </summary>
    Character GetEnemyUnderMouse()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        Camera camera = Camera.main;

        if (camera == null) return null;

        Ray ray = camera.ScreenPointToRay(mouseScreenPos);
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);



        // Сортируем хиты по расстоянию (ближайшие первые)
        System.Array.Sort(hits, (hit1, hit2) => hit1.distance.CompareTo(hit2.distance));

        foreach (RaycastHit hit in hits)
        {
            // Игнорируем Location_Bounds и другие системные объекты
            if (hit.collider.name.Contains("Location_Bounds") ||
                hit.collider.name.Contains("Grid") ||
                hit.collider.name.Contains("Terrain"))
            {
                continue;
            }



            // Сначала проверяем непосредственно в коллайдере
            Character character = hit.collider.GetComponent<Character>();

            // Если не нашли, ищем в родительских объектах
            if (character == null)
            {
                character = hit.collider.GetComponentInParent<Character>();
            }

            // Если все еще не нашли, ищем в дочерних объектах
            if (character == null)
            {
                Transform parent = hit.collider.transform;
                while (parent != null && character == null)
                {
                    character = parent.GetComponent<Character>();
                    parent = parent.parent;
                }
            }

            if (character != null && character.IsEnemyCharacter())
            {

                return character;
            }
        }


        return null;
    }
}