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

    [Header("Death")]
    private bool isDead = false;
    private bool isSearchable = false;
    private bool hasBeenSearched = false;
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
    private Inventory characterInventory;
    
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

        // Добавляем компонент инвентаря
        characterInventory = GetComponent<Inventory>();
        if (characterInventory == null)
        {
            characterInventory = gameObject.AddComponent<Inventory>();
        }

        // Добавляем систему оружия
        WeaponSystem weaponSystem = GetComponent<WeaponSystem>();
        if (weaponSystem == null)
        {
            weaponSystem = gameObject.AddComponent<WeaponSystem>();
        }

        // Ждем следующий кадр для правильной инициализации инвентаря
        StartCoroutine(DelayedInventorySetup());
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
        
        if (characterData.health <= 0 && !isDead)
        {
            // Выполняем смерть персонажа
            Die();
        }
    }

    /// <summary>
    /// Обработка смерти персонажа
    /// </summary>
    void Die()
    {
        if (isDead) return; // Предотвращаем повторную смерть

        isDead = true;

        // Поворачиваем персонажа на -90 градусов по оси X (падение)
        transform.rotation = Quaternion.Euler(-90f, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);

        // Отключаем все компоненты управления и движения
        DisableCharacterControl();

        // НЕ выбрасываем лут - он остается внутри для обыска
        // DropLootOnDeath(); // Закомментировано

        // Останавливаем боевые действия с этим персонажем
        StopCombatInvolvement();

        // Делаем персонажа доступным для обыска
        MakeSearchable();
    }

    /// <summary>
    /// Отключение всех компонентов управления персонажа
    /// </summary>
    void DisableCharacterControl()
    {
        // Отключаем движение
        CharacterMovement movement = GetComponent<CharacterMovement>();
        if (movement != null)
        {
            movement.StopMovement(); // Останавливаем текущее движение
            movement.enabled = false;
        }

        // Отключаем AI
        CharacterAI ai = GetComponent<CharacterAI>();
        if (ai != null)
        {
            ai.enabled = false;
        }

        // Останавливаем все боевые действия
        CombatSystem combatSystem = FindObjectOfType<CombatSystem>();
        if (combatSystem != null)
        {
            combatSystem.StopCombatForCharacter(this);
        }

        // НЕ отключаем коллайдер для мертвых - нужен для обыска
        // Collider characterCollider = GetComponent<Collider>();
        // if (characterCollider != null)
        // {
        //     characterCollider.enabled = false;
        // }
    }

    /// <summary>
    /// Остановка всех боевых действий с участием этого персонажа
    /// </summary>
    void StopCombatInvolvement()
    {
        CombatSystem combatSystem = FindObjectOfType<CombatSystem>();
        if (combatSystem != null)
        {
            // Останавливаем бой если этот персонаж атаковал кого-то
            if (combatSystem.IsInCombat(this))
            {
                combatSystem.StopCombatForCharacter(this);
            }
        }
    }

    /// <summary>
    /// Делаем мертвого персонажа доступным для обыска
    /// </summary>
    void MakeSearchable()
    {
        if (isDead)
        {
            isSearchable = true;
            hasBeenSearched = false;

            // Можно добавить визуальный индикатор что можно обыскать
            // Например, изменить цвет или добавить иконку
        }
    }

    /// <summary>
    /// Обыскать мертвого персонажа (вызывается извне)
    /// </summary>
    public bool SearchCorpse()
    {
        if (!isDead)
            return false; // Можно обыскивать только мертвых

        if (!CanBeSearched())
            return false;

        // Открываем инвентарь мертвого персонажа для обыска
        Inventory corpseInventory = GetComponent<Inventory>();
        if (corpseInventory != null)
        {
            // Открываем инвентарь трупа
            InventoryUI inventoryUI = FindObjectOfType<InventoryUI>();
            if (inventoryUI != null)
            {
                inventoryUI.SetCurrentInventory(corpseInventory, this);
                inventoryUI.ShowInventory();
                Debug.Log($"[SEARCH] Opened inventory of {GetFullName()}");
            }
            else
            {
                Debug.LogWarning($"[SEARCH] InventoryUI not found for {GetFullName()}");
                // Если нет UI, выбрасываем лут как fallback
                DropLootOnDeath();
            }

            // Помечаем как обысканный чтобы нельзя было обыскать повторно
            hasBeenSearched = true;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Проверить, можно ли обыскать этого персонажа
    /// </summary>
    public bool CanBeSearched()
    {
        return isDead && isSearchable && !hasBeenSearched;
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
    /// Проверить, мертв ли персонаж
    /// </summary>
    public bool IsDead()
    {
        return isDead;
    }

    /// <summary>
    /// Проверить, является ли персонаж игроком
    /// </summary>
    public bool IsPlayerCharacter()
    {
        bool isPlayer = characterData.faction == Faction.Player;
        return isPlayer;
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

    /// <summary>
    /// Отложенная настройка инвентаря для правильной инициализации
    /// </summary>
    System.Collections.IEnumerator DelayedInventorySetup()
    {
        // Ждем один кадр для завершения инициализации компонентов
        yield return null;

        // Настраиваем инвентарь в зависимости от фракции
        SetupInventoryForFaction();
    }

    void OnDestroy()
    {
        // Освобождаем материал
        if (characterMaterial != null)
        {
            DestroyImmediate(characterMaterial);
        }
    }
    
    /// <summary>
    /// Настроить инвентарь в зависимости от фракции
    /// </summary>
    void SetupInventoryForFaction()
    {
        if (characterInventory == null) return;

        switch (characterData.faction)
        {
            case Faction.Player:
                // Союзники имеют больший инвентарь
                characterInventory.maxSlots = 20;
                characterInventory.maxWeight = 100f;
                characterInventory.autoPickupEnabled = true;
                characterInventory.autoPickupRange = 1.5f;

                // Генерируем стартовые предметы для союзников
                GeneratePlayerStartingItems();
                break;

            case Faction.Enemy:
                // Враги имеют ограниченный инвентарь
                characterInventory.maxSlots = 10;
                characterInventory.maxWeight = 50f;
                characterInventory.autoPickupEnabled = false;

                // Генерируем случайные предметы для врагов
                GenerateEnemyLoot();
                break;

            case Faction.Neutral:
                // Нейтральные персонажи имеют средний инвентарь
                characterInventory.maxSlots = 15;
                characterInventory.maxWeight = 75f;
                characterInventory.autoPickupEnabled = false;

                // Генерируем стартовые предметы для нейтральных персонажей
                GenerateNeutralStartingItems();
                break;
        }
    }

    /// <summary>
    /// Генерировать стартовые предметы для союзников
    /// </summary>
    void GeneratePlayerStartingItems()
    {
        if (characterInventory == null || characterData.faction != Faction.Player)
            return;

        // Добавляем по 1 предмету каждого типа
        ItemType[] allTypes = { ItemType.Weapon, ItemType.Armor, ItemType.Tool, ItemType.Medical, ItemType.Resource, ItemType.Consumable };

        foreach (ItemType itemType in allTypes)
        {
            ItemData startingItem = CreateSpecificLoot(itemType);
            if (startingItem != null)
            {
                characterInventory.AddItem(startingItem, 1);
            }
        }
    }

    /// <summary>
    /// Генерировать стартовые предметы для нейтральных персонажей
    /// </summary>
    void GenerateNeutralStartingItems()
    {
        if (characterInventory == null || characterData.faction != Faction.Neutral)
            return;

        // Добавляем по 1 предмету каждого типа
        ItemType[] allTypes = { ItemType.Weapon, ItemType.Armor, ItemType.Tool, ItemType.Medical, ItemType.Resource, ItemType.Consumable };

        foreach (ItemType itemType in allTypes)
        {
            ItemData startingItem = CreateSpecificLoot(itemType);
            if (startingItem != null)
            {
                characterInventory.AddItem(startingItem, 1);
            }
        }
    }

    /// <summary>
    /// Генерировать добычу для врагов
    /// </summary>
    void GenerateEnemyLoot()
    {
        if (characterInventory == null || characterData.faction != Faction.Enemy)
            return;

        // Добавляем по 1 предмету каждого типа
        ItemType[] allTypes = { ItemType.Weapon, ItemType.Armor, ItemType.Tool, ItemType.Medical, ItemType.Resource, ItemType.Consumable };

        foreach (ItemType itemType in allTypes)
        {
            ItemData lootItem = CreateSpecificLoot(itemType);
            if (lootItem != null)
            {
                characterInventory.AddItem(lootItem, 1);
            }
        }
    }

    /// <summary>
    /// Создать предмет определенного типа
    /// </summary>
    ItemData CreateSpecificLoot(ItemType itemType)
    {
        ItemData item = new ItemData();
        item.itemType = itemType;

        // Настраиваем предмет в зависимости от типа
        switch (itemType)
        {
            case ItemType.Weapon:
                item.itemName = "Basic Weapon";
                item.description = "Simple weapon found on enemy";
                item.damage = staticRandom.Next(5, 15);
                item.value = staticRandom.Next(10, 50);
                item.weight = 2f;
                item.rarity = ItemRarity.Common;
                item.equipmentSlot = EquipmentSlot.RightHand; // Оружие можно держать в правой руке
                break;

            case ItemType.Medical:
                item.itemName = "Medkit";
                item.description = "Basic medical supplies";
                item.healing = staticRandom.Next(10, 30);
                item.value = staticRandom.Next(15, 40);
                item.weight = 0.5f;
                item.maxStackSize = 5;
                item.rarity = ItemRarity.Common;
                break;

            case ItemType.Resource:
                item.itemName = "Metal Scraps";
                item.description = "Useful crafting material";
                item.value = staticRandom.Next(5, 15);
                item.weight = 0.3f;
                item.maxStackSize = 10;
                item.rarity = ItemRarity.Common;
                break;

            case ItemType.Tool:
                item.itemName = "Basic Tool";
                item.description = "Simple maintenance tool";
                item.value = staticRandom.Next(8, 25);
                item.weight = 1f;
                item.rarity = ItemRarity.Common;
                break;

            case ItemType.Armor:
                // Создаем случайную броню для разных слотов
                EquipmentSlot[] armorSlots = { EquipmentSlot.Head, EquipmentSlot.Chest, EquipmentSlot.Legs, EquipmentSlot.Feet };
                EquipmentSlot armorSlot = armorSlots[staticRandom.Next(0, armorSlots.Length)];

                switch (armorSlot)
                {
                    case EquipmentSlot.Head:
                        item.itemName = "Basic Helmet";
                        item.description = "Simple protective helmet";
                        break;
                    case EquipmentSlot.Chest:
                        item.itemName = "Basic Chest Armor";
                        item.description = "Simple chest protection";
                        break;
                    case EquipmentSlot.Legs:
                        item.itemName = "Basic Leg Armor";
                        item.description = "Simple leg protection";
                        break;
                    case EquipmentSlot.Feet:
                        item.itemName = "Basic Boots";
                        item.description = "Simple protective boots";
                        break;
                }

                item.equipmentSlot = armorSlot;
                item.armor = staticRandom.Next(2, 8);
                item.weight = staticRandom.Next(1, 4);
                item.value = staticRandom.Next(15, 40);
                item.rarity = ItemRarity.Common;
                break;

            case ItemType.Consumable:
                item.itemName = "Basic Consumable";
                item.description = "Single-use item";
                item.value = staticRandom.Next(5, 20);
                item.weight = 0.2f;
                item.maxStackSize = 5;
                item.rarity = ItemRarity.Common;
                break;
        }

        return item;
    }

    /// <summary>
    /// Создать случайный предмет для добычи (старый метод для совместимости)
    /// </summary>
    ItemData CreateRandomLoot()
    {
        // Случайный тип предмета
        ItemType[] availableTypes = { ItemType.Weapon, ItemType.Armor, ItemType.Tool, ItemType.Medical, ItemType.Resource, ItemType.Consumable };
        ItemType randomType = availableTypes[staticRandom.Next(0, availableTypes.Length)];
        return CreateSpecificLoot(randomType);
    }

    /// <summary>
    /// Получить инвентарь персонажа
    /// </summary>
    public Inventory GetInventory()
    {
        return characterInventory;
    }

    /// <summary>
    /// Добавить предмет в инвентарь персонажа
    /// </summary>
    public bool AddItemToInventory(ItemData item, int quantity = 1)
    {
        if (characterInventory != null)
        {
            return characterInventory.AddItem(item, quantity);
        }
        return false;
    }

    /// <summary>
    /// Удалить предмет из инвентаря персонажа
    /// </summary>
    public bool RemoveItemFromInventory(ItemData item, int quantity = 1)
    {
        if (characterInventory != null)
        {
            return characterInventory.RemoveItem(item, quantity);
        }
        return false;
    }

    /// <summary>
    /// Проверить, есть ли предмет в инвентаре
    /// </summary>
    public bool HasItemInInventory(ItemData item, int quantity = 1)
    {
        if (characterInventory != null)
        {
            return characterInventory.HasItem(item, quantity);
        }
        return false;
    }

    /// <summary>
    /// Выбросить предметы при смерти
    /// </summary>
    void DropLootOnDeath()
    {
        if (characterInventory == null) return;

        var usedSlots = characterInventory.GetUsedSlotsList();
        foreach (var slot in usedSlots)
        {
            if (!slot.IsEmpty())
            {
                // Создаем предмет в мире рядом с персонажем
                Vector3 dropPosition = transform.position +
                    new Vector3(
                        UnityEngine.Random.Range(-1f, 1f),
                        0.5f,
                        UnityEngine.Random.Range(-1f, 1f)
                    );

                Item.CreateWorldItem(slot.itemData, dropPosition);
            }
        }

        // Очищаем инвентарь
        characterInventory.ClearInventory();
    }

    /// <summary>
    /// Обновленный метод получения информации о персонаже (включая инвентарь)
    /// </summary>
    public string GetDetailedCharacterInfo()
    {
        string info = GetCharacterInfo();

        if (characterInventory != null)
        {
            info += $"\n\nInventory: {characterInventory.GetUsedSlots()}/{characterInventory.maxSlots} slots";
            info += $"\nWeight: {characterInventory.GetCurrentWeight():F1}/{characterInventory.maxWeight}";

            var usedSlots = characterInventory.GetUsedSlotsList();
            if (usedSlots.Count > 0)
            {
                info += "\nItems:";
                foreach (var slot in usedSlots)
                {
                    info += $"\n- {slot.itemData.itemName} x{slot.quantity}";
                }
            }
        }

        return info;
    }

    void OnDrawGizmosSelected()
    {
        // Показываем информацию о персонаже в Scene view
        Gizmos.color = isSelected ? selectedColor : defaultColor;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 2.5f, 0.5f);

        // Показываем радиус автоподбора для союзников
        if (characterInventory != null && IsPlayerCharacter() && characterInventory.autoPickupEnabled)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, characterInventory.autoPickupRange);
        }
    }
}