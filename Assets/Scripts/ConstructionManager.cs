using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// РњРµРЅРµРґР¶РµСЂ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР° - СѓРїСЂР°РІР»СЏРµС‚ РїСЂРѕС†РµСЃСЃРѕРј СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР° Р±Р»РѕРєРѕРІ РєРѕСЂР°Р±Р»СЏ РїРµСЂСЃРѕРЅР°Р¶Р°РјРё
/// ARCHITECTURE: РќР°СЃР»РµРґСѓРµС‚СЃСЏ РѕС‚ BaseManager РґР»СЏ РёРЅС‚РµРіСЂР°С†РёРё СЃ ServiceLocator
/// </summary>
public class ConstructionManager : BaseManager
{
    private static ConstructionManager instance;

    public static ConstructionManager Instance
    {
        get
        {
            if (instance == null)
            {
                // РЎРѕР·РґР°РµРј GameObject СЃ РєРѕРјРїРѕРЅРµРЅС‚РѕРј ConstructionManager
                GameObject go = new GameObject("ConstructionManager");
                instance = go.AddComponent<ConstructionManager>();
                DontDestroyOnLoad(go); // РќРµ СѓРЅРёС‡С‚РѕР¶Р°С‚СЊ РїСЂРё Р·Р°РіСЂСѓР·РєРµ РЅРѕРІРѕР№ СЃС†РµРЅС‹

            }
            return instance;
        }
    }

    // РљРµС€РёСЂРѕРІР°РЅРЅР°СЏ СЃСЃС‹Р»РєР° РЅР° GridManager РґР»СЏ РѕРїС‚РёРјРёР·Р°С†РёРё
    private GridManager gridManager;

    [Header("Construction Settings")]
    [Tooltip("РљРѕР»РёС‡РµСЃС‚РІРѕ РїСЂС‹Р¶РєРѕРІ РґР»СЏ РїРѕСЃС‚СЂРѕР№РєРё РѕРґРЅРѕРіРѕ Р±Р»РѕРєР°")]
    public int jumpsRequired = 5;

    [Tooltip("Р’СЂРµРјСЏ РјРµР¶РґСѓ РїСЂС‹Р¶РєР°РјРё РІ СЃРµРєСѓРЅРґР°С…")]
    public float jumpInterval = 0.5f;

    [Tooltip("Р’С‹СЃРѕС‚Р° РїСЂС‹Р¶РєР°")]
    public float jumpHeight = 0.5f;

    [Tooltip("Р”РёСЃС‚Р°РЅС†РёСЏ РґРѕ Р±Р»РѕРєР° РґР»СЏ РЅР°С‡Р°Р»Р° СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР° (1 РєР»РµС‚РєР° - РїРµСЂСЃРѕРЅР°Р¶ РґРѕР»Р¶РµРЅ Р±С‹С‚СЊ РЅР° СЃРѕСЃРµРґРЅРµР№ РєР»РµС‚РєРµ)")]
    public float constructionRange = 10.5f; // Р Р°Р·РјРµСЂ РєР»РµС‚РєРё 10f, СЃРѕСЃРµРґРЅСЏСЏ РєР»РµС‚РєР° = 10f, Р±РµСЂРµРј С‡СѓС‚СЊ Р±РѕР»СЊС€Рµ РґР»СЏ РїРѕРіСЂРµС€РЅРѕСЃС‚Рё

    [Header("Progress Bar Settings")]
    [Tooltip("Р’С‹СЃРѕС‚Р° РїРѕР»РѕСЃС‹ РїСЂРѕРіСЂРµСЃСЃР° РЅР°Рґ Р±Р»РѕРєРѕРј")]
    public float progressBarHeight = 2.5f;

    [Tooltip("РЁРёСЂРёРЅР° РїРѕР»РѕСЃС‹ РїСЂРѕРіСЂРµСЃСЃР°")]
    public float progressBarWidth = 12f;

    [Tooltip("Р’С‹СЃРѕС‚Р° РїРѕР»РѕСЃС‹ РїСЂРѕРіСЂРµСЃСЃР°")]
    public float progressBarThickness = 2f;

    [Tooltip("РњР°СЃС€С‚Р°Р± Canvas РґР»СЏ РїРѕР»РѕСЃС‹ РїСЂРѕРіСЂРµСЃСЃР°")]
    public float progressBarScale = 0.12f; // РЈРјРµРЅСЊС€РµРЅРѕ РЅР° 40% (0.2 * 0.6 = 0.12)

    [Tooltip("Р¦РІРµС‚ С„РѕРЅР° РїРѕР»РѕСЃС‹")]
    public Color progressBarBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);

    [Tooltip("Р¦РІРµС‚ Р·Р°РїРѕР»РЅРµРЅРёСЏ РїРѕР»РѕСЃС‹")]
    public Color progressBarFillColor = new Color(0.9f, 0.2f, 0.2f, 0.9f);

    // РЎРїРёСЃРѕРє Р±Р»РѕРєРѕРІ РґР»СЏ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
    private List<ConstructionBlock> constructionQueue = new List<ConstructionBlock>();

    // РџРµСЂСЃРѕРЅР°Р¶Рё Р·Р°РЅСЏС‚С‹Рµ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІРѕРј
    private Dictionary<Character, ConstructionBlock> busyCharacters = new Dictionary<Character, ConstructionBlock>();

    // РљРћР РЈРўРРќР« РЎРўР РћРРўР•Р›Р¬РЎРўР’Рђ РґР»СЏ РєР°Р¶РґРѕРіРѕ РїРµСЂСЃРѕРЅР°Р¶Р° (РґР»СЏ РѕСЃС‚Р°РЅРѕРІРєРё)
    private Dictionary<Character, Coroutine> constructionCoroutines = new Dictionary<Character, Coroutine>();

    // РџРѕР»РѕСЃС‹ РїСЂРѕРіСЂРµСЃСЃР° РґР»СЏ Р±Р»РѕРєРѕРІ
    private Dictionary<ConstructionBlock, GameObject> progressBars = new Dictionary<ConstructionBlock, GameObject>();

    /// <summary>
    /// РРЅРёС†РёР°Р»РёР·Р°С†РёСЏ РјРµРЅРµРґР¶РµСЂР° СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР° С‡РµСЂРµР· ServiceLocator
    /// </summary>
    protected override void OnManagerInitialized()
    {
        base.OnManagerInitialized();

        // Singleton pattern
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // РџРѕР»СѓС‡Р°РµРј GridManager С‡РµСЂРµР· ServiceLocator
        gridManager = GetService<GridManager>();
        if (gridManager == null)
        {
            LogError("GridManager not found! Construction system will not work properly.");
        }
    }

    /// <summary>
    /// Р”РѕР±Р°РІРёС‚СЊ Р±Р»РѕРєРё РґР»СЏ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
    /// </summary>
    public void AddConstructionBlocks(List<ConstructionBlock> blocks)
    {
        constructionQueue.AddRange(blocks);
        // РќР• РЅР°Р·РЅР°С‡Р°РµРј СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІРѕ СЃСЂР°Р·Сѓ РІСЃРµРј РїРµСЂСЃРѕРЅР°Р¶Р°Рј!
        // РџРµСЂСЃРѕРЅР°Р¶Рё Р±СѓРґСѓС‚ Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРё Р·Р°РїСЂР°С€РёРІР°С‚СЊ Р·Р°РґР°С‡Рё РєРѕРіРґР° РїРµСЂРµР№РґСѓС‚ РІ СЃРѕСЃС‚РѕСЏРЅРёРµ Idle
    }

    /// <summary>
    /// РќР°Р·РЅР°С‡РёС‚СЊ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІРѕ РІСЃРµРј РїРµСЂСЃРѕРЅР°Р¶Р°Рј РёРіСЂРѕРєР° РЅР° РєР°СЂС‚Рµ
    /// </summary>
    void AssignConstructionToAllCharacters()
    {
        // РќР°С…РѕРґРёРј РІСЃРµС… РїРµСЂСЃРѕРЅР°Р¶РµР№ РёРіСЂРѕРєР°
        Character[] allCharacters = FindObjectsOfType<Character>();
        foreach (Character character in allCharacters)
        {
            if (character.IsPlayerCharacter() && !character.IsDead())
            {
                // Р•СЃР»Рё РїРµСЂСЃРѕРЅР°Р¶ РЅРµ Р·Р°РЅСЏС‚ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІРѕРј, РЅР°Р·РЅР°С‡Р°РµРј РµРјСѓ Р±Р»РѕРє
                if (!busyCharacters.ContainsKey(character))
                {
                    AssignConstructionTask(character);
                }
            }
        }
    }

    /// <summary>
    /// РџРЈР‘Р›РР§РќР«Р™ РњР•РўРћР”: РџРѕРїС‹С‚РєР° РЅР°Р·РЅР°С‡РёС‚СЊ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІРѕ РїРµСЂСЃРѕРЅР°Р¶Сѓ (РІС‹Р·С‹РІР°РµС‚СЃСЏ РёР· CharacterAI)
    /// РќР°Р·РЅР°С‡Р°РµС‚ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІРѕ РўРћР›Р¬РљРћ РµСЃР»Рё РїРµСЂСЃРѕРЅР°Р¶ РІ СЃРѕСЃС‚РѕСЏРЅРёРё Idle
    /// </summary>
    public void TryAssignConstructionToIdleCharacter(Character character)
    {
        // РџСЂРѕРІРµСЂРєР° 1: РџРµСЂСЃРѕРЅР°Р¶ РґРѕР»Р¶РµРЅ Р±С‹С‚СЊ РёРіСЂРѕРєРѕРј
        if (!character.IsPlayerCharacter())
        {
            return;
        }

        // РџСЂРѕРІРµСЂРєР° 2: РџРµСЂСЃРѕРЅР°Р¶ РЅРµ РґРѕР»Р¶РµРЅ Р±С‹С‚СЊ РјРµСЂС‚РІ
        if (character.IsDead())
        {
            return;
        }

        // РџСЂРѕРІРµСЂРєР° 3: РџРµСЂСЃРѕРЅР°Р¶ РЅРµ РґРѕР»Р¶РµРЅ Р±С‹С‚СЊ СѓР¶Рµ Р·Р°РЅСЏС‚ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІРѕРј
        if (busyCharacters.ContainsKey(character))
        {
            return;
        }

        // РџСЂРѕРІРµСЂРєР° 4: Р”РѕР»Р¶РЅС‹ Р±С‹С‚СЊ РґРѕСЃС‚СѓРїРЅС‹Рµ Р±Р»РѕРєРё РґР»СЏ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
        if (constructionQueue.Count == 0)
        {
            return;
        }

        // РџСЂРѕРІРµСЂРєР° 5: РџРµСЂСЃРѕРЅР°Р¶ РґРѕР»Р¶РµРЅ Р±С‹С‚СЊ РІ СЃРѕСЃС‚РѕСЏРЅРёРё Idle
        CharacterAI characterAI = character.GetComponent<CharacterAI>();
        if (characterAI == null || characterAI.GetCurrentState() != CharacterAI.AIState.Idle)
        {
            return;
        }

        // Р’РЎР• РџР РћР’Р•Р РљР РџР РћРЁР›Р - РЅР°Р·РЅР°С‡Р°РµРј СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІРѕ
        AssignConstructionTask(character);
    }

    /// <summary>
    /// РќР°Р·РЅР°С‡РёС‚СЊ Р·Р°РґР°С‡Сѓ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР° РїРµСЂСЃРѕРЅР°Р¶Сѓ
    /// </summary>
    void AssignConstructionTask(Character character)
    {
        // РџСЂРѕРІРµСЂСЏРµРј С‡С‚Рѕ РїРµСЂСЃРѕРЅР°Р¶ РµС‰Рµ РЅРµ Р·Р°РЅСЏС‚ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІРѕРј
        if (busyCharacters.ContainsKey(character))
        {
            return;
        }

        // РќР°С…РѕРґРёРј Р±Р»РёР¶Р°Р№С€РёР№ СЃРІРѕР±РѕРґРЅС‹Р№ Р±Р»РѕРє
        ConstructionBlock nearestBlock = FindNearestAvailableBlock(character.transform.position);

        if (nearestBlock != null)
        {
            // Р”РІРѕР№РЅР°СЏ РїСЂРѕРІРµСЂРєР° - Р±Р»РѕРє РґРµР№СЃС‚РІРёС‚РµР»СЊРЅРѕ СЃРІРѕР±РѕРґРµРЅ?
            if (nearestBlock.isAssigned)
            {
                return;
            }

            // РџРѕРјРµС‡Р°РµРј Р±Р»РѕРє РєР°Рє Р·Р°РЅСЏС‚С‹Р№ РЎР РђР—РЈ
            nearestBlock.isAssigned = true;
            busyCharacters[character] = nearestBlock;

            // Р—Р°РїСѓСЃРєР°РµРј РїСЂРѕС†РµСЃСЃ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР° Р РЎРћРҐР РђРќРЇР•Рњ РЎРЎР«Р›РљРЈ РќРђ РљРћР РЈРўРРќРЈ
            Coroutine coroutine = StartCoroutine(ConstructBlock(character, nearestBlock));
            constructionCoroutines[character] = coroutine;
        }
    }

    /// <summary>
    /// РќР°Р№С‚Рё Р±Р»РёР¶Р°Р№С€РёР№ СЃРІРѕР±РѕРґРЅС‹Р№ Р±Р»РѕРє РґР»СЏ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
    /// </summary>
    ConstructionBlock FindNearestAvailableBlock(Vector3 position)
    {
        ConstructionBlock nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (ConstructionBlock block in constructionQueue)
        {
            if (!block.isAssigned && !block.isCompleted)
            {
                float distance = Vector3.Distance(position, block.worldPosition);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = block;
                }
            }
        }

        return nearest;
    }

    /// <summary>
    /// РќР°Р№С‚Рё Р±Р»РёР¶Р°Р№С€СѓСЋ СЃРІРѕР±РѕРґРЅСѓСЋ РїРѕР·РёС†РёСЋ СЂСЏРґРѕРј СЃ Р±Р»РѕРєРѕРј РґР»СЏ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
    /// </summary>
    Vector3? FindNearestValidConstructionPosition(Vector3 characterPosition, ConstructionBlock block)
    {
        if (gridManager == null)
        {
            LogError("GridManager not available, using default adjacent position");
            return block.worldPosition + new Vector3(10f, 0, 0); // 10f - СЂР°Р·РјРµСЂ РєР»РµС‚РєРё РїРѕ СѓРјРѕР»С‡Р°РЅРёСЋ
        }

        // РџСЂРѕРІРµСЂСЏРµРј 4 СЃРѕСЃРµРґРЅРёРµ РєР»РµС‚РєРё (РІРµСЂС…, РЅРёР·, Р»РµРІРѕ, РїСЂР°РІРѕ)
        Vector2Int[] adjacentOffsets = new Vector2Int[]
        {
            new Vector2Int(0, 1),   // РІРµСЂС…
            new Vector2Int(0, -1),  // РЅРёР·
            new Vector2Int(-1, 0),  // Р»РµРІРѕ
            new Vector2Int(1, 0)    // РїСЂР°РІРѕ
        };

        Vector3? nearestPosition = null;
        Vector2Int? nearestGridPos = null;
        float nearestDistance = float.MaxValue;

        foreach (Vector2Int offset in adjacentOffsets)
        {
            Vector2Int adjacentGridPos = block.gridPosition + offset;

            // РџСЂРѕРІРµСЂСЏРµРј С‡С‚Рѕ РєР»РµС‚РєР° РІР°Р»РёРґРЅР° Рё РїСЂРѕС…РѕРґРёРјР°
            if (gridManager.IsValidGridPosition(adjacentGridPos))
            {
                GridCell cell = gridManager.GetCell(adjacentGridPos);
                bool isOccupied = (cell != null && cell.isOccupied);

                // РџР РћР’Р•Р РЇР•Рњ: РЅРµС‚ Р»Рё РЅР° СЌС‚РѕР№ РєР»РµС‚РєРµ Р±Р»РѕРєР° РЎРўР•РќР« РІ РѕС‡РµСЂРµРґРё СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
                bool hasWallInQueue = IsWallBlockInQueue(adjacentGridPos);

                // РљР»РµС‚РєР° РґРѕР»Р¶РЅР° Р±С‹С‚СЊ СЃРІРѕР±РѕРґРЅР° Р РЅР° РЅРµР№ РЅРµ РґРѕР»Р¶РЅРѕ Р±С‹С‚СЊ СЃС‚РµРЅС‹ РІ РѕС‡РµСЂРµРґРё СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
                if (cell != null && !cell.isOccupied && !hasWallInQueue)
                {
                    Vector3 worldPos = gridManager.GridToWorld(adjacentGridPos);
                    float distance = Vector3.Distance(characterPosition, worldPos);

                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestPosition = worldPos;
                        nearestGridPos = adjacentGridPos;
                    }
                }
            }
        }

        if (!nearestPosition.HasValue)
        {
        }

        return nearestPosition;
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ, РµСЃС‚СЊ Р»Рё Р±Р»РѕРє СЃС‚РµРЅС‹ РІ РѕС‡РµСЂРµРґРё СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР° РЅР° РґР°РЅРЅРѕР№ РєР»РµС‚РєРµ
    /// </summary>
    bool IsWallBlockInQueue(Vector2Int gridPosition)
    {
        foreach (ConstructionBlock queueBlock in constructionQueue)
        {
            if (queueBlock.gridPosition == gridPosition &&
                queueBlock.blockType == ConstructionBlock.BlockType.Wall &&
                !queueBlock.isCompleted)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// РџРѕРІРµСЂРЅСѓС‚СЊ РїРµСЂСЃРѕРЅР°Р¶Р° Р»РёС†РѕРј Рє Р±Р»РѕРєСѓ (РїРѕ 8 РЅР°РїСЂР°РІР»РµРЅРёСЏРј: РїСЂСЏРјС‹Рµ Рё РґРёР°РіРѕРЅР°Р»Рё)
    /// </summary>
    void RotateCharacterTowardsBlock(Character character, ConstructionBlock block)
    {
        // Р’С‹С‡РёСЃР»СЏРµРј РЅР°РїСЂР°РІР»РµРЅРёРµ РѕС‚ РїРµСЂСЃРѕРЅР°Р¶Р° Рє Р±Р»РѕРєСѓ
        Vector3 direction = block.worldPosition - character.transform.position;
        direction.y = 0; // РРіРЅРѕСЂРёСЂСѓРµРј РІРµСЂС‚РёРєР°Р»СЊРЅСѓСЋ СЃРѕСЃС‚Р°РІР»СЏСЋС‰СѓСЋ

        if (direction.magnitude < 0.1f)
        {
            // РџРµСЂСЃРѕРЅР°Р¶ СѓР¶Рµ РЅР° С‚РѕРј Р¶Рµ РјРµСЃС‚Рµ С‡С‚Рѕ Рё Р±Р»РѕРє - РЅРµ РїРѕРІРѕСЂР°С‡РёРІР°РµРј
            return;
        }

        // Р’С‹С‡РёСЃР»СЏРµРј СѓРіРѕР» РІ РіСЂР°РґСѓСЃР°С…
        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        // РќРѕСЂРјР°Р»РёР·СѓРµРј СѓРіРѕР» Рє РґРёР°РїР°Р·РѕРЅСѓ [0, 360)
        if (angle < 0) angle += 360f;

        // РћРєСЂСѓРіР»СЏРµРј РґРѕ Р±Р»РёР¶Р°Р№С€РµРіРѕ РёР· 8 РЅР°РїСЂР°РІР»РµРЅРёР№ (0В°, 45В°, 90В°, 135В°, 180В°, 225В°, 270В°, 315В°)
        float snappedAngle = Mathf.Round(angle / 45f) * 45f;

        // РџСЂРёРјРµРЅСЏРµРј РїРѕРІРѕСЂРѕС‚
        character.transform.rotation = Quaternion.Euler(0, snappedAngle, 0);
    }

    /// <summary>
    /// РџСЂРѕС†РµСЃСЃ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР° Р±Р»РѕРєР° РїРµСЂСЃРѕРЅР°Р¶РµРј
    /// </summary>
    IEnumerator ConstructBlock(Character character, ConstructionBlock block)
    {
        // 1. РћС‚РїСЂР°РІР»СЏРµРј РїРµСЂСЃРѕРЅР°Р¶Р° Рє Р±Р»РѕРєСѓ
        CharacterMovement movement = character.GetComponent<CharacterMovement>();
        if (movement == null)
        {
            yield break;
        }

        // РџР•Р Р•РљР›Р®Р§РђР•Рњ AI РїРµСЂСЃРѕРЅР°Р¶Р° РІ СЃРѕСЃС‚РѕСЏРЅРёРµ Working (СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІРѕ)
        CharacterAI characterAI = character.GetComponent<CharacterAI>();
        if (characterAI != null)
        {
            // FIX: Р"РІРёР³Р°РµРјСЃСЏ Рє СЂР°Р±РѕС‚Рµ - Р±Р»РѕРєРёСЂСѓРµРј РєРѕРЅС‚СЂР°С‚Р°РєСѓ
            characterAI.SetMovingToJob(true);
            characterAI.SetAIState(CharacterAI.AIState.Working);
        }

        // РџРѕР»СѓС‡Р°РµРј GridManager РґР»СЏ РїСЂРѕРІРµСЂРєРё РїРѕР·РёС†РёР№ РІ СЃРµС‚РєРµ
        Vector3 startPos = character.transform.position;
        if (gridManager == null)
        {
            LogError("GridManager not available!");
            OnConstructionFailed(character, block);
            yield break;
        }

        // Р•РЎР›Р Р‘Р›РћРљ - РЎРўР•РќРђ, РЎР РђР—РЈ РџРћРњР•Р§РђР•Рњ РљР›Р•РўРљРЈ РљРђРљ Р—РђРќРЇРўРЈР®
        // Р­С‚Рѕ РїСЂРµРґРѕС‚РІСЂР°С‚РёС‚ РїРѕРїР°РґР°РЅРёРµ РґСЂСѓРіРёС… РїРµСЂСЃРѕРЅР°Р¶РµР№ РІ РєР»РµС‚РєСѓ РІРѕ РІСЂРµРјСЏ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
        if (block.blockType == ConstructionBlock.BlockType.Wall)
        {
            GridCell wallCell = gridManager.GetCell(block.gridPosition);
            if (wallCell != null && !wallCell.isOccupied)
            {
                wallCell.isOccupied = true;
            }
        }

        // РџСЂРµРѕР±СЂР°Р·СѓРµРј РјРёСЂРѕРІСѓСЋ РїРѕР·РёС†РёСЋ РїРµСЂСЃРѕРЅР°Р¶Р° РІ РєРѕРѕСЂРґРёРЅР°С‚С‹ СЃРµС‚РєРё
        Vector2Int characterGridPos = gridManager.WorldToGrid(startPos);

        // РџСЂРѕРІРµСЂСЏРµРј, РЅР°С…РѕРґРёС‚СЃСЏ Р»Рё РїРµСЂСЃРѕРЅР°Р¶ РЈР–Р• РЅР° СЃРѕСЃРµРґРЅРµР№ РєР»РµС‚РєРµ
        int gridDistanceX = Mathf.Abs(characterGridPos.x - block.gridPosition.x);
        int gridDistanceY = Mathf.Abs(characterGridPos.y - block.gridPosition.y);
        int maxGridDistance = Mathf.Max(gridDistanceX, gridDistanceY);
        bool isAdjacentToBlock = (maxGridDistance == 1);

        // Р•СЃР»Рё РїРµСЂСЃРѕРЅР°Р¶ РЈР–Р• РЅР° СЃРѕСЃРµРґРЅРµР№ РєР»РµС‚РєРµ, РќР• РїРµСЂРµРјРµС‰Р°РµРј РµРіРѕ
        if (isAdjacentToBlock)
        {
            // РџСЂРѕРїСѓСЃРєР°РµРј РїРµСЂРµРјРµС‰РµРЅРёРµ, РёРґРµРј СЃСЂР°Р·Сѓ Рє Р°РЅРёРјР°С†РёРё СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
        }
        else
        {
            // РџРµСЂСЃРѕРЅР°Р¶ РќР• РЅР° СЃРѕСЃРµРґРЅРµР№ РєР»РµС‚РєРµ - РЅСѓР¶РЅРѕ РїРµСЂРµРјРµСЃС‚РёС‚СЊ РµРіРѕ
            // РќР°С…РѕРґРёРј Р±Р»РёР¶Р°Р№С€СѓСЋ СЃРІРѕР±РѕРґРЅСѓСЋ РєР»РµС‚РєСѓ СЂСЏРґРѕРј СЃ Р±Р»РѕРєРѕРј
            Vector3? targetPosition = FindNearestValidConstructionPosition(startPos, block);

            if (!targetPosition.HasValue)
            {
                OnConstructionFailed(character, block);
                yield break;
            }

            movement.MoveTo(targetPosition.Value);

            // Р–РґРµРј РїРѕРєР° РїРµСЂСЃРѕРЅР°Р¶ РёРґРµС‚
            yield return new WaitForSeconds(0.2f); // Р”Р°РµРј РІСЂРµРјСЏ РЅР°С‡Р°С‚СЊ РґРІРёР¶РµРЅРёРµ
            while (movement.IsMoving())
            {
                // РџСЂРѕРІРµСЂСЏРµРј С‡С‚Рѕ РїРµСЂСЃРѕРЅР°Р¶ РЅРµ СѓРјРµСЂ
                if (character.IsDead())
                {
                    OnConstructionFailed(character, block);
                    yield break;
                }

                // РџР РћР’Р•Р РЇР•Рњ РџР Р•Р Р«Р’РђРќРР• Р’Рћ Р’Р Р•РњРЇ Р”Р’РР–Р•РќРРЇ Рљ Р‘Р›РћРљРЈ
                if (characterAI != null && characterAI.GetCurrentState() != CharacterAI.AIState.Working)
                {
                    OnConstructionFailed(character, block);
                    yield break;
                }

                yield return null;
            }

            // 2. РџСЂРѕРІРµСЂСЏРµРј С‡С‚Рѕ РїРµСЂСЃРѕРЅР°Р¶ С‚РµРїРµСЂСЊ РЅР° СЃРѕСЃРµРґРЅРµР№ РєР»РµС‚РєРµ
            Vector3 finalPos = character.transform.position;
            Vector2Int finalGridPos = gridManager.WorldToGrid(finalPos);
            int finalGridDistanceX = Mathf.Abs(finalGridPos.x - block.gridPosition.x);
            int finalGridDistanceY = Mathf.Abs(finalGridPos.y - block.gridPosition.y);
            int finalMaxGridDistance = Mathf.Max(finalGridDistanceX, finalGridDistanceY);

            if (finalMaxGridDistance > 1)
            {
                OnConstructionFailed(character, block);
                yield break;
            }
        } // Р—Р°РєСЂС‹РІР°РµРј Р±Р»РѕРє else

        // РџРћР’РћР РђР§РР’РђР•Рњ РїРµСЂСЃРѕРЅР°Р¶Р° Р»РёС†РѕРј Рє Р±Р»РѕРєСѓ
        RotateCharacterTowardsBlock(character, block);

        // FIX: Р"РћРЁР›Р! РЎР±СЂР°СЃС‹РІР°РµРј С„Р»Р°Рі
        if (characterAI != null)
        {
            characterAI.SetMovingToJob(false);
        }

        // РЎРѕР·РґР°РµРј РїРѕР»РѕСЃСѓ РїСЂРѕРіСЂРµСЃСЃР° РґР»СЏ Р±Р»РѕРєР°
        CreateProgressBar(block);

        // 3. РџСЂС‹РіР°РµРј 5 СЂР°Р· (Р°РЅРёРјР°С†РёСЏ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°)
        for (int i = 0; i < jumpsRequired; i++)
        {
            // РџСЂРѕРІРµСЂСЏРµРј С‡С‚Рѕ РїРµСЂСЃРѕРЅР°Р¶ РЅРµ СѓРјРµСЂ
            if (character.IsDead())
            {
                OnConstructionFailed(character, block);
                yield break;
            }

            // РџР РћР’Р•Р РЇР•Рњ Р§РўРћ РџР•Р РЎРћРќРђР– Р’РЎР• Р•Р©Р• Р’ РЎРћРЎРўРћРЇРќРР Working (СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІРѕ РЅРµ РїСЂРµСЂРІР°РЅРѕ)
            if (characterAI != null && characterAI.GetCurrentState() != CharacterAI.AIState.Working)
            {
                OnConstructionFailed(character, block);
                yield break;
            }

            // РџСЂС‹Р¶РѕРє
            yield return StartCoroutine(Jump(character));

            // РћР±РЅРѕРІР»СЏРµРј РїСЂРѕРіСЂРµСЃСЃ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
            float progress = (float)(i + 1) / jumpsRequired;
            UpdateProgressBar(block, progress);

            // РџР°СѓР·Р° РјРµР¶РґСѓ РїСЂС‹Р¶РєР°РјРё
            if (i < jumpsRequired - 1)
            {
                yield return new WaitForSeconds(jumpInterval);

                // РџР РћР’Р•Р РЇР•Рњ РџР Р•Р Р«Р’РђРќРР• РџРћРЎР›Р• РџРђРЈР—Р«
                if (characterAI != null && characterAI.GetCurrentState() != CharacterAI.AIState.Working)
                {
                    OnConstructionFailed(character, block);
                    yield break;
                }
            }
        }

        // РЈРґР°Р»СЏРµРј РїРѕР»РѕСЃСѓ РїСЂРѕРіСЂРµСЃСЃР°
        RemoveProgressBar(block);

        // 4. Р‘Р»РѕРє РїРѕСЃС‚СЂРѕРµРЅ - Р·Р°РјРµРЅСЏРµРј РЅР° С„РёРЅР°Р»СЊРЅС‹Р№ РїСЂРµС„Р°Р±

        block.isCompleted = true;
        block.OnConstructionComplete?.Invoke();

        // РџР•Р Р•РљР›Р®Р§РђР•Рњ AI РїРµСЂСЃРѕРЅР°Р¶Р° РѕР±СЂР°С‚РЅРѕ РІ СЃРѕСЃС‚РѕСЏРЅРёРµ PlayerControlled
        if (characterAI != null)
        {
            characterAI.SetAIState(CharacterAI.AIState.PlayerControlled);
        }

        // РЈРґР°Р»СЏРµРј РёР· РѕС‡РµСЂРµРґРё
        constructionQueue.Remove(block);
        busyCharacters.Remove(character);

        // РЈРґР°Р»СЏРµРј РєРѕСЂСѓС‚РёРЅСѓ РёР· СЃР»РѕРІР°СЂСЏ (СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІРѕ Р·Р°РІРµСЂС€РµРЅРѕ РЅРѕСЂРјР°Р»СЊРЅРѕ)
        if (constructionCoroutines.ContainsKey(character))
        {
            constructionCoroutines.Remove(character);
        }

        // РќР°Р·РЅР°С‡Р°РµРј СЃР»РµРґСѓСЋС‰РёР№ Р±Р»РѕРє СЌС‚РѕРјСѓ РїРµСЂСЃРѕРЅР°Р¶Сѓ
        if (constructionQueue.Count > 0)
        {
            AssignConstructionTask(character);
        }
        else
        {

        }
    }

    /// <summary>
    /// РђРЅРёРјР°С†РёСЏ РїСЂС‹Р¶РєР° РїРµСЂСЃРѕРЅР°Р¶Р°
    /// </summary>
    IEnumerator Jump(Character character)
    {
        Vector3 startPosition = character.transform.position;
        Vector3 jumpTarget = startPosition + Vector3.up * jumpHeight;
        float jumpDuration = 0.3f;
        float elapsed = 0f;

        // РџСЂС‹Р¶РѕРє РІРІРµСЂС…
        while (elapsed < jumpDuration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (jumpDuration / 2f);
            character.transform.position = Vector3.Lerp(startPosition, jumpTarget, t);
            yield return null;
        }

        // РџР°РґРµРЅРёРµ РІРЅРёР·
        elapsed = 0f;
        while (elapsed < jumpDuration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (jumpDuration / 2f);
            character.transform.position = Vector3.Lerp(jumpTarget, startPosition, t);
            yield return null;
        }

        // Р“Р°СЂР°РЅС‚РёСЂСѓРµРј РІРѕР·РІСЂР°С‚ РЅР° РёСЃС…РѕРґРЅСѓСЋ РїРѕР·РёС†РёСЋ
        character.transform.position = startPosition;
    }

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚РєР° РЅРµСѓРґР°С‡РЅРѕРіРѕ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
    /// </summary>
    void OnConstructionFailed(Character character, ConstructionBlock block)
    {
        // РќР• СѓРґР°Р»СЏРµРј РїРѕР»РѕСЃСѓ РїСЂРѕРіСЂРµСЃСЃР° - РѕРЅР° РґРѕР»Р¶РЅР° РѕСЃС‚Р°С‚СЊСЃСЏ РІРёРґРёРјРѕР№!
        // РЎС‚СЂРѕРёС‚РµР»СЊСЃС‚РІРѕ РјРѕР¶РµС‚ Р±С‹С‚СЊ РїСЂРѕРґРѕР»Р¶РµРЅРѕ РґСЂСѓРіРёРј РїРµСЂСЃРѕРЅР°Р¶РµРј

        // РћРЎР’РћР‘РћР–Р”РђР•Рњ РљР›Р•РўРљРЈ РЎРўР•РќР«, Р•РЎР›Р РћРќРђ Р‘Р«Р›Рђ РџРћРњР•Р§Р•РќРђ РљРђРљ Р—РђРќРЇРўРђРЇ
        if (block.blockType == ConstructionBlock.BlockType.Wall && gridManager != null)
        {
            GridCell wallCell = gridManager.GetCell(block.gridPosition);
            if (wallCell != null && wallCell.isOccupied)
            {
                wallCell.isOccupied = false;
            }
        }

        // РџР•Р Р•РљР›Р®Р§РђР•Рњ AI РїРµСЂСЃРѕРЅР°Р¶Р° РѕР±СЂР°С‚РЅРѕ РІ СЃРѕСЃС‚РѕСЏРЅРёРµ PlayerControlled
        CharacterAI characterAI = character.GetComponent<CharacterAI>();
        if (characterAI != null)
        {
            characterAI.SetAIState(CharacterAI.AIState.PlayerControlled);
        }

        block.isAssigned = false;
        busyCharacters.Remove(character);

        // РЈРґР°Р»СЏРµРј РєРѕСЂСѓС‚РёРЅСѓ РёР· СЃР»РѕРІР°СЂСЏ (СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІРѕ РїСЂРѕРІР°Р»РµРЅРѕ)
        if (constructionCoroutines.ContainsKey(character))
        {
            constructionCoroutines.Remove(character);
        }

        // Р•СЃР»Рё РїРµСЂСЃРѕРЅР°Р¶ РµС‰Рµ Р¶РёРІ, РЅР°Р·РЅР°С‡Р°РµРј РµРјСѓ РґСЂСѓРіРѕР№ Р±Р»РѕРє
        // FIX: Do NOT auto-assign new task when construction is interrupted
        // Player will manually assign characters to construction tasks
        // This prevents characters from being stuck in endless construction loops
    }

    /// <summary>
    /// РћС‡РёСЃС‚РёС‚СЊ РѕС‡РµСЂРµРґСЊ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
    /// </summary>
    public void ClearConstructionQueue()
    {
        // РЈРґР°Р»СЏРµРј РІСЃРµ РїРѕР»РѕСЃС‹ РїСЂРѕРіСЂРµСЃСЃР°
        foreach (var progressBar in progressBars.Values)
        {
            if (progressBar != null)
            {
                Destroy(progressBar);
            }
        }
        progressBars.Clear();

        // РћРЎР’РћР‘РћР–Р”РђР•Рњ РљР›Р•РўРљР РЎРўР•Рќ, РљРћРўРћР Р«Р• Р‘Р«Р›Р РџРћРњР•Р§Р•РќР« РљРђРљ Р—РђРќРЇРўР«Р• Р’Рћ Р’Р Р•РњРЇ РЎРўР РћРРўР•Р›Р¬РЎРўР’Рђ
        if (gridManager != null)
        {
            foreach (var block in constructionQueue)
            {
                if (block.blockType == ConstructionBlock.BlockType.Wall && block.isAssigned && !block.isCompleted)
                {
                    GridCell wallCell = gridManager.GetCell(block.gridPosition);
                    if (wallCell != null && wallCell.isOccupied)
                    {
                        wallCell.isOccupied = false;
                    }
                }
            }
        }

        // РћРЎРўРђРќРђР’Р›РР’РђР•Рњ Р’РЎР• РљРћР РЈРўРРќР« РЎРўР РћРРўР•Р›Р¬РЎРўР’Рђ
        foreach (var character in busyCharacters.Keys)
        {
            if (character != null)
            {
                // РћСЃС‚Р°РЅР°РІР»РёРІР°РµРј РєРѕСЂСѓС‚РёРЅСѓ
                if (constructionCoroutines.ContainsKey(character))
                {
                    StopCoroutine(constructionCoroutines[character]);
                }

                // РџРµСЂРµРєР»СЋС‡Р°РµРј AI РѕР±СЂР°С‚РЅРѕ РІ СЃРѕСЃС‚РѕСЏРЅРёРµ PlayerControlled
                CharacterAI characterAI = character.GetComponent<CharacterAI>();
                if (characterAI != null)
                {
                    characterAI.SetAIState(CharacterAI.AIState.PlayerControlled);
                }
            }
        }

        constructionQueue.Clear();
        busyCharacters.Clear();
        constructionCoroutines.Clear(); // РћС‡РёС‰Р°РµРј СЃР»РѕРІР°СЂСЊ РєРѕСЂСѓС‚РёРЅ

    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РєРѕР»РёС‡РµСЃС‚РІРѕ Р±Р»РѕРєРѕРІ РІ РѕС‡РµСЂРµРґРё
    /// </summary>
    public int GetQueueSize()
    {
        return constructionQueue.Count;
    }

    /// <summary>
    /// РџРЈР‘Р›РР§РќР«Р™ РњР•РўРћР”: РџСЂРёРЅСѓРґРёС‚РµР»СЊРЅРѕ РѕСЃС‚Р°РЅРѕРІРёС‚СЊ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІРѕ РґР»СЏ РєРѕРЅРєСЂРµС‚РЅРѕРіРѕ РїРµСЂСЃРѕРЅР°Р¶Р°
    /// Р’С‹Р·С‹РІР°РµС‚СЃСЏ РєРѕРіРґР° РёРіСЂРѕРє РѕС‚РґР°РµС‚ РїРµСЂСЃРѕРЅР°Р¶Сѓ РґСЂСѓРіСѓСЋ РєРѕРјР°РЅРґСѓ РІРѕ РІСЂРµРјСЏ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
    /// </summary>
    public void StopConstructionForCharacter(Character character)
    {
        if (character == null)
            return;

        // РџСЂРѕРІРµСЂСЏРµРј С‡С‚Рѕ РїРµСЂСЃРѕРЅР°Р¶ Р·Р°РЅСЏС‚ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІРѕРј
        if (!busyCharacters.ContainsKey(character))
            return;

        ConstructionBlock block = busyCharacters[character];


        // вњ“ РљР РРўРР§Р•РЎРљР Р’РђР–РќРћ: РћРЎРўРђРќРђР’Р›РР’РђР•Рњ РљРћР РЈРўРРќРЈ РЎРўР РћРРўР•Р›Р¬РЎРўР’Рђ!
        if (constructionCoroutines.ContainsKey(character))
        {
            StopCoroutine(constructionCoroutines[character]);
            constructionCoroutines.Remove(character);
        }

        // РќР• СѓРґР°Р»СЏРµРј РїРѕР»РѕСЃСѓ РїСЂРѕРіСЂРµСЃСЃР° - РѕРЅР° РґРѕР»Р¶РЅР° РѕСЃС‚Р°С‚СЊСЃСЏ РІРёРґРёРјРѕР№!
        // РџРѕР»РѕСЃР° Р±СѓРґРµС‚ СѓРґР°Р»РµРЅР° С‚РѕР»СЊРєРѕ РєРѕРіРґР° СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІРѕ Р·Р°РІРµСЂС€РёС‚СЃСЏ

        // РћРЎР’РћР‘РћР–Р”РђР•Рњ РљР›Р•РўРљРЈ РЎРўР•РќР«, Р•РЎР›Р РћРќРђ Р‘Р«Р›Рђ РџРћРњР•Р§Р•РќРђ РљРђРљ Р—РђРќРЇРўРђРЇ
        if (block.blockType == ConstructionBlock.BlockType.Wall && gridManager != null)
        {
            GridCell wallCell = gridManager.GetCell(block.gridPosition);
            if (wallCell != null && wallCell.isOccupied)
            {
                wallCell.isOccupied = false;
            }
        }

        // РћСЃРІРѕР±РѕР¶РґР°РµРј Р±Р»РѕРє РґР»СЏ РґСЂСѓРіРёС… РїРµСЂСЃРѕРЅР°Р¶РµР№
        block.isAssigned = false;

        // РЈРґР°Р»СЏРµРј РїРµСЂСЃРѕРЅР°Р¶Р° РёР· Р·Р°РЅСЏС‚С‹С…
        busyCharacters.Remove(character);


    }

    /// <summary>
    /// РЎРѕР·РґР°С‚СЊ РїРѕР»РѕСЃСѓ РїСЂРѕРіСЂРµСЃСЃР° РґР»СЏ Р±Р»РѕРєР° (РёР»Рё РІРµСЂРЅСѓС‚СЊ СЃСѓС‰РµСЃС‚РІСѓСЋС‰СѓСЋ)
    /// </summary>
    GameObject CreateProgressBar(ConstructionBlock block)
    {
        // РџСЂРѕРІРµСЂСЏРµРј, СЃСѓС‰РµСЃС‚РІСѓРµС‚ Р»Рё СѓР¶Рµ РїРѕР»РѕСЃР° РїСЂРѕРіСЂРµСЃСЃР° РґР»СЏ СЌС‚РѕРіРѕ Р±Р»РѕРєР°
        if (progressBars.ContainsKey(block) && progressBars[block] != null)
        {
            return progressBars[block];
        }

        // РЎРѕР·РґР°РµРј РєРѕРЅС‚РµР№РЅРµСЂ РґР»СЏ РїРѕР»РѕСЃС‹ РїСЂРѕРіСЂРµСЃСЃР°
        GameObject progressBarContainer = new GameObject($"ProgressBar_{block.gridPosition}");
        progressBarContainer.transform.position = block.worldPosition + Vector3.up * progressBarHeight;

        // Р”РѕР±Р°РІР»СЏРµРј Billboard РєРѕРјРїРѕРЅРµРЅС‚ РґР»СЏ РїРѕРІРѕСЂРѕС‚Р° Рє РєР°РјРµСЂРµ
        progressBarContainer.AddComponent<Billboard>();

        // РЎРѕР·РґР°РµРј Canvas РґР»СЏ РјРёСЂРѕРІРѕРіРѕ РїСЂРѕСЃС‚СЂР°РЅСЃС‚РІР°
        Canvas canvas = progressBarContainer.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        // РќР°СЃС‚СЂР°РёРІР°РµРј СЂР°Р·РјРµСЂ Canvas
        RectTransform canvasRect = progressBarContainer.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(progressBarWidth, progressBarThickness);
        canvasRect.localScale = Vector3.one * progressBarScale; // РњР°СЃС€С‚Р°Р± РґР»СЏ РјРёСЂРѕРІРѕРіРѕ РїСЂРѕСЃС‚СЂР°РЅСЃС‚РІР°

        // РЎРѕР·РґР°РµРј С„РѕРЅ РїРѕР»РѕСЃС‹
        GameObject background = new GameObject("Background");
        background.transform.SetParent(progressBarContainer.transform, false);

        UnityEngine.UI.Image bgImage = background.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = progressBarBackgroundColor;

        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // РЎРѕР·РґР°РµРј Р·Р°РїРѕР»РЅРµРЅРёРµ РїРѕР»РѕСЃС‹ (СЂР°СЃС‚С‘С‚ СЃР»РµРІР° РЅР°РїСЂР°РІРѕ)
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(background.transform, false);

        UnityEngine.UI.Image fillImage = fill.AddComponent<UnityEngine.UI.Image>();
        fillImage.color = progressBarFillColor;

        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0, 0);     // Р›РµРІС‹Р№ РЅРёР¶РЅРёР№ СѓРіРѕР»
        fillRect.anchorMax = new Vector2(0, 1);     // Р›РµРІС‹Р№ РІРµСЂС…РЅРёР№ СѓРіРѕР» (С€РёСЂРёРЅР° = 0)
        fillRect.pivot = new Vector2(0, 0.5f);      // Pivot СЃР»РµРІР°
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        // РЎРѕС…СЂР°РЅСЏРµРј СЃСЃС‹Р»РєСѓ РЅР° РїРѕР»РѕСЃСѓ
        progressBars[block] = progressBarContainer;

        return progressBarContainer;
    }

    /// <summary>
    /// РћР±РЅРѕРІРёС‚СЊ РїСЂРѕРіСЂРµСЃСЃ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР° Р±Р»РѕРєР°
    /// </summary>
    void UpdateProgressBar(ConstructionBlock block, float progress)
    {
        if (progressBars.ContainsKey(block) && progressBars[block] != null)
        {
            // РќР°С…РѕРґРёРј Fill RectTransform
            Transform fillTransform = progressBars[block].transform.Find("Background/Fill");
            if (fillTransform != null)
            {
                RectTransform fillRect = fillTransform.GetComponent<RectTransform>();
                if (fillRect != null)
                {
                    // РР·РјРµРЅСЏРµРј С€РёСЂРёРЅСѓ РїРѕР»РѕСЃС‹ РѕС‚ 0 РґРѕ 1 (0% РґРѕ 100%)
                    fillRect.anchorMax = new Vector2(progress, 1);
                }
            }
        }
    }

    /// <summary>
    /// РЈРґР°Р»РёС‚СЊ РїРѕР»РѕСЃСѓ РїСЂРѕРіСЂРµСЃСЃР° Р±Р»РѕРєР°
    /// </summary>
    void RemoveProgressBar(ConstructionBlock block)
    {
        if (progressBars.ContainsKey(block))
        {
            if (progressBars[block] != null)
            {
                Destroy(progressBars[block]);
            }
            progressBars.Remove(block);
        }
    }
}

/// <summary>
/// Р”Р°РЅРЅС‹Рµ Рѕ Р±Р»РѕРєРµ РґР»СЏ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
/// </summary>
[System.Serializable]
public class ConstructionBlock
{
    public Vector2Int gridPosition;
    public Vector3 worldPosition;
    public GameObject ghostObject;
    public BlockType blockType;
    public bool isAssigned = false;
    public bool isCompleted = false;

    // Callback РєРѕРіРґР° СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІРѕ Р·Р°РІРµСЂС€РµРЅРѕ
    public System.Action OnConstructionComplete;

    public enum BlockType
    {
        Wall,
        Floor
    }

    public ConstructionBlock(Vector2Int gridPos, Vector3 worldPos, GameObject ghost, BlockType type)
    {
        gridPosition = gridPos;
        worldPosition = worldPos;
        ghostObject = ghost;
        blockType = type;
    }
}
