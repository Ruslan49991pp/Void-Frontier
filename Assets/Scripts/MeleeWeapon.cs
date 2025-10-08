using UnityEngine;
using System.Collections;

/// <summary>
/// Класс оружия ближнего боя
/// </summary>
[System.Serializable]
public class MeleeWeapon : Weapon
{
    [Header("Melee Weapon Settings")]
    public MeleeWeaponCategory category = MeleeWeaponCategory.Knife;
    public float attackAnimation = 0.5f;    // Время анимации атаки
    public float criticalChance = 0.1f;     // Шанс критического удара (10%)
    public float criticalMultiplier = 2f;   // Множитель критического урона

    /// <summary>
    /// Конструктор
    /// </summary>
    public MeleeWeapon()
    {
        weaponType = WeaponType.Melee;
        range = 1.5f; // Дальность ближнего боя (соседняя клетка)
    }

    /// <summary>
    /// Выполнить атаку ближнего боя
    /// </summary>
    public override void PerformAttack(Character attacker, Character target)
    {
        if (!CanAttack())
        {
            return;
        }

        if (!IsTargetInRange(attacker.transform.position, target.transform.position))
        {
            return;
        }

        // Запускаем корутину атаки
        MonoBehaviour attackerMono = attacker.GetComponent<MonoBehaviour>();
        if (attackerMono != null)
        {
            attackerMono.StartCoroutine(PerformMeleeAttackCoroutine(attacker, target));
        }
    }

    /// <summary>
    /// Корутина выполнения атаки ближнего боя
    /// </summary>
    private IEnumerator PerformMeleeAttackCoroutine(Character attacker, Character target)
    {
        // Поворачиваем атакующего к цели
        Vector3 direction = (target.transform.position - attacker.transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            attacker.transform.rotation = Quaternion.LookRotation(direction);
        }

        // Анимация атаки (небольшой выпад вперед)
        Vector3 startPosition = attacker.transform.position;
        Vector3 attackPosition = startPosition + direction * 0.3f;

        // Выпад вперед
        float elapsedTime = 0f;
        float halfAnimation = attackAnimation * 0.5f;

        while (elapsedTime < halfAnimation)
        {
            float t = elapsedTime / halfAnimation;
            attacker.transform.position = Vector3.Lerp(startPosition, attackPosition, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Рассчитываем урон
        float finalDamage = CalculateMeleeDamage();

        // Наносим урон
        target.TakeDamage(finalDamage);

        // Показываем эффект урона
        ShowMeleeHitEffect(target, finalDamage);

        // Используем оружие (снижаем прочность)
        Use();

        // Возврат в исходную позицию
        elapsedTime = 0f;
        while (elapsedTime < halfAnimation)
        {
            float t = elapsedTime / halfAnimation;
            attacker.transform.position = Vector3.Lerp(attackPosition, startPosition, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        attacker.transform.position = startPosition;
    }

    /// <summary>
    /// Рассчитать урон для ближнего боя с учетом критических ударов
    /// </summary>
    private float CalculateMeleeDamage()
    {
        float baseDamage = CalculateFinalDamage();

        // Проверяем критический удар
        bool isCritical = Random.Range(0f, 1f) < criticalChance;
        if (isCritical)
        {
            baseDamage *= criticalMultiplier;
        }

        // Добавляем вариативность урона ±10%
        float variability = Random.Range(0.9f, 1.1f);
        return baseDamage * variability;
    }

    /// <summary>
    /// Показать эффект попадания в ближнем бою
    /// </summary>
    private void ShowMeleeHitEffect(Character target, float damage)
    {
        // Создаем простой визуальный эффект
        GameObject effect = new GameObject("MeleeHitEffect");
        effect.transform.position = target.transform.position + Vector3.up * 1.8f;

        // Добавляем текстовую метку урона
        TextMesh damageText = effect.AddComponent<TextMesh>();
        damageText.text = $"-{damage:F0}";
        damageText.fontSize = 10;
        damageText.color = Color.white;
        damageText.anchor = TextAnchor.MiddleCenter;

        // Поворачиваем к камере
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Vector3 directionToCamera = mainCamera.transform.position - effect.transform.position;
            effect.transform.rotation = Quaternion.LookRotation(-directionToCamera);
        }

        // Добавляем компонент, который всегда поворачивает к камере
        effect.AddComponent<LookAtCamera>();

        // Анимация исчезновения
        MonoBehaviour targetMono = target.GetComponent<MonoBehaviour>();
        if (targetMono != null)
        {
            targetMono.StartCoroutine(AnimateDamageText(effect, 1.0f));
        }
        else
        {
            Object.Destroy(effect, 1.0f);
        }
    }

    /// <summary>
    /// Анимация текста урона
    /// </summary>
    private IEnumerator AnimateDamageText(GameObject effect, float duration)
    {
        TextMesh textMesh = effect.GetComponent<TextMesh>();
        Vector3 startPos = effect.transform.position;
        Vector3 endPos = new Vector3(startPos.x, 10f, startPos.z);

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            // Проверяем, что объект все еще существует
            if (effect == null || textMesh == null)
            {
                yield break;
            }

            float t = elapsedTime / duration;
            effect.transform.position = Vector3.Lerp(startPos, endPos, t);

            // Плавное исчезновение
            Color color = textMesh.color;
            color.a = 1f - t;
            textMesh.color = color;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Принудительно уничтожаем все дочерние объекты и компоненты
        if (effect != null)
        {
            // Отключаем все компоненты
            var allComponents = effect.GetComponents<Component>();

            foreach (var component in allComponents)
            {
                if (component != null && !(component is Transform))
                {
                    if (component is MonoBehaviour monoBehaviour)
                    {
                        monoBehaviour.enabled = false;
                    }
                    Object.Destroy(component);
                }
            }

            // Уничтожаем все дочерние объекты
            for (int i = effect.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = effect.transform.GetChild(i);
                Object.Destroy(child.gameObject);
            }

            // Немедленно уничтожаем основной объект
            Object.DestroyImmediate(effect);
        }
    }

    /// <summary>
    /// Проверить дальность для ближнего боя
    /// </summary>
    public override bool IsTargetInRange(Vector3 attackerPosition, Vector3 targetPosition)
    {
        float distance = Vector3.Distance(attackerPosition, targetPosition);
        return distance <= range;
    }

    /// <summary>
    /// Получить название категории оружия
    /// </summary>
    protected override string GetWeaponTypeName()
    {
        string baseType = base.GetWeaponTypeName();
        string categoryName = GetCategoryName();
        return $"{baseType} ({categoryName})";
    }

    /// <summary>
    /// Получить название категории
    /// </summary>
    private string GetCategoryName()
    {
        switch (category)
        {
            case MeleeWeaponCategory.Knife: return "Нож";
            case MeleeWeaponCategory.Sword: return "Меч";
            case MeleeWeaponCategory.Club: return "Дубинка";
            case MeleeWeaponCategory.Axe: return "Топор";
            default: return "Неизвестно";
        }
    }

    /// <summary>
    /// Получить подробное описание оружия ближнего боя
    /// </summary>
    public override string GetDetailedDescription()
    {
        string desc = base.GetDetailedDescription();
        desc += $"\nКритический удар: {(criticalChance * 100):F0}%";
        desc += $"\nКритический множитель: {criticalMultiplier:F1}x";
        desc += $"\nВремя атаки: {attackAnimation:F1}с";
        return desc;
    }

    /// <summary>
    /// Создать копию оружия
    /// </summary>
    public override Weapon CreateCopy()
    {
        MeleeWeapon copy = new MeleeWeapon();
        CopyBasicProperties(copy);

        // Копируем специфичные для ближнего боя свойства
        copy.category = this.category;
        copy.attackAnimation = this.attackAnimation;
        copy.criticalChance = this.criticalChance;
        copy.criticalMultiplier = this.criticalMultiplier;

        return copy;
    }

    /// <summary>
    /// Копировать базовые свойства оружия
    /// </summary>
    protected void CopyBasicProperties(Weapon copy)
    {
        copy.weaponName = this.weaponName;
        copy.description = this.description;
        copy.weaponType = this.weaponType;
        copy.rarity = this.rarity;
        copy.damage = this.damage;
        copy.range = this.range;
        copy.attackSpeed = this.attackSpeed;
        copy.accuracy = this.accuracy;
        copy.maxDurability = this.maxDurability;
        copy.currentDurability = this.currentDurability;
        copy.durabilityLossPerUse = this.durabilityLossPerUse;
        copy.icon = this.icon;
        copy.prefab = this.prefab;
    }

    /// <summary>
    /// Создать предустановленное оружие ближнего боя
    /// </summary>
    public static MeleeWeapon CreatePresetWeapon(MeleeWeaponCategory category, ItemRarity rarity = ItemRarity.Common)
    {
        MeleeWeapon weapon = new MeleeWeapon();
        weapon.category = category;
        weapon.rarity = rarity;

        // Базовые характеристики в зависимости от редкости
        float rarityMultiplier = GetRarityMultiplier(rarity);

        switch (category)
        {
            case MeleeWeaponCategory.Knife:
                weapon.weaponName = "Нож";
                weapon.description = "Быстрое и точное оружие ближнего боя";
                weapon.damage = 15f * rarityMultiplier;
                weapon.attackSpeed = 1.5f;
                weapon.accuracy = 0.9f;
                weapon.criticalChance = 0.15f;
                weapon.attackAnimation = 0.3f;
                break;

            case MeleeWeaponCategory.Sword:
                weapon.weaponName = "Меч";
                weapon.description = "Сбалансированное оружие с хорошим уроном";
                weapon.damage = 25f * rarityMultiplier;
                weapon.attackSpeed = 1f;
                weapon.accuracy = 0.85f;
                weapon.criticalChance = 0.1f;
                weapon.attackAnimation = 0.5f;
                break;

            case MeleeWeaponCategory.Club:
                weapon.weaponName = "Дубинка";
                weapon.description = "Медленное, но мощное оружие";
                weapon.damage = 35f * rarityMultiplier;
                weapon.attackSpeed = 0.7f;
                weapon.accuracy = 0.8f;
                weapon.criticalChance = 0.05f;
                weapon.attackAnimation = 0.7f;
                break;

            case MeleeWeaponCategory.Axe:
                weapon.weaponName = "Топор";
                weapon.description = "Тяжелое оружие с высоким критическим уроном";
                weapon.damage = 30f * rarityMultiplier;
                weapon.attackSpeed = 0.8f;
                weapon.accuracy = 0.75f;
                weapon.criticalChance = 0.2f;
                weapon.criticalMultiplier = 2.5f;
                weapon.attackAnimation = 0.6f;
                break;
        }

        // Модификация названия в зависимости от редкости
        if (rarity != ItemRarity.Common)
        {
            weapon.weaponName = GetRarityPrefix(rarity) + " " + weapon.weaponName;
        }

        return weapon;
    }

    /// <summary>
    /// Получить множитель редкости
    /// </summary>
    private static float GetRarityMultiplier(ItemRarity rarity)
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

    /// <summary>
    /// Получить префикс редкости
    /// </summary>
    private static string GetRarityPrefix(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Uncommon: return "Качественный";
            case ItemRarity.Rare: return "Редкий";
            case ItemRarity.Epic: return "Эпический";
            case ItemRarity.Legendary: return "Легендарный";
            default: return "";
        }
    }
}