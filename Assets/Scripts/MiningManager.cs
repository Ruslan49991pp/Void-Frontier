using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// РЈРїСЂР°РІР»РµРЅРёРµ РґРѕР±С‹С‡РµР№ СЂРµСЃСѓСЂСЃРѕРІ РёР· Р°СЃС‚РµСЂРѕРёРґРѕРІ
/// </summary>
public class MiningManager : MonoBehaviour
{
    [Header("Mining Settings")]
    public float miningJumpInterval = 0.8f; // РРЅС‚РµСЂРІР°Р» РјРµР¶РґСѓ РїСЂС‹Р¶РєР°РјРё (СѓРјРµРЅСЊС€РµРЅРѕ РґР»СЏ Р±РѕР»РµРµ С‡Р°СЃС‚С‹С… РїСЂС‹Р¶РєРѕРІ)
    public int metalPerJump = 10; // РњРµС‚Р°Р»Р»Р° Р·Р° РѕРґРёРЅ РїСЂС‹Р¶РѕРє
    public float jumpHeight = 0.5f; // Р’С‹СЃРѕС‚Р° РїСЂС‹Р¶РєР°
    public float jumpDuration = 0.3f; // Р”Р»РёС‚РµР»СЊРЅРѕСЃС‚СЊ РїСЂС‹Р¶РєР°

    [Header("References")]
    private GridManager gridManager;

    // Р”Р°РЅРЅС‹Рµ Рѕ РґРѕР±С‹С‡Рµ
    [System.Serializable]
    public class MiningTask
    {
        public Character character;
        public GameObject asteroid;
        public LocationObjectInfo asteroidInfo;
        public Vector2Int asteroidGridPosition;
        public Vector2Int reservedWorkPosition; // Р—Р°СЂРµР·РµСЂРІРёСЂРѕРІР°РЅРЅР°СЏ РїРѕР·РёС†РёСЏ РґР»СЏ СЂР°Р±РѕС‚С‹
        public bool isActive;
        public Coroutine miningCoroutine;
    }

    private List<MiningTask> miningTasks = new List<MiningTask>();

    // РЎР»РѕРІР°СЂСЊ Р·Р°СЂРµР·РµСЂРІРёСЂРѕРІР°РЅРЅС‹С… РєР»РµС‚РѕРє РґР»СЏ РґРѕР±С‹С‡Рё (С‡С‚РѕР±С‹ РїРµСЂСЃРѕРЅР°Р¶Рё РЅРµ Р·Р°РЅРёРјР°Р»Рё РѕРґРЅСѓ РєР»РµС‚РєСѓ)
    private Dictionary<Vector2Int, Character> reservedMiningPositions = new Dictionary<Vector2Int, Character>();

    void Awake()
    {
        gridManager = FindObjectOfType<GridManager>();
    }

    /// <summary>
    /// РќР°С‡Р°С‚СЊ РґРѕР±С‹С‡Сѓ СЂРµСЃСѓСЂСЃР°
    /// </summary>
    public void StartMining(Character character, GameObject asteroid)
    {


        if (character == null || asteroid == null)
        {
            return;
        }

        // РџСЂРѕРІРµСЂСЏРµРј, РµСЃС‚СЊ Р»Рё СѓР¶Рµ Р·Р°РґР°С‡Р° РґР»СЏ СЌС‚РѕРіРѕ РїРµСЂСЃРѕРЅР°Р¶Р°
        MiningTask existingTask = miningTasks.Find(t => t.character == character);
        if (existingTask != null)
        {

            // РћСЃС‚Р°РЅР°РІР»РёРІР°РµРј РїСЂРµРґС‹РґСѓС‰СѓСЋ Р·Р°РґР°С‡Сѓ
            StopMiningForCharacter(character);
        }

        // РџСЂРѕРІРµСЂСЏРµРј, С‡С‚Рѕ Р°СЃС‚РµСЂРѕРёРґ РёРјРµРµС‚ СЂРµСЃСѓСЂСЃС‹
        LocationObjectInfo asteroidInfo = asteroid.GetComponent<LocationObjectInfo>();
        if (asteroidInfo == null)
        {
            return;
        }



        if (!asteroidInfo.IsOfType("Asteroid"))
        {
            return;
        }



        if (asteroidInfo.metalAmount <= 0)
        {

            return;
        }

        // РСЃРїРѕР»СЊР·СѓРµРј СЃРѕС…СЂР°РЅРµРЅРЅСѓСЋ СЃС‚Р°СЂС‚РѕРІСѓСЋ РїРѕР·РёС†РёСЋ РІ СЃРµС‚РєРµ, РµСЃР»Рё РѕРЅР° РµСЃС‚СЊ
        Vector2Int asteroidGridPos;
        if (asteroidInfo.gridSize.x > 1)
        {
            // Р”Р»СЏ РјРЅРѕРіРѕРєР»РµС‚РѕС‡РЅС‹С… РѕР±СЉРµРєС‚РѕРІ РёСЃРїРѕР»СЊР·СѓРµРј СЃРѕС…СЂР°РЅРµРЅРЅСѓСЋ РїРѕР·РёС†РёСЋ
            asteroidGridPos = asteroidInfo.gridStartPosition;

        }
        else
        {
            // Р”Р»СЏ РѕРґРЅРѕРєР»РµС‚РѕС‡РЅС‹С… РѕР±СЉРµРєС‚РѕРІ РІС‹С‡РёСЃР»СЏРµРј РїРѕР·РёС†РёСЋ
            asteroidGridPos = gridManager.WorldToGrid(asteroid.transform.position);

        }

        // РЎРѕР·РґР°РµРј РЅРѕРІСѓСЋ Р·Р°РґР°С‡Сѓ РґРѕР±С‹С‡Рё
        // FIX: Immediately switch AI state to interrupt Working/other tasks
        CharacterAI characterAI = character.GetComponent<CharacterAI>();
        if (characterAI != null)
        {
            // Tell AI this is a player command (interrupts Working/Mining)
            characterAI.OnPlayerInitiatedMovement();
        }

        MiningTask task = new MiningTask
        {
            character = character,
            asteroid = asteroid,
            asteroidInfo = asteroidInfo,
            asteroidGridPosition = asteroidGridPos,
            isActive = true
        };

        miningTasks.Add(task);



        // Р—Р°РїСѓСЃРєР°РµРј РїСЂРѕС†РµСЃСЃ РґРѕР±С‹С‡Рё
        task.miningCoroutine = StartCoroutine(MiningProcess(task));
    }

    /// <summary>
    /// РџСЂРѕС†РµСЃСЃ РґРѕР±С‹С‡Рё
    /// </summary>
    IEnumerator MiningProcess(MiningTask task)
    {
        // ARCHITECTURE: РџСѓР±Р»РёРєСѓРµРј СЃРѕР±С‹С‚РёРµ РЅР°С‡Р°Р»Р° РґРѕР±С‹С‡Рё С‡РµСЂРµР· EventBus
        EventBus.Publish(new MiningStartedEvent(task.character, task.asteroid));

        Character character = task.character;
        CharacterMovement movement = character.GetComponent<CharacterMovement>();
        CharacterAI characterAI = character.GetComponent<CharacterAI>();

        // FIX: РЈСЃС‚Р°РЅР°РІР»РёРІР°РµРј С„Р»Р°Рі РґРІРёР¶РµРЅРёСЏ Рє РґРѕР±С‹С‡Рµ
        if (characterAI != null)
        {
            characterAI.SetMovingToJob(true);
        }

        // РџРѕР»СѓС‡Р°РµРј Р±Р»РёР¶Р°Р№С€СѓСЋ СЃРѕСЃРµРґРЅСЋСЋ РєР»РµС‚РєСѓ Рє Р°СЃС‚РµСЂРѕРёРґСѓ
        Vector2Int targetGridPosition = FindNearestAdjacentPosition(character.transform.position, task.asteroidGridPosition, task.asteroidInfo.gridSize, character);


        // РџСЂРѕРІРµСЂСЏРµРј РЅР° СЃРёРіРЅР°Р»СЊРЅРѕРµ Р·РЅР°С‡РµРЅРёРµ "РЅРµ РЅР°Р№РґРµРЅРѕ" (-9999, -9999)
        if (targetGridPosition.x == -9999 && targetGridPosition.y == -9999)
        {
            StopMiningForCharacter(character);
            yield break;
        }

        // Р Р•Р—Р•Р Р’РР РЈР•Рњ РЅР°Р№РґРµРЅРЅСѓСЋ РїРѕР·РёС†РёСЋ РґР»СЏ СЌС‚РѕРіРѕ РїРµСЂСЃРѕРЅР°Р¶Р°
        task.reservedWorkPosition = targetGridPosition;
        reservedMiningPositions[targetGridPosition] = character;

        // РџСЂРѕРІРµСЂСЏРµРј СЂР°СЃСЃС‚РѕСЏРЅРёРµ - РµСЃР»Рё РїРµСЂСЃРѕРЅР°Р¶ СѓР¶Рµ СЂСЏРґРѕРј СЃ Р°СЃС‚РµСЂРѕРёРґРѕРј, РЅРµ РґРІРёРіР°РµРјСЃСЏ
        Vector2Int characterGridPos = gridManager.WorldToGrid(character.transform.position);

        // РџСЂРѕРІРµСЂСЏРµРј, РЅР°С…РѕРґРёС‚СЃСЏ Р»Рё РїРµСЂСЃРѕРЅР°Р¶ СЂСЏРґРѕРј СЃ Р»СЋР±РѕР№ СЃС‚РѕСЂРѕРЅРѕР№ Р°СЃС‚РµСЂРѕРёРґР°
        bool isAdjacentToAsteroid = false;
        Vector2Int asteroidEnd = task.asteroidGridPosition + task.asteroidInfo.gridSize - Vector2Int.one;

        // РџСЂРѕРІРµСЂСЏРµРј Р±Р»РёР·РѕСЃС‚СЊ Рє Р»РµРІРѕР№/РїСЂР°РІРѕР№/РІРµСЂС…РЅРµР№/РЅРёР¶РЅРµР№ СЃС‚РѕСЂРѕРЅР°Рј
        if ((characterGridPos.x == task.asteroidGridPosition.x - 1 && characterGridPos.y >= task.asteroidGridPosition.y - 1 && characterGridPos.y <= asteroidEnd.y + 1) ||
            (characterGridPos.x == asteroidEnd.x + 1 && characterGridPos.y >= task.asteroidGridPosition.y - 1 && characterGridPos.y <= asteroidEnd.y + 1) ||
            (characterGridPos.y == task.asteroidGridPosition.y - 1 && characterGridPos.x >= task.asteroidGridPosition.x - 1 && characterGridPos.x <= asteroidEnd.x + 1) ||
            (characterGridPos.y == asteroidEnd.y + 1 && characterGridPos.x >= task.asteroidGridPosition.x - 1 && characterGridPos.x <= asteroidEnd.x + 1))
        {
            isAdjacentToAsteroid = true;
        }

        // Р¦РёРєР» РґРІРёР¶РµРЅРёСЏ Рє Р°СЃС‚РµСЂРѕРёРґСѓ СЃ РІРѕР·РјРѕР¶РЅРѕСЃС‚СЊСЋ РїРµСЂРµРЅР°РїСЂР°РІР»РµРЅРёСЏ
        int maxRedirects = 5; // РњР°РєСЃРёРјСѓРј 5 РїРµСЂРµРЅР°РїСЂР°РІР»РµРЅРёР№ С‡С‚РѕР±С‹ РёР·Р±РµР¶Р°С‚СЊ Р±РµСЃРєРѕРЅРµС‡РЅРѕРіРѕ С†РёРєР»Р°
        int redirectCount = 0;

        while (!isAdjacentToAsteroid && redirectCount < maxRedirects)
        {
            // РџРµСЂСЃРѕРЅР°Р¶ РґР°Р»РµРєРѕ - РґРІРёР¶РµРјСЃСЏ Рє С†РµР»РµРІРѕР№ РїРѕР·РёС†РёРё
            if (movement != null)
            {
                Vector3 targetWorldPosition = gridManager.GridToWorld(task.reservedWorkPosition);
                movement.MoveTo(targetWorldPosition);

                // Р’РђР–РќРћ: Р”Р°РµРј CharacterMovement РІСЂРµРјСЏ РЅР°С‡Р°С‚СЊ РґРІРёР¶РµРЅРёРµ
                yield return new WaitForSeconds(0.15f);

                bool needsRedirect = false;

                // Р–РґРµРј РѕРєРѕРЅС‡Р°РЅРёСЏ РґРІРёР¶РµРЅРёСЏ Р РїСЂРѕРІРµСЂСЏРµРј, РЅРµ Р·Р°РЅСЏС‚Р° Р»Рё Р·Р°СЂРµР·РµСЂРІРёСЂРѕРІР°РЅРЅР°СЏ РєР»РµС‚РєР°
                while (movement.IsMoving())
                {
                    yield return new WaitForSeconds(0.1f);

                    // РџСЂРѕРІРµСЂСЏРµРј, РЅРµ РїСЂРµСЂРІР°Р»Р°СЃСЊ Р»Рё Р·Р°РґР°С‡Р°
                    if (!task.isActive)
                    {
                        yield break;
                    }

                    // РџР РћР’Р•Р РљРђ Р’Рћ Р’Р Р•РњРЇ РџРЈРўР: РќРµ Р·Р°РЅСЏС‚Р° Р»Рё РЅР°С€Р° Р·Р°СЂРµР·РµСЂРІРёСЂРѕРІР°РЅРЅР°СЏ РєР»РµС‚РєР°?
                    GridCell cellDuringMovement = gridManager.GetCell(task.reservedWorkPosition);
                    Vector2Int currentGridPos = gridManager.WorldToGrid(character.transform.position);

                    if (cellDuringMovement != null && cellDuringMovement.isOccupied && currentGridPos != task.reservedWorkPosition)
                    {
                        // РљР»РµС‚РєР° Р·Р°РЅСЏС‚Р° РІРѕ РІСЂРµРјСЏ РїСѓС‚Рё! РћСЃС‚Р°РЅР°РІР»РёРІР°РµРј РґРІРёР¶РµРЅРёРµ Рё РёС‰РµРј РЅРѕРІСѓСЋ РєР»РµС‚РєСѓ
                        movement.StopMovement();

                        Vector2Int oldReservation = task.reservedWorkPosition;

                        // РС‰РµРј РЅРѕРІСѓСЋ СЃРІРѕР±РѕРґРЅСѓСЋ РєР»РµС‚РєСѓ
                        Vector2Int newPosition = FindNearestAdjacentPosition(character.transform.position, task.asteroidGridPosition, task.asteroidInfo.gridSize, character);

                        if (newPosition.x == -9999 && newPosition.y == -9999)
                        {
                            StopMiningForCharacter(character);
                            yield break;
                        }

                        // Р РµР·РµСЂРІРёСЂСѓРµРј РЅРѕРІСѓСЋ РїРѕР·РёС†РёСЋ РџР•Р Р•Р” РѕСЃРІРѕР±РѕР¶РґРµРЅРёРµРј СЃС‚Р°СЂРѕР№
                        task.reservedWorkPosition = newPosition;
                        reservedMiningPositions[newPosition] = character;

                        // РћСЃРІРѕР±РѕР¶РґР°РµРј СЃС‚Р°СЂСѓСЋ СЂРµР·РµСЂРІР°С†РёСЋ
                        if (reservedMiningPositions.ContainsKey(oldReservation))
                        {
                            if (reservedMiningPositions[oldReservation] == character)
                            {
                                reservedMiningPositions.Remove(oldReservation);
                            }
                        }

                        redirectCount++;
                        needsRedirect = true;
                        break; // Р’С‹С…РѕРґРёРј РёР· while(movement.IsMoving()) С‡С‚РѕР±С‹ РЅР°С‡Р°С‚СЊ РЅРѕРІРѕРµ РґРІРёР¶РµРЅРёРµ
                    }
                }

                if (needsRedirect)
                {
                    continue; // РќР°С‡РёРЅР°РµРј РЅРѕРІСѓСЋ РёС‚РµСЂР°С†РёСЋ С†РёРєР»Р° РґРІРёР¶РµРЅРёСЏ СЃ РЅРѕРІРѕР№ С†РµР»СЊСЋ
                }
            }

            // РџРѕСЃР»Рµ РґРІРёР¶РµРЅРёСЏ РїСЂРѕРІРµСЂСЏРµРј РїРѕР·РёС†РёСЋ
            characterGridPos = gridManager.WorldToGrid(character.transform.position);

            // РџСЂРѕРІРµСЂСЏРµРј, РґРѕСЃС‚РёРі Р»Рё РїРµСЂСЃРѕРЅР°Р¶ РїРѕР·РёС†РёРё СЂСЏРґРѕРј СЃ Р°СЃС‚РµСЂРѕРёРґРѕРј
            isAdjacentToAsteroid = false;
            if ((characterGridPos.x == task.asteroidGridPosition.x - 1 && characterGridPos.y >= task.asteroidGridPosition.y - 1 && characterGridPos.y <= asteroidEnd.y + 1) ||
                (characterGridPos.x == asteroidEnd.x + 1 && characterGridPos.y >= task.asteroidGridPosition.y - 1 && characterGridPos.y <= asteroidEnd.y + 1) ||
                (characterGridPos.y == task.asteroidGridPosition.y - 1 && characterGridPos.x >= task.asteroidGridPosition.x - 1 && characterGridPos.x <= asteroidEnd.x + 1) ||
                (characterGridPos.y == asteroidEnd.y + 1 && characterGridPos.x >= task.asteroidGridPosition.x - 1 && characterGridPos.x <= asteroidEnd.x + 1))
            {
                isAdjacentToAsteroid = true;
            }
        }

        // РџСЂРѕРІРµСЂРєР° С‡С‚Рѕ РґРѕСЃС‚РёРіР»Рё С†РµР»Рё
        if (redirectCount >= maxRedirects)
        {
            StopMiningForCharacter(character);
            yield break;
        }

        // Р•СЃР»Рё С†РёРєР» СѓСЃРїРµС€РЅРѕ Р·Р°РІРµСЂС€РёР»СЃСЏ, Р·РЅР°С‡РёС‚ isAdjacentToAsteroid == true
        // РџРµСЂСЃРѕРЅР°Р¶ РіР°СЂР°РЅС‚РёСЂРѕРІР°РЅРЅРѕ СЂСЏРґРѕРј СЃ Р°СЃС‚РµСЂРѕРёРґРѕРј
        characterGridPos = gridManager.WorldToGrid(character.transform.position);

        // РџРµСЂСЃРѕРЅР°Р¶ РїРѕРґС‚РІРµСЂР¶РґРµРЅРЅРѕ СЂСЏРґРѕРј СЃ Р°СЃС‚РµСЂРѕРёРґРѕРј - РїРµСЂРµРєР»СЋС‡Р°РµРј РІ СЃРѕСЃС‚РѕСЏРЅРёРµ Mining


        if (characterAI != null)
        {
            // FIX: Р"РћРЁР›Р! РЎР±СЂР°СЃС‹РІР°РµРј С„Р»Р°Рі
            characterAI.SetMovingToJob(false);
            characterAI.SetAIState(CharacterAI.AIState.Mining);
        }

        // РџРѕРІРѕСЂР°С‡РёРІР°РµРј РїРµСЂСЃРѕРЅР°Р¶Р° Рє Р°СЃС‚РµСЂРѕРёРґСѓ
        RotateCharacterTowardsAsteroid(character, task.asteroid);



        // РћСЃРЅРѕРІРЅРѕР№ С†РёРєР» РґРѕР±С‹С‡Рё
        int totalMinedMetal = 0; // Р”Р»СЏ СЃРѕР±С‹С‚РёСЏ MiningCompletedEvent
        int loopIteration = 0;
        while (task.isActive && task.asteroidInfo.metalAmount > 0)
        {
            loopIteration++;



            // РџСЂРѕРІРµСЂСЏРµРј, С‡С‚Рѕ РїРµСЂСЃРѕРЅР°Р¶ Р¶РёРІ
            if (character.IsDead())
            {

                StopMiningForCharacter(character);
                yield break;
            }


            // РљР РРўРР§РќРћ: РџСЂРѕРІРµСЂСЏРµРј РїРѕР·РёС†РёСЋ РїРµСЂСЃРѕРЅР°Р¶Р° РџР•Р Р•Р” РљРђР–Р”Р«Рњ РїСЂС‹Р¶РєРѕРј
            characterGridPos = gridManager.WorldToGrid(character.transform.position);



            // РџРµСЂРµСЃС‡РёС‚С‹РІР°РµРј asteroidEnd РґР»СЏ РїСЂРѕРІРµСЂРєРё adjacency
            asteroidEnd = task.asteroidGridPosition + task.asteroidInfo.gridSize - Vector2Int.one;


            // РџСЂРѕРІРµСЂСЏРµРј, РЅР°С…РѕРґРёС‚СЃСЏ Р»Рё РїРµСЂСЃРѕРЅР°Р¶ РІСЃС‘ РµС‰С‘ СЂСЏРґРѕРј СЃ Р°СЃС‚РµСЂРѕРёРґРѕРј
            isAdjacentToAsteroid = false;

            // Р”РµС‚Р°Р»СЊРЅР°СЏ РїСЂРѕРІРµСЂРєР° РєР°Р¶РґРѕРіРѕ СѓСЃР»РѕРІРёСЏ
            bool leftSide = (characterGridPos.x == task.asteroidGridPosition.x - 1 && characterGridPos.y >= task.asteroidGridPosition.y - 1 && characterGridPos.y <= asteroidEnd.y + 1);
            bool rightSide = (characterGridPos.x == asteroidEnd.x + 1 && characterGridPos.y >= task.asteroidGridPosition.y - 1 && characterGridPos.y <= asteroidEnd.y + 1);
            bool bottomSide = (characterGridPos.y == task.asteroidGridPosition.y - 1 && characterGridPos.x >= task.asteroidGridPosition.x - 1 && characterGridPos.x <= asteroidEnd.x + 1);
            bool topSide = (characterGridPos.y == asteroidEnd.y + 1 && characterGridPos.x >= task.asteroidGridPosition.x - 1 && characterGridPos.x <= asteroidEnd.x + 1);



            if (leftSide || rightSide || bottomSide || topSide)
            {
                isAdjacentToAsteroid = true;
            }



            // Р•СЃР»Рё РїРµСЂСЃРѕРЅР°Р¶ Р±РѕР»СЊС€Рµ РЅРµ СЂСЏРґРѕРј СЃ Р°СЃС‚РµСЂРѕРёРґРѕРј - РќР•РњР•Р”Р›Р•РќРќРћ РѕСЃС‚Р°РЅР°РІР»РёРІР°РµРј РґРѕР±С‹С‡Сѓ
            if (!isAdjacentToAsteroid)
            {

                StopMiningForCharacter(character);
                yield break;
            }

            // Р’С‹РїРѕР»РЅСЏРµРј РїСЂС‹Р¶РѕРє (С‚РѕР»СЊРєРѕ РµСЃР»Рё РїРµСЂСЃРѕРЅР°Р¶ СЂСЏРґРѕРј!)

            yield return StartCoroutine(PerformMiningJump(character));


            // Р”РѕР±С‹РІР°РµРј РјРµС‚Р°Р»Р»
            int minedAmount = Mathf.Min(metalPerJump, task.asteroidInfo.metalAmount);
            int oldAmount = task.asteroidInfo.metalAmount;
            task.asteroidInfo.metalAmount -= minedAmount;
            totalMinedMetal += minedAmount; // РќР°РєР°РїР»РёРІР°РµРј РґР»СЏ СЃРѕР±С‹С‚РёСЏ


            // Р”РѕР±Р°РІР»СЏРµРј РјРµС‚Р°Р»Р» РІ РёРЅРІРµРЅС‚Р°СЂСЊ РїРµСЂСЃРѕРЅР°Р¶Р°
            Inventory inventory = character.GetComponent<Inventory>();
            if (inventory != null)
            {

                AddMetalToInventory(inventory, minedAmount);

            }
            else
            {
            }

            // РџСЂРѕРІРµСЂСЏРµРј, РёСЃС‚РѕС‰РёР»СЃСЏ Р»Рё Р°СЃС‚РµСЂРѕРёРґ
            if (task.asteroidInfo.metalAmount <= 0)
            {

                break;
            }




            // Р–РґРµРј РґРѕ СЃР»РµРґСѓСЋС‰РµРіРѕ РїСЂС‹Р¶РєР°
            yield return new WaitForSeconds(miningJumpInterval);


        }

        // ARCHITECTURE: РџСѓР±Р»РёРєСѓРµРј СЃРѕР±С‹С‚РёРµ Р·Р°РІРµСЂС€РµРЅРёСЏ РґРѕР±С‹С‡Рё С‡РµСЂРµР· EventBus
        EventBus.Publish(new MiningCompletedEvent(character, task.asteroid, totalMinedMetal));

        // Р”РѕР±С‹С‡Р° Р·Р°РІРµСЂС€РµРЅР°
        StopMiningForCharacter(character);
    }

    /// <summary>
    /// РќР°Р№С‚Рё Р±Р»РёР¶Р°Р№С€СѓСЋ СЃРѕСЃРµРґРЅСЋСЋ РїРѕР·РёС†РёСЋ Рє Р°СЃС‚РµСЂРѕРёРґСѓ (СЃС‚Р°СЂС‹Р№ РјРµС‚РѕРґ РґР»СЏ СЃРѕРІРјРµСЃС‚РёРјРѕСЃС‚Рё)
    /// </summary>
    Vector2Int FindNearestAdjacentPosition(Vector3 characterPosition, Vector2Int asteroidStartPos, Vector2Int asteroidSize)
    {
        return FindNearestAdjacentPosition(characterPosition, asteroidStartPos, asteroidSize, null);
    }

    /// <summary>
    /// РќР°Р№С‚Рё Р±Р»РёР¶Р°Р№С€СѓСЋ СЃРѕСЃРµРґРЅСЋСЋ РїРѕР·РёС†РёСЋ Рє Р°СЃС‚РµСЂРѕРёРґСѓ (СѓС‡РёС‚С‹РІР°РµС‚ СЂР°Р·РјРµСЂ Р°СЃС‚РµСЂРѕРёРґР° Рё СЃРІРѕРё СЂРµР·РµСЂРІР°С†РёРё)
    /// </summary>
    Vector2Int FindNearestAdjacentPosition(Vector3 characterPosition, Vector2Int asteroidStartPos, Vector2Int asteroidSize, Character requestingCharacter)
    {
        Vector2Int characterGridPos = gridManager.WorldToGrid(characterPosition);
        Vector2Int notFoundSentinel = new Vector2Int(-9999, -9999);



        // Р“РµРЅРµСЂРёСЂСѓРµРј СЃРїРёСЃРѕРє РІСЃРµС… РєР»РµС‚РѕРє РїРѕ РїРµСЂРёРјРµС‚СЂСѓ Р°СЃС‚РµСЂРѕРёРґР°
        List<Vector2Int> perimeterCells = new List<Vector2Int>();

        // Р’РµСЂС…РЅСЏСЏ СЃС‚РѕСЂРѕРЅР° (Y = asteroidStartPos.y - 1)
        for (int x = asteroidStartPos.x - 1; x <= asteroidStartPos.x + asteroidSize.x; x++)
        {
            perimeterCells.Add(new Vector2Int(x, asteroidStartPos.y - 1));
        }

        // РќРёР¶РЅСЏСЏ СЃС‚РѕСЂРѕРЅР° (Y = asteroidStartPos.y + asteroidSize.y)
        for (int x = asteroidStartPos.x - 1; x <= asteroidStartPos.x + asteroidSize.x; x++)
        {
            perimeterCells.Add(new Vector2Int(x, asteroidStartPos.y + asteroidSize.y));
        }

        // Р›РµРІР°СЏ СЃС‚РѕСЂРѕРЅР° (X = asteroidStartPos.x - 1), РёСЃРєР»СЋС‡Р°СЏ СѓРіР»С‹
        for (int y = asteroidStartPos.y; y < asteroidStartPos.y + asteroidSize.y; y++)
        {
            perimeterCells.Add(new Vector2Int(asteroidStartPos.x - 1, y));
        }

        // РџСЂР°РІР°СЏ СЃС‚РѕСЂРѕРЅР° (X = asteroidStartPos.x + asteroidSize.x), РёСЃРєР»СЋС‡Р°СЏ СѓРіР»С‹
        for (int y = asteroidStartPos.y; y < asteroidStartPos.y + asteroidSize.y; y++)
        {
            perimeterCells.Add(new Vector2Int(asteroidStartPos.x + asteroidSize.x, y));
        }



        // РџСЂРѕРІРµСЂСЏРµРј, РЅР°С…РѕРґРёС‚СЃСЏ Р»Рё РїРµСЂСЃРѕРЅР°Р¶ СѓР¶Рµ РЅР° РѕРґРЅРѕР№ РёР· РїРµСЂРёРјРµСЂРЅС‹С… РєР»РµС‚РѕРє
        foreach (Vector2Int perimeterCell in perimeterCells)
        {
            if (characterGridPos == perimeterCell)
            {
                GridCell cell = gridManager.GetCell(perimeterCell);
                if (cell != null && !cell.isOccupied)
                {

                    return characterGridPos;
                }
            }
        }

        // РС‰РµРј Р±Р»РёР¶Р°Р№С€СѓСЋ СЃРІРѕР±РѕРґРЅСѓСЋ РїРµСЂРёРјРµС‚СЂРѕРІСѓСЋ РєР»РµС‚РєСѓ
        Vector2Int closestPosition = notFoundSentinel;
        float closestDistance = float.MaxValue;
        int validCells = 0;
        int occupiedCells = 0;
        int invalidCells = 0;

        foreach (Vector2Int perimeterPos in perimeterCells)
        {
            // РџСЂРѕРІРµСЂСЏРµРј РІР°Р»РёРґРЅРѕСЃС‚СЊ РїРѕР·РёС†РёРё
            if (!gridManager.IsValidGridPosition(perimeterPos))
            {
                invalidCells++;
                continue;
            }

            GridCell cell = gridManager.GetCell(perimeterPos);
            if (cell == null)
            {
                invalidCells++;
                continue;
            }

            if (cell.isOccupied)
            {
                occupiedCells++;
                continue;
            }

            // РџСЂРѕРІРµСЂСЏРµРј, РЅРµ Р·Р°СЂРµР·РµСЂРІРёСЂРѕРІР°РЅР° Р»Рё РєР»РµС‚РєР° РґСЂСѓРіРёРј РїРµСЂСЃРѕРЅР°Р¶РµРј
            if (reservedMiningPositions.ContainsKey(perimeterPos))
            {
                // Р•СЃР»Рё СЌС‚Рѕ РЅР°С€Р° СЃРѕР±СЃС‚РІРµРЅРЅР°СЏ СЂРµР·РµСЂРІР°С†РёСЏ - РјРѕР¶РµРј РёСЃРїРѕР»СЊР·РѕРІР°С‚СЊ
                if (requestingCharacter != null && reservedMiningPositions[perimeterPos] == requestingCharacter)
                {
                    // Р­С‚Рѕ РЅР°С€Р° СЂРµР·РµСЂРІР°С†РёСЏ, РїСЂРѕРґРѕР»Р¶Р°РµРј РїСЂРѕРІРµСЂРєСѓ РєР°Рє СЃРІРѕР±РѕРґРЅСѓСЋ РєР»РµС‚РєСѓ
                }
                else
                {
                    // Р—Р°СЂРµР·РµСЂРІРёСЂРѕРІР°РЅР° РєРµРј-С‚Рѕ РґСЂСѓРіРёРј
                    occupiedCells++;
                    continue;
                }
            }

            // РљР»РµС‚РєР° СЃРІРѕР±РѕРґРЅР° Рё РІР°Р»РёРґРЅР°
            validCells++;
            Vector3 perimeterWorldPos = gridManager.GridToWorld(perimeterPos);
            float distance = Vector3.Distance(characterPosition, perimeterWorldPos);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPosition = perimeterPos;
            }
        }



        if (closestPosition != notFoundSentinel)
        {

        }
        else
        {
        }

        return closestPosition;
    }

    /// <summary>
    /// РџРѕРІРµСЂРЅСѓС‚СЊ РїРµСЂСЃРѕРЅР°Р¶Р° Рє Р°СЃС‚РµСЂРѕРёРґСѓ
    /// </summary>
    void RotateCharacterTowardsAsteroid(Character character, GameObject asteroid)
    {
        Vector3 direction = asteroid.transform.position - character.transform.position;
        direction.y = 0;

        if (direction.magnitude < 0.1f)
            return;

        // Р’С‹С‡РёСЃР»СЏРµРј СѓРіРѕР»
        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        // РџСЂРёРІСЏР·С‹РІР°РµРј Рє 8 РЅР°РїСЂР°РІР»РµРЅРёСЏРј (0В°, 45В°, 90В°, 135В°, 180В°, 225В°, 270В°, 315В°)
        float snappedAngle = Mathf.Round(angle / 45f) * 45f;

        character.transform.rotation = Quaternion.Euler(0, snappedAngle, 0);
    }

    /// <summary>
    /// Р’С‹РїРѕР»РЅРёС‚СЊ Р°РЅРёРјР°С†РёСЋ РїСЂС‹Р¶РєР° РґР»СЏ РґРѕР±С‹С‡Рё
    /// </summary>
    IEnumerator PerformMiningJump(Character character)
    {
        Vector3 originalPosition = character.transform.position;
        float elapsedTime = 0f;

        // РџСЂС‹Р¶РѕРє РІРІРµСЂС… Рё РІРЅРёР·
        while (elapsedTime < jumpDuration)
        {
            float progress = elapsedTime / jumpDuration;
            // РЎРёРЅСѓСЃРѕРёРґР°Р»СЊРЅР°СЏ РєСЂРёРІР°СЏ РґР»СЏ РїР»Р°РІРЅРѕРіРѕ РїСЂС‹Р¶РєР°
            float height = Mathf.Sin(progress * Mathf.PI) * jumpHeight;
            character.transform.position = originalPosition + Vector3.up * height;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Р’РѕР·РІСЂР°С‰Р°РµРј РїРµСЂСЃРѕРЅР°Р¶Р° РЅР° РёСЃС…РѕРґРЅСѓСЋ РїРѕР·РёС†РёСЋ
        character.transform.position = originalPosition;
    }

    /// <summary>
    /// РЎРѕР·РґР°С‚СЊ ItemData РґР»СЏ РјРµС‚Р°Р»Р»Р°
    /// </summary>
    ItemData CreateMetalItemData()
    {
        ItemData metal = new ItemData();
        metal.itemName = ItemNames.METAL;
        metal.description = "Raw metal ore extracted from asteroids";
        metal.itemType = ItemType.Resource;
        metal.rarity = ItemRarity.Common;
        metal.maxStackSize = 999;
        metal.weight = 0.1f;
        metal.value = 1;
        metal.equipmentSlot = EquipmentSlot.None;

        // РџСЂРёРјРµРЅСЏРµРј РёРєРѕРЅРєСѓ С‡РµСЂРµР· С„Р°Р±СЂРёРєСѓ (РµСЃР»Рё РµСЃС‚СЊ)
        ItemFactory.ApplyIcon(metal);

        return metal;
    }

    /// <summary>
    /// Р”РѕР±Р°РІРёС‚СЊ РјРµС‚Р°Р»Р» РІ РёРЅРІРµРЅС‚Р°СЂСЊ РїРµСЂСЃРѕРЅР°Р¶Р°
    /// </summary>
    void AddMetalToInventory(Inventory inventory, int amount)
    {
        // РС‰РµРј РјРµС‚Р°Р»Р» РІ РёРЅРІРµРЅС‚Р°СЂРµ
        InventorySlot metalSlot = null;
        List<InventorySlot> allSlots = inventory.GetAllSlots();

        foreach (InventorySlot slot in allSlots)
        {
            if (slot.itemData != null && slot.itemData.itemName == ItemNames.METAL)
            {
                metalSlot = slot;
                break;
            }
        }

        if (metalSlot != null && metalSlot.itemData != null)
        {
            // РЈРІРµР»РёС‡РёРІР°РµРј РєРѕР»РёС‡РµСЃС‚РІРѕ С‡РµСЂРµР· РјРµС‚РѕРґ AddItem
            inventory.AddItem(metalSlot.itemData, amount);

        }
        else
        {
            // РЎРѕР·РґР°РµРј РЅРѕРІС‹Р№ РїСЂРµРґРјРµС‚ РјРµС‚Р°Р»Р»Р°
            ItemData metalItem = CreateMetalItemData();
            inventory.AddItem(metalItem, amount);

        }

        // РћР±РЅРѕРІР»СЏРµРј UI РёРЅРІРµРЅС‚Р°СЂСЏ
        ResourcePanelUI resourcePanel = FindObjectOfType<ResourcePanelUI>();
        if (resourcePanel != null)
        {
            resourcePanel.UpdateResourceDisplay();
        }
    }

    /// <summary>
    /// РћСЃС‚Р°РЅРѕРІРёС‚СЊ РґРѕР±С‹С‡Сѓ РґР»СЏ РїРµСЂСЃРѕРЅР°Р¶Р°
    /// </summary>
    public void StopMiningForCharacter(Character character)
    {
        // РџРѕР»СѓС‡Р°РµРј РёРЅС„РѕСЂРјР°С†РёСЋ Рѕ С‚РѕРј, РѕС‚РєСѓРґР° Р±С‹Р» РІС‹Р·РІР°РЅ СЌС‚РѕС‚ РјРµС‚РѕРґ
        System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace(1, true);
        string callerInfo = stackTrace.GetFrame(0)?.GetMethod()?.Name ?? "Unknown";





        // РџСЂРѕРІРµСЂСЏРµРј, С‡С‚Рѕ РїРµСЂСЃРѕРЅР°Р¶ РµС‰Рµ СЃСѓС‰РµСЃС‚РІСѓРµС‚
        if (character == null)
        {

            // РЈРґР°Р»СЏРµРј РІСЃРµ Р·Р°РґР°С‡Рё СЃ null РїРµСЂСЃРѕРЅР°Р¶Р°РјРё
            miningTasks.RemoveAll(t => t.character == null);
            return;
        }

        MiningTask task = miningTasks.Find(t => t.character == character);
        if (task != null)
        {

            task.isActive = false;

            if (task.miningCoroutine != null)
            {

                StopCoroutine(task.miningCoroutine);
            }

            // Р’РђР–РќРћ: РћСЃРІРѕР±РѕР¶РґР°РµРј Р·Р°СЂРµР·РµСЂРІРёСЂРѕРІР°РЅРЅСѓСЋ РїРѕР·РёС†РёСЋ
            if (reservedMiningPositions.ContainsKey(task.reservedWorkPosition))
            {
                if (reservedMiningPositions[task.reservedWorkPosition] == character)
                {
                    reservedMiningPositions.Remove(task.reservedWorkPosition);
                }
            }

            miningTasks.Remove(task);



            // Р’РѕР·РІСЂР°С‰Р°РµРј РїРµСЂСЃРѕРЅР°Р¶Р° РІ СЃРѕСЃС‚РѕСЏРЅРёРµ Idle
            CharacterAI characterAI = character.GetComponent<CharacterAI>();
            if (characterAI != null && characterAI.GetCurrentState() == CharacterAI.AIState.Mining)
            {

                characterAI.SetAIState(CharacterAI.AIState.Idle);
            }
        }
        else
        {

        }


    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ, РґРѕР±С‹РІР°РµС‚ Р»Рё РїРµСЂСЃРѕРЅР°Р¶ СЂРµСЃСѓСЂСЃС‹
    /// </summary>
    public bool IsCharacterMining(Character character)
    {
        return miningTasks.Exists(t => t.character == character && t.isActive);
    }

    /// <summary>
    /// РћСЃС‚Р°РЅРѕРІРёС‚СЊ РІСЃСЋ РґРѕР±С‹С‡Сѓ
    /// </summary>
    public void StopAllMining()
    {
        List<MiningTask> tasksToStop = new List<MiningTask>(miningTasks);
        foreach (MiningTask task in tasksToStop)
        {
            // РџСЂРѕРІРµСЂСЏРµРј, С‡С‚Рѕ РїРµСЂСЃРѕРЅР°Р¶ РµС‰Рµ СЃСѓС‰РµСЃС‚РІСѓРµС‚
            if (task.character != null)
            {
                StopMiningForCharacter(task.character);
            }
        }

        // РћС‡РёС‰Р°РµРј РІСЃРµ РѕСЃС‚Р°РІС€РёРµСЃСЏ Р·Р°РґР°С‡Рё СЃ null РїРµСЂСЃРѕРЅР°Р¶Р°РјРё
        miningTasks.Clear();

        // РћС‡РёС‰Р°РµРј РІСЃРµ СЂРµР·РµСЂРІР°С†РёРё
        reservedMiningPositions.Clear();
    }

    void OnDestroy()
    {
        StopAllMining();
    }
}
