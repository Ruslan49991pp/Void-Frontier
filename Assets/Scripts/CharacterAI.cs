using System.Collections;
using UnityEngine;

/// <summary>
/// РЎРёСЃС‚РµРјР° РР РґР»СЏ РїРµСЂСЃРѕРЅР°Р¶РµР№ СЃ СЂР°Р·Р»РёС‡РЅС‹РјРё СЃРѕСЃС‚РѕСЏРЅРёСЏРјРё
/// </summary>
public class CharacterAI : MonoBehaviour
{
    [Header("AI Settings")]
    public float idleTimeout = 3f; // Р’СЂРµРјСЏ РґРѕ РїРµСЂРµС…РѕРґР° РІ Idle СЃРѕСЃС‚РѕСЏРЅРёРµ
    public float wanderRadius = 2.5f; // Р Р°РґРёСѓСЃ Р±Р»СѓР¶РґР°РЅРёСЏ (5x5 РєР»РµС‚РѕРє = 2.5 СЂР°РґРёСѓСЃР°)
    public float wanderInterval = 2f; // РРЅС‚РµСЂРІР°Р» РјРµР¶РґСѓ С‚РѕС‡РєР°РјРё Р±Р»СѓР¶РґР°РЅРёСЏ
    public float pauseDuration = 2f; // Р’СЂРµРјСЏ РѕСЃС‚Р°РЅРѕРІРєРё РІ С‚РѕС‡РєРµ

    [Header("Enemy AI")]
    public float enemyDetectionRange = 10f; // Р Р°РґРёСѓСЃ РѕР±РЅР°СЂСѓР¶РµРЅРёСЏ РІСЂР°РіРѕРІ (10 РєР»РµС‚РѕРє)
    public float enemyDetectionInterval = 1f; // РРЅС‚РµСЂРІР°Р» СЃРєР°РЅРёСЂРѕРІР°РЅРёСЏ (1 СЃРµРєСѓРЅРґР°)

    [Header("Debug")]
    public bool debugMode = false; // РћС‚РєР»СЋС‡Р°РµРј debug Р»РѕРіРё
    public bool debugCombat = false; // РћС‚РєР»СЋС‡Р°РµРј РґРµР±Р°Рі Р±РѕРµРІРѕР№ СЃРёСЃС‚РµРјС‹
    public bool debugStateTransitions = true; // РћС‚СЃР»РµР¶РёРІР°РЅРёРµ РїРµСЂРµС…РѕРґРѕРІ СЃРѕСЃС‚РѕСЏРЅРёР№

    // РЎРѕСЃС‚РѕСЏРЅРёСЏ РР
    public enum AIState
    {
        PlayerControlled,  // РџРѕРґ СѓРїСЂР°РІР»РµРЅРёРµРј РёРіСЂРѕРєР°
        Move,             // Р”РІРёР¶РµРЅРёРµ Рє С†РµР»Рё
        Idle,             // РЎРІРѕР±РѕРґРЅРѕРµ Р±Р»СѓР¶РґР°РЅРёРµ
        Working,          // Р Р°Р±РѕС‚Р° (СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІРѕ Рё С‚.Рґ.)
        Mining,           // Р”РѕР±С‹С‡Р° СЂРµСЃСѓСЂСЃРѕРІ РёР· Р°СЃС‚РµСЂРѕРёРґРѕРІ
        Chasing,          // РџСЂРµСЃР»РµРґРѕРІР°РЅРёРµ РІСЂР°РіР° (РґРІРёР¶РµРЅРёРµ Рє С†РµР»Рё РґР»СЏ Р°С‚Р°РєРё)
        Attacking         // РђС‚Р°РєР° РІСЂР°РіР° (РІ СЂР°РґРёСѓСЃРµ Р°С‚Р°РєРё)
    }

    // РљРѕРјРїРѕРЅРµРЅС‚С‹
    private Character character;
    private CharacterMovement movement;
    private SelectionManager selectionManager;
    private GridManager gridManager;
    private CombatSystem combatSystem;
    private ConstructionManager constructionManager;
    private MiningManager miningManager;

    // РџРµСЂРµРјРµРЅРЅС‹Рµ СЃРѕСЃС‚РѕСЏРЅРёСЏ
    private AIState currentState = AIState.Idle; // ARCHITECTURE: Р'СЃРµ РїРµСЂСЃРѕРЅР°Р¶Рё РЅР°С‡РёРЅР°СЋС‚ РІ Idle
    private float lastActionTime; // ARCHITECTURE: Р'СЂРµРјСЏ РїРѕСЃР»РµРґРЅРµРіРѕ Р"Р•Р™РЎРўР'РРЇ (РЅРµ РІС‹РґРµР»РµРЅРёСЏ!)
    private Vector3 idleBasePosition; // Р‘Р°Р·РѕРІР°СЏ РїРѕР·РёС†РёСЏ РґР»СЏ Р±Р»СѓР¶РґР°РЅРёСЏ
    private Coroutine idleCoroutine;
    private bool isWandering = false;
    private bool playerInitiatedMovement = false; // Р¤Р»Р°Рі РґРІРёР¶РµРЅРёСЏ, РёРЅРёС†РёРёСЂРѕРІР°РЅРЅРѕРіРѕ РёРіСЂРѕРєРѕРј
    private bool movingToJob = false; // FIX: Р¤Р»Р°Рі РґРІРёР¶РµРЅРёСЏ Рє СЂР°Р±РѕС‚Рµ/РґРѕР±С‹С‡Рµ (Р±Р»РѕРєРёСЂСѓРµС‚ РєРѕРЅС‚СЂР°С‚Р°РєСѓ)
    private bool isPlayerControlled = false; // ARCHITECTURE: Р'С‚РѕСЂРёС‡РЅРѕРµ СЃРѕСЃС‚РѕСЏРЅРёРµ - РІС‹РґРµР»РµРЅРёРµ РїРµСЂСЃРѕРЅР°Р¶Р° (РЅРµ РїСЂРµСЂС‹РІР°РµС‚ РґСЂСѓРіРёРµ СЃРѕСЃС‚РѕСЏРЅРёСЏ)
    private float lastConstructionCheckTime = 0f; // Р'СЂРµРјСЏ РїРѕСЃР»РµРґРЅРµР№ РїСЂРѕРІРµСЂРєРё СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
    private const float constructionCheckInterval = 2f; // РРЅС‚РµСЂРІР°Р» РїСЂРѕРІРµСЂРєРё СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР° (СЃРµРєСѓРЅРґС‹)
    private float lastEnemyDetectionTime = 0f; // Р’СЂРµРјСЏ РїРѕСЃР»РµРґРЅРµРіРѕ СЃРєР°РЅРёСЂРѕРІР°РЅРёСЏ РІСЂР°РіРѕРІ
    private float combatStartTime = 0f; // Р’СЂРµРјСЏ РЅР°С‡Р°Р»Р° Р±РѕСЏ РґР»СЏ Р·Р°РґРµСЂР¶РєРё РїСЂРѕРІРµСЂРєРё
    private const float combatCheckDelay = 0.5f; // Р—Р°РґРµСЂР¶РєР° РїРµСЂРµРґ РїСЂРѕРІРµСЂРєРѕР№ РѕРєРѕРЅС‡Р°РЅРёСЏ Р±РѕСЏ (СЃРµРєСѓРЅРґС‹)

    // Р’СЂРµРјСЏ РїРѕСЃР»РµРґРЅРµР№ РєРѕРјР°РЅРґС‹ РґРІРёР¶РµРЅРёСЏ РѕС‚ РёРіСЂРѕРєР° (РґР»СЏ РѕС‚Р»Р°РґРєРё Рё Р»РѕРіРёСЂРѕРІР°РЅРёСЏ)
    private float playerMoveCommandTime = 0f;

    void Awake()
    {
        character = GetComponent<Character>();
        movement = GetComponent<CharacterMovement>();

        // Р”РѕР±Р°РІР»СЏРµРј CharacterMovement РµСЃР»Рё РµРіРѕ РЅРµС‚
        if (movement == null)
        {
            movement = gameObject.AddComponent<CharacterMovement>();
        }

        // PERFORMANCE: РСЃРїРѕР»СЊР·СѓРµРј ServiceLocator РІРјРµСЃС‚Рѕ FindObjectOfType (O(1) РІРјРµСЃС‚Рѕ O(n))
        selectionManager = ServiceLocator.Get<SelectionManager>();
        gridManager = ServiceLocator.Get<GridManager>();
        combatSystem = ServiceLocator.Get<CombatSystem>();
        constructionManager = ConstructionManager.Instance;
    }

    void Start()
    {
        lastActionTime = Time.time; // ARCHITECTURE: РќР°С‡РёРЅР°РµРј РѕС‚СЃС‡РµС‚ РґР»СЏ Idle СЃСЂР°Р·Сѓ
        idleBasePosition = transform.position;

        // РС‰РµРј MiningManager РІ Start() С‡С‚РѕР±С‹ РіР°СЂР°РЅС‚РёСЂРѕРІР°С‚СЊ С‡С‚Рѕ РѕРЅ СѓСЃРїРµР» РёРЅРёС†РёР°Р»РёР·РёСЂРѕРІР°С‚СЊСЃСЏ
        // PERFORMANCE: РСЃРїРѕР»СЊР·СѓРµРј ServiceLocator РІРјРµСЃС‚Рѕ FindObjectOfType (O(1) РІРјРµСЃС‚Рѕ O(n))
        miningManager = ServiceLocator.Get<MiningManager>();

        // РџРѕРґРїРёСЃС‹РІР°РµРјСЃСЏ РЅР° СЃРѕР±С‹С‚РёСЏ РІС‹РґРµР»РµРЅРёСЏ
        if (selectionManager != null)
        {
            selectionManager.OnSelectionChanged += OnSelectionChanged;
        }

        // Р’Р РђР“Р РЅР°С‡РёРЅР°СЋС‚ РІ СЃРѕСЃС‚РѕСЏРЅРёРё Idle (Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРёР№ РїРѕРёСЃРє С†РµР»РµР№)
        if (character != null && character.IsEnemyCharacter())
        {
            SwitchToState(AIState.Idle);
        }

        // Р’РђР–РќРћ: РџСЂРё СЃС‚Р°СЂС‚Рµ РёРіСЂС‹ РїСЂРѕРІРµСЂСЏРµРј РЅР°Р»РёС‡РёРµ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
        // Р­С‚Рѕ РЅСѓР¶РЅРѕ РґР»СЏ СЃР»СѓС‡Р°СЏ РєРѕРіРґР° РїРµСЂСЃРѕРЅР°Р¶Рё РЅР°С‡РёРЅР°СЋС‚ РІ СЃРѕСЃС‚РѕСЏРЅРёРё Idle
        StartCoroutine(CheckConstructionOnStartup());
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРєР° СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР° РїСЂРё Р·Р°РїСѓСЃРєРµ (С‡РµСЂРµР· РЅРµР±РѕР»СЊС€СѓСЋ Р·Р°РґРµСЂР¶РєСѓ)
    /// </summary>
    System.Collections.IEnumerator CheckConstructionOnStartup()
    {
        // Р–РґРµРј 0.5 СЃРµРєСѓРЅРґС‹ С‡С‚РѕР±С‹ РІСЃРµ РєРѕРјРїРѕРЅРµРЅС‚С‹ РёРЅРёС†РёР°Р»РёР·РёСЂРѕРІР°Р»РёСЃСЊ
        yield return new WaitForSeconds(0.5f);

        // Р•СЃР»Рё РїРµСЂСЃРѕРЅР°Р¶ РёРіСЂРѕРєР° Рё РЅРµ Р·Р°РЅСЏС‚ - РїС‹С‚Р°РµРјСЃСЏ РЅР°Р·РЅР°С‡РёС‚СЊ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІРѕ
        if (character != null && character.IsPlayerCharacter() && constructionManager != null)
        {
            constructionManager.TryAssignConstructionToIdleCharacter(character);
        }
    }

    void Update()
    {
        // РќРµ РІС‹РїРѕР»РЅСЏРµРј РР РґР»СЏ РјРµСЂС‚РІС‹С… РїРµСЂСЃРѕРЅР°Р¶РµР№
        if (character != null && character.IsDead())
        {
            return;
        }

        UpdateAIState();
        HandleCurrentState();
    }

    /// <summary>
    /// РћР±РЅРѕРІР»РµРЅРёРµ СЃРѕСЃС‚РѕСЏРЅРёСЏ РР РІ Р·Р°РІРёСЃРёРјРѕСЃС‚Рё РѕС‚ РІС‹РґРµР»РµРЅРёСЏ
    /// </summary>
    void UpdateAIState()
    {
        // Р’Р РђР“Р: Р•СЃР»Рё РІ Р±РѕСЋ, РЅРѕ С†РµР»СЊ РјРµСЂС‚РІР° РёР»Рё РІРЅРµ СЂР°РґРёСѓСЃР° - РІРѕР·РІСЂР°С‰Р°РµРјСЃСЏ РІ Idle
        if (character != null && character.IsEnemyCharacter())
        {
            if (currentState == AIState.Chasing || currentState == AIState.Attacking)
            {
                // Р’РђР–РќРћ: Р”Р°РµРј РІСЂРµРјСЏ CombatSystem РѕР±СЂР°Р±РѕС‚Р°С‚СЊ РЅР°Р·РЅР°С‡РµРЅРёРµ С†РµР»Рё РїРµСЂРµРґ РїСЂРѕРІРµСЂРєРѕР№
                float timeSinceCombatStart = Time.time - combatStartTime;

                if (timeSinceCombatStart >= combatCheckDelay)
                {
                    // РџСЂРѕРІРµСЂСЏРµРј, СѓС‡Р°СЃС‚РІСѓРµС‚ Р»Рё РїРµСЂСЃРѕРЅР°Р¶ РІ Р±РѕСЋ
                    if (combatSystem == null || !combatSystem.IsInCombat(character))
                    {
                        SwitchToState(AIState.Idle);
                        return;
                    }
                }
            }
        }

        // FIX: Р'Р»РѕРєРёСЂРѕРІРєР° РїРµСЂРµРјРµС‰РµРЅР° РЅРёР¶Рµ, РїРѕСЃР»Рµ РѕР±СЂР°Р±РѕС‚РєРё РєРѕРЅС‚СЂР°С‚Р°РєРё
        // Р"РґРµСЃСЊ РѕСЃС‚Р°РІР»РµРЅРѕ РїСѓСЃС‚РѕРµ РјРµСЃС‚Рѕ СЃРїРµС†РёР°Р»СЊРЅРѕ

        // Р’Р РђР“Р РІ Р±РѕСЋ РќР• РјРѕРіСѓС‚ Р±С‹С‚СЊ РїСЂРµСЂРІР°РЅС‹ (Сѓ РЅРёС… РЅРµС‚ PlayerControlled СЃРѕСЃС‚РѕСЏРЅРёСЏ)
        if (character != null && character.IsEnemyCharacter() &&
            (currentState == AIState.Chasing || currentState == AIState.Attacking))
        {
            return;
        }

        bool isSelected = character.IsSelected();

        // РЎРћР®Р—РќРРљР: Р•СЃР»Рё РІ Р±РѕСЋ, РЅРѕ С†РµР»СЊ РјРµСЂС‚РІР° РёР»Рё РІРЅРµ СЂР°РґРёСѓСЃР° - РІРѕР·РІСЂР°С‰Р°РµРјСЃСЏ РІ РїСЂР°РІРёР»СЊРЅРѕРµ СЃРѕСЃС‚РѕСЏРЅРёРµ
        if (character != null && character.IsPlayerCharacter())
        {
            if (currentState == AIState.Chasing || currentState == AIState.Attacking)
            {
                // Р’РђР–РќРћ: Р”Р°РµРј РІСЂРµРјСЏ CombatSystem РѕР±СЂР°Р±РѕС‚Р°С‚СЊ РЅР°Р·РЅР°С‡РµРЅРёРµ С†РµР»Рё РїРµСЂРµРґ РїСЂРѕРІРµСЂРєРѕР№
                float timeSinceCombatStart = Time.time - combatStartTime;

                if (timeSinceCombatStart >= combatCheckDelay)
                {
                    // РџСЂРѕРІРµСЂСЏРµРј, СѓС‡Р°СЃС‚РІСѓРµС‚ Р»Рё РїРµСЂСЃРѕРЅР°Р¶ РІ Р±РѕСЋ
                    if (combatSystem == null || !combatSystem.IsInCombat(character))
                    {
                        // ARCHITECTURE: Р'РѕР·РІСЂР°С‰Р°РµРјСЃСЏ РІ Idle (РІС‹РґРµР»РµРЅРёРµ - РІС‚РѕСЂРёС‡РЅРѕРµ СЃРѕСЃС‚РѕСЏРЅРёРµ, РѕРЅРѕ СѓР¶Рµ СѓСЃС‚Р°РЅРѕРІР»РµРЅРѕ)
                        SwitchToState(AIState.Idle);
                        return;
                    }
                }
            }
        }
        bool isMoving = movement != null && movement.IsMoving();
        float timeSinceLastAction = Time.time - lastActionTime; // ARCHITECTURE: Время с последнего ДЕЙСТВИЯ

        // Р”РµР±Р°Рі Р»РѕРіРё РѕС‚РєР»СЋС‡РµРЅС‹
        // if (debugMode && Time.frameCount % 120 == 0) // Р›РѕРі РєР°Р¶РґС‹Рµ 2 СЃРµРєСѓРЅРґС‹ (РїСЂРё 60 FPS)
        // {
        //              $"Selected={isSelected}, Moving={isMoving}, " +
        //              $"TimeSinceSelection={timeSinceSelection:F1}s, " +
        //              $"CurrentState={currentState}, " +
        //              $"IsWandering={isWandering}");
        // }

        // ARCHITECTURE: Обновляем флаг выделения (независимо от остальной логики)
        // ВАЖНО: НЕ обновляем lastActionTime! Выделение - не действие!
        if (isSelected)
        {
            isPlayerControlled = true;
        }
        else
        {
            isPlayerControlled = false;
        }

        // ARCHITECTURE: Обрабатываем команды и состояния НЕЗАВИСИМО от выделения
        // PlayerControlled - это флаг, а не действие, поэтому не блокирует логику ниже

        // FIX: Process player commands ALWAYS, regardless of isMoving or selection!
        if (playerInitiatedMovement)
        {
            if (currentState != AIState.Move)
            {
                // Player command interrupts Working/Mining/any state
                SwitchToState(AIState.Move);
            }
            playerInitiatedMovement = false;
            lastActionTime = Time.time; // ARCHITECTURE: Команда движения - это действие!
        }
        else if (isMoving)
        {
            // Auto movement: switch to Move only if not in special states
            if (currentState != AIState.Idle &&
                currentState != AIState.Move &&
                currentState != AIState.Working &&
                currentState != AIState.Mining &&
                currentState != AIState.Chasing &&
                currentState != AIState.Attacking)
            {
                SwitchToState(AIState.Move);
            }
        }
        // ARCHITECTURE: Idle появляется при бездействии НЕЗАВИСИМО от выделения
        // PlayerControlled (выделение) не является действием и не сбрасывает таймер!
        else if (timeSinceLastAction >= idleTimeout &&
                 currentState != AIState.Idle &&
                 currentState != AIState.Working &&
                 currentState != AIState.Mining &&
                 currentState != AIState.Chasing &&
                 currentState != AIState.Attacking)
        {
            // Персонаж не выполняет действий 3 секунды - переходим в Idle
            // Выделение (isPlayerControlled) не мешает переходу в Idle
            SwitchToState(AIState.Idle);
        }
    }

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚РєР° С‚РµРєСѓС‰РµРіРѕ СЃРѕСЃС‚РѕСЏРЅРёСЏ
    /// </summary>
    void HandleCurrentState()
    {
        // ============================================================================
        // COUNTER-ATTACK SYSTEM: РџСЂРѕРІРµСЂСЏРµРј РЅР°Р»РёС‡РёРµ Р°С‚Р°РєСѓСЋС‰РµРіРѕ РґР»СЏ РєРѕРЅС‚СЂР°С‚Р°РєРё
        // ============================================================================
        // Р’РђР–РќРћ: РљРѕРЅС‚СЂР°С‚Р°РєР° СЂР°Р±РѕС‚Р°РµС‚ РќР•РњР•Р”Р›Р•РќРќРћ РµСЃР»Рё РїРµСЂСЃРѕРЅР°Р¶ РќР• РІС‹РїРѕР»РЅСЏРµС‚ Р°РєС‚РёРІРЅСѓСЋ РєРѕРјР°РЅРґСѓ РёРіСЂРѕРєР°

        bool hasAttacker = character != null && character.HasActiveAttacker();

        // РЈРџР РћР©Р•РќРќРђРЇ Р›РћР“РРљРђ: РљРѕРЅС‚СЂР°С‚Р°РєР° РґРѕСЃС‚СѓРїРЅР° С‚РѕР»СЊРєРѕ РµСЃР»Рё РїРµСЂСЃРѕРЅР°Р¶ РќР• Р·Р°РЅСЏС‚
        // РџСЂРѕРІРµСЂСЏРµРј СЃРѕСЃС‚РѕСЏРЅРёРµ AI - СЌС‚Рѕ РЅР°РґРµР¶РЅРµРµ С‡РµРј РїСЂРѕРІРµСЂРєР° РІСЂРµРјРµРЅРё РєРѕРјР°РЅРґС‹
        // Р•СЃР»Рё РїРµСЂСЃРѕРЅР°Р¶ РІ Move/Working/Mining - РѕРЅ РІС‹РїРѕР»РЅСЏРµС‚ РїСЂРёРєР°Р· Рё РќР• РєРѕРЅС‚СЂР°С‚Р°РєСѓРµС‚
        // ARCHITECTURE: РљРћРќРўР -РђРўРђРљРђ РґРѕСЃС‚СѓРїРЅР° РІ: Idle (РїРµСЂСЃРѕРЅР°Р¶ СЃРІРѕР±РѕРґРµРЅ)
        // РљРћРќРўР -РђРўРђРљРђ Р'Р›РћРљРР РЈР•РўРЎРЇ РІ: Move (РІС‹РїРѕР»РЅРµРЅРёРµ РїСЂРёРєР°Р·Р°!), Working, Mining, Chasing, Attacking
        // FIX: Р'Р›РћРљРРўРЖ РєРѕРЅС‚СЂР°С‚Р°РєСѓ РµСЃР»Рё РёРґРµС‚ Рє СЂР°Р±РѕС‚Рµ/РґРѕР±С‹С‡Рµ
        if (movingToJob)
        {
            // РРґРµС‚ Рє Р·Р°РґР°РЅРёСЋ - РЅРµ РїСЂРµСЂС‹РІР°РµРј РїСѓС‚СЊ!
            hasAttacker = false; // Р'Р»РѕРєРёСЂСѓРµРј РєРѕРЅС‚СЂР°С‚Р°РєСѓ
        }

        bool canCounterAttack = currentState != AIState.Chasing &&
                                currentState != AIState.Attacking &&
                                currentState != AIState.Move; // FIX: Working/Mining РјРѕРіСѓС‚ РєРѕРЅС‚СЂР°С‚Р°РєРѕРІР°С‚СЊ!

        if (hasAttacker && canCounterAttack)
        {
            Character attacker = character.GetLastAttacker();

            if (attacker != null && !attacker.IsDead())
            {
                // РџСЂРѕРІРµСЂСЏРµРј С‡С‚Рѕ РїРµСЂСЃРѕРЅР°Р¶ РµС‰Рµ РЅРµ РІ Р±РѕСЋ
                if (combatSystem != null && !combatSystem.IsInCombat(character))
                {
                    // DEBUG: Р›РѕРіРёСЂСѓРµРј РєРѕРЅС‚СЂР°С‚Р°РєСѓ
                    if (debugStateTransitions && character != null)
                    {
                        bool isSelected = character.IsSelected();
                        string selectionStatus = isSelected ? "[SELECTED]" : "[NOT SELECTED]";
                        bool isRecentPlayerCommand = (Time.time - playerMoveCommandTime) < 1f;
                        string warning = isRecentPlayerCommand ? " *** INTERRUPTING PLAYER COMMAND! ***" : "";

                        Debug.Log($"[COUNTER-ATTACK] {selectionStatus} {character.GetFullName()} counter-attacking {attacker.GetFullName()} | State: {currentState}{warning}");
                    }

                    combatSystem.AssignCombatTarget(character, attacker);
                    SwitchToState(AIState.Chasing);

                    // РћС‡РёС‰Р°РµРј РёРЅС„РѕСЂРјР°С†РёСЋ РѕР± Р°С‚Р°РєСѓСЋС‰РµРј - РєРѕРЅС‚СЂР°С‚Р°РєР° РЅР°С‡Р°Р»Р°СЃСЊ
                    character.ClearLastAttacker();
                }
                else
                {
                    // РЈР¶Рµ РІ Р±РѕСЋ - РѕС‡РёС‰Р°РµРј Р°С‚Р°РєСѓСЋС‰РµРіРѕ
                    character.ClearLastAttacker();
                }
            }
            else
            {
                // РђС‚Р°РєСѓСЋС‰РёР№ РјРµСЂС‚РІ РёР»Рё null - РѕС‡РёС‰Р°РµРј
                character.ClearLastAttacker();
            }
        }

        // FIX: Р'Р›РћРљРР РЈР•Рњ РѕСЃС‚Р°Р»СЊРЅСѓСЋ Р»РѕРіРёРєСѓ РґР»СЏ Working/Mining РџРћРЎР›Р• РєРѕРЅС‚СЂР°С‚Р°РєРё
        // Р­С‚Рѕ РїРѕР·РІРѕР»СЏРµС‚ Working/Mining РѕС‚РІРµС‡Р°С‚СЊ РЅР° СѓРіСЂРѕР·С‹, РЅРѕ РЅРµ РїРµСЂРµРєР»СЋС‡Р°С‚СЊСЃСЏ Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРё
        if (currentState == AIState.Working || currentState == AIState.Mining)
        {
            return; // РљРѕРЅС‚СЂР°С‚Р°РєР° СѓР¶Рµ РѕР±СЂР°Р±РѕС‚Р°РЅР°, Р±Р»РѕРєРёСЂСѓРµРј С‚РѕР»СЊРєРѕ РѕСЃС‚Р°Р»СЊРЅСѓСЋ Р»РѕРіРёРєСѓ
        }

        // ============================================================================
        // REGULAR STATE HANDLING
        // ============================================================================
        switch (currentState)
        {
            case AIState.PlayerControlled:
                // ARCHITECTURE: DEPRECATED - PlayerControlled is now a secondary state (flag)
                // This case should never be reached, but kept for backward compatibility
                // Characters that are selected now use Idle state with isPlayerControlled=true flag
                break;

            case AIState.Move:
                // РќРёС‡РµРіРѕ РЅРµ РґРµР»Р°РµРј - РґРІРёР¶РµРЅРёРµ РѕР±СЂР°Р±Р°С‚С‹РІР°РµС‚СЃСЏ CharacterMovement
                break;

            case AIState.Idle:
                // РЎРѕСЃС‚РѕСЏРЅРёРµ РѕР±СЂР°Р±Р°С‚С‹РІР°РµС‚СЃСЏ РєРѕСЂСѓС‚РёРЅРѕР№

                // Р”Р›РЇ РЎРћР®Р—РќРРљРћР’: РџР•Р РРћР”РР§Р•РЎРљР РџР РћР’Р•Р РЇР•Рњ РќРђР›РР§РР• РЎРўР РћРРўР•Р›Р¬РЎРўР’Рђ Р Р’Р РђР“РћР’
                if (character != null && character.IsPlayerCharacter())
                {
                    if (Time.time - lastConstructionCheckTime >= constructionCheckInterval)
                    {
                        lastConstructionCheckTime = Time.time;

                        if (constructionManager != null)
                        {
                            constructionManager.TryAssignConstructionToIdleCharacter(character);
                        }
                    }

                    // РЎРљРђРќРР РЈР•Рњ РћР‘Р›РђРЎРўР¬ РќРђ РќРђР›РР§РР• Р’Р РђР“РћР’ (РїРѕСЃР»Рµ РїСЂРѕРІРµСЂРєРё СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°)
                    // FIX: РќР• СЃРєР°РЅРёСЂСѓРµРј РІСЂР°РіРѕРІ, РµСЃР»Рё РёРґРµРј Рє СЂР°Р±РѕС‚Рµ/РґРѕР±С‹С‡Рµ
                    if (!movingToJob && Time.time - lastEnemyDetectionTime >= enemyDetectionInterval)
                    {
                        lastEnemyDetectionTime = Time.time;
                        ScanForEnemies();
                    }
                }
                // Р”Р›РЇ Р’Р РђР“РћР’: РџР•Р РРћР”РР§Р•РЎРљР РЎРљРђРќРР РЈР•Рњ РћР‘Р›РђРЎРўР¬ РќРђ РќРђР›РР§РР• Р¦Р•Р›Р•Р™
                else if (character != null && character.IsEnemyCharacter())
                {
                    if (Time.time - lastEnemyDetectionTime >= enemyDetectionInterval)
                    {
                        lastEnemyDetectionTime = Time.time;
                        ScanForTargets();
                    }
                }
                break;

            case AIState.Working:
                // FIX: Р Р°Р±РѕС‡РёРµ РґРѕР»Р¶РЅС‹ СЃРєР°РЅРёСЂРѕРІР°С‚СЊ РѕРєСЂСѓР¶РµРЅРёРµ РЅР° РІСЂР°РіРѕРІ!
                if (character != null && character.IsPlayerCharacter())
                {
                    // FIX: РќР• СЃРєР°РЅРёСЂСѓРµРј РІСЂР°РіРѕРІ, РµСЃР»Рё РёРґРµРј Рє СЂР°Р±РѕС‚Рµ/РґРѕР±С‹С‡Рµ
                    if (!movingToJob && Time.time - lastEnemyDetectionTime >= enemyDetectionInterval)
                    {
                        lastEnemyDetectionTime = Time.time;
                        ScanForEnemies();
                    }
                }
                break;

            case AIState.Mining:
                // FIX: Р"РѕР±С‹С‚С‡РёРєРё РґРѕР»Р¶РЅС‹ СЃРєР°РЅРёСЂРѕРІР°С‚СЊ РѕРєСЂСѓР¶РµРЅРёРµ РЅР° РІСЂР°РіРѕРІ!
                if (character != null && character.IsPlayerCharacter())
                {
                    // FIX: РќР• СЃРєР°РЅРёСЂСѓРµРј РІСЂР°РіРѕРІ, РµСЃР»Рё РёРґРµРј Рє СЂР°Р±РѕС‚Рµ/РґРѕР±С‹С‡Рµ
                    if (!movingToJob && Time.time - lastEnemyDetectionTime >= enemyDetectionInterval)
                    {
                        lastEnemyDetectionTime = Time.time;
                        ScanForEnemies();
                    }
                }
                break;

            case AIState.Chasing:
                // РќРёС‡РµРіРѕ РЅРµ РґРµР»Р°РµРј - РїСЂРµСЃР»РµРґРѕРІР°РЅРёРµ РѕР±СЂР°Р±Р°С‚С‹РІР°РµС‚СЃСЏ CombatSystem
                break;

            case AIState.Attacking:
                // РќРёС‡РµРіРѕ РЅРµ РґРµР»Р°РµРј - Р°С‚Р°РєР° РѕР±СЂР°Р±Р°С‚С‹РІР°РµС‚СЃСЏ CombatSystem
                break;
        }
    }

    /// <summary>
    /// РџРµСЂРµРєР»СЋС‡РµРЅРёРµ СЃРѕСЃС‚РѕСЏРЅРёСЏ РР
    /// </summary>
    void SwitchToState(AIState newState)
    {
        if (currentState == newState) return;

        AIState oldState = currentState;

        // DEBUG: Р›РѕРіРёСЂСѓРµРј РїРµСЂРµС…РѕРґ СЃРѕСЃС‚РѕСЏРЅРёСЏ
        if (debugStateTransitions)
        {
            LogStateTransition(oldState, newState);
        }

        // Р'С‹С…РѕРґ РёР· РїСЂРµРґС‹РґСѓС‰РµРіРѕ СЃРѕСЃС‚РѕСЏРЅРёСЏ
        ExitState(currentState);

        // РЎРјРµРЅР° СЃРѕСЃС‚РѕСЏРЅРёСЏ
        currentState = newState;

        // Р'С…РѕРґ РІ РЅРѕРІРѕРµ СЃРѕСЃС‚РѕСЏРЅРёРµ
        EnterState(newState);
    }

    /// <summary>
    /// Р›РѕРіРёСЂРѕРІР°РЅРёРµ РїРµСЂРµС…РѕРґР° СЃРѕСЃС‚РѕСЏРЅРёСЏ СЃ РёРЅС„РѕСЂРјР°С†РёРµР№ Рѕ РІС‹РґРµР»РµРЅРёРё
    /// </summary>
    void LogStateTransition(AIState oldState, AIState newState)
    {
        if (character == null) return;

        // ARCHITECTURE: РџСЂРѕРІРµСЂСЏРµРј СЃС‚Р°С‚СѓСЃ РІС‹РґРµР»РµРЅРёСЏ (С‚РµРїРµСЂСЊ СЌС‚Рѕ С„Р»Р°Рі, Р° РЅРµ СЃРѕСЃС‚РѕСЏРЅРёРµ)
        bool isSelected = character.IsSelected();
        string selectionStatus = isPlayerControlled ? "[SELECTED]" : "[NOT SELECTED]";

        // РћРїСЂРµРґРµР»СЏРµРј С‚РёРї РїРµСЂСЃРѕРЅР°Р¶Р°
        string characterType = character.IsPlayerCharacter() ? "ALLY" : "ENEMY";

        // Р¤РѕСЂРјРёСЂСѓРµРј СЃРѕРѕР±С‰РµРЅРёРµ
        string message = $"[STATE TRANSITION] {selectionStatus} {characterType} '{character.GetFullName()}': {oldState} -> {newState}";

        // Р"РѕР±Р°РІР»СЏРµРј РєРѕРЅС‚РµРєСЃС‚ РїРµСЂРµС…РѕРґР°
        string context = GetTransitionContext(oldState, newState);
        if (!string.IsNullOrEmpty(context))
        {
            message += $" | Context: {context}";
        }

        Debug.Log(message);
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РєРѕРЅС‚РµРєСЃС‚ РїРµСЂРµС…РѕРґР° РґР»СЏ Р»СѓС‡С€РµРіРѕ РїРѕРЅРёРјР°РЅРёСЏ РїСЂРёС‡РёРЅС‹
    /// </summary>
    string GetTransitionContext(AIState oldState, AIState newState)
    {
        // РџРµСЂРµС…РѕРґ РІ Chasing/Attacking - С€РёСЂРѕРєРѕ Р»РѕРіРёСЂСѓРµРј
        if (newState == AIState.Chasing)
        {
            if (combatSystem != null && character != null && combatSystem.IsInCombat(character))
            {
                Character target = combatSystem.GetCombatTarget(character);
                if (target != null)
                {
                    bool hasAttacker = character.HasActiveAttacker();
                    bool isPlayerCommand = (Time.time - playerMoveCommandTime) < 0.5f;

                    string reason = hasAttacker ? "Counter-attack triggered" : "Enemy detected in range";
                    if (isPlayerCommand)
                    {
                        reason += " [WARNING: OVERRIDING PLAYER COMMAND!]";
                    }

                    return $"Target: {target.GetFullName()} | Reason: {reason}";
                }
            }
        }

        // РџРµСЂРµС…РѕРґ РІ Move
        if (newState == AIState.Move)
        {
            bool isPlayerInitiated = playerInitiatedMovement || (Time.time - playerMoveCommandTime) < 0.5f;
            return isPlayerInitiated ? "Player command" : "AI-initiated movement";
        }

        // ARCHITECTURE: PlayerControlled is deprecated as a state
        // Selection is now tracked via isPlayerControlled flag
        if (newState == AIState.PlayerControlled)
        {
            return $"[DEPRECATED STATE] Character selected | Previous: {oldState}";
        }

        // РџРµСЂРµС…РѕРґ РІ Working/Mining
        if (newState == AIState.Working)
        {
            return "Construction task assigned";
        }

        if (newState == AIState.Mining)
        {
            return "Mining task assigned";
        }

        // РџРµСЂРµС…РѕРґ РІ Idle
        if (newState == AIState.Idle)
        {
            float timeSinceLastAction = Time.time - lastActionTime;
            return $"Idle timeout reached ({timeSinceLastAction:F1}s since last action)";
        }

        return "";
    }

    /// <summary>
    /// Р’С‹С…РѕРґ РёР· СЃРѕСЃС‚РѕСЏРЅРёСЏ
    /// </summary>
    void ExitState(AIState state)
    {
        switch (state)
        {
            case AIState.Move:
                // РџСЂРё РІС‹С…РѕРґРµ РёР· СЃРѕСЃС‚РѕСЏРЅРёСЏ Move РЅРёС‡РµРіРѕ РѕСЃРѕР±РµРЅРЅРѕРіРѕ РЅРµ РґРµР»Р°РµРј
                break;

            case AIState.Idle:
                // РћСЃС‚Р°РЅР°РІР»РёРІР°РµРј Р±Р»СѓР¶РґР°РЅРёРµ
                if (idleCoroutine != null)
                {
                    StopCoroutine(idleCoroutine);
                    idleCoroutine = null;
                }
                isWandering = false;

                // РћСЃС‚Р°РЅР°РІР»РёРІР°РµРј РґРІРёР¶РµРЅРёРµ
                if (movement != null)
                {
                    movement.StopMovement();
                }
                break;

            case AIState.Working:
                // РџСЂРё РІС‹С…РѕРґРµ РёР· СЃРѕСЃС‚РѕСЏРЅРёСЏ Working РЅРёС‡РµРіРѕ РѕСЃРѕР±РµРЅРЅРѕРіРѕ РЅРµ РґРµР»Р°РµРј
                // РџРµСЂСЃРѕРЅР°Р¶ Р·Р°РІРµСЂС€РёР» СЂР°Р±РѕС‚Сѓ
                break;

            case AIState.Mining:
                // РџСЂРё РІС‹С…РѕРґРµ РёР· СЃРѕСЃС‚РѕСЏРЅРёСЏ Mining РѕСЃС‚Р°РЅР°РІР»РёРІР°РµРј РґРІРёР¶РµРЅРёРµ
                if (movement != null && movement.IsMoving())
                {
                    movement.StopMovement();
                }
                break;

            case AIState.Chasing:
                // РџСЂРё РІС‹С…РѕРґРµ РёР· СЃРѕСЃС‚РѕСЏРЅРёСЏ Chasing РѕСЃС‚Р°РЅР°РІР»РёРІР°РµРј РґРІРёР¶РµРЅРёРµ
                if (movement != null && movement.IsMoving())
                {
                    movement.StopMovement();
                }
                break;

            case AIState.Attacking:
                // РџСЂРё РІС‹С…РѕРґРµ РёР· СЃРѕСЃС‚РѕСЏРЅРёСЏ Attacking РЅРёС‡РµРіРѕ РѕСЃРѕР±РµРЅРЅРѕРіРѕ РЅРµ РґРµР»Р°РµРј
                // РђС‚Р°РєР° Р·Р°РІРµСЂС€РµРЅР° РёР»Рё РїСЂРµСЂРІР°РЅР°
                break;
        }
    }

    /// <summary>
    /// Р’С…РѕРґ РІ СЃРѕСЃС‚РѕСЏРЅРёРµ
    /// </summary>
    void EnterState(AIState state)
    {
        switch (state)
        {
            case AIState.PlayerControlled:
                // ARCHITECTURE: DEPRECATED - This state is no longer used
                // Selection is now handled via isPlayerControlled flag
                break;

            case AIState.Move:
                lastActionTime = Time.time; // ARCHITECTURE: Move - действие!

                // РљР РРўРР§Р•РЎРљР Р’РђР–РќРћ: РћС‡РёС‰Р°РµРј РёРЅС„РѕСЂРјР°С†РёСЋ РѕР± Р°С‚Р°РєСѓСЋС‰РµРј С‡С‚РѕР±С‹ РєРѕРЅС‚СЂР°С‚Р°РєР° РЅРµ РїРµСЂРµРѕРїСЂРµРґРµР»РёР»Р° РґРІРёР¶РµРЅРёРµ
                if (character != null)
                {
                    character.ClearLastAttacker();
                }

                // Р’РђР–РќРћ: РџСЂРё РїРѕР»СѓС‡РµРЅРёРё РєРѕРјР°РЅРґС‹ РґРІРёР¶РµРЅРёСЏ - РџР Р•Р Р«Р’РђР•Рњ Р‘РћР™
                if (combatSystem != null && character != null && character.IsPlayerCharacter())
                {
                    if (combatSystem.IsInCombat(character))
                    {
                        combatSystem.StopCombatForCharacter(character);
                    }
                }

                // РџСЂРё РІС…РѕРґРµ РІ СЃРѕСЃС‚РѕСЏРЅРёРµ Move РїСЂРѕСЃС‚Рѕ РїРѕР·РІРѕР»СЏРµРј РїРµСЂСЃРѕРЅР°Р¶Сѓ РґРІРёРіР°С‚СЊСЃСЏ
                // Р”РІРёР¶РµРЅРёРµ СѓР¶Рµ РѕР±СЂР°Р±Р°С‚С‹РІР°РµС‚СЃСЏ CharacterMovement
                break;

            case AIState.Idle:
                // РЈСЃС‚Р°РЅР°РІР»РёРІР°РµРј Р±Р°Р·РѕРІСѓСЋ РїРѕР·РёС†РёСЋ РґР»СЏ Р±Р»СѓР¶РґР°РЅРёСЏ
                idleBasePosition = transform.position;

                // РђР’РўРћРњРђРўРР§Р•РЎРљР Р—РђРџР РђРЁРР’РђР•Рњ РЎРўР РћРРўР•Р›Р¬РЎРўР’Рћ Р•РЎР›Р РџР•Р РЎРћРќРђР– РР“Р РћРљРђ
                if (character != null && character.IsPlayerCharacter() && constructionManager != null)
                {
                    constructionManager.TryAssignConstructionToIdleCharacter(character);
                }

                // Р—Р°РїСѓСЃРєР°РµРј РєРѕСЂСѓС‚РёРЅСѓ Р±Р»СѓР¶РґР°РЅРёСЏ (С‚РѕР»СЊРєРѕ РµСЃР»Рё РќР• РїРµСЂРµС€Р»Рё РІ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІРѕ)
                if (currentState == AIState.Idle)
                {
                    idleCoroutine = StartCoroutine(IdleWanderBehavior());
                }
                break;

            case AIState.Working:
                lastActionTime = Time.time; // ARCHITECTURE: Working - действие!

                // Р’РђР–РќРћ: РџСЂРё РЅР°Р·РЅР°С‡РµРЅРёРё СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР° - РџР Р•Р Р«Р’РђР•Рњ Р‘РћР™
                if (combatSystem != null && character != null && character.IsPlayerCharacter())
                {
                    if (combatSystem.IsInCombat(character))
                    {
                        combatSystem.StopCombatForCharacter(character);
                    }
                }

                // РџСЂРё РІС…РѕРґРµ РІ СЃРѕСЃС‚РѕСЏРЅРёРµ Working РѕСЃС‚Р°РЅР°РІР»РёРІР°РµРј РІСЃРµ Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРѕРµ РґРІРёР¶РµРЅРёРµ
                // Р—Р°РїРѕРјРёРЅР°РµРј С‚РµРєСѓС‰СѓСЋ РїРѕР·РёС†РёСЋ
                idleBasePosition = transform.position;

                // РћСЃС‚Р°РЅР°РІР»РёРІР°РµРј Р»СЋР±РѕРµ С‚РµРєСѓС‰РµРµ РґРІРёР¶РµРЅРёРµ
                if (movement != null && movement.IsMoving())
                {
                    movement.StopMovement();
                }
                break;

            case AIState.Mining:
                lastActionTime = Time.time; // ARCHITECTURE: Mining - действие!

                // Р’РђР–РќРћ: РџСЂРё РЅР°Р·РЅР°С‡РµРЅРёРё РґРѕР±С‹С‡Рё - РџР Р•Р Р«Р’РђР•Рњ Р‘РћР™
                if (combatSystem != null && character != null && character.IsPlayerCharacter())
                {
                    if (combatSystem.IsInCombat(character))
                    {
                        combatSystem.StopCombatForCharacter(character);
                    }
                }

                // РџСЂРё РІС…РѕРґРµ РІ СЃРѕСЃС‚РѕСЏРЅРёРµ Mining РѕСЃС‚Р°РЅР°РІР»РёРІР°РµРј РІСЃРµ Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРѕРµ РґРІРёР¶РµРЅРёРµ
                // Р—Р°РїРѕРјРёРЅР°РµРј С‚РµРєСѓС‰СѓСЋ РїРѕР·РёС†РёСЋ
                idleBasePosition = transform.position;

                // РћСЃС‚Р°РЅР°РІР»РёРІР°РµРј Р»СЋР±РѕРµ С‚РµРєСѓС‰РµРµ РґРІРёР¶РµРЅРёРµ
                if (movement != null && movement.IsMoving())
                {
                    movement.StopMovement();
                }
                break;

            case AIState.Chasing:
                lastActionTime = Time.time; // ARCHITECTURE: Chasing - действие!

                // РџСЂРё РІС…РѕРґРµ РІ СЃРѕСЃС‚РѕСЏРЅРёРµ Chasing РїРµСЂСЃРѕРЅР°Р¶ РЅР°С‡РёРЅР°РµС‚ РїСЂРµСЃР»РµРґРѕРІР°С‚СЊ С†РµР»СЊ
                // РџСЂРµСЃР»РµРґРѕРІР°РЅРёРµ СѓРїСЂР°РІР»СЏРµС‚СЃСЏ CombatSystem
                // Р—Р°РїРѕРјРёРЅР°РµРј С‚РµРєСѓС‰СѓСЋ РїРѕР·РёС†РёСЋ
                idleBasePosition = transform.position;

                // Р’РђР–РќРћ: Р—Р°РїРѕРјРёРЅР°РµРј РІСЂРµРјСЏ РЅР°С‡Р°Р»Р° Р±РѕСЏ РґР»СЏ Р·Р°РґРµСЂР¶РєРё РїСЂРѕРІРµСЂРєРё
                combatStartTime = Time.time;
                break;

            case AIState.Attacking:
                lastActionTime = Time.time; // ARCHITECTURE: Attacking - действие!

                // РџСЂРё РІС…РѕРґРµ РІ СЃРѕСЃС‚РѕСЏРЅРёРµ Attacking РїРµСЂСЃРѕРЅР°Р¶ РЅР°С‡РёРЅР°РµС‚ Р°С‚Р°РєРѕРІР°С‚СЊ
                // РђС‚Р°РєР° СѓРїСЂР°РІР»СЏРµС‚СЃСЏ CombatSystem
                // РћСЃС‚Р°РЅР°РІР»РёРІР°РµРј РґРІРёР¶РµРЅРёРµ, РµСЃР»Рё РѕРЅРѕ Р±С‹Р»Рѕ
                if (movement != null && movement.IsMoving())
                {
                    movement.StopMovement();
                }

                // Р’РђР–РќРћ: Р—Р°РїРѕРјРёРЅР°РµРј РІСЂРµРјСЏ РЅР°С‡Р°Р»Р° Р±РѕСЏ РґР»СЏ Р·Р°РґРµСЂР¶РєРё РїСЂРѕРІРµСЂРєРё
                combatStartTime = Time.time;
                break;
        }
    }

    /// <summary>
    /// РљРѕСЂСѓС‚РёРЅР° РїРѕРІРµРґРµРЅРёСЏ РІ СЃРѕСЃС‚РѕСЏРЅРёРё Idle
    /// </summary>
    IEnumerator IdleWanderBehavior()
    {
        isWandering = true;

        // Debug logging disabled

        while (currentState == AIState.Idle && isWandering)
        {
            // Р’С‹Р±РёСЂР°РµРј СЃР»СѓС‡Р°Р№РЅСѓСЋ С‚РѕС‡РєСѓ РІ РѕР±Р»Р°СЃС‚Рё 5x5 РєР»РµС‚РѕРє
            Vector3 wanderTarget = GetRandomWanderPoint();

            // Debug logging disabled

            // Р”РІРёРіР°РµРјСЃСЏ Рє С†РµР»Рё
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
                    // Р–РґРµРј Р·Р°РІРµСЂС€РµРЅРёСЏ РґРІРёР¶РµРЅРёСЏ
                    float waitTime = 0f;
                    while (movement.IsMoving() && currentState == AIState.Idle)
                    {
                        waitTime += 0.1f;
                        if (debugMode && waitTime % 2f < 0.1f) // Р›РѕРі РєР°Р¶РґС‹Рµ 2 СЃРµРєСѓРЅРґС‹
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

            // РџСЂРѕРІРµСЂСЏРµРј, С‡С‚Рѕ РјС‹ РІСЃРµ РµС‰Рµ РІ СЃРѕСЃС‚РѕСЏРЅРёРё Idle
            if (currentState != AIState.Idle)
            {
                // Debug logging disabled

                break;
            }

            // Debug logging disabled

            // РџР°СѓР·Р° РІ РґРѕСЃС‚РёРіРЅСѓС‚РѕР№ С‚РѕС‡РєРµ
            yield return new WaitForSeconds(pauseDuration);

            // РџСЂРѕРІРµСЂСЏРµРј, С‡С‚Рѕ РјС‹ РІСЃРµ РµС‰Рµ РІ СЃРѕСЃС‚РѕСЏРЅРёРё Idle
            if (currentState != AIState.Idle)
            {
                // Debug logging disabled

                break;
            }

            // Debug logging disabled

            // Р–РґРµРј РґРѕ СЃР»РµРґСѓСЋС‰РµРіРѕ РїРµСЂРµРјРµС‰РµРЅРёСЏ
            yield return new WaitForSeconds(wanderInterval);
        }

        isWandering = false;

        // Debug logging disabled
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ СЃР»СѓС‡Р°Р№РЅСѓСЋ С‚РѕС‡РєСѓ РґР»СЏ Р±Р»СѓР¶РґР°РЅРёСЏ РІ РѕР±Р»Р°СЃС‚Рё 5x5 РєР»РµС‚РѕРє
    /// </summary>
    Vector3 GetRandomWanderPoint()
    {
        if (gridManager == null)
        {
            // Debug logging disabled

            return idleBasePosition;
        }

        // РљРѕРЅРІРµСЂС‚РёСЂСѓРµРј Р±Р°Р·РѕРІСѓСЋ РїРѕР·РёС†РёСЋ РІ РєРѕРѕСЂРґРёРЅР°С‚С‹ СЃРµС‚РєРё
        Vector2Int baseGridPos = gridManager.WorldToGrid(idleBasePosition);

        // Debug logging disabled

        // РЎР»СѓС‡Р°Р№РЅР°СЏ РїРѕР·РёС†РёСЏ РІ РѕР±Р»Р°СЃС‚Рё 5x5 (РѕС‚ -2 РґРѕ +2 РєР»РµС‚РѕРє)
        int offsetX = Random.Range(-2, 3);
        int offsetY = Random.Range(-2, 3);

        Vector2Int targetGridPos = baseGridPos + new Vector2Int(offsetX, offsetY);

        // РџСЂРѕРІРµСЂСЏРµРј РІР°Р»РёРґРЅРѕСЃС‚СЊ РїРѕР·РёС†РёРё
        if (gridManager.IsValidGridPosition(targetGridPos))
        {
            // РџСЂРѕРІРµСЂСЏРµРј, С‡С‚Рѕ РєР»РµС‚РєР° РЅРµ Р·Р°РЅСЏС‚Р°
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

        // Р•СЃР»Рё РЅРµ СѓРґР°Р»РѕСЃСЊ РЅР°Р№С‚Рё СЃРІРѕР±РѕРґРЅСѓСЋ РєР»РµС‚РєСѓ, РїСЂРѕР±СѓРµРј РЅРµСЃРєРѕР»СЊРєРѕ СЂР°Р·
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

        // Р•СЃР»Рё РЅРµ РЅР°С€Р»Рё СЃРІРѕР±РѕРґРЅСѓСЋ РєР»РµС‚РєСѓ, РІРѕР·РІСЂР°С‰Р°РµРј Р±Р°Р·РѕРІСѓСЋ РїРѕР·РёС†РёСЋ
        // Debug logging disabled
        return idleBasePosition;
    }

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚С‡РёРє РёР·РјРµРЅРµРЅРёСЏ РІС‹РґРµР»РµРЅРёСЏ
    /// </summary>
    void OnSelectionChanged(System.Collections.Generic.List<GameObject> selectedObjects)
    {
        // РџСЂРѕРІРµСЂСЏРµРј, РІС‹РґРµР»РµРЅ Р»Рё СЌС‚РѕС‚ РїРµСЂСЃРѕРЅР°Р¶
        bool isSelected = selectedObjects.Contains(gameObject);
    
        if (isSelected)
        {
            isPlayerControlled = true;
            // ARCHITECTURE: Выделение НЕ сбрасывает таймер для Idle!

            // ARCHITECTURE: Если игрок кликает на персонажа в бою - ПРЕРЫВАЕМ БОЙ
            // Прерывание боя - это ДЕЙСТВИЕ, сбрасывает таймер Idle
            if ((currentState == AIState.Chasing || currentState == AIState.Attacking) &&
                character != null && character.IsPlayerCharacter() && combatSystem != null)
            {
                // Прерываем бой - персонаж перейдет в Idle
                combatSystem.StopCombatForCharacter(character);
                lastActionTime = Time.time; // Прерывание боя - это действие!
            }
        }
        else
        {
            isPlayerControlled = false;
        }
    }

    /// <summary>
    /// РџСЂРёРЅСѓРґРёС‚РµР»СЊРЅРѕ СѓСЃС‚Р°РЅРѕРІРёС‚СЊ СЃРѕСЃС‚РѕСЏРЅРёРµ РР
    /// </summary>
    public void SetAIState(AIState state)
    {
        SwitchToState(state);
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ С‚РµРєСѓС‰РµРµ СЃРѕСЃС‚РѕСЏРЅРёРµ РР
    /// </summary>
    public AIState GetCurrentState()
    {
        return currentState;
    }

    /// <summary>
    /// FIX: РЈСЃС‚Р°РЅРѕРІРёС‚СЊ С„Р»Р°Рі РґРІРёР¶РµРЅРёСЏ Рє СЂР°Р±РѕС‚Рµ
    /// </summary>
    public void SetMovingToJob(bool value)
    {
        movingToJob = value;
    }

    /// <summary>
    /// FIX: РџСЂРѕРІРµСЂРёС‚СЊ, РёРґРµС‚ Р»Рё РїРµСЂСЃРѕРЅР°Р¶ Рє СЂР°Р±РѕС‚Рµ
    /// </summary>
    public bool IsMovingToJob()
    {
        return movingToJob;
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ, Р±Р»СѓР¶РґР°РµС‚ Р»Рё РїРµСЂСЃРѕРЅР°Р¶
    /// </summary>
    public bool IsWandering()
    {
        return isWandering;
    }

    /// <summary>
    /// РЎРєР°РЅРёСЂРѕРІР°С‚СЊ РѕР±Р»Р°СЃС‚СЊ РЅР° РЅР°Р»РёС‡РёРµ С†РµР»РµР№ РґР»СЏ Р°С‚Р°РєРё (С‚РѕР»СЊРєРѕ РґР»СЏ РІСЂР°РіРѕРІ)
    /// </summary>
    void ScanForTargets()
    {
        if (character == null || !character.IsEnemyCharacter() || combatSystem == null)
            return;

        // РЈР¶Рµ РІ Р±РѕСЋ - РЅРµ РёС‰РµРј РЅРѕРІСѓСЋ С†РµР»СЊ
        if (combatSystem.IsInCombat(character))
            return;

        // РќР°С…РѕРґРёРј РІСЃРµС… РїРµСЂСЃРѕРЅР°Р¶РµР№ РІ СЂР°РґРёСѓСЃРµ
        Character[] allCharacters = FindObjectsOfType<Character>();
        Character closestTarget = null;
        float closestDistance = float.MaxValue;

        foreach (Character potentialTarget in allCharacters)
        {
            // РџСЂРѕРїСѓСЃРєР°РµРј СЃРµР±СЏ, РјРµСЂС‚РІС‹С…, Рё СЃРѕСЋР·РЅРёРєРѕРІ
            if (potentialTarget == character ||
                potentialTarget.IsDead() ||
                !potentialTarget.IsPlayerCharacter())
                continue;

            // РџСЂРѕРІРµСЂСЏРµРј СЂР°СЃСЃС‚РѕСЏРЅРёРµ
            float distance = Vector3.Distance(transform.position, potentialTarget.transform.position);
            if (distance <= enemyDetectionRange && distance < closestDistance)
            {
                closestTarget = potentialTarget;
                closestDistance = distance;
            }
        }

        // Р•СЃР»Рё РЅР°С€Р»Рё С†РµР»СЊ - Р°С‚Р°РєСѓРµРј
        if (closestTarget != null)
        {
            combatSystem.AssignCombatTarget(character, closestTarget);
            SwitchToState(AIState.Chasing);
        }
    }

    /// <summary>
    /// РЎРєР°РЅРёСЂРѕРІР°С‚СЊ РѕР±Р»Р°СЃС‚СЊ РЅР° РЅР°Р»РёС‡РёРµ РІСЂР°РіРѕРІ РґР»СЏ Р°С‚Р°РєРё (С‚РѕР»СЊРєРѕ РґР»СЏ СЃРѕСЋР·РЅРёРєРѕРІ)
    /// </summary>
    void ScanForEnemies()
    {
        if (character == null || !character.IsPlayerCharacter() || combatSystem == null)
        {
            return;
        }

        // РЈР¶Рµ РІ Р±РѕСЋ - РЅРµ РёС‰РµРј РЅРѕРІСѓСЋ С†РµР»СЊ
        if (combatSystem.IsInCombat(character))
        {
            return;
        }

        // РќР°С…РѕРґРёРј РІСЃРµС… РїРµСЂСЃРѕРЅР°Р¶РµР№ РІ СЂР°РґРёСѓСЃРµ
        Character[] allCharacters = FindObjectsOfType<Character>();
        Character closestEnemy = null;
        float closestDistance = float.MaxValue;
        int enemiesFound = 0;

        foreach (Character potentialEnemy in allCharacters)
        {
            // РџСЂРѕРїСѓСЃРєР°РµРј СЃРµР±СЏ, РјРµСЂС‚РІС‹С…, Рё СЃРѕСЋР·РЅРёРєРѕРІ
            if (potentialEnemy == character ||
                potentialEnemy.IsDead() ||
                !potentialEnemy.IsEnemyCharacter())
                continue;

            // РџСЂРѕРІРµСЂСЏРµРј СЂР°СЃСЃС‚РѕСЏРЅРёРµ
            float distance = Vector3.Distance(transform.position, potentialEnemy.transform.position);
            enemiesFound++;

            if (distance <= enemyDetectionRange && distance < closestDistance)
            {
                closestEnemy = potentialEnemy;
                closestDistance = distance;
            }
        }

        // Р•СЃР»Рё РЅР°С€Р»Рё РІСЂР°РіР° - Р°С‚Р°РєСѓРµРј
        if (closestEnemy != null)
        {
            // DEBUG: Р›РѕРіРёСЂСѓРµРј Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРѕРµ РѕР±РЅР°СЂСѓР¶РµРЅРёРµ РІСЂР°РіР°
            if (debugStateTransitions && character != null)
            {
                bool isSelected = character.IsSelected();
                string selectionStatus = isSelected ? "[SELECTED]" : "[NOT SELECTED]";
                bool isRecentPlayerCommand = (Time.time - playerMoveCommandTime) < 1f;
                string warning = isRecentPlayerCommand ? " *** INTERRUPTING PLAYER COMMAND! ***" : "";

                Debug.Log($"[AUTO-DETECT] {selectionStatus} {character.GetFullName()} detected enemy {closestEnemy.GetFullName()} at {closestDistance:F1}m | State: {currentState}{warning}");
            }

            combatSystem.AssignCombatTarget(character, closestEnemy);
            SwitchToState(AIState.Chasing);
        }
    }

    /// <summary>
    /// РЈРІРµРґРѕРјРёС‚СЊ Рѕ С‚РѕРј, С‡С‚Рѕ РґРІРёР¶РµРЅРёРµ РёРЅРёС†РёРёСЂРѕРІР°РЅРѕ РёРіСЂРѕРєРѕРј
    /// </summary>
    public void OnPlayerInitiatedMovement()
    {
        playerInitiatedMovement = true;

        // Р—Р°РїРѕРјРёРЅР°РµРј РІСЂРµРјСЏ РєРѕРјР°РЅРґС‹ РґР»СЏ РѕС‚Р»Р°РґРєРё
        playerMoveCommandTime = Time.time;

        // РљР РРўРР§Р•РЎРљР Р’РђР–РќРћ: РћС‡РёС‰Р°РµРј РёРЅС„РѕСЂРјР°С†РёСЋ РѕР± Р°С‚Р°РєСѓСЋС‰РµРј С‡С‚РѕР±С‹ РєРѕРЅС‚СЂР°С‚Р°РєР° РЅРµ РїРµСЂРµРѕРїСЂРµРґРµР»РёР»Р° РєРѕРјР°РЅРґСѓ РёРіСЂРѕРєР°
        if (character != null)
        {
            character.ClearLastAttacker();
        }

        // РћСЃС‚Р°РЅР°РІР»РёРІР°РµРј Р±РѕР№ РїСЂРё РїРѕР»СѓС‡РµРЅРёРё РєРѕРјР°РЅРґС‹ РґРІРёР¶РµРЅРёСЏ РѕС‚ РёРіСЂРѕРєР°
        if (combatSystem != null && character != null)
        {
            combatSystem.StopCombatForCharacter(character);
        }

        // РџР Р•Р Р«Р’РђР•Рњ Р‘РћР™ РµСЃР»Рё РїРµСЂСЃРѕРЅР°Р¶ Р±С‹Р» РІ СЃРѕСЃС‚РѕСЏРЅРёРё Chasing/Attacking
        if (currentState == AIState.Chasing || currentState == AIState.Attacking)
        {
            SwitchToState(AIState.Move);
        }

        // РџР Р•Р Р«Р’РђР•Рњ РЎРўР РћРРўР•Р›Р¬РЎРўР’Рћ РµСЃР»Рё РїРµСЂСЃРѕРЅР°Р¶ Р±С‹Р» РІ СЃРѕСЃС‚РѕСЏРЅРёРё Working
        if (currentState == AIState.Working)
        {
            // Р’РђР–РќРћ: РћСЃС‚Р°РЅР°РІР»РёРІР°РµРј СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІРѕ РІ ConstructionManager
            if (constructionManager != null)
            {
                constructionManager.StopConstructionForCharacter(character);
            }

            // РџРµСЂРµРєР»СЋС‡Р°РµРј СЃРѕСЃС‚РѕСЏРЅРёРµ - СЌС‚Рѕ РІС‹Р·РѕРІРµС‚ ExitState(Working) Рё РѕСЃС‚Р°РЅРѕРІРёС‚ РєРѕСЂСѓС‚РёРЅСѓ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
            SwitchToState(AIState.Move);
        }

        // РџР Р•Р Р«Р’РђР•Рњ Р”РћР‘Р«Р§РЈ РµСЃР»Рё РїРµСЂСЃРѕРЅР°Р¶ Р±С‹Р» РІ СЃРѕСЃС‚РѕСЏРЅРёРё Mining
        if (currentState == AIState.Mining)
        {
            // Р’РђР–РќРћ: РћСЃС‚Р°РЅР°РІР»РёРІР°РµРј РґРѕР±С‹С‡Сѓ РІ MiningManager
            if (miningManager != null)
            {
                miningManager.StopMiningForCharacter(character);
            }

            // РџРµСЂРµРєР»СЋС‡Р°РµРј СЃРѕСЃС‚РѕСЏРЅРёРµ - СЌС‚Рѕ РІС‹Р·РѕРІРµС‚ ExitState(Mining) Рё РѕСЃС‚Р°РЅРѕРІРёС‚ РґРІРёР¶РµРЅРёРµ
            SwitchToState(AIState.Move);
        }

        // РљР РРўРР§Р•РЎРљР Р’РђР–РќРћ: РџРµСЂРµРєР»СЋС‡Р°РµРј РІ СЃРѕСЃС‚РѕСЏРЅРёРµ Move РќР•РњР•Р”Р›Р•РќРќРћ РїСЂРё РїРѕР»СѓС‡РµРЅРёРё РєРѕРјР°РЅРґС‹ РёРіСЂРѕРєР°
        // Р­С‚Рѕ РїСЂРµРґРѕС‚РІСЂР°С‰Р°РµС‚ РєРѕРЅС‚СЂР°С‚Р°РєСѓ РґРѕ СЃРјРµРЅС‹ СЃРѕСЃС‚РѕСЏРЅРёСЏ
        // ARCHITECTURE: PlayerControlled is no longer a primary state
        if (currentState == AIState.Idle)
        {
            SwitchToState(AIState.Move);
        }
    }

    void OnDestroy()
    {
        // РћС‚РїРёСЃС‹РІР°РµРјСЃСЏ РѕС‚ СЃРѕР±С‹С‚РёР№
        if (selectionManager != null)
        {
            selectionManager.OnSelectionChanged -= OnSelectionChanged;
        }

        // РћСЃС‚Р°РЅР°РІР»РёРІР°РµРј РєРѕСЂСѓС‚РёРЅС‹
        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (gridManager == null) return;

        // РџРѕРєР°Р·С‹РІР°РµРј СЃРѕСЃС‚РѕСЏРЅРёРµ РїРµСЂСЃРѕРЅР°Р¶Р° С†РІРµС‚РѕРј
        switch (currentState)
        {
            case AIState.PlayerControlled:
                Gizmos.color = Color.cyan;
                break;
            case AIState.Move:
                Gizmos.color = Color.blue;
                break;
            case AIState.Idle:
                Gizmos.color = Color.yellow;
                // РџРѕРєР°Р·С‹РІР°РµРј РѕР±Р»Р°СЃС‚СЊ Р±Р»СѓР¶РґР°РЅРёСЏ
                Gizmos.DrawWireCube(idleBasePosition, new Vector3(wanderRadius * 2, 0.1f, wanderRadius * 2));
                break;
            case AIState.Working:
                Gizmos.color = Color.green;
                break;
            case AIState.Mining:
                Gizmos.color = new Color(0.5f, 0.3f, 0.1f); // РљРѕСЂРёС‡РЅРµРІС‹Р№ (С†РІРµС‚ СЂСѓРґС‹)
                break;
            case AIState.Chasing:
                Gizmos.color = new Color(1f, 0.5f, 0f); // РћСЂР°РЅР¶РµРІС‹Р№
                break;
            case AIState.Attacking:
                Gizmos.color = Color.red;
                break;
        }

        // РџРѕРєР°Р·С‹РІР°РµРј РёРЅРґРёРєР°С‚РѕСЂ СЃРѕСЃС‚РѕСЏРЅРёСЏ РЅР°Рґ РїРµСЂСЃРѕРЅР°Р¶РµРј
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.3f);
    }
}
