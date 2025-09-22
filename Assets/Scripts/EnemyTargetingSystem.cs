using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Система указания целей для дружественных юнитов
/// Позволяет выделенным союзникам следовать за врагами при клике по ним
/// </summary>
public class EnemyTargetingSystem : MonoBehaviour
{
    [Header("Targeting Settings")]
    public bool enableEnemyTargeting = false; // Отключаем систему преследования врагов
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


        selectionManager = FindObjectOfType<SelectionManager>();
        movementController = FindObjectOfType<MovementController>();
        gridManager = FindObjectOfType<GridManager>();
        playerCamera = Camera.main;

    
        CreateTargetIndicatorPrefab();

    }

    void Start()
    {


        // Подписываемся на изменения выделения
        if (selectionManager != null)
        {
            selectionManager.OnSelectionChanged += OnSelectionChanged;

        }
        else
        {

        }

        // Запускаем корутину обновления следования
        InvokeRepeating(nameof(UpdateFollowing), updateInterval, updateInterval);



    }

    void Update()
    {
        // Подробное логирование работы системы наведения
        // if (Time.frameCount % 60 == 0) // Каждую секунду при 60 FPS
        // {
        //     var selectedAllies = GetSelectedAllies();
        //         $"Update tick: Selected allies: {selectedAllies.Count}, Targeting mode: {isTargetingMode}, Hovered enemy: {hoveredEnemy?.GetFullName() ?? "None"}");
        // }

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
        // Если система преследования отключена, не обрабатываем ввод
        if (!enableEnemyTargeting)
        {
            // Убеждаемся, что режим указания цели выключен
            if (isTargetingMode)
            {
                isTargetingMode = false;

            }
            return;
        }

        // Проверяем, есть ли выделенные союзники
        var selectedAllies = GetSelectedAllies();
        bool hasSelectedAllies = selectedAllies.Count > 0;

        // Включаем режим указания цели только при наличии выделенных союзников
        if (hasSelectedAllies != isTargetingMode)
        {
            isTargetingMode = hasSelectedAllies;
            //     $"Targeting mode {(isTargetingMode ? "ENABLED" : "DISABLED")}. Selected allies: {selectedAllies.Count}");
        }

        // Обрабатываем клик по врагу только в режиме указания цели
        if (isTargetingMode && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))) // ЛКМ или ПКМ
        {
            string clickType = Input.GetMouseButtonDown(0) ? "LMB" : "RMB";


            Character clickedEnemy = GetEnemyUnderMouse();
            if (clickedEnemy != null)
            {
                //     $"✓ {clickType} clicked on enemy: {clickedEnemy.GetFullName()}, assigning to {selectedAllies.Count} allies");

                AssignTargetToAllies(selectedAllies, clickedEnemy);
            }
            else
            {

            }
        }
    }

    /// <summary>
    /// Обработка подсветки врагов при наведении
    /// </summary>
    void HandleEnemyHover()
    {
        // Если система преследования отключена, не показываем подсветку
        if (!enableEnemyTargeting || !isTargetingMode)
        {
            // Если не в режиме указания цели, убираем подсветку
            if (hoveredEnemy != null)
            {

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
            //     $"Enemy hover changed: from {hoveredEnemy?.GetFullName() ?? "None"} to {currentEnemy?.GetFullName() ?? "None"}");

            // Убираем подсветку с предыдущего врага
            if (hoveredEnemy != null)
            {

                RemoveEnemyHighlight(hoveredEnemy);
            }

            // Добавляем подсветку новому врагу
            if (currentEnemy != null)
            {

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

            return null;
        }

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);



        // Сортируем хиты по расстоянию (ближайшие первые)
        System.Array.Sort(hits, (hit1, hit2) => hit1.distance.CompareTo(hit2.distance));

        foreach (RaycastHit hit in hits)
        {
            // Игнорируем Location_Bounds и другие системные объекты
            if (hit.collider.name.Contains("Location_Bounds") ||
                hit.collider.name.Contains("Grid") ||
                hit.collider.name.Contains("Terrain"))
            {

                continue;
            }



            // Сначала проверяем непосредственно в коллайдере
            Character character = hit.collider.GetComponent<Character>();

            // Если не нашли, ищем в родительских объектах
            if (character == null)
            {
                character = hit.collider.GetComponentInParent<Character>();
                if (character != null)
                {

                }
            }
            else
            {

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

                }
            }

            if (character != null)
            {

                if (character.IsEnemyCharacter())
                {

                    return character;
                }
                else
                {

                }
            }
            else
            {

            }
        }

        // Дополнительно попробуем проверить все Character'ы в сцене и их расстояние до луча
        Character[] allCharacters = FindObjectsOfType<Character>();


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

            return closestEnemy;
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
    /// Назначить цель союзникам
    /// </summary>
    void AssignTargetToAllies(List<Character> allies, Character target)
    {




        if (allies.Count == 0)
        {

            return;
        }

        foreach (Character ally in allies)
        {


            // Останавливаем предыдущее следование
            StopFollowing(ally);

            // Назначаем новую цель
            activeTargets[ally] = target;


            // Создаем индикатор цели
            CreateTargetIndicator(target);

            // Начинаем следование
            StartFollowing(ally, target);


        }



    }

    /// <summary>
    /// Начать следование за целью
    /// </summary>
    void StartFollowing(Character follower, Character target)
    {


        // Находим ближайшую позицию рядом с целью
        Vector3 followPosition = GetFollowPosition(target);


        // Отправляем союзника к цели
        CharacterMovement movement = follower.GetComponent<CharacterMovement>();
        if (movement != null)
        {

            movement.MoveTo(followPosition);

            // Уведомляем AI о движении, инициированном игроком
            CharacterAI ai = follower.GetComponent<CharacterAI>();
            if (ai != null)
            {

                ai.OnPlayerInitiatedMovement();
            }
            else
            {

            }
        }
        else
        {


            // Добавляем компонент движения если его нет
            movement = follower.gameObject.AddComponent<CharacterMovement>();

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



        List<Character> followersToRemove = new List<Character>();

        foreach (var kvp in activeTargets)
        {
            Character follower = kvp.Key;
            Character target = kvp.Value;

            // Проверяем, что оба персонажа еще существуют
            if (follower == null || target == null)
            {

                followersToRemove.Add(follower);
                continue;
            }

            // Проверяем, не слишком ли далеко союзник от цели
            float distance = Vector3.Distance(follower.transform.position, target.transform.position);


            if (distance > followDistance * 2f) // Если слишком далеко, обновляем позицию
            {
                CharacterMovement movement = follower.GetComponent<CharacterMovement>();
                if (movement != null)
                {
                    bool isMoving = movement.IsMoving();


                    if (!isMoving)
                    {
                        // Союзник не движется и далеко от цели - отправляем его ближе
                        Vector3 newFollowPosition = GetFollowPosition(target);

                        movement.MoveTo(newFollowPosition);
                    }
                }
                else
                {

                    movement = follower.gameObject.AddComponent<CharacterMovement>();
                    Vector3 newFollowPosition = GetFollowPosition(target);
                    movement.MoveTo(newFollowPosition);
                }
            }
            else if (distance <= followDistance)
            {

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



        // Получаем все MeshRenderer'ы в объекте и его детях (как в SelectionManager)
        MeshRenderer[] renderers = enemy.GetComponentsInChildren<MeshRenderer>();

        if (renderers.Length == 0)
        {

            return;
        }



        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer == null || renderer.material == null) continue;

            // Сохраняем оригинальный материал
            if (!originalEnemyMaterials.ContainsKey(renderer))
            {
                originalEnemyMaterials[renderer] = renderer.material;

            }

            // Создаем материал подсветки
            if (!highlightMaterials.ContainsKey(renderer))
            {
                Material highlightMat = new Material(originalEnemyMaterials[renderer]);
                highlightMat.color = enemyHighlightColor;
                highlightMaterials[renderer] = highlightMat;

            }

            renderer.material = highlightMaterials[renderer];
            currentHighlightedRenderers.Add(renderer);

        }


    }

    /// <summary>
    /// Убрать подсветку с врага (работает как SelectionManager EndHover)
    /// </summary>
    void RemoveEnemyHighlight(Character enemy)
    {
        if (enemy == null) return;



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

                    restoredCount++;
                }

                currentHighlightedRenderers.RemoveAt(i);
            }
        }


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



        foreach (var kvp in activeTargets)
        {
            Character follower = kvp.Key;
            Character target = kvp.Value;

        }






    }

    /// <summary>
    /// Тестовый метод для проверки системы наведения
    /// </summary>
    public void TestTargetingSystem()
    {


        // Проверяем компоненты





        // Проверяем персонажей в сцене
        Character[] allCharacters = FindObjectsOfType<Character>();


        int playerCount = 0, enemyCount = 0;
        foreach (var character in allCharacters)
        {
            if (character.IsPlayerCharacter()) playerCount++;
            else if (character.IsEnemyCharacter()) enemyCount++;

            // Проверяем компоненты каждого персонажа
            bool hasRenderer = character.characterRenderer != null || character.GetComponent<Renderer>() != null;
            bool hasCollider = character.GetComponent<Collider>() != null;

        }




        // Проверяем выделение
        var selectedAllies = GetSelectedAllies();



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