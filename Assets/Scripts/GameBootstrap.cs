using UnityEngine;

/// <summary>
/// GameBootstrap - точка входа в игру, инициализирует все системы и регистрирует их в ServiceLocator
///
/// ============================================================================
/// ВАЖНО: Script Execution Order
/// ============================================================================
/// Этот скрипт должен быть первым в Script Execution Order
/// (Edit -> Project Settings -> Script Execution Order)
/// Установите GameBootstrap на -1000 чтобы он выполнялся раньше всех остальных
///
/// ============================================================================
/// АРХИТЕКТУРА: Автоматическое развертывание систем (12 систем)
/// ============================================================================
/// GameBootstrap автоматически создает большинство необходимых систем если их нет в сцене.
/// Это критически важно для процедурной генерации локаций!
///
/// ВАЖНО: BuildMenuManager и ItemIconManager НЕ создаются автоматически,
/// так как требуют Inspector настройки ссылок на UI элементы!
///
/// АВТОМАТИЧЕСКИ СОЗДАЮТСЯ:
/// - Core: GridManager, SelectionManager, CombatSystem, ConstructionManager, MiningManager, EnemyTargetingSystem
/// - Building: ShipBuildingSystem
/// - UI: ResourcePanelUI, InventoryUI, EventSystem
/// - Camera: CameraController
/// - Game: GamePauseManager
///
/// Преимущества:
/// ✓ Процедурные локации работают сразу без ручной настройки
/// ✓ Единая точка конфигурации всех систем
/// ✓ Гарантированная консистентность между сценами
/// ✓ Гибкость - можно переопределить систему в конкретной сцене
///
/// ============================================================================
/// ИСПОЛЬЗОВАНИЕ ДЛЯ ПРОЦЕДУРНЫХ ЛОКАЦИЙ
/// ============================================================================
/// 1. Создайте новую сцену (или генерируйте процедурно)
/// 2. Добавьте GameObject с GameBootstrap компонентом
/// 3. ВСЁ! Все системы создадутся автоматически
///
/// Процедурная генерация:
///   SceneManager.LoadScene("GeneratedLocation_" + seed);
///   // GameBootstrap автоматически развернет все системы
///
/// ============================================================================
/// ПОРЯДОК ИНИЦИАЛИЗАЦИИ
/// ============================================================================
/// 1. EnsureSystemsExist() - создание отсутствующих систем
/// 2. RegisterServices() - регистрация в ServiceLocator
/// 3. ServiceLocator.SetInitialized() - все системы готовы к работе
///
/// После инициализации используйте ServiceLocator.Get<T>() вместо FindObjectOfType
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

        // Создаем недостающие системы если их нет в сцене
        EnsureSystemsExist();

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
    /// Создание недостающих систем если их нет в сцене
    /// ARCHITECTURE: Автоматическое развертывание всех необходимых систем
    /// Это позволяет процедурно генерируемым локациям работать без ручной настройки
    /// </summary>
    void EnsureSystemsExist()
    {
        // ========================================================================
        // CORE SYSTEMS - критические системы, нужные в каждой сцене
        // ========================================================================

        // GridManager - система сетки
        if (FindObjectOfType<GridManager>() == null)
        {
            GameObject gridObj = new GameObject("GridManager");
            gridObj.AddComponent<GridManager>();
            Debug.Log("[GameBootstrap] ✓ Created GridManager");
        }

        // SelectionManager - система выделения объектов
        if (FindObjectOfType<SelectionManager>() == null)
        {
            GameObject selectionObj = new GameObject("SelectionManager");
            selectionObj.AddComponent<SelectionManager>();
            Debug.Log("[GameBootstrap] ✓ Created SelectionManager");
        }

        // CombatSystem - боевая система (BaseManager)
        if (FindObjectOfType<CombatSystem>() == null)
        {
            GameObject combatSystemObj = new GameObject("CombatSystem");
            combatSystemObj.AddComponent<CombatSystem>();
            Debug.Log("[GameBootstrap] ✓ Created CombatSystem");
        }

        // ConstructionManager - строительство (BaseManager)
        if (FindObjectOfType<ConstructionManager>() == null)
        {
            GameObject constructionManagerObj = new GameObject("ConstructionManager");
            constructionManagerObj.AddComponent<ConstructionManager>();
            Debug.Log("[GameBootstrap] ✓ Created ConstructionManager");
        }

        // MiningManager - добыча ресурсов (BaseManager)
        if (FindObjectOfType<MiningManager>() == null)
        {
            GameObject miningObj = new GameObject("MiningManager");
            miningObj.AddComponent<MiningManager>();
            Debug.Log("[GameBootstrap] ✓ Created MiningManager");
        }

        // EnemyTargetingSystem - система целеуказания
        if (FindObjectOfType<EnemyTargetingSystem>() == null)
        {
            GameObject enemyTargetingObj = new GameObject("EnemyTargetingSystem");
            enemyTargetingObj.AddComponent<EnemyTargetingSystem>();
            Debug.Log("[GameBootstrap] ✓ Created EnemyTargetingSystem");
        }

        // ========================================================================
        // BUILDING SYSTEMS - системы строительства
        // ========================================================================

        // ShipBuildingSystem - строительство корабля
        if (FindObjectOfType<ShipBuildingSystem>() == null)
        {
            GameObject buildingObj = new GameObject("ShipBuildingSystem");
            buildingObj.AddComponent<ShipBuildingSystem>();
            Debug.Log("[GameBootstrap] ✓ Created ShipBuildingSystem");
        }

        // ========================================================================
        // UI SYSTEMS - пользовательский интерфейс
        // ========================================================================

        // ResourcePanelUI - панель ресурсов
        if (FindObjectOfType<ResourcePanelUI>() == null)
        {
            GameObject resourcePanelObj = new GameObject("ResourcePanelUI");
            resourcePanelObj.AddComponent<ResourcePanelUI>();
            Debug.Log("[GameBootstrap] ✓ Created ResourcePanelUI");
        }

        // InventoryUI - интерфейс инвентаря
        if (FindObjectOfType<InventoryUI>() == null)
        {
            GameObject inventoryUIObj = new GameObject("InventoryUI");
            inventoryUIObj.AddComponent<InventoryUI>();
            Debug.Log("[GameBootstrap] ✓ Created InventoryUI");
        }

        // ВАЖНО: BuildMenuManager и ItemIconManager ДОЛЖНЫ быть в сцене с настроенными Inspector ссылками!
        // Они НЕ создаются автоматически, так как требуют ссылки на UI элементы

        // EventSystem - обработчик UI событий (required for all UI)
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("[GameBootstrap] ✓ Created EventSystem");
        }

        // ========================================================================
        // CAMERA & INPUT - камера и управление
        // ========================================================================

        // CameraController - управление камерой
        if (FindObjectOfType<CameraController>() == null)
        {
            GameObject cameraObj = new GameObject("CameraController");
            cameraObj.AddComponent<CameraController>();
            Debug.Log("[GameBootstrap] ✓ Created CameraController");
        }

        // ========================================================================
        // GAME MANAGEMENT - управление игрой
        // ========================================================================

        // GamePauseManager - система паузы (обычно singleton, может уже существовать)
        if (FindObjectOfType<GamePauseManager>() == null)
        {
            GameObject pauseObj = new GameObject("GamePauseManager");
            pauseObj.AddComponent<GamePauseManager>();
            Debug.Log("[GameBootstrap] ✓ Created GamePauseManager");
        }

        Debug.Log("[GameBootstrap] All required systems ensured to exist");
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

        // Enemy Targeting система
        EnemyTargetingSystem enemyTargetingSystem = FindObjectOfType<EnemyTargetingSystem>();
        if (enemyTargetingSystem != null)
        {
            ServiceLocator.Register<EnemyTargetingSystem>(enemyTargetingSystem);
        }
        // Не выводим предупреждение - EnemyTargetingSystem может отсутствовать

        // Inventory UI система
        InventoryUI inventoryUI = FindObjectOfType<InventoryUI>();
        if (inventoryUI != null)
        {
            ServiceLocator.Register<InventoryUI>(inventoryUI);
        }
        // Не выводим предупреждение - InventoryUI может отсутствовать

        // BuildMenu система - ТРЕБУЕТ Inspector настройки, должна быть в сцене
        BuildMenuManager buildMenuManager = FindObjectOfType<BuildMenuManager>();
        if (buildMenuManager != null)
        {
            ServiceLocator.Register<BuildMenuManager>(buildMenuManager);
        }

        // ItemIcon система - ТРЕБУЕТ Inspector настройки, должна быть в сцене
        ItemIconManager itemIconManager = FindObjectOfType<ItemIconManager>();
        if (itemIconManager != null)
        {
            ServiceLocator.Register<ItemIconManager>(itemIconManager);
        }

        Debug.Log($"[GameBootstrap] Registered {ServiceLocator.ServiceCount} services");
    }

    void OnDestroy()
    {
        // Очищаем ServiceLocator при уничтожении
        Debug.Log("[GameBootstrap] Cleaning up ServiceLocator...");
        ServiceLocator.Clear();
    }
}
