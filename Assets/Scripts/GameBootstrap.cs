using UnityEngine;

/// <summary>
/// GameBootstrap - С‚РѕС‡РєР° РІС…РѕРґР° РІ РёРіСЂСѓ, РёРЅРёС†РёР°Р»РёР·РёСЂСѓРµС‚ РІСЃРµ СЃРёСЃС‚РµРјС‹ Рё СЂРµРіРёСЃС‚СЂРёСЂСѓРµС‚ РёС… РІ ServiceLocator
///
/// ============================================================================
/// Р’РђР–РќРћ: Script Execution Order
/// ============================================================================
/// Р­С‚РѕС‚ СЃРєСЂРёРїС‚ РґРѕР»Р¶РµРЅ Р±С‹С‚СЊ РїРµСЂРІС‹Рј РІ Script Execution Order
/// (Edit -> Project Settings -> Script Execution Order)
/// РЈСЃС‚Р°РЅРѕРІРёС‚Рµ GameBootstrap РЅР° -1000 С‡С‚РѕР±С‹ РѕРЅ РІС‹РїРѕР»РЅСЏР»СЃСЏ СЂР°РЅСЊС€Рµ РІСЃРµС… РѕСЃС‚Р°Р»СЊРЅС‹С…
///
/// ============================================================================
/// РђР РҐРРўР•РљРўРЈР Рђ: РђРІС‚РѕРјР°С‚РёС‡РµСЃРєРѕРµ СЂР°Р·РІРµСЂС‚С‹РІР°РЅРёРµ СЃРёСЃС‚РµРј (12 СЃРёСЃС‚РµРј)
/// ============================================================================
/// GameBootstrap Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРё СЃРѕР·РґР°РµС‚ Р±РѕР»СЊС€РёРЅСЃС‚РІРѕ РЅРµРѕР±С…РѕРґРёРјС‹С… СЃРёСЃС‚РµРј РµСЃР»Рё РёС… РЅРµС‚ РІ СЃС†РµРЅРµ.
/// Р­С‚Рѕ РєСЂРёС‚РёС‡РµСЃРєРё РІР°Р¶РЅРѕ РґР»СЏ РїСЂРѕС†РµРґСѓСЂРЅРѕР№ РіРµРЅРµСЂР°С†РёРё Р»РѕРєР°С†РёР№!
///
/// Р’РђР–РќРћ: BuildMenuManager Рё ItemIconManager РќР• СЃРѕР·РґР°СЋС‚СЃСЏ Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРё,
/// С‚Р°Рє РєР°Рє С‚СЂРµР±СѓСЋС‚ Inspector РЅР°СЃС‚СЂРѕР№РєРё СЃСЃС‹Р»РѕРє РЅР° UI СЌР»РµРјРµРЅС‚С‹!
///
/// РђР’РўРћРњРђРўРР§Р•РЎРљР РЎРћР—Р”РђР®РўРЎРЇ:
/// - Core: GridManager, LocationManager, SelectionManager, CombatSystem, ConstructionManager, MiningManager, EnemyTargetingSystem
/// - Building: ShipBuildingSystem
/// - UI: ResourcePanelUI, InventoryUI, EventSystem
/// - Camera: CameraController
/// - Lighting: Directional Light (РµСЃР»Рё РЅРµС‚ РІ СЃС†РµРЅРµ)
/// - Game: GamePauseManager
///
/// РџСЂРµРёРјСѓС‰РµСЃС‚РІР°:
/// вњ“ РџСЂРѕС†РµРґСѓСЂРЅС‹Рµ Р»РѕРєР°С†РёРё СЂР°Р±РѕС‚Р°СЋС‚ СЃСЂР°Р·Сѓ Р±РµР· СЂСѓС‡РЅРѕР№ РЅР°СЃС‚СЂРѕР№РєРё
/// вњ“ Р•РґРёРЅР°СЏ С‚РѕС‡РєР° РєРѕРЅС„РёРіСѓСЂР°С†РёРё РІСЃРµС… СЃРёСЃС‚РµРј
/// вњ“ Р“Р°СЂР°РЅС‚РёСЂРѕРІР°РЅРЅР°СЏ РєРѕРЅСЃРёСЃС‚РµРЅС‚РЅРѕСЃС‚СЊ РјРµР¶РґСѓ СЃС†РµРЅР°РјРё
/// вњ“ Р“РёР±РєРѕСЃС‚СЊ - РјРѕР¶РЅРѕ РїРµСЂРµРѕРїСЂРµРґРµР»РёС‚СЊ СЃРёСЃС‚РµРјСѓ РІ РєРѕРЅРєСЂРµС‚РЅРѕР№ СЃС†РµРЅРµ
///
/// ============================================================================
/// РРЎРџРћР›Р¬Р—РћР’РђРќРР• Р”Р›РЇ РџР РћР¦Р•Р”РЈР РќР«РҐ Р›РћРљРђР¦РР™
/// ============================================================================
/// 1. РЎРѕР·РґР°Р№С‚Рµ РЅРѕРІСѓСЋ СЃС†РµРЅСѓ (РёР»Рё РіРµРЅРµСЂРёСЂСѓР№С‚Рµ РїСЂРѕС†РµРґСѓСЂРЅРѕ)
/// 2. Р”РѕР±Р°РІСЊС‚Рµ GameObject СЃ GameBootstrap РєРѕРјРїРѕРЅРµРЅС‚РѕРј
/// 3. Р’РЎРЃ! Р’СЃРµ СЃРёСЃС‚РµРјС‹ СЃРѕР·РґР°РґСѓС‚СЃСЏ Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРё
///
/// РџСЂРѕС†РµРґСѓСЂРЅР°СЏ РіРµРЅРµСЂР°С†РёСЏ:
///   SceneManager.LoadScene("GeneratedLocation_" + seed);
///   // GameBootstrap Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРё СЂР°Р·РІРµСЂРЅРµС‚ РІСЃРµ СЃРёСЃС‚РµРјС‹
///
/// ============================================================================
/// РџРћР РЇР”РћРљ РРќРР¦РРђР›РР—РђР¦РР
/// ============================================================================
/// 1. EnsureSystemsExist() - СЃРѕР·РґР°РЅРёРµ РѕС‚СЃСѓС‚СЃС‚РІСѓСЋС‰РёС… СЃРёСЃС‚РµРј
/// 2. RegisterServices() - СЂРµРіРёСЃС‚СЂР°С†РёСЏ РІ ServiceLocator
/// 3. ServiceLocator.SetInitialized() - РІСЃРµ СЃРёСЃС‚РµРјС‹ РіРѕС‚РѕРІС‹ Рє СЂР°Р±РѕС‚Рµ
///
/// РџРѕСЃР»Рµ РёРЅРёС†РёР°Р»РёР·Р°С†РёРё РёСЃРїРѕР»СЊР·СѓР№С‚Рµ ServiceLocator.Get<T>() РІРјРµСЃС‚Рѕ FindObjectOfType
/// </summary>
[DefaultExecutionOrder(-1000)]
public class GameBootstrap : MonoBehaviour
{
    [Header("Debug")]
    [Tooltip("Р’С‹РІРѕРґРёС‚СЊ СЃРїРёСЃРѕРє Р·Р°СЂРµРіРёСЃС‚СЂРёСЂРѕРІР°РЅРЅС‹С… СЃРµСЂРІРёСЃРѕРІ РІ РєРѕРЅСЃРѕР»СЊ")]
    public bool debugPrintServices = true;

    void Awake()
    {

        // РћС‡РёС‰Р°РµРј ServiceLocator РЅР° СЃР»СѓС‡Р°Р№ РїРµСЂРµР·Р°РіСЂСѓР·РєРё СЃС†РµРЅС‹
        ServiceLocator.Clear();

        // РЎРѕР·РґР°РµРј РЅРµРґРѕСЃС‚Р°СЋС‰РёРµ СЃРёСЃС‚РµРјС‹ РµСЃР»Рё РёС… РЅРµС‚ РІ СЃС†РµРЅРµ
        EnsureSystemsExist();

        // Р РµРіРёСЃС‚СЂРёСЂСѓРµРј РІСЃРµ СЃРёСЃС‚РµРјС‹
        RegisterServices();

        // РЈСЃС‚Р°РЅР°РІР»РёРІР°РµРј С„Р»Р°Рі РёРЅРёС†РёР°Р»РёР·Р°С†РёРё
        ServiceLocator.SetInitialized();

        // Р’С‹РІРѕРґРёРј СЃРїРёСЃРѕРє СЃРµСЂРІРёСЃРѕРІ РґР»СЏ РѕС‚Р»Р°РґРєРё
        if (debugPrintServices)
        {
            ServiceLocator.DebugPrintServices();
        }

    }

    /// <summary>
    /// РЎРѕР·РґР°РЅРёРµ РЅРµРґРѕСЃС‚Р°СЋС‰РёС… СЃРёСЃС‚РµРј РµСЃР»Рё РёС… РЅРµС‚ РІ СЃС†РµРЅРµ
    /// ARCHITECTURE: РђРІС‚РѕРјР°С‚РёС‡РµСЃРєРѕРµ СЂР°Р·РІРµСЂС‚С‹РІР°РЅРёРµ РІСЃРµС… РЅРµРѕР±С…РѕРґРёРјС‹С… СЃРёСЃС‚РµРј
    /// Р­С‚Рѕ РїРѕР·РІРѕР»СЏРµС‚ РїСЂРѕС†РµРґСѓСЂРЅРѕ РіРµРЅРµСЂРёСЂСѓРµРјС‹Рј Р»РѕРєР°С†РёСЏРј СЂР°Р±РѕС‚Р°С‚СЊ Р±РµР· СЂСѓС‡РЅРѕР№ РЅР°СЃС‚СЂРѕР№РєРё
    /// </summary>
    void EnsureSystemsExist()
    {
        // ========================================================================
        // CORE SYSTEMS - РєСЂРёС‚РёС‡РµСЃРєРёРµ СЃРёСЃС‚РµРјС‹, РЅСѓР¶РЅС‹Рµ РІ РєР°Р¶РґРѕР№ СЃС†РµРЅРµ
        // ========================================================================

        // GridManager - СЃРёСЃС‚РµРјР° СЃРµС‚РєРё
        if (FindObjectOfType<GridManager>() == null)
        {
            GameObject gridObj = new GameObject("GridManager");
            gridObj.AddComponent<GridManager>();
        }

        // LocationManager - РіРµРЅРµСЂР°С†РёСЏ Р»РѕРєР°С†РёРё (Р°СЃС‚РµСЂРѕРёРґС‹, СЃС‚Р°РЅС†РёРё, РѕР±Р»РѕРјРєРё)
        if (FindObjectOfType<LocationManager>() == null)
        {
            GameObject locationObj = new GameObject("LocationManager");
            locationObj.AddComponent<LocationManager>();
        }

        // SelectionManager - СЃРёСЃС‚РµРјР° РІС‹РґРµР»РµРЅРёСЏ РѕР±СЉРµРєС‚РѕРІ
        if (FindObjectOfType<SelectionManager>() == null)
        {
            GameObject selectionObj = new GameObject("SelectionManager");
            selectionObj.AddComponent<SelectionManager>();
        }

        // CombatSystem - Р±РѕРµРІР°СЏ СЃРёСЃС‚РµРјР° (BaseManager)
        if (FindObjectOfType<CombatSystem>() == null)
        {
            GameObject combatSystemObj = new GameObject("CombatSystem");
            combatSystemObj.AddComponent<CombatSystem>();
        }

        // ConstructionManager - СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІРѕ (BaseManager)
        if (FindObjectOfType<ConstructionManager>() == null)
        {
            GameObject constructionManagerObj = new GameObject("ConstructionManager");
            constructionManagerObj.AddComponent<ConstructionManager>();
        }

        // MiningManager - РґРѕР±С‹С‡Р° СЂРµСЃСѓСЂСЃРѕРІ (BaseManager)
        if (FindObjectOfType<MiningManager>() == null)
        {
            GameObject miningObj = new GameObject("MiningManager");
            miningObj.AddComponent<MiningManager>();
        }

        // EnemyTargetingSystem - СЃРёСЃС‚РµРјР° С†РµР»РµСѓРєР°Р·Р°РЅРёСЏ
        if (FindObjectOfType<EnemyTargetingSystem>() == null)
        {
            GameObject enemyTargetingObj = new GameObject("EnemyTargetingSystem");
            enemyTargetingObj.AddComponent<EnemyTargetingSystem>();
        }

        // ========================================================================
        // BUILDING SYSTEMS - СЃРёСЃС‚РµРјС‹ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
        // ========================================================================

        // ShipBuildingSystem - СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІРѕ РєРѕСЂР°Р±Р»СЏ
        if (FindObjectOfType<ShipBuildingSystem>() == null)
        {
            GameObject buildingObj = new GameObject("ShipBuildingSystem");
            buildingObj.AddComponent<ShipBuildingSystem>();
        }

        // ========================================================================
        // UI SYSTEMS - РїРѕР»СЊР·РѕРІР°С‚РµР»СЊСЃРєРёР№ РёРЅС‚РµСЂС„РµР№СЃ
        // ========================================================================

        // ResourcePanelUI - РїР°РЅРµР»СЊ СЂРµСЃСѓСЂСЃРѕРІ
        if (FindObjectOfType<ResourcePanelUI>() == null)
        {
            GameObject resourcePanelObj = new GameObject("ResourcePanelUI");
            resourcePanelObj.AddComponent<ResourcePanelUI>();
        }

        // InventoryUI - РёРЅС‚РµСЂС„РµР№СЃ РёРЅРІРµРЅС‚Р°СЂСЏ
        if (FindObjectOfType<InventoryUI>() == null)
        {
            GameObject inventoryUIObj = new GameObject("InventoryUI");
            inventoryUIObj.AddComponent<InventoryUI>();
        }

        // Р’РђР–РќРћ: BuildMenuManager Рё ItemIconManager Р”РћР›Р–РќР« Р±С‹С‚СЊ РІ СЃС†РµРЅРµ СЃ РЅР°СЃС‚СЂРѕРµРЅРЅС‹РјРё Inspector СЃСЃС‹Р»РєР°РјРё!
        // РћРЅРё РќР• СЃРѕР·РґР°СЋС‚СЃСЏ Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРё, С‚Р°Рє РєР°Рє С‚СЂРµР±СѓСЋС‚ СЃСЃС‹Р»РєРё РЅР° UI СЌР»РµРјРµРЅС‚С‹

        // EventSystem - РѕР±СЂР°Р±РѕС‚С‡РёРє UI СЃРѕР±С‹С‚РёР№ (required for all UI)
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // ========================================================================
        // CAMERA & INPUT - РєР°РјРµСЂР° Рё СѓРїСЂР°РІР»РµРЅРёРµ
        // ========================================================================

        // CameraController - СѓРїСЂР°РІР»РµРЅРёРµ РєР°РјРµСЂРѕР№
        if (FindObjectOfType<CameraController>() == null)
        {
            GameObject cameraObj = new GameObject("CameraController");
            cameraObj.AddComponent<CameraController>();
        }

        // ========================================================================
        // LIGHTING - РѕСЃРІРµС‰РµРЅРёРµ СЃС†РµРЅС‹
        // ========================================================================

        // Directional Light - РѕСЃРЅРѕРІРЅРѕР№ РёСЃС‚РѕС‡РЅРёРє СЃРІРµС‚Р° (РєСЂРёС‚РёС‡РЅРѕ РґР»СЏ URP/Lit РјР°С‚РµСЂРёР°Р»РѕРІ)
        Light[] lights = FindObjectsOfType<Light>();
        bool hasDirectionalLight = false;
        foreach (Light light in lights)
        {
            if (light.type == LightType.Directional)
            {
                hasDirectionalLight = true;
                break;
            }
        }

        if (!hasDirectionalLight)
        {
            GameObject lightObj = new GameObject("Directional Light");
            Light dirLight = lightObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = Color.white;
            dirLight.intensity = 1.0f;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        // ========================================================================
        // GAME MANAGEMENT - СѓРїСЂР°РІР»РµРЅРёРµ РёРіСЂРѕР№
        // ========================================================================

        // GamePauseManager - СЃРёСЃС‚РµРјР° РїР°СѓР·С‹ (РѕР±С‹С‡РЅРѕ singleton, РјРѕР¶РµС‚ СѓР¶Рµ СЃСѓС‰РµСЃС‚РІРѕРІР°С‚СЊ)
        if (FindObjectOfType<GamePauseManager>() == null)
        {
            GameObject pauseObj = new GameObject("GamePauseManager");
            pauseObj.AddComponent<GamePauseManager>();
        }

    }

    /// <summary>
    /// Р РµРіРёСЃС‚СЂР°С†РёСЏ РІСЃРµС… СЃРµСЂРІРёСЃРѕРІ/РјРµРЅРµРґР¶РµСЂРѕРІ РІ ServiceLocator
    /// </summary>
    void RegisterServices()
    {
        // Р’РђР–РќРћ: Р РµРіРёСЃС‚СЂРёСЂСѓРµРј РєРѕРЅРєСЂРµС‚РЅС‹Рµ С‚РёРїС‹ РєР»Р°СЃСЃРѕРІ, Р° РќР• РёРЅС‚РµСЂС„РµР№СЃС‹
        // РРЅС‚РµСЂС„РµР№СЃС‹ Р±СѓРґСѓС‚ РґРѕР±Р°РІР»РµРЅС‹ РІ Р¤Р°Р·Рµ 2.2

        // === CORE SYSTEMS ===

        // Grid СЃРёСЃС‚РµРјР°
        GridManager gridManager = FindObjectOfType<GridManager>();
        if (gridManager != null)
        {
            ServiceLocator.Register<GridManager>(gridManager);
        }
        else
        {
        }

        // Location СЃРёСЃС‚РµРјР°
        LocationManager locationManager = FindObjectOfType<LocationManager>();
        if (locationManager != null)
        {
            ServiceLocator.Register<LocationManager>(locationManager);
        }
        else
        {
        }

        // Selection СЃРёСЃС‚РµРјР°
        SelectionManager selectionManager = FindObjectOfType<SelectionManager>();
        if (selectionManager != null)
        {
            ServiceLocator.Register<SelectionManager>(selectionManager);
        }
        else
        {
        }

        // Building СЃРёСЃС‚РµРјР°
        ShipBuildingSystem buildingSystem = FindObjectOfType<ShipBuildingSystem>();
        if (buildingSystem != null)
        {
            ServiceLocator.Register<ShipBuildingSystem>(buildingSystem);
        }
        else
        {
        }

        // Resource СЃРёСЃС‚РµРјР°
        ResourcePanelUI resourcePanel = FindObjectOfType<ResourcePanelUI>();
        if (resourcePanel != null)
        {
            ServiceLocator.Register<ResourcePanelUI>(resourcePanel);
        }
        else
        {
        }

        // Camera СЃРёСЃС‚РµРјР°
        CameraController cameraController = FindObjectOfType<CameraController>();
        if (cameraController != null)
        {
            ServiceLocator.Register<CameraController>(cameraController);
        }
        else
        {
        }

        // Pause СЃРёСЃС‚РµРјР°
        GamePauseManager pauseManager = FindObjectOfType<GamePauseManager>();
        if (pauseManager != null)
        {
            ServiceLocator.Register<GamePauseManager>(pauseManager);
        }
        // РќРµ РІС‹РІРѕРґРёРј РїСЂРµРґСѓРїСЂРµР¶РґРµРЅРёРµ - GamePauseManager РјРѕР¶РµС‚ Р±С‹С‚СЊ singleton

        // Mining СЃРёСЃС‚РµРјР°
        MiningManager miningManager = FindObjectOfType<MiningManager>();
        if (miningManager != null)
        {
            ServiceLocator.Register<MiningManager>(miningManager);
        }
        // РќРµ РІС‹РІРѕРґРёРј РїСЂРµРґСѓРїСЂРµР¶РґРµРЅРёРµ - MiningManager СЃРѕР·РґР°РµС‚СЃСЏ РґРёРЅР°РјРёС‡РµСЃРєРё

        // Combat СЃРёСЃС‚РµРјР°
        CombatSystem combatSystem = FindObjectOfType<CombatSystem>();
        if (combatSystem != null)
        {
            ServiceLocator.Register<CombatSystem>(combatSystem);
        }
        // РќРµ РІС‹РІРѕРґРёРј РїСЂРµРґСѓРїСЂРµР¶РґРµРЅРёРµ - CombatSystem РјРѕР¶РµС‚ СЃРѕР·РґР°РІР°С‚СЊСЃСЏ РґРёРЅР°РјРёС‡РµСЃРєРё

        // Construction СЃРёСЃС‚РµРјР°
        ConstructionManager constructionManager = FindObjectOfType<ConstructionManager>();
        if (constructionManager != null)
        {
            ServiceLocator.Register<ConstructionManager>(constructionManager);
        }
        // РќРµ РІС‹РІРѕРґРёРј РїСЂРµРґСѓРїСЂРµР¶РґРµРЅРёРµ - ConstructionManager РјРѕР¶РµС‚ РѕС‚СЃСѓС‚СЃС‚РІРѕРІР°С‚СЊ

        // Enemy Targeting СЃРёСЃС‚РµРјР°
        EnemyTargetingSystem enemyTargetingSystem = FindObjectOfType<EnemyTargetingSystem>();
        if (enemyTargetingSystem != null)
        {
            ServiceLocator.Register<EnemyTargetingSystem>(enemyTargetingSystem);
        }
        // РќРµ РІС‹РІРѕРґРёРј РїСЂРµРґСѓРїСЂРµР¶РґРµРЅРёРµ - EnemyTargetingSystem РјРѕР¶РµС‚ РѕС‚СЃСѓС‚СЃС‚РІРѕРІР°С‚СЊ

        // Inventory UI СЃРёСЃС‚РµРјР°
        InventoryUI inventoryUI = FindObjectOfType<InventoryUI>();
        if (inventoryUI != null)
        {
            ServiceLocator.Register<InventoryUI>(inventoryUI);
        }
        // РќРµ РІС‹РІРѕРґРёРј РїСЂРµРґСѓРїСЂРµР¶РґРµРЅРёРµ - InventoryUI РјРѕР¶РµС‚ РѕС‚СЃСѓС‚СЃС‚РІРѕРІР°С‚СЊ

        // BuildMenu СЃРёСЃС‚РµРјР° - РўР Р•Р‘РЈР•Рў Inspector РЅР°СЃС‚СЂРѕР№РєРё, РґРѕР»Р¶РЅР° Р±С‹С‚СЊ РІ СЃС†РµРЅРµ
        BuildMenuManager buildMenuManager = FindObjectOfType<BuildMenuManager>();
        if (buildMenuManager != null)
        {
            ServiceLocator.Register<BuildMenuManager>(buildMenuManager);
        }

        // ItemIcon СЃРёСЃС‚РµРјР° - РўР Р•Р‘РЈР•Рў Inspector РЅР°СЃС‚СЂРѕР№РєРё, РґРѕР»Р¶РЅР° Р±С‹С‚СЊ РІ СЃС†РµРЅРµ
        ItemIconManager itemIconManager = FindObjectOfType<ItemIconManager>();
        if (itemIconManager != null)
        {
            ServiceLocator.Register<ItemIconManager>(itemIconManager);
        }

    }

    void OnDestroy()
    {
        // РћС‡РёС‰Р°РµРј ServiceLocator РїСЂРё СѓРЅРёС‡С‚РѕР¶РµРЅРёРё
        ServiceLocator.Clear();
    }
}
