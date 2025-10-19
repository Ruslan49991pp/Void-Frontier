using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// РЎРёСЃС‚РµРјР° СѓРїСЂР°РІР»РµРЅРёСЏ РѕСЂСѓР¶РёРµРј РїРµСЂСЃРѕРЅР°Р¶Р°
/// РђРІС‚РѕРјР°С‚РёС‡РµСЃРєРё РІС‹Р±РёСЂР°РµС‚ РѕСЂСѓР¶РёРµ РІ Р·Р°РІРёСЃРёРјРѕСЃС‚Рё РѕС‚ РґРёСЃС‚Р°РЅС†РёРё РґРѕ С†РµР»Рё
/// </summary>
public class WeaponSystem : MonoBehaviour
{
    [Header("Weapon System Settings")]
    public float meleePreferenceRange = 1.5f;  // Р”РёСЃС‚Р°РЅС†РёСЏ РїСЂРµРґРїРѕС‡С‚РµРЅРёСЏ Р±Р»РёР¶РЅРµРіРѕ Р±РѕСЏ (СЃРѕСЃРµРґРЅСЏСЏ РєР»РµС‚РєР°)
    public float rangedPreferenceRange = 8f;   // Р”РёСЃС‚Р°РЅС†РёСЏ РїСЂРµРґРїРѕС‡С‚РµРЅРёСЏ РѕРіРЅРµСЃС‚СЂРµР»СЊРЅРѕРіРѕ РѕСЂСѓР¶РёСЏ
    public bool autoReloadEnabled = true;      // РђРІС‚РѕРјР°С‚РёС‡РµСЃРєР°СЏ РїРµСЂРµР·Р°СЂСЏРґРєР°
    public bool debugMode = false;             // РћС‚Р»Р°РґРѕС‡РЅС‹Р№ СЂРµР¶РёРј

    [Header("Default Weapons")]
    public MeleeWeaponCategory defaultMeleeCategory = MeleeWeaponCategory.Knife;
    public RangedWeaponCategory defaultRangedCategory = RangedWeaponCategory.Pistol;
    public ItemRarity defaultWeaponRarity = ItemRarity.Common;

    // РћСЂСѓР¶РёРµ РїРµСЂСЃРѕРЅР°Р¶Р°
    private List<Weapon> weapons = new List<Weapon>();
    private Weapon currentWeapon;
    private Character character;
    private Inventory inventory;

    // РљРµС€ РѕСЂСѓР¶РёСЏ РґР»СЏ СЃРѕС…СЂР°РЅРµРЅРёСЏ СЃРѕСЃС‚РѕСЏРЅРёСЏ (РїР°С‚СЂРѕРЅС‹, РїСЂРѕС‡РЅРѕСЃС‚СЊ)
    private Dictionary<string, RangedWeapon> rangedWeaponCache = new Dictionary<string, RangedWeapon>();

    // РЎС‚Р°С‚РёСЃС‚РёРєР° РёСЃРїРѕР»СЊР·РѕРІР°РЅРёСЏ
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
        // РРЅРёС†РёР°Р»РёР·РёСЂСѓРµРј Р±Р°Р·РѕРІРѕРµ РѕСЂСѓР¶РёРµ
        InitializeDefaultWeapons();

        // РџРѕРґРїРёСЃС‹РІР°РµРјСЃСЏ РЅР° РёР·РјРµРЅРµРЅРёСЏ РёРЅРІРµРЅС‚Р°СЂСЏ РґР»СЏ Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРѕР№ СЌРєРёРїРёСЂРѕРІРєРё
        if (inventory != null)
        {
            inventory.OnInventoryChanged += CheckAndEquipWeapons;
            inventory.OnEquipmentChanged += SyncEquipmentWithWeapons;
        }
    }

    /// <summary>
    /// РРЅРёС†РёР°Р»РёР·Р°С†РёСЏ Р±Р°Р·РѕРІРѕРіРѕ РѕСЂСѓР¶РёСЏ РґР»СЏ РїРµСЂСЃРѕРЅР°Р¶Р°
    /// </summary>
    private void InitializeDefaultWeapons()
    {
        // РЎРѕР·РґР°РµРј РўРћР›Р¬РљРћ Р±Р°Р·РѕРІРѕРµ РѕСЂСѓР¶РёРµ Р±Р»РёР¶РЅРµРіРѕ Р±РѕСЏ (РЅРѕР¶ - РІРёСЂС‚СѓР°Р»СЊРЅС‹Р№, РІСЃРµРіРґР° РґРѕСЃС‚СѓРїРµРЅ)
        MeleeWeapon meleeWeapon = MeleeWeapon.CreatePresetWeapon(defaultMeleeCategory, defaultWeaponRarity);
        AddWeapon(meleeWeapon);

        // РќР• СЃРѕР·РґР°РµРј Р±Р°Р·РѕРІРѕРµ РѕРіРЅРµСЃС‚СЂРµР»СЊРЅРѕРµ РѕСЂСѓР¶РёРµ - РѕРЅРѕ РґРѕР»Р¶РЅРѕ Р±С‹С‚СЊ РЅР°Р№РґРµРЅРѕ/СЌРєРёРїРёСЂРѕРІР°РЅРѕ
        // RangedWeapon rangedWeapon = RangedWeapon.CreatePresetWeapon(defaultRangedCategory, defaultWeaponRarity);
        // AddWeapon(rangedWeapon);

        // Р’С‹Р±РёСЂР°РµРј РѕСЂСѓР¶РёРµ Р±Р»РёР¶РЅРµРіРѕ Р±РѕСЏ РїРѕ СѓРјРѕР»С‡Р°РЅРёСЋ
        SetCurrentWeapon(meleeWeapon);
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ Рё СЌРєРёРїРёСЂРѕРІР°С‚СЊ РѕСЂСѓР¶РёРµ РёР· РёРЅРІРµРЅС‚Р°СЂСЏ РїСЂРё РёР·РјРµРЅРµРЅРёРё РёРЅРІРµРЅС‚Р°СЂСЏ
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
                    // РџСЂРѕРІРµСЂСЏРµРј, СЌРєРёРїРёСЂРѕРІР°РЅРѕ Р»Рё СѓР¶Рµ РѕСЂСѓР¶РёРµ
                    if (!inventory.HasWeaponEquipped())
                    {
                        // РЎРѕС…СЂР°РЅСЏРµРј СЃСЃС‹Р»РєСѓ РЅР° ItemData РїРµСЂРµРґ СЌРєРёРїРёСЂРѕРІРєРѕР№
                        ItemData weaponData = slot.itemData;

                        // РђРІС‚РѕРјР°С‚РёС‡РµСЃРєРё СЌРєРёРїРёСЂСѓРµРј РїРµСЂРІРѕРµ РЅР°Р№РґРµРЅРЅРѕРµ РѕСЂСѓР¶РёРµ
                        if (inventory.EquipItem(weaponData))
                        {
                            break;
                        }
                    }
                }
            }

            // РЎРёРЅС…СЂРѕРЅРёР·РёСЂСѓРµРј РѕСЂСѓР¶РёРµ СЃ СЌРєРёРїРёСЂРѕРІРєРѕР№
            SyncEquipmentWithWeapons();
        }
        catch (System.Exception e)
        {
            // РљСЂРёС‚РёС‡РЅР°СЏ РѕС€РёР±РєР° - РѕСЃС‚Р°РІР»СЏРµРј
        }
    }

    /// <summary>
    /// РЎРёРЅС…СЂРѕРЅРёР·РёСЂРѕРІР°С‚СЊ РѕСЂСѓР¶РёРµ WeaponSystem СЃ СЌРєРёРїРёСЂРѕРІРєРѕР№ РёР· Inventory
    /// </summary>
    private void SyncEquipmentWithWeapons()
    {
        if (inventory == null || character == null)
        {
            return;
        }

        try
        {
            // РћС‡РёС‰Р°РµРј СЃРїРёСЃРѕРє РѕРіРЅРµСЃС‚СЂРµР»СЊРЅРѕРіРѕ РѕСЂСѓР¶РёСЏ (РѕСЃС‚Р°РІР»СЏРµРј С‚РѕР»СЊРєРѕ РЅРѕР¶)
            weapons.RemoveAll(w => w != null && w.weaponType == WeaponType.Ranged);

            // РџСЂРѕРІРµСЂСЏРµРј СЌРєРёРїРёСЂРѕРІР°РЅРЅРѕРµ РѕСЂСѓР¶РёРµ РІ СЂСѓРєР°С…
            ItemData leftHandWeapon = inventory.GetEquippedItem(EquipmentSlot.LeftHand);
            ItemData rightHandWeapon = inventory.GetEquippedItem(EquipmentSlot.RightHand);

            // Р”РѕР±Р°РІР»СЏРµРј СЌРєРёРїРёСЂРѕРІР°РЅРЅРѕРµ РѕСЂСѓР¶РёРµ РІ WeaponSystem
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
            // РљСЂРёС‚РёС‡РЅР°СЏ РѕС€РёР±РєР° - РѕСЃС‚Р°РІР»СЏРµРј
        }
    }

    /// <summary>
    /// РЎРѕР·РґР°С‚СЊ RangedWeapon РёР· ItemData СЃ СЃРѕС…СЂР°РЅРµРЅРёРµРј СЃРѕСЃС‚РѕСЏРЅРёСЏ
    /// </summary>
    private RangedWeapon CreateRangedWeaponFromItemData(ItemData itemData)
    {
        if (itemData == null || itemData.itemType != ItemType.Weapon)
            return null;

        // РџСЂРѕРІРµСЂСЏРµРј, РµСЃС‚СЊ Р»Рё СѓР¶Рµ РѕСЂСѓР¶РёРµ РІ РєРµС€Рµ
        string weaponKey = itemData.itemName;
        if (rangedWeaponCache.ContainsKey(weaponKey))
        {
            return rangedWeaponCache[weaponKey];
        }

        // РЎРѕР·РґР°РµРј РЅРѕРІРѕРµ РѕРіРЅРµСЃС‚СЂРµР»СЊРЅРѕРµ РѕСЂСѓР¶РёРµ РЅР° РѕСЃРЅРѕРІРµ С…Р°СЂР°РєС‚РµСЂРёСЃС‚РёРє РїСЂРµРґРјРµС‚Р°
        RangedWeapon weapon = RangedWeapon.CreatePresetWeapon(defaultRangedCategory, itemData.rarity);
        weapon.weaponName = itemData.itemName;
        weapon.description = itemData.description;
        weapon.damage = itemData.damage;
        weapon.icon = itemData.icon;
        weapon.prefab = itemData.prefab;

        // Р’РђР–РќРћ: РЈР±РµР¶РґР°РµРјСЃСЏ С‡С‚Рѕ РѕСЂСѓР¶РёРµ РїРѕР»РЅРѕСЃС‚СЊСЋ Р·Р°СЂСЏР¶РµРЅРѕ РїСЂРё РїРµСЂРІРѕРј СЃРѕР·РґР°РЅРёРё
        weapon.ForceReload();

        // РЎРѕС…СЂР°РЅСЏРµРј РІ РєРµС€
        rangedWeaponCache[weaponKey] = weapon;

        return weapon;
    }

    /// <summary>
    /// Р”РѕР±Р°РІРёС‚СЊ РѕСЂСѓР¶РёРµ РІ Р°СЂСЃРµРЅР°Р»
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
    /// РЈРґР°Р»РёС‚СЊ РѕСЂСѓР¶РёРµ РёР· Р°СЂСЃРµРЅР°Р»Р°
    /// </summary>
    public void RemoveWeapon(Weapon weapon)
    {
        if (weapon == null) return;

        weapons.Remove(weapon);

        // РЈРґР°Р»СЏРµРј РёР· РєРµС€Р° РµСЃР»Рё СЌС‚Рѕ РѕРіРЅРµСЃС‚СЂРµР»СЊРЅРѕРµ РѕСЂСѓР¶РёРµ
        if (weapon is RangedWeapon)
        {
            rangedWeaponCache.Remove(weapon.weaponName);
        }

        // Р•СЃР»Рё СЌС‚Рѕ Р±С‹Р»Рѕ С‚РµРєСѓС‰РµРµ РѕСЂСѓР¶РёРµ, РІС‹Р±РёСЂР°РµРј РґСЂСѓРіРѕРµ
        if (currentWeapon == weapon)
        {
            SelectBestWeapon(Vector3.zero, 5f); // Р’С‹Р±РёСЂР°РµРј РѕСЂСѓР¶РёРµ РґР»СЏ СЃСЂРµРґРЅРµР№ РґРёСЃС‚Р°РЅС†РёРё
        }
    }

    /// <summary>
    /// РЈСЃС‚Р°РЅРѕРІРёС‚СЊ С‚РµРєСѓС‰РµРµ РѕСЂСѓР¶РёРµ
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
    /// РџРѕР»СѓС‡РёС‚СЊ С‚РµРєСѓС‰РµРµ РѕСЂСѓР¶РёРµ
    /// </summary>
    public Weapon GetCurrentWeapon()
    {
        return currentWeapon;
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РІСЃРµ РѕСЂСѓР¶РёРµ РїРµСЂСЃРѕРЅР°Р¶Р°
    /// </summary>
    public List<Weapon> GetAllWeapons()
    {
        return new List<Weapon>(weapons);
    }

    /// <summary>
    /// Р’С‹Р±СЂР°С‚СЊ Р»СѓС‡С€РµРµ РѕСЂСѓР¶РёРµ РґР»СЏ Р°С‚Р°РєРё С†РµР»Рё
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
            // Р”Р»СЏ РѕСЂСѓР¶РёСЏ Р±РµР· РїР°С‚СЂРѕРЅРѕРІ СЂР°Р·СЂРµС€Р°РµРј РІС‹Р±РѕСЂ (РґР»СЏ РїРµСЂРµР·Р°СЂСЏРґРєРё)
            bool canConsider = weapon.CanAttack();

            // РћСЃРѕР±С‹Р№ СЃР»СѓС‡Р°Р№: РґР°Р»СЊРЅРѕР±РѕР№РЅРѕРµ РѕСЂСѓР¶РёРµ Р±РµР· РїР°С‚СЂРѕРЅРѕРІ РјРѕР¶РµС‚ Р±С‹С‚СЊ РїРµСЂРµР·Р°СЂСЏР¶РµРЅРѕ
            if (!canConsider && weapon is RangedWeapon rangedWeapon)
            {
                if (rangedWeapon.NeedsReload() && !rangedWeapon.IsReloading())
                {
                    canConsider = true; // Р Р°Р·СЂРµС€Р°РµРј РІС‹Р±РѕСЂ РґР»СЏ РїРµСЂРµР·Р°СЂСЏРґРєРё
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

        // Р•СЃР»Рё РЅРµ РЅР°С€Р»Рё РїРѕРґС…РѕРґСЏС‰РµРµ РѕСЂСѓР¶РёРµ (РЅР°РїСЂРёРјРµСЂ, РІСЃРµ СЃР»РѕРјР°РЅРѕ), Р±РµСЂРµРј Р»СЋР±РѕРµ
        if (bestWeapon == null)
        {
            bestWeapon = weapons[0];
        }

        SetCurrentWeapon(bestWeapon);
    }

    /// <summary>
    /// Р Р°СЃСЃС‡РёС‚Р°С‚СЊ "РѕС†РµРЅРєСѓ" РѕСЂСѓР¶РёСЏ РґР»СЏ РґР°РЅРЅРѕР№ РґРёСЃС‚Р°РЅС†РёРё
    /// </summary>
    private float CalculateWeaponScore(Weapon weapon, float distance)
    {
        float score = 0f;

        // Р‘Р°Р·РѕРІР°СЏ РѕС†РµРЅРєР° - РјРѕР¶РµРј Р»Рё РёСЃРїРѕР»СЊР·РѕРІР°С‚СЊ РѕСЂСѓР¶РёРµ
        if (!weapon.CanAttack())
            return -1f;

        // РџСЂРѕРІРµСЂСЏРµРј РґР°Р»СЊРЅРѕСЃС‚СЊ
        if (distance > weapon.range)
            return -1f; // Р¦РµР»СЊ РІРЅРµ РґР°Р»СЊРЅРѕСЃС‚Рё

        // РћС†РµРЅРєР° РїРѕ С‚РёРїСѓ РѕСЂСѓР¶РёСЏ Рё РґРёСЃС‚Р°РЅС†РёРё
        if (weapon.weaponType == WeaponType.Melee)
        {
            // Р‘Р»РёР¶РЅРёР№ Р±РѕР№ РїСЂРµРґРїРѕС‡С‚РёС‚РµР»РµРЅ РўРћР›Р¬РљРћ РЅР° РєРѕСЂРѕС‚РєРѕР№ РґРёСЃС‚Р°РЅС†РёРё
            if (distance <= meleePreferenceRange)
            {
                score += 100f; // Р’С‹СЃРѕРєРёР№ РїСЂРёРѕСЂРёС‚РµС‚
                score += (meleePreferenceRange - distance) * 10f; // Р§РµРј Р±Р»РёР¶Рµ, С‚РµРј Р»СѓС‡С€Рµ
            }
            else
            {
                // Р—Р° РїСЂРµРґРµР»Р°РјРё РґРёСЃС‚Р°РЅС†РёРё Р±Р»РёР¶РЅРµРіРѕ Р±РѕСЏ - РћР§Р•РќР¬ РЅРёР·РєРёР№ РїСЂРёРѕСЂРёС‚РµС‚
                // Р§РµРј РґР°Р»СЊС€Рµ, С‚РµРј С…СѓР¶Рµ
                score += 5f - (distance - meleePreferenceRange) * 2f;

                // Р•СЃР»Рё РґР°Р»СЊС€Рµ 3 РµРґРёРЅРёС† - РІРѕРѕР±С‰Рµ РЅРµ СЂР°СЃСЃРјР°С‚СЂРёРІР°РµРј
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
                // Р‘Р•РЎРљРћРќР•Р§РќР«Р• РџРђРўР РћРќР« - РѕСЂСѓР¶РёРµ РІСЃРµРіРґР° РіРѕС‚РѕРІРѕ Рє СЃС‚СЂРµР»СЊР±Рµ
                if (!rangedWeapon.IsReloading())
                {
                    // РћРіРЅРµСЃС‚СЂРµР»СЊРЅРѕРµ РѕСЂСѓР¶РёРµ Р’РЎР•Р“Р”Рђ РїСЂРµРґРїРѕС‡С‚РёС‚РµР»СЊРЅРµРµ
                    score += 150f; // Р’Р«РЎРћРљРР™ Р±Р°Р·РѕРІС‹Р№ РїСЂРёРѕСЂРёС‚РµС‚

                    // РћРіРЅРµСЃС‚СЂРµР»СЊРЅРѕРµ РѕСЂСѓР¶РёРµ РїСЂРµРґРїРѕС‡С‚РёС‚РµР»СЊРЅРѕ РЅР° РґР°Р»СЊРЅРµР№ РґРёСЃС‚Р°РЅС†РёРё
                    if (distance >= meleePreferenceRange)
                    {
                        score += 50f; // Р”РѕРїРѕР»РЅРёС‚РµР»СЊРЅС‹Р№ Р±РѕРЅСѓСЃ РЅР° РґР°Р»СЊРЅРµР№ РґРёСЃС‚Р°РЅС†РёРё
                        if (distance <= rangedPreferenceRange)
                        {
                            score += (distance - meleePreferenceRange) * 5f; // Р‘РѕРЅСѓСЃ Р·Р° СЃСЂРµРґРЅСЋСЋ РґРёСЃС‚Р°РЅС†РёСЋ
                        }
                    }
                    else
                    {
                        // Р”Р°Р¶Рµ РЅР° Р±Р»РёР¶РЅРµР№ РґРёСЃС‚Р°РЅС†РёРё РѕСЂСѓР¶РёРµ РґР°Р»СЊРЅРµРіРѕ Р±РѕСЏ РїСЂРµРґРїРѕС‡С‚РёС‚РµР»СЊРЅРµРµ
                        score += 20f;
                    }
                }
                else
                {
                    // РџРµСЂРµР·Р°СЂСЏР¶Р°РµС‚СЃСЏ - РЅРёР·РєРёР№ РїСЂРёРѕСЂРёС‚РµС‚
                    score = 10f;
                }
            }
        }

        // Р‘РѕРЅСѓСЃ Р·Р° СѓСЂРѕРЅ Рё СЃРѕСЃС‚РѕСЏРЅРёРµ РѕСЂСѓР¶РёСЏ
        score += weapon.damage * 0.5f;
        score += weapon.GetDurabilityPercent() * 20f;

        // РЁС‚СЂР°С„ Р·Р° СЂРµРґРєРѕСЃС‚СЊ (С‡С‚РѕР±С‹ СЃРѕС…СЂР°РЅРёС‚СЊ СЂРµРґРєРѕРµ РѕСЂСѓР¶РёРµ)
        switch (weapon.rarity)
        {
            case ItemRarity.Legendary: score -= 30f; break;
            case ItemRarity.Epic: score -= 20f; break;
            case ItemRarity.Rare: score -= 10f; break;
        }

        return score;
    }

    /// <summary>
    /// РђС‚Р°РєРѕРІР°С‚СЊ С†РµР»СЊ СЃ Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРёРј РІС‹Р±РѕСЂРѕРј РѕСЂСѓР¶РёСЏ
    /// </summary>
    public void AttackTarget(Character target)
    {
        if (target == null)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, target.transform.position);

        // Р’С‹Р±РёСЂР°РµРј Р»СѓС‡С€РµРµ РѕСЂСѓР¶РёРµ РґР»СЏ СЌС‚РѕР№ РґРёСЃС‚Р°РЅС†РёРё
        SelectBestWeapon(target.transform.position, distance);

        if (currentWeapon == null)
        {
            return;
        }

        // РџСЂРѕРІРµСЂСЏРµРј РїРµСЂРµР·Р°СЂСЏРґРєСѓ РѕРіРЅРµСЃС‚СЂРµР»СЊРЅРѕРіРѕ РѕСЂСѓР¶РёСЏ
        if (currentWeapon is RangedWeapon rangedWeapon)
        {
            if (rangedWeapon.NeedsReload() && autoReloadEnabled && !rangedWeapon.IsReloading())
            {
                StartCoroutine(rangedWeapon.ReloadWeapon());
                return; // РќРµ Р°С‚Р°РєСѓРµРј РІРѕ РІСЂРµРјСЏ РїРµСЂРµР·Р°СЂСЏРґРєРё
            }
            else if (rangedWeapon.IsReloading())
            {
                return; // РќРµ Р°С‚Р°РєСѓРµРј РІРѕ РІСЂРµРјСЏ РїРµСЂРµР·Р°СЂСЏРґРєРё
            }
        }

        // Р’С‹РїРѕР»РЅСЏРµРј Р°С‚Р°РєСѓ
        currentWeapon.PerformAttack(character, target);

        // РћР±РЅРѕРІР»СЏРµРј СЃС‚Р°С‚РёСЃС‚РёРєСѓ
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
    /// РџРѕР»СѓС‡РёС‚СЊ РѕСЂСѓР¶РёРµ РѕРїСЂРµРґРµР»РµРЅРЅРѕРіРѕ С‚РёРїР°
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
    /// РџРѕР»СѓС‡РёС‚СЊ Р»СѓС‡С€РµРµ РѕСЂСѓР¶РёРµ Р±Р»РёР¶РЅРµРіРѕ Р±РѕСЏ
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
    /// РџРѕР»СѓС‡РёС‚СЊ Р»СѓС‡С€РµРµ РѕРіРЅРµСЃС‚СЂРµР»СЊРЅРѕРµ РѕСЂСѓР¶РёРµ
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
    /// РџСЂРёРЅСѓРґРёС‚РµР»СЊРЅРѕ РїРµСЂРµР·Р°СЂСЏРґРёС‚СЊ РІСЃРµ РѕРіРЅРµСЃС‚СЂРµР»СЊРЅРѕРµ РѕСЂСѓР¶РёРµ
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
    /// РџРѕР»СѓС‡РёС‚СЊ СЃС‚Р°С‚РёСЃС‚РёРєСѓ РёСЃРїРѕР»СЊР·РѕРІР°РЅРёСЏ РѕСЂСѓР¶РёСЏ
    /// </summary>
    public string GetWeaponStats()
    {
        return $"Melee attacks: {meleeAttacks}, Ranged attacks: {rangedAttacks}, Weapon switches: {weaponSwitches}";
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РёРЅС„РѕСЂРјР°С†РёСЋ Рѕ С‚РµРєСѓС‰РµРј РѕСЂСѓР¶РёРё
    /// </summary>
    public string GetCurrentWeaponInfo()
    {
        if (currentWeapon == null)
            return "No weapon equipped";

        return currentWeapon.GetDetailedDescription();
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂРёС‚СЊ, РјРѕР¶РµС‚ Р»Рё РїРµСЂСЃРѕРЅР°Р¶ Р°С‚Р°РєРѕРІР°С‚СЊ СЃ С‚РµРєСѓС‰РёРј РѕСЂСѓР¶РёРµРј
    /// </summary>
    public bool CanAttackWithCurrentWeapon()
    {
        return currentWeapon != null && currentWeapon.CanAttack();
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РґР°Р»СЊРЅРѕСЃС‚СЊ С‚РµРєСѓС‰РµРіРѕ РѕСЂСѓР¶РёСЏ
    /// </summary>
    public float GetCurrentWeaponRange()
    {
        return currentWeapon?.range ?? 0f;
    }

    void OnDestroy()
    {
        // РћС‚РїРёСЃС‹РІР°РµРјСЃСЏ РѕС‚ СЃРѕР±С‹С‚РёР№ РёРЅРІРµРЅС‚Р°СЂСЏ
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
            // РџРѕРєР°Р·С‹РІР°РµРј РґР°Р»СЊРЅРѕСЃС‚СЊ С‚РµРєСѓС‰РµРіРѕ РѕСЂСѓР¶РёСЏ
            Gizmos.color = currentWeapon.weaponType == WeaponType.Melee ? Color.red : Color.blue;
            Gizmos.DrawWireSphere(transform.position, currentWeapon.range);

            // РџРѕРєР°Р·С‹РІР°РµРј Р·РѕРЅС‹ РїСЂРµРґРїРѕС‡С‚РµРЅРёСЏ
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, meleePreferenceRange);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, rangedPreferenceRange);
        }
    }
}
