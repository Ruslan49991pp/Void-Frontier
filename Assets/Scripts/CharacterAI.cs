using System.Collections;
using UnityEngine;

/// <summary>
/// Система ИИ для персонажей с различными состояниями
/// </summary>
public class CharacterAI : MonoBehaviour
{
    [Header("AI Settings")]
    public float idleTimeout = 3f; // Время до перехода в Idle состояние
    public float wanderRadius = 2.5f; // Радиус блуждания (5x5 клеток = 2.5 радиуса)
    public float wanderInterval = 2f; // Интервал между точками блуждания
    public float pauseDuration = 2f; // Время остановки в точке

    [Header("Debug")]
    public bool debugMode = false; // Отключаем debug логи

    // Состояния ИИ
    public enum AIState
    {
        PlayerControlled,  // Под управлением игрока
        Move,             // Движение к цели
        Idle              // Свободное блуждание
    }

    // Компоненты
    private Character character;
    private CharacterMovement movement;
    private SelectionManager selectionManager;
    private GridManager gridManager;

    // Переменные состояния
    private AIState currentState = AIState.PlayerControlled;
    private float lastSelectionTime;
    private Vector3 idleBasePosition; // Базовая позиция для блуждания
    private Coroutine idleCoroutine;
    private bool isWandering = false;
    private bool playerInitiatedMovement = false; // Флаг движения, инициированного игроком

    void Awake()
    {
        character = GetComponent<Character>();
        movement = GetComponent<CharacterMovement>();

        // Добавляем CharacterMovement если его нет
        if (movement == null)
        {
            movement = gameObject.AddComponent<CharacterMovement>();
        }

        selectionManager = FindObjectOfType<SelectionManager>();
        gridManager = FindObjectOfType<GridManager>();
    }

    void Start()
    {
        lastSelectionTime = Time.time;
        idleBasePosition = transform.position;

        // Подписываемся на события выделения
        if (selectionManager != null)
        {
            selectionManager.OnSelectionChanged += OnSelectionChanged;
        }
    }

    void Update()
    {
        UpdateAIState();
        HandleCurrentState();
    }

    /// <summary>
    /// Обновление состояния ИИ в зависимости от выделения
    /// </summary>
    void UpdateAIState()
    {
        bool isSelected = character.IsSelected();
        bool isMoving = movement != null && movement.IsMoving();
        float timeSinceSelection = Time.time - lastSelectionTime;

        // Дебаг логи отключены
        // if (debugMode && Time.frameCount % 120 == 0) // Лог каждые 2 секунды (при 60 FPS)
        // {
        //              $"Selected={isSelected}, Moving={isMoving}, " +
        //              $"TimeSinceSelection={timeSinceSelection:F1}s, " +
        //              $"CurrentState={currentState}, " +
        //              $"IsWandering={isWandering}");
        // }

        if (isSelected)
        {
            // Персонаж выделен - обновляем время последнего выделения
            lastSelectionTime = Time.time;

            if (currentState != AIState.PlayerControlled)
            {
                // Debug logging disabled
                SwitchToState(AIState.PlayerControlled);
            }
        }
        else
        {
            // Персонаж не выделен

            if (isMoving)
            {
                // Если движение инициировано игроком, переключаемся в Move независимо от текущего состояния
                if (playerInitiatedMovement)
                {
                    if (currentState != AIState.Move)
                    {
                        // Debug logging disabled
                        SwitchToState(AIState.Move);
                    }
                    playerInitiatedMovement = false; // Сбрасываем флаг
                }
                // Если персонаж движется НЕ в состоянии Idle (автоматическое движение)
                else if (currentState != AIState.Idle && currentState != AIState.Move)
                {
                    // Debug logging disabled
                    SwitchToState(AIState.Move);
                }
                // В состоянии Idle движение является частью блуждания - не переключаемся
            }
            else if (timeSinceSelection >= idleTimeout && currentState != AIState.Idle)
            {
                // Персонаж не движется и прошло время - состояние Idle
                // Debug logging disabled
                SwitchToState(AIState.Idle);
            }
        }
    }

    /// <summary>
    /// Обработка текущего состояния
    /// </summary>
    void HandleCurrentState()
    {
        switch (currentState)
        {
            case AIState.PlayerControlled:
                // Ничего не делаем - персонаж под управлением игрока
                break;

            case AIState.Move:
                // Ничего не делаем - движение обрабатывается CharacterMovement
                break;

            case AIState.Idle:
                // Состояние обрабатывается корутиной
                break;
        }
    }

    /// <summary>
    /// Переключение состояния ИИ
    /// </summary>
    void SwitchToState(AIState newState)
    {
        if (currentState == newState) return;

        // Debug logging disabled

        // Выход из предыдущего состояния
        ExitState(currentState);

        // Смена состояния
        currentState = newState;

        // Вход в новое состояние
        EnterState(newState);
    }

    /// <summary>
    /// Выход из состояния
    /// </summary>
    void ExitState(AIState state)
    {
        switch (state)
        {
            case AIState.Move:
                // При выходе из состояния Move ничего особенного не делаем
                break;

            case AIState.Idle:
                // Останавливаем блуждание
                if (idleCoroutine != null)
                {
                    StopCoroutine(idleCoroutine);
                    idleCoroutine = null;
                }
                isWandering = false;

                // Останавливаем движение
                if (movement != null)
                {
                    movement.StopMovement();
                }
                break;
        }
    }

    /// <summary>
    /// Вход в состояние
    /// </summary>
    void EnterState(AIState state)
    {
        switch (state)
        {
            case AIState.PlayerControlled:
                // Запоминаем текущую позицию как базу для будущего блуждания
                idleBasePosition = transform.position;
                break;

            case AIState.Move:
                // При входе в состояние Move просто позволяем персонажу двигаться
                // Движение уже обрабатывается CharacterMovement
                break;

            case AIState.Idle:
                // Устанавливаем базовую позицию для блуждания
                idleBasePosition = transform.position;

                // Запускаем корутину блуждания
                idleCoroutine = StartCoroutine(IdleWanderBehavior());
                break;
        }
    }

    /// <summary>
    /// Корутина поведения в состоянии Idle
    /// </summary>
    IEnumerator IdleWanderBehavior()
    {
        isWandering = true;

        // Debug logging disabled

        while (currentState == AIState.Idle && isWandering)
        {
            // Выбираем случайную точку в области 5x5 клеток
            Vector3 wanderTarget = GetRandomWanderPoint();

            // Debug logging disabled

            // Двигаемся к цели
            if (movement != null)
            {
                bool moveStarted = false;
                try
                {
                    movement.MoveTo(wanderTarget);
                    moveStarted = true;

                    // Debug logging disabled
                }
                catch (System.Exception)
                {
                    // Debug logging disabled
                }

                if (moveStarted)
                {
                    // Ждем завершения движения
                    float waitTime = 0f;
                    while (movement.IsMoving() && currentState == AIState.Idle)
                    {
                        waitTime += 0.1f;
                        if (debugMode && waitTime % 2f < 0.1f) // Лог каждые 2 секунды
                        {

                        }
                        yield return new WaitForSeconds(0.1f);
                    }

                    // Debug logging disabled
                }
            }
            else
            {
                // Debug logging disabled
            }

            // Проверяем, что мы все еще в состоянии Idle
            if (currentState != AIState.Idle)
            {
                // Debug logging disabled

                break;
            }

            // Debug logging disabled

            // Пауза в достигнутой точке
            yield return new WaitForSeconds(pauseDuration);

            // Проверяем, что мы все еще в состоянии Idle
            if (currentState != AIState.Idle)
            {
                // Debug logging disabled

                break;
            }

            // Debug logging disabled

            // Ждем до следующего перемещения
            yield return new WaitForSeconds(wanderInterval);
        }

        isWandering = false;

        // Debug logging disabled
    }

    /// <summary>
    /// Получить случайную точку для блуждания в области 5x5 клеток
    /// </summary>
    Vector3 GetRandomWanderPoint()
    {
        if (gridManager == null)
        {
            // Debug logging disabled

            return idleBasePosition;
        }

        // Конвертируем базовую позицию в координаты сетки
        Vector2Int baseGridPos = gridManager.WorldToGrid(idleBasePosition);

        // Debug logging disabled

        // Случайная позиция в области 5x5 (от -2 до +2 клеток)
        int offsetX = Random.Range(-2, 3);
        int offsetY = Random.Range(-2, 3);

        Vector2Int targetGridPos = baseGridPos + new Vector2Int(offsetX, offsetY);

        // Проверяем валидность позиции
        if (gridManager.IsValidGridPosition(targetGridPos))
        {
            // Проверяем, что клетка не занята
            var cell = gridManager.GetCell(targetGridPos);
            if (cell == null || !cell.isOccupied)
            {
                Vector3 worldPos = gridManager.GridToWorld(targetGridPos);
                // Debug logging disabled
                return worldPos;
            }
            // Debug logging disabled
        }
        // Debug logging disabled

        // Если не удалось найти свободную клетку, пробуем несколько раз
        for (int attempts = 0; attempts < 10; attempts++)
        {
            offsetX = Random.Range(-2, 3);
            offsetY = Random.Range(-2, 3);
            targetGridPos = baseGridPos + new Vector2Int(offsetX, offsetY);

            if (gridManager.IsValidGridPosition(targetGridPos))
            {
                var cell = gridManager.GetCell(targetGridPos);
                if (cell == null || !cell.isOccupied)
                {
                    Vector3 worldPos = gridManager.GridToWorld(targetGridPos);
                    // Debug logging disabled
                    return worldPos;
                }
            }
        }

        // Если не нашли свободную клетку, возвращаем базовую позицию
        // Debug logging disabled
        return idleBasePosition;
    }

    /// <summary>
    /// Обработчик изменения выделения
    /// </summary>
    void OnSelectionChanged(System.Collections.Generic.List<GameObject> selectedObjects)
    {
        // Проверяем, выделен ли этот персонаж
        bool isSelected = selectedObjects.Contains(gameObject);

        if (isSelected)
        {
            lastSelectionTime = Time.time;
        }
    }

    /// <summary>
    /// Принудительно установить состояние ИИ
    /// </summary>
    public void SetAIState(AIState state)
    {
        SwitchToState(state);
    }

    /// <summary>
    /// Получить текущее состояние ИИ
    /// </summary>
    public AIState GetCurrentState()
    {
        return currentState;
    }

    /// <summary>
    /// Проверить, блуждает ли персонаж
    /// </summary>
    public bool IsWandering()
    {
        return isWandering;
    }

    /// <summary>
    /// Уведомить о том, что движение инициировано игроком
    /// </summary>
    public void OnPlayerInitiatedMovement()
    {
        playerInitiatedMovement = true;

        // Останавливаем бой при получении команды движения от игрока
        CombatSystem combatSystem = FindObjectOfType<CombatSystem>();
        if (combatSystem != null)
        {
            Character character = GetComponent<Character>();
            if (character != null)
            {
                combatSystem.StopCombatForCharacter(character);
            }
        }

        // Debug logging disabled
    }

    void OnDestroy()
    {
        // Отписываемся от событий
        if (selectionManager != null)
        {
            selectionManager.OnSelectionChanged -= OnSelectionChanged;
        }

        // Останавливаем корутины
        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (currentState == AIState.Idle && gridManager != null)
        {
            // Показываем область блуждания в Scene view
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(idleBasePosition, new Vector3(wanderRadius * 2, 0.1f, wanderRadius * 2));

            // Показываем базовую точку
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(idleBasePosition, 0.5f);
        }
    }
}