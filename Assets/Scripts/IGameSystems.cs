using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Интерфейсы для всех основных игровых систем
///
/// АРХИТЕКТУРА: Интерфейсы позволяют:
/// 1. Отделить реализацию от контракта (SOLID - Dependency Inversion)
/// 2. Упростить тестирование (можно создавать моки)
/// 3. Сделать код более гибким и расширяемым
/// 4. Четко определить API каждой системы
///
/// ИСПОЛЬЗОВАНИЕ:
///   // Вместо конкретного класса используем интерфейс
///   IGridManager gridManager = ServiceLocator.Get<IGridManager>();
///   GridCell cell = gridManager.GetCell(position);
/// </summary>

// ============================================================================
// GRID СИСТЕМА
// ============================================================================

/// <summary>
/// Интерфейс для системы управления сеткой
/// Отвечает за пространственное разделение мира и управление ячейками
/// </summary>
public interface IGridManager
{
    // Получение ячейки по позиции
    GridCell GetCell(Vector2Int gridPosition);
    GridCell GetCellFromWorld(Vector3 worldPosition);

    // Преобразование координат
    Vector2Int WorldToGrid(Vector3 worldPosition);
    Vector3 GridToWorld(Vector2Int gridPosition);

    // Проверки
    bool IsValidGridPosition(Vector2Int gridPosition);
    bool IsCellOccupied(Vector2Int gridPosition);

    // Управление ячейками
    void OccupyCell(Vector2Int gridPosition, GameObject occupant);
    void FreeCell(Vector2Int gridPosition);

    // Визуализация
    void ShowGrid();
    void HideGrid();
}

// ============================================================================
// SELECTION СИСТЕМА
// ============================================================================

/// <summary>
/// Интерфейс для системы выделения объектов
/// Отвечает за выделение юнитов, зданий и предметов
/// </summary>
public interface ISelectionManager
{
    // Получение выделенных объектов
    List<GameObject> GetSelectedObjects();
    bool IsSelected(GameObject obj);

    // Управление выделением
    void AddToSelection(GameObject obj);
    void RemoveFromSelection(GameObject obj);
    void ClearSelection();
    void ToggleSelection(GameObject obj);

    // События
    event System.Action<List<GameObject>> OnSelectionChanged;

    // Состояние
    bool IsBoxSelecting { get; }
    bool RightClickHandledThisFrame { get; }
}

// ============================================================================
// BUILDING СИСТЕМА
// ============================================================================

/// <summary>
/// Интерфейс для системы строительства корабля
/// Отвечает за размещение модулей и управление режимом строительства
/// </summary>
public interface IBuildingSystem
{
    // Режим строительства
    bool IsBuildingModeActive();
    void EnterBuildMode();
    void ExitBuildMode();

    // Размещение модулей
    bool CanPlaceModule(GameObject modulePrefab, Vector2Int position);
    GameObject PlaceModule(GameObject modulePrefab, Vector2Int position);

    // Удаление модулей
    bool CanDeleteModule(Vector2Int position);
    void DeleteModule(Vector2Int position);

    // Управление ресурсами
    bool HasEnoughResources(int metalCost);
    void SpendResources(int metalCost);

    // Состояние
    bool IsScrollWheelUsedThisFrame();
}

// ============================================================================
// COMBAT СИСТЕМА
// ============================================================================

/// <summary>
/// Интерфейс для боевой системы
/// Отвечает за управление боем между персонажами
/// </summary>
public interface ICombatSystem
{
    // Начало/остановка боя
    void StartCombat(Character attacker, Character target);
    void StopCombat(Character character);

    // Проверки
    bool IsInCombat(Character character);
    Character GetCurrentTarget(Character character);

    // Управление позициями
    Vector3 GetCombatPosition(Character character, Character target);
    void ReserveCombatPosition(Character character, Vector3 position);
    void ReleaseCombatPosition(Character character);
}

// ============================================================================
// MINING СИСТЕМА
// ============================================================================

/// <summary>
/// Интерфейс для системы добычи ресурсов
/// Отвечает за добычу металла из астероидов
/// </summary>
public interface IMiningManager
{
    // Начало/остановка добычи
    void StartMining(Character character, GameObject asteroid);
    void StopMining(Character character);

    // Проверки
    bool IsMining(Character character);
    GameObject GetMiningTarget(Character character);

    // Прогресс добычи
    float GetMiningProgress(Character character);
}

// ============================================================================
// CONSTRUCTION СИСТЕМА
// ============================================================================

/// <summary>
/// Интерфейс для системы строительства объектов
/// Отвечает за строительство модулей персонажами
/// </summary>
public interface IConstructionManager
{
    // Начало/остановка строительства
    void StartConstruction(Character character, GameObject constructionSite);
    void StopConstruction(Character character);

    // Проверки
    bool IsConstructing(Character character);
    GameObject GetConstructionSite(Character character);

    // Прогресс строительства
    float GetConstructionProgress(GameObject constructionSite);
}

// ============================================================================
// CAMERA СИСТЕМА
// ============================================================================

/// <summary>
/// Интерфейс для системы управления камерой
/// Отвечает за перемещение и зум камеры
/// </summary>
public interface ICameraController
{
    // Управление фокусом
    void CenterOnTarget();
    void SetFocusTarget(Transform target);

    // Режим следования
    void StartFollowingTarget(Transform target);
    void StopFollowingTarget();
    bool IsFollowingTarget();

    // Границы камеры
    void SetBoundsFromCollider(BoxCollider boundsCollider);
}

// ============================================================================
// RESOURCE СИСТЕМА
// ============================================================================

/// <summary>
/// Интерфейс для системы отображения ресурсов
/// Отвечает за UI панель ресурсов
/// </summary>
public interface IResourcePanel
{
    // Обновление ресурсов
    void UpdateResourceDisplay();
    void ForceRefresh();

    // Получение данных
    int GetTotalMetal();
    int GetCharacterCount();
}
