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
    public Material enemyMaterial; // Если не установлен, будет загружен M_Enemy из Resources
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


        for (int i = 0; i < enemyCount; i++)
        {
            GameObject enemy = CreateEnemyCharacter($"Enemy_{i + 1}");

            // Размещаем врага в случайной позиции
            Vector3 spawnPosition = GetRandomSpawnPosition();
            enemy.transform.position = spawnPosition;


        }
    }

    /// <summary>
    /// Создать врага-персонажа
    /// </summary>
    GameObject CreateEnemyCharacter(string enemyName)
    {


        GameObject enemy;

        // Загружаем префаб SKM_Character из Resources
        GameObject characterPrefab = Resources.Load<GameObject>("Prefabs/SKM_Character");
        if (characterPrefab == null)
        {

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



        if (allRenderers.Length > 0)
        {
            // Приоритет: 1) enemyMaterial из Inspector'а, 2) GhostRed из Resources, 3) создание на лету
            Material enemyMat = enemyMaterial != null ? enemyMaterial : CreateEnemyMaterial();

            foreach (MeshRenderer renderer in allRenderers)
            {


                renderer.material = enemyMat;

                if (primaryRenderer == null)
                {
                    primaryRenderer = renderer;
                }
            }

            // Устанавливаем основной рендерер для Character
            character.characterRenderer = primaryRenderer;

        }
        else
        {
            // Попробуем найти любые другие рендереры
            Renderer[] anyRenderers = enemy.GetComponentsInChildren<Renderer>();


            if (anyRenderers.Length > 0)
            {
                Material enemyMat = enemyMaterial != null ? enemyMaterial : CreateEnemyMaterial();

                foreach (Renderer renderer in anyRenderers)
                {

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


    }

    /// <summary>
    /// Создать надежный материал для врага
    /// </summary>
    Material CreateEnemyMaterial()
    {
        // Сначала пытаемся загрузить готовый красный материал для врагов
        Material enemyMat = Resources.Load<Material>("Materials/M_Enemy");
        if (enemyMat != null)
        {

            return enemyMat;
        }


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

                break;
            }
        }

        // Если ничего не нашли, создаем с базовым конструктором
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse"));
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