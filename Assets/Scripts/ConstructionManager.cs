using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Менеджер строительства - управляет процессом строительства блоков корабля персонажами
/// ARCHITECTURE: Наследуется от BaseManager для интеграции с ServiceLocator
/// </summary>
public class ConstructionManager : BaseManager
{
    private static ConstructionManager instance;

    public static ConstructionManager Instance
    {
        get
        {
            if (instance == null)
            {
                // Создаем GameObject с компонентом ConstructionManager
                GameObject go = new GameObject("ConstructionManager");
                instance = go.AddComponent<ConstructionManager>();
                DontDestroyOnLoad(go); // Не уничтожать при загрузке новой сцены

            }
            return instance;
        }
    }

    // Кешированная ссылка на GridManager для оптимизации
    private GridManager gridManager;

    [Header("Construction Settings")]
    [Tooltip("Количество прыжков для постройки одного блока")]
    public int jumpsRequired = 5;

    [Tooltip("Время между прыжками в секундах")]
    public float jumpInterval = 0.5f;

    [Tooltip("Высота прыжка")]
    public float jumpHeight = 0.5f;

    [Tooltip("Дистанция до блока для начала строительства (1 клетка - персонаж должен быть на соседней клетке)")]
    public float constructionRange = 10.5f; // Размер клетки 10f, соседняя клетка = 10f, берем чуть больше для погрешности

    [Header("Progress Bar Settings")]
    [Tooltip("Высота полосы прогресса над блоком")]
    public float progressBarHeight = 2.5f;

    [Tooltip("Ширина полосы прогресса")]
    public float progressBarWidth = 12f;

    [Tooltip("Высота полосы прогресса")]
    public float progressBarThickness = 2f;

    [Tooltip("Масштаб Canvas для полосы прогресса")]
    public float progressBarScale = 0.12f; // Уменьшено на 40% (0.2 * 0.6 = 0.12)

    [Tooltip("Цвет фона полосы")]
    public Color progressBarBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);

    [Tooltip("Цвет заполнения полосы")]
    public Color progressBarFillColor = new Color(0.9f, 0.2f, 0.2f, 0.9f);

    // Список блоков для строительства
    private List<ConstructionBlock> constructionQueue = new List<ConstructionBlock>();

    // Персонажи занятые строительством
    private Dictionary<Character, ConstructionBlock> busyCharacters = new Dictionary<Character, ConstructionBlock>();

    // КОРУТИНЫ СТРОИТЕЛЬСТВА для каждого персонажа (для остановки)
    private Dictionary<Character, Coroutine> constructionCoroutines = new Dictionary<Character, Coroutine>();

    // Полосы прогресса для блоков
    private Dictionary<ConstructionBlock, GameObject> progressBars = new Dictionary<ConstructionBlock, GameObject>();

    /// <summary>
    /// Инициализация менеджера строительства через ServiceLocator
    /// </summary>
    protected override void OnManagerInitialized()
    {
        base.OnManagerInitialized();

        // Singleton pattern
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Получаем GridManager через ServiceLocator
        gridManager = GetService<GridManager>();
        if (gridManager == null)
        {
            LogError("GridManager not found! Construction system will not work properly.");
        }
    }

    /// <summary>
    /// Добавить блоки для строительства
    /// </summary>
    public void AddConstructionBlocks(List<ConstructionBlock> blocks)
    {
        constructionQueue.AddRange(blocks);
        // НЕ назначаем строительство сразу всем персонажам!
        // Персонажи будут автоматически запрашивать задачи когда перейдут в состояние Idle
    }

    /// <summary>
    /// Назначить строительство всем персонажам игрока на карте
    /// </summary>
    void AssignConstructionToAllCharacters()
    {
        // Находим всех персонажей игрока
        Character[] allCharacters = FindObjectsOfType<Character>();
        foreach (Character character in allCharacters)
        {
            if (character.IsPlayerCharacter() && !character.IsDead())
            {
                // Если персонаж не занят строительством, назначаем ему блок
                if (!busyCharacters.ContainsKey(character))
                {
                    AssignConstructionTask(character);
                }
            }
        }
    }

    /// <summary>
    /// ПУБЛИЧНЫЙ МЕТОД: Попытка назначить строительство персонажу (вызывается из CharacterAI)
    /// Назначает строительство ТОЛЬКО если персонаж в состоянии Idle
    /// </summary>
    public void TryAssignConstructionToIdleCharacter(Character character)
    {
        // Проверка 1: Персонаж должен быть игроком
        if (!character.IsPlayerCharacter())
        {
            return;
        }

        // Проверка 2: Персонаж не должен быть мертв
        if (character.IsDead())
        {
            return;
        }

        // Проверка 3: Персонаж не должен быть уже занят строительством
        if (busyCharacters.ContainsKey(character))
        {
            return;
        }

        // Проверка 4: Должны быть доступные блоки для строительства
        if (constructionQueue.Count == 0)
        {
            return;
        }

        // Проверка 5: Персонаж должен быть в состоянии Idle
        CharacterAI characterAI = character.GetComponent<CharacterAI>();
        if (characterAI == null || characterAI.GetCurrentState() != CharacterAI.AIState.Idle)
        {
            return;
        }

        // ВСЕ ПРОВЕРКИ ПРОШЛИ - назначаем строительство
        AssignConstructionTask(character);
    }

    /// <summary>
    /// Назначить задачу строительства персонажу
    /// </summary>
    void AssignConstructionTask(Character character)
    {
        // Проверяем что персонаж еще не занят строительством
        if (busyCharacters.ContainsKey(character))
        {
            Debug.LogWarning($"[ConstructionManager] {character.GetFullName()} is already busy with construction!");
            return;
        }

        // Находим ближайший свободный блок
        ConstructionBlock nearestBlock = FindNearestAvailableBlock(character.transform.position);

        if (nearestBlock != null)
        {
            // Двойная проверка - блок действительно свободен?
            if (nearestBlock.isAssigned)
            {
                Debug.LogWarning($"[ConstructionManager] Block at {nearestBlock.gridPosition} is already assigned to another character!");
                return;
            }

            // Помечаем блок как занятый СРАЗУ
            nearestBlock.isAssigned = true;
            busyCharacters[character] = nearestBlock;

            // Запускаем процесс строительства И СОХРАНЯЕМ ССЫЛКУ НА КОРУТИНУ
            Coroutine coroutine = StartCoroutine(ConstructBlock(character, nearestBlock));
            constructionCoroutines[character] = coroutine;
        }
    }

    /// <summary>
    /// Найти ближайший свободный блок для строительства
    /// </summary>
    ConstructionBlock FindNearestAvailableBlock(Vector3 position)
    {
        ConstructionBlock nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (ConstructionBlock block in constructionQueue)
        {
            if (!block.isAssigned && !block.isCompleted)
            {
                float distance = Vector3.Distance(position, block.worldPosition);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = block;
                }
            }
        }

        return nearest;
    }

    /// <summary>
    /// Найти ближайшую свободную позицию рядом с блоком для строительства
    /// </summary>
    Vector3? FindNearestValidConstructionPosition(Vector3 characterPosition, ConstructionBlock block)
    {
        if (gridManager == null)
        {
            LogError("GridManager not available, using default adjacent position");
            return block.worldPosition + new Vector3(10f, 0, 0); // 10f - размер клетки по умолчанию
        }

        // Проверяем 4 соседние клетки (верх, низ, лево, право)
        Vector2Int[] adjacentOffsets = new Vector2Int[]
        {
            new Vector2Int(0, 1),   // верх
            new Vector2Int(0, -1),  // низ
            new Vector2Int(-1, 0),  // лево
            new Vector2Int(1, 0)    // право
        };

        Vector3? nearestPosition = null;
        Vector2Int? nearestGridPos = null;
        float nearestDistance = float.MaxValue;

        foreach (Vector2Int offset in adjacentOffsets)
        {
            Vector2Int adjacentGridPos = block.gridPosition + offset;

            // Проверяем что клетка валидна и проходима
            if (gridManager.IsValidGridPosition(adjacentGridPos))
            {
                GridCell cell = gridManager.GetCell(adjacentGridPos);
                bool isOccupied = (cell != null && cell.isOccupied);

                // ПРОВЕРЯЕМ: нет ли на этой клетке блока СТЕНЫ в очереди строительства
                bool hasWallInQueue = IsWallBlockInQueue(adjacentGridPos);

                // Клетка должна быть свободна И на ней не должно быть стены в очереди строительства
                if (cell != null && !cell.isOccupied && !hasWallInQueue)
                {
                    Vector3 worldPos = gridManager.GridToWorld(adjacentGridPos);
                    float distance = Vector3.Distance(characterPosition, worldPos);

                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestPosition = worldPos;
                        nearestGridPos = adjacentGridPos;
                    }
                }
            }
        }

        if (!nearestPosition.HasValue)
        {
            Debug.LogWarning($"[ConstructionManager] → NO valid construction position found near block at {block.gridPosition}!");
        }

        return nearestPosition;
    }

    /// <summary>
    /// Проверить, есть ли блок стены в очереди строительства на данной клетке
    /// </summary>
    bool IsWallBlockInQueue(Vector2Int gridPosition)
    {
        foreach (ConstructionBlock queueBlock in constructionQueue)
        {
            if (queueBlock.gridPosition == gridPosition &&
                queueBlock.blockType == ConstructionBlock.BlockType.Wall &&
                !queueBlock.isCompleted)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Повернуть персонажа лицом к блоку (по 8 направлениям: прямые и диагонали)
    /// </summary>
    void RotateCharacterTowardsBlock(Character character, ConstructionBlock block)
    {
        // Вычисляем направление от персонажа к блоку
        Vector3 direction = block.worldPosition - character.transform.position;
        direction.y = 0; // Игнорируем вертикальную составляющую

        if (direction.magnitude < 0.1f)
        {
            // Персонаж уже на том же месте что и блок - не поворачиваем
            return;
        }

        // Вычисляем угол в градусах
        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        // Нормализуем угол к диапазону [0, 360)
        if (angle < 0) angle += 360f;

        // Округляем до ближайшего из 8 направлений (0°, 45°, 90°, 135°, 180°, 225°, 270°, 315°)
        float snappedAngle = Mathf.Round(angle / 45f) * 45f;

        // Применяем поворот
        character.transform.rotation = Quaternion.Euler(0, snappedAngle, 0);
    }

    /// <summary>
    /// Процесс строительства блока персонажем
    /// </summary>
    IEnumerator ConstructBlock(Character character, ConstructionBlock block)
    {
        // 1. Отправляем персонажа к блоку
        CharacterMovement movement = character.GetComponent<CharacterMovement>();
        if (movement == null)
        {
            Debug.LogError($"[ConstructionManager] Character {character.GetFullName()} has no CharacterMovement component");
            yield break;
        }

        // ПЕРЕКЛЮЧАЕМ AI персонажа в состояние Working (строительство)
        CharacterAI characterAI = character.GetComponent<CharacterAI>();
        if (characterAI != null)
        {
            characterAI.SetAIState(CharacterAI.AIState.Working);
        }

        // Получаем GridManager для проверки позиций в сетке
        Vector3 startPos = character.transform.position;
        if (gridManager == null)
        {
            LogError("GridManager not available!");
            OnConstructionFailed(character, block);
            yield break;
        }

        // ЕСЛИ БЛОК - СТЕНА, СРАЗУ ПОМЕЧАЕМ КЛЕТКУ КАК ЗАНЯТУЮ
        // Это предотвратит попадание других персонажей в клетку во время строительства
        if (block.blockType == ConstructionBlock.BlockType.Wall)
        {
            GridCell wallCell = gridManager.GetCell(block.gridPosition);
            if (wallCell != null && !wallCell.isOccupied)
            {
                wallCell.isOccupied = true;
            }
        }

        // Преобразуем мировую позицию персонажа в координаты сетки
        Vector2Int characterGridPos = gridManager.WorldToGrid(startPos);

        // Проверяем, находится ли персонаж УЖЕ на соседней клетке
        int gridDistanceX = Mathf.Abs(characterGridPos.x - block.gridPosition.x);
        int gridDistanceY = Mathf.Abs(characterGridPos.y - block.gridPosition.y);
        int maxGridDistance = Mathf.Max(gridDistanceX, gridDistanceY);
        bool isAdjacentToBlock = (maxGridDistance == 1);

        // Если персонаж УЖЕ на соседней клетке, НЕ перемещаем его
        if (isAdjacentToBlock)
        {
            // Пропускаем перемещение, идем сразу к анимации строительства
        }
        else
        {
            // Персонаж НЕ на соседней клетке - нужно переместить его
            // Находим ближайшую свободную клетку рядом с блоком
            Vector3? targetPosition = FindNearestValidConstructionPosition(startPos, block);

            if (!targetPosition.HasValue)
            {
                Debug.LogWarning($"[ConstructionManager] No valid construction position found near block at {block.gridPosition}");
                OnConstructionFailed(character, block);
                yield break;
            }

            movement.MoveTo(targetPosition.Value);

            // Ждем пока персонаж идет
            yield return new WaitForSeconds(0.2f); // Даем время начать движение
            while (movement.IsMoving())
            {
                // Проверяем что персонаж не умер
                if (character.IsDead())
                {
                    OnConstructionFailed(character, block);
                    yield break;
                }

                // ПРОВЕРЯЕМ ПРЕРЫВАНИЕ ВО ВРЕМЯ ДВИЖЕНИЯ К БЛОКУ
                if (characterAI != null && characterAI.GetCurrentState() != CharacterAI.AIState.Working)
                {
                    OnConstructionFailed(character, block);
                    yield break;
                }

                yield return null;
            }

            // 2. Проверяем что персонаж теперь на соседней клетке
            Vector3 finalPos = character.transform.position;
            Vector2Int finalGridPos = gridManager.WorldToGrid(finalPos);
            int finalGridDistanceX = Mathf.Abs(finalGridPos.x - block.gridPosition.x);
            int finalGridDistanceY = Mathf.Abs(finalGridPos.y - block.gridPosition.y);
            int finalMaxGridDistance = Mathf.Max(finalGridDistanceX, finalGridDistanceY);

            if (finalMaxGridDistance > 1)
            {
                Debug.LogWarning($"[ConstructionManager] {character.GetFullName()} couldn't reach block at {block.gridPosition}, grid distance: {finalMaxGridDistance} cells > 1");
                OnConstructionFailed(character, block);
                yield break;
            }
        } // Закрываем блок else

        // ПОВОРАЧИВАЕМ персонажа лицом к блоку
        RotateCharacterTowardsBlock(character, block);

        // Создаем полосу прогресса для блока
        CreateProgressBar(block);

        // 3. Прыгаем 5 раз (анимация строительства)
        for (int i = 0; i < jumpsRequired; i++)
        {
            // Проверяем что персонаж не умер
            if (character.IsDead())
            {
                OnConstructionFailed(character, block);
                yield break;
            }

            // ПРОВЕРЯЕМ ЧТО ПЕРСОНАЖ ВСЕ ЕЩЕ В СОСТОЯНИИ Working (строительство не прервано)
            if (characterAI != null && characterAI.GetCurrentState() != CharacterAI.AIState.Working)
            {
                OnConstructionFailed(character, block);
                yield break;
            }

            // Прыжок
            yield return StartCoroutine(Jump(character));

            // Обновляем прогресс строительства
            float progress = (float)(i + 1) / jumpsRequired;
            UpdateProgressBar(block, progress);

            // Пауза между прыжками
            if (i < jumpsRequired - 1)
            {
                yield return new WaitForSeconds(jumpInterval);

                // ПРОВЕРЯЕМ ПРЕРЫВАНИЕ ПОСЛЕ ПАУЗЫ
                if (characterAI != null && characterAI.GetCurrentState() != CharacterAI.AIState.Working)
                {
                    OnConstructionFailed(character, block);
                    yield break;
                }
            }
        }

        // Удаляем полосу прогресса
        RemoveProgressBar(block);

        // 4. Блок построен - заменяем на финальный префаб

        block.isCompleted = true;
        block.OnConstructionComplete?.Invoke();

        // ПЕРЕКЛЮЧАЕМ AI персонажа обратно в состояние PlayerControlled
        if (characterAI != null)
        {
            characterAI.SetAIState(CharacterAI.AIState.PlayerControlled);
        }

        // Удаляем из очереди
        constructionQueue.Remove(block);
        busyCharacters.Remove(character);

        // Удаляем корутину из словаря (строительство завершено нормально)
        if (constructionCoroutines.ContainsKey(character))
        {
            constructionCoroutines.Remove(character);
        }

        // Назначаем следующий блок этому персонажу
        if (constructionQueue.Count > 0)
        {
            AssignConstructionTask(character);
        }
        else
        {

        }
    }

    /// <summary>
    /// Анимация прыжка персонажа
    /// </summary>
    IEnumerator Jump(Character character)
    {
        Vector3 startPosition = character.transform.position;
        Vector3 jumpTarget = startPosition + Vector3.up * jumpHeight;
        float jumpDuration = 0.3f;
        float elapsed = 0f;

        // Прыжок вверх
        while (elapsed < jumpDuration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (jumpDuration / 2f);
            character.transform.position = Vector3.Lerp(startPosition, jumpTarget, t);
            yield return null;
        }

        // Падение вниз
        elapsed = 0f;
        while (elapsed < jumpDuration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (jumpDuration / 2f);
            character.transform.position = Vector3.Lerp(jumpTarget, startPosition, t);
            yield return null;
        }

        // Гарантируем возврат на исходную позицию
        character.transform.position = startPosition;
    }

    /// <summary>
    /// Обработка неудачного строительства
    /// </summary>
    void OnConstructionFailed(Character character, ConstructionBlock block)
    {
        Debug.LogWarning($"[ConstructionManager] Construction failed for {character.GetFullName()} at block {block.gridPosition}");

        // НЕ удаляем полосу прогресса - она должна остаться видимой!
        // Строительство может быть продолжено другим персонажем

        // ОСВОБОЖДАЕМ КЛЕТКУ СТЕНЫ, ЕСЛИ ОНА БЫЛА ПОМЕЧЕНА КАК ЗАНЯТАЯ
        if (block.blockType == ConstructionBlock.BlockType.Wall && gridManager != null)
        {
            GridCell wallCell = gridManager.GetCell(block.gridPosition);
            if (wallCell != null && wallCell.isOccupied)
            {
                wallCell.isOccupied = false;
            }
        }

        // ПЕРЕКЛЮЧАЕМ AI персонажа обратно в состояние PlayerControlled
        CharacterAI characterAI = character.GetComponent<CharacterAI>();
        if (characterAI != null)
        {
            characterAI.SetAIState(CharacterAI.AIState.PlayerControlled);
        }

        block.isAssigned = false;
        busyCharacters.Remove(character);

        // Удаляем корутину из словаря (строительство провалено)
        if (constructionCoroutines.ContainsKey(character))
        {
            constructionCoroutines.Remove(character);
        }

        // Если персонаж еще жив, назначаем ему другой блок
        if (!character.IsDead() && constructionQueue.Count > 0)
        {
            AssignConstructionTask(character);
        }
    }

    /// <summary>
    /// Очистить очередь строительства
    /// </summary>
    public void ClearConstructionQueue()
    {
        // Удаляем все полосы прогресса
        foreach (var progressBar in progressBars.Values)
        {
            if (progressBar != null)
            {
                Destroy(progressBar);
            }
        }
        progressBars.Clear();

        // ОСВОБОЖДАЕМ КЛЕТКИ СТЕН, КОТОРЫЕ БЫЛИ ПОМЕЧЕНЫ КАК ЗАНЯТЫЕ ВО ВРЕМЯ СТРОИТЕЛЬСТВА
        if (gridManager != null)
        {
            foreach (var block in constructionQueue)
            {
                if (block.blockType == ConstructionBlock.BlockType.Wall && block.isAssigned && !block.isCompleted)
                {
                    GridCell wallCell = gridManager.GetCell(block.gridPosition);
                    if (wallCell != null && wallCell.isOccupied)
                    {
                        wallCell.isOccupied = false;
                    }
                }
            }
        }

        // ОСТАНАВЛИВАЕМ ВСЕ КОРУТИНЫ СТРОИТЕЛЬСТВА
        foreach (var character in busyCharacters.Keys)
        {
            if (character != null)
            {
                // Останавливаем корутину
                if (constructionCoroutines.ContainsKey(character))
                {
                    StopCoroutine(constructionCoroutines[character]);
                }

                // Переключаем AI обратно в состояние PlayerControlled
                CharacterAI characterAI = character.GetComponent<CharacterAI>();
                if (characterAI != null)
                {
                    characterAI.SetAIState(CharacterAI.AIState.PlayerControlled);
                }
            }
        }

        constructionQueue.Clear();
        busyCharacters.Clear();
        constructionCoroutines.Clear(); // Очищаем словарь корутин

    }

    /// <summary>
    /// Получить количество блоков в очереди
    /// </summary>
    public int GetQueueSize()
    {
        return constructionQueue.Count;
    }

    /// <summary>
    /// ПУБЛИЧНЫЙ МЕТОД: Принудительно остановить строительство для конкретного персонажа
    /// Вызывается когда игрок отдает персонажу другую команду во время строительства
    /// </summary>
    public void StopConstructionForCharacter(Character character)
    {
        if (character == null)
            return;

        // Проверяем что персонаж занят строительством
        if (!busyCharacters.ContainsKey(character))
            return;

        ConstructionBlock block = busyCharacters[character];


        // ✓ КРИТИЧЕСКИ ВАЖНО: ОСТАНАВЛИВАЕМ КОРУТИНУ СТРОИТЕЛЬСТВА!
        if (constructionCoroutines.ContainsKey(character))
        {
            StopCoroutine(constructionCoroutines[character]);
            constructionCoroutines.Remove(character);

        }
        else
        {
            Debug.LogWarning($"[ConstructionManager] ⚠ Корутина не найдена для {character.GetFullName()}!");
        }

        // НЕ удаляем полосу прогресса - она должна остаться видимой!
        // Полоса будет удалена только когда строительство завершится

        // ОСВОБОЖДАЕМ КЛЕТКУ СТЕНЫ, ЕСЛИ ОНА БЫЛА ПОМЕЧЕНА КАК ЗАНЯТАЯ
        if (block.blockType == ConstructionBlock.BlockType.Wall && gridManager != null)
        {
            GridCell wallCell = gridManager.GetCell(block.gridPosition);
            if (wallCell != null && wallCell.isOccupied)
            {
                wallCell.isOccupied = false;
            }
        }

        // Освобождаем блок для других персонажей
        block.isAssigned = false;

        // Удаляем персонажа из занятых
        busyCharacters.Remove(character);


    }

    /// <summary>
    /// Создать полосу прогресса для блока (или вернуть существующую)
    /// </summary>
    GameObject CreateProgressBar(ConstructionBlock block)
    {
        // Проверяем, существует ли уже полоса прогресса для этого блока
        if (progressBars.ContainsKey(block) && progressBars[block] != null)
        {
            return progressBars[block];
        }

        // Создаем контейнер для полосы прогресса
        GameObject progressBarContainer = new GameObject($"ProgressBar_{block.gridPosition}");
        progressBarContainer.transform.position = block.worldPosition + Vector3.up * progressBarHeight;

        // Добавляем Billboard компонент для поворота к камере
        progressBarContainer.AddComponent<Billboard>();

        // Создаем Canvas для мирового пространства
        Canvas canvas = progressBarContainer.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        // Настраиваем размер Canvas
        RectTransform canvasRect = progressBarContainer.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(progressBarWidth, progressBarThickness);
        canvasRect.localScale = Vector3.one * progressBarScale; // Масштаб для мирового пространства

        // Создаем фон полосы
        GameObject background = new GameObject("Background");
        background.transform.SetParent(progressBarContainer.transform, false);

        UnityEngine.UI.Image bgImage = background.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = progressBarBackgroundColor;

        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Создаем заполнение полосы (растёт слева направо)
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(background.transform, false);

        UnityEngine.UI.Image fillImage = fill.AddComponent<UnityEngine.UI.Image>();
        fillImage.color = progressBarFillColor;

        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0, 0);     // Левый нижний угол
        fillRect.anchorMax = new Vector2(0, 1);     // Левый верхний угол (ширина = 0)
        fillRect.pivot = new Vector2(0, 0.5f);      // Pivot слева
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        // Сохраняем ссылку на полосу
        progressBars[block] = progressBarContainer;

        return progressBarContainer;
    }

    /// <summary>
    /// Обновить прогресс строительства блока
    /// </summary>
    void UpdateProgressBar(ConstructionBlock block, float progress)
    {
        if (progressBars.ContainsKey(block) && progressBars[block] != null)
        {
            // Находим Fill RectTransform
            Transform fillTransform = progressBars[block].transform.Find("Background/Fill");
            if (fillTransform != null)
            {
                RectTransform fillRect = fillTransform.GetComponent<RectTransform>();
                if (fillRect != null)
                {
                    // Изменяем ширину полосы от 0 до 1 (0% до 100%)
                    fillRect.anchorMax = new Vector2(progress, 1);
                }
            }
        }
    }

    /// <summary>
    /// Удалить полосу прогресса блока
    /// </summary>
    void RemoveProgressBar(ConstructionBlock block)
    {
        if (progressBars.ContainsKey(block))
        {
            if (progressBars[block] != null)
            {
                Destroy(progressBars[block]);
            }
            progressBars.Remove(block);
        }
    }
}

/// <summary>
/// Данные о блоке для строительства
/// </summary>
[System.Serializable]
public class ConstructionBlock
{
    public Vector2Int gridPosition;
    public Vector3 worldPosition;
    public GameObject ghostObject;
    public BlockType blockType;
    public bool isAssigned = false;
    public bool isCompleted = false;

    // Callback когда строительство завершено
    public System.Action OnConstructionComplete;

    public enum BlockType
    {
        Wall,
        Floor
    }

    public ConstructionBlock(Vector2Int gridPos, Vector3 worldPos, GameObject ghost, BlockType type)
    {
        gridPosition = gridPos;
        worldPosition = worldPos;
        ghostObject = ghost;
        blockType = type;
    }
}
