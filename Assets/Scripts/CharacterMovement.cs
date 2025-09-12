using System.Collections;
using UnityEngine;

/// <summary>
/// Компонент для плавного перемещения персонажа к целевой позиции
/// </summary>
public class CharacterMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 360f;
    public float arrivalThreshold = 0.1f;
    
    [Header("Animation")]
    public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    // Внутренние переменные
    private Vector3 targetPosition;
    private bool isMoving = false;
    private Coroutine movementCoroutine;
    private GridManager gridManager;
    private Character characterScript;
    
    // События
    public System.Action<CharacterMovement> OnMovementComplete;
    public System.Action<CharacterMovement> OnMovementStarted;
    
    void Awake()
    {
        characterScript = GetComponent<Character>();
        gridManager = FindObjectOfType<GridManager>();
    }
    
    /// <summary>
    /// Переместить персонажа к целевой позиции
    /// </summary>
    public void MoveTo(Vector3 worldPosition)
    {
        targetPosition = worldPosition;
        
        // Останавливаем предыдущее движение если оно было
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
        }
        
        // Начинаем новое движение
        movementCoroutine = StartCoroutine(MoveToTarget());
    }
    
    /// <summary>
    /// Переместить персонажа к целевой клетке сетки
    /// </summary>
    public void MoveToGridCell(Vector2Int gridPosition)
    {
        if (gridManager == null)
        {
            Debug.LogError("CharacterMovement: GridManager не найден!");
            return;
        }
        
        Vector3 worldPos = gridManager.GridToWorld(gridPosition);
        MoveTo(worldPos);
    }
    
    /// <summary>
    /// Корутина плавного движения к цели
    /// </summary>
    IEnumerator MoveToTarget()
    {
        Vector3 startPosition = transform.position;
        float distance = Vector3.Distance(startPosition, targetPosition);
        
        // Если уже на месте, выходим
        if (distance < arrivalThreshold)
        {
            OnMovementComplete?.Invoke(this);
            yield break;
        }
        
        isMoving = true;
        OnMovementStarted?.Invoke(this);
        
        // Обновляем занимаемые клетки в сетке
        UpdateGridOccupancy(startPosition, false); // Освобождаем старую позицию
        
        float journeyTime = distance / moveSpeed;
        float elapsedTime = 0;
        
        Debug.Log($"CharacterMovement [{characterScript?.GetFullName()}]: начинаем движение от {startPosition} к {targetPosition}, время: {journeyTime:F2}s");
        
        while (elapsedTime < journeyTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / journeyTime;
            progress = Mathf.Clamp01(progress);
            
            // Применяем кривую анимации
            float curveValue = movementCurve.Evaluate(progress);
            
            // Интерполируем позицию
            Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, curveValue);
            transform.position = currentPos;
            
            // Поворачиваем персонажа в сторону движения
            if (distance > 0.1f)
            {
                Vector3 direction = (targetPosition - startPosition).normalized;
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation, 
                        targetRotation, 
                        rotationSpeed * Time.deltaTime
                    );
                }
            }
            
            yield return null;
        }
        
        // Устанавливаем финальную позицию
        transform.position = targetPosition;
        
        // Занимаем новую клетку в сетке
        UpdateGridOccupancy(targetPosition, true);
        
        isMoving = false;
        movementCoroutine = null;
        
        Debug.Log($"CharacterMovement [{characterScript?.GetFullName()}]: движение завершено в позиции {targetPosition}");
        OnMovementComplete?.Invoke(this);
    }
    
    /// <summary>
    /// Обновление занятости клеток в сетке
    /// </summary>
    void UpdateGridOccupancy(Vector3 worldPosition, bool occupy)
    {
        if (gridManager == null) return;
        
        Vector2Int gridPos = gridManager.WorldToGrid(worldPosition);
        
        if (occupy)
        {
            gridManager.OccupyCell(gridPos, gameObject, "Character");
        }
        else
        {
            gridManager.FreeCell(gridPos);
        }
    }
    
    /// <summary>
    /// Остановить текущее движение
    /// </summary>
    public void StopMovement()
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }
        
        isMoving = false;
    }
    
    /// <summary>
    /// Проверить, движется ли персонаж
    /// </summary>
    public bool IsMoving()
    {
        return isMoving;
    }
    
    /// <summary>
    /// Получить текущую целевую позицию
    /// </summary>
    public Vector3 GetTargetPosition()
    {
        return targetPosition;
    }
    
    /// <summary>
    /// Получить прогресс движения (0-1)
    /// </summary>
    public float GetMovementProgress()
    {
        if (!isMoving) return 1f;
        
        float totalDistance = Vector3.Distance(transform.position, targetPosition);
        if (totalDistance < 0.01f) return 1f;
        
        // Примерная оценка прогресса
        return 1f - (totalDistance / Vector3.Distance(transform.position, targetPosition));
    }
    
    void OnDestroy()
    {
        StopMovement();
    }
}