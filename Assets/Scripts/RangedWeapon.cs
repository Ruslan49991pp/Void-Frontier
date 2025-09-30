using UnityEngine;
using System.Collections;

/// <summary>
/// Класс огнестрельного оружия (дальний бой)
/// </summary>
[System.Serializable]
public class RangedWeapon : Weapon
{
    [Header("Ranged Weapon Settings")]
    public RangedWeaponCategory category = RangedWeaponCategory.Pistol;
    public float bulletSpeed = 50f;         // Скорость пули
    public float reloadTime = 2f;           // Время перезарядки
    public int magazineSize = 10;           // Размер магазина
    public int currentAmmo = 10;            // Текущие патроны в магазине
    public float recoil = 0.1f;             // Отдача (влияет на точность)
    public bool isAutomatic = false;        // Автоматическое оружие

    [Header("Bullet Properties")]
    public GameObject bulletPrefab;         // Префаб пули
    public float muzzleFlashDuration = 0.1f; // Длительность вспышки выстрела

    // Внутренние переменные
    private bool isReloading = false;
    private float lastShotTime = 0f;

    /// <summary>
    /// Конструктор
    /// </summary>
    public RangedWeapon()
    {
        weaponType = WeaponType.Ranged;
        range = 10f; // Дальность огнестрельного оружия
        currentAmmo = magazineSize;
    }

    /// <summary>
    /// Может ли оружие стрелять
    /// </summary>
    public override bool CanAttack()
    {
        return base.CanAttack() && currentAmmo > 0 && !isReloading;
    }

    /// <summary>
    /// Есть ли патроны в магазине
    /// </summary>
    public bool HasAmmo()
    {
        return currentAmmo > 0;
    }

    /// <summary>
    /// Нужна ли перезарядка
    /// </summary>
    public bool NeedsReload()
    {
        return currentAmmo == 0;
    }

    /// <summary>
    /// Перезаряжается ли оружие сейчас
    /// </summary>
    public bool IsReloading()
    {
        return isReloading;
    }

    /// <summary>
    /// Выполнить атаку огнестрельным оружием
    /// </summary>
    public override void PerformAttack(Character attacker, Character target)
    {
        if (!CanAttack())
        {
            if (NeedsReload())
            {
                Debug.Log($"[RangedWeapon] {weaponName} needs reload!");
                // Автоматически начинаем перезарядку
                MonoBehaviour reloadMono = attacker.GetComponent<MonoBehaviour>();
                if (reloadMono != null && !isReloading)
                {
                    reloadMono.StartCoroutine(ReloadWeapon());
                }
            }
            else if (IsBroken())
            {
                Debug.LogWarning($"[RangedWeapon] {weaponName} is broken and cannot fire!");
            }
            return;
        }

        // Проверяем время последнего выстрела для соблюдения скорости стрельбы
        float timeSinceLastShot = Time.time - lastShotTime;
        if (timeSinceLastShot < GetAttackCooldown())
        {
            Debug.Log($"[RangedWeapon] {weaponName} cooling down, {GetAttackCooldown() - timeSinceLastShot:F2}s remaining");
            return;
        }

        // Выполняем выстрел
        MonoBehaviour attackerMono = attacker.GetComponent<MonoBehaviour>();
        if (attackerMono != null)
        {
            attackerMono.StartCoroutine(PerformShotCoroutine(attacker, target));
        }
    }

    /// <summary>
    /// Корутина выполнения выстрела
    /// </summary>
    private IEnumerator PerformShotCoroutine(Character attacker, Character target)
    {
        Debug.Log($"[RangedWeapon] {attacker.GetFullName()} shoots {target.GetFullName()} with {weaponName}");

        // Поворачиваем стрелка к цели
        Vector3 direction = (target.transform.position - attacker.transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            attacker.transform.rotation = Quaternion.LookRotation(direction);
        }

        // Определяем позицию выстрела (немного впереди стрелка)
        Vector3 muzzlePosition = attacker.transform.position +
                                attacker.transform.forward * 0.5f +
                                Vector3.up * 1f; // Высота оружия

        // Показываем вспышку выстрела
        MonoBehaviour attackerMono = attacker.GetComponent<MonoBehaviour>();
        if (attackerMono != null)
        {
            attackerMono.StartCoroutine(ShowMuzzleFlash(attacker, muzzlePosition));
        }

        // Создаем пулю
        FireBullet(muzzlePosition, target.transform.position, attacker);

        // Обновляем состояние оружия
        currentAmmo--;
        lastShotTime = Time.time;
        Use(); // Снижаем прочность

        Debug.Log($"[RangedWeapon] Shot fired. Ammo remaining: {currentAmmo}/{magazineSize}");

        yield return null;
    }

    /// <summary>
    /// Выстрелить пулей
    /// </summary>
    private void FireBullet(Vector3 muzzlePosition, Vector3 targetPosition, Character shooter)
    {
        GameObject bulletObject;

        // Если есть префаб пули, используем его
        if (bulletPrefab != null)
        {
            bulletObject = Object.Instantiate(bulletPrefab, muzzlePosition, Quaternion.identity);
        }
        else
        {
            // Создаем базовую пулю
            bulletObject = new GameObject("Bullet");
            bulletObject.transform.position = muzzlePosition;
        }

        // Добавляем компонент Bullet если его нет
        Bullet bulletComponent = bulletObject.GetComponent<Bullet>();
        if (bulletComponent == null)
        {
            bulletComponent = bulletObject.AddComponent<Bullet>();
        }

        // Рассчитываем финальную точность с учетом отдачи
        float finalAccuracy = accuracy * (1f - recoil);

        // Инициализируем пулю
        bulletComponent.Initialize(
            muzzlePosition,
            targetPosition,
            shooter,
            CalculateFinalDamage(), // Урон с учетом состояния оружия
            bulletSpeed,
            finalAccuracy
        );
    }

    /// <summary>
    /// Показать вспышку выстрела
    /// </summary>
    private IEnumerator ShowMuzzleFlash(Character shooter, Vector3 muzzlePosition)
    {
        // Создаем простую вспышку
        GameObject flash = new GameObject("MuzzleFlash");
        flash.transform.position = muzzlePosition;
        flash.transform.SetParent(shooter.transform);

        // Создаем визуал вспышки
        GameObject flashSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flashSphere.transform.SetParent(flash.transform);
        flashSphere.transform.localPosition = Vector3.zero;
        flashSphere.transform.localScale = Vector3.one * 0.5f;

        // Убираем коллайдер
        Collider flashCollider = flashSphere.GetComponent<Collider>();
        if (flashCollider != null)
        {
            Object.DestroyImmediate(flashCollider);
        }

        // Настраиваем материал вспышки
        Renderer flashRenderer = flashSphere.GetComponent<Renderer>();
        if (flashRenderer != null)
        {
            Material flashMaterial = new Material(Shader.Find("Standard"));
            flashMaterial.color = Color.yellow;
            flashMaterial.SetFloat("_Mode", 0);
            flashMaterial.EnableKeyword("_EMISSION");
            flashMaterial.SetColor("_EmissionColor", Color.yellow * 2f);
            flashRenderer.material = flashMaterial;
        }

        // Ждем указанное время
        yield return new WaitForSeconds(muzzleFlashDuration);

        // Удаляем вспышку
        Object.Destroy(flash);
    }

    /// <summary>
    /// Перезарядить оружие
    /// </summary>
    public IEnumerator ReloadWeapon()
    {
        if (isReloading)
        {
            yield break;
        }

        isReloading = true;
        Debug.Log($"[RangedWeapon] Reloading {weaponName}... ({reloadTime:F1}s)");

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = magazineSize;
        isReloading = false;

        Debug.Log($"[RangedWeapon] {weaponName} reloaded! Ammo: {currentAmmo}/{magazineSize}");
    }

    /// <summary>
    /// Принудительно перезарядить оружие (без корутины)
    /// </summary>
    public void ForceReload()
    {
        currentAmmo = magazineSize;
        isReloading = false;
        Debug.Log($"[RangedWeapon] {weaponName} force reloaded! Ammo: {currentAmmo}/{magazineSize}");
    }

    /// <summary>
    /// Получить процент патронов в магазине
    /// </summary>
    public float GetAmmoPercent()
    {
        if (magazineSize <= 0) return 0f;
        return (float)currentAmmo / magazineSize;
    }

    /// <summary>
    /// Проверить дальность для огнестрельного оружия
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
            case RangedWeaponCategory.Pistol: return "Пистолет";
            case RangedWeaponCategory.Rifle: return "Винтовка";
            case RangedWeaponCategory.Shotgun: return "Дробовик";
            case RangedWeaponCategory.SMG: return "ПП";
            default: return "Неизвестно";
        }
    }

    /// <summary>
    /// Получить подробное описание огнестрельного оружия
    /// </summary>
    public override string GetDetailedDescription()
    {
        string desc = base.GetDetailedDescription();
        desc += $"\nПатроны: {currentAmmo}/{magazineSize}";
        desc += $"\nСкорость пули: {bulletSpeed:F0}";
        desc += $"\nВремя перезарядки: {reloadTime:F1}с";
        desc += $"\nОтдача: {(recoil * 100):F0}%";
        desc += $"\nТип стрельбы: {(isAutomatic ? "Автоматический" : "Одиночный")}";

        if (isReloading)
        {
            desc += "\n(ПЕРЕЗАРЯДКА...)";
        }
        else if (currentAmmo == 0)
        {
            desc += "\n(НУЖНА ПЕРЕЗАРЯДКА)";
        }

        return desc;
    }

    /// <summary>
    /// Создать копию оружия
    /// </summary>
    public override Weapon CreateCopy()
    {
        RangedWeapon copy = new RangedWeapon();
        CopyBasicProperties(copy);

        // Копируем специфичные для огнестрельного оружия свойства
        copy.category = this.category;
        copy.bulletSpeed = this.bulletSpeed;
        copy.reloadTime = this.reloadTime;
        copy.magazineSize = this.magazineSize;
        copy.currentAmmo = this.currentAmmo;
        copy.recoil = this.recoil;
        copy.isAutomatic = this.isAutomatic;
        copy.bulletPrefab = this.bulletPrefab;
        copy.muzzleFlashDuration = this.muzzleFlashDuration;

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
    /// Создать предустановленное огнестрельное оружие
    /// </summary>
    public static RangedWeapon CreatePresetWeapon(RangedWeaponCategory category, ItemRarity rarity = ItemRarity.Common)
    {
        RangedWeapon weapon = new RangedWeapon();
        weapon.category = category;
        weapon.rarity = rarity;

        // Базовые характеристики в зависимости от редкости
        float rarityMultiplier = GetRarityMultiplier(rarity);

        switch (category)
        {
            case RangedWeaponCategory.Pistol:
                weapon.weaponName = "Пистолет";
                weapon.description = "Легкое и быстрое огнестрельное оружие";
                weapon.damage = 20f * rarityMultiplier;
                weapon.range = 8f;
                weapon.attackSpeed = 2f; // 2 выстрела в секунду
                weapon.accuracy = 0.8f;
                weapon.magazineSize = 12;
                weapon.reloadTime = 1.5f;
                weapon.bulletSpeed = 40f;
                weapon.recoil = 0.1f;
                break;

            case RangedWeaponCategory.Rifle:
                weapon.weaponName = "Винтовка";
                weapon.description = "Точное оружие дальнего боя";
                weapon.damage = 40f * rarityMultiplier;
                weapon.range = 15f;
                weapon.attackSpeed = 0.8f; // Медленная стрельба
                weapon.accuracy = 0.95f; // Высокая точность
                weapon.magazineSize = 8;
                weapon.reloadTime = 2.5f;
                weapon.bulletSpeed = 80f;
                weapon.recoil = 0.2f;
                break;

            case RangedWeaponCategory.Shotgun:
                weapon.weaponName = "Дробовик";
                weapon.description = "Мощное оружие ближнего боя";
                weapon.damage = 60f * rarityMultiplier;
                weapon.range = 5f; // Короткая дальность
                weapon.attackSpeed = 0.5f; // Очень медленная стрельба
                weapon.accuracy = 0.7f; // Низкая точность на дальней дистанции
                weapon.magazineSize = 6;
                weapon.reloadTime = 3f;
                weapon.bulletSpeed = 30f;
                weapon.recoil = 0.3f; // Высокая отдача
                break;

            case RangedWeaponCategory.SMG:
                weapon.weaponName = "Пистолет-пулемет";
                weapon.description = "Автоматическое оружие с высокой скорострельностью";
                weapon.damage = 15f * rarityMultiplier;
                weapon.range = 6f;
                weapon.attackSpeed = 4f; // Очень быстрая стрельба
                weapon.accuracy = 0.75f;
                weapon.magazineSize = 25;
                weapon.reloadTime = 2f;
                weapon.bulletSpeed = 35f;
                weapon.recoil = 0.15f;
                weapon.isAutomatic = true;
                break;
        }

        // Инициализируем патроны
        weapon.currentAmmo = weapon.magazineSize;

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