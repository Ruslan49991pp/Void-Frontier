using UnityEngine;

/// <summary>
/// Типы оружия
/// </summary>
public enum WeaponType
{
    Melee,   // Ближний бой
    Ranged   // Дальний бой (огнестрельное)
}

/// <summary>
/// Категории оружия ближнего боя
/// </summary>
public enum MeleeWeaponCategory
{
    Knife,      // Нож
    Sword,      // Меч
    Club,       // Дубинка
    Axe         // Топор
}

/// <summary>
/// Категории огнестрельного оружия
/// </summary>
public enum RangedWeaponCategory
{
    Pistol,     // Пистолет
    Rifle,      // Винтовка
    Shotgun,    // Дробовик
    SMG         // Пистолет-пулемет
}

/// <summary>
/// Базовый класс для всех видов оружия
/// </summary>
[System.Serializable]
public abstract class Weapon
{
    [Header("Basic Weapon Info")]
    public string weaponName = "Unknown Weapon";
    public string description = "";
    public WeaponType weaponType;
    public ItemRarity rarity = ItemRarity.Common;

    [Header("Combat Stats")]
    public float damage = 10f;           // Базовый урон
    public float range = 1f;             // Дальность
    public float attackSpeed = 1f;       // Скорость атаки (атак в секунду)
    public float accuracy = 1f;          // Точность (0.0 - 1.0)

    [Header("Durability")]
    public float maxDurability = 100f;   // Максимальная прочность
    public float currentDurability = 100f; // Текущая прочность
    public float durabilityLossPerUse = 1f; // Потеря прочности за использование

    [Header("Visual")]
    public Sprite icon;                  // Иконка оружия
    public GameObject prefab;            // Префаб для отображения

    /// <summary>
    /// Конструктор по умолчанию
    /// </summary>
    public Weapon()
    {
        currentDurability = maxDurability;
    }

    /// <summary>
    /// Может ли оружие атаковать
    /// </summary>
    public virtual bool CanAttack()
    {
        return currentDurability > 0;
    }

    /// <summary>
    /// Проверить, находится ли цель в дальности
    /// </summary>
    public virtual bool IsTargetInRange(Vector3 attackerPosition, Vector3 targetPosition)
    {
        float distance = Vector3.Distance(attackerPosition, targetPosition);
        return distance <= range;
    }

    /// <summary>
    /// Выполнить атаку (абстрактный метод)
    /// </summary>
    public abstract void PerformAttack(Character attacker, Character target);

    /// <summary>
    /// Получить время между атаками
    /// </summary>
    public virtual float GetAttackCooldown()
    {
        return 1f / attackSpeed;
    }

    /// <summary>
    /// Использовать оружие (снижает прочность)
    /// </summary>
    public virtual void Use()
    {
        currentDurability = Mathf.Max(0, currentDurability - durabilityLossPerUse);
    }

    /// <summary>
    /// Починить оружие
    /// </summary>
    public virtual void Repair(float amount)
    {
        currentDurability = Mathf.Min(maxDurability, currentDurability + amount);
    }

    /// <summary>
    /// Получить процент прочности
    /// </summary>
    public virtual float GetDurabilityPercent()
    {
        if (maxDurability <= 0) return 0f;
        return currentDurability / maxDurability;
    }

    /// <summary>
    /// Сломано ли оружие
    /// </summary>
    public virtual bool IsBroken()
    {
        return currentDurability <= 0;
    }

    /// <summary>
    /// Рассчитать финальный урон с учетом прочности и точности
    /// </summary>
    public virtual float CalculateFinalDamage()
    {
        float baseDamage = damage;

        // Учитываем прочность (сломанное оружие наносит 10% урона)
        float durabilityModifier = IsBroken() ? 0.1f : GetDurabilityPercent();

        // Применяем случайность в зависимости от точности
        float accuracyModifier = Random.Range(accuracy * 0.8f, accuracy * 1.2f);

        return baseDamage * durabilityModifier * accuracyModifier;
    }

    /// <summary>
    /// Получить цвет редкости
    /// </summary>
    public virtual Color GetRarityColor()
    {
        switch (rarity)
        {
            case ItemRarity.Common: return Color.white;
            case ItemRarity.Uncommon: return Color.green;
            case ItemRarity.Rare: return Color.blue;
            case ItemRarity.Epic: return Color.magenta;
            case ItemRarity.Legendary: return Color.yellow;
            default: return Color.white;
        }
    }

    /// <summary>
    /// Получить подробное описание оружия
    /// </summary>
    public virtual string GetDetailedDescription()
    {
        string desc = description + "\n\n";
        desc += $"Тип: {GetWeaponTypeName()}\n";
        desc += $"Урон: {damage:F1}\n";
        desc += $"Дальность: {range:F1}\n";
        desc += $"Скорость атаки: {attackSpeed:F1}/сек\n";
        desc += $"Точность: {(accuracy * 100):F0}%\n";
        desc += $"Прочность: {currentDurability:F0}/{maxDurability:F0}";

        if (IsBroken())
        {
            desc += " (СЛОМАНО)";
        }

        return desc;
    }

    /// <summary>
    /// Получить название типа оружия
    /// </summary>
    protected virtual string GetWeaponTypeName()
    {
        switch (weaponType)
        {
            case WeaponType.Melee: return "Ближний бой";
            case WeaponType.Ranged: return "Дальний бой";
            default: return "Неизвестно";
        }
    }

    /// <summary>
    /// Создать копию оружия
    /// </summary>
    public abstract Weapon CreateCopy();
}