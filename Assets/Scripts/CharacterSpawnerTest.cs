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
        GameObject characterGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        characterGO.name = $"TestCharacter_{Random.Range(1000, 9999)}";

        // Случайная позиция
        Vector3 randomPos = new Vector3(
            Random.Range(-10f, 10f),
            1f,
            Random.Range(-10f, 10f)
        );
        characterGO.transform.position = randomPos;

        // Добавляем компонент Character
        Character character = characterGO.AddComponent<Character>();

        // Генерируем случайного персонажа
        character.GenerateRandomCharacter();

        // Добавляем LocationObjectInfo для системы выделения
        LocationObjectInfo objectInfo = characterGO.AddComponent<LocationObjectInfo>();
        objectInfo.objectType = "Character";
        objectInfo.objectName = character.GetFullName();
        objectInfo.health = character.characterData.health;

        // Устанавливаем renderer для Character компонента
        Renderer renderer = characterGO.GetComponent<Renderer>();
        if (renderer != null)
        {
            character.characterRenderer = renderer;
        }

        Debug.Log($"CharacterSpawnerTest: Created character {character.GetFullName()} at {randomPos}");
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