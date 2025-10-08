using UnityEngine;

/// <summary>
/// Менеджер системы инвентаря
/// Отвечает за инициализацию и управление всей системой инвентаря
/// </summary>
public class InventoryManager : MonoBehaviour
{
    [Header("Inventory Settings")]
    public bool enableInventorySystem = true;
    public bool createTestItems = true;

    [Header("Test Items")]
    public int testItemsCount = 5;

    // Компоненты системы
    private InventoryUI inventoryUI;
    private static InventoryManager instance;

    /// <summary>
    /// Singleton экземпляр
    /// </summary>
    public static InventoryManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<InventoryManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("InventoryManager");
                    instance = go.AddComponent<InventoryManager>();
                }
            }
            return instance;
        }
    }

    void Awake()
    {
        // Обеспечиваем единственность экземпляра
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeInventorySystem();
    }

    void Start()
    {
        if (createTestItems)
        {
            CreateTestItems();
        }
    }

    /// <summary>
    /// Инициализация системы инвентаря
    /// </summary>
    void InitializeInventorySystem()
    {
        if (!enableInventorySystem) return;

        // Создаем UI инвентаря
        GameObject inventoryUIGO = new GameObject("InventoryUI");
        inventoryUI = inventoryUIGO.AddComponent<InventoryUI>();


    }

    /// <summary>
    /// Создать тестовые предметы в мире
    /// </summary>
    void CreateTestItems()
    {
        if (!enableInventorySystem) return;



        // Создаем полный набор тестовых предметов для проверки всех слотов
        CreateTestItemSet();
    }

    void CreateTestItemSet()
    {
        int itemIndex = 1;

        // 1. Weapon
        ItemData weapon = CreateSpecificTestItem(ItemType.Weapon, EquipmentSlot.RightHand, itemIndex++);
        CreateWorldTestItem(weapon);

        // 2-5. All armor types
        CreateWorldTestItem(CreateSpecificTestItem(ItemType.Armor, EquipmentSlot.Head, itemIndex++));
        CreateWorldTestItem(CreateSpecificTestItem(ItemType.Armor, EquipmentSlot.Chest, itemIndex++));
        CreateWorldTestItem(CreateSpecificTestItem(ItemType.Armor, EquipmentSlot.Legs, itemIndex++));
        CreateWorldTestItem(CreateSpecificTestItem(ItemType.Armor, EquipmentSlot.Feet, itemIndex++));

        // 6. Medical
        CreateWorldTestItem(CreateSpecificTestItem(ItemType.Medical, EquipmentSlot.None, itemIndex++));

        // 7. Tool
        CreateWorldTestItem(CreateSpecificTestItem(ItemType.Tool, EquipmentSlot.None, itemIndex++));

        // 8. Resource
        CreateWorldTestItem(CreateSpecificTestItem(ItemType.Resource, EquipmentSlot.None, itemIndex++));
    }

    void CreateWorldTestItem(ItemData item)
    {
        // Спавн в центре случайной клетки сетки
        int randomX = Random.Range(-5, 6); // От -5 до 5
        int randomZ = Random.Range(-5, 6);

        // Центр клетки: целочисленные координаты + 0.5
        Vector3 position = new Vector3(
            randomX + 0.5f,
            0.5f,
            randomZ + 0.5f
        );

        GameObject worldItem = Item.CreateWorldItem(item, position);

    }

    /// <summary>
    /// Создать конкретный тестовый предмет
    /// </summary>
    ItemData CreateSpecificTestItem(ItemType itemType, EquipmentSlot equipSlot, int index)
    {
        ItemData item = new ItemData();
        item.itemType = itemType;
        item.rarity = ItemRarity.Common;

        switch (itemType)
        {
            case ItemType.Weapon:
                item.itemName = ItemNames.WEAPON;
                item.description = "A test weapon for combat";
                item.damage = Random.Range(10, 30);
                item.weight = 2.5f;
                item.value = Random.Range(50, 150);
                item.equipmentSlot = equipSlot;
                break;

            case ItemType.Armor:
                switch (equipSlot)
                {
                    case EquipmentSlot.Head:
                        item.itemName = ItemNames.HELMET;
                        item.description = "Protective helmet for head";
                        break;
                    case EquipmentSlot.Chest:
                        item.itemName = ItemNames.BODY_ARMOR;
                        item.description = "Protective chest armor";
                        break;
                    case EquipmentSlot.Legs:
                        item.itemName = ItemNames.PANTS;
                        item.description = "Protective leg armor";
                        break;
                    case EquipmentSlot.Feet:
                        item.itemName = ItemNames.BOOTS;
                        item.description = "Protective footwear";
                        break;
                }
                item.armor = Random.Range(5, 20);
                item.weight = 3f;
                item.value = Random.Range(40, 120);
                item.equipmentSlot = equipSlot;
                break;

            case ItemType.Medical:
                item.itemName = ItemNames.MEDKIT;
                item.description = "Medical supplies for healing";
                item.healing = Random.Range(15, 40);
                item.weight = 0.5f;
                item.value = Random.Range(25, 60);
                item.maxStackSize = 5;
                break;

            case ItemType.Tool:
                item.itemName = ItemNames.TOOL;
                item.description = "Useful tool for various tasks";
                item.weight = 1.5f;
                item.value = Random.Range(20, 80);
                break;

            case ItemType.Resource:
                item.itemName = ItemNames.RESOURCE;
                item.description = "Valuable crafting material";
                item.weight = 0.3f;
                item.value = Random.Range(10, 30);
                item.maxStackSize = 10;
                break;
        }

        // Применяем иконку через фабрику
        ItemFactory.ApplyIcon(item);

        return item;
    }

    /// <summary>
    /// Создать тестовый предмет (старый метод)
    /// </summary>
    ItemData CreateTestItem(int index)
    {
        ItemData item = new ItemData();

        // Создаем различные типы предметов для тестирования
        ItemType[] types = { ItemType.Weapon, ItemType.Armor, ItemType.Medical, ItemType.Tool, ItemType.Resource };
        ItemRarity[] rarities = { ItemRarity.Common, ItemRarity.Uncommon, ItemRarity.Rare };

        item.itemType = types[index % types.Length];
        item.rarity = rarities[Random.Range(0, rarities.Length)];

        switch (item.itemType)
        {
            case ItemType.Weapon:
                item.itemName = $"Test Weapon {index + 1}";
                item.description = "A test weapon for combat";
                item.damage = Random.Range(10, 30);
                item.weight = 2.5f;
                item.value = Random.Range(50, 150);
                // Weapon can be equipped in any hand - will be handled by equip logic
                item.equipmentSlot = EquipmentSlot.RightHand; // Default, but can be placed in either hand
                break;

            case ItemType.Armor:
                // Create different armor types based on index
                EquipmentSlot[] armorSlots = { EquipmentSlot.Head, EquipmentSlot.Chest, EquipmentSlot.Legs, EquipmentSlot.Feet };
                EquipmentSlot armorSlot = armorSlots[index % armorSlots.Length];

                switch (armorSlot)
                {
                    case EquipmentSlot.Head:
                        item.itemName = $"Test Helmet {index + 1}";
                        item.description = "Protective helmet for head";
                        break;
                    case EquipmentSlot.Chest:
                        item.itemName = $"Test Armor {index + 1}";
                        item.description = "Protective chest armor";
                        break;
                    case EquipmentSlot.Legs:
                        item.itemName = $"Test Pants {index + 1}";
                        item.description = "Protective leg armor";
                        break;
                    case EquipmentSlot.Feet:
                        item.itemName = $"Test Boots {index + 1}";
                        item.description = "Protective footwear";
                        break;
                }

                item.armor = Random.Range(5, 20);
                item.weight = 3f;
                item.value = Random.Range(40, 120);
                item.equipmentSlot = armorSlot;
                break;

            case ItemType.Medical:
                item.itemName = $"Medkit {index + 1}";
                item.description = "Medical supplies for healing";
                item.healing = Random.Range(15, 40);
                item.weight = 0.5f;
                item.value = Random.Range(25, 60);
                item.maxStackSize = 5;
                break;

            case ItemType.Tool:
                item.itemName = $"Tool {index + 1}";
                item.description = "Useful tool for various tasks";
                item.weight = 1.5f;
                item.value = Random.Range(20, 80);
                break;

            case ItemType.Resource:
                item.itemName = $"Resource {index + 1}";
                item.description = "Valuable crafting material";
                item.weight = 0.3f;
                item.value = Random.Range(10, 30);
                item.maxStackSize = 10;
                break;
        }

        return item;
    }

    /// <summary>
    /// Добавить предмет персонажу
    /// </summary>
    public bool GiveItemToCharacter(Character character, ItemData item, int quantity = 1)
    {
        if (character == null || item == null) return false;

        Inventory inventory = character.GetInventory();
        if (inventory != null)
        {
            return inventory.AddItem(item, quantity);
        }

        return false;
    }

    /// <summary>
    /// Создать предмет в мире рядом с персонажем
    /// </summary>
    public GameObject CreateItemNearCharacter(Character character, ItemData item)
    {
        if (character == null || item == null) return null;

        Vector3 characterPos = character.transform.position;
        Vector3 offset = character.transform.forward * 2f;
        Vector3 targetPos = characterPos + offset;

        // Округляем до ближайшего центра клетки
        int gridX = Mathf.RoundToInt(targetPos.x);
        int gridZ = Mathf.RoundToInt(targetPos.z);

        Vector3 dropPosition = new Vector3(
            gridX + 0.5f,
            0.5f,
            gridZ + 0.5f
        );

        return Item.CreateWorldItem(item, dropPosition);
    }

    /// <summary>
    /// Получить UI инвентаря
    /// </summary>
    public InventoryUI GetInventoryUI()
    {
        return inventoryUI;
    }

    /// <summary>
    /// Включить/выключить систему инвентаря
    /// </summary>
    public void SetInventorySystemEnabled(bool enabled)
    {
        enableInventorySystem = enabled;

        if (inventoryUI != null)
        {
            inventoryUI.gameObject.SetActive(enabled);
        }
    }

    /// <summary>
    /// Создать случайный предмет определенного типа
    /// </summary>
    public ItemData CreateRandomItem(ItemType itemType, ItemRarity rarity = ItemRarity.Common)
    {
        ItemData item = new ItemData();
        item.itemType = itemType;
        item.rarity = rarity;

        switch (itemType)
        {
            case ItemType.Weapon:
                item.itemName = "Random Weapon";
                item.description = "A randomly generated weapon";
                item.damage = Random.Range(5, 25);
                item.weight = Random.Range(1f, 4f);
                item.value = Random.Range(30, 100);
                break;

            case ItemType.Armor:
                item.itemName = "Random Armor";
                item.description = "Randomly generated protective gear";
                item.armor = Random.Range(3, 15);
                item.weight = Random.Range(2f, 5f);
                item.value = Random.Range(25, 80);
                break;

            case ItemType.Medical:
                item.itemName = "Medical Supply";
                item.description = "Medical item for healing";
                item.healing = Random.Range(10, 30);
                item.weight = Random.Range(0.2f, 0.8f);
                item.value = Random.Range(15, 45);
                item.maxStackSize = Random.Range(3, 8);
                break;

            case ItemType.Tool:
                item.itemName = "Utility Tool";
                item.description = "A useful tool";
                item.weight = Random.Range(0.5f, 2f);
                item.value = Random.Range(10, 50);
                break;

            case ItemType.Resource:
                item.itemName = "Crafting Material";
                item.description = "Material for crafting";
                item.weight = Random.Range(0.1f, 0.5f);
                item.value = Random.Range(5, 20);
                item.maxStackSize = Random.Range(5, 15);
                break;

            case ItemType.Consumable:
                item.itemName = "Consumable Item";
                item.description = "Single-use item";
                item.weight = Random.Range(0.1f, 0.3f);
                item.value = Random.Range(8, 25);
                item.maxStackSize = Random.Range(3, 10);
                break;
        }

        // Модифицируем характеристики на основе редкости
        float rarityMultiplier = GetRarityMultiplier(rarity);
        item.damage = Mathf.RoundToInt(item.damage * rarityMultiplier);
        item.armor = Mathf.RoundToInt(item.armor * rarityMultiplier);
        item.healing = Mathf.RoundToInt(item.healing * rarityMultiplier);
        item.value = Mathf.RoundToInt(item.value * rarityMultiplier);

        return item;
    }

    /// <summary>
    /// Получить множитель характеристик для редкости
    /// </summary>
    float GetRarityMultiplier(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return 1f;
            case ItemRarity.Uncommon: return 1.2f;
            case ItemRarity.Rare: return 1.5f;
            case ItemRarity.Epic: return 2f;
            case ItemRarity.Legendary: return 3f;
            default: return 1f;
        }
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
