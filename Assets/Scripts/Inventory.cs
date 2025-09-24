using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Слот инвентаря
/// </summary>
[System.Serializable]
public class InventorySlot
{
    public ItemData itemData;
    public int quantity;

    public InventorySlot()
    {
        itemData = null;
        quantity = 0;
    }

    public InventorySlot(ItemData item, int qty)
    {
        itemData = item;
        quantity = qty;
    }

    /// <summary>
    /// Проверить, пустой ли слот
    /// </summary>
    public bool IsEmpty()
    {
        return itemData == null || quantity <= 0;
    }

    /// <summary>
    /// Очистить слот
    /// </summary>
    public void Clear()
    {
        itemData = null;
        quantity = 0;
    }

    /// <summary>
    /// Проверить, можно ли добавить предмет в этот слот
    /// </summary>
    public bool CanAddItem(ItemData item, int qty)
    {
        if (IsEmpty())
            return true;

        if (itemData.itemName == item.itemName)
        {
            return quantity + qty <= itemData.maxStackSize;
        }

        return false;
    }

    /// <summary>
    /// Добавить предмет в слот
    /// </summary>
    public int AddItem(ItemData item, int qty)
    {
        if (IsEmpty())
        {
            itemData = item;
            quantity = Mathf.Min(qty, item.maxStackSize);
            return qty - quantity; // Возвращаем остаток
        }

        if (itemData.itemName == item.itemName)
        {
            int canAdd = Mathf.Min(qty, itemData.maxStackSize - quantity);
            quantity += canAdd;
            return qty - canAdd; // Возвращаем остаток
        }

        return qty; // Не можем добавить
    }

    /// <summary>
    /// Удалить предмет из слота
    /// </summary>
    public int RemoveItem(int qty)
    {
        if (IsEmpty())
            return 0;

        int removed = Mathf.Min(qty, quantity);
        quantity -= removed;

        if (quantity <= 0)
        {
            Clear();
        }

        return removed;
    }
}

/// <summary>
/// Система инвентаря
/// </summary>
public class Inventory : MonoBehaviour
{
    [Header("Inventory Settings")]
    public int maxSlots = 20;
    public float maxWeight = 100f;
    public bool unlimitedWeight = false;

    [Header("Auto-Pickup")]
    public bool autoPickupEnabled = true;
    public float autoPickupRange = 1.5f;
    public LayerMask itemLayer = -1;

    [Header("Debug")]
    public bool debugMode = false;

    // События
    public System.Action<ItemData, int> OnItemAdded;
    public System.Action<ItemData, int> OnItemRemoved;
    public System.Action OnInventoryChanged;
    public System.Action OnEquipmentChanged;

    // Слоты инвентаря
    private List<InventorySlot> slots;

    // Слоты экипировки
    private Dictionary<EquipmentSlot, InventorySlot> equipmentSlots;

    void Awake()
    {
        // Инициализируем слоты
        slots = new List<InventorySlot>();
        for (int i = 0; i < maxSlots; i++)
        {
            slots.Add(new InventorySlot());
        }

        // Инициализируем слоты экипировки
        equipmentSlots = new Dictionary<EquipmentSlot, InventorySlot>();
        equipmentSlots[EquipmentSlot.LeftHand] = new InventorySlot();
        equipmentSlots[EquipmentSlot.RightHand] = new InventorySlot();
        equipmentSlots[EquipmentSlot.Head] = new InventorySlot();
        equipmentSlots[EquipmentSlot.Chest] = new InventorySlot();
        equipmentSlots[EquipmentSlot.Legs] = new InventorySlot();
        equipmentSlots[EquipmentSlot.Feet] = new InventorySlot();
    }

    void Update()
    {
        if (autoPickupEnabled)
        {
            CheckForNearbyItems();
        }
    }

    /// <summary>
    /// Проверить наличие предметов поблизости для автоподбора
    /// </summary>
    void CheckForNearbyItems()
    {
        Collider[] nearbyItems = Physics.OverlapSphere(transform.position, autoPickupRange, itemLayer);

        foreach (Collider itemCollider in nearbyItems)
        {
            Item item = itemCollider.GetComponent<Item>();
            if (item != null && item.canBePickedUp)
            {
                Character character = GetComponent<Character>();
                if (character != null && item.CanBePickedUpBy(character))
                {
                    item.PickUp(character);
                }
            }
        }
    }

    /// <summary>
    /// Добавить предмет в инвентарь
    /// </summary>
    public bool AddItem(ItemData itemData, int quantity)
    {
        if (itemData == null || quantity <= 0)
            return false;

        // Проверяем ограничение по весу
        if (!unlimitedWeight && GetCurrentWeight() + (itemData.weight * quantity) > maxWeight)
        {
            if (debugMode)
                Debug.Log($"Cannot add {itemData.itemName}: weight limit exceeded");
            return false;
        }

        int remainingQuantity = quantity;

        // Сначала пытаемся добавить в существующие стеки
        for (int i = 0; i < slots.Count && remainingQuantity > 0; i++)
        {
            InventorySlot slot = slots[i];
            if (!slot.IsEmpty() && slot.itemData.itemName == itemData.itemName)
            {
                remainingQuantity = slot.AddItem(itemData, remainingQuantity);
            }
        }

        // Затем ищем пустые слоты
        for (int i = 0; i < slots.Count && remainingQuantity > 0; i++)
        {
            InventorySlot slot = slots[i];
            if (slot.IsEmpty())
            {
                remainingQuantity = slot.AddItem(itemData, remainingQuantity);
            }
        }

        int addedQuantity = quantity - remainingQuantity;
        if (addedQuantity > 0)
        {
            OnItemAdded?.Invoke(itemData, addedQuantity);
            OnInventoryChanged?.Invoke();

            if (debugMode)
                Debug.Log($"Added {addedQuantity} {itemData.itemName} to inventory");

            return remainingQuantity == 0; // true если добавили все
        }

        return false;
    }

    /// <summary>
    /// Удалить предмет из инвентаря
    /// </summary>
    public bool RemoveItem(ItemData itemData, int quantity)
    {
        if (itemData == null || quantity <= 0)
            return false;

        int remainingToRemove = quantity;

        // Ищем предметы для удаления
        for (int i = 0; i < slots.Count && remainingToRemove > 0; i++)
        {
            InventorySlot slot = slots[i];
            if (!slot.IsEmpty() && slot.itemData.itemName == itemData.itemName)
            {
                int removed = slot.RemoveItem(remainingToRemove);
                remainingToRemove -= removed;
            }
        }

        int removedQuantity = quantity - remainingToRemove;
        if (removedQuantity > 0)
        {
            OnItemRemoved?.Invoke(itemData, removedQuantity);
            OnInventoryChanged?.Invoke();

            if (debugMode)
                Debug.Log($"Removed {removedQuantity} {itemData.itemName} from inventory");

            return remainingToRemove == 0; // true если удалили все
        }

        return false;
    }

    /// <summary>
    /// Удалить предмет из конкретного слота
    /// </summary>
    public bool RemoveItemFromSlot(int slotIndex, int quantity)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count)
            return false;

        InventorySlot slot = slots[slotIndex];
        if (slot.IsEmpty())
            return false;

        ItemData itemData = slot.itemData;
        int removed = slot.RemoveItem(quantity);

        if (removed > 0)
        {
            OnItemRemoved?.Invoke(itemData, removed);
            OnInventoryChanged?.Invoke();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Получить количество определенного предмета
    /// </summary>
    public int GetItemCount(ItemData itemData)
    {
        if (itemData == null)
            return 0;

        int totalCount = 0;
        foreach (InventorySlot slot in slots)
        {
            if (!slot.IsEmpty() && slot.itemData.itemName == itemData.itemName)
            {
                totalCount += slot.quantity;
            }
        }

        return totalCount;
    }

    /// <summary>
    /// Проверить, есть ли предмет в инвентаре
    /// </summary>
    public bool HasItem(ItemData itemData, int quantity = 1)
    {
        return GetItemCount(itemData) >= quantity;
    }

    /// <summary>
    /// Получить текущий вес инвентаря
    /// </summary>
    public float GetCurrentWeight()
    {
        float weight = 0f;
        foreach (InventorySlot slot in slots)
        {
            if (!slot.IsEmpty())
            {
                weight += slot.itemData.weight * slot.quantity;
            }
        }
        return weight;
    }

    /// <summary>
    /// Получить количество занятых слотов
    /// </summary>
    public int GetUsedSlots()
    {
        int used = 0;
        foreach (InventorySlot slot in slots)
        {
            if (!slot.IsEmpty())
                used++;
        }
        return used;
    }

    /// <summary>
    /// Получить процент заполненности по весу
    /// </summary>
    public float GetWeightPercent()
    {
        if (unlimitedWeight || maxWeight <= 0)
            return 0f;

        return GetCurrentWeight() / maxWeight;
    }

    /// <summary>
    /// Получить слот по индексу
    /// </summary>
    public InventorySlot GetSlot(int index)
    {
        if (index >= 0 && index < slots.Count)
            return slots[index];
        return null;
    }

    /// <summary>
    /// Получить все слоты
    /// </summary>
    public List<InventorySlot> GetAllSlots()
    {
        return new List<InventorySlot>(slots);
    }

    /// <summary>
    /// Получить все непустые слоты
    /// </summary>
    public List<InventorySlot> GetUsedSlotsList()
    {
        return slots.Where(slot => !slot.IsEmpty()).ToList();
    }

    /// <summary>
    /// Очистить весь инвентарь
    /// </summary>
    public void ClearInventory()
    {
        foreach (InventorySlot slot in slots)
        {
            slot.Clear();
        }

        OnInventoryChanged?.Invoke();

        if (debugMode)
            Debug.Log("Inventory cleared");
    }

    /// <summary>
    /// Переместить предмет между слотами
    /// </summary>
    public bool MoveItem(int fromSlot, int toSlot)
    {
        if (fromSlot < 0 || fromSlot >= slots.Count || toSlot < 0 || toSlot >= slots.Count)
            return false;

        if (fromSlot == toSlot)
            return false;

        InventorySlot from = slots[fromSlot];
        InventorySlot to = slots[toSlot];

        if (from.IsEmpty())
            return false;

        // Если целевой слот пустой
        if (to.IsEmpty())
        {
            to.itemData = from.itemData;
            to.quantity = from.quantity;
            from.Clear();
            OnInventoryChanged?.Invoke();
            return true;
        }

        // Если в целевом слоте тот же предмет
        if (to.itemData.itemName == from.itemData.itemName)
        {
            int canMove = Mathf.Min(from.quantity, to.itemData.maxStackSize - to.quantity);
            if (canMove > 0)
            {
                to.quantity += canMove;
                from.RemoveItem(canMove);
                OnInventoryChanged?.Invoke();
                return true;
            }
        }
        else
        {
            // Меняем местами
            ItemData tempData = to.itemData;
            int tempQuantity = to.quantity;

            to.itemData = from.itemData;
            to.quantity = from.quantity;

            from.itemData = tempData;
            from.quantity = tempQuantity;

            OnInventoryChanged?.Invoke();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Выбросить предмет из инвентаря в мир
    /// </summary>
    public bool DropItem(int slotIndex, int quantity = 1)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count)
            return false;

        InventorySlot slot = slots[slotIndex];
        if (slot.IsEmpty())
            return false;

        int toDrop = Mathf.Min(quantity, slot.quantity);
        ItemData itemData = slot.itemData;

        // Создаем предмет в мире
        Vector3 dropPosition = transform.position + transform.forward * 1.5f;
        Item.CreateWorldItem(itemData, dropPosition);

        // Удаляем из инвентаря
        RemoveItemFromSlot(slotIndex, toDrop);

        if (debugMode)
            Debug.Log($"Dropped {toDrop} {itemData.itemName}");

        return true;
    }

    /// <summary>
    /// Получить информацию об инвентаре для отладки
    /// </summary>
    public string GetInventoryInfo()
    {
        string info = $"Inventory ({GetUsedSlots()}/{maxSlots} slots, {GetCurrentWeight():F1}/{maxWeight} weight):\n";

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];
            if (!slot.IsEmpty())
            {
                info += $"  [{i}] {slot.itemData.itemName} x{slot.quantity}\n";
            }
        }

        return info;
    }

    // === СИСТЕМА ЭКИПИРОВКИ ===

    /// <summary>
    /// Экипировать предмет
    /// </summary>
    public bool EquipItem(ItemData item)
    {
        if (item == null || !item.CanBeEquipped())
            return false;

        EquipmentSlot slot = item.equipmentSlot;
        if (!equipmentSlots.ContainsKey(slot))
            return false;

        // Если слот занят, снимаем предыдущий предмет
        if (!equipmentSlots[slot].IsEmpty())
        {
            UnequipItem(slot);
        }

        // Экипируем новый предмет
        equipmentSlots[slot] = new InventorySlot(item, 1);
        OnEquipmentChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Снять экипировку с слота
    /// </summary>
    public bool UnequipItem(EquipmentSlot slot)
    {
        if (!equipmentSlots.ContainsKey(slot) || equipmentSlots[slot].IsEmpty())
            return false;

        ItemData item = equipmentSlots[slot].itemData;

        // Пытаемся добавить предмет в обычный инвентарь
        if (AddItem(item, 1))
        {
            equipmentSlots[slot].Clear();
            OnEquipmentChanged?.Invoke();
            return true;
        }

        return false; // Не хватает места в инвентаре
    }

    /// <summary>
    /// Получить экипированный предмет из слота
    /// </summary>
    public ItemData GetEquippedItem(EquipmentSlot slot)
    {
        if (equipmentSlots.ContainsKey(slot) && !equipmentSlots[slot].IsEmpty())
        {
            return equipmentSlots[slot].itemData;
        }
        return null;
    }

    /// <summary>
    /// Получить все слоты экипировки
    /// </summary>
    public Dictionary<EquipmentSlot, InventorySlot> GetAllEquipmentSlots()
    {
        return equipmentSlots;
    }

    /// <summary>
    /// Проверить, экипирован ли предмет в указанном слоте
    /// </summary>
    public bool IsEquipped(EquipmentSlot slot)
    {
        return equipmentSlots.ContainsKey(slot) && !equipmentSlots[slot].IsEmpty();
    }

    void OnDrawGizmosSelected()
    {
        if (autoPickupEnabled)
        {
            // Показываем радиус автоподбора
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, autoPickupRange);
        }
    }
}