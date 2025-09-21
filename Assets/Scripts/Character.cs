using UnityEngine;

public enum Faction
{
    Player,    // Дружественные юниты игрока
    Enemy,     // Враждебные юниты
    Neutral    // Нейтральные юниты
}

[System.Serializable]
public class CharacterData
{
    public string firstName;
    public string lastName;
    public int level;
    public float health;
    public float maxHealth;
    public string profession;
    public string bio;
    public Faction faction = Faction.Player;
}

public class Character : MonoBehaviour
{
    [Header("Character Info")]
    public CharacterData characterData;
    
    [Header("Visual")]
    public Renderer characterRenderer;
    public Color defaultColor = Color.green;
    public Color selectedColor = new Color(1f, 0.5f, 0f, 1f); // Оранжевый цвет
    public Color hoverColor = Color.cyan;
    
    [Header("Stats")]
    public float moveSpeed = 3f;
    
    // Статическая система генерации имен с отслеживанием использованных комбинаций
    private static readonly string[] FirstNames = {
        "James", "John", "Robert", "Michael", "William", "David", "Richard", "Joseph", "Thomas", "Christopher",
        "Charles", "Daniel", "Matthew", "Anthony", "Mark", "Donald", "Steven", "Paul", "Andrew", "Joshua",
        "Kenneth", "Kevin", "Brian", "George", "Timothy", "Ronald", "Jason", "Edward", "Jeffrey", "Ryan",
        "Jacob", "Gary", "Nicholas", "Eric", "Jonathan", "Stephen", "Larry", "Justin", "Scott", "Brandon",
        "Benjamin", "Samuel", "Gregory", "Frank", "Raymond", "Alexander", "Patrick", "Jack", "Dennis", "Jerry",
        "Tyler", "Nathan", "Harold", "Jordan", "Douglas", "Arthur", "Noah", "Henry", "Zachary", "Carl",
        "Mary", "Patricia", "Jennifer", "Linda", "Elizabeth", "Barbara", "Susan", "Jessica", "Sarah", "Karen",
        "Nancy", "Lisa", "Betty", "Helen", "Sandra", "Donna", "Carol", "Ruth", "Sharon", "Michelle",
        "Laura", "Kimberly", "Deborah", "Dorothy", "Amy", "Angela", "Ashley", "Brenda", "Emma", "Olivia",
        "Cynthia", "Marie", "Janet", "Catherine", "Frances", "Christine", "Samantha", "Debra", "Rachel", "Carolyn"
    };
    
    private static readonly string[] LastNames = {
        "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez",
        "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin",
        "Lee", "Perez", "Thompson", "White", "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson",
        "Walker", "Young", "Allen", "King", "Wright", "Scott", "Torres", "Nguyen", "Hill", "Flores",
        "Green", "Adams", "Nelson", "Baker", "Hall", "Rivera", "Campbell", "Mitchell", "Carter", "Roberts",
        "Gomez", "Phillips", "Evans", "Turner", "Diaz", "Parker", "Cruz", "Edwards", "Collins", "Reyes",
        "Stewart", "Morris", "Morales", "Murphy", "Cook", "Rogers", "Gutierrez", "Ortiz", "Morgan", "Cooper",
        "Peterson", "Bailey", "Reed", "Kelly", "Howard", "Ramos", "Kim", "Cox", "Ward", "Richardson",
        "Watson", "Brooks", "Chavez", "Wood", "James", "Bennett", "Gray", "Mendoza", "Ruiz", "Hughes"
    };
    
    private static readonly string[] Professions = {
        "Engineer", "Pilot", "Medic", "Scientist", "Security", "Mechanic", "Navigator", "Communications",
        "Geologist", "Biologist", "Technician", "Specialist", "Officer", "Researcher", "Analyst"
    };
    
    // Статическая система для отслеживания использованных имен
    private static System.Collections.Generic.HashSet<string> usedNames = new System.Collections.Generic.HashSet<string>();
    private static System.Random staticRandom = new System.Random(System.DateTime.Now.Millisecond);
    
    // Внутренние переменные
    private bool isSelected = false;
    private bool isHovered = false;
    private Material characterMaterial;
    private Camera mainCamera;
    private CharacterAI characterAI;
    
    void Awake()
    {
        // Находим renderer если не назначен
        if (characterRenderer == null)
            characterRenderer = GetComponent<Renderer>();

        // Создаем собственный материал для изменения цвета
        if (characterRenderer != null)
        {
            characterMaterial = new Material(characterRenderer.material);
            characterRenderer.material = characterMaterial;
            SetColor(defaultColor);
        }

        // Находим главную камеру
        mainCamera = Camera.main;
        
        // Добавляем LocationObjectInfo для интеграции с системой выделения
        var objectInfo = GetComponent<LocationObjectInfo>();
        if (objectInfo == null)
        {
            objectInfo = gameObject.AddComponent<LocationObjectInfo>();
        }
        
        // ВСЕГДА генерируем новые данные персонажа для каждого экземпляра
        // Это предотвращает дублирование данных из префаба
        GenerateRandomCharacter();
        
        // Настраиваем LocationObjectInfo
        objectInfo.objectType = "Character";
        objectInfo.objectName = GetFullName();
        objectInfo.health = characterData.health;

        // Добавляем компонент ИИ
        characterAI = GetComponent<CharacterAI>();
        if (characterAI == null)
        {
            characterAI = gameObject.AddComponent<CharacterAI>();
        }
    }

    void Update()
    {
        // Проверяем hover только если персонаж не выделен
        if (!isSelected)
        {
            CheckMouseHover();
        }
        else
        {
            // Если персонаж выделен, убираем hover состояние
            if (isHovered)
            {
                isHovered = false;
            }
        }
    }
    
    /// <summary>
    /// Генерация случайного персонажа с уникальным именем
    /// </summary>
    public void GenerateRandomCharacter()
    {
        characterData = new CharacterData();
        
        // Генерируем уникальное имя
        GenerateUniqueName();
        
        characterData.level = staticRandom.Next(1, 6);
        characterData.maxHealth = 100f;
        characterData.health = characterData.maxHealth;
        characterData.profession = Professions[staticRandom.Next(0, Professions.Length)];
        
        // Генерируем краткую биографию
        characterData.bio = $"Level {characterData.level} {characterData.profession} with extensive experience in space operations.";
        
    }
    
    /// <summary>
    /// Генерация уникального имени для персонажа
    /// </summary>
    void GenerateUniqueName()
    {
        int maxAttempts = 1000; // Предотвращение бесконечного цикла
        int attempts = 0;
        
        do
        {
            string firstName = FirstNames[staticRandom.Next(0, FirstNames.Length)];
            string lastName = LastNames[staticRandom.Next(0, LastNames.Length)];
            string fullName = $"{firstName} {lastName}";
            
            // Проверяем уникальность имени
            if (!usedNames.Contains(fullName))
            {
                characterData.firstName = firstName;
                characterData.lastName = lastName;
                usedNames.Add(fullName);
                return;
            }
            
            attempts++;
        }
        while (attempts < maxAttempts);
        
        // Если не удалось сгенерировать уникальное имя, добавляем номер
        string baseName = $"{FirstNames[staticRandom.Next(0, FirstNames.Length)]} {LastNames[staticRandom.Next(0, LastNames.Length)]}";
        int counter = 1;
        string uniqueName;
        
        do
        {
            uniqueName = $"{baseName} {counter}";
            counter++;
        }
        while (usedNames.Contains(uniqueName));
        
        string[] nameParts = uniqueName.Split(' ');
        characterData.firstName = nameParts[0];
        characterData.lastName = string.Join(" ", nameParts, 1, nameParts.Length - 1);
        usedNames.Add(uniqueName);
    }
    
    /// <summary>
    /// Очистить список использованных имен (для отладки)
    /// </summary>
    public static void ClearUsedNames()
    {
        usedNames.Clear();
        // Пересоздаем Random с новым seed для лучшей случайности
        staticRandom = new System.Random(System.DateTime.Now.Millisecond + UnityEngine.Random.Range(0, 1000));
    }
    
    /// <summary>
    /// Получить статистику использованных имен
    /// </summary>
    public static void LogNameStatistics()
    {
        
        if (usedNames.Count > 0)
        {
            foreach (string name in usedNames)
            {
            }
        }
    }
    
    /// <summary>
    /// Получить полное имя персонажа
    /// </summary>
    public string GetFullName()
    {
        if (characterData == null) return "Unknown Character";
        return $"{characterData.firstName} {characterData.lastName}";
    }
    
    /// <summary>
    /// Получить информацию о персонаже для UI
    /// </summary>
    public string GetCharacterInfo()
    {
        if (characterData == null) return "No data available";
        
        return $"{GetFullName()}\n" +
               $"Profession: {characterData.profession}\n" +
               $"Level: {characterData.level}\n" +
               $"Health: {characterData.health:F0}/{characterData.maxHealth:F0}";
    }
    
    /// <summary>
    /// Установить состояние выделения
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (isSelected == selected) return;

        isSelected = selected;

        if (selected)
        {
            // При выделении убираем hover и устанавливаем цвет в зависимости от фракции
            isHovered = false;
            if (IsPlayerCharacter())
            {
                SetColor(selectedColor); // Оранжевый для союзников
            }
            else
            {
                SetColor(Color.red); // Красный для врагов (только для просмотра информации)
            }
        }
        else
        {
            // При снятии выделения возвращаем цвет по умолчанию
            // Hover будет обработан в Update()
            SetColor(defaultColor);
        }
    }
    
    /// <summary>
    /// Изменить цвет персонажа
    /// </summary>
    void SetColor(Color color)
    {
        if (characterMaterial != null)
        {
            characterMaterial.color = color;
        }
        else
        {
            // Пытаемся восстановить материал
            if (characterRenderer != null)
            {
                characterMaterial = new Material(characterRenderer.material);
                characterRenderer.material = characterMaterial;
                characterMaterial.color = color;
            }
        }
    }
    
    /// <summary>
    /// Проверить, выделен ли персонаж
    /// </summary>
    public bool IsSelected()
    {
        return isSelected;
    }
    
    /// <summary>
    /// Нанести урон персонажу
    /// </summary>
    public void TakeDamage(float damage)
    {
        characterData.health = Mathf.Max(0, characterData.health - damage);
        
        // Обновляем health в LocationObjectInfo
        var objectInfo = GetComponent<LocationObjectInfo>();
        if (objectInfo != null)
        {
            objectInfo.health = characterData.health;
        }
        
        if (characterData.health <= 0)
        {
        }
    }
    
    /// <summary>
    /// Восстановить здоровье
    /// </summary>
    public void Heal(float amount)
    {
        characterData.health = Mathf.Min(characterData.maxHealth, characterData.health + amount);

        // Обновляем health в LocationObjectInfo
        var objectInfo = GetComponent<LocationObjectInfo>();
        if (objectInfo != null)
        {
            objectInfo.health = characterData.health;
        }
    }

    /// <summary>
    /// Получить текущее здоровье
    /// </summary>
    public float GetHealth()
    {
        return characterData.health;
    }

    /// <summary>
    /// Получить максимальное здоровье
    /// </summary>
    public float GetMaxHealth()
    {
        return characterData.maxHealth;
    }

    /// <summary>
    /// Получить процент здоровья (0.0 - 1.0)
    /// </summary>
    public float GetHealthPercent()
    {
        if (characterData.maxHealth <= 0) return 0f;
        return characterData.health / characterData.maxHealth;
    }

    /// <summary>
    /// Установить здоровье (для тестирования)
    /// </summary>
    public void SetHealth(float health)
    {
        characterData.health = Mathf.Clamp(health, 0, characterData.maxHealth);

        // Обновляем health в LocationObjectInfo
        var objectInfo = GetComponent<LocationObjectInfo>();
        if (objectInfo != null)
        {
            objectInfo.health = characterData.health;
        }
    }

    /// <summary>
    /// Получить фракцию персонажа
    /// </summary>
    public Faction GetFaction()
    {
        return characterData.faction;
    }

    /// <summary>
    /// Установить фракцию персонажа
    /// </summary>
    public void SetFaction(Faction faction)
    {
        characterData.faction = faction;
    }

    /// <summary>
    /// Проверить, является ли персонаж игроком
    /// </summary>
    public bool IsPlayerCharacter()
    {
        return characterData.faction == Faction.Player;
    }

    /// <summary>
    /// Проверить, является ли персонаж врагом
    /// </summary>
    public bool IsEnemyCharacter()
    {
        return characterData.faction == Faction.Enemy;
    }

    /// <summary>
    /// Проверить, являются ли два персонажа союзниками
    /// </summary>
    public bool IsAllyWith(Character otherCharacter)
    {
        if (otherCharacter == null) return false;
        return characterData.faction == otherCharacter.characterData.faction;
    }

    /// <summary>
    /// Проверить, являются ли два персонажа врагами
    /// </summary>
    public bool IsEnemyWith(Character otherCharacter)
    {
        if (otherCharacter == null) return false;

        // Игроки и враги - враги друг другу
        if ((characterData.faction == Faction.Player && otherCharacter.characterData.faction == Faction.Enemy) ||
            (characterData.faction == Faction.Enemy && otherCharacter.characterData.faction == Faction.Player))
        {
            return true;
        }

        return false;
    }
    
    /// <summary>
    /// Проверка наведения мыши на персонажа
    /// </summary>
    void CheckMouseHover()
    {
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        bool shouldHover = false;

        // Используем RaycastAll чтобы проверить все объекты на луче
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject == gameObject)
            {
                shouldHover = true;
                break;
            }
        }

        // Обновляем состояние hover
        if (shouldHover && !isHovered)
        {
            isHovered = true;
            SetColor(hoverColor);
        }
        else if (!shouldHover && isHovered)
        {
            isHovered = false;
            SetColor(defaultColor);
        }
    }

    void OnDestroy()
    {
        // Освобождаем материал
        if (characterMaterial != null)
        {
            DestroyImmediate(characterMaterial);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Показываем информацию о персонаже в Scene view
        Gizmos.color = isSelected ? selectedColor : defaultColor;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 2.5f, 0.5f);
    }
}