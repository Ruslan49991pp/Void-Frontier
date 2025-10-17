using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Управление добычей ресурсов из астероидов
/// </summary>
public class MiningManager : MonoBehaviour
{
    [Header("Mining Settings")]
    public float miningJumpInterval = 0.8f; // Интервал между прыжками (уменьшено для более частых прыжков)
    public int metalPerJump = 10; // Металла за один прыжок
    public float jumpHeight = 0.5f; // Высота прыжка
    public float jumpDuration = 0.3f; // Длительность прыжка

    [Header("References")]
    private GridManager gridManager;

    // Данные о добыче
    [System.Serializable]
    public class MiningTask
    {
        public Character character;
        public GameObject asteroid;
        public LocationObjectInfo asteroidInfo;
        public Vector2Int asteroidGridPosition;
        public bool isActive;
        public Coroutine miningCoroutine;
    }

    private List<MiningTask> miningTasks = new List<MiningTask>();

    void Awake()
    {
        gridManager = FindObjectOfType<GridManager>();
    }

    /// <summary>
    /// Начать добычу ресурса
    /// </summary>
    public void StartMining(Character character, GameObject asteroid)
    {
        Debug.Log($"[MiningManager] StartMining called for character: {(character != null ? character.GetFullName() : "NULL")}, asteroid: {(asteroid != null ? asteroid.name : "NULL")}");

        if (character == null || asteroid == null)
        {
            Debug.LogWarning("[MiningManager] Character or asteroid is null");
            return;
        }

        // Проверяем, есть ли уже задача для этого персонажа
        MiningTask existingTask = miningTasks.Find(t => t.character == character);
        if (existingTask != null)
        {
            Debug.Log($"[MiningManager] Character already has a mining task, stopping it");
            // Останавливаем предыдущую задачу
            StopMiningForCharacter(character);
        }

        // Проверяем, что астероид имеет ресурсы
        LocationObjectInfo asteroidInfo = asteroid.GetComponent<LocationObjectInfo>();
        if (asteroidInfo == null)
        {
            Debug.LogWarning($"[MiningManager] Asteroid {asteroid.name} has no LocationObjectInfo component!");
            return;
        }

        Debug.Log($"[MiningManager] Asteroid info - Type: {asteroidInfo.objectType}, IsOfType('Asteroid'): {asteroidInfo.IsOfType("Asteroid")}");

        if (!asteroidInfo.IsOfType("Asteroid"))
        {
            Debug.LogWarning($"[MiningManager] Object {asteroid.name} is not an asteroid (type: {asteroidInfo.objectType})");
            return;
        }

        Debug.Log($"[MiningManager] Asteroid {asteroid.name} has {asteroidInfo.metalAmount}/{asteroidInfo.maxMetalAmount} metal");

        if (asteroidInfo.metalAmount <= 0)
        {
            Debug.Log($"[MiningManager] Asteroid {asteroid.name} is depleted");
            return;
        }

        // Используем сохраненную стартовую позицию в сетке, если она есть
        Vector2Int asteroidGridPos;
        if (asteroidInfo.gridSize.x > 1)
        {
            // Для многоклеточных объектов используем сохраненную позицию
            asteroidGridPos = asteroidInfo.gridStartPosition;
            Debug.Log($"[MiningManager] Using stored grid position {asteroidGridPos} for {asteroidInfo.gridSize.x}x{asteroidInfo.gridSize.y} asteroid");
        }
        else
        {
            // Для одноклеточных объектов вычисляем позицию
            asteroidGridPos = gridManager.WorldToGrid(asteroid.transform.position);
            Debug.Log($"[MiningManager] Calculated grid position {asteroidGridPos} from world position");
        }

        // Создаем новую задачу добычи
        MiningTask task = new MiningTask
        {
            character = character,
            asteroid = asteroid,
            asteroidInfo = asteroidInfo,
            asteroidGridPosition = asteroidGridPos,
            isActive = true
        };

        miningTasks.Add(task);

        Debug.Log($"MiningManager: {character.GetFullName()} starting to mine {asteroid.name} (Metal: {asteroidInfo.metalAmount})");

        // Запускаем процесс добычи
        task.miningCoroutine = StartCoroutine(MiningProcess(task));
    }

    /// <summary>
    /// Процесс добычи
    /// </summary>
    IEnumerator MiningProcess(MiningTask task)
    {
        Debug.Log($"[MiningManager] MiningProcess started for {task.character.GetFullName()}");

        Character character = task.character;
        CharacterMovement movement = character.GetComponent<CharacterMovement>();
        CharacterAI characterAI = character.GetComponent<CharacterAI>();

        Debug.Log($"[MiningManager] Character position: {character.transform.position}, Asteroid grid position: {task.asteroidGridPosition}, size: {task.asteroidInfo.gridSize}");

        // Получаем ближайшую соседнюю клетку к астероиду
        Vector2Int targetGridPosition = FindNearestAdjacentPosition(character.transform.position, task.asteroidGridPosition, task.asteroidInfo.gridSize);
        Debug.Log($"[MiningManager] Target grid position for mining: {targetGridPosition}");

        // Проверяем на сигнальное значение "не найдено" (-9999, -9999)
        if (targetGridPosition.x == -9999 && targetGridPosition.y == -9999)
        {
            Debug.LogWarning($"MiningManager: No valid adjacent position found for {character.GetFullName()}");
            StopMiningForCharacter(character);
            yield break;
        }

        Vector3 targetWorldPosition = gridManager.GridToWorld(targetGridPosition);

        // Проверяем расстояние - если персонаж уже рядом с астероидом, не двигаемся
        Vector2Int characterGridPos = gridManager.WorldToGrid(character.transform.position);

        // Проверяем, находится ли персонаж рядом с любой стороной астероида
        bool isAdjacentToAsteroid = false;
        Vector2Int asteroidEnd = task.asteroidGridPosition + task.asteroidInfo.gridSize - Vector2Int.one;

        // Проверяем близость к левой/правой/верхней/нижней сторонам
        if ((characterGridPos.x == task.asteroidGridPosition.x - 1 && characterGridPos.y >= task.asteroidGridPosition.y - 1 && characterGridPos.y <= asteroidEnd.y + 1) ||
            (characterGridPos.x == asteroidEnd.x + 1 && characterGridPos.y >= task.asteroidGridPosition.y - 1 && characterGridPos.y <= asteroidEnd.y + 1) ||
            (characterGridPos.y == task.asteroidGridPosition.y - 1 && characterGridPos.x >= task.asteroidGridPosition.x - 1 && characterGridPos.x <= asteroidEnd.x + 1) ||
            (characterGridPos.y == asteroidEnd.y + 1 && characterGridPos.x >= task.asteroidGridPosition.x - 1 && characterGridPos.x <= asteroidEnd.x + 1))
        {
            isAdjacentToAsteroid = true;
        }

        if (!isAdjacentToAsteroid)
        {
            // Персонаж далеко - движемся к астероиду
            Debug.Log($"MiningManager: {character.GetFullName()} moving to asteroid from {characterGridPos}");

            if (movement != null)
            {
                movement.MoveTo(targetWorldPosition);

                // ВАЖНО: Даем CharacterMovement время начать движение (у него есть задержка старта)
                Debug.Log($"MiningManager: Waiting for movement to start...");
                yield return new WaitForSeconds(0.15f);
                Debug.Log($"MiningManager: Movement should have started, IsMoving = {movement.IsMoving()}");

                // Ждем окончания движения
                while (movement.IsMoving())
                {
                    yield return new WaitForSeconds(0.1f);

                    // Проверяем, не прервалась ли задача
                    if (!task.isActive)
                    {
                        Debug.Log($"MiningManager: Mining interrupted for {character.GetFullName()}");
                        yield break;
                    }
                }

                Debug.Log($"MiningManager: {character.GetFullName()} finished moving to asteroid");
            }
        }
        else
        {
            Debug.Log($"MiningManager: {character.GetFullName()} already adjacent to asteroid, skipping movement");
        }

        // ВАЖНО: После движения подтверждаем, что персонаж действительно рядом
        characterGridPos = gridManager.WorldToGrid(character.transform.position);

        // Проверяем снова, находится ли персонаж рядом с астероидом
        isAdjacentToAsteroid = false;
        if ((characterGridPos.x == task.asteroidGridPosition.x - 1 && characterGridPos.y >= task.asteroidGridPosition.y - 1 && characterGridPos.y <= asteroidEnd.y + 1) ||
            (characterGridPos.x == asteroidEnd.x + 1 && characterGridPos.y >= task.asteroidGridPosition.y - 1 && characterGridPos.y <= asteroidEnd.y + 1) ||
            (characterGridPos.y == task.asteroidGridPosition.y - 1 && characterGridPos.x >= task.asteroidGridPosition.x - 1 && characterGridPos.x <= asteroidEnd.x + 1) ||
            (characterGridPos.y == asteroidEnd.y + 1 && characterGridPos.x >= task.asteroidGridPosition.x - 1 && characterGridPos.x <= asteroidEnd.x + 1))
        {
            isAdjacentToAsteroid = true;
        }

        if (!isAdjacentToAsteroid)
        {
            Debug.LogWarning($"MiningManager: {character.GetFullName()} failed to reach adjacent position to asteroid! Current: {characterGridPos}");
            StopMiningForCharacter(character);
            yield break;
        }

        // Персонаж подтвержденно рядом с астероидом - переключаем в состояние Mining
        Debug.Log($"MiningManager: {character.GetFullName()} confirmed adjacent at {characterGridPos}, switching to Mining state");

        if (characterAI != null)
        {
            characterAI.SetAIState(CharacterAI.AIState.Mining);
        }

        // Поворачиваем персонажа к астероиду
        RotateCharacterTowardsAsteroid(character, task.asteroid);

        Debug.Log($"MiningManager: {character.GetFullName()} starting mining jumps");

        // Основной цикл добычи
        Debug.Log($"[MiningManager] ========== ENTERING MINING LOOP ==========");
        Debug.Log($"[MiningManager] Initial state - task.isActive: {task.isActive}, metalAmount: {task.asteroidInfo.metalAmount}");

        int loopIteration = 0;
        while (task.isActive && task.asteroidInfo.metalAmount > 0)
        {
            loopIteration++;
            Debug.Log($"[MiningManager] ===== LOOP ITERATION {loopIteration} START =====");
            Debug.Log($"[MiningManager] Loop conditions - task.isActive: {task.isActive}, metalAmount: {task.asteroidInfo.metalAmount}");

            // Проверяем, что персонаж жив
            if (character.IsDead())
            {
                Debug.Log($"[MiningManager] Character {character.GetFullName()} is DEAD - stopping mining");
                StopMiningForCharacter(character);
                yield break;
            }
            Debug.Log($"[MiningManager] Character {character.GetFullName()} is alive - continuing");

            // КРИТИЧНО: Проверяем позицию персонажа ПЕРЕД КАЖДЫМ прыжком
            characterGridPos = gridManager.WorldToGrid(character.transform.position);
            Debug.Log($"[MiningManager] Character world position: {character.transform.position}");
            Debug.Log($"[MiningManager] Character grid position: {characterGridPos}");

            // Пересчитываем asteroidEnd для проверки adjacency
            asteroidEnd = task.asteroidGridPosition + task.asteroidInfo.gridSize - Vector2Int.one;
            Debug.Log($"[MiningManager] Asteroid area - start: {task.asteroidGridPosition}, end: {asteroidEnd}, size: {task.asteroidInfo.gridSize}");

            // Проверяем, находится ли персонаж всё ещё рядом с астероидом
            isAdjacentToAsteroid = false;

            // Детальная проверка каждого условия
            bool leftSide = (characterGridPos.x == task.asteroidGridPosition.x - 1 && characterGridPos.y >= task.asteroidGridPosition.y - 1 && characterGridPos.y <= asteroidEnd.y + 1);
            bool rightSide = (characterGridPos.x == asteroidEnd.x + 1 && characterGridPos.y >= task.asteroidGridPosition.y - 1 && characterGridPos.y <= asteroidEnd.y + 1);
            bool bottomSide = (characterGridPos.y == task.asteroidGridPosition.y - 1 && characterGridPos.x >= task.asteroidGridPosition.x - 1 && characterGridPos.x <= asteroidEnd.x + 1);
            bool topSide = (characterGridPos.y == asteroidEnd.y + 1 && characterGridPos.x >= task.asteroidGridPosition.x - 1 && characterGridPos.x <= asteroidEnd.x + 1);

            Debug.Log($"[MiningManager] Adjacency check - left: {leftSide}, right: {rightSide}, bottom: {bottomSide}, top: {topSide}");

            if (leftSide || rightSide || bottomSide || topSide)
            {
                isAdjacentToAsteroid = true;
            }

            Debug.Log($"[MiningManager] Final adjacency result: {isAdjacentToAsteroid}");

            // Если персонаж больше не рядом с астероидом - НЕМЕДЛЕННО останавливаем добычу
            if (!isAdjacentToAsteroid)
            {
                Debug.Log($"[MiningManager] CHARACTER NOT ADJACENT - Stopping mining!");
                StopMiningForCharacter(character);
                yield break;
            }

            // Выполняем прыжок (только если персонаж рядом!)
            Debug.Log($"[MiningManager] Starting mining jump...");
            yield return StartCoroutine(PerformMiningJump(character));
            Debug.Log($"[MiningManager] Mining jump completed");

            // Добываем металл
            int minedAmount = Mathf.Min(metalPerJump, task.asteroidInfo.metalAmount);
            int oldAmount = task.asteroidInfo.metalAmount;
            task.asteroidInfo.metalAmount -= minedAmount;
            Debug.Log($"[MiningManager] Mining metal - amount: {minedAmount}, before: {oldAmount}, after: {task.asteroidInfo.metalAmount}");

            // Добавляем металл в инвентарь персонажа
            Inventory inventory = character.GetComponent<Inventory>();
            if (inventory != null)
            {
                Debug.Log($"[MiningManager] Adding {minedAmount} metal to inventory");
                AddMetalToInventory(inventory, minedAmount);
                Debug.Log($"[MiningManager] Metal added to inventory successfully");
            }
            else
            {
                Debug.LogWarning($"[MiningManager] Character has no Inventory component!");
            }

            // Проверяем, истощился ли астероид
            if (task.asteroidInfo.metalAmount <= 0)
            {
                Debug.Log($"[MiningManager] ASTEROID DEPLETED - Breaking loop");
                break;
            }

            Debug.Log($"[MiningManager] Waiting {miningJumpInterval}s before next jump...");
            Debug.Log($"[MiningManager] ===== LOOP ITERATION {loopIteration} END =====");

            // Ждем до следующего прыжка
            yield return new WaitForSeconds(miningJumpInterval);

            Debug.Log($"[MiningManager] Wait completed, checking loop conditions again...");
        }

        Debug.Log($"[MiningManager] ========== EXITED MINING LOOP ==========");
        Debug.Log($"[MiningManager] Exit reason - task.isActive: {task.isActive}, metalAmount: {task.asteroidInfo.metalAmount}");

        // Добыча завершена
        Debug.Log($"MiningManager: {character.GetFullName()} finished mining");
        StopMiningForCharacter(character);
    }

    /// <summary>
    /// Найти ближайшую соседнюю позицию к астероиду (учитывает размер астероида)
    /// </summary>
    Vector2Int FindNearestAdjacentPosition(Vector3 characterPosition, Vector2Int asteroidStartPos, Vector2Int asteroidSize)
    {
        Vector2Int characterGridPos = gridManager.WorldToGrid(characterPosition);
        Vector2Int notFoundSentinel = new Vector2Int(-9999, -9999);

        Debug.Log($"[MiningManager] FindNearestAdjacentPosition: Character at {characterGridPos}, Asteroid area: {asteroidStartPos} to ({asteroidStartPos.x + asteroidSize.x - 1}, {asteroidStartPos.y + asteroidSize.y - 1})");

        // Генерируем список всех клеток по периметру астероида
        List<Vector2Int> perimeterCells = new List<Vector2Int>();

        // Верхняя сторона (Y = asteroidStartPos.y - 1)
        for (int x = asteroidStartPos.x - 1; x <= asteroidStartPos.x + asteroidSize.x; x++)
        {
            perimeterCells.Add(new Vector2Int(x, asteroidStartPos.y - 1));
        }

        // Нижняя сторона (Y = asteroidStartPos.y + asteroidSize.y)
        for (int x = asteroidStartPos.x - 1; x <= asteroidStartPos.x + asteroidSize.x; x++)
        {
            perimeterCells.Add(new Vector2Int(x, asteroidStartPos.y + asteroidSize.y));
        }

        // Левая сторона (X = asteroidStartPos.x - 1), исключая углы
        for (int y = asteroidStartPos.y; y < asteroidStartPos.y + asteroidSize.y; y++)
        {
            perimeterCells.Add(new Vector2Int(asteroidStartPos.x - 1, y));
        }

        // Правая сторона (X = asteroidStartPos.x + asteroidSize.x), исключая углы
        for (int y = asteroidStartPos.y; y < asteroidStartPos.y + asteroidSize.y; y++)
        {
            perimeterCells.Add(new Vector2Int(asteroidStartPos.x + asteroidSize.x, y));
        }

        Debug.Log($"[MiningManager] Generated {perimeterCells.Count} perimeter cells around asteroid");

        // Проверяем, находится ли персонаж уже на одной из перимерных клеток
        foreach (Vector2Int perimeterCell in perimeterCells)
        {
            if (characterGridPos == perimeterCell)
            {
                GridCell cell = gridManager.GetCell(perimeterCell);
                if (cell != null && !cell.isOccupied)
                {
                    Debug.Log($"[MiningManager] Character already at perimeter cell {characterGridPos}");
                    return characterGridPos;
                }
            }
        }

        // Ищем ближайшую свободную периметровую клетку
        Vector2Int closestPosition = notFoundSentinel;
        float closestDistance = float.MaxValue;
        int validCells = 0;
        int occupiedCells = 0;
        int invalidCells = 0;

        foreach (Vector2Int perimeterPos in perimeterCells)
        {
            // Проверяем валидность позиции
            if (!gridManager.IsValidGridPosition(perimeterPos))
            {
                invalidCells++;
                continue;
            }

            GridCell cell = gridManager.GetCell(perimeterPos);
            if (cell == null)
            {
                invalidCells++;
                continue;
            }

            if (cell.isOccupied)
            {
                occupiedCells++;
                continue;
            }

            // Клетка свободна и валидна
            validCells++;
            Vector3 perimeterWorldPos = gridManager.GridToWorld(perimeterPos);
            float distance = Vector3.Distance(characterPosition, perimeterWorldPos);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPosition = perimeterPos;
            }
        }

        Debug.Log($"[MiningManager] Perimeter check: {validCells} free, {occupiedCells} occupied, {invalidCells} invalid");

        if (closestPosition != notFoundSentinel)
        {
            Debug.Log($"[MiningManager] Found closest free perimeter cell: {closestPosition} at distance {closestDistance:F2}");
        }
        else
        {
            Debug.LogWarning($"[MiningManager] No free perimeter cells found around asteroid!");
        }

        return closestPosition;
    }

    /// <summary>
    /// Повернуть персонажа к астероиду
    /// </summary>
    void RotateCharacterTowardsAsteroid(Character character, GameObject asteroid)
    {
        Vector3 direction = asteroid.transform.position - character.transform.position;
        direction.y = 0;

        if (direction.magnitude < 0.1f)
            return;

        // Вычисляем угол
        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        // Привязываем к 8 направлениям (0°, 45°, 90°, 135°, 180°, 225°, 270°, 315°)
        float snappedAngle = Mathf.Round(angle / 45f) * 45f;

        character.transform.rotation = Quaternion.Euler(0, snappedAngle, 0);
    }

    /// <summary>
    /// Выполнить анимацию прыжка для добычи
    /// </summary>
    IEnumerator PerformMiningJump(Character character)
    {
        Vector3 originalPosition = character.transform.position;
        float elapsedTime = 0f;

        // Прыжок вверх и вниз
        while (elapsedTime < jumpDuration)
        {
            float progress = elapsedTime / jumpDuration;
            // Синусоидальная кривая для плавного прыжка
            float height = Mathf.Sin(progress * Mathf.PI) * jumpHeight;
            character.transform.position = originalPosition + Vector3.up * height;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Возвращаем персонажа на исходную позицию
        character.transform.position = originalPosition;
    }

    /// <summary>
    /// Создать ItemData для металла
    /// </summary>
    ItemData CreateMetalItemData()
    {
        ItemData metal = new ItemData();
        metal.itemName = ItemNames.METAL;
        metal.description = "Raw metal ore extracted from asteroids";
        metal.itemType = ItemType.Resource;
        metal.rarity = ItemRarity.Common;
        metal.maxStackSize = 999;
        metal.weight = 0.1f;
        metal.value = 1;
        metal.equipmentSlot = EquipmentSlot.None;

        // Применяем иконку через фабрику (если есть)
        ItemFactory.ApplyIcon(metal);

        return metal;
    }

    /// <summary>
    /// Добавить металл в инвентарь персонажа
    /// </summary>
    void AddMetalToInventory(Inventory inventory, int amount)
    {
        // Ищем металл в инвентаре
        InventorySlot metalSlot = null;
        List<InventorySlot> allSlots = inventory.GetAllSlots();

        foreach (InventorySlot slot in allSlots)
        {
            if (slot.itemData != null && slot.itemData.itemName == ItemNames.METAL)
            {
                metalSlot = slot;
                break;
            }
        }

        if (metalSlot != null && metalSlot.itemData != null)
        {
            // Увеличиваем количество через метод AddItem
            inventory.AddItem(metalSlot.itemData, amount);
            Debug.Log($"MiningManager: Added {amount} metal. Total: {inventory.GetItemCount(metalSlot.itemData)}");
        }
        else
        {
            // Создаем новый предмет металла
            ItemData metalItem = CreateMetalItemData();
            inventory.AddItem(metalItem, amount);
            Debug.Log($"MiningManager: Created new metal stack with {amount} metal");
        }

        // Обновляем UI инвентаря
        ResourcePanelUI resourcePanel = FindObjectOfType<ResourcePanelUI>();
        if (resourcePanel != null)
        {
            resourcePanel.UpdateResourceDisplay();
        }
    }

    /// <summary>
    /// Остановить добычу для персонажа
    /// </summary>
    public void StopMiningForCharacter(Character character)
    {
        // Получаем информацию о том, откуда был вызван этот метод
        System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace(1, true);
        string callerInfo = stackTrace.GetFrame(0)?.GetMethod()?.Name ?? "Unknown";

        Debug.Log($"[MiningManager] ========== STOP MINING CALLED ==========");
        Debug.Log($"[MiningManager] Called from: {callerInfo}");
        Debug.Log($"[MiningManager] Character: {(character != null ? character.GetFullName() : "NULL")}");

        // Проверяем, что персонаж еще существует
        if (character == null)
        {
            Debug.Log($"[MiningManager] Character is NULL - removing all null tasks");
            // Удаляем все задачи с null персонажами
            miningTasks.RemoveAll(t => t.character == null);
            return;
        }

        MiningTask task = miningTasks.Find(t => t.character == character);
        if (task != null)
        {
            Debug.Log($"[MiningManager] Found mining task - setting isActive to false");
            task.isActive = false;

            if (task.miningCoroutine != null)
            {
                Debug.Log($"[MiningManager] Stopping mining coroutine");
                StopCoroutine(task.miningCoroutine);
            }

            Debug.Log($"[MiningManager] Removing task from list");
            miningTasks.Remove(task);

            Debug.Log($"[MiningManager] Mining stopped for {character.GetFullName()}");

            // Возвращаем персонажа в состояние Idle
            CharacterAI characterAI = character.GetComponent<CharacterAI>();
            if (characterAI != null && characterAI.GetCurrentState() == CharacterAI.AIState.Mining)
            {
                Debug.Log($"[MiningManager] Switching character AI state to Idle");
                characterAI.SetAIState(CharacterAI.AIState.Idle);
            }
        }
        else
        {
            Debug.Log($"[MiningManager] No mining task found for {character.GetFullName()}");
        }

        Debug.Log($"[MiningManager] ========== STOP MINING COMPLETED ==========");
    }

    /// <summary>
    /// Проверить, добывает ли персонаж ресурсы
    /// </summary>
    public bool IsCharacterMining(Character character)
    {
        return miningTasks.Exists(t => t.character == character && t.isActive);
    }

    /// <summary>
    /// Остановить всю добычу
    /// </summary>
    public void StopAllMining()
    {
        List<MiningTask> tasksToStop = new List<MiningTask>(miningTasks);
        foreach (MiningTask task in tasksToStop)
        {
            // Проверяем, что персонаж еще существует
            if (task.character != null)
            {
                StopMiningForCharacter(task.character);
            }
        }

        // Очищаем все оставшиеся задачи с null персонажами
        miningTasks.Clear();
    }

    void OnDestroy()
    {
        StopAllMining();
    }
}
