using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Система боевых действий с преследованием врагов и анимацией атак
/// ARCHITECTURE: Наследуется от BaseManager для интеграции с ServiceLocator
/// </summary>
public class CombatSystem : BaseManager
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

    // Словарь зарезервированных боевых позиций (чтобы персонажи не останавливались в одной клетке)
    private Dictionary<Vector2Int, Character> reservedCombatPositions = new Dictionary<Vector2Int, Character>();

    // Класс для хранения данных о боевых действиях персонажа
    private class CombatData
    {
        public Character target;
        public float lastAttackTime;
        public bool isAttacking;
        public bool isPursuing;
        public Vector3 originalPosition;
        public Vector2Int reservedCombatPosition; // Зарезервированная позиция для стрельбы
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
    /// Инициализация менеджера боя через ServiceLocator
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
        // Блокируем ввод если меню паузы активно
        if (!IsGamePaused())
        {
            HandleCombatInput();
        }
        UpdateCombatStates();
    }

    /// <summary>
    /// Проверить находится ли игра на паузе
    /// </summary>
    bool IsGamePaused()
    {
        return GamePauseManager.Instance != null && GamePauseManager.Instance.IsPaused();
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
            return;
        }

        if (!activeCombatants.ContainsKey(attacker))
        {
            return;
        }

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

        // ВАЖНО: Освобождаем зарезервированную боевую позицию
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

        // Останавливаем движение (только если персонаж еще существует)
        if (attacker != null)
        {
            CharacterMovement movement = attacker.GetComponent<CharacterMovement>();
            if (movement != null)
            {
                movement.StopMovement();
            }
        }

        // Убираем индикатор цели, если никто больше не атакует эту цель
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
                    bool blockedByAlly;
                    hasLineOfSight = HasClearLineOfSight(attacker, combatData.target, out blockedByAlly);

                    if (!hasLineOfSight)
                    {
                        // Если заблокировано союзником - продолжаем стрелять (дружественный огонь)
                        // Если заблокировано стеной/препятствием - ищем новую позицию
                        if (!blockedByAlly)
                        {
                            // Линия видимости заблокирована стеной/препятствием - ищем позицию с чистым выстрелом
                            Vector3? clearShotPosition = FindPositionWithClearShot(attacker, combatData.target, currentAttackRange);

                            if (clearShotPosition.HasValue)
                            {
                                // Нашли позицию - двигаемся туда
                                movement.MoveTo(clearShotPosition.Value);
                                combatData.isPursuing = true;

                                // НЕ вызываем OnPlayerInitiatedMovement() - это движение инициировано боевой системой, а не игроком

                                yield return new WaitForSeconds(GameConstants.Combat.COMBAT_FRAME_DELAY);
                                continue; // Пропускаем атаку, продолжаем движение
                            }
                            else
                            {
                                // Не нашли позицию - подходим ближе
                                Vector3 targetPosition = GetNearestAttackPosition(attacker, combatData.target);
                                movement.MoveTo(targetPosition);
                                combatData.isPursuing = true;

                                // НЕ вызываем OnPlayerInitiatedMovement() - это движение инициировано боевой системой, а не игроком

                                yield return new WaitForSeconds(GameConstants.Combat.COMBAT_FRAME_DELAY);
                                continue;
                            }
                        }
                        // Если заблокировано союзником - не двигаемся, продолжаем к атаке (дружественный огонь произойдет)
                    }
                }

                // Линия видимости чистая или оружие ближнего боя - проверяем позицию перед остановкой
                Vector2Int currentGridPos = gridManager.WorldToGrid(attacker.transform.position);
                GridCell currentCell = gridManager.GetCell(currentGridPos);

                // ПРОВЕРКА: Не занята ли текущая клетка или зарезервирована другим персонажем?
                bool cellOccupied = currentCell != null && currentCell.isOccupied;
                bool cellReservedByOther = reservedCombatPositions.ContainsKey(currentGridPos) &&
                                          reservedCombatPositions[currentGridPos] != attacker;

                if (cellOccupied || cellReservedByOther)
                {
                    // Клетка занята! Ищем ближайшую свободную клетку вокруг цели
                    Vector3 freePosition = GetNearestAttackPosition(attacker, combatData.target);
                    Vector2Int freeGridPos = gridManager.WorldToGrid(freePosition);

                    // Если нашли другую клетку - двигаемся к ней
                    if (freeGridPos != currentGridPos)
                    {
                        movement.MoveTo(freePosition);
                        combatData.isPursuing = true;
                        yield return new WaitForSeconds(GameConstants.Combat.COMBAT_FRAME_DELAY);
                        continue; // Продолжаем цикл, не останавливаемся
                    }
                }

                // Клетка свободна - останавливаемся и атакуем
                combatData.isPursuing = false;
                movement.StopMovement();

                // ВАЖНО: Выравниваем позицию по центру клетки для точной стрельбы
                Vector3 cellCenterPosition = gridManager.GridToWorld(currentGridPos);
                attacker.transform.position = cellCenterPosition;

                if (combatData.reservedCombatPosition.x == GameConstants.Combat.INVALID_GRID_POSITION &&
                    combatData.reservedCombatPosition.y == GameConstants.Combat.INVALID_GRID_POSITION)
                {
                    // Еще не резервировали - резервируем текущую позицию
                    combatData.reservedCombatPosition = currentGridPos;
                    reservedCombatPositions[currentGridPos] = attacker;
                }
                else if (combatData.reservedCombatPosition != currentGridPos)
                {
                    // Персонаж сдвинулся - освобождаем старую резервацию и резервируем новую
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
                    yield return new WaitForSeconds(GameConstants.Combat.COMBAT_FRAME_DELAY);
                }
            }
            else
            {
                // Цель за пределами дистанции атаки - преследуем ее независимо от расстояния
                Vector3 targetPosition = GetNearestAttackPosition(attacker, combatData.target);

                // Проверяем, нужно ли обновить маршрут (если цель сдвинулась или персонаж не движется)
                bool needToUpdatePath = !movement.IsMoving() ||
                                       Vector3.Distance(movement.GetDestination(), targetPosition) > GameConstants.Combat.PATH_UPDATE_THRESHOLD;

                if (!combatData.isPursuing || needToUpdatePath)
                {
                    movement.MoveTo(targetPosition);
                    combatData.isPursuing = true;

                    // НЕ вызываем OnPlayerInitiatedMovement() - это движение инициировано боевой системой, а не игроком

                }

                // Освобождаем резервацию если преследуем (не стоим на месте для стрельбы)
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

                yield return new WaitForSeconds(GameConstants.Combat.COMBAT_FRAME_DELAY); // Более частое обновление для лучшего преследования
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
        yield return new WaitForSeconds(attackCooldownTime * GameConstants.Combat.ATTACK_COOLDOWN_MULTIPLIER);

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
        yield return new WaitForSeconds(GameConstants.Combat.ATTACK_PAUSE_DURATION);

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
        Vector3 damagePosition = target.transform.position + Vector3.up * GameConstants.Combat.DAMAGE_TEXT_HEIGHT_OFFSET;
        GameObject damageTextObj = LookAtCamera.CreateBillboardText(
            $"-{damage:F0}",
            damagePosition,
            Color.white,
            12
        );

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
        TextMesh textMesh = damageTextObj.GetComponent<TextMesh>();
        Vector3 startPos = damageTextObj.transform.position;
        Vector3 endPos = new Vector3(startPos.x, GameConstants.Combat.DAMAGE_TEXT_END_HEIGHT, startPos.z);
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
            yield return null;
        }

        // Принудительно уничтожаем через менеджер
        if (damageTextObj != null)
        {
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
            DamageTextManager.Instance.ForceCleanupObject(obj);
        }
    }

    /// <summary>
    /// Проверить есть ли прямая линия видимости между атакующим и целью
    /// </summary>
    /// <param name="attacker">Атакующий персонаж</param>
    /// <param name="target">Целевой персонаж</param>
    /// <param name="blockedByAlly">OUT: true если линия заблокирована союзником (а не стеной)</param>
    bool HasClearLineOfSight(Character attacker, Character target, out bool blockedByAlly)
    {
        blockedByAlly = false;

        if (attacker == null || target == null)
            return false;

        // Позиция выстрела (на высоте персонажа)
        Vector3 shooterPos = attacker.transform.position + Vector3.up * GameConstants.Combat.SHOOTER_HEIGHT_OFFSET;
        Vector3 targetPos = target.transform.position + Vector3.up * GameConstants.Combat.SHOOTER_HEIGHT_OFFSET;

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
                return true;
            }
            else if (hitCharacter != null && hitCharacter.IsAllyWith(attacker))
            {
                // Попали в союзника - линия заблокирована союзником
                blockedByAlly = true;
                return false;
            }
            else
            {
                // Попали в препятствие (стену/препятствие)
                blockedByAlly = false;
                return false;
            }
        }

        // Ничего не попало - линия видимости чистая
        return true;
    }

    /// <summary>
    /// Проверить есть ли прямая линия видимости (старый метод для совместимости)
    /// </summary>
    bool HasClearLineOfSight(Character attacker, Character target)
    {
        bool blockedByAlly;
        return HasClearLineOfSight(attacker, target, out blockedByAlly);
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

        // Проверяем каждую позицию
        foreach (Vector2Int gridPos in searchPositions)
        {
            if (!gridManager.IsValidGridPosition(gridPos))
                continue;

            var cell = gridManager.GetCell(gridPos);
            if (cell != null && cell.isOccupied)
                continue; // Занято

            // Проверяем что клетка не зарезервирована другим персонажем
            if (reservedCombatPositions.ContainsKey(gridPos))
            {
                // Если это наша собственная резервация - можем использовать
                if (attacker != null && reservedCombatPositions[gridPos] == attacker)
                {
                    // Проверяем линию видимости с этой позиции
                    Vector3 worldPos = gridManager.GridToWorld(gridPos);
                    if (CheckLineOfSightFromPosition(worldPos, target))
                    {
                        return worldPos;
                    }
                }
                continue; // Зарезервирована кем-то другим
            }

            Vector3 worldPos2 = gridManager.GridToWorld(gridPos);

            // Проверяем линию видимости с этой позиции
            if (CheckLineOfSightFromPosition(worldPos2, target))
            {
                return worldPos2;
            }
        }

        return null;
    }

    /// <summary>
    /// Проверить линию видимости с заданной позиции до цели
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
                // Попали в цель - линия видимости чистая
                return true;
            }
            else
            {
                // Попали в препятствие
                return false;
            }
        }

        // Нет препятствий - линия видимости чистая
        return true;
    }

    /// <summary>
    /// Получить ближайшую позицию для атаки цели (старый метод для совместимости)
    /// </summary>
    Vector3 GetNearestAttackPosition(Character target)
    {
        return GetNearestAttackPosition(null, target);
    }

    /// <summary>
    /// Получить ближайшую позицию для атаки цели с учетом резерваций
    /// </summary>
    Vector3 GetNearestAttackPosition(Character attacker, Character target)
    {
        if (gridManager == null)
        {
            // Fallback: позиция рядом с целью
            return target.transform.position + Vector3.right * attackRange;
        }

        Vector2Int attackerGridPos = attacker != null ? gridManager.WorldToGrid(attacker.transform.position) : Vector2Int.zero;
        Vector2Int targetGridPos = gridManager.WorldToGrid(target.transform.position);

        // Ищем свободную клетку расширенным поиском (радиус 1, потом радиус 2)
        // Это позволяет найти позиции даже когда ближайшие заняты
        for (int searchRadius = 1; searchRadius <= 3; searchRadius++)
        {
            List<(Vector2Int offset, float distance)> offsetsWithDistance = new List<(Vector2Int, float)>();

            // Генерируем все позиции в текущем радиусе
            for (int x = -searchRadius; x <= searchRadius; x++)
            {
                for (int y = -searchRadius; y <= searchRadius; y++)
                {
                    // Пропускаем позицию цели и внутренние радиусы (уже проверены)
                    if (x == 0 && y == 0) continue;
                    if (searchRadius > 1 && Mathf.Abs(x) < searchRadius && Mathf.Abs(y) < searchRadius) continue;

                    Vector2Int offset = new Vector2Int(x, y);
                    Vector2Int attackGridPos = targetGridPos + offset;

                    float distance = attacker != null ? Vector2Int.Distance(attackerGridPos, attackGridPos) : 0f;
                    offsetsWithDistance.Add((offset, distance));
                }
            }

            // Сортируем по расстоянию от атакующего (ближайшие первые)
            offsetsWithDistance.Sort((a, b) => a.distance.CompareTo(b.distance));

            // Проверяем позиции в порядке близости
            foreach (var (offset, _) in offsetsWithDistance)
            {
                Vector2Int attackGridPos = targetGridPos + offset;

                if (!gridManager.IsValidGridPosition(attackGridPos))
                    continue;

                var cell = gridManager.GetCell(attackGridPos);

                // Проверяем что клетка не занята
                if (cell != null && cell.isOccupied)
                    continue;

                // Проверяем что клетка не зарезервирована другим персонажем
                if (reservedCombatPositions.ContainsKey(attackGridPos))
                {
                    // Если это наша собственная резервация - можем использовать
                    if (attacker != null && reservedCombatPositions[attackGridPos] == attacker)
                    {
                        return gridManager.GridToWorld(attackGridPos);
                    }
                    continue; // Зарезервирована кем-то другим
                }

                // Проверяем что позиция не на линии огня союзников
                Vector3 worldPos = gridManager.GridToWorld(attackGridPos);
                if (IsPositionOnFireLine(worldPos, attacker))
                {
                    continue; // Позиция на линии огня - пропускаем
                }

                // Клетка свободна и безопасна!
                return worldPos;
            }
        }

        // Если даже после расширенного поиска не нашли свободную клетку,
        // возвращаем ближайшую к атакующему позицию (даже если она занята)
        // Система движения сама обработает это
        Vector3 directionToTarget = (target.transform.position - (attacker != null ? attacker.transform.position : Vector3.zero)).normalized;
        Vector3 fallbackPos = target.transform.position - directionToTarget * 2f;
        return fallbackPos;
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

                // Используем максимальную дальность оружия + запас для преследования
                // Но не меньше стандартного pursuitRange
                if (maxWeaponRange > 0)
                {
                    maxPursuitDistance = Mathf.Max(pursuitRange, maxWeaponRange * GameConstants.Combat.PURSUIT_RANGE_MULTIPLIER);
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

    /// <summary>
    /// Завершение работы менеджера боя
    /// </summary>
    protected override void OnManagerShutdown()
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

        // Очищаем все резервации
        reservedCombatPositions.Clear();

        base.OnManagerShutdown();
    }

    /// <summary>
    /// Проверить, находится ли позиция на линии огня союзников
    /// </summary>
    bool IsPositionOnFireLine(Vector3 checkPosition, Character excludeAttacker)
    {
        if (gridManager == null) return false;

        // Проверяем все активные боевые действия
        foreach (var kvp in activeCombatants)
        {
            Character shooter = kvp.Key;
            Character target = kvp.Value.target;

            // Пропускаем проверяемого персонажа
            if (shooter == excludeAttacker || shooter == null || target == null)
                continue;

            // Проверяем только союзников
            if (excludeAttacker != null)
            {
                bool shooterIsPlayer = shooter.IsPlayerCharacter();
                bool excludeIsPlayer = excludeAttacker.IsPlayerCharacter();

                // Проверяем только союзников (оба игроки или оба враги)
                if (shooterIsPlayer != excludeIsPlayer)
                    continue;
            }

            // Получаем линию огня
            Vector3 shooterPos = shooter.transform.position + Vector3.up * GameConstants.Combat.SHOOTER_HEIGHT_OFFSET;
            Vector3 targetPos = target.transform.position + Vector3.up * GameConstants.Combat.SHOOTER_HEIGHT_OFFSET;
            Vector3 checkPos = checkPosition + Vector3.up * GameConstants.Combat.SHOOTER_HEIGHT_OFFSET;

            // Проверяем, находится ли checkPosition близко к линии огня
            Vector3 lineDir = (targetPos - shooterPos).normalized;
            float distanceAlongLine = Vector3.Dot(checkPos - shooterPos, lineDir);

            // Проверяем только если позиция между стрелком и целью
            float lineLength = Vector3.Distance(shooterPos, targetPos);
            if (distanceAlongLine > 0 && distanceAlongLine < lineLength)
            {
                // Вычисляем ближайшую точку на линии
                Vector3 closestPoint = shooterPos + lineDir * distanceAlongLine;
                float distanceToLine = Vector3.Distance(checkPos, closestPoint);

                // Если ближе чем 1.5 метра к линии огня - небезопасно
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
                Vector3 shooterPos = attacker.transform.position + Vector3.up * GameConstants.Combat.SHOOTER_HEIGHT_OFFSET;
                Vector3 targetPos = target.transform.position + Vector3.up * GameConstants.Combat.SHOOTER_HEIGHT_OFFSET;

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
