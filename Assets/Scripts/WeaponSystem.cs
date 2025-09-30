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
    public float meleePreferenceRange = 3f;    // Дистанция предпочтения ближнего боя
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
            Debug.LogError("[WeaponSystem] Character component not found!");
            enabled = false;
            return;
        }
    }

    void Start()
    {
        // Инициализируем базовое оружие
        InitializeDefaultWeapons();
    }

    /// <summary>
    /// Инициализация базового оружия для персонажа
    /// </summary>
    private void InitializeDefaultWeapons()
    {
        // Создаем базовое оружие ближнего боя
        MeleeWeapon meleeWeapon = MeleeWeapon.CreatePresetWeapon(defaultMeleeCategory, defaultWeaponRarity);
        AddWeapon(meleeWeapon);

        // Создаем базовое огнестрельное оружие
        RangedWeapon rangedWeapon = RangedWeapon.CreatePresetWeapon(defaultRangedCategory, defaultWeaponRarity);
        AddWeapon(rangedWeapon);

        // Выбираем оружие ближнего боя по умолчанию
        SetCurrentWeapon(meleeWeapon);

        if (debugMode)
        {
            Debug.Log($"[WeaponSystem] {character.GetFullName()} initialized with {meleeWeapon.weaponName} and {rangedWeapon.weaponName}");
        }
    }

    /// <summary>
    /// Добавить оружие в арсенал
    /// </summary>
    public void AddWeapon(Weapon weapon)
    {
        if (weapon == null)
        {
            Debug.LogWarning("[WeaponSystem] Attempted to add null weapon");
            return;
        }

        weapons.Add(weapon);

        if (debugMode)
        {
            Debug.Log($"[WeaponSystem] Added {weapon.weaponName} to {character.GetFullName()}'s arsenal");
        }
    }

    /// <summary>
    /// Удалить оружие из арсенала
    /// </summary>
    public void RemoveWeapon(Weapon weapon)
    {
        if (weapon == null) return;

        weapons.Remove(weapon);

        // Если это было текущее оружие, выбираем другое
        if (currentWeapon == weapon)
        {
            SelectBestWeapon(Vector3.zero, 5f); // Выбираем оружие для средней дистанции
        }

        if (debugMode)
        {
            Debug.Log($"[WeaponSystem] Removed {weapon.weaponName} from {character.GetFullName()}'s arsenal");
        }
    }

    /// <summary>
    /// Установить текущее оружие
    /// </summary>
    public void SetCurrentWeapon(Weapon weapon)
    {
        if (weapon == null || !weapons.Contains(weapon))
        {
            Debug.LogWarning("[WeaponSystem] Attempted to set invalid weapon as current");
            return;
        }

        if (currentWeapon != weapon)
        {
            currentWeapon = weapon;
            weaponSwitches++;

            if (debugMode)
            {
                Debug.Log($"[WeaponSystem] {character.GetFullName()} switched to {weapon.weaponName}");
            }
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
            Debug.LogWarning("[WeaponSystem] No weapons available!");
            return;
        }

        Weapon bestWeapon = null;
        float bestScore = -1f;

        foreach (Weapon weapon in weapons)
        {
            if (!weapon.CanAttack())
                continue;

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
            // Ближний бой предпочтителен на короткой дистанции
            if (distance <= meleePreferenceRange)
            {
                score += 100f; // Высокий приоритет
                score += (meleePreferenceRange - distance) * 10f; // Чем ближе, тем лучше
            }
            else
            {
                score += 50f; // Средний приоритет
            }
        }
        else if (weapon.weaponType == WeaponType.Ranged)
        {
            RangedWeapon rangedWeapon = weapon as RangedWeapon;

            // Проверяем наличие патронов
            if (rangedWeapon != null && rangedWeapon.HasAmmo())
            {
                // Огнестрельное оружие предпочтительно на дальней дистанции
                if (distance >= meleePreferenceRange)
                {
                    score += 100f; // Высокий приоритет
                    if (distance <= rangedPreferenceRange)
                    {
                        score += (distance - meleePreferenceRange) * 5f; // Бонус за среднюю дистанцию
                    }
                }
                else
                {
                    score += 30f; // Низкий приоритет на ближней дистанции
                }

                // Бонус за количество патронов
                score += rangedWeapon.GetAmmoPercent() * 20f;
            }
            else
            {
                // Без патронов - очень низкий приоритет
                score = 10f;
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
            Debug.LogWarning("[WeaponSystem] Cannot attack null target");
            return;
        }

        float distance = Vector3.Distance(transform.position, target.transform.position);

        // Выбираем лучшее оружие для этой дистанции
        SelectBestWeapon(target.transform.position, distance);

        if (currentWeapon == null)
        {
            Debug.LogWarning("[WeaponSystem] No weapon available for attack");
            return;
        }

        // Проверяем перезарядку огнестрельного оружия
        if (currentWeapon is RangedWeapon rangedWeapon)
        {
            if (rangedWeapon.NeedsReload() && autoReloadEnabled && !rangedWeapon.IsReloading())
            {
                StartCoroutine(rangedWeapon.ReloadWeapon());
                Debug.Log($"[WeaponSystem] Auto-reloading {rangedWeapon.weaponName}");
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

        if (debugMode)
        {
            Debug.Log($"[WeaponSystem] {character.GetFullName()} attacked {target.GetFullName()} " +
                     $"with {currentWeapon.weaponName} at distance {distance:F1}");
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

        if (debugMode)
        {
            Debug.Log($"[WeaponSystem] All ranged weapons reloaded for {character.GetFullName()}");
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