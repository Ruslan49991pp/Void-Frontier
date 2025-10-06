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
    public bool debugMode = true; // Включаем debug для отслеживания преследования

    [Header("Line of Sight")]
    public LayerMask lineOfSightBlockers = -1; // Слои которые блокируют линию видимости
    public float lineOfSightCheckInterval = 0.5f; // Интервал проверки линии видимости
    public int maxPositionSearchAttempts = 8; // Максимум попыток найти позицию с чистой траекторией

    // Компоненты системы
    private SelectionManager selectionManager;
    private GridManager gridManager;
    private Camera playerCamera;
    private EnemyTargetingSystem enemyTargetingSystem;

    // Отслеживание линии видимости
    private Dictionary<Character, bool> hasLineOfSight = new Dictionary<Character, bool>();

    // Состояние боевых действий
    private Dictionary<Character, CombatData> activeCombatants = new Dictionary<Character, CombatData>();
    private Material damageIndicatorMaterial;

    // Защита от одновременного применения урона к одной цели
    private HashSet<Character> damageIndicationInProgress = new HashSet<Character>();

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
        enemyTargetingSystem = FindObjectOfType<EnemyTargetingSystem>();

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

            Debug.LogWarning($"[CombatSystem] Material '{damageIndicatorMaterialName}' not found. Using fallback red material.");
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

            if (character != null && character.IsEnemyCharacter() && !character.IsDead())
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

    }

    /// <summary>
    /// Остановить боевые действия персонажа (публичный метод)
    /// </summary>
    public void StopCombatForCharacter(Character attacker)
    {
        StopCombat(attacker);
    }

    /// <summary>
    /// Остановить боевые действия персонажа
    /// </summary>
    public void StopCombat(Character attacker)
    {
        // Проверяем что персонаж еще существует
        if (attacker == null)
        {
            Debug.LogWarning($"[CombatSystem] StopCombat called for null attacker");
            return;
        }

        if (!activeCombatants.ContainsKey(attacker))
        {
            Debug.LogWarning($"[CombatSystem] StopCombat called for {attacker.GetFullName()} but they are not in active combatants");
            return;
        }

        CombatData combatData = activeCombatants[attacker];
        Debug.Log($"[CombatSystem] Stopping combat for {attacker.GetFullName()} (was attacking {combatData.target?.GetFullName()})");

        // Останавливаем корутины
        if (combatData.combatCoroutine != null)
        {
            StopCoroutine(combatData.combatCoroutine);
            combatData.combatCoroutine = null;
            Debug.Log($"[CombatSystem] Stopped combat coroutine for {attacker.GetFullName()}");
        }

        if (combatData.attackAnimationCoroutine != null)
        {
            StopCoroutine(combatData.attackAnimationCoroutine);
            combatData.attackAnimationCoroutine = null;
            Debug.Log($"[CombatSystem] Stopped attack animation coroutine for {attacker.GetFullName()}");
        }

        // Останавливаем движение (только если персонаж еще существует)
        if (attacker != null)
        {
            CharacterMovement movement = attacker.GetComponent<CharacterMovement>();
            if (movement != null)
            {
                movement.StopMovement();
                Debug.Log($"[CombatSystem] Stopped movement for {attacker.GetFullName()}");
            }
        }

        // Убираем индикатор цели, если никто больше не атакует эту цель
        Character target = combatData.target;
        activeCombatants.Remove(attacker);
        Debug.Log($"[CombatSystem] Removed {(attacker != null ? attacker.GetFullName() : "destroyed character")} from active combatants");

        if (target != null && enemyTargetingSystem != null)
        {
            // Проверяем, атакует ли кто-то еще эту цель
            bool isTargetStillBeingAttacked = false;
            foreach (var combat in activeCombatants.Values)
            {
                if (combat.target == target)
                {
                    isTargetStillBeingAttacked = true;
                    break;
                }
            }

            // Если никто больше не атакует эту цель, убираем индикатор
            if (!isTargetStillBeingAttacked)
            {
                enemyTargetingSystem.ClearTargetForCharacter(attacker);
            }
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

        while (combatData.target != null && combatData.target.GetHealth() > 0 && !combatData.target.IsDead())
        {
            float distanceToTarget = Vector3.Distance(attacker.transform.position, combatData.target.transform.position);

            // Получаем систему оружия для определения дальности атаки
            WeaponSystem weaponSystem = attacker.GetComponent<WeaponSystem>();
            float currentAttackRange = attackRange; // Дефолтная дальность
            float currentAttackCooldown = attackCooldown; // Дефолтный кулдаун

            if (weaponSystem != null)
            {
                // Выбираем лучшее оружие для текущей дистанции
                weaponSystem.SelectBestWeapon(combatData.target.transform.position, distanceToTarget);

                Weapon currentWeapon = weaponSystem.GetCurrentWeapon();
                if (currentWeapon != null)
                {
                    currentAttackRange = currentWeapon.range;
                    currentAttackCooldown = currentWeapon.GetAttackCooldown();
                }
            }

            // Проверяем, находится ли цель в дистанции атаки
            if (distanceToTarget <= currentAttackRange)
            {
                // Цель в дистанции атаки - проверяем линию видимости для дальнобойного оружия
                Weapon currentWeapon = weaponSystem?.GetCurrentWeapon();
                bool needsLineOfSight = currentWeapon != null && currentWeapon.weaponType == WeaponType.Ranged;
                bool hasLineOfSight = true;

                if (needsLineOfSight)
                {
                    hasLineOfSight = HasClearLineOfSight(attacker, combatData.target);

                    if (!hasLineOfSight)
                    {
                        // Линия видимости заблокирована - ищем позицию с чистым выстрелом
                        Vector3? clearShotPosition = FindPositionWithClearShot(attacker, combatData.target, currentAttackRange);

                        if (clearShotPosition.HasValue)
                        {
                            // Нашли позицию - двигаемся туда
                            if (debugMode)
                                Debug.Log($"[CombatSystem] {attacker.GetFullName()} moving to clear shot position");

                            movement.MoveTo(clearShotPosition.Value);
                            combatData.isPursuing = true;

                            // НЕ вызываем OnPlayerInitiatedMovement() - это движение инициировано боевой системой, а не игроком

                            yield return new WaitForSeconds(0.1f);
                            continue; // Пропускаем атаку, продолжаем движение
                        }
                        else
                        {
                            // Не нашли позицию - подходим ближе
                            if (debugMode)
                                Debug.LogWarning($"[CombatSystem] {attacker.GetFullName()} can't find clear shot, moving closer");

                            Vector3 targetPosition = GetNearestAttackPosition(combatData.target);
                            movement.MoveTo(targetPosition);
                            combatData.isPursuing = true;

                            // НЕ вызываем OnPlayerInitiatedMovement() - это движение инициировано боевой системой, а не игроком

                            yield return new WaitForSeconds(0.1f);
                            continue;
                        }
                    }
                }

                // Линия видимости чистая или оружие ближнего боя - останавливаемся и атакуем
                combatData.isPursuing = false;
                movement.StopMovement();

                // Проверяем кулдаун атаки - атака начинается ТОЛЬКО если не атакуем сейчас И прошел кулдаун
                float timeSinceLastAttack = Time.time - combatData.lastAttackTime;
                if (!combatData.isAttacking && timeSinceLastAttack >= currentAttackCooldown)
                {
                    // Выполняем атаку - блокируем новые атаки пока не завершится
                    yield return StartCoroutine(PerformAttack(attacker, combatData));
                }
                else
                {
                    // Ждем готовности к следующей атаке
                    yield return new WaitForSeconds(0.1f);
                }
            }
            else
            {
                // Цель за пределами дистанции атаки - преследуем ее независимо от расстояния
                Vector3 targetPosition = GetNearestAttackPosition(combatData.target);

                // Проверяем, нужно ли обновить маршрут (если цель сдвинулась или персонаж не движется)
                bool needToUpdatePath = !movement.IsMoving() ||
                                       Vector3.Distance(movement.GetDestination(), targetPosition) > 1.5f;

                if (!combatData.isPursuing || needToUpdatePath)
                {
                    movement.MoveTo(targetPosition);
                    combatData.isPursuing = true;

                    // НЕ вызываем OnPlayerInitiatedMovement() - это движение инициировано боевой системой, а не игроком

                }

                yield return new WaitForSeconds(0.1f); // Более частое обновление для лучшего преследования
            }

            yield return null;
        }

        // Боевые действия завершены

        // Убираем индикатор цели при завершении боя
        Character target = combatData.target;
        activeCombatants.Remove(attacker);

        if (target != null && enemyTargetingSystem != null)
        {
            // Проверяем, атакует ли кто-то еще эту цель
            bool isTargetStillBeingAttacked = false;
            foreach (var combat in activeCombatants.Values)
            {
                if (combat.target == target)
                {
                    isTargetStillBeingAttacked = true;
                    break;
                }
            }

            // Если никто больше не атакует эту цель, убираем индикатор
            if (!isTargetStillBeingAttacked)
            {
                enemyTargetingSystem.ClearTargetForCharacter(attacker);
            }
        }
    }

    /// <summary>
    /// Выполнить атаку
    /// </summary>
    IEnumerator PerformAttack(Character attacker, CombatData combatData)
    {
        // Получаем систему оружия атакующего
        WeaponSystem weaponSystem = attacker.GetComponent<WeaponSystem>();
        if (weaponSystem == null)
        {
            // Если нет системы оружия, используем старую систему
            yield return StartCoroutine(PerformLegacyAttack(attacker, combatData));
            yield break;
        }

        float distanceToTarget = Vector3.Distance(attacker.transform.position, combatData.target.transform.position);

        // Используем систему оружия для атаки
        combatData.isAttacking = true;

        // Система оружия сама выберет подходящее оружие и выполнит атаку
        weaponSystem.AttackTarget(combatData.target);

        // Получаем время атаки от текущего оружия
        Weapon currentWeapon = weaponSystem.GetCurrentWeapon();
        float attackCooldownTime = currentWeapon != null ? currentWeapon.GetAttackCooldown() : attackCooldown;

        // Ждем завершения атаки
        yield return new WaitForSeconds(attackCooldownTime * 0.5f); // Половина времени для анимации

        // ВАЖНО: Устанавливаем время последней атаки ПОСЛЕ завершения всей атаки
        combatData.lastAttackTime = Time.time;
        combatData.isAttacking = false;
        combatData.attackAnimationCoroutine = null;
    }

    /// <summary>
    /// Выполнить атаку по старой системе (для совместимости)
    /// </summary>
    IEnumerator PerformLegacyAttack(Character attacker, CombatData combatData)
    {
        // Проверяем, что цель все еще в дистанции атаки перед началом
        float distanceCheck = Vector3.Distance(attacker.transform.position, combatData.target.transform.position);
        if (distanceCheck > attackRange)
        {
            yield break;
        }

        combatData.isAttacking = true;

        // Поворачиваем атакующего лицом к цели
        yield return StartCoroutine(RotateTowardsTarget(attacker, combatData.target));

        // Запускаем анимацию атаки
        combatData.attackAnimationCoroutine = StartCoroutine(AttackAnimation(attacker, combatData.target));

        // Ждем завершения анимации атаки
        yield return combatData.attackAnimationCoroutine;

        // Наносим урон
        if (combatData.target != null && combatData.target.GetHealth() > 0 && !combatData.target.IsDead())
        {
            combatData.target.TakeDamage(attackDamage);

            // Показываем индикацию урона
            StartCoroutine(ShowDamageIndication(combatData.target));

            // Показываем текст урона
            ShowDamageText(combatData.target, attackDamage);
        }

        // ВАЖНО: Устанавливаем время последней атаки ПОСЛЕ завершения всей атаки
        combatData.lastAttackTime = Time.time;
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
    /// Повернуть персонажа лицом к цели
    /// </summary>
    IEnumerator RotateTowardsTarget(Character attacker, Character target)
    {
        if (attacker == null || target == null)
            yield break;

        Vector3 direction = (target.transform.position - attacker.transform.position).normalized;

        // Убираем компонент Y для поворота только в горизонтальной плоскости
        direction.y = 0;

        if (direction == Vector3.zero)
            yield break;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        float rotationSpeed = 720f; // Градусов в секунду для быстрого поворота во время атаки

        while (Quaternion.Angle(attacker.transform.rotation, targetRotation) > 1f)
        {
            attacker.transform.rotation = Quaternion.RotateTowards(
                attacker.transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
            yield return null;
        }

        // Устанавливаем финальное направление
        attacker.transform.rotation = targetRotation;
    }

    /// <summary>
    /// Показать индикацию урона (мигание материала)
    /// </summary>
    IEnumerator ShowDamageIndication(Character target)
    {
        if (target == null || damageIndicatorMaterial == null)
        {
            Debug.LogError($"[CombatSystem] ShowDamageIndication failed: target={target}, damageIndicatorMaterial={damageIndicatorMaterial}");
            yield break;
        }

        // Защита от одновременного применения урона к одной цели
        if (damageIndicationInProgress.Contains(target))
        {
            yield break;
        }

        damageIndicationInProgress.Add(target);

        // Получаем все рендереры цели
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();

        // Сохраняем оригинальные материалы и создаем массивы с материалом урона
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null && renderer.sharedMaterials != null && renderer.sharedMaterials.Length > 0)
            {
                // Сохраняем все оригинальные материалы
                originalMaterials[renderer] = (Material[])renderer.sharedMaterials.Clone();

                // Создаем массив материалов урона (заменяем все материалы на материал урона)
                Material[] damageArray = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < damageArray.Length; i++)
                {
                    damageArray[i] = damageIndicatorMaterial;
                }

                // Применяем материалы урона
                renderer.sharedMaterials = damageArray;
            }
        }

        // Ждем указанное время
        yield return new WaitForSeconds(damageFlashDuration);

        // Восстанавливаем оригинальные материалы
        foreach (var kvp in originalMaterials)
        {
            if (kvp.Key != null && kvp.Value != null)
            {
                kvp.Key.sharedMaterials = kvp.Value;
            }
        }

        // Убираем блокировку
        damageIndicationInProgress.Remove(target);
    }

    /// <summary>
    /// Показать текст урона с поворотом к камере
    /// </summary>
    void ShowDamageText(Character target, float damage)
    {
        if (target == null) return;

        // Создаем объект с текстом урона
        Vector3 damagePosition = target.transform.position + Vector3.up * 1.8f;
        GameObject damageTextObj = LookAtCamera.CreateBillboardText(
            $"-{damage:F0}",
            damagePosition,
            Color.white,
            12
        );

        Debug.Log($"[CombatSystem] Created damage text object: {damageTextObj.name} at position {damageTextObj.transform.position}");

        // Анимируем текст - СТРОГО 1 секунда
        Coroutine animationCoroutine = StartCoroutine(AnimateLegacyDamageText(damageTextObj, 1.0f));

        // Регистрируем в менеджере для отслеживания
        DamageTextManager.Instance.RegisterDamageText(damageTextObj, animationCoroutine);

        // ПРИНУДИТЕЛЬНОЕ уничтожение ровно через 1 секунду
        StartCoroutine(ForceCleanupAfterDelay(damageTextObj, 1.0f));
    }

    /// <summary>
    /// Анимация текста урона для старой системы боя
    /// </summary>
    IEnumerator AnimateLegacyDamageText(GameObject damageTextObj, float duration)
    {
        Debug.Log($"[CombatSystem] Starting damage text animation for {damageTextObj.name}, duration: {duration}s");

        TextMesh textMesh = damageTextObj.GetComponent<TextMesh>();
        Vector3 startPos = damageTextObj.transform.position;
        Vector3 endPos = new Vector3(startPos.x, 10f, startPos.z);

        Debug.Log($"[CombatSystem] Animation path: {startPos} -> {endPos}");

        Color startColor = textMesh.color;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;

            // Движение вверх
            damageTextObj.transform.position = Vector3.Lerp(startPos, endPos, t);

            // Плавное исчезновение
            Color color = startColor;
            color.a = 1f - t;
            textMesh.color = color;

            // Увеличение размера в начале
            float scale = 1f + (0.3f * (1f - t));
            damageTextObj.transform.localScale = Vector3.one * scale;

            elapsedTime += Time.deltaTime;

            // Логируем каждые 0.5 секунды
            if (Mathf.FloorToInt(elapsedTime * 2) > Mathf.FloorToInt((elapsedTime - Time.deltaTime) * 2))
            {
                Debug.Log($"[CombatSystem] Animation progress: {t:F2}, position: {damageTextObj.transform.position}, alpha: {color.a:F2}");
            }

            yield return null;
        }

        Debug.Log($"[CombatSystem] Animation completed for {damageTextObj.name}, elapsed time: {elapsedTime:F2}s");

        // Принудительно уничтожаем через менеджер
        if (damageTextObj != null)
        {
            Debug.Log($"[CombatSystem] Requesting cleanup for {damageTextObj.name}");
            DamageTextManager.Instance.ForceCleanupObject(damageTextObj);
        }
    }

    /// <summary>
    /// Принудительная очистка объекта через заданное время
    /// </summary>
    IEnumerator ForceCleanupAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (obj != null)
        {
            Debug.Log($"[CombatSystem] MANDATORY cleanup after {delay}s for {obj.name}");
            DamageTextManager.Instance.ForceCleanupObject(obj);
        }
    }

    /// <summary>
    /// Проверить есть ли прямая линия видимости между атакующим и целью
    /// </summary>
    bool HasClearLineOfSight(Character attacker, Character target)
    {
        if (attacker == null || target == null)
            return false;

        // Позиция выстрела (на высоте персонажа)
        Vector3 shooterPos = attacker.transform.position + Vector3.up * 1f;
        Vector3 targetPos = target.transform.position + Vector3.up * 1f;

        Vector3 direction = targetPos - shooterPos;
        float distance = direction.magnitude;

        // Raycast для проверки препятствий
        RaycastHit hit;
        if (Physics.Raycast(shooterPos, direction.normalized, out hit, distance, lineOfSightBlockers))
        {
            // Проверяем, попали ли мы именно в цель или в препятствие
            Character hitCharacter = hit.collider.GetComponent<Character>();
            if (hitCharacter == null)
                hitCharacter = hit.collider.GetComponentInParent<Character>();

            if (hitCharacter == target)
            {
                // Попали в цель - линия видимости чистая
                if (debugMode)
                    Debug.Log($"[CombatSystem] {attacker.GetFullName()} has CLEAR line of sight to {target.GetFullName()}");
                return true;
            }
            else
            {
                // Попали в препятствие
                if (debugMode)
                    Debug.Log($"[CombatSystem] {attacker.GetFullName()} line of sight BLOCKED by {hit.collider.name} to {target.GetFullName()}");
                return false;
            }
        }

        // Ничего не попало - линия видимости чистая
        if (debugMode)
            Debug.Log($"[CombatSystem] {attacker.GetFullName()} has CLEAR line of sight to {target.GetFullName()} (no obstacles)");
        return true;
    }

    /// <summary>
    /// Найти позицию с чистой линией видимости до цели
    /// </summary>
    Vector3? FindPositionWithClearShot(Character attacker, Character target, float searchRadius)
    {
        if (gridManager == null)
            return null;

        Vector2Int attackerGridPos = gridManager.WorldToGrid(attacker.transform.position);
        Vector2Int targetGridPos = gridManager.WorldToGrid(target.transform.position);

        // Генерируем позиции по кругу вокруг текущей позиции
        List<Vector2Int> searchPositions = new List<Vector2Int>();

        // Начинаем с текущей позиции
        searchPositions.Add(attackerGridPos);

        // Добавляем позиции по радиусу
        int radiusCells = Mathf.CeilToInt(searchRadius);
        for (int radius = 1; radius <= radiusCells; radius++)
        {
            for (int angle = 0; angle < 360; angle += 45) // 8 направлений
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

        // Проверяем каждую позицию
        foreach (Vector2Int gridPos in searchPositions)
        {
            if (!gridManager.IsValidGridPosition(gridPos))
                continue;

            var cell = gridManager.GetCell(gridPos);
            if (cell != null && cell.isOccupied)
                continue; // Занято

            Vector3 worldPos = gridManager.GridToWorld(gridPos);

            // Проверяем линию видимости с этой позиции
            Vector3 shooterPos = worldPos + Vector3.up * 1f;
            Vector3 targetPos = target.transform.position + Vector3.up * 1f;

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
                    // Нашли позицию с чистым выстрелом!
                    if (debugMode)
                        Debug.Log($"[CombatSystem] Found clear shot position for {attacker.GetFullName()} at {worldPos}");
                    return worldPos;
                }
            }
            else
            {
                // Нет препятствий - тоже подходит
                if (debugMode)
                    Debug.Log($"[CombatSystem] Found clear shot position for {attacker.GetFullName()} at {worldPos} (no obstacles)");
                return worldPos;
            }
        }

        if (debugMode)
            Debug.LogWarning($"[CombatSystem] Could not find clear shot position for {attacker.GetFullName()}");
        return null;
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

            // Проверяем, что персонажи еще существуют и атакующий не мертв
            if (attacker == null || combatData.target == null || combatData.target.GetHealth() <= 0 ||
                combatData.target.IsDead() || attacker.IsDead())
            {
                toRemove.Add(attacker);
                continue;
            }

            // Получаем максимальную дальность преследования на основе оружия персонажа
            float maxPursuitDistance = pursuitRange; // Дефолтное значение

            WeaponSystem weaponSystem = attacker.GetComponent<WeaponSystem>();
            if (weaponSystem != null)
            {
                // Получаем все оружие персонажа
                var weapons = weaponSystem.GetAllWeapons();
                float maxWeaponRange = 0f;

                // Находим максимальную дальность среди всего оружия
                foreach (var weapon in weapons)
                {
                    if (weapon.range > maxWeaponRange)
                    {
                        maxWeaponRange = weapon.range;
                    }
                }

                // Используем максимальную дальность оружия + 50% запас для преследования
                // Но не меньше стандартного pursuitRange
                if (maxWeaponRange > 0)
                {
                    maxPursuitDistance = Mathf.Max(pursuitRange, maxWeaponRange * 1.5f);
                }
            }

            // ОТКЛЮЧЕНО: Не проверяем дистанцию - если игрок дал команду атаковать, персонаж должен преследовать цель на любом расстоянии
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
        // Метод оставлен для обратной совместимости, но логи отключены
    }

    void OnDestroy()
    {
        // Останавливаем все боевые действия
        List<Character> allCombatants = new List<Character>(activeCombatants.Keys);
        foreach (Character attacker in allCombatants)
        {
            // Проверяем что персонаж еще существует перед остановкой боя
            if (attacker != null)
            {
                StopCombat(attacker);
            }
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

        // Показываем линии к целям с учетом линии видимости
        foreach (var kvp in activeCombatants)
        {
            Character attacker = kvp.Key;
            Character target = kvp.Value.target;

            if (attacker != null && target != null)
            {
                Vector3 shooterPos = attacker.transform.position + Vector3.up * 1f;
                Vector3 targetPos = target.transform.position + Vector3.up * 1f;

                // Проверяем линию видимости
                bool clearShot = HasClearLineOfSight(attacker, target);

                // Цвет линии зависит от состояния
                if (kvp.Value.isAttacking)
                {
                    Gizmos.color = Color.red; // Атакует
                }
                else if (clearShot)
                {
                    Gizmos.color = Color.green; // Чистая линия видимости
                }
                else
                {
                    Gizmos.color = Color.yellow; // Заблокировано
                }

                Gizmos.DrawLine(shooterPos, targetPos);

                // Рисуем точки на позициях стрелка и цели
                Gizmos.DrawWireSphere(shooterPos, 0.2f);
                Gizmos.DrawWireSphere(targetPos, 0.2f);
            }
        }
    }
}