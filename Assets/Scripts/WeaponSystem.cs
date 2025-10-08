using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Система управления оружием персонажа
/// Автоматически выбирает оружие в зависимости от дистанции до цели
/// </summary>
public class WeaponSystem : MonoBehaviour
{
    [Header("Weapon System Settings")]
    public float meleePreferenceRange = 1.5f;  // Дистанция предпочтения ближнего боя (соседняя клетка)
    public float rangedPreferenceRange = 8f;   // Дистанция предпочтения огнестрельного оружия
    public bool autoReloadEnabled = true;      // Автоматическая перезарядка
    public bool debugMode = false;             // Отладочный режим

    [Header("Default Weapons")]
    public MeleeWeaponCategory defaultMeleeCategory = MeleeWeaponCategory.Knife;
    public RangedWeaponCategory defaultRangedCategory = RangedWeaponCategory.Pistol;
    public ItemRarity defaultWeaponRarity = ItemRarity.Common;

    // Оружие персонажа
    private List<Weapon> weapons = new List<Weapon>();
    private Weapon currentWeapon;
    private Character character;
    private Inventory inventory;

    // Кеш оружия для сохранения состояния (патроны, прочность)
    private Dictionary<string, RangedWeapon> rangedWeaponCache = new Dictionary<string, RangedWeapon>();

    // Статистика использования
    private int meleeAttacks = 0;
    private int rangedAttacks = 0;
    private int weaponSwitches = 0;

    void Awake()
    {
        character = GetComponent<Character>();
        inventory = GetComponent<Inventory>();

        if (character == null)
        {
            enabled = false;
            return;
        }
    }

    void Start()
    {
        // Инициализируем базовое оружие
        InitializeDefaultWeapons();

        // Подписываемся на изменения инвентаря для автоматической экипировки
        if (inventory != null)
        {
            inventory.OnInventoryChanged += CheckAndEquipWeapons;
            inventory.OnEquipmentChanged += SyncEquipmentWithWeapons;
        }
    }

    /// <summary>
    /// Инициализация базового оружия для персонажа
    /// </summary>
    private void InitializeDefaultWeapons()
    {
        // Создаем ТОЛЬКО базовое оружие ближнего боя (нож - виртуальный, всегда доступен)
        MeleeWeapon meleeWeapon = MeleeWeapon.CreatePresetWeapon(defaultMeleeCategory, defaultWeaponRarity);
        AddWeapon(meleeWeapon);

        // НЕ создаем базовое огнестрельное оружие - оно должно быть найдено/экипировано
        // RangedWeapon rangedWeapon = RangedWeapon.CreatePresetWeapon(defaultRangedCategory, defaultWeaponRarity);
        // AddWeapon(rangedWeapon);

        // Выбираем оружие ближнего боя по умолчанию
        SetCurrentWeapon(meleeWeapon);
    }

    /// <summary>
    /// Проверить и экипировать оружие из инвентаря при изменении инвентаря
    /// </summary>
    private void CheckAndEquipWeapons()
    {
        if (inventory == null || character == null)
        {
            return;
        }

        try
        {
            List<InventorySlot> usedSlots = inventory.GetUsedSlotsList();
            if (usedSlots == null)
            {
                return;
            }

            foreach (InventorySlot slot in usedSlots)
            {
                if (slot == null)
                {
                    continue;
                }

                if (slot.itemData != null && slot.itemData.itemType == ItemType.Weapon)
                {
                    // Проверяем, экипировано ли уже оружие
                    if (!inventory.HasWeaponEquipped())
                    {
                        // Сохраняем ссылку на ItemData перед экипировкой
                        ItemData weaponData = slot.itemData;

                        // Автоматически экипируем первое найденное оружие
                        if (inventory.EquipItem(weaponData))
                        {
                            break;
                        }
                    }
                }
            }

            // Синхронизируем оружие с экипировкой
            SyncEquipmentWithWeapons();
        }
        catch (System.Exception e)
        {
            // Критичная ошибка - оставляем
            Debug.LogError($"[WeaponSystem] Exception in CheckAndEquipWeapons: {e.Message}");
        }
    }

    /// <summary>
    /// Синхронизировать оружие WeaponSystem с экипировкой из Inventory
    /// </summary>
    private void SyncEquipmentWithWeapons()
    {
        if (inventory == null || character == null)
        {
            return;
        }

        try
        {
            // Очищаем список огнестрельного оружия (оставляем только нож)
            weapons.RemoveAll(w => w != null && w.weaponType == WeaponType.Ranged);

            // Проверяем экипированное оружие в руках
            ItemData leftHandWeapon = inventory.GetEquippedItem(EquipmentSlot.LeftHand);
            ItemData rightHandWeapon = inventory.GetEquippedItem(EquipmentSlot.RightHand);

            // Добавляем экипированное оружие в WeaponSystem
            if (leftHandWeapon != null && leftHandWeapon.itemType == ItemType.Weapon)
            {
                RangedWeapon rangedWeapon = CreateRangedWeaponFromItemData(leftHandWeapon);
                if (rangedWeapon != null)
                {
                    AddWeapon(rangedWeapon);
                }
            }

            if (rightHandWeapon != null && rightHandWeapon.itemType == ItemType.Weapon)
            {
                RangedWeapon rangedWeapon = CreateRangedWeaponFromItemData(rightHandWeapon);
                if (rangedWeapon != null)
                {
                    AddWeapon(rangedWeapon);
                }
            }
        }
        catch (System.Exception e)
        {
            // Критичная ошибка - оставляем
            Debug.LogError($"[WeaponSystem] Exception in SyncEquipmentWithWeapons: {e.Message}");
        }
    }

    /// <summary>
    /// Создать RangedWeapon из ItemData с сохранением состояния
    /// </summary>
    private RangedWeapon CreateRangedWeaponFromItemData(ItemData itemData)
    {
        if (itemData == null || itemData.itemType != ItemType.Weapon)
            return null;

        // Проверяем, есть ли уже оружие в кеше
        string weaponKey = itemData.itemName;
        if (rangedWeaponCache.ContainsKey(weaponKey))
        {
            return rangedWeaponCache[weaponKey];
        }

        // Создаем новое огнестрельное оружие на основе характеристик предмета
        RangedWeapon weapon = RangedWeapon.CreatePresetWeapon(defaultRangedCategory, itemData.rarity);
        weapon.weaponName = itemData.itemName;
        weapon.description = itemData.description;
        weapon.damage = itemData.damage;
        weapon.icon = itemData.icon;
        weapon.prefab = itemData.prefab;

        // ВАЖНО: Убеждаемся что оружие полностью заряжено при первом создании
        weapon.ForceReload();

        // Сохраняем в кеш
        rangedWeaponCache[weaponKey] = weapon;

        return weapon;
    }

    /// <summary>
    /// Добавить оружие в арсенал
    /// </summary>
    public void AddWeapon(Weapon weapon)
    {
        if (weapon == null)
        {
            return;
        }

        weapons.Add(weapon);
    }

    /// <summary>
    /// Удалить оружие из арсенала
    /// </summary>
    public void RemoveWeapon(Weapon weapon)
    {
        if (weapon == null) return;

        weapons.Remove(weapon);

        // Удаляем из кеша если это огнестрельное оружие
        if (weapon is RangedWeapon)
        {
            rangedWeaponCache.Remove(weapon.weaponName);
        }

        // Если это было текущее оружие, выбираем другое
        if (currentWeapon == weapon)
        {
            SelectBestWeapon(Vector3.zero, 5f); // Выбираем оружие для средней дистанции
        }
    }

    /// <summary>
    /// Установить текущее оружие
    /// </summary>
    public void SetCurrentWeapon(Weapon weapon)
    {
        if (weapon == null || !weapons.Contains(weapon))
        {
            return;
        }

        if (currentWeapon != weapon)
        {
            currentWeapon = weapon;
            weaponSwitches++;
        }
    }

    /// <summary>
    /// Получить текущее оружие
    /// </summary>
    public Weapon GetCurrentWeapon()
    {
        return currentWeapon;
    }

    /// <summary>
    /// Получить все оружие персонажа
    /// </summary>
    public List<Weapon> GetAllWeapons()
    {
        return new List<Weapon>(weapons);
    }

    /// <summary>
    /// Выбрать лучшее оружие для атаки цели
    /// </summary>
    public void SelectBestWeapon(Vector3 targetPosition, float distanceToTarget)
    {
        if (weapons.Count == 0)
        {
            return;
        }

        Weapon bestWeapon = null;
        float bestScore = -1f;

        foreach (Weapon weapon in weapons)
        {
            // Для оружия без патронов разрешаем выбор (для перезарядки)
            bool canConsider = weapon.CanAttack();

            // Особый случай: дальнобойное оружие без патронов может быть перезаряжено
            if (!canConsider && weapon is RangedWeapon rangedWeapon)
            {
                if (rangedWeapon.NeedsReload() && !rangedWeapon.IsReloading())
                {
                    canConsider = true; // Разрешаем выбор для перезарядки
                }
            }

            if (!canConsider)
            {
                continue;
            }

            float score = CalculateWeaponScore(weapon, distanceToTarget);

            if (score > bestScore)
            {
                bestScore = score;
                bestWeapon = weapon;
            }
        }

        // Если не нашли подходящее оружие (например, все сломано), берем любое
        if (bestWeapon == null)
        {
            bestWeapon = weapons[0];
        }

        SetCurrentWeapon(bestWeapon);
    }

    /// <summary>
    /// Рассчитать "оценку" оружия для данной дистанции
    /// </summary>
    private float CalculateWeaponScore(Weapon weapon, float distance)
    {
        float score = 0f;

        // Базовая оценка - можем ли использовать оружие
        if (!weapon.CanAttack())
            return -1f;

        // Проверяем дальность
        if (distance > weapon.range)
            return -1f; // Цель вне дальности

        // Оценка по типу оружия и дистанции
        if (weapon.weaponType == WeaponType.Melee)
        {
            // Ближний бой предпочтителен ТОЛЬКО на короткой дистанции
            if (distance <= meleePreferenceRange)
            {
                score += 100f; // Высокий приоритет
                score += (meleePreferenceRange - distance) * 10f; // Чем ближе, тем лучше
            }
            else
            {
                // За пределами дистанции ближнего боя - ОЧЕНЬ низкий приоритет
                // Чем дальше, тем хуже
                score += 5f - (distance - meleePreferenceRange) * 2f;

                // Если дальше 3 единиц - вообще не рассматриваем
                if (distance > 3f)
                {
                    return -1f;
                }
            }
        }
        else if (weapon.weaponType == WeaponType.Ranged)
        {
            RangedWeapon rangedWeapon = weapon as RangedWeapon;

            if (rangedWeapon != null)
            {
                // БЕСКОНЕЧНЫЕ ПАТРОНЫ - оружие всегда готово к стрельбе
                if (!rangedWeapon.IsReloading())
                {
                    // Огнестрельное оружие ВСЕГДА предпочтительнее
                    score += 150f; // ВЫСОКИЙ базовый приоритет

                    // Огнестрельное оружие предпочтительно на дальней дистанции
                    if (distance >= meleePreferenceRange)
                    {
                        score += 50f; // Дополнительный бонус на дальней дистанции
                        if (distance <= rangedPreferenceRange)
                        {
                            score += (distance - meleePreferenceRange) * 5f; // Бонус за среднюю дистанцию
                        }
                    }
                    else
                    {
                        // Даже на ближней дистанции оружие дальнего боя предпочтительнее
                        score += 20f;
                    }
                }
                else
                {
                    // Перезаряжается - низкий приоритет
                    score = 10f;
                }
            }
        }

        // Бонус за урон и состояние оружия
        score += weapon.damage * 0.5f;
        score += weapon.GetDurabilityPercent() * 20f;

        // Штраф за редкость (чтобы сохранить редкое оружие)
        switch (weapon.rarity)
        {
            case ItemRarity.Legendary: score -= 30f; break;
            case ItemRarity.Epic: score -= 20f; break;
            case ItemRarity.Rare: score -= 10f; break;
        }

        return score;
    }

    /// <summary>
    /// Атаковать цель с автоматическим выбором оружия
    /// </summary>
    public void AttackTarget(Character target)
    {
        if (target == null)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, target.transform.position);

        // Выбираем лучшее оружие для этой дистанции
        SelectBestWeapon(target.transform.position, distance);

        if (currentWeapon == null)
        {
            return;
        }

        // Проверяем перезарядку огнестрельного оружия
        if (currentWeapon is RangedWeapon rangedWeapon)
        {
            if (rangedWeapon.NeedsReload() && autoReloadEnabled && !rangedWeapon.IsReloading())
            {
                StartCoroutine(rangedWeapon.ReloadWeapon());
                return; // Не атакуем во время перезарядки
            }
            else if (rangedWeapon.IsReloading())
            {
                return; // Не атакуем во время перезарядки
            }
        }

        // Выполняем атаку
        currentWeapon.PerformAttack(character, target);

        // Обновляем статистику
        if (currentWeapon.weaponType == WeaponType.Melee)
        {
            meleeAttacks++;
        }
        else if (currentWeapon.weaponType == WeaponType.Ranged)
        {
            rangedAttacks++;
        }
    }

    /// <summary>
    /// Получить оружие определенного типа
    /// </summary>
    public Weapon GetWeaponByType(WeaponType type)
    {
        foreach (Weapon weapon in weapons)
        {
            if (weapon.weaponType == type && weapon.CanAttack())
            {
                return weapon;
            }
        }
        return null;
    }

    /// <summary>
    /// Получить лучшее оружие ближнего боя
    /// </summary>
    public MeleeWeapon GetBestMeleeWeapon()
    {
        MeleeWeapon best = null;
        float bestScore = -1f;

        foreach (Weapon weapon in weapons)
        {
            if (weapon is MeleeWeapon melee && melee.CanAttack())
            {
                float score = melee.damage * melee.GetDurabilityPercent();
                if (score > bestScore)
                {
                    bestScore = score;
                    best = melee;
                }
            }
        }

        return best;
    }

    /// <summary>
    /// Получить лучшее огнестрельное оружие
    /// </summary>
    public RangedWeapon GetBestRangedWeapon()
    {
        RangedWeapon best = null;
        float bestScore = -1f;

        foreach (Weapon weapon in weapons)
        {
            if (weapon is RangedWeapon ranged && ranged.CanAttack() && ranged.HasAmmo())
            {
                float score = ranged.damage * ranged.GetDurabilityPercent() * ranged.GetAmmoPercent();
                if (score > bestScore)
                {
                    bestScore = score;
                    best = ranged;
                }
            }
        }

        return best;
    }

    /// <summary>
    /// Принудительно перезарядить все огнестрельное оружие
    /// </summary>
    public void ReloadAllRangedWeapons()
    {
        foreach (Weapon weapon in weapons)
        {
            if (weapon is RangedWeapon ranged)
            {
                ranged.ForceReload();
            }
        }
    }

    /// <summary>
    /// Получить статистику использования оружия
    /// </summary>
    public string GetWeaponStats()
    {
        return $"Melee attacks: {meleeAttacks}, Ranged attacks: {rangedAttacks}, Weapon switches: {weaponSwitches}";
    }

    /// <summary>
    /// Получить информацию о текущем оружии
    /// </summary>
    public string GetCurrentWeaponInfo()
    {
        if (currentWeapon == null)
            return "No weapon equipped";

        return currentWeapon.GetDetailedDescription();
    }

    /// <summary>
    /// Проверить, может ли персонаж атаковать с текущим оружием
    /// </summary>
    public bool CanAttackWithCurrentWeapon()
    {
        return currentWeapon != null && currentWeapon.CanAttack();
    }

    /// <summary>
    /// Получить дальность текущего оружия
    /// </summary>
    public float GetCurrentWeaponRange()
    {
        return currentWeapon?.range ?? 0f;
    }

    void OnDestroy()
    {
        // Отписываемся от событий инвентаря
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= CheckAndEquipWeapons;
            inventory.OnEquipmentChanged -= SyncEquipmentWithWeapons;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (currentWeapon != null)
        {
            // Показываем дальность текущего оружия
            Gizmos.color = currentWeapon.weaponType == WeaponType.Melee ? Color.red : Color.blue;
            Gizmos.DrawWireSphere(transform.position, currentWeapon.range);

            // Показываем зоны предпочтения
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, meleePreferenceRange);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, rangedPreferenceRange);
        }
    }
}