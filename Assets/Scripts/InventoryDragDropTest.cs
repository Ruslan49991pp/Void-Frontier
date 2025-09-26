using UnityEngine;

/// <summary>
/// Тестовый скрипт для проверки drag and drop функциональности инвентаря
/// Добавить как компонент к GameObject в сцене для проведения тестов
/// </summary>
public class InventoryDragDropTest : MonoBehaviour
{
    [Header("Test Settings")]
    public bool enableTestingOnStart = true;
    public KeyCode testKey = KeyCode.T;

    private InventoryManager inventoryManager;
    private Character testCharacter;

    void Start()
    {
        if (enableTestingOnStart)
        {
            InitializeTest();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(testKey))
        {
            RunDragDropTest();
        }
    }

    /// <summary>
    /// Инициализация теста
    /// </summary>
    void InitializeTest()
    {
        Debug.Log("[DragDropTest] Initializing drag and drop test...");

        // Находим InventoryManager
        inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null)
        {
            Debug.LogError("[DragDropTest] InventoryManager not found!");
            return;
        }

        // Находим тестового персонажа
        Character[] allCharacters = FindObjectsOfType<Character>();
        foreach (Character character in allCharacters)
        {
            if (character.IsPlayerCharacter())
            {
                testCharacter = character;
                break;
            }
        }

        if (testCharacter == null)
        {
            Debug.LogError("[DragDropTest] No player character found!");
            return;
        }

        Debug.Log("[DragDropTest] Test initialized successfully. Press T to run test.");
    }

    /// <summary>
    /// Запуск теста drag and drop
    /// </summary>
    void RunDragDropTest()
    {
        if (testCharacter == null)
        {
            Debug.LogError("[DragDropTest] Test character not available!");
            return;
        }

        Inventory inventory = testCharacter.GetInventory();
        if (inventory == null)
        {
            Debug.LogError("[DragDropTest] Test character has no inventory!");
            return;
        }

        Debug.Log("[DragDropTest] Running drag and drop test...");

        // Очищаем инвентарь
        inventory.ClearInventory();

        // Создаем тестовые предметы
        CreateTestItems(inventory);

        // Открываем инвентарь для визуального тестирования
        InventoryUI inventoryUI = inventoryManager.GetInventoryUI();
        if (inventoryUI != null)
        {
            inventoryUI.SetCurrentInventory(inventory);
            inventoryUI.ShowInventory();
            Debug.Log("[DragDropTest] Inventory opened. Now you can test drag and drop manually!");
            Debug.Log("[DragDropTest] Features to test:");
            Debug.Log("  1. TOOLTIPS: Hover mouse over any item to see detailed info");
            Debug.Log("  2. Drag items between inventory slots");
            Debug.Log("  3. Drag items from inventory to equipment slots");
            Debug.Log("  4. Drag items from equipment slots back to inventory");
            Debug.Log("  5. Stack items of the same type");
            Debug.Log("  6. Swap different items between slots");
        }
    }

    /// <summary>
    /// Создание тестовых предметов
    /// </summary>
    void CreateTestItems(Inventory inventory)
    {
        // Создаем различные типы предметов для тестирования

        // Оружие для правой руки
        ItemData weapon = new ItemData();
        weapon.itemName = "Test Sword";
        weapon.description = "A test sword for drag and drop";
        weapon.itemType = ItemType.Weapon;
        weapon.rarity = ItemRarity.Common;
        weapon.equipmentSlot = EquipmentSlot.RightHand;
        weapon.damage = 15;
        weapon.weight = 2f;
        weapon.value = 100;
        inventory.AddItem(weapon, 1);

        // Щит для левой руки
        ItemData shield = new ItemData();
        shield.itemName = "Test Shield";
        shield.description = "A protective shield";
        shield.itemType = ItemType.Armor;
        shield.rarity = ItemRarity.Common;
        shield.equipmentSlot = EquipmentSlot.LeftHand;
        shield.armor = 5;
        shield.weight = 3f;
        shield.value = 75;
        inventory.AddItem(shield, 1);

        // Броня для груди
        ItemData armor = new ItemData();
        armor.itemName = "Test Armor";
        armor.description = "Protective chest armor";
        armor.itemType = ItemType.Armor;
        armor.rarity = ItemRarity.Uncommon;
        armor.equipmentSlot = EquipmentSlot.Chest;
        armor.armor = 10;
        armor.weight = 5f;
        armor.value = 150;
        inventory.AddItem(armor, 1);

        // Шлем
        ItemData helmet = new ItemData();
        helmet.itemName = "Test Helmet";
        helmet.description = "Protective helmet";
        helmet.itemType = ItemType.Armor;
        helmet.rarity = ItemRarity.Common;
        helmet.equipmentSlot = EquipmentSlot.Head;
        helmet.armor = 3;
        helmet.weight = 1.5f;
        helmet.value = 60;
        inventory.AddItem(helmet, 1);

        // Штаны
        ItemData legs = new ItemData();
        legs.itemName = "Test Pants";
        legs.description = "Protective leg armor";
        legs.itemType = ItemType.Armor;
        legs.rarity = ItemRarity.Common;
        legs.equipmentSlot = EquipmentSlot.Legs;
        legs.armor = 4;
        legs.weight = 2f;
        legs.value = 80;
        inventory.AddItem(legs, 1);

        // Ботинки
        ItemData boots = new ItemData();
        boots.itemName = "Test Boots";
        boots.description = "Protective footwear";
        boots.itemType = ItemType.Armor;
        boots.rarity = ItemRarity.Common;
        boots.equipmentSlot = EquipmentSlot.Feet;
        boots.armor = 2;
        boots.weight = 1f;
        boots.value = 50;
        inventory.AddItem(boots, 1);

        // Медицинские предметы (стакуемые)
        ItemData medkit = new ItemData();
        medkit.itemName = "Medkit";
        medkit.description = "First aid kit";
        medkit.itemType = ItemType.Medical;
        medkit.rarity = ItemRarity.Common;
        medkit.healing = 25;
        medkit.weight = 0.5f;
        medkit.value = 50;
        medkit.maxStackSize = 5;
        inventory.AddItem(medkit, 3);

        // Ресурсы (стакуемые)
        ItemData resource = new ItemData();
        resource.itemName = "Iron Ore";
        resource.description = "Raw iron ore";
        resource.itemType = ItemType.Resource;
        resource.rarity = ItemRarity.Common;
        resource.weight = 0.3f;
        resource.value = 10;
        resource.maxStackSize = 10;
        inventory.AddItem(resource, 7);

        // Инструмент
        ItemData tool = new ItemData();
        tool.itemName = "Mining Pick";
        tool.description = "Tool for mining";
        tool.itemType = ItemType.Tool;
        tool.rarity = ItemRarity.Common;
        tool.weight = 1.5f;
        tool.value = 75;
        inventory.AddItem(tool, 1);

        Debug.Log("[DragDropTest] Created test items in inventory:");
        Debug.Log($"  - {weapon.itemName} (Equipment: {weapon.equipmentSlot})");
        Debug.Log($"  - {shield.itemName} (Equipment: {shield.equipmentSlot})");
        Debug.Log($"  - {armor.itemName} (Equipment: {armor.equipmentSlot})");
        Debug.Log($"  - {helmet.itemName} (Equipment: {helmet.equipmentSlot})");
        Debug.Log($"  - {legs.itemName} (Equipment: {legs.equipmentSlot})");
        Debug.Log($"  - {boots.itemName} (Equipment: {boots.equipmentSlot})");
        Debug.Log($"  - {medkit.itemName} x3 (Stackable)");
        Debug.Log($"  - {resource.itemName} x7 (Stackable)");
        Debug.Log($"  - {tool.itemName}");
    }

    void OnGUI()
    {
        if (testCharacter != null)
        {
            GUI.Label(new Rect(10, 10, 500, 20), $"Drag & Drop + Tooltips Test - Press {testKey} to run test");
            GUI.Label(new Rect(10, 30, 500, 20), "NEW: Hover mouse over items to see tooltips!");
        }
        else
        {
            GUI.Label(new Rect(10, 10, 400, 20), "Drag & Drop Test - No test character available");
        }
    }
}