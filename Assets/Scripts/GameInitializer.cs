using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    [Header("Auto Initialize")]
    public bool autoInitializeBootstrap = true;
    public bool autoInitializeUI = true;
    public bool autoInitializeEventSystem = true;
    public bool autoInitializeResolution = true;
    public bool autoInitializeCharacterIcons = true;
    public bool autoInitializeEnemyTargeting = true;
    public bool autoInitializeInventory = true;
    public bool autoInitializePauseSystem = true;
    public bool autoInitializeSelectionInfoDisplay = true;
    public bool autoInitializeObjectSelectDisplay = true;
    public bool autoInitializeItemFactory = true;

    void Awake()
    {
        if (autoInitializeItemFactory)
        {
            EnsureItemFactory();
        }

        if (autoInitializeBootstrap)
        {
            EnsureBootstrapManager();
        }

        if (autoInitializePauseSystem)
        {
            EnsurePauseSystem();
        }
    }

    void Start()
    {
        if (autoInitializeResolution)
        {
            EnsureResolutionManager();
        }

        // РћРўРљР›Р®Р§Р•РќРћ: РќРµ РёСЃРїРѕР»СЊР·СѓРµРј РґРёРЅР°РјРёС‡РµСЃРєРё РіРµРЅРµСЂРёСЂСѓРµРјС‹Р№ UI
        // if (autoInitializeUI)
        // {
        //     EnsureGameUI();
        // }

        if (autoInitializeEventSystem)
        {
            EnsureEventSystem();
        }

        if (autoInitializeCharacterIcons)
        {
            EnsureCanvasCharacterIconsManager();
        }

        if (autoInitializeEnemyTargeting)
        {
            EnsureEnemyTargetingSystem();
            EnsureTargetingInstructions();
        }

        if (autoInitializeInventory)
        {
            EnsureInventoryManager();
        }

        if (autoInitializeSelectionInfoDisplay)
        {
            EnsureEnemySelectDisplay();
        }

        if (autoInitializeObjectSelectDisplay)
        {
            EnsureObjectSelectDisplay();
        }

        // РћРўРљР›Р®Р§Р•РќРћ: Р’РµСЃСЊ РґРёРЅР°РјРёС‡РµСЃРєРёР№ UI РЅРµ РёСЃРїРѕР»СЊР·СѓРµС‚СЃСЏ
        // // РЎРѕР·РґР°РµРј РїСЂРѕСЃС‚РѕР№ РґРµР±Р°Рі РґРёСЃРїР»РµР№
        // GameObject simpleDebugGO = new GameObject("SimpleDebugDisplay");
        // simpleDebugGO.AddComponent<SimpleDebugDisplay>();

        // // РЎРѕР·РґР°РµРј РґРµР±Р°Рі РјРѕРЅРёС‚РѕСЂ
        // GameObject debugMonitorGO = new GameObject("DebugSystemMonitor");
        // debugMonitorGO.AddComponent<DebugSystemMonitor>();

        // // РЎРѕР·РґР°РµРј РёРЅСЃС‚СЂСѓРєС†РёРё РїРѕ РѕС‚Р»Р°РґРєРµ
        // GameObject debugInstructionsGO = new GameObject("DebugInstructions");
        // debugInstructionsGO.AddComponent<DebugInstructions>();

        // // РЈРґР°Р»СЏРµРј РєРЅРѕРїРєРё Center
        // GameObject removerGO = new GameObject("RemoveCenterButtons");
        // removerGO.AddComponent<RemoveCenterButtons>();

        // Р”РѕР±Р°РІР»СЏРµРј С‚РµСЃС‚РѕРІС‹Р№ СЃРїР°РІРЅРµСЂ РїРµСЂСЃРѕРЅР°Р¶РµР№
        GameObject spawnerGO = new GameObject("CharacterSpawnerTest");
        spawnerGO.AddComponent<CharacterSpawnerTest>();

        // Р”РѕР±Р°РІР»СЏРµРј С‚РµСЃС‚РѕРІС‹Р№ СЃРїР°РІРЅРµСЂ РІСЂР°РіРѕРІ
        GameObject enemySpawnerGO = new GameObject("EnemySpawnerTest");
        enemySpawnerGO.AddComponent<EnemySpawnerTest>();

        // // Р”РѕР±Р°РІР»СЏРµРј СЃРёСЃС‚РµРјСѓ РѕР±РЅРѕРІР»РµРЅРёСЏ РїРµСЂСЃРѕРЅР°Р¶РµР№
        // GameObject refreshGO = new GameObject("CharacterRefreshTest");
        // refreshGO.AddComponent<CharacterRefreshTest>();

        // // Р”РѕР±Р°РІР»СЏРµРј РѕС‚Р»Р°РґС‡РёРє СЃС‚СЂСѓРєС‚СѓСЂС‹ SKM_Character
        // GameObject debuggerGO = new GameObject("SKMCharacterDebugger");
        // debuggerGO.AddComponent<SKMCharacterDebugger>();

        // РћРўРљР›Р®Р§Р•РќРћ: РќРµ РёСЃРїРѕР»СЊР·СѓРµРј РґРёРЅР°РјРёС‡РµСЃРєРёР№ UI
        // // Р”РѕР±Р°РІР»СЏРµРј UI РґР»СЏ С‚РµСЃС‚РёСЂРѕРІР°РЅРёСЏ HP
        // EnsureHPTestUI();

        // // Р’Р Р•РњР•РќРќРћ: РЎРєСЂС‹РІР°РµРј РїР°РЅРµР»СЊ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
        // GameObject hideActionAreaGO = new GameObject("HideActionArea");
        // hideActionAreaGO.AddComponent<HideActionArea>();
    }

    /// <summary>
    /// РЈР±РµРґРёС‚СЊСЃСЏ С‡С‚Рѕ BootstrapManager СЃСѓС‰РµСЃС‚РІСѓРµС‚ РІ СЃС†РµРЅРµ
    /// </summary>
    void EnsureBootstrapManager()
    {
        BootstrapManager bootstrapManager = FindObjectOfType<BootstrapManager>();
        if (bootstrapManager == null)
        {
            GameObject bootstrapManagerGO = new GameObject("BootstrapManager");
            bootstrapManager = bootstrapManagerGO.AddComponent<BootstrapManager>();
        }
    }

    /// <summary>
    /// РЈР±РµРґРёС‚СЊСЃСЏ С‡С‚Рѕ СЃРёСЃС‚РµРјР° РїР°СѓР·С‹ РёРЅРёС†РёР°Р»РёР·РёСЂРѕРІР°РЅР°
    /// </summary>
    void EnsurePauseSystem()
    {
        // РРЅРёС†РёР°Р»РёР·РёСЂСѓРµРј GamePauseManager
        if (GamePauseManager.Instance != null)
        {
            // Initialized
        }

        // РРЅРёС†РёР°Р»РёР·РёСЂСѓРµРј PauseMenuManager
        if (PauseMenuManager.Instance != null)
        {
            // Initialized
        }
    }

    /// <summary>
    /// РЈР±РµРґРёС‚СЊСЃСЏ С‡С‚Рѕ ResolutionManager СЃСѓС‰РµСЃС‚РІСѓРµС‚ РІ СЃС†РµРЅРµ
    /// </summary>
    void EnsureResolutionManager()
    {
        ResolutionManager resolutionManager = FindObjectOfType<ResolutionManager>();
        if (resolutionManager == null)
        {
            GameObject resolutionManagerGO = new GameObject("ResolutionManager");
            resolutionManager = resolutionManagerGO.AddComponent<ResolutionManager>();
        }
    }

    /// <summary>
    /// РЈР±РµРґРёС‚СЊСЃСЏ С‡С‚Рѕ EventSystem СЃСѓС‰РµСЃС‚РІСѓРµС‚ РґР»СЏ UI
    /// </summary>
    void EnsureEventSystem()
    {
        UnityEngine.EventSystems.EventSystem eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystem = eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    /// <summary>
    /// РЈР±РµРґРёС‚СЊСЃСЏ С‡С‚Рѕ CanvasCharacterIconsManager СЃСѓС‰РµСЃС‚РІСѓРµС‚ РІ СЃС†РµРЅРµ
    /// </summary>
    void EnsureCanvasCharacterIconsManager()
    {
        CanvasCharacterIconsManager iconManager = FindObjectOfType<CanvasCharacterIconsManager>();
        if (iconManager == null)
        {
            GameObject iconManagerGO = new GameObject("CanvasCharacterIconsManager");
            iconManager = iconManagerGO.AddComponent<CanvasCharacterIconsManager>();

            // Р—Р°РіСЂСѓР¶Р°РµРј РїСЂРµС„Р°Р± CharacterPortrait
            GameObject prefab = Resources.Load<GameObject>("Prefabs/UI/CharacterPortrait");
            if (prefab == null)
            {
                // РџСЂРѕР±СѓРµРј Р±РµР· РїР°РїРєРё Resources
                prefab = UnityEngine.Object.FindObjectOfType<GameObject>();
            }
            iconManager.characterPortraitPrefab = prefab;
        }
    }

    /// <summary>
    /// РЈР±РµРґРёС‚СЊСЃСЏ С‡С‚Рѕ HPTestUI СЃСѓС‰РµСЃС‚РІСѓРµС‚ РІ СЃС†РµРЅРµ
    /// </summary>
    void EnsureHPTestUI()
    {
        HPTestUI hpTestUI = FindObjectOfType<HPTestUI>();
        if (hpTestUI == null)
        {
            GameObject hpTestUIGO = new GameObject("HPTestUI");
            hpTestUI = hpTestUIGO.AddComponent<HPTestUI>();
        }
    }

    /// <summary>
    /// РЈР±РµРґРёС‚СЊСЃСЏ С‡С‚Рѕ EnemyTargetingSystem СЃСѓС‰РµСЃС‚РІСѓРµС‚ РІ СЃС†РµРЅРµ
    /// </summary>
    void EnsureEnemyTargetingSystem()
    {
        EnemyTargetingSystem enemyTargetingSystem = FindObjectOfType<EnemyTargetingSystem>();
        if (enemyTargetingSystem == null)
        {
            GameObject enemyTargetingGO = new GameObject("EnemyTargetingSystem");
            enemyTargetingSystem = enemyTargetingGO.AddComponent<EnemyTargetingSystem>();
        }
    }

    /// <summary>
    /// РЈР±РµРґРёС‚СЊСЃСЏ С‡С‚Рѕ TargetingInstructions СЃСѓС‰РµСЃС‚РІСѓРµС‚ РІ СЃС†РµРЅРµ
    /// </summary>
    void EnsureTargetingInstructions()
    {
        TargetingInstructions targetingInstructions = FindObjectOfType<TargetingInstructions>();
        if (targetingInstructions == null)
        {
            GameObject instructionsGO = new GameObject("TargetingInstructions");
            targetingInstructions = instructionsGO.AddComponent<TargetingInstructions>();
        }
    }

    /// <summary>
    /// РЈР±РµРґРёС‚СЊСЃСЏ С‡С‚Рѕ InventoryManager СЃСѓС‰РµСЃС‚РІСѓРµС‚ РІ СЃС†РµРЅРµ
    /// </summary>
    void EnsureInventoryManager()
    {
        InventoryManager inventoryManager = FindObjectOfType<InventoryManager>();
        if (inventoryManager == null)
        {
            GameObject inventoryManagerGO = new GameObject("InventoryManager");
            inventoryManager = inventoryManagerGO.AddComponent<InventoryManager>();
        }
    }

    /// <summary>
    /// РЈР±РµРґРёС‚СЊСЃСЏ С‡С‚Рѕ EnemySelectDisplay СЃСѓС‰РµСЃС‚РІСѓРµС‚ РІ СЃС†РµРЅРµ Рё РїСЂР°РІРёР»СЊРЅРѕ РЅР°СЃС‚СЂРѕРµРЅ
    /// </summary>
    void EnsureEnemySelectDisplay()
    {
        // РС‰РµРј EnemySelectDisplay РІ СЃС†РµРЅРµ
        EnemySelectDisplay enemySelectDisplay = FindObjectOfType<EnemySelectDisplay>();

        if (enemySelectDisplay == null)
        {
            // РС‰РµРј EnemySelect РЅР° Canvas_MainUI
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>(true);
            GameObject enemySelectPanel = null;

            foreach (GameObject obj in allObjects)
            {
                if (obj.name == "EnemySelect")
                {
                    enemySelectPanel = obj;
                    break;
                }
            }

            if (enemySelectPanel != null)
            {
                // Р”РѕР±Р°РІР»СЏРµРј РєРѕРјРїРѕРЅРµРЅС‚ EnemySelectDisplay РµСЃР»Рё РµРіРѕ РЅРµС‚
                enemySelectDisplay = enemySelectPanel.GetComponent<EnemySelectDisplay>();
                if (enemySelectDisplay == null)
                {
                    // РђРєС‚РёРІРёСЂСѓРµРј РїР°РЅРµР»СЊ РїРµСЂРµРґ РґРѕР±Р°РІР»РµРЅРёРµРј РєРѕРјРїРѕРЅРµРЅС‚Р° С‡С‚РѕР±С‹ РІС‹Р·РІР°Р»СЃСЏ Awake()
                    bool wasActive = enemySelectPanel.activeSelf;
                    if (!wasActive)
                    {
                        enemySelectPanel.SetActive(true);
                    }

                    enemySelectDisplay = enemySelectPanel.AddComponent<EnemySelectDisplay>();

                    // Р”РµР°РєС‚РёРІРёСЂСѓРµРј РїР°РЅРµР»СЊ РѕР±СЂР°С‚РЅРѕ РµСЃР»Рё Р±С‹Р»Р° РЅРµР°РєС‚РёРІРЅРѕР№
                    if (!wasActive)
                    {
                        enemySelectPanel.SetActive(false);
                    }
                }
                else
                {
                    // РђРєС‚РёРІРёСЂСѓРµРј РїР°РЅРµР»СЊ РЅР° РјРѕРјРµРЅС‚ РёРЅРёС†РёР°Р»РёР·Р°С†РёРё С‡С‚РѕР±С‹ РІС‹Р·РІР°Р»СЃСЏ Awake() Рё Start()
                    if (!enemySelectPanel.activeSelf)
                    {
                        enemySelectPanel.SetActive(true);

                        // Р”РµР°РєС‚РёРІРёСЂСѓРµРј РїР°РЅРµР»СЊ РѕР±СЂР°С‚РЅРѕ РїРѕСЃР»Рµ РЅРµР±РѕР»СЊС€РѕР№ Р·Р°РґРµСЂР¶РєРё
                        // РСЃРїРѕР»СЊР·СѓРµРј РєРѕСЂСѓС‚РёРЅСѓ РЅР° GameInitializer (Р°РєС‚РёРІРЅРѕРј РѕР±СЉРµРєС‚Рµ)
                        StartCoroutine(DeactivatePanelAfterDelay(enemySelectPanel, 0.1f));
                    }
                }
            }
        }
    }

    /// <summary>
    /// РЈР±РµРґРёС‚СЊСЃСЏ С‡С‚Рѕ ObjectSelectDisplay СЃСѓС‰РµСЃС‚РІСѓРµС‚ РІ СЃС†РµРЅРµ Рё РїСЂР°РІРёР»СЊРЅРѕ РЅР°СЃС‚СЂРѕРµРЅ
    /// </summary>
    void EnsureObjectSelectDisplay()
    {
        // РС‰РµРј ObjectSelectDisplay РІ СЃС†РµРЅРµ
        ObjectSelectDisplay objectSelectDisplay = FindObjectOfType<ObjectSelectDisplay>();

        if (objectSelectDisplay == null)
        {
            // РС‰РµРј ObjectSelect РЅР° Canvas_MainUI
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>(true);
            GameObject objectSelectPanel = null;

            foreach (GameObject obj in allObjects)
            {
                if (obj.name == "ObjectSelect")
                {
                    objectSelectPanel = obj;
                    break;
                }
            }

            if (objectSelectPanel != null)
            {
                // Р”РѕР±Р°РІР»СЏРµРј РєРѕРјРїРѕРЅРµРЅС‚ ObjectSelectDisplay РµСЃР»Рё РµРіРѕ РЅРµС‚
                objectSelectDisplay = objectSelectPanel.GetComponent<ObjectSelectDisplay>();
                if (objectSelectDisplay == null)
                {
                    // РђРєС‚РёРІРёСЂСѓРµРј РїР°РЅРµР»СЊ РїРµСЂРµРґ РґРѕР±Р°РІР»РµРЅРёРµРј РєРѕРјРїРѕРЅРµРЅС‚Р° С‡С‚РѕР±С‹ РІС‹Р·РІР°Р»СЃСЏ Awake(), Start() Рё OnEnable()
                    bool wasActive = objectSelectPanel.activeSelf;
                    if (!wasActive)
                    {
                        objectSelectPanel.SetActive(true);
                    }

                    objectSelectDisplay = objectSelectPanel.AddComponent<ObjectSelectDisplay>();
                    // РљРѕРјРїРѕРЅРµРЅС‚ СЃР°Рј СЃРєСЂРѕРµС‚ РїР°РЅРµР»СЊ РІ Start()
                }
                else
                {
                    // РђРєС‚РёРІРёСЂСѓРµРј РїР°РЅРµР»СЊ РЅР° РјРѕРјРµРЅС‚ РёРЅРёС†РёР°Р»РёР·Р°С†РёРё С‡С‚РѕР±С‹ РІС‹Р·РІР°Р»СЃСЏ Start() Рё OnEnable()
                    if (!objectSelectPanel.activeSelf)
                    {
                        objectSelectPanel.SetActive(true);
                        // РљРѕРјРїРѕРЅРµРЅС‚ СЃР°Рј СЃРєСЂРѕРµС‚ РїР°РЅРµР»СЊ РІ Start()
                    }
                }
            }
        }
    }

    /// <summary>
    /// РљРѕСЂСѓС‚РёРЅР° РґР»СЏ РґРµР°РєС‚РёРІР°С†РёРё РїР°РЅРµР»Рё РїРѕСЃР»Рµ Р·Р°РґРµСЂР¶РєРё
    /// </summary>
    System.Collections.IEnumerator DeactivatePanelAfterDelay(GameObject panel, float delay)
    {
        yield return new UnityEngine.WaitForSeconds(delay);
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    /// <summary>
    /// РЈР±РµРґРёС‚СЊСЃСЏ С‡С‚Рѕ ItemFactory РёРЅРёС†РёР°Р»РёР·РёСЂРѕРІР°РЅ СЃ ItemDatabase
    /// </summary>
    void EnsureItemFactory()
    {
        // Р—Р°РіСЂСѓР¶Р°РµРј ItemDatabase РёР· Resources
        ItemDatabase itemDatabase = Resources.Load<ItemDatabase>("ItemDatabase");

        if (itemDatabase == null)
        {
            return;
        }

        // РРЅРёС†РёР°Р»РёР·РёСЂСѓРµРј ItemFactory
        ItemFactory.Initialize(itemDatabase);
    }
}
