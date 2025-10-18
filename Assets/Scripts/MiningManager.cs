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
        public Vector2Int reservedWorkPosition; // Зарезервированная позиция для работы
        public bool isActive;
        public Coroutine miningCoroutine;
    }

    private List<MiningTask> miningTasks = new List<MiningTask>();

    // Словарь зарезервированных клеток для добычи (чтобы персонажи не занимали одну клетку)
    private Dictionary<Vector2Int, Character> reservedMiningPositions = new Dictionary<Vector2Int, Character>();

    void Awake()
    {
        gridManager = FindObjectOfType<GridManager>();
    }

    /// <summary>
    /// Начать добычу ресурса
    /// </summary>
    public void StartMining(Character character, GameObject asteroid)
    {


        if (character == null || asteroid == null)
        {
            Debug.LogWarning("[MiningManager] Character or asteroid is null");
            return;
        }

        // Проверяем, есть ли уже задача для этого персонажа
        MiningTask existingTask = miningTasks.Find(t => t.character == character);
        if (existingTask != null)
        {

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



        if (!asteroidInfo.IsOfType("Asteroid"))
        {
            Debug.LogWarning($"[MiningManager] Object {asteroid.name} is not an asteroid (type: {asteroidInfo.objectType})");
            return;
        }



        if (asteroidInfo.metalAmount <= 0)
        {

            return;
        }

        // Используем сохраненную стартовую позицию в сетке, если она есть
        Vector2Int asteroidGridPos;
        if (asteroidInfo.gridSize.x > 1)
        {
            // Для многоклеточных объектов используем сохраненную позицию
            asteroidGridPos = asteroidInfo.gridStartPosition;

        }
        else
        {
            // Для одноклеточных объектов вычисляем позицию
            asteroidGridPos = gridManager.WorldToGrid(asteroid.transform.position);

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



        // Запускаем процесс добычи
        task.miningCoroutine = StartCoroutine(MiningProcess(task));
    }

    /// <summary>
    /// Процесс добычи
    /// </summary>
    IEnumerator MiningProcess(MiningTask task)
    {
        // ARCHITECTURE: Публикуем событие начала добычи через EventBus
        EventBus.Publish(new MiningStartedEvent(task.character, task.asteroid));

        Character character = task.character;
        CharacterMovement movement = character.GetComponent<CharacterMovement>();
        CharacterAI characterAI = character.GetComponent<CharacterAI>();



        // Получаем ближайшую соседнюю клетку к астероиду
        Vector2Int targetGridPosition = FindNearestAdjacentPosition(character.transform.position, task.asteroidGridPosition, task.asteroidInfo.gridSize, character);


        // Проверяем на сигнальное значение "не найдено" (-9999, -9999)
        if (targetGridPosition.x == -9999 && targetGridPosition.y == -9999)
        {
            Debug.LogWarning($"MiningManager: No valid adjacent position found for {character.GetFullName()}");
            StopMiningForCharacter(character);
            yield break;
        }

        // РЕЗЕРВИРУЕМ найденную позицию для этого персонажа
        task.reservedWorkPosition = targetGridPosition;
        reservedMiningPositions[targetGridPosition] = character;

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

        // Цикл движения к астероиду с возможностью перенаправления
        int maxRedirects = 5; // Максимум 5 перенаправлений чтобы избежать бесконечного цикла
        int redirectCount = 0;

        while (!isAdjacentToAsteroid && redirectCount < maxRedirects)
        {
            // Персонаж далеко - движемся к целевой позиции
            if (movement != null)
            {
                Vector3 targetWorldPosition = gridManager.GridToWorld(task.reservedWorkPosition);
                movement.MoveTo(targetWorldPosition);

                // ВАЖНО: Даем CharacterMovement время начать движение
                yield return new WaitForSeconds(0.15f);

                bool needsRedirect = false;

                // Ждем окончания движения И проверяем, не занята ли зарезервированная клетка
                while (movement.IsMoving())
                {
                    yield return new WaitForSeconds(0.1f);

                    // Проверяем, не прервалась ли задача
                    if (!task.isActive)
                    {
                        yield break;
                    }

                    // ПРОВЕРКА ВО ВРЕМЯ ПУТИ: Не занята ли наша зарезервированная клетка?
                    GridCell cellDuringMovement = gridManager.GetCell(task.reservedWorkPosition);
                    Vector2Int currentGridPos = gridManager.WorldToGrid(character.transform.position);

                    if (cellDuringMovement != null && cellDuringMovement.isOccupied && currentGridPos != task.reservedWorkPosition)
                    {
                        // Клетка занята во время пути! Останавливаем движение и ищем новую клетку
                        movement.StopMovement();

                        Vector2Int oldReservation = task.reservedWorkPosition;

                        // Ищем новую свободную клетку
                        Vector2Int newPosition = FindNearestAdjacentPosition(character.transform.position, task.asteroidGridPosition, task.asteroidInfo.gridSize, character);

                        if (newPosition.x == -9999 && newPosition.y == -9999)
                        {
                            Debug.LogWarning($"MiningManager: {character.GetFullName()} cannot find free position - no adjacent cells available!");
                            StopMiningForCharacter(character);
                            yield break;
                        }

                        // Резервируем новую позицию ПЕРЕД освобождением старой
                        task.reservedWorkPosition = newPosition;
                        reservedMiningPositions[newPosition] = character;

                        // Освобождаем старую резервацию
                        if (reservedMiningPositions.ContainsKey(oldReservation))
                        {
                            if (reservedMiningPositions[oldReservation] == character)
                            {
                                reservedMiningPositions.Remove(oldReservation);
                            }
                        }

                        redirectCount++;
                        needsRedirect = true;
                        break; // Выходим из while(movement.IsMoving()) чтобы начать новое движение
                    }
                }

                if (needsRedirect)
                {
                    continue; // Начинаем новую итерацию цикла движения с новой целью
                }
            }

            // После движения проверяем позицию
            characterGridPos = gridManager.WorldToGrid(character.transform.position);

            // Проверяем, достиг ли персонаж позиции рядом с астероидом
            isAdjacentToAsteroid = false;
            if ((characterGridPos.x == task.asteroidGridPosition.x - 1 && characterGridPos.y >= task.asteroidGridPosition.y - 1 && characterGridPos.y <= asteroidEnd.y + 1) ||
                (characterGridPos.x == asteroidEnd.x + 1 && characterGridPos.y >= task.asteroidGridPosition.y - 1 && characterGridPos.y <= asteroidEnd.y + 1) ||
                (characterGridPos.y == task.asteroidGridPosition.y - 1 && characterGridPos.x >= task.asteroidGridPosition.x - 1 && characterGridPos.x <= asteroidEnd.x + 1) ||
                (characterGridPos.y == asteroidEnd.y + 1 && characterGridPos.x >= task.asteroidGridPosition.x - 1 && characterGridPos.x <= asteroidEnd.x + 1))
            {
                isAdjacentToAsteroid = true;
            }
        }

        // Проверка что достигли цели
        if (redirectCount >= maxRedirects)
        {
            Debug.LogWarning($"MiningManager: {character.GetFullName()} exceeded maximum redirects ({maxRedirects})!");
            StopMiningForCharacter(character);
            yield break;
        }

        // Если цикл успешно завершился, значит isAdjacentToAsteroid == true
        // Персонаж гарантированно рядом с астероидом
        characterGridPos = gridManager.WorldToGrid(character.transform.position);

        // Персонаж подтвержденно рядом с астероидом - переключаем в состояние Mining


        if (characterAI != null)
        {
            characterAI.SetAIState(CharacterAI.AIState.Mining);
        }

        // Поворачиваем персонажа к астероиду
        RotateCharacterTowardsAsteroid(character, task.asteroid);



        // Основной цикл добычи
        int totalMinedMetal = 0; // Для события MiningCompletedEvent
        int loopIteration = 0;
        while (task.isActive && task.asteroidInfo.metalAmount > 0)
        {
            loopIteration++;



            // Проверяем, что персонаж жив
            if (character.IsDead())
            {

                StopMiningForCharacter(character);
                yield break;
            }


            // КРИТИЧНО: Проверяем позицию персонажа ПЕРЕД КАЖДЫМ прыжком
            characterGridPos = gridManager.WorldToGrid(character.transform.position);



            // Пересчитываем asteroidEnd для проверки adjacency
            asteroidEnd = task.asteroidGridPosition + task.asteroidInfo.gridSize - Vector2Int.one;


            // Проверяем, находится ли персонаж всё ещё рядом с астероидом
            isAdjacentToAsteroid = false;

            // Детальная проверка каждого условия
            bool leftSide = (characterGridPos.x == task.asteroidGridPosition.x - 1 && characterGridPos.y >= task.asteroidGridPosition.y - 1 && characterGridPos.y <= asteroidEnd.y + 1);
            bool rightSide = (characterGridPos.x == asteroidEnd.x + 1 && characterGridPos.y >= task.asteroidGridPosition.y - 1 && characterGridPos.y <= asteroidEnd.y + 1);
            bool bottomSide = (characterGridPos.y == task.asteroidGridPosition.y - 1 && characterGridPos.x >= task.asteroidGridPosition.x - 1 && characterGridPos.x <= asteroidEnd.x + 1);
            bool topSide = (characterGridPos.y == asteroidEnd.y + 1 && characterGridPos.x >= task.asteroidGridPosition.x - 1 && characterGridPos.x <= asteroidEnd.x + 1);



            if (leftSide || rightSide || bottomSide || topSide)
            {
                isAdjacentToAsteroid = true;
            }



            // Если персонаж больше не рядом с астероидом - НЕМЕДЛЕННО останавливаем добычу
            if (!isAdjacentToAsteroid)
            {

                StopMiningForCharacter(character);
                yield break;
            }

            // Выполняем прыжок (только если персонаж рядом!)

            yield return StartCoroutine(PerformMiningJump(character));


            // Добываем металл
            int minedAmount = Mathf.Min(metalPerJump, task.asteroidInfo.metalAmount);
            int oldAmount = task.asteroidInfo.metalAmount;
            task.asteroidInfo.metalAmount -= minedAmount;
            totalMinedMetal += minedAmount; // Накапливаем для события


            // Добавляем металл в инвентарь персонажа
            Inventory inventory = character.GetComponent<Inventory>();
            if (inventory != null)
            {

                AddMetalToInventory(inventory, minedAmount);

            }
            else
            {
                Debug.LogWarning($"[MiningManager] Character has no Inventory component!");
            }

            // Проверяем, истощился ли астероид
            if (task.asteroidInfo.metalAmount <= 0)
            {

                break;
            }




            // Ждем до следующего прыжка
            yield return new WaitForSeconds(miningJumpInterval);


        }

        // ARCHITECTURE: Публикуем событие завершения добычи через EventBus
        EventBus.Publish(new MiningCompletedEvent(character, task.asteroid, totalMinedMetal));

        // Добыча завершена
        StopMiningForCharacter(character);
    }

    /// <summary>
    /// Найти ближайшую соседнюю позицию к астероиду (старый метод для совместимости)
    /// </summary>
    Vector2Int FindNearestAdjacentPosition(Vector3 characterPosition, Vector2Int asteroidStartPos, Vector2Int asteroidSize)
    {
        return FindNearestAdjacentPosition(characterPosition, asteroidStartPos, asteroidSize, null);
    }

    /// <summary>
    /// Найти ближайшую соседнюю позицию к астероиду (учитывает размер астероида и свои резервации)
    /// </summary>
    Vector2Int FindNearestAdjacentPosition(Vector3 characterPosition, Vector2Int asteroidStartPos, Vector2Int asteroidSize, Character requestingCharacter)
    {
        Vector2Int characterGridPos = gridManager.WorldToGrid(characterPosition);
        Vector2Int notFoundSentinel = new Vector2Int(-9999, -9999);



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



        // Проверяем, находится ли персонаж уже на одной из перимерных клеток
        foreach (Vector2Int perimeterCell in perimeterCells)
        {
            if (characterGridPos == perimeterCell)
            {
                GridCell cell = gridManager.GetCell(perimeterCell);
                if (cell != null && !cell.isOccupied)
                {

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

            // Проверяем, не зарезервирована ли клетка другим персонажем
            if (reservedMiningPositions.ContainsKey(perimeterPos))
            {
                // Если это наша собственная резервация - можем использовать
                if (requestingCharacter != null && reservedMiningPositions[perimeterPos] == requestingCharacter)
                {
                    // Это наша резервация, продолжаем проверку как свободную клетку
                }
                else
                {
                    // Зарезервирована кем-то другим
                    occupiedCells++;
                    continue;
                }
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



        if (closestPosition != notFoundSentinel)
        {

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

        }
        else
        {
            // Создаем новый предмет металла
            ItemData metalItem = CreateMetalItemData();
            inventory.AddItem(metalItem, amount);

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





        // Проверяем, что персонаж еще существует
        if (character == null)
        {

            // Удаляем все задачи с null персонажами
            miningTasks.RemoveAll(t => t.character == null);
            return;
        }

        MiningTask task = miningTasks.Find(t => t.character == character);
        if (task != null)
        {

            task.isActive = false;

            if (task.miningCoroutine != null)
            {

                StopCoroutine(task.miningCoroutine);
            }

            // ВАЖНО: Освобождаем зарезервированную позицию
            if (reservedMiningPositions.ContainsKey(task.reservedWorkPosition))
            {
                if (reservedMiningPositions[task.reservedWorkPosition] == character)
                {
                    reservedMiningPositions.Remove(task.reservedWorkPosition);
                }
            }

            miningTasks.Remove(task);



            // Возвращаем персонажа в состояние Idle
            CharacterAI characterAI = character.GetComponent<CharacterAI>();
            if (characterAI != null && characterAI.GetCurrentState() == CharacterAI.AIState.Mining)
            {

                characterAI.SetAIState(CharacterAI.AIState.Idle);
            }
        }
        else
        {

        }


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

        // Очищаем все резервации
        reservedMiningPositions.Clear();
    }

    void OnDestroy()
    {
        StopAllMining();
    }
}
