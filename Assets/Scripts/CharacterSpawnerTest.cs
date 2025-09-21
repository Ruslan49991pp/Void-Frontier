using UnityEngine;

/// <summary>
/// Тестовый скрипт для создания персонажей если их нет в сцене
/// </summary>
public class CharacterSpawnerTest : MonoBehaviour
{
    [Header("Test Settings")]
    public KeyCode spawnCharacterKey = KeyCode.C;
    public int charactersToSpawn = 3;

    void Start()
    {
        // Проверяем есть ли персонажи в сцене
        Invoke("CheckAndCreateCharacters", 2f);
    }

    void Update()
    {
        if (Input.GetKeyDown(spawnCharacterKey))
        {
            SpawnTestCharacter();
        }
    }

    void CheckAndCreateCharacters()
    {
        Character[] existingCharacters = FindObjectsOfType<Character>();
        Debug.Log($"CharacterSpawnerTest: Found {existingCharacters.Length} existing characters");

        if (existingCharacters.Length == 0)
        {
            Debug.Log("CharacterSpawnerTest: No characters found, creating test characters");
            for (int i = 0; i < charactersToSpawn; i++)
            {
                SpawnTestCharacter();
            }
        }
    }

    void SpawnTestCharacter()
    {
        DebugLogger.Log(DebugLogger.LogCategory.Spawning, "Creating test character using SKM_Character prefab");

        // Загружаем префаб SKM_Character из Resources
        GameObject characterPrefab = Resources.Load<GameObject>("Prefabs/SKM_Character");
        GameObject characterGO;

        if (characterPrefab != null)
        {
            characterGO = Instantiate(characterPrefab);
            DebugLogger.Log(DebugLogger.LogCategory.Spawning, "Instantiated SKM_Character prefab for player character");
        }
        else
        {
            DebugLogger.LogError(DebugLogger.LogCategory.Spawning, "SKM_Character prefab not found! Creating fallback capsule.");
            characterGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        }

        characterGO.name = $"TestCharacter_{Random.Range(1000, 9999)}";

        // Случайная позиция
        Vector3 randomPos = new Vector3(
            Random.Range(-10f, 10f),
            1f,
            Random.Range(-10f, 10f)
        );
        characterGO.transform.position = randomPos;

        // Получаем или добавляем компонент Character
        Character character = characterGO.GetComponent<Character>();
        if (character == null)
        {
            character = characterGO.AddComponent<Character>();
            DebugLogger.Log(DebugLogger.LogCategory.Spawning, "Added Character component to player character");
        }

        // Генерируем случайного персонажа
        character.GenerateRandomCharacter();
        // Убеждаемся что это игрок
        character.characterData.faction = Faction.Player;

        // Находим и настраиваем рендерер
        Renderer renderer = character.characterRenderer;
        if (renderer == null)
        {
            renderer = characterGO.GetComponent<Renderer>();
            if (renderer == null)
            {
                renderer = characterGO.GetComponentInChildren<Renderer>();
            }
            character.characterRenderer = renderer;
        }

        if (renderer != null)
        {
            DebugLogger.Log(DebugLogger.LogCategory.Spawning, $"Found renderer for {character.GetFullName()}: {renderer.name}");
        }
        else
        {
            DebugLogger.LogError(DebugLogger.LogCategory.Spawning, $"No renderer found for {character.GetFullName()}!");
        }

        // Убеждаемся что есть коллайдер
        Collider collider = characterGO.GetComponent<Collider>();
        if (collider == null)
        {
            collider = characterGO.GetComponentInChildren<Collider>();
            if (collider == null)
            {
                collider = characterGO.AddComponent<CapsuleCollider>();
                DebugLogger.Log(DebugLogger.LogCategory.Spawning, $"Added CapsuleCollider to {character.GetFullName()}");
            }
        }

        // Добавляем LocationObjectInfo для системы выделения
        LocationObjectInfo objectInfo = characterGO.GetComponent<LocationObjectInfo>();
        if (objectInfo == null)
        {
            objectInfo = characterGO.AddComponent<LocationObjectInfo>();
        }
        objectInfo.objectType = "Character";
        objectInfo.objectName = character.GetFullName();
        objectInfo.health = character.characterData.health;

        DebugLogger.Log(DebugLogger.LogCategory.Spawning, $"✓ Player character {character.GetFullName()} created at {randomPos} - Faction: {character.GetFaction()}, Renderer: {renderer != null}, Collider: {collider != null}");
    }

    void OnGUI()
    {
        Character[] characters = FindObjectsOfType<Character>();

        int yPos = 350;
        GUI.Label(new Rect(10, yPos, 400, 20), $"Characters in scene: {characters.Length}");
        GUI.Label(new Rect(10, yPos + 20, 400, 20), $"Press {spawnCharacterKey} to spawn test character");

        if (characters.Length == 0)
        {
            GUI.Label(new Rect(10, yPos + 40, 400, 20), "No characters found - spawning test characters...");
        }
    }
}