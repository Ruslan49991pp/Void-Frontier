using UnityEngine;

/// <summary>
/// Скрипт для очистки старых персонажей и создания новых с правильными префабами SKM_Character
/// </summary>
public class CharacterRefreshTest : MonoBehaviour
{
    [Header("Refresh Settings")]
    public KeyCode refreshKey = KeyCode.R;
    public int playerCharacters = 3;
    public int enemyCharacters = 3;

    void Update()
    {
        if (Input.GetKeyDown(refreshKey))
        {
            RefreshAllCharacters();
        }
    }

    /// <summary>
    /// Удалить всех старых персонажей и создать новых
    /// </summary>
    public void RefreshAllCharacters()
    {
        DebugLogger.Log(DebugLogger.LogCategory.Spawning, "=== REFRESHING ALL CHARACTERS ===");

        // Удаляем всех старых персонажей
        Character[] oldCharacters = FindObjectsOfType<Character>();
        DebugLogger.Log(DebugLogger.LogCategory.Spawning, $"Found {oldCharacters.Length} old characters to remove");

        foreach (Character character in oldCharacters)
        {
            if (character != null && character.gameObject != null)
            {
                DebugLogger.Log(DebugLogger.LogCategory.Spawning, $"Removing old character: {character.GetFullName()}");
                DestroyImmediate(character.gameObject);
            }
        }

        // Ждем немного чтобы объекты удалились
        Invoke(nameof(CreateNewCharacters), 0.5f);
    }

    void CreateNewCharacters()
    {
        DebugLogger.Log(DebugLogger.LogCategory.Spawning, "Creating new characters with SKM_Character prefab");

        // Создаем игроков
        for (int i = 0; i < playerCharacters; i++)
        {
            CreatePlayerCharacter(i);
        }

        // Создаем врагов
        for (int i = 0; i < enemyCharacters; i++)
        {
            CreateEnemyCharacter(i);
        }

        DebugLogger.Log(DebugLogger.LogCategory.Spawning, "=== CHARACTER REFRESH COMPLETE ===");
    }

    void CreatePlayerCharacter(int index)
    {
        GameObject characterPrefab = Resources.Load<GameObject>("Prefabs/SKM_Character");
        if (characterPrefab == null)
        {
            DebugLogger.LogError(DebugLogger.LogCategory.Spawning, "SKM_Character prefab not found!");
            return;
        }

        GameObject player = Instantiate(characterPrefab);
        player.name = $"Player_{index + 1}";

        // Позиция игрока
        Vector3 playerPos = new Vector3(
            Random.Range(-5f, 0f),
            1f,
            Random.Range(-5f, 5f)
        );
        player.transform.position = playerPos;

        // Настраиваем компонент Character
        Character character = player.GetComponent<Character>();
        if (character == null)
        {
            character = player.AddComponent<Character>();
        }

        character.GenerateRandomCharacter();
        character.characterData.faction = Faction.Player;

        // Настраиваем рендерер
        SetupCharacterRenderer(character, Color.green);

        // Добавляем необходимые компоненты
        EnsureCharacterComponents(player, character);

        DebugLogger.Log(DebugLogger.LogCategory.Spawning, $"✓ Created player: {character.GetFullName()} at {playerPos}");
    }

    void CreateEnemyCharacter(int index)
    {
        GameObject characterPrefab = Resources.Load<GameObject>("Prefabs/SKM_Character");
        if (characterPrefab == null)
        {
            DebugLogger.LogError(DebugLogger.LogCategory.Spawning, "SKM_Character prefab not found!");
            return;
        }

        GameObject enemy = Instantiate(characterPrefab);
        enemy.name = $"Enemy_{index + 1}";

        // Позиция врага
        Vector3 enemyPos = new Vector3(
            Random.Range(3f, 8f),
            1f,
            Random.Range(-5f, 5f)
        );
        enemy.transform.position = enemyPos;

        // Настраиваем компонент Character
        Character character = enemy.GetComponent<Character>();
        if (character == null)
        {
            character = enemy.AddComponent<Character>();
        }

        // Настраиваем как врага
        character.characterData = new CharacterData();
        character.characterData.firstName = $"Enemy";
        character.characterData.lastName = $"{index + 1}";
        character.characterData.faction = Faction.Enemy;
        character.characterData.profession = "Hostile Unit";
        character.characterData.level = Random.Range(1, 5);
        character.characterData.maxHealth = 100f;
        character.characterData.health = character.characterData.maxHealth;
        character.characterData.bio = $"Hostile enemy unit #{index + 1}";

        // Настраиваем рендерер
        SetupCharacterRenderer(character, Color.red);

        // Добавляем необходимые компоненты
        EnsureCharacterComponents(enemy, character);

        DebugLogger.Log(DebugLogger.LogCategory.Spawning, $"✓ Created enemy: {character.GetFullName()} at {enemyPos}");
    }

    void SetupCharacterRenderer(Character character, Color defaultColor)
    {
        // Находим рендерер
        Renderer renderer = character.characterRenderer;
        if (renderer == null)
        {
            renderer = character.GetComponent<Renderer>();
            if (renderer == null)
            {
                renderer = character.GetComponentInChildren<Renderer>();
            }
            character.characterRenderer = renderer;
        }

        if (renderer != null)
        {
            // Настраиваем цвета
            character.defaultColor = defaultColor;
            character.selectedColor = new Color(1f, 0.5f, 0f, 1f); // Оранжевый
            character.hoverColor = Color.cyan;

            // Применяем цвет по умолчанию
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = defaultColor;
            renderer.material = mat;

            DebugLogger.Log(DebugLogger.LogCategory.Spawning, $"Setup renderer for {character.GetFullName()}: {renderer.name}");
        }
        else
        {
            DebugLogger.LogError(DebugLogger.LogCategory.Spawning, $"No renderer found for {character.GetFullName()}!");
        }
    }

    void EnsureCharacterComponents(GameObject gameObject, Character character)
    {
        // Убеждаемся что есть коллайдер
        Collider collider = gameObject.GetComponent<Collider>();
        if (collider == null)
        {
            collider = gameObject.GetComponentInChildren<Collider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<CapsuleCollider>();
            }
        }

        // Добавляем CharacterMovement
        CharacterMovement movement = gameObject.GetComponent<CharacterMovement>();
        if (movement == null)
        {
            movement = gameObject.AddComponent<CharacterMovement>();
        }

        // Добавляем LocationObjectInfo
        LocationObjectInfo objectInfo = gameObject.GetComponent<LocationObjectInfo>();
        if (objectInfo == null)
        {
            objectInfo = gameObject.AddComponent<LocationObjectInfo>();
        }
        objectInfo.objectType = "Character";
        objectInfo.objectName = character.GetFullName();
        objectInfo.health = character.characterData.health;

        // Правильный слой
        gameObject.layer = LayerMask.NameToLayer("Default");
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 30), $"Press {refreshKey} to refresh all characters");

        Character[] characters = FindObjectsOfType<Character>();
        int players = 0, enemies = 0;

        foreach (var character in characters)
        {
            if (character.IsPlayerCharacter()) players++;
            else if (character.IsEnemyCharacter()) enemies++;
        }

        GUI.Label(new Rect(10, 40, 300, 30), $"Players: {players}, Enemies: {enemies}");
    }
}