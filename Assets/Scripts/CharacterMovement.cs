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
        Debug.Log($"CharacterMovement [{name}]: НОВАЯ КОМАНДА движения к {worldPosition}");
        
        targetPosition = worldPosition;
        
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
        
        Debug.Log($"CharacterMovement [{name}]: запущено новое движение к {worldPosition}");
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
        
        Debug.Log($"CharacterMovement [{name}]: ищу путь от клетки {startGrid} к клетке {targetGrid}");
        Debug.Log($"CharacterMovement [{name}]: мировые координаты от {startPosition} к {targetPosition}");
        
        currentPath = pathfinder.FindPath(startGrid, targetGrid);
        currentPathIndex = 0;
        
        if (currentPath != null && currentPath.Count > 0)
        {
            Debug.Log($"Найден путь длиной {currentPath.Count} клеток");
            // Движение по найденному пути
            yield return StartCoroutine(MoveAlongPath());
        }
        else
        {
            // Прямое движение если путь не найден
            Debug.Log("Путь не найден, движение напрямую");
            yield return StartCoroutine(MoveDirectly());
        }
        
        // Завершение движения
        isMoving = false;
        movementCoroutine = null;
        currentPath = null;
        
        // Скрываем путь
        HidePathLine();
        
        Debug.Log($"CharacterMovement [{name}]: ПОЛНОЕ ЗАВЕРШЕНИЕ движения, вызываем событие");
        
        // Вызываем событие завершения движения
        OnMovementComplete?.Invoke(this);
    }
    
    /// <summary>
    /// Движение по найденному пути
    /// </summary>
    IEnumerator MoveAlongPath()
    {
        for (currentPathIndex = 0; currentPathIndex < currentPath.Count; currentPathIndex++)
        {
            Vector3 nextTarget = gridManager.GridToWorld(currentPath[currentPathIndex]);
            
            // Проверяем, не слишком ли близко мы уже к этой точке
            float distanceToTarget = Vector3.Distance(transform.position, nextTarget);
            Debug.Log($"CharacterMovement [{name}]: движение к точке {currentPathIndex}: {currentPath[currentPathIndex]}, расстояние: {distanceToTarget:F2}");
            
            // Если уже близко к точке (меньше 0.5 единиц), пропускаем ее
            if (distanceToTarget < 0.5f)
            {
                Debug.Log($"CharacterMovement [{name}]: точка {currentPathIndex} слишком близко, пропускаем");
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
                
                // Логирование каждые 30 кадров (примерно каждые полсекунды)
                if (frameCount % 30 == 0)
                {
                    float dist = Vector3.Distance(currentPos, nextTarget);
                    Debug.Log($"CharacterMovement [{name}]: движение к точке {currentPathIndex}, расстояние: {dist:F3}, скорость: {moveThisFrame:F3}");
                }
                
                if (Vector3.Distance(currentPos, nextTarget) <= moveThisFrame)
                {
                    transform.position = nextTarget;
                    Debug.Log($"CharacterMovement [{name}]: достиг точки {currentPathIndex} за один кадр");
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
            Debug.Log($"CharacterMovement [{name}]: достиг точки {currentPathIndex}: {currentPath[currentPathIndex]}");
        }
        
        Debug.Log($"CharacterMovement [{name}]: путь завершен, все {currentPath.Count} точек пройдены");
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
        Debug.Log($"CharacterMovement [{name}]: ОСТАНАВЛИВАЕМ движение - была корутина: {movementCoroutine != null}, было движение: {isMoving}");
        
        // Останавливаем корутину
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
            Debug.Log($"CharacterMovement [{name}]: корутина остановлена");
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
        
        Debug.Log($"CharacterMovement [{name}]: движение ПОЛНОСТЬЮ остановлено и очищено");
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
            Vector3[] positions = { transform.position, targetPosition };
            positions[0].y += 0.5f; // Приподнимаем над землей
            positions[1].y += 0.5f;
            
            pathLine.positionCount = 2;
            pathLine.SetPositions(positions);
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
    
    void OnDestroy()
    {
        StopMovement();
    }
}