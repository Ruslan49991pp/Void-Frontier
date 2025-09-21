using UnityEngine;

/// <summary>
/// Тестовый скрипт для создания врагов для тестирования системы указания целей
/// </summary>
public class EnemySpawnerTest : MonoBehaviour
{
    [Header("Enemy Spawn Settings")]
    public int enemyCount = 3;
    public float spawnRadius = 5f;
    public Vector3 spawnCenter = new Vector3(5, 0, 5);

    [Header("Enemy Prefab Settings")]
    public Material enemyMaterial;
    public Color enemyColor = Color.red;

    private GridManager gridManager;

    void Start()
    {
        gridManager = FindObjectOfType<GridManager>();

        // Создаем врагов через небольшую задержку, чтобы GridManager успел инициализироваться
        Invoke(nameof(SpawnEnemies), 1f);
    }

    /// <summary>
    /// Создать тестовых врагов
    /// </summary>
    public void SpawnEnemies()
    {
        Debug.Log($"[ENEMY SPAWNER] Creating {enemyCount} test enemies");

        for (int i = 0; i < enemyCount; i++)
        {
            GameObject enemy = CreateEnemyCharacter($"Enemy_{i + 1}");

            // Размещаем врага в случайной позиции
            Vector3 spawnPosition = GetRandomSpawnPosition();
            enemy.transform.position = spawnPosition;

            Debug.Log($"[ENEMY SPAWNER] Created enemy: {enemy.name} at position {spawnPosition}");
        }
    }

    /// <summary>
    /// Создать врага-персонажа
    /// </summary>
    GameObject CreateEnemyCharacter(string enemyName)
    {
        DebugLogger.Log(DebugLogger.LogCategory.Spawning, $"Creating enemy character: {enemyName}");

        GameObject enemy;

        // Загружаем префаб SKM_Character из Resources
        GameObject characterPrefab = Resources.Load<GameObject>("Prefabs/SKM_Character");
        if (characterPrefab == null)
        {
            DebugLogger.LogError(DebugLogger.LogCategory.Spawning, "SKM_Character prefab not found in Resources/Prefabs! Creating fallback capsule.");

            // Fallback к капсуле если префаб не найден
            enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = enemyName;
            enemy.transform.localScale = Vector3.one;
        }
        else
        {
            // Создаем врага из префаба SKM_Character
            enemy = Instantiate(characterPrefab);
            enemy.name = enemyName;
            DebugLogger.Log(DebugLogger.LogCategory.Spawning, $"Instantiated SKM_Character prefab for {enemyName}");
        }

        SetupEnemyCharacter(enemy, enemyName);
        return enemy;
    }

    /// <summary>
    /// Настроить GameObject как врага
    /// </summary>
    void SetupEnemyCharacter(GameObject enemy, string enemyName)
    {
        // Получаем или добавляем компонент Character
        Character character = enemy.GetComponent<Character>();
        if (character == null)
        {
            character = enemy.AddComponent<Character>();
            DebugLogger.Log(DebugLogger.LogCategory.Spawning, $"Added Character component to {enemyName}");
        }

        // Настраиваем как врага
        character.characterData = new CharacterData();

        // Генерируем случайные имена для врагов
        string[] enemyFirstNames = { "Viktor", "Igor", "Boris", "Alexei", "Dmitri", "Sergei", "Pavel", "Nikolai", "Anton", "Maksim" };
        string[] enemyLastNames = { "Volkov", "Petrov", "Kozlov", "Morozov", "Smirnov", "Popov", "Lebedev", "Novikov", "Fedorov", "Orlov" };

        character.characterData.firstName = enemyFirstNames[Random.Range(0, enemyFirstNames.Length)];
        character.characterData.lastName = enemyLastNames[Random.Range(0, enemyLastNames.Length)];
        character.characterData.faction = Faction.Enemy;
        character.characterData.profession = "Hostile Unit";
        character.characterData.level = Random.Range(1, 5);
        character.characterData.maxHealth = 100f;
        character.characterData.health = character.characterData.maxHealth;
        character.characterData.bio = $"Hostile {character.characterData.profession} - Level {character.characterData.level}";

        // Настраиваем цвета для врага
        character.defaultColor = enemyColor;
        character.selectedColor = Color.red;
        character.hoverColor = Color.yellow;

        // Находим и настраиваем ВСЕ рендереры
        MeshRenderer[] allRenderers = enemy.GetComponentsInChildren<MeshRenderer>();
        Renderer primaryRenderer = null;

        DebugLogger.Log(DebugLogger.LogCategory.Spawning, $"Found {allRenderers.Length} MeshRenderers for {enemyName}");

        if (allRenderers.Length > 0)
        {
            // Создаем единый материал для всех рендереров
            Material enemyMat = enemyMaterial != null ? enemyMaterial : CreateEnemyMaterial();

            foreach (MeshRenderer renderer in allRenderers)
            {
                DebugLogger.Log(DebugLogger.LogCategory.Spawning, $"Setting material for renderer: {renderer.name}, current material: {renderer.material?.name ?? "NULL"}");

                renderer.material = enemyMat;
                DebugLogger.Log(DebugLogger.LogCategory.Spawning, $"✓ Applied material to {renderer.name}: {renderer.material.name}");

                if (primaryRenderer == null)
                {
                    primaryRenderer = renderer;
                }
            }

            // Устанавливаем основной рендерер для Character
            character.characterRenderer = primaryRenderer;
            DebugLogger.Log(DebugLogger.LogCategory.Spawning, $"Set primary renderer for {enemyName}: {primaryRenderer.name}");
        }
        else
        {
            // Попробуем найти любые другие рендереры
            Renderer[] anyRenderers = enemy.GetComponentsInChildren<Renderer>();
            DebugLogger.Log(DebugLogger.LogCategory.Spawning, $"No MeshRenderers found, checking all Renderers: {anyRenderers.Length}");

            if (anyRenderers.Length > 0)
            {
                Material enemyMat = enemyMaterial != null ? enemyMaterial : CreateEnemyMaterial();

                foreach (Renderer renderer in anyRenderers)
                {
                    DebugLogger.Log(DebugLogger.LogCategory.Spawning, $"Setting material for Renderer: {renderer.name}, type: {renderer.GetType().Name}");
                    renderer.material = enemyMat;

                    if (primaryRenderer == null)
                    {
                        primaryRenderer = renderer;
                    }
                }

                character.characterRenderer = primaryRenderer;
            }
            else
            {
                DebugLogger.LogError(DebugLogger.LogCategory.Spawning, $"No renderers found for {enemyName}! Enemy highlighting will not work.");
            }
        }

        // Убеждаемся что есть коллайдер для раскастов
        Collider collider = enemy.GetComponent<Collider>();
        if (collider == null)
        {
            collider = enemy.GetComponentInChildren<Collider>();
            if (collider == null)
            {
                collider = enemy.AddComponent<CapsuleCollider>();
                DebugLogger.Log(DebugLogger.LogCategory.Spawning, $"Added CapsuleCollider to {enemyName}");
            }
        }

        // Добавляем движение (но не AI - враги пока статичные)
        CharacterMovement movement = enemy.GetComponent<CharacterMovement>();
        if (movement == null)
        {
            movement = enemy.AddComponent<CharacterMovement>();
            movement.debugMovement = false; // Отключаем дебаг для врагов
        }

        // Добавляем LocationObjectInfo для системы выделения
        LocationObjectInfo objectInfo = enemy.GetComponent<LocationObjectInfo>();
        if (objectInfo == null)
        {
            objectInfo = enemy.AddComponent<LocationObjectInfo>();
        }
        objectInfo.objectType = "Character";
        objectInfo.objectName = character.GetFullName();
        objectInfo.health = character.characterData.health;

        // Устанавливаем правильный слой для взаимодействия с SelectionManager
        enemy.layer = LayerMask.NameToLayer("Default");

        DebugLogger.Log(DebugLogger.LogCategory.Spawning, $"✓ Enemy {enemyName} setup complete - Faction: {character.GetFaction()}, Renderer: {primaryRenderer != null}, Collider: {collider != null}");
    }

    /// <summary>
    /// Создать надежный материал для врага
    /// </summary>
    Material CreateEnemyMaterial()
    {
        Material mat = null;

        // Пробуем различные шейдеры в порядке предпочтения
        string[] shaderNames = {
            "Standard",
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Simple Lit",
            "Legacy Shaders/Diffuse",
            "Legacy Shaders/VertexLit",
            "Unlit/Color",
            "Sprites/Default"
        };

        foreach (string shaderName in shaderNames)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader != null && shader.name != "Hidden/InternalErrorShader")
            {
                mat = new Material(shader);
                DebugLogger.Log(DebugLogger.LogCategory.Spawning, $"Successfully created material with shader: {shaderName}");
                break;
            }
        }

        // Если ничего не нашли, создаем с базовым конструктором
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse"));
            DebugLogger.LogWarning(DebugLogger.LogCategory.Spawning, "Created material with fallback shader");
        }

        // Настраиваем материал
        mat.color = enemyColor;
        mat.name = "EnemyMaterial_Generated";

        // Дополнительные настройки для видимости
        if (mat.HasProperty("_MainTex"))
        {
            mat.SetColor("_Color", enemyColor);
        }
        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", enemyColor);
        }

        DebugLogger.Log(DebugLogger.LogCategory.Spawning, $"✓ Created enemy material: {mat.name}, shader: {mat.shader?.name ?? "NULL"}, color: {mat.color}");
        return mat;
    }

    /// <summary>
    /// Получить случайную позицию для спавна врага
    /// </summary>
    Vector3 GetRandomSpawnPosition()
    {
        if (gridManager != null)
        {
            // Пытаемся найти свободную клетку на сетке
            for (int attempts = 0; attempts < 20; attempts++)
            {
                Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
                Vector3 worldPos = spawnCenter + new Vector3(randomOffset.x, 0, randomOffset.y);

                Vector2Int gridPos = gridManager.WorldToGrid(worldPos);

                if (gridManager.IsValidGridPosition(gridPos))
                {
                    var cell = gridManager.GetCell(gridPos);
                    if (cell == null || !cell.isOccupied)
                    {
                        return gridManager.GridToWorld(gridPos);
                    }
                }
            }
        }

        // Fallback: случайная позиция вокруг центра спавна
        Vector2 fallbackOffset = Random.insideUnitCircle * spawnRadius;
        return spawnCenter + new Vector3(fallbackOffset.x, 0, fallbackOffset.y);
    }

    /// <summary>
    /// Создать дополнительного врага (для тестирования в runtime)
    /// </summary>
    public void SpawnAdditionalEnemy()
    {
        GameObject enemy = CreateEnemyCharacter($"Enemy_Additional_{Random.Range(100, 999)}");
        Vector3 spawnPosition = GetRandomSpawnPosition();
        enemy.transform.position = spawnPosition;

        Debug.Log($"[ENEMY SPAWNER] Created additional enemy: {enemy.name} at position {spawnPosition}");
    }

    void OnDrawGizmosSelected()
    {
        // Показываем область спавна врагов
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(spawnCenter, spawnRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(spawnCenter, Vector3.one * 0.5f);
    }
}