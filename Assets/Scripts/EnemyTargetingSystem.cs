using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Система указания целей для дружественных юнитов
/// Позволяет выделенным союзникам следовать за врагами при клике по ним
/// </summary>
public class EnemyTargetingSystem : MonoBehaviour
{
    [Header("Targeting Settings")]
    public Color enemyHighlightColor = Color.red;
    public float followDistance = 1.5f; // Расстояние следования (соседняя клетка)
    public float updateInterval = 0.5f; // Частота обновления следования

    [Header("Visual Indicators")]
    public GameObject targetIndicatorPrefab;
    public Color targetIndicatorColor = Color.red;

    [Header("Debug")]
    public bool debugMode = true;

    // Компоненты
    private SelectionManager selectionManager;
    private MovementController movementController;
    private GridManager gridManager;
    private Camera playerCamera;

    // Состояние системы
    private bool isTargetingMode = false;
    private Character hoveredEnemy = null;
    private Dictionary<MeshRenderer, Material> originalEnemyMaterials = new Dictionary<MeshRenderer, Material>();
    private Dictionary<MeshRenderer, Material> highlightMaterials = new Dictionary<MeshRenderer, Material>();
    private List<MeshRenderer> currentHighlightedRenderers = new List<MeshRenderer>();

    // Система следования
    private Dictionary<Character, Character> activeTargets = new Dictionary<Character, Character>(); // следующий -> цель
    private Dictionary<Character, GameObject> targetIndicators = new Dictionary<Character, GameObject>();

    void Awake()
    {
        DebugLogger.Log(DebugLogger.LogCategory.Targeting, "EnemyTargetingSystem Awake started");

        selectionManager = FindObjectOfType<SelectionManager>();
        movementController = FindObjectOfType<MovementController>();
        gridManager = FindObjectOfType<GridManager>();
        playerCamera = Camera.main;

        DebugLogger.Log(DebugLogger.LogCategory.Targeting,
            $"Component references: " +
            $"SelectionManager={selectionManager != null}, " +
            $"MovementController={movementController != null}, " +
            $"GridManager={gridManager != null}, " +
            $"Camera={playerCamera != null}");

        CreateTargetIndicatorPrefab();
        DebugLogger.Log(DebugLogger.LogCategory.Targeting, "EnemyTargetingSystem Awake completed");
    }

    void Start()
    {
        DebugLogger.Log(DebugLogger.LogCategory.Targeting, "EnemyTargetingSystem Start began");

        // Подписываемся на изменения выделения
        if (selectionManager != null)
        {
            selectionManager.OnSelectionChanged += OnSelectionChanged;
            DebugLogger.Log(DebugLogger.LogCategory.Targeting, "Subscribed to SelectionManager.OnSelectionChanged");
        }
        else
        {
            DebugLogger.LogError(DebugLogger.LogCategory.Targeting, "SelectionManager is NULL! Cannot subscribe to selection events.");
        }

        // Запускаем корутину обновления следования
        InvokeRepeating(nameof(UpdateFollowing), updateInterval, updateInterval);
        DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Started UpdateFollowing with interval {updateInterval}s");

        DebugLogger.Log(DebugLogger.LogCategory.Targeting, "EnemyTargetingSystem Start completed");
    }

    void Update()
    {
        // Подробное логирование работы системы наведения
        if (Time.frameCount % 60 == 0) // Каждую секунду при 60 FPS
        {
            var selectedAllies = GetSelectedAllies();
            DebugLogger.Log(DebugLogger.LogCategory.Targeting,
                $"Update tick: Selected allies: {selectedAllies.Count}, Targeting mode: {isTargetingMode}, Hovered enemy: {hoveredEnemy?.GetFullName() ?? "None"}");
        }

        HandleTargetingInput();
        HandleEnemyHover();
    }

    /// <summary>
    /// Создание префаба индикатора цели
    /// </summary>
    void CreateTargetIndicatorPrefab()
    {
        if (targetIndicatorPrefab == null)
        {
            GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            prefab.name = "TargetIndicator";
            prefab.transform.localScale = new Vector3(1.2f, 0.1f, 1.2f);

            // Убираем коллайдер
            DestroyImmediate(prefab.GetComponent<Collider>());

            // Настраиваем материал
            Renderer renderer = prefab.GetComponent<Renderer>();
            Material material = new Material(Shader.Find("Standard"));
            material.color = targetIndicatorColor;
            material.SetFloat("_Mode", 3); // Transparent mode
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            renderer.material = material;

            prefab.SetActive(false);
            targetIndicatorPrefab = prefab;
        }
    }

    /// <summary>
    /// Обработка ввода для указания цели
    /// </summary>
    void HandleTargetingInput()
    {
        // Проверяем, есть ли выделенные союзники
        var selectedAllies = GetSelectedAllies();
        bool hasSelectedAllies = selectedAllies.Count > 0;

        // Включаем режим указания цели только при наличии выделенных союзников
        if (hasSelectedAllies != isTargetingMode)
        {
            isTargetingMode = hasSelectedAllies;
            DebugLogger.Log(DebugLogger.LogCategory.Targeting,
                $"Targeting mode {(isTargetingMode ? "ENABLED" : "DISABLED")}. Selected allies: {selectedAllies.Count}");
        }

        // Обрабатываем клик по врагу только в режиме указания цели
        if (isTargetingMode && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))) // ЛКМ или ПКМ
        {
            string clickType = Input.GetMouseButtonDown(0) ? "LMB" : "RMB";
            DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"{clickType} clicked in targeting mode, checking for enemy under mouse...");

            Character clickedEnemy = GetEnemyUnderMouse();
            if (clickedEnemy != null)
            {
                DebugLogger.Log(DebugLogger.LogCategory.Targeting,
                    $"✓ {clickType} clicked on enemy: {clickedEnemy.GetFullName()}, assigning to {selectedAllies.Count} allies");

                AssignTargetToAllies(selectedAllies, clickedEnemy);
            }
            else
            {
                DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"✗ {clickType} click in targeting mode but no enemy under mouse");
            }
        }
    }

    /// <summary>
    /// Обработка подсветки врагов при наведении
    /// </summary>
    void HandleEnemyHover()
    {
        if (!isTargetingMode)
        {
            // Если не в режиме указания цели, убираем подсветку
            if (hoveredEnemy != null)
            {
                DebugLogger.Log(DebugLogger.LogCategory.Targeting, "Targeting mode disabled, removing enemy highlight");
                RemoveEnemyHighlight(hoveredEnemy);
                hoveredEnemy = null;
            }
            return;
        }

        // Проверяем есть ли враг под мышью
        Character currentEnemy = GetEnemyUnderMouse();

        // Логируем только когда что-то меняется
        if (currentEnemy != hoveredEnemy)
        {
            DebugLogger.Log(DebugLogger.LogCategory.Targeting,
                $"Enemy hover changed: from {hoveredEnemy?.GetFullName() ?? "None"} to {currentEnemy?.GetFullName() ?? "None"}");

            // Убираем подсветку с предыдущего врага
            if (hoveredEnemy != null)
            {
                DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Removing highlight from {hoveredEnemy.GetFullName()}");
                RemoveEnemyHighlight(hoveredEnemy);
            }

            // Добавляем подсветку новому врагу
            if (currentEnemy != null)
            {
                DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Adding highlight to {currentEnemy.GetFullName()}");
                AddEnemyHighlight(currentEnemy);
            }

            hoveredEnemy = currentEnemy;
        }
    }

    /// <summary>
    /// Получить врага под курсором мыши
    /// </summary>
    Character GetEnemyUnderMouse()
    {
        if (playerCamera == null)
        {
            DebugLogger.LogError(DebugLogger.LogCategory.Targeting, "Player camera is NULL!");
            return null;
        }

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);

        DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Mouse raycast found {hits.Length} hits at position {Input.mousePosition}");

        // Сортируем хиты по расстоянию (ближайшие первые)
        System.Array.Sort(hits, (hit1, hit2) => hit1.distance.CompareTo(hit2.distance));

        foreach (RaycastHit hit in hits)
        {
            // Игнорируем Location_Bounds и другие системные объекты
            if (hit.collider.name.Contains("Location_Bounds") ||
                hit.collider.name.Contains("Grid") ||
                hit.collider.name.Contains("Terrain"))
            {
                DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Skipping system object: {hit.collider.name}");
                continue;
            }

            DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Checking hit object: {hit.collider.name} at distance: {hit.distance:F2}");

            // Сначала проверяем непосредственно в коллайдере
            Character character = hit.collider.GetComponent<Character>();

            // Если не нашли, ищем в родительских объектах
            if (character == null)
            {
                character = hit.collider.GetComponentInParent<Character>();
                if (character != null)
                {
                    DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Found Character in parent: {character.name}");
                }
            }
            else
            {
                DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Found Character directly: {character.name}");
            }

            // Если все еще не нашли, ищем в дочерних объектах
            if (character == null)
            {
                Transform parent = hit.collider.transform;
                while (parent != null && character == null)
                {
                    character = parent.GetComponent<Character>();
                    parent = parent.parent;
                }
                if (character != null)
                {
                    DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Found Character in hierarchy: {character.name}");
                }
            }

            if (character != null)
            {
                DebugLogger.Log(DebugLogger.LogCategory.Targeting,
                    $"Character {character.GetFullName()} - Faction: {character.GetFaction()}, IsEnemy: {character.IsEnemyCharacter()}");

                if (character.IsEnemyCharacter())
                {
                    DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"✓ Enemy found under mouse: {character.GetFullName()}");
                    return character;
                }
                else
                {
                    DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"✗ Character {character.GetFullName()} is not an enemy (faction: {character.GetFaction()})");
                }
            }
            else
            {
                DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"No Character component found on {hit.collider.name}");
            }
        }

        // Дополнительно попробуем проверить все Character'ы в сцене и их расстояние до луча
        Character[] allCharacters = FindObjectsOfType<Character>();
        DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Fallback: Checking {allCharacters.Length} characters in scene for proximity to mouse ray");

        float minDistance = float.MaxValue;
        Character closestEnemy = null;

        foreach (Character character in allCharacters)
        {
            if (character.IsEnemyCharacter())
            {
                float distance = Vector3.Cross(ray.direction, character.transform.position - ray.origin).magnitude;
                if (distance < 2f && distance < minDistance) // В пределах 2 единиц от луча
                {
                    minDistance = distance;
                    closestEnemy = character;
                }
            }
        }

        if (closestEnemy != null)
        {
            DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Fallback found enemy: {closestEnemy.GetFullName()} at distance {minDistance:F2}");
            return closestEnemy;
        }

        DebugLogger.Log(DebugLogger.LogCategory.Targeting, "No enemy found under mouse");
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
    /// Назначить цель союзникам
    /// </summary>
    void AssignTargetToAllies(List<Character> allies, Character target)
    {
        DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"=== ASSIGNING TARGET ===");
        DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Target: {target.GetFullName()} (Faction: {target.GetFaction()})");
        DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Allies: {allies.Count}");

        if (allies.Count == 0)
        {
            DebugLogger.LogError(DebugLogger.LogCategory.Targeting, "No allies to assign target to!");
            return;
        }

        foreach (Character ally in allies)
        {
            DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Processing ally: {ally.GetFullName()}");

            // Останавливаем предыдущее следование
            StopFollowing(ally);

            // Назначаем новую цель
            activeTargets[ally] = target;
            DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"✓ {ally.GetFullName()} assigned to target {target.GetFullName()}");

            // Создаем индикатор цели
            CreateTargetIndicator(target);

            // Начинаем следование
            StartFollowing(ally, target);

            DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"✓ {ally.GetFullName()} started following {target.GetFullName()}");
        }

        DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"=== TARGET ASSIGNMENT COMPLETE ===");
        DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Active targets count: {activeTargets.Count}");
    }

    /// <summary>
    /// Начать следование за целью
    /// </summary>
    void StartFollowing(Character follower, Character target)
    {
        DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"StartFollowing: {follower.GetFullName()} -> {target.GetFullName()}");

        // Находим ближайшую позицию рядом с целью
        Vector3 followPosition = GetFollowPosition(target);
        DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Follow position calculated: {followPosition}");

        // Отправляем союзника к цели
        CharacterMovement movement = follower.GetComponent<CharacterMovement>();
        if (movement != null)
        {
            DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"CharacterMovement found, sending {follower.GetFullName()} to {followPosition}");
            movement.MoveTo(followPosition);

            // Уведомляем AI о движении, инициированном игроком
            CharacterAI ai = follower.GetComponent<CharacterAI>();
            if (ai != null)
            {
                DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Notifying AI for {follower.GetFullName()} about player-initiated movement");
                ai.OnPlayerInitiatedMovement();
            }
            else
            {
                DebugLogger.LogWarning(DebugLogger.LogCategory.Targeting, $"No CharacterAI found on {follower.GetFullName()}");
            }
        }
        else
        {
            DebugLogger.LogError(DebugLogger.LogCategory.Targeting, $"No CharacterMovement found on {follower.GetFullName()}! Cannot start following.");

            // Добавляем компонент движения если его нет
            movement = follower.gameObject.AddComponent<CharacterMovement>();
            DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Added CharacterMovement to {follower.GetFullName()}, trying again...");
            movement.MoveTo(followPosition);
        }
    }

    /// <summary>
    /// Остановить следование
    /// </summary>
    void StopFollowing(Character follower)
    {
        if (activeTargets.ContainsKey(follower))
        {
            Character oldTarget = activeTargets[follower];
            activeTargets.Remove(follower);

            // Удаляем индикатор цели если больше никто не следует за этой целью
            if (!IsTargetBeingFollowed(oldTarget))
            {
                RemoveTargetIndicator(oldTarget);
            }

            if (debugMode)
            {
                Debug.Log($"[TARGETING] {follower.GetFullName()} stopped following {oldTarget.GetFullName()}");
            }
        }
    }

    /// <summary>
    /// Получить позицию для следования рядом с целью
    /// </summary>
    Vector3 GetFollowPosition(Character target)
    {
        if (gridManager == null)
        {
            return target.transform.position + Vector3.right * followDistance;
        }

        Vector2Int targetGridPos = gridManager.WorldToGrid(target.transform.position);

        // Ищем ближайшую свободную клетку рядом с целью
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
            Vector2Int followGridPos = targetGridPos + offset;

            if (gridManager.IsValidGridPosition(followGridPos))
            {
                var cell = gridManager.GetCell(followGridPos);
                if (cell == null || !cell.isOccupied)
                {
                    return gridManager.GridToWorld(followGridPos);
                }
            }
        }

        // Если не нашли свободную клетку, возвращаем позицию справа от цели
        Vector2Int fallbackPos = targetGridPos + Vector2Int.right;
        return gridManager.GridToWorld(fallbackPos);
    }

    /// <summary>
    /// Обновление следования (вызывается периодически)
    /// </summary>
    void UpdateFollowing()
    {
        if (activeTargets.Count == 0)
        {
            // Нет активных целей для обновления
            return;
        }

        DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"UpdateFollowing: Processing {activeTargets.Count} active targets");

        List<Character> followersToRemove = new List<Character>();

        foreach (var kvp in activeTargets)
        {
            Character follower = kvp.Key;
            Character target = kvp.Value;

            // Проверяем, что оба персонажа еще существуют
            if (follower == null || target == null)
            {
                DebugLogger.LogWarning(DebugLogger.LogCategory.Targeting, $"Removing invalid target: follower={follower?.name}, target={target?.name}");
                followersToRemove.Add(follower);
                continue;
            }

            // Проверяем, не слишком ли далеко союзник от цели
            float distance = Vector3.Distance(follower.transform.position, target.transform.position);
            DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"{follower.GetFullName()} -> {target.GetFullName()}: distance = {distance:F2}");

            if (distance > followDistance * 2f) // Если слишком далеко, обновляем позицию
            {
                CharacterMovement movement = follower.GetComponent<CharacterMovement>();
                if (movement != null)
                {
                    bool isMoving = movement.IsMoving();
                    DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"{follower.GetFullName()} is {(isMoving ? "moving" : "stationary")}");

                    if (!isMoving)
                    {
                        // Союзник не движется и далеко от цели - отправляем его ближе
                        Vector3 newFollowPosition = GetFollowPosition(target);
                        DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Updating follow position for {follower.GetFullName()}: {newFollowPosition}");
                        movement.MoveTo(newFollowPosition);
                    }
                }
                else
                {
                    DebugLogger.LogError(DebugLogger.LogCategory.Targeting, $"No CharacterMovement on {follower.GetFullName()}! Adding component...");
                    movement = follower.gameObject.AddComponent<CharacterMovement>();
                    Vector3 newFollowPosition = GetFollowPosition(target);
                    movement.MoveTo(newFollowPosition);
                }
            }
            else if (distance <= followDistance)
            {
                DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"✓ {follower.GetFullName()} is close enough to {target.GetFullName()}");
            }
        }

        // Удаляем недействительные цели
        foreach (Character follower in followersToRemove)
        {
            if (follower != null)
            {
                StopFollowing(follower);
            }
            else
            {
                // Удаляем null ключи из словаря
                if (activeTargets.ContainsKey(follower))
                {
                    activeTargets.Remove(follower);
                }
            }
        }

        // Обновляем позиции индикаторов целей
        UpdateTargetIndicators();
    }

    /// <summary>
    /// Добавить подсветку врагу (работает как SelectionManager hover)
    /// </summary>
    void AddEnemyHighlight(Character enemy)
    {
        if (enemy == null) return;

        DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Adding highlight to {enemy.GetFullName()}");

        // Получаем все MeshRenderer'ы в объекте и его детях (как в SelectionManager)
        MeshRenderer[] renderers = enemy.GetComponentsInChildren<MeshRenderer>();

        if (renderers.Length == 0)
        {
            DebugLogger.LogError(DebugLogger.LogCategory.Targeting, $"No MeshRenderer found for enemy {enemy.GetFullName()}! Cannot highlight.");
            return;
        }

        DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Found {renderers.Length} MeshRenderers for {enemy.GetFullName()}");

        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer == null || renderer.material == null) continue;

            // Сохраняем оригинальный материал
            if (!originalEnemyMaterials.ContainsKey(renderer))
            {
                originalEnemyMaterials[renderer] = renderer.material;
                DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Stored original material for {renderer.name}: {renderer.material.name}");
            }

            // Создаем материал подсветки
            if (!highlightMaterials.ContainsKey(renderer))
            {
                Material highlightMat = new Material(originalEnemyMaterials[renderer]);
                highlightMat.color = enemyHighlightColor;
                highlightMaterials[renderer] = highlightMat;
                DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Created highlight material for {renderer.name} with color: {enemyHighlightColor}");
            }

            renderer.material = highlightMaterials[renderer];
            currentHighlightedRenderers.Add(renderer);
            DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Applied highlight material to {renderer.name}");
        }

        DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"✓ Highlight applied to {enemy.GetFullName()} with {currentHighlightedRenderers.Count} renderers");
    }

    /// <summary>
    /// Убрать подсветку с врага (работает как SelectionManager EndHover)
    /// </summary>
    void RemoveEnemyHighlight(Character enemy)
    {
        if (enemy == null) return;

        DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Removing highlight from {enemy.GetFullName()}");

        // Восстанавливаем материалы всех подсвеченных рендереров
        int restoredCount = 0;
        for (int i = currentHighlightedRenderers.Count - 1; i >= 0; i--)
        {
            MeshRenderer renderer = currentHighlightedRenderers[i];
            if (renderer == null)
            {
                currentHighlightedRenderers.RemoveAt(i);
                continue;
            }

            // Проверяем принадлежит ли рендерер этому врагу
            if (renderer.transform.IsChildOf(enemy.transform) || renderer.transform == enemy.transform)
            {
                if (originalEnemyMaterials.ContainsKey(renderer))
                {
                    renderer.material = originalEnemyMaterials[renderer];
                    DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Restored original material for {renderer.name}");
                    restoredCount++;
                }

                currentHighlightedRenderers.RemoveAt(i);
            }
        }

        DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"✓ Removed highlight from {enemy.GetFullName()}, restored {restoredCount} renderers");
    }

    /// <summary>
    /// Создать индикатор цели
    /// </summary>
    void CreateTargetIndicator(Character target)
    {
        if (target == null || targetIndicators.ContainsKey(target))
            return;

        GameObject indicator = Instantiate(targetIndicatorPrefab);
        indicator.SetActive(true);

        // Позиционируем под целью
        Vector3 position = target.transform.position;
        position.y = 0.1f; // Немного приподнимаем над землей
        indicator.transform.position = position;

        targetIndicators[target] = indicator;
    }

    /// <summary>
    /// Удалить индикатор цели
    /// </summary>
    void RemoveTargetIndicator(Character target)
    {
        if (target != null && targetIndicators.TryGetValue(target, out GameObject indicator))
        {
            if (indicator != null)
            {
                DestroyImmediate(indicator);
            }
            targetIndicators.Remove(target);
        }
    }

    /// <summary>
    /// Обновить позиции индикаторов целей
    /// </summary>
    void UpdateTargetIndicators()
    {
        List<Character> targetsToRemove = new List<Character>();

        foreach (var kvp in targetIndicators)
        {
            Character target = kvp.Key;
            GameObject indicator = kvp.Value;

            if (target == null || indicator == null)
            {
                targetsToRemove.Add(target);
                continue;
            }

            // Обновляем позицию индикатора
            Vector3 position = target.transform.position;
            position.y = 0.1f;
            indicator.transform.position = position;
        }

        // Удаляем недействительные индикаторы
        foreach (Character target in targetsToRemove)
        {
            RemoveTargetIndicator(target);
        }
    }

    /// <summary>
    /// Проверить, следует ли кто-то за указанной целью
    /// </summary>
    bool IsTargetBeingFollowed(Character target)
    {
        foreach (var kvp in activeTargets)
        {
            if (kvp.Value == target)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Обработчик изменения выделения
    /// </summary>
    void OnSelectionChanged(List<GameObject> selectedObjects)
    {
        // Проверяем, есть ли выделенные союзники
        var selectedAllies = GetSelectedAllies();

        // Если союзники сняты с выделения, останавливаем их следование
        List<Character> followersToStop = new List<Character>();
        foreach (var kvp in activeTargets)
        {
            Character follower = kvp.Key;
            if (!selectedAllies.Contains(follower))
            {
                followersToStop.Add(follower);
            }
        }

        foreach (Character follower in followersToStop)
        {
            StopFollowing(follower);
        }
    }

    /// <summary>
    /// Получить информацию о текущих целях (для отладки)
    /// </summary>
    public void LogActiveTargets()
    {
        DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"=== ACTIVE TARGETS DEBUG ===");
        DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Active targets count: {activeTargets.Count}");

        foreach (var kvp in activeTargets)
        {
            Character follower = kvp.Key;
            Character target = kvp.Value;
            DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"  {follower.GetFullName()} -> {target.GetFullName()}");
        }

        DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Target indicators count: {targetIndicators.Count}");
        DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Original materials count: {originalEnemyMaterials.Count}");
        DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Highlight materials count: {highlightMaterials.Count}");
        DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Currently hovered enemy: {hoveredEnemy?.GetFullName() ?? "None"}");
        DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Targeting mode active: {isTargetingMode}");
    }

    /// <summary>
    /// Тестовый метод для проверки системы наведения
    /// </summary>
    public void TestTargetingSystem()
    {
        DebugLogger.Log(DebugLogger.LogCategory.Targeting, "=== TESTING TARGETING SYSTEM ===");

        // Проверяем компоненты
        DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"SelectionManager: {selectionManager != null}");
        DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"MovementController: {movementController != null}");
        DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"GridManager: {gridManager != null}");
        DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Camera: {playerCamera != null}");

        // Проверяем персонажей в сцене
        Character[] allCharacters = FindObjectsOfType<Character>();
        DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Total characters: {allCharacters.Length}");

        int playerCount = 0, enemyCount = 0;
        foreach (var character in allCharacters)
        {
            if (character.IsPlayerCharacter()) playerCount++;
            else if (character.IsEnemyCharacter()) enemyCount++;

            // Проверяем компоненты каждого персонажа
            bool hasRenderer = character.characterRenderer != null || character.GetComponent<Renderer>() != null;
            bool hasCollider = character.GetComponent<Collider>() != null;
            DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"  {character.GetFullName()} - Renderer: {hasRenderer}, Collider: {hasCollider}");
        }

        DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Player characters: {playerCount}");
        DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Enemy characters: {enemyCount}");

        // Проверяем выделение
        var selectedAllies = GetSelectedAllies();
        DebugLogger.Log(DebugLogger.LogCategory.Targeting, $"Currently selected allies: {selectedAllies.Count}");

        DebugLogger.Log(DebugLogger.LogCategory.Targeting, "=== TARGETING SYSTEM TEST COMPLETE ===");
    }

    void OnDestroy()
    {
        // Отписываемся от событий
        if (selectionManager != null)
        {
            selectionManager.OnSelectionChanged -= OnSelectionChanged;
        }

        // Останавливаем все следования
        List<Character> allFollowers = new List<Character>(activeTargets.Keys);
        foreach (Character follower in allFollowers)
        {
            StopFollowing(follower);
        }

        // Очищаем материалы
        foreach (var material in highlightMaterials.Values)
        {
            if (material != null)
            {
                DestroyImmediate(material);
            }
        }

        // Удаляем все индикаторы
        List<Character> allTargets = new List<Character>(targetIndicators.Keys);
        foreach (Character target in allTargets)
        {
            RemoveTargetIndicator(target);
        }
    }
}