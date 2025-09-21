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
    public bool debugMode = false;

    // Состояния ИИ
    public enum AIState
    {
        PlayerControlled,  // Под управлением игрока
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

        if (isSelected)
        {
            // Персонаж выделен - обновляем время последнего выделения
            lastSelectionTime = Time.time;

            if (currentState != AIState.PlayerControlled)
            {
                SwitchToState(AIState.PlayerControlled);
            }
        }
        else
        {
            // Персонаж не выделен - проверяем timeout
            float timeSinceSelection = Time.time - lastSelectionTime;

            if (timeSinceSelection >= idleTimeout && currentState != AIState.Idle)
            {
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

        if (debugMode)
        {
            Debug.Log($"[CharacterAI] {character.GetFullName()}: {currentState} -> {newState}");
        }

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

        while (currentState == AIState.Idle && isWandering)
        {
            // Выбираем случайную точку в области 5x5 клеток
            Vector3 wanderTarget = GetRandomWanderPoint();

            if (debugMode)
            {
                Debug.Log($"[CharacterAI] {character.GetFullName()}: Moving to wander point {wanderTarget}");
            }

            // Двигаемся к цели
            if (movement != null)
            {
                movement.MoveTo(wanderTarget);

                // Ждем завершения движения
                while (movement.IsMoving() && currentState == AIState.Idle)
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }

            // Проверяем, что мы все еще в состоянии Idle
            if (currentState != AIState.Idle) break;

            if (debugMode)
            {
                Debug.Log($"[CharacterAI] {character.GetFullName()}: Arrived at wander point, pausing for {pauseDuration}s");
            }

            // Пауза в достигнутой точке
            yield return new WaitForSeconds(pauseDuration);

            // Проверяем, что мы все еще в состоянии Idle
            if (currentState != AIState.Idle) break;

            // Ждем до следующего перемещения
            yield return new WaitForSeconds(wanderInterval);
        }

        isWandering = false;
    }

    /// <summary>
    /// Получить случайную точку для блуждания в области 5x5 клеток
    /// </summary>
    Vector3 GetRandomWanderPoint()
    {
        // Конвертируем базовую позицию в координаты сетки
        Vector2Int baseGridPos = gridManager.WorldToGrid(idleBasePosition);

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
                return gridManager.GridToWorld(targetGridPos);
            }
        }

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
                    return gridManager.GridToWorld(targetGridPos);
                }
            }
        }

        // Если не нашли свободную клетку, возвращаем базовую позицию
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