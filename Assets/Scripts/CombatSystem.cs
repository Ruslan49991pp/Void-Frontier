using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Система боевых действий с преследованием врагов и анимацией атак
/// </summary>
public class CombatSystem : MonoBehaviour
{
    [Header("Combat Settings")]
    public float attackRange = 1.5f; // Дистанция атаки (соседняя клетка)
    public float attackCooldown = 1f; // Скорость атаки (1 удар в секунду)
    public float attackDamage = 25f; // Урон от атаки
    public float pursuitRange = 10f; // Дистанция преследования

    [Header("Attack Animation")]
    public float lungeDistance = 0.25f; // Расстояние прыжка (1/4 клетки)
    public float lungeSpeed = 8f; // Скорость анимации атаки
    public AnimationCurve lungeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Damage Indication")]
    public string damageIndicatorMaterialName = "M_Enemy_Damage";
    public float damageFlashDuration = 0.2f;

    [Header("Debug")]
    public bool debugMode = false;

    // Компоненты системы
    private SelectionManager selectionManager;
    private GridManager gridManager;
    private Camera playerCamera;

    // Состояние боевых действий
    private Dictionary<Character, CombatData> activeCombatants = new Dictionary<Character, CombatData>();
    private Material damageIndicatorMaterial;

    // Класс для хранения данных о боевых действиях персонажа
    private class CombatData
    {
        public Character target;
        public float lastAttackTime;
        public bool isAttacking;
        public bool isPursuing;
        public Vector3 originalPosition;
        public Coroutine combatCoroutine;
        public Coroutine attackAnimationCoroutine;

        public CombatData()
        {
            target = null;
            lastAttackTime = -999f;
            isAttacking = false;
            isPursuing = false;
            originalPosition = Vector3.zero;
            combatCoroutine = null;
            attackAnimationCoroutine = null;
        }
    }

    void Awake()
    {
        selectionManager = FindObjectOfType<SelectionManager>();
        gridManager = FindObjectOfType<GridManager>();
        playerCamera = Camera.main;

        LoadDamageIndicatorMaterial();
    }

    void Start()
    {
        if (selectionManager == null)
        {
            Debug.LogError("[CombatSystem] SelectionManager not found!");
            return;
        }
    }

    void Update()
    {
        HandleCombatInput();
        UpdateCombatStates();
    }

    /// <summary>
    /// Загрузка материала индикации урона
    /// </summary>
    void LoadDamageIndicatorMaterial()
    {
        // Пытаемся найти материал в ресурсах
        damageIndicatorMaterial = Resources.Load<Material>(damageIndicatorMaterialName);

        // Если не нашли в Resources, пытаемся найти в Assets/Materials
        if (damageIndicatorMaterial == null)
        {
            damageIndicatorMaterial = Resources.Load<Material>("Materials/" + damageIndicatorMaterialName);
        }

        if (damageIndicatorMaterial == null)
        {
            // Создаем простой красный материал если не нашли M_Enemy_Damage
            damageIndicatorMaterial = new Material(Shader.Find("Standard"));
            damageIndicatorMaterial.color = new Color(1f, 0.2f, 0.2f, 1f); // Ярко-красный
            damageIndicatorMaterial.SetFloat("_Mode", 0); // Opaque
            damageIndicatorMaterial.EnableKeyword("_EMISSION");
            damageIndicatorMaterial.SetColor("_EmissionColor", new Color(0.8f, 0f, 0f, 1f));

            if (debugMode)
                Debug.LogWarning($"[CombatSystem] Material '{damageIndicatorMaterialName}' not found. Using fallback red material.");
        }
        else
        {
            if (debugMode)
                Debug.Log($"[CombatSystem] Successfully loaded material '{damageIndicatorMaterialName}'");
        }
    }

    /// <summary>
    /// Обработка ввода для боевых действий (ПКМ по врагу)
    /// </summary>
    void HandleCombatInput()
    {
        // Проверяем нажатие ПКМ
        if (Input.GetMouseButtonDown(1))
        {
            // Получаем выделенных союзников
            List<Character> selectedAllies = GetSelectedAllies();
            if (selectedAllies.Count == 0)
                return;

            // Проверяем, есть ли враг под курсором
            Character targetEnemy = GetEnemyUnderMouse();
            if (targetEnemy != null)
            {
                if (debugMode)
                    Debug.Log($"[CombatSystem] Assigning attack target '{targetEnemy.GetFullName()}' to {selectedAllies.Count} allies");

                // Назначаем цель для атаки всем выделенным союзникам
                foreach (Character ally in selectedAllies)
                {
                    AssignCombatTarget(ally, targetEnemy);
                }
            }
        }
    }

    /// <summary>
    /// Получить врага под курсором мыши
    /// </summary>
    Character GetEnemyUnderMouse()
    {
        if (playerCamera == null)
            return null;

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);

        // Сортируем хиты по расстоянию
        System.Array.Sort(hits, (hit1, hit2) => hit1.distance.CompareTo(hit2.distance));

        foreach (RaycastHit hit in hits)
        {
            // Игнорируем системные объекты
            if (hit.collider.name.Contains("Location_Bounds") ||
                hit.collider.name.Contains("Grid") ||
                hit.collider.name.Contains("Terrain"))
                continue;

            Character character = hit.collider.GetComponent<Character>();
            if (character == null)
                character = hit.collider.GetComponentInParent<Character>();

            if (character != null && character.IsEnemyCharacter())
            {
                return character;
            }
        }

        return null;
    }

    /// <summary>
    /// Получить список выделенных союзников
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
    /// Назначить цель для боевых действий
    /// </summary>
    public void AssignCombatTarget(Character attacker, Character target)
    {
        if (attacker == null || target == null)
            return;

        // Останавливаем предыдущие боевые действия
        StopCombat(attacker);

        // Создаем или обновляем данные о боевых действиях
        if (!activeCombatants.ContainsKey(attacker))
        {
            activeCombatants[attacker] = new CombatData();
        }

        CombatData combatData = activeCombatants[attacker];
        combatData.target = target;
        combatData.isPursuing = true;
        combatData.originalPosition = attacker.transform.position;

        // Запускаем корутину боевых действий
        combatData.combatCoroutine = StartCoroutine(CombatBehavior(attacker, combatData));

        if (debugMode)
            Debug.Log($"[CombatSystem] {attacker.GetFullName()} assigned to attack {target.GetFullName()}");
    }

    /// <summary>
    /// Остановить боевые действия персонажа
    /// </summary>
    public void StopCombat(Character attacker)
    {
        if (activeCombatants.ContainsKey(attacker))
        {
            CombatData combatData = activeCombatants[attacker];

            // Останавливаем корутины
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

            // Останавливаем движение
            CharacterMovement movement = attacker.GetComponent<CharacterMovement>();
            if (movement != null)
            {
                movement.StopMovement();
            }

            activeCombatants.Remove(attacker);

            if (debugMode)
                Debug.Log($"[CombatSystem] Stopped combat for {attacker.GetFullName()}");
        }
    }

    /// <summary>
    /// Основная корутина боевого поведения
    /// </summary>
    IEnumerator CombatBehavior(Character attacker, CombatData combatData)
    {
        CharacterMovement movement = attacker.GetComponent<CharacterMovement>();
        CharacterAI ai = attacker.GetComponent<CharacterAI>();

        if (movement == null)
        {
            movement = attacker.gameObject.AddComponent<CharacterMovement>();
        }

        while (combatData.target != null && combatData.target.GetHealth() > 0)
        {
            float distanceToTarget = Vector3.Distance(attacker.transform.position, combatData.target.transform.position);

            // Проверяем, находится ли цель в дистанции атаки
            if (distanceToTarget <= attackRange)
            {
                // Цель в дистанции атаки
                combatData.isPursuing = false;

                // Проверяем кулдаун атаки
                if (Time.time - combatData.lastAttackTime >= attackCooldown && !combatData.isAttacking)
                {
                    // Выполняем атаку
                    yield return StartCoroutine(PerformAttack(attacker, combatData));
                }
                else
                {
                    // Ждем окончания кулдауна
                    yield return new WaitForSeconds(0.1f);
                }
            }
            else if (distanceToTarget <= pursuitRange)
            {
                // Цель в дистанции преследования - двигаемся к ней
                if (!combatData.isPursuing || !movement.IsMoving())
                {
                    Vector3 targetPosition = GetNearestAttackPosition(combatData.target);
                    movement.MoveTo(targetPosition);
                    combatData.isPursuing = true;

                    // Уведомляем AI о движении
                    if (ai != null)
                    {
                        ai.OnPlayerInitiatedMovement();
                    }

                    if (debugMode)
                        Debug.Log($"[CombatSystem] {attacker.GetFullName()} pursuing {combatData.target.GetFullName()}");
                }

                yield return new WaitForSeconds(0.2f);
            }
            else
            {
                // Цель слишком далеко - прекращаем преследование
                if (debugMode)
                    Debug.Log($"[CombatSystem] Target too far away. Stopping combat for {attacker.GetFullName()}");
                break;
            }

            yield return null;
        }

        // Боевые действия завершены
        if (debugMode)
            Debug.Log($"[CombatSystem] Combat ended for {attacker.GetFullName()}");

        activeCombatants.Remove(attacker);
    }

    /// <summary>
    /// Выполнить атаку
    /// </summary>
    IEnumerator PerformAttack(Character attacker, CombatData combatData)
    {
        combatData.isAttacking = true;
        combatData.lastAttackTime = Time.time;

        if (debugMode)
            Debug.Log($"[CombatSystem] {attacker.GetFullName()} attacking {combatData.target.GetFullName()}");

        // Запускаем анимацию атаки
        combatData.attackAnimationCoroutine = StartCoroutine(AttackAnimation(attacker, combatData.target));

        // Ждем завершения анимации атаки
        yield return combatData.attackAnimationCoroutine;

        // Наносим урон
        if (combatData.target != null && combatData.target.GetHealth() > 0)
        {
            combatData.target.TakeDamage(attackDamage);

            // Показываем индикацию урона
            StartCoroutine(ShowDamageIndication(combatData.target));

            if (debugMode)
                Debug.Log($"[CombatSystem] {attacker.GetFullName()} dealt {attackDamage} damage to {combatData.target.GetFullName()} (HP: {combatData.target.GetHealth()})");
        }

        combatData.isAttacking = false;
        combatData.attackAnimationCoroutine = null;
    }

    /// <summary>
    /// Анимация атаки (прыжок в клетку врага и возврат)
    /// </summary>
    IEnumerator AttackAnimation(Character attacker, Character target)
    {
        Vector3 startPosition = attacker.transform.position;
        Vector3 targetDirection = (target.transform.position - startPosition).normalized;
        Vector3 lungePosition = startPosition + targetDirection * lungeDistance;

        // Фаза 1: Прыжок к врагу
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

        // Небольшая пауза в точке атаки
        yield return new WaitForSeconds(0.1f);

        // Фаза 2: Возврат на исходную позицию
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
    /// Показать индикацию урона (мигание материала)
    /// </summary>
    IEnumerator ShowDamageIndication(Character target)
    {
        if (target == null || damageIndicatorMaterial == null)
            yield break;

        // Получаем все рендереры цели
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        Dictionary<Renderer, Material> originalMaterials = new Dictionary<Renderer, Material>();

        // Сохраняем оригинальные материалы и применяем материал урона
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null && renderer.material != null)
            {
                originalMaterials[renderer] = renderer.material;
                renderer.material = damageIndicatorMaterial;
            }
        }

        // Ждем указанное время
        yield return new WaitForSeconds(damageFlashDuration);

        // Восстанавливаем оригинальные материалы
        foreach (var kvp in originalMaterials)
        {
            if (kvp.Key != null)
            {
                kvp.Key.material = kvp.Value;
            }
        }
    }

    /// <summary>
    /// Получить ближайшую позицию для атаки цели
    /// </summary>
    Vector3 GetNearestAttackPosition(Character target)
    {
        if (gridManager == null)
        {
            // Fallback: позиция рядом с целью
            return target.transform.position + Vector3.right * attackRange;
        }

        Vector2Int targetGridPos = gridManager.WorldToGrid(target.transform.position);

        // Ищем ближайшую свободную клетку в дистанции атаки
        Vector2Int[] offsets = {
            new Vector2Int(1, 0),   // право
            new Vector2Int(-1, 0),  // лево
            new Vector2Int(0, 1),   // верх
            new Vector2Int(0, -1),  // низ
            new Vector2Int(1, 1),   // право-верх
            new Vector2Int(-1, 1),  // лево-верх
            new Vector2Int(1, -1),  // право-низ
            new Vector2Int(-1, -1)  // лево-низ
        };

        foreach (Vector2Int offset in offsets)
        {
            Vector2Int attackGridPos = targetGridPos + offset;

            if (gridManager.IsValidGridPosition(attackGridPos))
            {
                var cell = gridManager.GetCell(attackGridPos);
                if (cell == null || !cell.isOccupied)
                {
                    return gridManager.GridToWorld(attackGridPos);
                }
            }
        }

        // Если не нашли свободную клетку, возвращаем позицию справа от цели
        Vector2Int fallbackPos = targetGridPos + Vector2Int.right;
        return gridManager.GridToWorld(fallbackPos);
    }

    /// <summary>
    /// Обновление состояний всех сражающихся персонажей
    /// </summary>
    void UpdateCombatStates()
    {
        List<Character> toRemove = new List<Character>();

        foreach (var kvp in activeCombatants)
        {
            Character attacker = kvp.Key;
            CombatData combatData = kvp.Value;

            // Проверяем, что персонажи еще существуют
            if (attacker == null || combatData.target == null || combatData.target.GetHealth() <= 0)
            {
                toRemove.Add(attacker);
                continue;
            }

            // Проверяем, не слишком ли далеко цель
            float distance = Vector3.Distance(attacker.transform.position, combatData.target.transform.position);
            if (distance > pursuitRange)
            {
                toRemove.Add(attacker);
            }
        }

        // Удаляем завершенные боевые действия
        foreach (Character attacker in toRemove)
        {
            StopCombat(attacker);
        }
    }

    /// <summary>
    /// Проверить, участвует ли персонаж в бою
    /// </summary>
    public bool IsInCombat(Character character)
    {
        return activeCombatants.ContainsKey(character);
    }

    /// <summary>
    /// Получить цель персонажа в бою
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
    /// Получить информацию о боевых действиях для отладки
    /// </summary>
    public void LogCombatInfo()
    {
        Debug.Log($"[CombatSystem] Active combatants: {activeCombatants.Count}");

        foreach (var kvp in activeCombatants)
        {
            Character attacker = kvp.Key;
            CombatData data = kvp.Value;

            Debug.Log($"  {attacker.GetFullName()} -> {data.target?.GetFullName() ?? "NULL"} " +
                     $"(Pursuing: {data.isPursuing}, Attacking: {data.isAttacking})");
        }
    }

    void OnDestroy()
    {
        // Останавливаем все боевые действия
        List<Character> allCombatants = new List<Character>(activeCombatants.Keys);
        foreach (Character attacker in allCombatants)
        {
            StopCombat(attacker);
        }

        activeCombatants.Clear();
    }

    void OnDrawGizmosSelected()
    {
        // Показываем дистанции атаки и преследования в Scene view
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pursuitRange);

        // Показываем линии к целям
        foreach (var kvp in activeCombatants)
        {
            Character attacker = kvp.Key;
            Character target = kvp.Value.target;

            if (attacker != null && target != null)
            {
                Gizmos.color = kvp.Value.isAttacking ? Color.red : new Color(1f, 0.5f, 0f); // оранжевый
                Gizmos.DrawLine(attacker.transform.position, target.transform.position);
            }
        }
    }
}