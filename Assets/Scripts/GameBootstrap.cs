using UnityEngine;

/// <summary>
/// GameBootstrap - точка входа в игру, инициализирует все системы и регистрирует их в ServiceLocator
///
/// ВАЖНО: Этот скрипт должен быть первым в Script Execution Order (Edit -> Project Settings -> Script Execution Order)
/// Установите GameBootstrap на -1000 чтобы он выполнялся раньше всех остальных скриптов
///
/// Порядок инициализации:
/// 1. Находим все менеджеры в сцене
/// 2. Регистрируем их в ServiceLocator
/// 3. Уведомляем о завершении инициализации
///
/// После инициализации все системы могут использовать ServiceLocator.Get<T>() вместо FindObjectOfType
/// </summary>
[DefaultExecutionOrder(-1000)]
public class GameBootstrap : MonoBehaviour
{
    [Header("Debug")]
    [Tooltip("Выводить список зарегистрированных сервисов в консоль")]
    public bool debugPrintServices = true;

    void Awake()
    {
        Debug.Log("[GameBootstrap] Starting game initialization...");

        // Очищаем ServiceLocator на случай перезагрузки сцены
        ServiceLocator.Clear();

        // Регистрируем все системы
        RegisterServices();

        // Устанавливаем флаг инициализации
        ServiceLocator.SetInitialized();

        // Выводим список сервисов для отладки
        if (debugPrintServices)
        {
            ServiceLocator.DebugPrintServices();
        }

        Debug.Log("[GameBootstrap] Game initialization complete!");
    }

    /// <summary>
    /// Регистрация всех сервисов/менеджеров в ServiceLocator
    /// </summary>
    void RegisterServices()
    {
        // ВАЖНО: Регистрируем конкретные типы классов, а НЕ интерфейсы
        // Интерфейсы будут добавлены в Фазе 2.2

        // === CORE SYSTEMS ===

        // Grid система
        GridManager gridManager = FindObjectOfType<GridManager>();
        if (gridManager != null)
        {
            ServiceLocator.Register<GridManager>(gridManager);
        }
        else
        {
            Debug.LogWarning("[GameBootstrap] GridManager not found in scene!");
        }

        // Selection система
        SelectionManager selectionManager = FindObjectOfType<SelectionManager>();
        if (selectionManager != null)
        {
            ServiceLocator.Register<SelectionManager>(selectionManager);
        }
        else
        {
            Debug.LogWarning("[GameBootstrap] SelectionManager not found in scene!");
        }

        // Building система
        ShipBuildingSystem buildingSystem = FindObjectOfType<ShipBuildingSystem>();
        if (buildingSystem != null)
        {
            ServiceLocator.Register<ShipBuildingSystem>(buildingSystem);
        }
        else
        {
            Debug.LogWarning("[GameBootstrap] ShipBuildingSystem not found in scene!");
        }

        // Resource система
        ResourcePanelUI resourcePanel = FindObjectOfType<ResourcePanelUI>();
        if (resourcePanel != null)
        {
            ServiceLocator.Register<ResourcePanelUI>(resourcePanel);
        }
        else
        {
            Debug.LogWarning("[GameBootstrap] ResourcePanelUI not found in scene!");
        }

        // Camera система
        CameraController cameraController = FindObjectOfType<CameraController>();
        if (cameraController != null)
        {
            ServiceLocator.Register<CameraController>(cameraController);
        }
        else
        {
            Debug.LogWarning("[GameBootstrap] CameraController not found in scene!");
        }

        // Pause система
        GamePauseManager pauseManager = FindObjectOfType<GamePauseManager>();
        if (pauseManager != null)
        {
            ServiceLocator.Register<GamePauseManager>(pauseManager);
        }
        // Не выводим предупреждение - GamePauseManager может быть singleton

        // Mining система
        MiningManager miningManager = FindObjectOfType<MiningManager>();
        if (miningManager != null)
        {
            ServiceLocator.Register<MiningManager>(miningManager);
        }
        // Не выводим предупреждение - MiningManager создается динамически

        // Combat система
        CombatSystem combatSystem = FindObjectOfType<CombatSystem>();
        if (combatSystem != null)
        {
            ServiceLocator.Register<CombatSystem>(combatSystem);
        }
        // Не выводим предупреждение - CombatSystem может создаваться динамически

        // Construction система
        ConstructionManager constructionManager = FindObjectOfType<ConstructionManager>();
        if (constructionManager != null)
        {
            ServiceLocator.Register<ConstructionManager>(constructionManager);
        }
        // Не выводим предупреждение - ConstructionManager может отсутствовать

        Debug.Log($"[GameBootstrap] Registered {ServiceLocator.ServiceCount} services");
    }

    void OnDestroy()
    {
        // Очищаем ServiceLocator при уничтожении
        Debug.Log("[GameBootstrap] Cleaning up ServiceLocator...");
        ServiceLocator.Clear();
    }
}
