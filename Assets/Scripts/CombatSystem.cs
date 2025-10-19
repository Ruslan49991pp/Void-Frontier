using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// РЎРёСЃС‚РµРјР° Р±РѕРµРІС‹С… РґРµР№СЃС‚РІРёР№ СЃ РїСЂРµСЃР»РµРґРѕРІР°РЅРёРµРј РІСЂР°РіРѕРІ Рё Р°РЅРёРјР°С†РёРµР№ Р°С‚Р°Рє
/// ARCHITECTURE: РќР°СЃР»РµРґСѓРµС‚СЃСЏ РѕС‚ BaseManager РґР»СЏ РёРЅС‚РµРіСЂР°С†РёРё СЃ ServiceLocator
/// </summary>
public class CombatSystem : BaseManager
{
    [Header("Combat Settings")]
    public float attackRange = 1.5f; // Р”РёСЃС‚Р°РЅС†РёСЏ Р°С‚Р°РєРё (СЃРѕСЃРµРґРЅСЏСЏ РєР»РµС‚РєР°)
    public float attackCooldown = 1f; // РЎРєРѕСЂРѕСЃС‚СЊ Р°С‚Р°РєРё (1 СѓРґР°СЂ РІ СЃРµРєСѓРЅРґСѓ)
    public float attackDamage = 25f; // РЈСЂРѕРЅ РѕС‚ Р°С‚Р°РєРё
    public float pursuitRange = 10f; // Р”РёСЃС‚Р°РЅС†РёСЏ РїСЂРµСЃР»РµРґРѕРІР°РЅРёСЏ

    [Header("Attack Animation")]
    public float lungeDistance = 0.25f; // Р Р°СЃСЃС‚РѕСЏРЅРёРµ РїСЂС‹Р¶РєР° (1/4 РєР»РµС‚РєРё)
    public float lungeSpeed = 8f; // РЎРєРѕСЂРѕСЃС‚СЊ Р°РЅРёРјР°С†РёРё Р°С‚Р°РєРё
    public AnimationCurve lungeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Damage Indication")]
    public string damageIndicatorMaterialName = "M_Enemy_Damage";
    public float damageFlashDuration = 0.2f;

    [Header("Debug")]
    public bool debugMode = true; // Р’РєР»СЋС‡Р°РµРј debug РґР»СЏ РѕС‚СЃР»РµР¶РёРІР°РЅРёСЏ РїСЂРµСЃР»РµРґРѕРІР°РЅРёСЏ

    [Header("Line of Sight")]
    public LayerMask lineOfSightBlockers = -1; // РЎР»РѕРё РєРѕС‚РѕСЂС‹Рµ Р±Р»РѕРєРёСЂСѓСЋС‚ Р»РёРЅРёСЋ РІРёРґРёРјРѕСЃС‚Рё
    public float lineOfSightCheckInterval = 0.5f; // РРЅС‚РµСЂРІР°Р» РїСЂРѕРІРµСЂРєРё Р»РёРЅРёРё РІРёРґРёРјРѕСЃС‚Рё
    public int maxPositionSearchAttempts = 8; // РњР°РєСЃРёРјСѓРј РїРѕРїС‹С‚РѕРє РЅР°Р№С‚Рё РїРѕР·РёС†РёСЋ СЃ С‡РёСЃС‚РѕР№ С‚СЂР°РµРєС‚РѕСЂРёРµР№

    // РљРѕРјРїРѕРЅРµРЅС‚С‹ СЃРёСЃС‚РµРјС‹
    private SelectionManager selectionManager;
    private GridManager gridManager;
    private Camera playerCamera;
    private EnemyTargetingSystem enemyTargetingSystem;

    // РћС‚СЃР»РµР¶РёРІР°РЅРёРµ Р»РёРЅРёРё РІРёРґРёРјРѕСЃС‚Рё
    private Dictionary<Character, bool> hasLineOfSight = new Dictionary<Character, bool>();

    // РЎРѕСЃС‚РѕСЏРЅРёРµ Р±РѕРµРІС‹С… РґРµР№СЃС‚РІРёР№
    private Dictionary<Character, CombatData> activeCombatants = new Dictionary<Character, CombatData>();
    private Material damageIndicatorMaterial;

    // Р—Р°С‰РёС‚Р° РѕС‚ РѕРґРЅРѕРІСЂРµРјРµРЅРЅРѕРіРѕ РїСЂРёРјРµРЅРµРЅРёСЏ СѓСЂРѕРЅР° Рє РѕРґРЅРѕР№ С†РµР»Рё
    private HashSet<Character> damageIndicationInProgress = new HashSet<Character>();

    // РЎР»РѕРІР°СЂСЊ Р·Р°СЂРµР·РµСЂРІРёСЂРѕРІР°РЅРЅС‹С… Р±РѕРµРІС‹С… РїРѕР·РёС†РёР№ (С‡С‚РѕР±С‹ РїРµСЂСЃРѕРЅР°Р¶Рё РЅРµ РѕСЃС‚Р°РЅР°РІР»РёРІР°Р»РёСЃСЊ РІ РѕРґРЅРѕР№ РєР»РµС‚РєРµ)
    private Dictionary<Vector2Int, Character> reservedCombatPositions = new Dictionary<Vector2Int, Character>();

    // РљР»Р°СЃСЃ РґР»СЏ С…СЂР°РЅРµРЅРёСЏ РґР°РЅРЅС‹С… Рѕ Р±РѕРµРІС‹С… РґРµР№СЃС‚РІРёСЏС… РїРµСЂСЃРѕРЅР°Р¶Р°
    private class CombatData
    {
        public Character target;
        public float lastAttackTime;
        public bool isAttacking;
        public bool isPursuing;
        public Vector3 originalPosition;
        public Vector2Int reservedCombatPosition; // Р—Р°СЂРµР·РµСЂРІРёСЂРѕРІР°РЅРЅР°СЏ РїРѕР·РёС†РёСЏ РґР»СЏ СЃС‚СЂРµР»СЊР±С‹
        public Coroutine combatCoroutine;
        public Coroutine attackAnimationCoroutine;

        public CombatData()
        {
            target = null;
            lastAttackTime = GameConstants.Combat.INVALID_LAST_ATTACK_TIME;
            isAttacking = false;
            isPursuing = false;
            originalPosition = Vector3.zero;
            reservedCombatPosition = new Vector2Int(GameConstants.Combat.INVALID_GRID_POSITION, GameConstants.Combat.INVALID_GRID_POSITION);
            combatCoroutine = null;
            attackAnimationCoroutine = null;
        }
    }

    /// <summary>
    /// РРЅРёС†РёР°Р»РёР·Р°С†РёСЏ РјРµРЅРµРґР¶РµСЂР° Р±РѕСЏ С‡РµСЂРµР· ServiceLocator
    /// </summary>
    protected override void OnManagerInitialized()
    {
        base.OnManagerInitialized();

        selectionManager = GetService<SelectionManager>();
        gridManager = GetService<GridManager>();
        playerCamera = Camera.main;
        enemyTargetingSystem = GetService<EnemyTargetingSystem>();

        LoadDamageIndicatorMaterial();

        if (selectionManager == null)
        {
            LogError("SelectionManager not found!");
        }
        if (gridManager == null)
        {
            LogError("GridManager not found!");
        }
    }

    void Update()
    {
        // Р‘Р»РѕРєРёСЂСѓРµРј РІРІРѕРґ РµСЃР»Рё РјРµРЅСЋ РїР°СѓР·С‹ Р°РєС‚РёРІРЅРѕ
        if (!IsGamePaused())
        {
            HandleCombatInput();
        }
        UpdateCombatStates();
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ РЅР°С…РѕРґРёС‚СЃСЏ Р»Рё РёРіСЂР° РЅР° РїР°СѓР·Рµ
    /// </summary>
    bool IsGamePaused()
    {
        return GamePauseManager.Instance != null && GamePauseManager.Instance.IsPaused();
    }

    /// <summary>
    /// Р—Р°РіСЂСѓР·РєР° РјР°С‚РµСЂРёР°Р»Р° РёРЅРґРёРєР°С†РёРё СѓСЂРѕРЅР°
    /// </summary>
    void LoadDamageIndicatorMaterial()
    {
        // РџС‹С‚Р°РµРјСЃСЏ РЅР°Р№С‚Рё РјР°С‚РµСЂРёР°Р» РІ СЂРµСЃСѓСЂСЃР°С…
        damageIndicatorMaterial = Resources.Load<Material>(damageIndicatorMaterialName);

        // Р•СЃР»Рё РЅРµ РЅР°С€Р»Рё РІ Resources, РїС‹С‚Р°РµРјСЃСЏ РЅР°Р№С‚Рё РІ Assets/Materials
        if (damageIndicatorMaterial == null)
        {
            damageIndicatorMaterial = Resources.Load<Material>("Materials/" + damageIndicatorMaterialName);
        }

        if (damageIndicatorMaterial == null)
        {
            // РЎРѕР·РґР°РµРј РїСЂРѕСЃС‚РѕР№ РєСЂР°СЃРЅС‹Р№ РјР°С‚РµСЂРёР°Р» РµСЃР»Рё РЅРµ РЅР°С€Р»Рё M_Enemy_Damage
            damageIndicatorMaterial = new Material(Shader.Find("Standard"));
            damageIndicatorMaterial.color = new Color(1f, 0.2f, 0.2f, 1f); // РЇСЂРєРѕ-РєСЂР°СЃРЅС‹Р№
            damageIndicatorMaterial.SetFloat("_Mode", 0); // Opaque
            damageIndicatorMaterial.EnableKeyword("_EMISSION");
            damageIndicatorMaterial.SetColor("_EmissionColor", new Color(0.8f, 0f, 0f, 1f));
        }
    }

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚РєР° РІРІРѕРґР° РґР»СЏ Р±РѕРµРІС‹С… РґРµР№СЃС‚РІРёР№ (РџРљРњ РїРѕ РІСЂР°РіСѓ)
    /// </summary>
    void HandleCombatInput()
    {
        // РџСЂРѕРІРµСЂСЏРµРј РЅР°Р¶Р°С‚РёРµ РџРљРњ
        if (Input.GetMouseButtonDown(1))
        {
            // РџРѕР»СѓС‡Р°РµРј РІС‹РґРµР»РµРЅРЅС‹С… СЃРѕСЋР·РЅРёРєРѕРІ
            List<Character> selectedAllies = GetSelectedAllies();
            if (selectedAllies.Count == 0)
                return;

            // РџСЂРѕРІРµСЂСЏРµРј, РµСЃС‚СЊ Р»Рё РІСЂР°Рі РїРѕРґ РєСѓСЂСЃРѕСЂРѕРј
            Character targetEnemy = GetEnemyUnderMouse();
            if (targetEnemy != null)
            {

                // РќР°Р·РЅР°С‡Р°РµРј С†РµР»СЊ РґР»СЏ Р°С‚Р°РєРё РІСЃРµРј РІС‹РґРµР»РµРЅРЅС‹Рј СЃРѕСЋР·РЅРёРєР°Рј
                foreach (Character ally in selectedAllies)
                {
                    AssignCombatTarget(ally, targetEnemy);
                }
            }
        }
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РІСЂР°РіР° РїРѕРґ РєСѓСЂСЃРѕСЂРѕРј РјС‹С€Рё
    /// </summary>
    Character GetEnemyUnderMouse()
    {
        if (playerCamera == null)
            return null;

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);

        // РЎРѕСЂС‚РёСЂСѓРµРј С…РёС‚С‹ РїРѕ СЂР°СЃСЃС‚РѕСЏРЅРёСЋ
        System.Array.Sort(hits, (hit1, hit2) => hit1.distance.CompareTo(hit2.distance));

        foreach (RaycastHit hit in hits)
        {
            // РРіРЅРѕСЂРёСЂСѓРµРј СЃРёСЃС‚РµРјРЅС‹Рµ РѕР±СЉРµРєС‚С‹
            if (hit.collider.name.Contains("Location_Bounds") ||
                hit.collider.name.Contains("Grid") ||
                hit.collider.name.Contains("Terrain"))
                continue;

            Character character = hit.collider.GetComponent<Character>();
            if (character == null)
                character = hit.collider.GetComponentInParent<Character>();

            if (character != null && character.IsEnemyCharacter() && !character.IsDead())
            {
                return character;
            }
        }

        return null;
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ СЃРїРёСЃРѕРє РІС‹РґРµР»РµРЅРЅС‹С… СЃРѕСЋР·РЅРёРєРѕРІ
    /// </summary>
    List<Character> GetSelectedAllies()
    {
        List<Character> allies = new List<Character>();

        if (selectionManager != null)
        {
            var selectedObjects = selectionManager.GetSelectedObjects();
            foreach (var obj in selectedObjects)
            {
                Character character = obj.GetComponent<Character>();
                if (character != null && character.IsPlayerCharacter())
                {
                    allies.Add(character);
                }
            }
        }

        return allies;
    }

    /// <summary>
    /// РќР°Р·РЅР°С‡РёС‚СЊ С†РµР»СЊ РґР»СЏ Р±РѕРµРІС‹С… РґРµР№СЃС‚РІРёР№
    /// </summary>
    public void AssignCombatTarget(Character attacker, Character target)
    {
        if (attacker == null || target == null)
            return;

        // РћСЃС‚Р°РЅР°РІР»РёРІР°РµРј РїСЂРµРґС‹РґСѓС‰РёРµ Р±РѕРµРІС‹Рµ РґРµР№СЃС‚РІРёСЏ
        StopCombat(attacker);

        // РЎРѕР·РґР°РµРј РёР»Рё РѕР±РЅРѕРІР»СЏРµРј РґР°РЅРЅС‹Рµ Рѕ Р±РѕРµРІС‹С… РґРµР№СЃС‚РІРёСЏС…
        if (!activeCombatants.ContainsKey(attacker))
        {
            activeCombatants[attacker] = new CombatData();
        }

        CombatData combatData = activeCombatants[attacker];
        combatData.target = target;
        combatData.isPursuing = true;
        combatData.originalPosition = attacker.transform.position;

        // Р—Р°РїСѓСЃРєР°РµРј РєРѕСЂСѓС‚РёРЅСѓ Р±РѕРµРІС‹С… РґРµР№СЃС‚РІРёР№
        combatData.combatCoroutine = StartCoroutine(CombatBehavior(attacker, combatData));

    }

    /// <summary>
    /// РћСЃС‚Р°РЅРѕРІРёС‚СЊ Р±РѕРµРІС‹Рµ РґРµР№СЃС‚РІРёСЏ РїРµСЂСЃРѕРЅР°Р¶Р° (РїСѓР±Р»РёС‡РЅС‹Р№ РјРµС‚РѕРґ)
    /// </summary>
    public void StopCombatForCharacter(Character attacker)
    {
        StopCombat(attacker);
    }

    /// <summary>
    /// РћСЃС‚Р°РЅРѕРІРёС‚СЊ Р±РѕРµРІС‹Рµ РґРµР№СЃС‚РІРёСЏ РїРµСЂСЃРѕРЅР°Р¶Р°
    /// </summary>
    public void StopCombat(Character attacker)
    {
        // РџСЂРѕРІРµСЂСЏРµРј С‡С‚Рѕ РїРµСЂСЃРѕРЅР°Р¶ РµС‰Рµ СЃСѓС‰РµСЃС‚РІСѓРµС‚
        if (attacker == null)
        {
            return;
        }

        if (!activeCombatants.ContainsKey(attacker))
        {
            return;
        }

        CombatData combatData = activeCombatants[attacker];

        // РћСЃС‚Р°РЅР°РІР»РёРІР°РµРј РєРѕСЂСѓС‚РёРЅС‹
        if (combatData.combatCoroutine != null)
        {
            StopCoroutine(combatData.combatCoroutine);
            combatData.combatCoroutine = null;
        }

        if (combatData.attackAnimationCoroutine != null)
        {
            StopCoroutine(combatData.attackAnimationCoroutine);
            combatData.attackAnimationCoroutine = null;
        }

        // Р’РђР–РќРћ: РћСЃРІРѕР±РѕР¶РґР°РµРј Р·Р°СЂРµР·РµСЂРІРёСЂРѕРІР°РЅРЅСѓСЋ Р±РѕРµРІСѓСЋ РїРѕР·РёС†РёСЋ
        if (combatData.reservedCombatPosition.x != GameConstants.Combat.INVALID_GRID_POSITION &&
            combatData.reservedCombatPosition.y != GameConstants.Combat.INVALID_GRID_POSITION)
        {
            if (reservedCombatPositions.ContainsKey(combatData.reservedCombatPosition))
            {
                if (reservedCombatPositions[combatData.reservedCombatPosition] == attacker)
                {
                    reservedCombatPositions.Remove(combatData.reservedCombatPosition);
                }
            }
        }

        // РћСЃС‚Р°РЅР°РІР»РёРІР°РµРј РґРІРёР¶РµРЅРёРµ (С‚РѕР»СЊРєРѕ РµСЃР»Рё РїРµСЂСЃРѕРЅР°Р¶ РµС‰Рµ СЃСѓС‰РµСЃС‚РІСѓРµС‚)
        if (attacker != null)
        {
            CharacterMovement movement = attacker.GetComponent<CharacterMovement>();
            if (movement != null)
            {
                movement.StopMovement();
            }
        }

        // РЈР±РёСЂР°РµРј РёРЅРґРёРєР°С‚РѕСЂ С†РµР»Рё, РµСЃР»Рё РЅРёРєС‚Рѕ Р±РѕР»СЊС€Рµ РЅРµ Р°С‚Р°РєСѓРµС‚ СЌС‚Сѓ С†РµР»СЊ
        Character target = combatData.target;
        activeCombatants.Remove(attacker);

        if (target != null && enemyTargetingSystem != null)
        {
            // РџСЂРѕРІРµСЂСЏРµРј, Р°С‚Р°РєСѓРµС‚ Р»Рё РєС‚Рѕ-С‚Рѕ РµС‰Рµ СЌС‚Сѓ С†РµР»СЊ
            bool isTargetStillBeingAttacked = false;
            foreach (var combat in activeCombatants.Values)
            {
                if (combat.target == target)
                {
                    isTargetStillBeingAttacked = true;
                    break;
                }
            }

            // Р•СЃР»Рё РЅРёРєС‚Рѕ Р±РѕР»СЊС€Рµ РЅРµ Р°С‚Р°РєСѓРµС‚ СЌС‚Сѓ С†РµР»СЊ, СѓР±РёСЂР°РµРј РёРЅРґРёРєР°С‚РѕСЂ
            if (!isTargetStillBeingAttacked)
            {
                enemyTargetingSystem.ClearTargetForCharacter(attacker);
            }
        }
    }

    /// <summary>
    /// РћСЃРЅРѕРІРЅР°СЏ РєРѕСЂСѓС‚РёРЅР° Р±РѕРµРІРѕРіРѕ РїРѕРІРµРґРµРЅРёСЏ
    /// </summary>
    IEnumerator CombatBehavior(Character attacker, CombatData combatData)
    {
        CharacterMovement movement = attacker.GetComponent<CharacterMovement>();
        CharacterAI ai = attacker.GetComponent<CharacterAI>();

        if (movement == null)
        {
            movement = attacker.gameObject.AddComponent<CharacterMovement>();
        }

        while (combatData.target != null && combatData.target.GetHealth() > 0 && !combatData.target.IsDead())
        {
            float distanceToTarget = Vector3.Distance(attacker.transform.position, combatData.target.transform.position);

            // РџРѕР»СѓС‡Р°РµРј СЃРёСЃС‚РµРјСѓ РѕСЂСѓР¶РёСЏ РґР»СЏ РѕРїСЂРµРґРµР»РµРЅРёСЏ РґР°Р»СЊРЅРѕСЃС‚Рё Р°С‚Р°РєРё
            WeaponSystem weaponSystem = attacker.GetComponent<WeaponSystem>();
            float currentAttackRange = attackRange; // Р”РµС„РѕР»С‚РЅР°СЏ РґР°Р»СЊРЅРѕСЃС‚СЊ
            float currentAttackCooldown = attackCooldown; // Р”РµС„РѕР»С‚РЅС‹Р№ РєСѓР»РґР°СѓРЅ

            if (weaponSystem != null)
            {
                // Р’С‹Р±РёСЂР°РµРј Р»СѓС‡С€РµРµ РѕСЂСѓР¶РёРµ РґР»СЏ С‚РµРєСѓС‰РµР№ РґРёСЃС‚Р°РЅС†РёРё
                weaponSystem.SelectBestWeapon(combatData.target.transform.position, distanceToTarget);

                Weapon currentWeapon = weaponSystem.GetCurrentWeapon();
                if (currentWeapon != null)
                {
                    currentAttackRange = currentWeapon.range;
                    currentAttackCooldown = currentWeapon.GetAttackCooldown();
                }
            }

            // РџСЂРѕРІРµСЂСЏРµРј, РЅР°С…РѕРґРёС‚СЃСЏ Р»Рё С†РµР»СЊ РІ РґРёСЃС‚Р°РЅС†РёРё Р°С‚Р°РєРё
            if (distanceToTarget <= currentAttackRange)
            {
                // Р¦РµР»СЊ РІ РґРёСЃС‚Р°РЅС†РёРё Р°С‚Р°РєРё - РїСЂРѕРІРµСЂСЏРµРј Р»РёРЅРёСЋ РІРёРґРёРјРѕСЃС‚Рё РґР»СЏ РґР°Р»СЊРЅРѕР±РѕР№РЅРѕРіРѕ РѕСЂСѓР¶РёСЏ
                Weapon currentWeapon = weaponSystem?.GetCurrentWeapon();
                bool needsLineOfSight = currentWeapon != null && currentWeapon.weaponType == WeaponType.Ranged;
                bool hasLineOfSight = true;

                if (needsLineOfSight)
                {
                    bool blockedByAlly;
                    hasLineOfSight = HasClearLineOfSight(attacker, combatData.target, out blockedByAlly);

                    if (!hasLineOfSight)
                    {
                        // Р•СЃР»Рё Р·Р°Р±Р»РѕРєРёСЂРѕРІР°РЅРѕ СЃРѕСЋР·РЅРёРєРѕРј - РїСЂРѕРґРѕР»Р¶Р°РµРј СЃС‚СЂРµР»СЏС‚СЊ (РґСЂСѓР¶РµСЃС‚РІРµРЅРЅС‹Р№ РѕРіРѕРЅСЊ)
                        // Р•СЃР»Рё Р·Р°Р±Р»РѕРєРёСЂРѕРІР°РЅРѕ СЃС‚РµРЅРѕР№/РїСЂРµРїСЏС‚СЃС‚РІРёРµРј - РёС‰РµРј РЅРѕРІСѓСЋ РїРѕР·РёС†РёСЋ
                        if (!blockedByAlly)
                        {
                            // Р›РёРЅРёСЏ РІРёРґРёРјРѕСЃС‚Рё Р·Р°Р±Р»РѕРєРёСЂРѕРІР°РЅР° СЃС‚РµРЅРѕР№/РїСЂРµРїСЏС‚СЃС‚РІРёРµРј - РёС‰РµРј РїРѕР·РёС†РёСЋ СЃ С‡РёСЃС‚С‹Рј РІС‹СЃС‚СЂРµР»РѕРј
                            Vector3? clearShotPosition = FindPositionWithClearShot(attacker, combatData.target, currentAttackRange);

                            if (clearShotPosition.HasValue)
                            {
                                // РќР°С€Р»Рё РїРѕР·РёС†РёСЋ - РґРІРёРіР°РµРјСЃСЏ С‚СѓРґР°
                                movement.MoveTo(clearShotPosition.Value);
                                combatData.isPursuing = true;

                                // РќР• РІС‹Р·С‹РІР°РµРј OnPlayerInitiatedMovement() - СЌС‚Рѕ РґРІРёР¶РµРЅРёРµ РёРЅРёС†РёРёСЂРѕРІР°РЅРѕ Р±РѕРµРІРѕР№ СЃРёСЃС‚РµРјРѕР№, Р° РЅРµ РёРіСЂРѕРєРѕРј

                                yield return new WaitForSeconds(GameConstants.Combat.COMBAT_FRAME_DELAY);
                                continue; // РџСЂРѕРїСѓСЃРєР°РµРј Р°С‚Р°РєСѓ, РїСЂРѕРґРѕР»Р¶Р°РµРј РґРІРёР¶РµРЅРёРµ
                            }
                            else
                            {
                                // РќРµ РЅР°С€Р»Рё РїРѕР·РёС†РёСЋ - РїРѕРґС…РѕРґРёРј Р±Р»РёР¶Рµ
                                Vector3 targetPosition = GetNearestAttackPosition(attacker, combatData.target);
                                movement.MoveTo(targetPosition);
                                combatData.isPursuing = true;

                                // РќР• РІС‹Р·С‹РІР°РµРј OnPlayerInitiatedMovement() - СЌС‚Рѕ РґРІРёР¶РµРЅРёРµ РёРЅРёС†РёРёСЂРѕРІР°РЅРѕ Р±РѕРµРІРѕР№ СЃРёСЃС‚РµРјРѕР№, Р° РЅРµ РёРіСЂРѕРєРѕРј

                                yield return new WaitForSeconds(GameConstants.Combat.COMBAT_FRAME_DELAY);
                                continue;
                            }
                        }
                        // Р•СЃР»Рё Р·Р°Р±Р»РѕРєРёСЂРѕРІР°РЅРѕ СЃРѕСЋР·РЅРёРєРѕРј - РЅРµ РґРІРёРіР°РµРјСЃСЏ, РїСЂРѕРґРѕР»Р¶Р°РµРј Рє Р°С‚Р°РєРµ (РґСЂСѓР¶РµСЃС‚РІРµРЅРЅС‹Р№ РѕРіРѕРЅСЊ РїСЂРѕРёР·РѕР№РґРµС‚)
                    }
                }

                // Р›РёРЅРёСЏ РІРёРґРёРјРѕСЃС‚Рё С‡РёСЃС‚Р°СЏ РёР»Рё РѕСЂСѓР¶РёРµ Р±Р»РёР¶РЅРµРіРѕ Р±РѕСЏ - РїСЂРѕРІРµСЂСЏРµРј РїРѕР·РёС†РёСЋ РїРµСЂРµРґ РѕСЃС‚Р°РЅРѕРІРєРѕР№
                Vector2Int currentGridPos = gridManager.WorldToGrid(attacker.transform.position);
                GridCell currentCell = gridManager.GetCell(currentGridPos);

                // РџР РћР’Р•Р РљРђ: РќРµ Р·Р°РЅСЏС‚Р° Р»Рё С‚РµРєСѓС‰Р°СЏ РєР»РµС‚РєР° РёР»Рё Р·Р°СЂРµР·РµСЂРІРёСЂРѕРІР°РЅР° РґСЂСѓРіРёРј РїРµСЂСЃРѕРЅР°Р¶РµРј?
                bool cellOccupied = currentCell != null && currentCell.isOccupied;
                bool cellReservedByOther = reservedCombatPositions.ContainsKey(currentGridPos) &&
                                          reservedCombatPositions[currentGridPos] != attacker;

                if (cellOccupied || cellReservedByOther)
                {
                    // РљР»РµС‚РєР° Р·Р°РЅСЏС‚Р°! РС‰РµРј Р±Р»РёР¶Р°Р№С€СѓСЋ СЃРІРѕР±РѕРґРЅСѓСЋ РєР»РµС‚РєСѓ РІРѕРєСЂСѓРі С†РµР»Рё
                    Vector3 freePosition = GetNearestAttackPosition(attacker, combatData.target);
                    Vector2Int freeGridPos = gridManager.WorldToGrid(freePosition);

                    // Р•СЃР»Рё РЅР°С€Р»Рё РґСЂСѓРіСѓСЋ РєР»РµС‚РєСѓ - РґРІРёРіР°РµРјСЃСЏ Рє РЅРµР№
                    if (freeGridPos != currentGridPos)
                    {
                        movement.MoveTo(freePosition);
                        combatData.isPursuing = true;
                        yield return new WaitForSeconds(GameConstants.Combat.COMBAT_FRAME_DELAY);
                        continue; // РџСЂРѕРґРѕР»Р¶Р°РµРј С†РёРєР», РЅРµ РѕСЃС‚Р°РЅР°РІР»РёРІР°РµРјСЃСЏ
                    }
                }

                // РљР»РµС‚РєР° СЃРІРѕР±РѕРґРЅР° - РѕСЃС‚Р°РЅР°РІР»РёРІР°РµРјСЃСЏ Рё Р°С‚Р°РєСѓРµРј
                combatData.isPursuing = false;
                movement.StopMovement();

                // Р’РђР–РќРћ: Р’С‹СЂР°РІРЅРёРІР°РµРј РїРѕР·РёС†РёСЋ РїРѕ С†РµРЅС‚СЂСѓ РєР»РµС‚РєРё РґР»СЏ С‚РѕС‡РЅРѕР№ СЃС‚СЂРµР»СЊР±С‹
                Vector3 cellCenterPosition = gridManager.GridToWorld(currentGridPos);
                attacker.transform.position = cellCenterPosition;

                if (combatData.reservedCombatPosition.x == GameConstants.Combat.INVALID_GRID_POSITION &&
                    combatData.reservedCombatPosition.y == GameConstants.Combat.INVALID_GRID_POSITION)
                {
                    // Р•С‰Рµ РЅРµ СЂРµР·РµСЂРІРёСЂРѕРІР°Р»Рё - СЂРµР·РµСЂРІРёСЂСѓРµРј С‚РµРєСѓС‰СѓСЋ РїРѕР·РёС†РёСЋ
                    combatData.reservedCombatPosition = currentGridPos;
                    reservedCombatPositions[currentGridPos] = attacker;
                }
                else if (combatData.reservedCombatPosition != currentGridPos)
                {
                    // РџРµСЂСЃРѕРЅР°Р¶ СЃРґРІРёРЅСѓР»СЃСЏ - РѕСЃРІРѕР±РѕР¶РґР°РµРј СЃС‚Р°СЂСѓСЋ СЂРµР·РµСЂРІР°С†РёСЋ Рё СЂРµР·РµСЂРІРёСЂСѓРµРј РЅРѕРІСѓСЋ
                    if (reservedCombatPositions.ContainsKey(combatData.reservedCombatPosition))
                    {
                        if (reservedCombatPositions[combatData.reservedCombatPosition] == attacker)
                        {
                            reservedCombatPositions.Remove(combatData.reservedCombatPosition);
                        }
                    }
                    combatData.reservedCombatPosition = currentGridPos;
                    reservedCombatPositions[currentGridPos] = attacker;
                }

                // РџСЂРѕРІРµСЂСЏРµРј РєСѓР»РґР°СѓРЅ Р°С‚Р°РєРё - Р°С‚Р°РєР° РЅР°С‡РёРЅР°РµС‚СЃСЏ РўРћР›Р¬РљРћ РµСЃР»Рё РЅРµ Р°С‚Р°РєСѓРµРј СЃРµР№С‡Р°СЃ Р РїСЂРѕС€РµР» РєСѓР»РґР°СѓРЅ
                float timeSinceLastAttack = Time.time - combatData.lastAttackTime;
                if (!combatData.isAttacking && timeSinceLastAttack >= currentAttackCooldown)
                {
                    // Р’С‹РїРѕР»РЅСЏРµРј Р°С‚Р°РєСѓ - Р±Р»РѕРєРёСЂСѓРµРј РЅРѕРІС‹Рµ Р°С‚Р°РєРё РїРѕРєР° РЅРµ Р·Р°РІРµСЂС€РёС‚СЃСЏ
                    yield return StartCoroutine(PerformAttack(attacker, combatData));
                }
                else
                {
                    // Р–РґРµРј РіРѕС‚РѕРІРЅРѕСЃС‚Рё Рє СЃР»РµРґСѓСЋС‰РµР№ Р°С‚Р°РєРµ
                    yield return new WaitForSeconds(GameConstants.Combat.COMBAT_FRAME_DELAY);
                }
            }
            else
            {
                // Р¦РµР»СЊ Р·Р° РїСЂРµРґРµР»Р°РјРё РґРёСЃС‚Р°РЅС†РёРё Р°С‚Р°РєРё - РїСЂРµСЃР»РµРґСѓРµРј РµРµ РЅРµР·Р°РІРёСЃРёРјРѕ РѕС‚ СЂР°СЃСЃС‚РѕСЏРЅРёСЏ
                Vector3 targetPosition = GetNearestAttackPosition(attacker, combatData.target);

                // РџСЂРѕРІРµСЂСЏРµРј, РЅСѓР¶РЅРѕ Р»Рё РѕР±РЅРѕРІРёС‚СЊ РјР°СЂС€СЂСѓС‚ (РµСЃР»Рё С†РµР»СЊ СЃРґРІРёРЅСѓР»Р°СЃСЊ РёР»Рё РїРµСЂСЃРѕРЅР°Р¶ РЅРµ РґРІРёР¶РµС‚СЃСЏ)
                bool needToUpdatePath = !movement.IsMoving() ||
                                       Vector3.Distance(movement.GetDestination(), targetPosition) > GameConstants.Combat.PATH_UPDATE_THRESHOLD;

                if (!combatData.isPursuing || needToUpdatePath)
                {
                    movement.MoveTo(targetPosition);
                    combatData.isPursuing = true;

                    // РќР• РІС‹Р·С‹РІР°РµРј OnPlayerInitiatedMovement() - СЌС‚Рѕ РґРІРёР¶РµРЅРёРµ РёРЅРёС†РёРёСЂРѕРІР°РЅРѕ Р±РѕРµРІРѕР№ СЃРёСЃС‚РµРјРѕР№, Р° РЅРµ РёРіСЂРѕРєРѕРј

                }

                // РћСЃРІРѕР±РѕР¶РґР°РµРј СЂРµР·РµСЂРІР°С†РёСЋ РµСЃР»Рё РїСЂРµСЃР»РµРґСѓРµРј (РЅРµ СЃС‚РѕРёРј РЅР° РјРµСЃС‚Рµ РґР»СЏ СЃС‚СЂРµР»СЊР±С‹)
                if (combatData.reservedCombatPosition.x != GameConstants.Combat.INVALID_GRID_POSITION &&
                    combatData.reservedCombatPosition.y != GameConstants.Combat.INVALID_GRID_POSITION)
                {
                    if (reservedCombatPositions.ContainsKey(combatData.reservedCombatPosition))
                    {
                        if (reservedCombatPositions[combatData.reservedCombatPosition] == attacker)
                        {
                            reservedCombatPositions.Remove(combatData.reservedCombatPosition);
                        }
                    }
                    combatData.reservedCombatPosition = new Vector2Int(GameConstants.Combat.INVALID_GRID_POSITION, GameConstants.Combat.INVALID_GRID_POSITION);
                }

                yield return new WaitForSeconds(GameConstants.Combat.COMBAT_FRAME_DELAY); // Р‘РѕР»РµРµ С‡Р°СЃС‚РѕРµ РѕР±РЅРѕРІР»РµРЅРёРµ РґР»СЏ Р»СѓС‡С€РµРіРѕ РїСЂРµСЃР»РµРґРѕРІР°РЅРёСЏ
            }

            yield return null;
        }

        // Р‘РѕРµРІС‹Рµ РґРµР№СЃС‚РІРёСЏ Р·Р°РІРµСЂС€РµРЅС‹

        // РЈР±РёСЂР°РµРј РёРЅРґРёРєР°С‚РѕСЂ С†РµР»Рё РїСЂРё Р·Р°РІРµСЂС€РµРЅРёРё Р±РѕСЏ
        Character target = combatData.target;
        activeCombatants.Remove(attacker);

        if (target != null && enemyTargetingSystem != null)
        {
            // РџСЂРѕРІРµСЂСЏРµРј, Р°С‚Р°РєСѓРµС‚ Р»Рё РєС‚Рѕ-С‚Рѕ РµС‰Рµ СЌС‚Сѓ С†РµР»СЊ
            bool isTargetStillBeingAttacked = false;
            foreach (var combat in activeCombatants.Values)
            {
                if (combat.target == target)
                {
                    isTargetStillBeingAttacked = true;
                    break;
                }
            }

            // Р•СЃР»Рё РЅРёРєС‚Рѕ Р±РѕР»СЊС€Рµ РЅРµ Р°С‚Р°РєСѓРµС‚ СЌС‚Сѓ С†РµР»СЊ, СѓР±РёСЂР°РµРј РёРЅРґРёРєР°С‚РѕСЂ
            if (!isTargetStillBeingAttacked)
            {
                enemyTargetingSystem.ClearTargetForCharacter(attacker);
            }
        }
    }

    /// <summary>
    /// Р’С‹РїРѕР»РЅРёС‚СЊ Р°С‚Р°РєСѓ
    /// </summary>
    IEnumerator PerformAttack(Character attacker, CombatData combatData)
    {
        // РџРѕР»СѓС‡Р°РµРј СЃРёСЃС‚РµРјСѓ РѕСЂСѓР¶РёСЏ Р°С‚Р°РєСѓСЋС‰РµРіРѕ
        WeaponSystem weaponSystem = attacker.GetComponent<WeaponSystem>();
        if (weaponSystem == null)
        {
            // Р•СЃР»Рё РЅРµС‚ СЃРёСЃС‚РµРјС‹ РѕСЂСѓР¶РёСЏ, РёСЃРїРѕР»СЊР·СѓРµРј СЃС‚Р°СЂСѓСЋ СЃРёСЃС‚РµРјСѓ
            yield return StartCoroutine(PerformLegacyAttack(attacker, combatData));
            yield break;
        }

        float distanceToTarget = Vector3.Distance(attacker.transform.position, combatData.target.transform.position);

        // РСЃРїРѕР»СЊР·СѓРµРј СЃРёСЃС‚РµРјСѓ РѕСЂСѓР¶РёСЏ РґР»СЏ Р°С‚Р°РєРё
        combatData.isAttacking = true;

        // РЎРёСЃС‚РµРјР° РѕСЂСѓР¶РёСЏ СЃР°РјР° РІС‹Р±РµСЂРµС‚ РїРѕРґС…РѕРґСЏС‰РµРµ РѕСЂСѓР¶РёРµ Рё РІС‹РїРѕР»РЅРёС‚ Р°С‚Р°РєСѓ
        weaponSystem.AttackTarget(combatData.target);

        // РџРѕР»СѓС‡Р°РµРј РІСЂРµРјСЏ Р°С‚Р°РєРё РѕС‚ С‚РµРєСѓС‰РµРіРѕ РѕСЂСѓР¶РёСЏ
        Weapon currentWeapon = weaponSystem.GetCurrentWeapon();
        float attackCooldownTime = currentWeapon != null ? currentWeapon.GetAttackCooldown() : attackCooldown;

        // Р–РґРµРј Р·Р°РІРµСЂС€РµРЅРёСЏ Р°С‚Р°РєРё
        yield return new WaitForSeconds(attackCooldownTime * GameConstants.Combat.ATTACK_COOLDOWN_MULTIPLIER);

        // Р’РђР–РќРћ: РЈСЃС‚Р°РЅР°РІР»РёРІР°РµРј РІСЂРµРјСЏ РїРѕСЃР»РµРґРЅРµР№ Р°С‚Р°РєРё РџРћРЎР›Р• Р·Р°РІРµСЂС€РµРЅРёСЏ РІСЃРµР№ Р°С‚Р°РєРё
        combatData.lastAttackTime = Time.time;
        combatData.isAttacking = false;
        combatData.attackAnimationCoroutine = null;
    }

    /// <summary>
    /// Р’С‹РїРѕР»РЅРёС‚СЊ Р°С‚Р°РєСѓ РїРѕ СЃС‚Р°СЂРѕР№ СЃРёСЃС‚РµРјРµ (РґР»СЏ СЃРѕРІРјРµСЃС‚РёРјРѕСЃС‚Рё)
    /// </summary>
    IEnumerator PerformLegacyAttack(Character attacker, CombatData combatData)
    {
        // РџСЂРѕРІРµСЂСЏРµРј, С‡С‚Рѕ С†РµР»СЊ РІСЃРµ РµС‰Рµ РІ РґРёСЃС‚Р°РЅС†РёРё Р°С‚Р°РєРё РїРµСЂРµРґ РЅР°С‡Р°Р»РѕРј
        float distanceCheck = Vector3.Distance(attacker.transform.position, combatData.target.transform.position);
        if (distanceCheck > attackRange)
        {
            yield break;
        }

        combatData.isAttacking = true;

        // РџРѕРІРѕСЂР°С‡РёРІР°РµРј Р°С‚Р°РєСѓСЋС‰РµРіРѕ Р»РёС†РѕРј Рє С†РµР»Рё
        yield return StartCoroutine(RotateTowardsTarget(attacker, combatData.target));

        // Р—Р°РїСѓСЃРєР°РµРј Р°РЅРёРјР°С†РёСЋ Р°С‚Р°РєРё
        combatData.attackAnimationCoroutine = StartCoroutine(AttackAnimation(attacker, combatData.target));

        // Р–РґРµРј Р·Р°РІРµСЂС€РµРЅРёСЏ Р°РЅРёРјР°С†РёРё Р°С‚Р°РєРё
        yield return combatData.attackAnimationCoroutine;

        // РќР°РЅРѕСЃРёРј СѓСЂРѕРЅ
        if (combatData.target != null && combatData.target.GetHealth() > 0 && !combatData.target.IsDead())
        {
            combatData.target.TakeDamage(attackDamage);

            // РџРѕРєР°Р·С‹РІР°РµРј РёРЅРґРёРєР°С†РёСЋ СѓСЂРѕРЅР°
            StartCoroutine(ShowDamageIndication(combatData.target));

            // РџРѕРєР°Р·С‹РІР°РµРј С‚РµРєСЃС‚ СѓСЂРѕРЅР°
            ShowDamageText(combatData.target, attackDamage);
        }

        // Р’РђР–РќРћ: РЈСЃС‚Р°РЅР°РІР»РёРІР°РµРј РІСЂРµРјСЏ РїРѕСЃР»РµРґРЅРµР№ Р°С‚Р°РєРё РџРћРЎР›Р• Р·Р°РІРµСЂС€РµРЅРёСЏ РІСЃРµР№ Р°С‚Р°РєРё
        combatData.lastAttackTime = Time.time;
        combatData.isAttacking = false;
        combatData.attackAnimationCoroutine = null;
    }

    /// <summary>
    /// РђРЅРёРјР°С†РёСЏ Р°С‚Р°РєРё (РїСЂС‹Р¶РѕРє РІ РєР»РµС‚РєСѓ РІСЂР°РіР° Рё РІРѕР·РІСЂР°С‚)
    /// </summary>
    IEnumerator AttackAnimation(Character attacker, Character target)
    {
        Vector3 startPosition = attacker.transform.position;
        Vector3 targetDirection = (target.transform.position - startPosition).normalized;
        Vector3 lungePosition = startPosition + targetDirection * lungeDistance;

        // Р¤Р°Р·Р° 1: РџСЂС‹Р¶РѕРє Рє РІСЂР°РіСѓ
        float elapsedTime = 0f;
        float lungeDuration = lungeDistance / lungeSpeed;

        while (elapsedTime < lungeDuration)
        {
            float t = elapsedTime / lungeDuration;
            float curveValue = lungeCurve.Evaluate(t);

            attacker.transform.position = Vector3.Lerp(startPosition, lungePosition, curveValue);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        attacker.transform.position = lungePosition;

        // РќРµР±РѕР»СЊС€Р°СЏ РїР°СѓР·Р° РІ С‚РѕС‡РєРµ Р°С‚Р°РєРё
        yield return new WaitForSeconds(GameConstants.Combat.ATTACK_PAUSE_DURATION);

        // Р¤Р°Р·Р° 2: Р’РѕР·РІСЂР°С‚ РЅР° РёСЃС…РѕРґРЅСѓСЋ РїРѕР·РёС†РёСЋ
        elapsedTime = 0f;

        while (elapsedTime < lungeDuration)
        {
            float t = elapsedTime / lungeDuration;
            float curveValue = lungeCurve.Evaluate(t);

            attacker.transform.position = Vector3.Lerp(lungePosition, startPosition, curveValue);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        attacker.transform.position = startPosition;
    }

    /// <summary>
    /// РџРѕРІРµСЂРЅСѓС‚СЊ РїРµСЂСЃРѕРЅР°Р¶Р° Р»РёС†РѕРј Рє С†РµР»Рё
    /// </summary>
    IEnumerator RotateTowardsTarget(Character attacker, Character target)
    {
        if (attacker == null || target == null)
            yield break;

        Vector3 direction = (target.transform.position - attacker.transform.position).normalized;

        // РЈР±РёСЂР°РµРј РєРѕРјРїРѕРЅРµРЅС‚ Y РґР»СЏ РїРѕРІРѕСЂРѕС‚Р° С‚РѕР»СЊРєРѕ РІ РіРѕСЂРёР·РѕРЅС‚Р°Р»СЊРЅРѕР№ РїР»РѕСЃРєРѕСЃС‚Рё
        direction.y = 0;

        if (direction == Vector3.zero)
            yield break;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        float rotationSpeed = GameConstants.Combat.ROTATION_SPEED_ATTACK;

        while (Quaternion.Angle(attacker.transform.rotation, targetRotation) > GameConstants.Combat.ROTATION_ANGLE_THRESHOLD)
        {
            attacker.transform.rotation = Quaternion.RotateTowards(
                attacker.transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
            yield return null;
        }

        // РЈСЃС‚Р°РЅР°РІР»РёРІР°РµРј С„РёРЅР°Р»СЊРЅРѕРµ РЅР°РїСЂР°РІР»РµРЅРёРµ
        attacker.transform.rotation = targetRotation;
    }

    /// <summary>
    /// РџРѕРєР°Р·Р°С‚СЊ РёРЅРґРёРєР°С†РёСЋ СѓСЂРѕРЅР° (РјРёРіР°РЅРёРµ РјР°С‚РµСЂРёР°Р»Р°)
    /// </summary>
    IEnumerator ShowDamageIndication(Character target)
    {
        if (target == null || damageIndicatorMaterial == null)
        {
            yield break;
        }

        // Р—Р°С‰РёС‚Р° РѕС‚ РѕРґРЅРѕРІСЂРµРјРµРЅРЅРѕРіРѕ РїСЂРёРјРµРЅРµРЅРёСЏ СѓСЂРѕРЅР° Рє РѕРґРЅРѕР№ С†РµР»Рё
        if (damageIndicationInProgress.Contains(target))
        {
            yield break;
        }

        damageIndicationInProgress.Add(target);

        // РџРѕР»СѓС‡Р°РµРј РІСЃРµ СЂРµРЅРґРµСЂРµСЂС‹ С†РµР»Рё
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();

        // РЎРѕС…СЂР°РЅСЏРµРј РѕСЂРёРіРёРЅР°Р»СЊРЅС‹Рµ РјР°С‚РµСЂРёР°Р»С‹ Рё СЃРѕР·РґР°РµРј РјР°СЃСЃРёРІС‹ СЃ РјР°С‚РµСЂРёР°Р»РѕРј СѓСЂРѕРЅР°
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null && renderer.sharedMaterials != null && renderer.sharedMaterials.Length > 0)
            {
                // РЎРѕС…СЂР°РЅСЏРµРј РІСЃРµ РѕСЂРёРіРёРЅР°Р»СЊРЅС‹Рµ РјР°С‚РµСЂРёР°Р»С‹
                originalMaterials[renderer] = (Material[])renderer.sharedMaterials.Clone();

                // РЎРѕР·РґР°РµРј РјР°СЃСЃРёРІ РјР°С‚РµСЂРёР°Р»РѕРІ СѓСЂРѕРЅР° (Р·Р°РјРµРЅСЏРµРј РІСЃРµ РјР°С‚РµСЂРёР°Р»С‹ РЅР° РјР°С‚РµСЂРёР°Р» СѓСЂРѕРЅР°)
                Material[] damageArray = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < damageArray.Length; i++)
                {
                    damageArray[i] = damageIndicatorMaterial;
                }

                // РџСЂРёРјРµРЅСЏРµРј РјР°С‚РµСЂРёР°Р»С‹ СѓСЂРѕРЅР°
                renderer.sharedMaterials = damageArray;
            }
        }

        // Р–РґРµРј СѓРєР°Р·Р°РЅРЅРѕРµ РІСЂРµРјСЏ
        yield return new WaitForSeconds(damageFlashDuration);

        // Р’РѕСЃСЃС‚Р°РЅР°РІР»РёРІР°РµРј РѕСЂРёРіРёРЅР°Р»СЊРЅС‹Рµ РјР°С‚РµСЂРёР°Р»С‹
        foreach (var kvp in originalMaterials)
        {
            if (kvp.Key != null && kvp.Value != null)
            {
                kvp.Key.sharedMaterials = kvp.Value;
            }
        }

        // РЈР±РёСЂР°РµРј Р±Р»РѕРєРёСЂРѕРІРєСѓ
        damageIndicationInProgress.Remove(target);
    }

    /// <summary>
    /// РџРѕРєР°Р·Р°С‚СЊ С‚РµРєСЃС‚ СѓСЂРѕРЅР° СЃ РїРѕРІРѕСЂРѕС‚РѕРј Рє РєР°РјРµСЂРµ
    /// </summary>
    void ShowDamageText(Character target, float damage)
    {
        if (target == null) return;

        // РЎРѕР·РґР°РµРј РѕР±СЉРµРєС‚ СЃ С‚РµРєСЃС‚РѕРј СѓСЂРѕРЅР°
        Vector3 damagePosition = target.transform.position + Vector3.up * GameConstants.Combat.DAMAGE_TEXT_HEIGHT_OFFSET;
        GameObject damageTextObj = LookAtCamera.CreateBillboardText(
            $"-{damage:F0}",
            damagePosition,
            Color.white,
            12
        );

        // РђРЅРёРјРёСЂСѓРµРј С‚РµРєСЃС‚ - РЎРўР РћР“Рћ 1 СЃРµРєСѓРЅРґР°
        Coroutine animationCoroutine = StartCoroutine(AnimateLegacyDamageText(damageTextObj, 1.0f));

        // Р РµРіРёСЃС‚СЂРёСЂСѓРµРј РІ РјРµРЅРµРґР¶РµСЂРµ РґР»СЏ РѕС‚СЃР»РµР¶РёРІР°РЅРёСЏ
        DamageTextManager.Instance.RegisterDamageText(damageTextObj, animationCoroutine);

        // РџР РРќРЈР”РРўР•Р›Р¬РќРћР• СѓРЅРёС‡С‚РѕР¶РµРЅРёРµ СЂРѕРІРЅРѕ С‡РµСЂРµР· 1 СЃРµРєСѓРЅРґСѓ
        StartCoroutine(ForceCleanupAfterDelay(damageTextObj, 1.0f));
    }

    /// <summary>
    /// РђРЅРёРјР°С†РёСЏ С‚РµРєСЃС‚Р° СѓСЂРѕРЅР° РґР»СЏ СЃС‚Р°СЂРѕР№ СЃРёСЃС‚РµРјС‹ Р±РѕСЏ
    /// </summary>
    IEnumerator AnimateLegacyDamageText(GameObject damageTextObj, float duration)
    {
        TextMesh textMesh = damageTextObj.GetComponent<TextMesh>();
        Vector3 startPos = damageTextObj.transform.position;
        Vector3 endPos = new Vector3(startPos.x, GameConstants.Combat.DAMAGE_TEXT_END_HEIGHT, startPos.z);
        Color startColor = textMesh.color;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;

            // Р”РІРёР¶РµРЅРёРµ РІРІРµСЂС…
            damageTextObj.transform.position = Vector3.Lerp(startPos, endPos, t);

            // РџР»Р°РІРЅРѕРµ РёСЃС‡РµР·РЅРѕРІРµРЅРёРµ
            Color color = startColor;
            color.a = 1f - t;
            textMesh.color = color;

            // РЈРІРµР»РёС‡РµРЅРёРµ СЂР°Р·РјРµСЂР° РІ РЅР°С‡Р°Р»Рµ
            float scale = 1f + (0.3f * (1f - t));
            damageTextObj.transform.localScale = Vector3.one * scale;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // РџСЂРёРЅСѓРґРёС‚РµР»СЊРЅРѕ СѓРЅРёС‡С‚РѕР¶Р°РµРј С‡РµСЂРµР· РјРµРЅРµРґР¶РµСЂ
        if (damageTextObj != null)
        {
            DamageTextManager.Instance.ForceCleanupObject(damageTextObj);
        }
    }

    /// <summary>
    /// РџСЂРёРЅСѓРґРёС‚РµР»СЊРЅР°СЏ РѕС‡РёСЃС‚РєР° РѕР±СЉРµРєС‚Р° С‡РµСЂРµР· Р·Р°РґР°РЅРЅРѕРµ РІСЂРµРјСЏ
    /// </summary>
    IEnumerator ForceCleanupAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (obj != null)
        {
            DamageTextManager.Instance.ForceCleanupObject(obj);
        }
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ РµСЃС‚СЊ Р»Рё РїСЂСЏРјР°СЏ Р»РёРЅРёСЏ РІРёРґРёРјРѕСЃС‚Рё РјРµР¶РґСѓ Р°С‚Р°РєСѓСЋС‰РёРј Рё С†РµР»СЊСЋ
    /// </summary>
    /// <param name="attacker">РђС‚Р°РєСѓСЋС‰РёР№ РїРµСЂСЃРѕРЅР°Р¶</param>
    /// <param name="target">Р¦РµР»РµРІРѕР№ РїРµСЂСЃРѕРЅР°Р¶</param>
    /// <param name="blockedByAlly">OUT: true РµСЃР»Рё Р»РёРЅРёСЏ Р·Р°Р±Р»РѕРєРёСЂРѕРІР°РЅР° СЃРѕСЋР·РЅРёРєРѕРј (Р° РЅРµ СЃС‚РµРЅРѕР№)</param>
    bool HasClearLineOfSight(Character attacker, Character target, out bool blockedByAlly)
    {
        blockedByAlly = false;

        if (attacker == null || target == null)
            return false;

        // РџРѕР·РёС†РёСЏ РІС‹СЃС‚СЂРµР»Р° (РЅР° РІС‹СЃРѕС‚Рµ РїРµСЂСЃРѕРЅР°Р¶Р°)
        Vector3 shooterPos = attacker.transform.position + Vector3.up * GameConstants.Combat.SHOOTER_HEIGHT_OFFSET;
        Vector3 targetPos = target.transform.position + Vector3.up * GameConstants.Combat.SHOOTER_HEIGHT_OFFSET;

        Vector3 direction = targetPos - shooterPos;
        float distance = direction.magnitude;

        // Raycast РґР»СЏ РїСЂРѕРІРµСЂРєРё РїСЂРµРїСЏС‚СЃС‚РІРёР№
        RaycastHit hit;
        if (Physics.Raycast(shooterPos, direction.normalized, out hit, distance, lineOfSightBlockers))
        {
            // РџСЂРѕРІРµСЂСЏРµРј, РїРѕРїР°Р»Рё Р»Рё РјС‹ РёРјРµРЅРЅРѕ РІ С†РµР»СЊ РёР»Рё РІ РїСЂРµРїСЏС‚СЃС‚РІРёРµ
            Character hitCharacter = hit.collider.GetComponent<Character>();
            if (hitCharacter == null)
                hitCharacter = hit.collider.GetComponentInParent<Character>();

            if (hitCharacter == target)
            {
                // РџРѕРїР°Р»Рё РІ С†РµР»СЊ - Р»РёРЅРёСЏ РІРёРґРёРјРѕСЃС‚Рё С‡РёСЃС‚Р°СЏ
                return true;
            }
            else if (hitCharacter != null && hitCharacter.IsAllyWith(attacker))
            {
                // РџРѕРїР°Р»Рё РІ СЃРѕСЋР·РЅРёРєР° - Р»РёРЅРёСЏ Р·Р°Р±Р»РѕРєРёСЂРѕРІР°РЅР° СЃРѕСЋР·РЅРёРєРѕРј
                blockedByAlly = true;
                return false;
            }
            else
            {
                // РџРѕРїР°Р»Рё РІ РїСЂРµРїСЏС‚СЃС‚РІРёРµ (СЃС‚РµРЅСѓ/РїСЂРµРїСЏС‚СЃС‚РІРёРµ)
                blockedByAlly = false;
                return false;
            }
        }

        // РќРёС‡РµРіРѕ РЅРµ РїРѕРїР°Р»Рѕ - Р»РёРЅРёСЏ РІРёРґРёРјРѕСЃС‚Рё С‡РёСЃС‚Р°СЏ
        return true;
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ РµСЃС‚СЊ Р»Рё РїСЂСЏРјР°СЏ Р»РёРЅРёСЏ РІРёРґРёРјРѕСЃС‚Рё (СЃС‚Р°СЂС‹Р№ РјРµС‚РѕРґ РґР»СЏ СЃРѕРІРјРµСЃС‚РёРјРѕСЃС‚Рё)
    /// </summary>
    bool HasClearLineOfSight(Character attacker, Character target)
    {
        bool blockedByAlly;
        return HasClearLineOfSight(attacker, target, out blockedByAlly);
    }

    /// <summary>
    /// РќР°Р№С‚Рё РїРѕР·РёС†РёСЋ СЃ С‡РёСЃС‚РѕР№ Р»РёРЅРёРµР№ РІРёРґРёРјРѕСЃС‚Рё РґРѕ С†РµР»Рё
    /// </summary>
    Vector3? FindPositionWithClearShot(Character attacker, Character target, float searchRadius)
    {
        if (gridManager == null)
            return null;

        Vector2Int attackerGridPos = gridManager.WorldToGrid(attacker.transform.position);
        Vector2Int targetGridPos = gridManager.WorldToGrid(target.transform.position);

        // Р“РµРЅРµСЂРёСЂСѓРµРј РїРѕР·РёС†РёРё РїРѕ РєСЂСѓРіСѓ РІРѕРєСЂСѓРі С‚РµРєСѓС‰РµР№ РїРѕР·РёС†РёРё
        List<Vector2Int> searchPositions = new List<Vector2Int>();

        // РќР°С‡РёРЅР°РµРј СЃ С‚РµРєСѓС‰РµР№ РїРѕР·РёС†РёРё
        searchPositions.Add(attackerGridPos);

        // Р”РѕР±Р°РІР»СЏРµРј РїРѕР·РёС†РёРё РїРѕ СЂР°РґРёСѓСЃСѓ
        int radiusCells = Mathf.CeilToInt(searchRadius);
        for (int radius = 1; radius <= radiusCells; radius++)
        {
            for (int angle = 0; angle < 360; angle += GameConstants.Combat.LINE_OF_SIGHT_SEARCH_ANGLE_STEP)
            {
                float rad = angle * Mathf.Deg2Rad;
                int offsetX = Mathf.RoundToInt(Mathf.Cos(rad) * radius);
                int offsetY = Mathf.RoundToInt(Mathf.Sin(rad) * radius);

                Vector2Int searchPos = attackerGridPos + new Vector2Int(offsetX, offsetY);
                if (!searchPositions.Contains(searchPos))
                {
                    searchPositions.Add(searchPos);
                }
            }
        }

        // РџСЂРѕРІРµСЂСЏРµРј РєР°Р¶РґСѓСЋ РїРѕР·РёС†РёСЋ
        foreach (Vector2Int gridPos in searchPositions)
        {
            if (!gridManager.IsValidGridPosition(gridPos))
                continue;

            var cell = gridManager.GetCell(gridPos);
            if (cell != null && cell.isOccupied)
                continue; // Р—Р°РЅСЏС‚Рѕ

            // РџСЂРѕРІРµСЂСЏРµРј С‡С‚Рѕ РєР»РµС‚РєР° РЅРµ Р·Р°СЂРµР·РµСЂРІРёСЂРѕРІР°РЅР° РґСЂСѓРіРёРј РїРµСЂСЃРѕРЅР°Р¶РµРј
            if (reservedCombatPositions.ContainsKey(gridPos))
            {
                // Р•СЃР»Рё СЌС‚Рѕ РЅР°С€Р° СЃРѕР±СЃС‚РІРµРЅРЅР°СЏ СЂРµР·РµСЂРІР°С†РёСЏ - РјРѕР¶РµРј РёСЃРїРѕР»СЊР·РѕРІР°С‚СЊ
                if (attacker != null && reservedCombatPositions[gridPos] == attacker)
                {
                    // РџСЂРѕРІРµСЂСЏРµРј Р»РёРЅРёСЋ РІРёРґРёРјРѕСЃС‚Рё СЃ СЌС‚РѕР№ РїРѕР·РёС†РёРё
                    Vector3 worldPos = gridManager.GridToWorld(gridPos);
                    if (CheckLineOfSightFromPosition(worldPos, target))
                    {
                        return worldPos;
                    }
                }
                continue; // Р—Р°СЂРµР·РµСЂРІРёСЂРѕРІР°РЅР° РєРµРј-С‚Рѕ РґСЂСѓРіРёРј
            }

            Vector3 worldPos2 = gridManager.GridToWorld(gridPos);

            // РџСЂРѕРІРµСЂСЏРµРј Р»РёРЅРёСЋ РІРёРґРёРјРѕСЃС‚Рё СЃ СЌС‚РѕР№ РїРѕР·РёС†РёРё
            if (CheckLineOfSightFromPosition(worldPos2, target))
            {
                return worldPos2;
            }
        }

        return null;
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ Р»РёРЅРёСЋ РІРёРґРёРјРѕСЃС‚Рё СЃ Р·Р°РґР°РЅРЅРѕР№ РїРѕР·РёС†РёРё РґРѕ С†РµР»Рё
    /// </summary>
    bool CheckLineOfSightFromPosition(Vector3 position, Character target)
    {
        Vector3 shooterPos = position + Vector3.up * GameConstants.Combat.SHOOTER_HEIGHT_OFFSET;
        Vector3 targetPos = target.transform.position + Vector3.up * GameConstants.Combat.SHOOTER_HEIGHT_OFFSET;

        Vector3 direction = targetPos - shooterPos;
        float distance = direction.magnitude;

        RaycastHit hit;
        if (Physics.Raycast(shooterPos, direction.normalized, out hit, distance, lineOfSightBlockers))
        {
            Character hitCharacter = hit.collider.GetComponent<Character>();
            if (hitCharacter == null)
                hitCharacter = hit.collider.GetComponentInParent<Character>();

            if (hitCharacter == target)
            {
                // РџРѕРїР°Р»Рё РІ С†РµР»СЊ - Р»РёРЅРёСЏ РІРёРґРёРјРѕСЃС‚Рё С‡РёСЃС‚Р°СЏ
                return true;
            }
            else
            {
                // РџРѕРїР°Р»Рё РІ РїСЂРµРїСЏС‚СЃС‚РІРёРµ
                return false;
            }
        }

        // РќРµС‚ РїСЂРµРїСЏС‚СЃС‚РІРёР№ - Р»РёРЅРёСЏ РІРёРґРёРјРѕСЃС‚Рё С‡РёСЃС‚Р°СЏ
        return true;
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ Р±Р»РёР¶Р°Р№С€СѓСЋ РїРѕР·РёС†РёСЋ РґР»СЏ Р°С‚Р°РєРё С†РµР»Рё (СЃС‚Р°СЂС‹Р№ РјРµС‚РѕРґ РґР»СЏ СЃРѕРІРјРµСЃС‚РёРјРѕСЃС‚Рё)
    /// </summary>
    Vector3 GetNearestAttackPosition(Character target)
    {
        return GetNearestAttackPosition(null, target);
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ Р±Р»РёР¶Р°Р№С€СѓСЋ РїРѕР·РёС†РёСЋ РґР»СЏ Р°С‚Р°РєРё С†РµР»Рё СЃ СѓС‡РµС‚РѕРј СЂРµР·РµСЂРІР°С†РёР№
    /// </summary>
    Vector3 GetNearestAttackPosition(Character attacker, Character target)
    {
        if (gridManager == null)
        {
            // Fallback: РїРѕР·РёС†РёСЏ СЂСЏРґРѕРј СЃ С†РµР»СЊСЋ
            return target.transform.position + Vector3.right * attackRange;
        }

        Vector2Int attackerGridPos = attacker != null ? gridManager.WorldToGrid(attacker.transform.position) : Vector2Int.zero;
        Vector2Int targetGridPos = gridManager.WorldToGrid(target.transform.position);

        // РС‰РµРј СЃРІРѕР±РѕРґРЅСѓСЋ РєР»РµС‚РєСѓ СЂР°СЃС€РёСЂРµРЅРЅС‹Рј РїРѕРёСЃРєРѕРј (СЂР°РґРёСѓСЃ 1, РїРѕС‚РѕРј СЂР°РґРёСѓСЃ 2)
        // Р­С‚Рѕ РїРѕР·РІРѕР»СЏРµС‚ РЅР°Р№С‚Рё РїРѕР·РёС†РёРё РґР°Р¶Рµ РєРѕРіРґР° Р±Р»РёР¶Р°Р№С€РёРµ Р·Р°РЅСЏС‚С‹
        for (int searchRadius = 1; searchRadius <= 3; searchRadius++)
        {
            List<(Vector2Int offset, float distance)> offsetsWithDistance = new List<(Vector2Int, float)>();

            // Р“РµРЅРµСЂРёСЂСѓРµРј РІСЃРµ РїРѕР·РёС†РёРё РІ С‚РµРєСѓС‰РµРј СЂР°РґРёСѓСЃРµ
            for (int x = -searchRadius; x <= searchRadius; x++)
            {
                for (int y = -searchRadius; y <= searchRadius; y++)
                {
                    // РџСЂРѕРїСѓСЃРєР°РµРј РїРѕР·РёС†РёСЋ С†РµР»Рё Рё РІРЅСѓС‚СЂРµРЅРЅРёРµ СЂР°РґРёСѓСЃС‹ (СѓР¶Рµ РїСЂРѕРІРµСЂРµРЅС‹)
                    if (x == 0 && y == 0) continue;
                    if (searchRadius > 1 && Mathf.Abs(x) < searchRadius && Mathf.Abs(y) < searchRadius) continue;

                    Vector2Int offset = new Vector2Int(x, y);
                    Vector2Int attackGridPos = targetGridPos + offset;

                    float distance = attacker != null ? Vector2Int.Distance(attackerGridPos, attackGridPos) : 0f;
                    offsetsWithDistance.Add((offset, distance));
                }
            }

            // РЎРѕСЂС‚РёСЂСѓРµРј РїРѕ СЂР°СЃСЃС‚РѕСЏРЅРёСЋ РѕС‚ Р°С‚Р°РєСѓСЋС‰РµРіРѕ (Р±Р»РёР¶Р°Р№С€РёРµ РїРµСЂРІС‹Рµ)
            offsetsWithDistance.Sort((a, b) => a.distance.CompareTo(b.distance));

            // РџСЂРѕРІРµСЂСЏРµРј РїРѕР·РёС†РёРё РІ РїРѕСЂСЏРґРєРµ Р±Р»РёР·РѕСЃС‚Рё
            foreach (var (offset, _) in offsetsWithDistance)
            {
                Vector2Int attackGridPos = targetGridPos + offset;

                if (!gridManager.IsValidGridPosition(attackGridPos))
                    continue;

                var cell = gridManager.GetCell(attackGridPos);

                // РџСЂРѕРІРµСЂСЏРµРј С‡С‚Рѕ РєР»РµС‚РєР° РЅРµ Р·Р°РЅСЏС‚Р°
                if (cell != null && cell.isOccupied)
                    continue;

                // РџСЂРѕРІРµСЂСЏРµРј С‡С‚Рѕ РєР»РµС‚РєР° РЅРµ Р·Р°СЂРµР·РµСЂРІРёСЂРѕРІР°РЅР° РґСЂСѓРіРёРј РїРµСЂСЃРѕРЅР°Р¶РµРј
                if (reservedCombatPositions.ContainsKey(attackGridPos))
                {
                    // Р•СЃР»Рё СЌС‚Рѕ РЅР°С€Р° СЃРѕР±СЃС‚РІРµРЅРЅР°СЏ СЂРµР·РµСЂРІР°С†РёСЏ - РјРѕР¶РµРј РёСЃРїРѕР»СЊР·РѕРІР°С‚СЊ
                    if (attacker != null && reservedCombatPositions[attackGridPos] == attacker)
                    {
                        return gridManager.GridToWorld(attackGridPos);
                    }
                    continue; // Р—Р°СЂРµР·РµСЂРІРёСЂРѕРІР°РЅР° РєРµРј-С‚Рѕ РґСЂСѓРіРёРј
                }

                // РџСЂРѕРІРµСЂСЏРµРј С‡С‚Рѕ РїРѕР·РёС†РёСЏ РЅРµ РЅР° Р»РёРЅРёРё РѕРіРЅСЏ СЃРѕСЋР·РЅРёРєРѕРІ
                Vector3 worldPos = gridManager.GridToWorld(attackGridPos);
                if (IsPositionOnFireLine(worldPos, attacker))
                {
                    continue; // РџРѕР·РёС†РёСЏ РЅР° Р»РёРЅРёРё РѕРіРЅСЏ - РїСЂРѕРїСѓСЃРєР°РµРј
                }

                // РљР»РµС‚РєР° СЃРІРѕР±РѕРґРЅР° Рё Р±РµР·РѕРїР°СЃРЅР°!
                return worldPos;
            }
        }

        // Р•СЃР»Рё РґР°Р¶Рµ РїРѕСЃР»Рµ СЂР°СЃС€РёСЂРµРЅРЅРѕРіРѕ РїРѕРёСЃРєР° РЅРµ РЅР°С€Р»Рё СЃРІРѕР±РѕРґРЅСѓСЋ РєР»РµС‚РєСѓ,
        // РІРѕР·РІСЂР°С‰Р°РµРј Р±Р»РёР¶Р°Р№С€СѓСЋ Рє Р°С‚Р°РєСѓСЋС‰РµРјСѓ РїРѕР·РёС†РёСЋ (РґР°Р¶Рµ РµСЃР»Рё РѕРЅР° Р·Р°РЅСЏС‚Р°)
        // РЎРёСЃС‚РµРјР° РґРІРёР¶РµРЅРёСЏ СЃР°РјР° РѕР±СЂР°Р±РѕС‚Р°РµС‚ СЌС‚Рѕ
        Vector3 directionToTarget = (target.transform.position - (attacker != null ? attacker.transform.position : Vector3.zero)).normalized;
        Vector3 fallbackPos = target.transform.position - directionToTarget * 2f;
        return fallbackPos;
    }

    /// <summary>
    /// РћР±РЅРѕРІР»РµРЅРёРµ СЃРѕСЃС‚РѕСЏРЅРёР№ РІСЃРµС… СЃСЂР°Р¶Р°СЋС‰РёС…СЃСЏ РїРµСЂСЃРѕРЅР°Р¶РµР№
    /// </summary>
    void UpdateCombatStates()
    {
        List<Character> toRemove = new List<Character>();

        foreach (var kvp in activeCombatants)
        {
            Character attacker = kvp.Key;
            CombatData combatData = kvp.Value;

            // РџСЂРѕРІРµСЂСЏРµРј, С‡С‚Рѕ РїРµСЂСЃРѕРЅР°Р¶Рё РµС‰Рµ СЃСѓС‰РµСЃС‚РІСѓСЋС‚ Рё Р°С‚Р°РєСѓСЋС‰РёР№ РЅРµ РјРµСЂС‚РІ
            if (attacker == null || combatData.target == null || combatData.target.GetHealth() <= 0 ||
                combatData.target.IsDead() || attacker.IsDead())
            {
                toRemove.Add(attacker);
                continue;
            }

            // РџРѕР»СѓС‡Р°РµРј РјР°РєСЃРёРјР°Р»СЊРЅСѓСЋ РґР°Р»СЊРЅРѕСЃС‚СЊ РїСЂРµСЃР»РµРґРѕРІР°РЅРёСЏ РЅР° РѕСЃРЅРѕРІРµ РѕСЂСѓР¶РёСЏ РїРµСЂСЃРѕРЅР°Р¶Р°
            float maxPursuitDistance = pursuitRange; // Р”РµС„РѕР»С‚РЅРѕРµ Р·РЅР°С‡РµРЅРёРµ

            WeaponSystem weaponSystem = attacker.GetComponent<WeaponSystem>();
            if (weaponSystem != null)
            {
                // РџРѕР»СѓС‡Р°РµРј РІСЃРµ РѕСЂСѓР¶РёРµ РїРµСЂСЃРѕРЅР°Р¶Р°
                var weapons = weaponSystem.GetAllWeapons();
                float maxWeaponRange = 0f;

                // РќР°С…РѕРґРёРј РјР°РєСЃРёРјР°Р»СЊРЅСѓСЋ РґР°Р»СЊРЅРѕСЃС‚СЊ СЃСЂРµРґРё РІСЃРµРіРѕ РѕСЂСѓР¶РёСЏ
                foreach (var weapon in weapons)
                {
                    if (weapon.range > maxWeaponRange)
                    {
                        maxWeaponRange = weapon.range;
                    }
                }

                // РСЃРїРѕР»СЊР·СѓРµРј РјР°РєСЃРёРјР°Р»СЊРЅСѓСЋ РґР°Р»СЊРЅРѕСЃС‚СЊ РѕСЂСѓР¶РёСЏ + Р·Р°РїР°СЃ РґР»СЏ РїСЂРµСЃР»РµРґРѕРІР°РЅРёСЏ
                // РќРѕ РЅРµ РјРµРЅСЊС€Рµ СЃС‚Р°РЅРґР°СЂС‚РЅРѕРіРѕ pursuitRange
                if (maxWeaponRange > 0)
                {
                    maxPursuitDistance = Mathf.Max(pursuitRange, maxWeaponRange * GameConstants.Combat.PURSUIT_RANGE_MULTIPLIER);
                }
            }

            // РћРўРљР›Р®Р§Р•РќРћ: РќРµ РїСЂРѕРІРµСЂСЏРµРј РґРёСЃС‚Р°РЅС†РёСЋ - РµСЃР»Рё РёРіСЂРѕРє РґР°Р» РєРѕРјР°РЅРґСѓ Р°С‚Р°РєРѕРІР°С‚СЊ, РїРµСЂСЃРѕРЅР°Р¶ РґРѕР»Р¶РµРЅ РїСЂРµСЃР»РµРґРѕРІР°С‚СЊ С†РµР»СЊ РЅР° Р»СЋР±РѕРј СЂР°СЃСЃС‚РѕСЏРЅРёРё
            // float distance = Vector3.Distance(attacker.transform.position, combatData.target.transform.position);
            // if (distance > maxPursuitDistance)
            // {
            //     if (debugMode)
            //     {
            //         Debug.Log($"[CombatSystem] {attacker.GetFullName()} stopped pursuing {combatData.target.GetFullName()} - " +
            //                  $"distance {distance:F1} exceeds max pursuit range {maxPursuitDistance:F1}");
            //     }
            //     toRemove.Add(attacker);
            // }
        }

        // РЈРґР°Р»СЏРµРј Р·Р°РІРµСЂС€РµРЅРЅС‹Рµ Р±РѕРµРІС‹Рµ РґРµР№СЃС‚РІРёСЏ
        foreach (Character attacker in toRemove)
        {
            StopCombat(attacker);
        }
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ, СѓС‡Р°СЃС‚РІСѓРµС‚ Р»Рё РїРµСЂСЃРѕРЅР°Р¶ РІ Р±РѕСЋ
    /// </summary>
    public bool IsInCombat(Character character)
    {
        return activeCombatants.ContainsKey(character);
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ С†РµР»СЊ РїРµСЂСЃРѕРЅР°Р¶Р° РІ Р±РѕСЋ
    /// </summary>
    public Character GetCombatTarget(Character attacker)
    {
        if (activeCombatants.ContainsKey(attacker))
        {
            return activeCombatants[attacker].target;
        }
        return null;
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РёРЅС„РѕСЂРјР°С†РёСЋ Рѕ Р±РѕРµРІС‹С… РґРµР№СЃС‚РІРёСЏС… РґР»СЏ РѕС‚Р»Р°РґРєРё
    /// </summary>
    public void LogCombatInfo()
    {
        // РњРµС‚РѕРґ РѕСЃС‚Р°РІР»РµРЅ РґР»СЏ РѕР±СЂР°С‚РЅРѕР№ СЃРѕРІРјРµСЃС‚РёРјРѕСЃС‚Рё, РЅРѕ Р»РѕРіРё РѕС‚РєР»СЋС‡РµРЅС‹
    }

    /// <summary>
    /// Р—Р°РІРµСЂС€РµРЅРёРµ СЂР°Р±РѕС‚С‹ РјРµРЅРµРґР¶РµСЂР° Р±РѕСЏ
    /// </summary>
    protected override void OnManagerShutdown()
    {
        // РћСЃС‚Р°РЅР°РІР»РёРІР°РµРј РІСЃРµ Р±РѕРµРІС‹Рµ РґРµР№СЃС‚РІРёСЏ
        List<Character> allCombatants = new List<Character>(activeCombatants.Keys);
        foreach (Character attacker in allCombatants)
        {
            // РџСЂРѕРІРµСЂСЏРµРј С‡С‚Рѕ РїРµСЂСЃРѕРЅР°Р¶ РµС‰Рµ СЃСѓС‰РµСЃС‚РІСѓРµС‚ РїРµСЂРµРґ РѕСЃС‚Р°РЅРѕРІРєРѕР№ Р±РѕСЏ
            if (attacker != null)
            {
                StopCombat(attacker);
            }
        }

        activeCombatants.Clear();

        // РћС‡РёС‰Р°РµРј РІСЃРµ СЂРµР·РµСЂРІР°С†РёРё
        reservedCombatPositions.Clear();

        base.OnManagerShutdown();
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ, РЅР°С…РѕРґРёС‚СЃСЏ Р»Рё РїРѕР·РёС†РёСЏ РЅР° Р»РёРЅРёРё РѕРіРЅСЏ СЃРѕСЋР·РЅРёРєРѕРІ
    /// </summary>
    bool IsPositionOnFireLine(Vector3 checkPosition, Character excludeAttacker)
    {
        if (gridManager == null) return false;

        // РџСЂРѕРІРµСЂСЏРµРј РІСЃРµ Р°РєС‚РёРІРЅС‹Рµ Р±РѕРµРІС‹Рµ РґРµР№СЃС‚РІРёСЏ
        foreach (var kvp in activeCombatants)
        {
            Character shooter = kvp.Key;
            Character target = kvp.Value.target;

            // РџСЂРѕРїСѓСЃРєР°РµРј РїСЂРѕРІРµСЂСЏРµРјРѕРіРѕ РїРµСЂСЃРѕРЅР°Р¶Р°
            if (shooter == excludeAttacker || shooter == null || target == null)
                continue;

            // РџСЂРѕРІРµСЂСЏРµРј С‚РѕР»СЊРєРѕ СЃРѕСЋР·РЅРёРєРѕРІ
            if (excludeAttacker != null)
            {
                bool shooterIsPlayer = shooter.IsPlayerCharacter();
                bool excludeIsPlayer = excludeAttacker.IsPlayerCharacter();

                // РџСЂРѕРІРµСЂСЏРµРј С‚РѕР»СЊРєРѕ СЃРѕСЋР·РЅРёРєРѕРІ (РѕР±Р° РёРіСЂРѕРєРё РёР»Рё РѕР±Р° РІСЂР°РіРё)
                if (shooterIsPlayer != excludeIsPlayer)
                    continue;
            }

            // РџРѕР»СѓС‡Р°РµРј Р»РёРЅРёСЋ РѕРіРЅСЏ
            Vector3 shooterPos = shooter.transform.position + Vector3.up * GameConstants.Combat.SHOOTER_HEIGHT_OFFSET;
            Vector3 targetPos = target.transform.position + Vector3.up * GameConstants.Combat.SHOOTER_HEIGHT_OFFSET;
            Vector3 checkPos = checkPosition + Vector3.up * GameConstants.Combat.SHOOTER_HEIGHT_OFFSET;

            // РџСЂРѕРІРµСЂСЏРµРј, РЅР°С…РѕРґРёС‚СЃСЏ Р»Рё checkPosition Р±Р»РёР·РєРѕ Рє Р»РёРЅРёРё РѕРіРЅСЏ
            Vector3 lineDir = (targetPos - shooterPos).normalized;
            float distanceAlongLine = Vector3.Dot(checkPos - shooterPos, lineDir);

            // РџСЂРѕРІРµСЂСЏРµРј С‚РѕР»СЊРєРѕ РµСЃР»Рё РїРѕР·РёС†РёСЏ РјРµР¶РґСѓ СЃС‚СЂРµР»РєРѕРј Рё С†РµР»СЊСЋ
            float lineLength = Vector3.Distance(shooterPos, targetPos);
            if (distanceAlongLine > 0 && distanceAlongLine < lineLength)
            {
                // Р’С‹С‡РёСЃР»СЏРµРј Р±Р»РёР¶Р°Р№С€СѓСЋ С‚РѕС‡РєСѓ РЅР° Р»РёРЅРёРё
                Vector3 closestPoint = shooterPos + lineDir * distanceAlongLine;
                float distanceToLine = Vector3.Distance(checkPos, closestPoint);

                // Р•СЃР»Рё Р±Р»РёР¶Рµ С‡РµРј 1.5 РјРµС‚СЂР° Рє Р»РёРЅРёРё РѕРіРЅСЏ - РЅРµР±РµР·РѕРїР°СЃРЅРѕ
                if (distanceToLine < 1.5f)
                {
                    return true;
                }
            }
        }

        return false;
    }

    void OnDrawGizmosSelected()
    {
        // РџРѕРєР°Р·С‹РІР°РµРј РґРёСЃС‚Р°РЅС†РёРё Р°С‚Р°РєРё Рё РїСЂРµСЃР»РµРґРѕРІР°РЅРёСЏ РІ Scene view
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pursuitRange);

        // РџРѕРєР°Р·С‹РІР°РµРј Р»РёРЅРёРё Рє С†РµР»СЏРј СЃ СѓС‡РµС‚РѕРј Р»РёРЅРёРё РІРёРґРёРјРѕСЃС‚Рё
        foreach (var kvp in activeCombatants)
        {
            Character attacker = kvp.Key;
            Character target = kvp.Value.target;

            if (attacker != null && target != null)
            {
                Vector3 shooterPos = attacker.transform.position + Vector3.up * GameConstants.Combat.SHOOTER_HEIGHT_OFFSET;
                Vector3 targetPos = target.transform.position + Vector3.up * GameConstants.Combat.SHOOTER_HEIGHT_OFFSET;

                // РџСЂРѕРІРµСЂСЏРµРј Р»РёРЅРёСЋ РІРёРґРёРјРѕСЃС‚Рё
                bool clearShot = HasClearLineOfSight(attacker, target);

                // Р¦РІРµС‚ Р»РёРЅРёРё Р·Р°РІРёСЃРёС‚ РѕС‚ СЃРѕСЃС‚РѕСЏРЅРёСЏ
                if (kvp.Value.isAttacking)
                {
                    Gizmos.color = Color.red; // РђС‚Р°РєСѓРµС‚
                }
                else if (clearShot)
                {
                    Gizmos.color = Color.green; // Р§РёСЃС‚Р°СЏ Р»РёРЅРёСЏ РІРёРґРёРјРѕСЃС‚Рё
                }
                else
                {
                    Gizmos.color = Color.yellow; // Р—Р°Р±Р»РѕРєРёСЂРѕРІР°РЅРѕ
                }

                Gizmos.DrawLine(shooterPos, targetPos);

                // Р РёСЃСѓРµРј С‚РѕС‡РєРё РЅР° РїРѕР·РёС†РёСЏС… СЃС‚СЂРµР»РєР° Рё С†РµР»Рё
                Gizmos.DrawWireSphere(shooterPos, 0.2f);
                Gizmos.DrawWireSphere(targetPos, 0.2f);
            }
        }
    }
}
