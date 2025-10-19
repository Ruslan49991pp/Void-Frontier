using UnityEngine;

public enum Faction
{
    Player,    // Р”СЂСѓР¶РµСЃС‚РІРµРЅРЅС‹Рµ СЋРЅРёС‚С‹ РёРіСЂРѕРєР°
    Enemy,     // Р’СЂР°Р¶РґРµР±РЅС‹Рµ СЋРЅРёС‚С‹
    Neutral    // РќРµР№С‚СЂР°Р»СЊРЅС‹Рµ СЋРЅРёС‚С‹
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
    public Color selectedColor = new Color(1f, 0.5f, 0f, 1f); // РћСЂР°РЅР¶РµРІС‹Р№ С†РІРµС‚
    public Color hoverColor = Color.cyan;

    [Header("Counter-Attack")]
    private Character lastAttacker = null; // РџРѕСЃР»РµРґРЅРёР№ Р°С‚Р°РєСѓСЋС‰РёР№ РґР»СЏ СЃРёСЃС‚РµРјС‹ РєРѕРЅС‚СЂР°С‚Р°РєРё
    private float lastAttackTime = 0f;     // Р’СЂРµРјСЏ РїРѕСЃР»РµРґРЅРµР№ Р°С‚Р°РєРё
    public float counterAttackTimeout = 10f;  // Р’СЂРµРјСЏ РїРѕСЃР»Рµ РєРѕС‚РѕСЂРѕРіРѕ Р·Р°Р±С‹РІР°РµРј РѕР± Р°С‚Р°РєСѓСЋС‰РµРј
    
    [Header("Stats")]
    public float moveSpeed = GameConstants.Character.DEFAULT_MOVE_SPEED;
    
    // РЎС‚Р°С‚РёС‡РµСЃРєР°СЏ СЃРёСЃС‚РµРјР° РіРµРЅРµСЂР°С†РёРё РёРјРµРЅ СЃ РѕС‚СЃР»РµР¶РёРІР°РЅРёРµРј РёСЃРїРѕР»СЊР·РѕРІР°РЅРЅС‹С… РєРѕРјР±РёРЅР°С†РёР№
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
    
    // РЎС‚Р°С‚РёС‡РµСЃРєР°СЏ СЃРёСЃС‚РµРјР° РґР»СЏ РѕС‚СЃР»РµР¶РёРІР°РЅРёСЏ РёСЃРїРѕР»СЊР·РѕРІР°РЅРЅС‹С… РёРјРµРЅ
    private static System.Collections.Generic.HashSet<string> usedNames = new System.Collections.Generic.HashSet<string>();
    private static System.Random staticRandom = new System.Random(System.DateTime.Now.Millisecond);
    
    // Р’РЅСѓС‚СЂРµРЅРЅРёРµ РїРµСЂРµРјРµРЅРЅС‹Рµ
    private bool isSelected = false;
    private Material characterMaterial;
    private Camera mainCamera;
    private CharacterAI characterAI;
    private Inventory characterInventory;

    // РљРµС€РёСЂРѕРІР°РЅРЅС‹Рµ СЃСЃС‹Р»РєРё РЅР° СЃРёСЃС‚РµРјС‹ С‡РµСЂРµР· ServiceLocator (РґР»СЏ РѕРїС‚РёРјРёР·Р°С†РёРё)
    private CombatSystem cachedCombatSystem;
    private InventoryUI cachedInventoryUI;

    // DEPRECATED: РЎС‚Р°СЂРѕРµ СЃС‚Р°С‚РёС‡РµСЃРєРѕРµ СЃРѕР±С‹С‚РёРµ - Р·Р°РјРµРЅРµРЅРѕ РЅР° EventBus
    // РћСЃС‚Р°РІР»РµРЅРѕ РґР»СЏ РѕР±СЂР°С‚РЅРѕР№ СЃРѕРІРјРµСЃС‚РёРјРѕСЃС‚Рё, Р±СѓРґРµС‚ СѓРґР°Р»РµРЅРѕ РІ Р±СѓРґСѓС‰РµРј
    [System.Obsolete("Use EventBus.Subscribe<CharacterSpawnedEvent>() instead")]
    public static event System.Action<Character> OnPlayerCharacterSpawned;

    void Awake()
    {
        // РќР°С…РѕРґРёРј renderer РµСЃР»Рё РЅРµ РЅР°Р·РЅР°С‡РµРЅ
        if (characterRenderer == null)
            characterRenderer = GetComponent<Renderer>();

        // РЎРѕР·РґР°РµРј СЃРѕР±СЃС‚РІРµРЅРЅС‹Р№ РјР°С‚РµСЂРёР°Р» РґР»СЏ РёР·РјРµРЅРµРЅРёСЏ С†РІРµС‚Р°
        if (characterRenderer != null)
        {
            characterMaterial = new Material(characterRenderer.material);
            characterRenderer.material = characterMaterial;
            SetColor(defaultColor);
        }

        // РќР°С…РѕРґРёРј РіР»Р°РІРЅСѓСЋ РєР°РјРµСЂСѓ
        mainCamera = Camera.main;

        // Р”РѕР±Р°РІР»СЏРµРј LocationObjectInfo РґР»СЏ РёРЅС‚РµРіСЂР°С†РёРё СЃ СЃРёСЃС‚РµРјРѕР№ РІС‹РґРµР»РµРЅРёСЏ
        var objectInfo = GetComponent<LocationObjectInfo>();
        if (objectInfo == null)
        {
            objectInfo = gameObject.AddComponent<LocationObjectInfo>();
        }

        // Р’РЎР•Р“Р”Рђ РіРµРЅРµСЂРёСЂСѓРµРј РЅРѕРІС‹Рµ РґР°РЅРЅС‹Рµ РїРµСЂСЃРѕРЅР°Р¶Р° РґР»СЏ РєР°Р¶РґРѕРіРѕ СЌРєР·РµРјРїР»СЏСЂР°
        // Р­С‚Рѕ РїСЂРµРґРѕС‚РІСЂР°С‰Р°РµС‚ РґСѓР±Р»РёСЂРѕРІР°РЅРёРµ РґР°РЅРЅС‹С… РёР· РїСЂРµС„Р°Р±Р°
        GenerateRandomCharacter();

        // РќР°СЃС‚СЂР°РёРІР°РµРј LocationObjectInfo
        objectInfo.objectType = "Character";
        objectInfo.objectName = GetFullName();
        objectInfo.health = characterData.health;

        // Р”РѕР±Р°РІР»СЏРµРј РєРѕРјРїРѕРЅРµРЅС‚ РР
        characterAI = GetComponent<CharacterAI>();
        if (characterAI == null)
        {
            characterAI = gameObject.AddComponent<CharacterAI>();
        }

        // Р”РѕР±Р°РІР»СЏРµРј РєРѕРјРїРѕРЅРµРЅС‚ РёРЅРІРµРЅС‚Р°СЂСЏ
        characterInventory = GetComponent<Inventory>();
        if (characterInventory == null)
        {
            characterInventory = gameObject.AddComponent<Inventory>();
        }

        // Р”РѕР±Р°РІР»СЏРµРј СЃРёСЃС‚РµРјСѓ РѕСЂСѓР¶РёСЏ
        WeaponSystem weaponSystem = GetComponent<WeaponSystem>();
        if (weaponSystem == null)
        {
            weaponSystem = gameObject.AddComponent<WeaponSystem>();
        }

        // Р–РґРµРј СЃР»РµРґСѓСЋС‰РёР№ РєР°РґСЂ РґР»СЏ РїСЂР°РІРёР»СЊРЅРѕР№ РёРЅРёС†РёР°Р»РёР·Р°С†РёРё РёРЅРІРµРЅС‚Р°СЂСЏ
        StartCoroutine(DelayedInventorySetup());
    }

    void Start()
    {
        // ARCHITECTURE: РџСѓР±Р»РёРєСѓРµРј СЃРѕР±С‹С‚РёРµ СЃРїР°РІРЅР° РїРµСЂСЃРѕРЅР°Р¶Р° С‡РµСЂРµР· EventBus
        EventBus.Publish(new CharacterSpawnedEvent(this));

        // DEPRECATED: РџРѕРґРґРµСЂР¶РєР° СЃС‚Р°СЂРѕРіРѕ API РґР»СЏ РѕР±СЂР°С‚РЅРѕР№ СЃРѕРІРјРµСЃС‚РёРјРѕСЃС‚Рё
        #pragma warning disable CS0618 // Type or member is obsolete
        if (IsPlayerCharacter() && OnPlayerCharacterSpawned != null)
        {
            OnPlayerCharacterSpawned.Invoke(this);
        }
        #pragma warning restore CS0618
    }

    void Update()
    {
        // PERFORMANCE FIX: Hover С‚РµРїРµСЂСЊ РѕР±СЂР°Р±Р°С‚С‹РІР°РµС‚СЃСЏ С†РµРЅС‚СЂР°Р»РёР·РѕРІР°РЅРЅРѕ С‡РµСЂРµР· SelectionManager
        // РЈР±СЂР°Р»Рё CheckMouseHover() РєРѕС‚РѕСЂС‹Р№ РІС‹Р·С‹РІР°Р»СЃСЏ РґР»СЏ РљРђР–Р”РћР“Рћ РїРµСЂСЃРѕРЅР°Р¶Р° РєР°Р¶РґС‹Р№ РєР°РґСЂ
        // Р­С‚Рѕ СЌРєРѕРЅРѕРјРёС‚ СЃРѕС‚РЅРё raycast РІ СЃРµРєСѓРЅРґСѓ (10 РїРµСЂСЃРѕРЅР°Р¶РµР№ = 600 raycast/СЃРµРє)
        // РўРµРїРµСЂСЊ SelectionManager РґРµР»Р°РµС‚ РѕРґРёРЅ raycast РЅР° РІСЃРµ РѕР±СЉРµРєС‚С‹
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ CombatSystem С‡РµСЂРµР· ServiceLocator СЃ РєРµС€РёСЂРѕРІР°РЅРёРµРј
    /// PERFORMANCE: Р—Р°РјРµРЅР° FindObjectOfType (O(n)) РЅР° ServiceLocator (O(1))
    /// </summary>
    CombatSystem GetCombatSystem()
    {
        if (cachedCombatSystem == null)
        {
            cachedCombatSystem = ServiceLocator.Get<CombatSystem>();
        }
        return cachedCombatSystem;
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ InventoryUI С‡РµСЂРµР· ServiceLocator СЃ РєРµС€РёСЂРѕРІР°РЅРёРµРј
    /// PERFORMANCE: Р—Р°РјРµРЅР° FindObjectOfType (O(n)) РЅР° ServiceLocator (O(1))
    /// </summary>
    InventoryUI GetInventoryUI()
    {
        if (cachedInventoryUI == null)
        {
            cachedInventoryUI = ServiceLocator.Get<InventoryUI>();
        }
        return cachedInventoryUI;
    }
    
    /// <summary>
    /// Р“РµРЅРµСЂР°С†РёСЏ СЃР»СѓС‡Р°Р№РЅРѕРіРѕ РїРµСЂСЃРѕРЅР°Р¶Р° СЃ СѓРЅРёРєР°Р»СЊРЅС‹Рј РёРјРµРЅРµРј
    /// </summary>
    public void GenerateRandomCharacter()
    {
        characterData = new CharacterData();
        
        // Р“РµРЅРµСЂРёСЂСѓРµРј СѓРЅРёРєР°Р»СЊРЅРѕРµ РёРјСЏ
        GenerateUniqueName();
        
        characterData.level = staticRandom.Next(1, 6);
        characterData.maxHealth = GameConstants.Character.DEFAULT_HEALTH;
        characterData.health = characterData.maxHealth;
        characterData.profession = Professions[staticRandom.Next(0, Professions.Length)];
        
        // Р“РµРЅРµСЂРёСЂСѓРµРј РєСЂР°С‚РєСѓСЋ Р±РёРѕРіСЂР°С„РёСЋ
        characterData.bio = $"Level {characterData.level} {characterData.profession} with extensive experience in space operations.";
        
    }
    
    /// <summary>
    /// Р“РµРЅРµСЂР°С†РёСЏ СѓРЅРёРєР°Р»СЊРЅРѕРіРѕ РёРјРµРЅРё РґР»СЏ РїРµСЂСЃРѕРЅР°Р¶Р°
    /// </summary>
    void GenerateUniqueName()
    {
        int maxAttempts = GameConstants.Character.MAX_NAME_GENERATION_ATTEMPTS;
        int attempts = 0;
        
        do
        {
            string firstName = FirstNames[staticRandom.Next(0, FirstNames.Length)];
            string lastName = LastNames[staticRandom.Next(0, LastNames.Length)];
            string fullName = $"{firstName} {lastName}";
            
            // РџСЂРѕРІРµСЂСЏРµРј СѓРЅРёРєР°Р»СЊРЅРѕСЃС‚СЊ РёРјРµРЅРё
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
        
        // Р•СЃР»Рё РЅРµ СѓРґР°Р»РѕСЃСЊ СЃРіРµРЅРµСЂРёСЂРѕРІР°С‚СЊ СѓРЅРёРєР°Р»СЊРЅРѕРµ РёРјСЏ, РґРѕР±Р°РІР»СЏРµРј РЅРѕРјРµСЂ
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
    /// РћС‡РёСЃС‚РёС‚СЊ СЃРїРёСЃРѕРє РёСЃРїРѕР»СЊР·РѕРІР°РЅРЅС‹С… РёРјРµРЅ (РґР»СЏ РѕС‚Р»Р°РґРєРё)
    /// </summary>
    public static void ClearUsedNames()
    {
        usedNames.Clear();
        // РџРµСЂРµСЃРѕР·РґР°РµРј Random СЃ РЅРѕРІС‹Рј seed РґР»СЏ Р»СѓС‡С€РµР№ СЃР»СѓС‡Р°Р№РЅРѕСЃС‚Рё
        staticRandom = new System.Random(System.DateTime.Now.Millisecond + UnityEngine.Random.Range(0, GameConstants.Character.RANDOM_SEED_RANGE));
    }
    
    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ СЃС‚Р°С‚РёСЃС‚РёРєСѓ РёСЃРїРѕР»СЊР·РѕРІР°РЅРЅС‹С… РёРјРµРЅ
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
    /// РџРѕР»СѓС‡РёС‚СЊ РїРѕР»РЅРѕРµ РёРјСЏ РїРµСЂСЃРѕРЅР°Р¶Р°
    /// </summary>
    public string GetFullName()
    {
        if (characterData == null) return "Unknown Character";
        return $"{characterData.firstName} {characterData.lastName}";
    }
    
    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РёРЅС„РѕСЂРјР°С†РёСЋ Рѕ РїРµСЂСЃРѕРЅР°Р¶Рµ РґР»СЏ UI
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
    /// РЈСЃС‚Р°РЅРѕРІРёС‚СЊ СЃРѕСЃС‚РѕСЏРЅРёРµ РІС‹РґРµР»РµРЅРёСЏ
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (isSelected == selected) return;

        isSelected = selected;

        if (selected)
        {
            // РџСЂРё РІС‹РґРµР»РµРЅРёРё СѓСЃС‚Р°РЅР°РІР»РёРІР°РµРј С†РІРµС‚ РІ Р·Р°РІРёСЃРёРјРѕСЃС‚Рё РѕС‚ С„СЂР°РєС†РёРё
            if (IsPlayerCharacter())
            {
                SetColor(selectedColor); // РћСЂР°РЅР¶РµРІС‹Р№ РґР»СЏ СЃРѕСЋР·РЅРёРєРѕРІ
            }
            else
            {
                SetColor(Color.red); // РљСЂР°СЃРЅС‹Р№ РґР»СЏ РІСЂР°РіРѕРІ (С‚РѕР»СЊРєРѕ РґР»СЏ РїСЂРѕСЃРјРѕС‚СЂР° РёРЅС„РѕСЂРјР°С†РёРё)
            }
        }
        else
        {
            // РџСЂРё СЃРЅСЏС‚РёРё РІС‹РґРµР»РµРЅРёСЏ РІРѕР·РІСЂР°С‰Р°РµРј С†РІРµС‚ РїРѕ СѓРјРѕР»С‡Р°РЅРёСЋ
            // Hover Р±СѓРґРµС‚ РѕР±СЂР°Р±РѕС‚Р°РЅ РІ Update()
            SetColor(defaultColor);
        }
    }
    
    /// <summary>
    /// РР·РјРµРЅРёС‚СЊ С†РІРµС‚ РїРµСЂСЃРѕРЅР°Р¶Р°
    /// </summary>
    void SetColor(Color color)
    {
        if (characterMaterial != null)
        {
            characterMaterial.color = color;
        }
        else
        {
            // РџС‹С‚Р°РµРјСЃСЏ РІРѕСЃСЃС‚Р°РЅРѕРІРёС‚СЊ РјР°С‚РµСЂРёР°Р»
            if (characterRenderer != null)
            {
                characterMaterial = new Material(characterRenderer.material);
                characterRenderer.material = characterMaterial;
                characterMaterial.color = color;
            }
        }
    }
    
    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ, РІС‹РґРµР»РµРЅ Р»Рё РїРµСЂСЃРѕРЅР°Р¶
    /// </summary>
    public bool IsSelected()
    {
        return isSelected;
    }
    
    /// <summary>
    /// РќР°РЅРµСЃС‚Рё СѓСЂРѕРЅ РїРµСЂСЃРѕРЅР°Р¶Сѓ
    /// </summary>
    public void TakeDamage(float damage, Character attacker = null)
    {
        float oldHealth = characterData.health;
        characterData.health = Mathf.Max(0, characterData.health - damage);
        float newHealth = characterData.health;

        // DEBUG: Р›РѕРіРёСЂСѓРµРј РїРѕР»СѓС‡РµРЅРёРµ СѓСЂРѕРЅР°
        string factionName = IsPlayerCharacter() ? "ALLY" : "ENEMY";

        // COUNTER-ATTACK: Р—Р°РїРѕРјРёРЅР°РµРј Р°С‚Р°РєСѓСЋС‰РµРіРѕ РґР»СЏ СЃРёСЃС‚РµРјС‹ РєРѕРЅС‚СЂР°С‚Р°РєРё
        if (attacker != null && attacker != this && !isDead)
        {
            lastAttacker = attacker;
            lastAttackTime = Time.time;
        }

        // ARCHITECTURE: РџСѓР±Р»РёРєСѓРµРј СЃРѕР±С‹С‚РёРµ РїРѕР»СѓС‡РµРЅРёСЏ СѓСЂРѕРЅР° С‡РµСЂРµР· EventBus
        EventBus.Publish(new CharacterDamagedEvent(this, damage));

        // РћР±РЅРѕРІР»СЏРµРј health РІ LocationObjectInfo
        var objectInfo = GetComponent<LocationObjectInfo>();
        if (objectInfo != null)
        {
            objectInfo.health = characterData.health;
        }

        if (characterData.health <= 0 && !isDead)
        {
            // Р’С‹РїРѕР»РЅСЏРµРј СЃРјРµСЂС‚СЊ РїРµСЂСЃРѕРЅР°Р¶Р°
            Die();
        }
    }

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚РєР° СЃРјРµСЂС‚Рё РїРµСЂСЃРѕРЅР°Р¶Р°
    /// </summary>
    void Die()
    {
        if (isDead) return; // РџСЂРµРґРѕС‚РІСЂР°С‰Р°РµРј РїРѕРІС‚РѕСЂРЅСѓСЋ СЃРјРµСЂС‚СЊ

        isDead = true;

        // ARCHITECTURE: РџСѓР±Р»РёРєСѓРµРј СЃРѕР±С‹С‚РёРµ СЃРјРµСЂС‚Рё РїРµСЂСЃРѕРЅР°Р¶Р° С‡РµСЂРµР· EventBus
        EventBus.Publish(new CharacterDiedEvent(this));

        // РџРѕРІРѕСЂР°С‡РёРІР°РµРј РїРµСЂСЃРѕРЅР°Р¶Р° РїРѕ РѕСЃРё X (РїР°РґРµРЅРёРµ)
        transform.rotation = Quaternion.Euler(GameConstants.Character.DEATH_ROTATION_ANGLE, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);

        // РћС‚РєР»СЋС‡Р°РµРј РІСЃРµ РєРѕРјРїРѕРЅРµРЅС‚С‹ СѓРїСЂР°РІР»РµРЅРёСЏ Рё РґРІРёР¶РµРЅРёСЏ
        DisableCharacterControl();

        // РќР• РІС‹Р±СЂР°СЃС‹РІР°РµРј Р»СѓС‚ - РѕРЅ РѕСЃС‚Р°РµС‚СЃСЏ РІРЅСѓС‚СЂРё РґР»СЏ РѕР±С‹СЃРєР°
        // DropLootOnDeath(); // Р—Р°РєРѕРјРјРµРЅС‚РёСЂРѕРІР°РЅРѕ

        // РћСЃС‚Р°РЅР°РІР»РёРІР°РµРј Р±РѕРµРІС‹Рµ РґРµР№СЃС‚РІРёСЏ СЃ СЌС‚РёРј РїРµСЂСЃРѕРЅР°Р¶РµРј
        StopCombatInvolvement();

        // Р”РµР»Р°РµРј РїРµСЂСЃРѕРЅР°Р¶Р° РґРѕСЃС‚СѓРїРЅС‹Рј РґР»СЏ РѕР±С‹СЃРєР°
        MakeSearchable();

        // Р’РђР–РќРћ: Р”РµР»Р°РµРј РєРѕР»Р»Р°Р№РґРµСЂ С‚СЂРёРіРіРµСЂРѕРј С‡С‚РѕР±С‹ РїСѓР»Рё РїСЂРѕС…РѕРґРёР»Рё СЃРєРІРѕР·СЊ С‚РµР»Рѕ
        // Bullet.cs РёСЃРїРѕР»СЊР·СѓРµС‚ QueryTriggerInteraction.Ignore, РїРѕСЌС‚РѕРјСѓ С‚СЂРёРіРіРµСЂС‹ РёРіРЅРѕСЂРёСЂСѓСЋС‚СЃСЏ
        // SelectionManager РёСЃРїРѕР»СЊР·СѓРµС‚ QueryTriggerInteraction.Collide, РїРѕСЌС‚РѕРјСѓ С‚СЂСѓРї РјРѕР¶РЅРѕ РєР»РёРєР°С‚СЊ
        Collider characterCollider = GetComponent<Collider>();
        if (characterCollider != null)
        {
            characterCollider.isTrigger = true;
        }
    }

    /// <summary>
    /// РћС‚РєР»СЋС‡РµРЅРёРµ РІСЃРµС… РєРѕРјРїРѕРЅРµРЅС‚РѕРІ СѓРїСЂР°РІР»РµРЅРёСЏ РїРµСЂСЃРѕРЅР°Р¶Р°
    /// </summary>
    void DisableCharacterControl()
    {
        // РћС‚РєР»СЋС‡Р°РµРј РґРІРёР¶РµРЅРёРµ
        CharacterMovement movement = GetComponent<CharacterMovement>();
        if (movement != null)
        {
            movement.StopMovement(); // РћСЃС‚Р°РЅР°РІР»РёРІР°РµРј С‚РµРєСѓС‰РµРµ РґРІРёР¶РµРЅРёРµ
            movement.enabled = false;
        }

        // РћС‚РєР»СЋС‡Р°РµРј AI
        CharacterAI ai = GetComponent<CharacterAI>();
        if (ai != null)
        {
            ai.enabled = false;
        }

        // РћСЃС‚Р°РЅР°РІР»РёРІР°РµРј РІСЃРµ Р±РѕРµРІС‹Рµ РґРµР№СЃС‚РІРёСЏ
        CombatSystem combatSystem = GetCombatSystem();
        if (combatSystem != null)
        {
            combatSystem.StopCombatForCharacter(this);
        }

        // РќР• РѕС‚РєР»СЋС‡Р°РµРј РєРѕР»Р»Р°Р№РґРµСЂ РґР»СЏ РјРµСЂС‚РІС‹С… - РЅСѓР¶РµРЅ РґР»СЏ РѕР±С‹СЃРєР°
        // Collider characterCollider = GetComponent<Collider>();
        // if (characterCollider != null)
        // {
        //     characterCollider.enabled = false;
        // }
    }

    /// <summary>
    /// РћСЃС‚Р°РЅРѕРІРєР° РІСЃРµС… Р±РѕРµРІС‹С… РґРµР№СЃС‚РІРёР№ СЃ СѓС‡Р°СЃС‚РёРµРј СЌС‚РѕРіРѕ РїРµСЂСЃРѕРЅР°Р¶Р°
    /// </summary>
    void StopCombatInvolvement()
    {
        CombatSystem combatSystem = GetCombatSystem();
        if (combatSystem != null)
        {
            // РћСЃС‚Р°РЅР°РІР»РёРІР°РµРј Р±РѕР№ РµСЃР»Рё СЌС‚РѕС‚ РїРµСЂСЃРѕРЅР°Р¶ Р°С‚Р°РєРѕРІР°Р» РєРѕРіРѕ-С‚Рѕ
            if (combatSystem.IsInCombat(this))
            {
                combatSystem.StopCombatForCharacter(this);
            }
        }
    }

    /// <summary>
    /// Р”РµР»Р°РµРј РјРµСЂС‚РІРѕРіРѕ РїРµСЂСЃРѕРЅР°Р¶Р° РґРѕСЃС‚СѓРїРЅС‹Рј РґР»СЏ РѕР±С‹СЃРєР°
    /// </summary>
    void MakeSearchable()
    {
        if (isDead)
        {
            isSearchable = true;
            hasBeenSearched = false;

            // РњРѕР¶РЅРѕ РґРѕР±Р°РІРёС‚СЊ РІРёР·СѓР°Р»СЊРЅС‹Р№ РёРЅРґРёРєР°С‚РѕСЂ С‡С‚Рѕ РјРѕР¶РЅРѕ РѕР±С‹СЃРєР°С‚СЊ
            // РќР°РїСЂРёРјРµСЂ, РёР·РјРµРЅРёС‚СЊ С†РІРµС‚ РёР»Рё РґРѕР±Р°РІРёС‚СЊ РёРєРѕРЅРєСѓ
        }
    }

    /// <summary>
    /// РћР±С‹СЃРєР°С‚СЊ РјРµСЂС‚РІРѕРіРѕ РїРµСЂСЃРѕРЅР°Р¶Р° (РІС‹Р·С‹РІР°РµС‚СЃСЏ РёР·РІРЅРµ)
    /// </summary>
    public bool SearchCorpse()
    {
        if (!isDead)
            return false; // РњРѕР¶РЅРѕ РѕР±С‹СЃРєРёРІР°С‚СЊ С‚РѕР»СЊРєРѕ РјРµСЂС‚РІС‹С…

        if (!CanBeSearched())
            return false;

        // РћС‚РєСЂС‹РІР°РµРј РёРЅРІРµРЅС‚Р°СЂСЊ РјРµСЂС‚РІРѕРіРѕ РїРµСЂСЃРѕРЅР°Р¶Р° РґР»СЏ РѕР±С‹СЃРєР°
        Inventory corpseInventory = GetComponent<Inventory>();
        if (corpseInventory != null)
        {
            // РћС‚РєСЂС‹РІР°РµРј РёРЅРІРµРЅС‚Р°СЂСЊ С‚СЂСѓРїР°
            InventoryUI inventoryUI = GetInventoryUI();
            if (inventoryUI != null)
            {
                inventoryUI.SetCurrentInventory(corpseInventory, this);
                inventoryUI.ShowInventory();
            }
            else
            {
                // Р•СЃР»Рё РЅРµС‚ UI, РІС‹Р±СЂР°СЃС‹РІР°РµРј Р»СѓС‚ РєР°Рє fallback
                DropLootOnDeath();
            }

            // РџРѕРјРµС‡Р°РµРј РєР°Рє РѕР±С‹СЃРєР°РЅРЅС‹Р№ С‡С‚РѕР±С‹ РЅРµР»СЊР·СЏ Р±С‹Р»Рѕ РѕР±С‹СЃРєР°С‚СЊ РїРѕРІС‚РѕСЂРЅРѕ
            hasBeenSearched = true;

            return true;
        }

        return false;
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ, РјРѕР¶РЅРѕ Р»Рё РѕР±С‹СЃРєР°С‚СЊ СЌС‚РѕРіРѕ РїРµСЂСЃРѕРЅР°Р¶Р°
    /// </summary>
    public bool CanBeSearched()
    {
        return isDead && isSearchable && !hasBeenSearched;
    }

    /// <summary>
    /// Р’РѕСЃСЃС‚Р°РЅРѕРІРёС‚СЊ Р·РґРѕСЂРѕРІСЊРµ
    /// </summary>
    public void Heal(float amount)
    {
        characterData.health = Mathf.Min(characterData.maxHealth, characterData.health + amount);

        // РћР±РЅРѕРІР»СЏРµРј health РІ LocationObjectInfo
        var objectInfo = GetComponent<LocationObjectInfo>();
        if (objectInfo != null)
        {
            objectInfo.health = characterData.health;
        }
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ С‚РµРєСѓС‰РµРµ Р·РґРѕСЂРѕРІСЊРµ
    /// </summary>
    public float GetHealth()
    {
        return characterData.health;
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РјР°РєСЃРёРјР°Р»СЊРЅРѕРµ Р·РґРѕСЂРѕРІСЊРµ
    /// </summary>
    public float GetMaxHealth()
    {
        return characterData.maxHealth;
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РїСЂРѕС†РµРЅС‚ Р·РґРѕСЂРѕРІСЊСЏ (0.0 - 1.0)
    /// </summary>
    public float GetHealthPercent()
    {
        if (characterData.maxHealth <= 0) return 0f;
        return characterData.health / characterData.maxHealth;
    }

    /// <summary>
    /// РЈСЃС‚Р°РЅРѕРІРёС‚СЊ Р·РґРѕСЂРѕРІСЊРµ (РґР»СЏ С‚РµСЃС‚РёСЂРѕРІР°РЅРёСЏ)
    /// </summary>
    public void SetHealth(float health)
    {
        characterData.health = Mathf.Clamp(health, 0, characterData.maxHealth);

        // РћР±РЅРѕРІР»СЏРµРј health РІ LocationObjectInfo
        var objectInfo = GetComponent<LocationObjectInfo>();
        if (objectInfo != null)
        {
            objectInfo.health = characterData.health;
        }
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ С„СЂР°РєС†РёСЋ РїРµСЂСЃРѕРЅР°Р¶Р°
    /// </summary>
    public Faction GetFaction()
    {
        return characterData.faction;
    }

    /// <summary>
    /// РЈСЃС‚Р°РЅРѕРІРёС‚СЊ С„СЂР°РєС†РёСЋ РїРµСЂСЃРѕРЅР°Р¶Р°
    /// </summary>
    public void SetFaction(Faction faction)
    {
        Faction oldFaction = characterData.faction;
        characterData.faction = faction;

        // Р•СЃР»Рё РїРµСЂСЃРѕРЅР°Р¶ РїРµСЂРµС€РµР» РІ С„СЂР°РєС†РёСЋ РёРіСЂРѕРєР°, РїСѓР±Р»РёРєСѓРµРј СЃРѕР±С‹С‚РёРµ СЃРїР°РІРЅР°
        if (oldFaction != Faction.Player && faction == Faction.Player)
        {
            // ARCHITECTURE: РџСѓР±Р»РёРєСѓРµРј СЃРѕР±С‹С‚РёРµ С‡РµСЂРµР· EventBus
            EventBus.Publish(new CharacterSpawnedEvent(this));

            // DEPRECATED: РџРѕРґРґРµСЂР¶РєР° СЃС‚Р°СЂРѕРіРѕ API РґР»СЏ РѕР±СЂР°С‚РЅРѕР№ СЃРѕРІРјРµСЃС‚РёРјРѕСЃС‚Рё
            #pragma warning disable CS0618
            if (OnPlayerCharacterSpawned != null)
            {
                OnPlayerCharacterSpawned.Invoke(this);
            }
            #pragma warning restore CS0618
        }
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ, РјРµСЂС‚РІ Р»Рё РїРµСЂСЃРѕРЅР°Р¶
    /// </summary>
    public bool IsDead()
    {
        return isDead;
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ, СЏРІР»СЏРµС‚СЃСЏ Р»Рё РїРµСЂСЃРѕРЅР°Р¶ РёРіСЂРѕРєРѕРј
    /// </summary>
    public bool IsPlayerCharacter()
    {
        bool isPlayer = characterData.faction == Faction.Player;
        return isPlayer;
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ, СЏРІР»СЏРµС‚СЃСЏ Р»Рё РїРµСЂСЃРѕРЅР°Р¶ РІСЂР°РіРѕРј
    /// </summary>
    public bool IsEnemyCharacter()
    {
        return characterData.faction == Faction.Enemy;
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ, СЏРІР»СЏСЋС‚СЃСЏ Р»Рё РґРІР° РїРµСЂСЃРѕРЅР°Р¶Р° СЃРѕСЋР·РЅРёРєР°РјРё
    /// </summary>
    public bool IsAllyWith(Character otherCharacter)
    {
        if (otherCharacter == null) return false;
        return characterData.faction == otherCharacter.characterData.faction;
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ, СЏРІР»СЏСЋС‚СЃСЏ Р»Рё РґРІР° РїРµСЂСЃРѕРЅР°Р¶Р° РІСЂР°РіР°РјРё
    /// </summary>
    public bool IsEnemyWith(Character otherCharacter)
    {
        if (otherCharacter == null) return false;

        // РРіСЂРѕРєРё Рё РІСЂР°РіРё - РІСЂР°РіРё РґСЂСѓРі РґСЂСѓРіСѓ
        if ((characterData.faction == Faction.Player && otherCharacter.characterData.faction == Faction.Enemy) ||
            (characterData.faction == Faction.Enemy && otherCharacter.characterData.faction == Faction.Player))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РїРѕСЃР»РµРґРЅРµРіРѕ Р°С‚Р°РєСѓСЋС‰РµРіРѕ РґР»СЏ СЃРёСЃС‚РµРјС‹ РєРѕРЅС‚СЂР°С‚Р°РєРё
    /// </summary>
    public Character GetLastAttacker()
    {
        // РџСЂРѕРІРµСЂСЏРµРј С‚Р°Р№РјР°СѓС‚ - РµСЃР»Рё РїСЂРѕС€Р»Рѕ РјРЅРѕРіРѕ РІСЂРµРјРµРЅРё, Р·Р°Р±С‹РІР°РµРј РѕР± Р°С‚Р°РєСѓСЋС‰РµРј
        if (Time.time - lastAttackTime > counterAttackTimeout)
        {
            lastAttacker = null;
        }

        // РџСЂРѕРІРµСЂСЏРµРј С‡С‚Рѕ Р°С‚Р°РєСѓСЋС‰РёР№ РµС‰Рµ Р¶РёРІ
        if (lastAttacker != null && lastAttacker.IsDead())
        {
            lastAttacker = null;
        }

        return lastAttacker;
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ, РµСЃС‚СЊ Р»Рё Р°РєС‚РёРІРЅС‹Р№ Р°С‚Р°РєСѓСЋС‰РёР№ РґР»СЏ РєРѕРЅС‚СЂР°С‚Р°РєРё
    /// </summary>
    public bool HasActiveAttacker()
    {
        return GetLastAttacker() != null;
    }

    /// <summary>
    /// РћС‡РёСЃС‚РёС‚СЊ РёРЅС„РѕСЂРјР°С†РёСЋ РѕР± Р°С‚Р°РєСѓСЋС‰РµРј (РІС‹Р·С‹РІР°РµС‚СЃСЏ РїСЂРё РЅР°С‡Р°Р»Рµ РєРѕРЅС‚СЂР°С‚Р°РєРё)
    /// </summary>
    public void ClearLastAttacker()
    {
        lastAttacker = null;
        lastAttackTime = 0f;
    }

    /// <summary>
    /// DEPRECATED: РџСЂРѕРІРµСЂРєР° РЅР°РІРµРґРµРЅРёСЏ РјС‹С€Рё РЅР° РїРµСЂСЃРѕРЅР°Р¶Р°
    /// PERFORMANCE FIX: Р­С‚РѕС‚ РјРµС‚РѕРґ Р±РѕР»СЊС€Рµ РЅРµ РёСЃРїРѕР»СЊР·СѓРµС‚СЃСЏ!
    /// Hover С‚РµРїРµСЂСЊ РѕР±СЂР°Р±Р°С‚С‹РІР°РµС‚СЃСЏ С†РµРЅС‚СЂР°Р»РёР·РѕРІР°РЅРЅРѕ С‡РµСЂРµР· SelectionManager.HandleHover()
    /// Р­С‚Рѕ СЌРєРѕРЅРѕРјРёС‚ СЃРѕС‚РЅРё raycast РІ СЃРµРєСѓРЅРґСѓ (РѕРґРёРЅ raycast РІРјРµСЃС‚Рѕ N raycast РґР»СЏ N РїРµСЂСЃРѕРЅР°Р¶РµР№)
    ///
    /// РћСЃС‚Р°РІР»РµРЅ РґР»СЏ СЃРїСЂР°РІРєРё, РјРѕР¶РЅРѕ СѓРґР°Р»РёС‚СЊ РІ Р±СѓРґСѓС‰РµРј
    /// </summary>
    /*
    void CheckMouseHover()
    {
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        bool shouldHover = false;

        // РСЃРїРѕР»СЊР·СѓРµРј RaycastAll С‡С‚РѕР±С‹ РїСЂРѕРІРµСЂРёС‚СЊ РІСЃРµ РѕР±СЉРµРєС‚С‹ РЅР° Р»СѓС‡Рµ
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject == gameObject)
            {
                shouldHover = true;
                break;
            }
        }

        // РћР±РЅРѕРІР»СЏРµРј СЃРѕСЃС‚РѕСЏРЅРёРµ hover
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
    */

    /// <summary>
    /// РћС‚Р»РѕР¶РµРЅРЅР°СЏ РЅР°СЃС‚СЂРѕР№РєР° РёРЅРІРµРЅС‚Р°СЂСЏ РґР»СЏ РїСЂР°РІРёР»СЊРЅРѕР№ РёРЅРёС†РёР°Р»РёР·Р°С†РёРё
    /// </summary>
    System.Collections.IEnumerator DelayedInventorySetup()
    {
        // Р–РґРµРј РѕРґРёРЅ РєР°РґСЂ РґР»СЏ Р·Р°РІРµСЂС€РµРЅРёСЏ РёРЅРёС†РёР°Р»РёР·Р°С†РёРё РєРѕРјРїРѕРЅРµРЅС‚РѕРІ
        yield return null;

        // РќР°СЃС‚СЂР°РёРІР°РµРј РёРЅРІРµРЅС‚Р°СЂСЊ РІ Р·Р°РІРёСЃРёРјРѕСЃС‚Рё РѕС‚ С„СЂР°РєС†РёРё
        SetupInventoryForFaction();
    }

    void OnDestroy()
    {
        // РћСЃРІРѕР±РѕР¶РґР°РµРј РјР°С‚РµСЂРёР°Р»
        if (characterMaterial != null)
        {
            DestroyImmediate(characterMaterial);
        }
    }
    
    /// <summary>
    /// РќР°СЃС‚СЂРѕРёС‚СЊ РёРЅРІРµРЅС‚Р°СЂСЊ РІ Р·Р°РІРёСЃРёРјРѕСЃС‚Рё РѕС‚ С„СЂР°РєС†РёРё
    /// </summary>
    void SetupInventoryForFaction()
    {
        if (characterInventory == null) return;

        switch (characterData.faction)
        {
            case Faction.Player:
                // РЎРѕСЋР·РЅРёРєРё РёРјРµСЋС‚ Р±РѕР»СЊС€РёР№ РёРЅРІРµРЅС‚Р°СЂСЊ
                characterInventory.maxSlots = GameConstants.Character.PLAYER_INVENTORY_SLOTS;
                characterInventory.maxWeight = GameConstants.Character.PLAYER_INVENTORY_MAX_WEIGHT;
                // РћРўРљР›Р®Р§Р•РќРћ: Р°РІС‚РѕРїРѕРґР±РѕСЂ С‚РµРїРµСЂСЊ СЂР°Р±РѕС‚Р°РµС‚ С‚РѕР»СЊРєРѕ РїРѕ РџРљРњ
                characterInventory.autoPickupEnabled = false;
                characterInventory.autoPickupRange = GameConstants.Items.PICKUP_RANGE;

                // Р“РµРЅРµСЂРёСЂСѓРµРј СЃС‚Р°СЂС‚РѕРІС‹Рµ РїСЂРµРґРјРµС‚С‹ РґР»СЏ СЃРѕСЋР·РЅРёРєРѕРІ
                GeneratePlayerStartingItems();
                break;

            case Faction.Enemy:
                // Р’СЂР°РіРё РёРјРµСЋС‚ РѕРіСЂР°РЅРёС‡РµРЅРЅС‹Р№ РёРЅРІРµРЅС‚Р°СЂСЊ
                characterInventory.maxSlots = GameConstants.Character.ENEMY_INVENTORY_SLOTS;
                characterInventory.maxWeight = GameConstants.Character.ENEMY_INVENTORY_MAX_WEIGHT;
                characterInventory.autoPickupEnabled = false;

                // Р“РµРЅРµСЂРёСЂСѓРµРј СЃР»СѓС‡Р°Р№РЅС‹Рµ РїСЂРµРґРјРµС‚С‹ РґР»СЏ РІСЂР°РіРѕРІ
                GenerateEnemyLoot();
                break;

            case Faction.Neutral:
                // РќРµР№С‚СЂР°Р»СЊРЅС‹Рµ РїРµСЂСЃРѕРЅР°Р¶Рё РёРјРµСЋС‚ СЃСЂРµРґРЅРёР№ РёРЅРІРµРЅС‚Р°СЂСЊ
                characterInventory.maxSlots = GameConstants.Character.NEUTRAL_INVENTORY_SLOTS;
                characterInventory.maxWeight = GameConstants.Character.NEUTRAL_INVENTORY_MAX_WEIGHT;
                characterInventory.autoPickupEnabled = false;

                // Р“РµРЅРµСЂРёСЂСѓРµРј СЃС‚Р°СЂС‚РѕРІС‹Рµ РїСЂРµРґРјРµС‚С‹ РґР»СЏ РЅРµР№С‚СЂР°Р»СЊРЅС‹С… РїРµСЂСЃРѕРЅР°Р¶РµР№
                GenerateNeutralStartingItems();
                break;
        }
    }

    /// <summary>
    /// Р“РµРЅРµСЂРёСЂРѕРІР°С‚СЊ СЃС‚Р°СЂС‚РѕРІС‹Рµ РїСЂРµРґРјРµС‚С‹ РґР»СЏ СЃРѕСЋР·РЅРёРєРѕРІ
    /// </summary>
    void GeneratePlayerStartingItems()
    {
        if (characterInventory == null || characterData.faction != Faction.Player)
            return;

        // Р”РѕР±Р°РІР»СЏРµРј РїРѕ 1 РїСЂРµРґРјРµС‚Сѓ РєР°Р¶РґРѕРіРѕ С‚РёРїР°
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
    /// Р“РµРЅРµСЂРёСЂРѕРІР°С‚СЊ СЃС‚Р°СЂС‚РѕРІС‹Рµ РїСЂРµРґРјРµС‚С‹ РґР»СЏ РЅРµР№С‚СЂР°Р»СЊРЅС‹С… РїРµСЂСЃРѕРЅР°Р¶РµР№
    /// </summary>
    void GenerateNeutralStartingItems()
    {
        if (characterInventory == null || characterData.faction != Faction.Neutral)
            return;

        // Р”РѕР±Р°РІР»СЏРµРј РїРѕ 1 РїСЂРµРґРјРµС‚Сѓ РєР°Р¶РґРѕРіРѕ С‚РёРїР°
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
    /// Р“РµРЅРµСЂРёСЂРѕРІР°С‚СЊ РґРѕР±С‹С‡Сѓ РґР»СЏ РІСЂР°РіРѕРІ
    /// </summary>
    void GenerateEnemyLoot()
    {
        if (characterInventory == null || characterData.faction != Faction.Enemy)
            return;

        // Р”РѕР±Р°РІР»СЏРµРј РїРѕ 1 РїСЂРµРґРјРµС‚Сѓ РєР°Р¶РґРѕРіРѕ С‚РёРїР°
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
    /// РЎРѕР·РґР°С‚СЊ РїСЂРµРґРјРµС‚ РѕРїСЂРµРґРµР»РµРЅРЅРѕРіРѕ С‚РёРїР°
    /// </summary>
    ItemData CreateSpecificLoot(ItemType itemType)
    {
        ItemData item = new ItemData();
        item.itemType = itemType;

        // РќР°СЃС‚СЂР°РёРІР°РµРј РїСЂРµРґРјРµС‚ РІ Р·Р°РІРёСЃРёРјРѕСЃС‚Рё РѕС‚ С‚РёРїР°
        switch (itemType)
        {
            case ItemType.Weapon:
                item.itemName = ItemNames.WEAPON;
                item.description = "Simple weapon found on enemy";
                item.damage = staticRandom.Next(5, 15);
                item.value = staticRandom.Next(10, 50);
                item.weight = 2f;
                item.rarity = ItemRarity.Common;
                item.equipmentSlot = EquipmentSlot.RightHand;
                break;

            case ItemType.Medical:
                item.itemName = ItemNames.MEDKIT;
                item.description = "Basic medical supplies";
                item.healing = staticRandom.Next(10, 30);
                item.value = staticRandom.Next(15, 40);
                item.weight = 0.5f;
                item.maxStackSize = 5;
                item.rarity = ItemRarity.Common;
                break;

            case ItemType.Resource:
                item.itemName = ItemNames.RESOURCE;
                item.description = "Useful crafting material";
                item.value = staticRandom.Next(5, 15);
                item.weight = 0.3f;
                item.maxStackSize = 10;
                item.rarity = ItemRarity.Common;
                break;

            case ItemType.Tool:
                item.itemName = ItemNames.TOOL;
                item.description = "Simple maintenance tool";
                item.value = staticRandom.Next(8, 25);
                item.weight = 1f;
                item.rarity = ItemRarity.Common;
                break;

            case ItemType.Armor:
                // РЎРѕР·РґР°РµРј СЃР»СѓС‡Р°Р№РЅСѓСЋ Р±СЂРѕРЅСЋ РґР»СЏ СЂР°Р·РЅС‹С… СЃР»РѕС‚РѕРІ
                EquipmentSlot[] armorSlots = { EquipmentSlot.Head, EquipmentSlot.Chest, EquipmentSlot.Legs, EquipmentSlot.Feet };
                EquipmentSlot armorSlot = armorSlots[staticRandom.Next(0, armorSlots.Length)];

                switch (armorSlot)
                {
                    case EquipmentSlot.Head:
                        item.itemName = ItemNames.HELMET;
                        item.description = "Protective helmet";
                        break;
                    case EquipmentSlot.Chest:
                        item.itemName = ItemNames.BODY_ARMOR;
                        item.description = "Chest protection";
                        break;
                    case EquipmentSlot.Legs:
                        item.itemName = ItemNames.PANTS;
                        item.description = "Leg protection";
                        break;
                    case EquipmentSlot.Feet:
                        item.itemName = ItemNames.BOOTS;
                        item.description = "Protective boots";
                        break;
                }

                item.equipmentSlot = armorSlot;
                item.armor = staticRandom.Next(2, 8);
                item.weight = staticRandom.Next(1, 4);
                item.value = staticRandom.Next(15, 40);
                item.rarity = ItemRarity.Common;
                break;

            case ItemType.Consumable:
                item.itemName = ItemNames.CONSUMABLE;
                item.description = "Single-use item";
                item.value = staticRandom.Next(5, 20);
                item.weight = 0.2f;
                item.maxStackSize = 5;
                item.rarity = ItemRarity.Common;
                break;
        }

        // РџСЂРёРјРµРЅСЏРµРј РёРєРѕРЅРєСѓ С‡РµСЂРµР· С„Р°Р±СЂРёРєСѓ
        ItemFactory.ApplyIcon(item);

        return item;
    }

    /// <summary>
    /// РЎРѕР·РґР°С‚СЊ СЃР»СѓС‡Р°Р№РЅС‹Р№ РїСЂРµРґРјРµС‚ РґР»СЏ РґРѕР±С‹С‡Рё (СЃС‚Р°СЂС‹Р№ РјРµС‚РѕРґ РґР»СЏ СЃРѕРІРјРµСЃС‚РёРјРѕСЃС‚Рё)
    /// </summary>
    ItemData CreateRandomLoot()
    {
        // РЎР»СѓС‡Р°Р№РЅС‹Р№ С‚РёРї РїСЂРµРґРјРµС‚Р°
        ItemType[] availableTypes = { ItemType.Weapon, ItemType.Armor, ItemType.Tool, ItemType.Medical, ItemType.Resource, ItemType.Consumable };
        ItemType randomType = availableTypes[staticRandom.Next(0, availableTypes.Length)];
        return CreateSpecificLoot(randomType);
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РёРЅРІРµРЅС‚Р°СЂСЊ РїРµСЂСЃРѕРЅР°Р¶Р°
    /// </summary>
    public Inventory GetInventory()
    {
        return characterInventory;
    }

    /// <summary>
    /// Р”РѕР±Р°РІРёС‚СЊ РїСЂРµРґРјРµС‚ РІ РёРЅРІРµРЅС‚Р°СЂСЊ РїРµСЂСЃРѕРЅР°Р¶Р°
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
    /// РЈРґР°Р»РёС‚СЊ РїСЂРµРґРјРµС‚ РёР· РёРЅРІРµРЅС‚Р°СЂСЏ РїРµСЂСЃРѕРЅР°Р¶Р°
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
    /// РџСЂРѕРІРµСЂРёС‚СЊ, РµСЃС‚СЊ Р»Рё РїСЂРµРґРјРµС‚ РІ РёРЅРІРµРЅС‚Р°СЂРµ
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
    /// Р’С‹Р±СЂРѕСЃРёС‚СЊ РїСЂРµРґРјРµС‚С‹ РїСЂРё СЃРјРµСЂС‚Рё
    /// </summary>
    void DropLootOnDeath()
    {
        if (characterInventory == null) return;

        var usedSlots = characterInventory.GetUsedSlotsList();
        foreach (var slot in usedSlots)
        {
            if (!slot.IsEmpty())
            {
                // РЎРѕР·РґР°РµРј РїСЂРµРґРјРµС‚ РІ РјРёСЂРµ СЂСЏРґРѕРј СЃ РїРµСЂСЃРѕРЅР°Р¶РµРј
                Vector3 dropPosition = transform.position +
                    new Vector3(
                        UnityEngine.Random.Range(-1f, 1f),
                        GameConstants.Items.ITEM_SPAWN_HEIGHT,
                        UnityEngine.Random.Range(-1f, 1f)
                    );

                Item.CreateWorldItem(slot.itemData, dropPosition);
            }
        }

        // РћС‡РёС‰Р°РµРј РёРЅРІРµРЅС‚Р°СЂСЊ
        characterInventory.ClearInventory();
    }

    /// <summary>
    /// РћР±РЅРѕРІР»РµРЅРЅС‹Р№ РјРµС‚РѕРґ РїРѕР»СѓС‡РµРЅРёСЏ РёРЅС„РѕСЂРјР°С†РёРё Рѕ РїРµСЂСЃРѕРЅР°Р¶Рµ (РІРєР»СЋС‡Р°СЏ РёРЅРІРµРЅС‚Р°СЂСЊ)
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
        // РџРѕРєР°Р·С‹РІР°РµРј РёРЅС„РѕСЂРјР°С†РёСЋ Рѕ РїРµСЂСЃРѕРЅР°Р¶Рµ РІ Scene view
        Gizmos.color = isSelected ? selectedColor : defaultColor;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * GameConstants.Character.GIZMO_HEIGHT_OFFSET, GameConstants.Character.GIZMO_SPHERE_RADIUS);

        // РџРѕРєР°Р·С‹РІР°РµРј СЂР°РґРёСѓСЃ Р°РІС‚РѕРїРѕРґР±РѕСЂР° РґР»СЏ СЃРѕСЋР·РЅРёРєРѕРІ
        if (characterInventory != null && IsPlayerCharacter() && characterInventory.autoPickupEnabled)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, characterInventory.autoPickupRange);
        }
    }
}
