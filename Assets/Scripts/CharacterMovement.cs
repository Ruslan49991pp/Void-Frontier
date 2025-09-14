using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Простое движение персонажа к цели с постоянной скоростью
/// </summary>
public class CharacterMovement : MonoBehaviour
{
    public static readonly float MOVE_SPEED = 5f; // Единая скорость для всех персонажей
    
    private Vector3 targetPosition;
    private bool isMoving = false;
    private Coroutine movementCoroutine;
    private LineRenderer pathLine;
    
    // Система навигации
    private SimplePathfinder pathfinder;
    private GridManager gridManager;
    private List<Vector2Int> currentPath;
    private int currentPathIndex;
    private Vector2Int originalTarget; // Изначальная цель для проверки занятости
    private Vector2Int lastOccupiedCell; // Последняя занимаемая клетка для обновления занятости
    
    // События
    public System.Action<CharacterMovement> OnMovementComplete;
    
    void Awake()
    {
        CreatePathLine();
        gridManager = FindObjectOfType<GridManager>();
        pathfinder = FindObjectOfType<SimplePathfinder>();
        
        if (pathfinder == null)
        {
            // Создаем pathfinder если его нет
            GameObject pathfinderGO = new GameObject("SimplePathfinder");
            pathfinder = pathfinderGO.AddComponent<SimplePathfinder>();
        }
    }

    void Start()
    {
        // Инициализируем последнюю занимаемую клетку текущей позицией
        if (gridManager != null)
        {
            lastOccupiedCell = gridManager.WorldToGrid(transform.position);
        }
    }
    
    /// <summary>
    /// Создать LineRenderer для визуализации пути
    /// </summary>
    void CreatePathLine()
    {
        GameObject lineObj = new GameObject("PathLine");
        lineObj.transform.SetParent(transform);
        pathLine = lineObj.AddComponent<LineRenderer>();
        
        pathLine.material = new Material(Shader.Find("Sprites/Default"));
        pathLine.material.color = Color.blue;
        pathLine.startWidth = 0.2f;
        pathLine.endWidth = 0.2f;
        pathLine.positionCount = 0;
        pathLine.useWorldSpace = true;
        pathLine.sortingOrder = 1;
        pathLine.enabled = false;
    }
    
    /// <summary>
    /// Переместить персонажа к целевой позиции
    /// </summary>
    public void MoveTo(Vector3 worldPosition)
    {

        targetPosition = worldPosition;

        // Сохраняем изначальную цель для мониторинга занятости
        originalTarget = gridManager.WorldToGrid(worldPosition);

        // ПОЛНОЕ обнуление предыдущего состояния
        StopMovement();

        // Небольшая задержка чтобы корутины точно остановились
        StartCoroutine(StartMovementAfterDelay(worldPosition));
    }
    
    IEnumerator StartMovementAfterDelay(Vector3 worldPosition)
    {
        yield return null; // Ждем один кадр
        
        targetPosition = worldPosition;
        
        // Показываем новый путь
        ShowPathLine();
        
        // Немедленно начинаем новое движение
        movementCoroutine = StartCoroutine(MoveToTarget());
        
    }
    
    /// <summary>
    /// Корутина движения к цели с постоянной скоростью
    /// </summary>
    IEnumerator MoveToTarget()
    {
        Vector3 startPosition = transform.position;
        
        // Если уже на месте, выходим
        if (Vector3.Distance(startPosition, targetPosition) < 0.1f)
        {
            yield break;
        }
        
        isMoving = true;
        
        // Находим путь с обходом препятствий
        Vector2Int startGrid = gridManager.WorldToGrid(startPosition);
        Vector2Int targetGrid = gridManager.WorldToGrid(targetPosition);
        
        
        currentPath = pathfinder.FindPath(startGrid, targetGrid);
        currentPathIndex = 0;
        
        if (currentPath != null && currentPath.Count > 0)
        {
            // Движение по найденному пути
            yield return StartCoroutine(MoveAlongPath());
        }
        else
        {
            // Прямое движение если путь не найден
            yield return StartCoroutine(MoveDirectly());
        }
        
        // Завершение движения
        isMoving = false;
        movementCoroutine = null;
        currentPath = null;
        
        // Скрываем путь
        HidePathLine();

        // Обновляем занятость клеток после завершения движения
        UpdateCellOccupancy();

        // Вызываем событие завершения движения
        OnMovementComplete?.Invoke(this);
    }
    
    /// <summary>
    /// Движение по найденному пути
    /// </summary>
    IEnumerator MoveAlongPath()
    {
        // Проверяем, что путь существует
        if (currentPath == null || currentPath.Count == 0)
        {
            // Завершение движения
            isMoving = false;
            movementCoroutine = null;
            currentPath = null;

            // Скрываем путь
            HidePathLine();

            // Обновляем занятость клеток после завершения движения
            UpdateCellOccupancy();

            // Вызываем событие завершения движения
            OnMovementComplete?.Invoke(this);
            yield break;
        }

        for (currentPathIndex = 0; currentPathIndex < currentPath.Count; currentPathIndex++)
        {
            // Дополнительная проверка на случай изменения пути во время выполнения
            if (currentPath == null || currentPathIndex >= currentPath.Count)
            {
                yield break;
            }

            Vector3 nextTarget = gridManager.GridToWorld(currentPath[currentPathIndex]);

            // Проверяем, не слишком ли близко мы уже к этой точке
            float distanceToTarget = Vector3.Distance(transform.position, nextTarget);
            
            // Если уже близко к точке (меньше 0.5 единиц), пропускаем ее
            if (distanceToTarget < 0.5f)
            {
                transform.position = nextTarget; // Корректируем позицию
                continue;
            }
            
            // Движение к следующей точке пути
            int frameCount = 0;
            while (Vector3.Distance(transform.position, nextTarget) > 0.1f)
            {
                Vector3 currentPos = transform.position;
                Vector3 direction = (nextTarget - currentPos).normalized;
                float moveThisFrame = MOVE_SPEED * Time.deltaTime;
                
                // Проверяем занятость цели каждые 10 кадров
                if (frameCount % 10 == 0)
                {
                    if (CheckAndHandleTargetOccupancy())
                    {
                        yield break; // Путь был перестроен, выходим
                    }
                }
                
                if (Vector3.Distance(currentPos, nextTarget) <= moveThisFrame)
                {
                    transform.position = nextTarget;
                    break;
                }
                
                transform.position = currentPos + direction * moveThisFrame;
                UpdatePathLine();
                frameCount++;
                yield return null;
                
                // Защита от бесконечного цикла
                if (frameCount > 1000)
                {
                    Debug.LogError($"CharacterMovement [{name}]: застрял в движении к точке {currentPathIndex}, принудительно завершаем");
                    transform.position = nextTarget;
                    break;
                }
            }
            
            transform.position = nextTarget;
        }
        
    }
    
    /// <summary>
    /// Прямое движение к цели (fallback)
    /// </summary>
    IEnumerator MoveDirectly()
    {
        while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
        {
            Vector3 currentPos = transform.position;
            Vector3 direction = (targetPosition - currentPos).normalized;
            float moveThisFrame = MOVE_SPEED * Time.deltaTime;
            
            if (Vector3.Distance(currentPos, targetPosition) <= moveThisFrame)
            {
                transform.position = targetPosition;
                break;
            }
            
            transform.position = currentPos + direction * moveThisFrame;
            UpdatePathLine();
            yield return null;
        }
        
        transform.position = targetPosition;
    }
    
    /// <summary>
    /// Проверить, движется ли персонаж
    /// </summary>
    public bool IsMoving()
    {
        return isMoving;
    }
    
    /// <summary>
    /// Остановить текущее движение
    /// </summary>
    public void StopMovement()
    {
        
        // Останавливаем корутину
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }
        
        // Останавливаем ВСЕ корутины для надежности
        StopAllCoroutines();
        
        // Полная очистка состояния
        isMoving = false;
        currentPath = null;
        currentPathIndex = 0;
        
        // Очищаем событие
        OnMovementComplete = null;
        
        HidePathLine();
        
    }
    
    /// <summary>
    /// Показать линию пути
    /// </summary>
    void ShowPathLine()
    {
        if (pathLine != null)
        {
            UpdatePathLine();
            pathLine.enabled = true;
        }
    }
    
    /// <summary>
    /// Обновить позиции линии пути
    /// </summary>
    void UpdatePathLine()
    {
        if (pathLine != null && pathLine.enabled)
        {
            if (currentPath != null && currentPath.Count > 0 && gridManager != null)
            {
                // Создаем массив позиций для всего пути
                List<Vector3> pathPositions = new List<Vector3>();

                // Добавляем текущую позицию персонажа
                Vector3 startPos = transform.position;
                startPos.y += 0.5f; // Приподнимаем над землей
                pathPositions.Add(startPos);

                // Добавляем все точки пути, начиная с текущего индекса
                for (int i = currentPathIndex; i < currentPath.Count; i++)
                {
                    GridCell cell = gridManager.GetCell(currentPath[i]);
                    if (cell != null)
                    {
                        Vector3 worldPos = cell.worldPosition;
                        worldPos.y += 0.5f; // Приподнимаем над землей
                        pathPositions.Add(worldPos);
                    }
                }

                // Устанавливаем позиции линии
                pathLine.positionCount = pathPositions.Count;
                pathLine.SetPositions(pathPositions.ToArray());
            }
            else
            {
                // Fallback: если нет пути, показываем прямую линию
                Vector3[] positions = { transform.position, targetPosition };
                positions[0].y += 0.5f; // Приподнимаем над землей
                positions[1].y += 0.5f;

                pathLine.positionCount = 2;
                pathLine.SetPositions(positions);
            }
        }
    }
    
    /// <summary>
    /// Скрыть линию пути
    /// </summary>
    void HidePathLine()
    {
        if (pathLine != null)
        {
            pathLine.enabled = false;
        }
    }
    
    /// <summary>
    /// Проверить занятость изначальной цели и при необходимости перестроить путь
    /// </summary>
    bool CheckAndHandleTargetOccupancy()
    {
        if (gridManager == null || pathfinder == null)
            return false;

        // Проверяем, занята ли изначальная целевая клетка другим персонажем
        var targetCell = gridManager.GetCell(originalTarget);
        if (targetCell != null && targetCell.isOccupied && targetCell.objectType == "Character")
        {
            // Проверяем, не являемся ли мы сами занимающим эту клетку персонажем
            Vector2Int myCurrentPos = gridManager.WorldToGrid(transform.position);
            if (myCurrentPos == originalTarget)
            {
                return false; // Мы сами можем занимать эту клетку
            }
            // Находим ближайшую свободную клетку к изначальной цели
            Vector2Int newTarget = FindNearestFreeCell(originalTarget);

            if (newTarget != originalTarget)
            {
                // Перестраиваем путь к новой цели
                Vector3 newTargetWorld = gridManager.GridToWorld(newTarget);
                targetPosition = newTargetWorld;

                // Обновляем originalTarget на новую цель
                originalTarget = newTarget;

                // Получаем текущую позицию в сетке
                Vector2Int currentGridPos = gridManager.WorldToGrid(transform.position);

                // Строим новый путь
                List<Vector2Int> newPath = pathfinder.FindPath(currentGridPos, newTarget);
                if (newPath != null && newPath.Count > 0)
                {
                    currentPath = newPath;
                    currentPathIndex = 0;

                    // Останавливаем текущую корутину перед запуском новой
                    if (movementCoroutine != null)
                    {
                        StopCoroutine(movementCoroutine);
                    }

                    // Обновляем визуализацию пути
                    UpdatePathLine();

                    // Запускаем новое движение
                    movementCoroutine = StartCoroutine(MoveAlongPath());

                    return true; // Путь был перестроен
                }
            }
        }

        return false; // Перестроения не было
    }

    /// <summary>
    /// Найти ближайшую свободную клетку к указанной позиции
    /// </summary>
    Vector2Int FindNearestFreeCell(Vector2Int targetPos)
    {
        // Проверяем саму целевую клетку
        var cell = gridManager.GetCell(targetPos);
        if (cell == null || !cell.isOccupied)
        {
            return targetPos;
        }

        // Ищем свободные клетки в радиусе от цели
        for (int radius = 1; radius <= 5; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    // Проверяем только клетки на границе текущего радиуса
                    if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        continue;

                    Vector2Int checkPos = new Vector2Int(targetPos.x + x, targetPos.y + y);

                    if (gridManager.IsValidGridPosition(checkPos))
                    {
                        var checkCell = gridManager.GetCell(checkPos);
                        if (checkCell == null || !checkCell.isOccupied)
                        {
                            return checkPos;
                        }
                    }
                }
            }
        }

        // Если не нашли свободную клетку, возвращаем изначальную
        return targetPos;
    }

    /// <summary>
    /// Обновить занятость клеток после движения персонажа
    /// </summary>
    void UpdateCellOccupancy()
    {
        if (gridManager == null)
            return;

        Vector2Int currentCell = gridManager.WorldToGrid(transform.position);

        // Если позиция изменилась, обновляем занятость
        if (currentCell != lastOccupiedCell)
        {
            // Освобождаем предыдущую клетку
            if (gridManager.IsValidGridPosition(lastOccupiedCell))
            {
                gridManager.FreeCell(lastOccupiedCell);
            }

            // Занимаем новую клетку
            if (gridManager.IsValidGridPosition(currentCell))
            {
                gridManager.OccupyCell(currentCell, gameObject, "Character");
            }

            // Обновляем отслеживаемую позицию
            lastOccupiedCell = currentCell;
        }
    }

    void OnDestroy()
    {
        StopMovement();

        // Освобождаем клетку при уничтожении персонажа
        if (gridManager != null && gridManager.IsValidGridPosition(lastOccupiedCell))
        {
            gridManager.FreeCell(lastOccupiedCell);
        }
    }
}