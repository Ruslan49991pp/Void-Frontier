using UnityEngine;
using System.Collections;

/// <summary>
/// Тестовый скрипт для проверки системы оружия
/// </summary>
public class WeaponSystemTest : MonoBehaviour
{
    [Header("Test Settings")]
    public bool runTestOnStart = false;
    public KeyCode testKey = KeyCode.T;
    public KeyCode damageTextTestKey = KeyCode.Y;
    public KeyCode cleanupAllKey = KeyCode.C;

    void Start()
    {
        if (runTestOnStart)
        {
            RunWeaponSystemTest();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(testKey))
        {
            RunWeaponSystemTest();
        }

        if (Input.GetKeyDown(damageTextTestKey))
        {
            TestDamageTextDisplay();
        }

        if (Input.GetKeyDown(cleanupAllKey))
        {
            ForceCleanupAllDamageTexts();
        }
    }

    /// <summary>
    /// Запустить тест системы оружия
    /// </summary>
    public void RunWeaponSystemTest()
    {
        Debug.Log("=== WEAPON SYSTEM TEST START ===");

        // Тест создания оружия ближнего боя
        TestMeleeWeaponCreation();

        // Тест создания огнестрельного оружия
        TestRangedWeaponCreation();

        // Тест системы оружия на персонажах
        TestCharacterWeaponSystem();

        Debug.Log("=== WEAPON SYSTEM TEST END ===");
    }

    /// <summary>
    /// Тест создания оружия ближнего боя
    /// </summary>
    void TestMeleeWeaponCreation()
    {
        Debug.Log("[TEST] Testing melee weapon creation...");

        // Создаем разные типы оружия ближнего боя
        MeleeWeapon knife = MeleeWeapon.CreatePresetWeapon(MeleeWeaponCategory.Knife, ItemRarity.Common);
        MeleeWeapon sword = MeleeWeapon.CreatePresetWeapon(MeleeWeaponCategory.Sword, ItemRarity.Rare);
        MeleeWeapon axe = MeleeWeapon.CreatePresetWeapon(MeleeWeaponCategory.Axe, ItemRarity.Epic);

        Debug.Log($"[TEST] Created {knife.weaponName}: Damage={knife.damage}, Range={knife.range}");
        Debug.Log($"[TEST] Created {sword.weaponName}: Damage={sword.damage}, Range={sword.range}");
        Debug.Log($"[TEST] Created {axe.weaponName}: Damage={axe.damage}, Range={axe.range}");

        // Тест расчета урона
        float knifeDamage = knife.CalculateFinalDamage();
        Debug.Log($"[TEST] {knife.weaponName} final damage: {knifeDamage:F1}");
    }

    /// <summary>
    /// Тест создания огнестрельного оружия
    /// </summary>
    void TestRangedWeaponCreation()
    {
        Debug.Log("[TEST] Testing ranged weapon creation...");

        // Создаем разные типы огнестрельного оружия
        RangedWeapon pistol = RangedWeapon.CreatePresetWeapon(RangedWeaponCategory.Pistol, ItemRarity.Common);
        RangedWeapon rifle = RangedWeapon.CreatePresetWeapon(RangedWeaponCategory.Rifle, ItemRarity.Uncommon);
        RangedWeapon shotgun = RangedWeapon.CreatePresetWeapon(RangedWeaponCategory.Shotgun, ItemRarity.Legendary);

        Debug.Log($"[TEST] Created {pistol.weaponName}: Damage={pistol.damage}, Range={pistol.range}, Ammo={pistol.currentAmmo}/{pistol.magazineSize}");
        Debug.Log($"[TEST] Created {rifle.weaponName}: Damage={rifle.damage}, Range={rifle.range}, Ammo={rifle.currentAmmo}/{rifle.magazineSize}");
        Debug.Log($"[TEST] Created {shotgun.weaponName}: Damage={shotgun.damage}, Range={shotgun.range}, Ammo={shotgun.currentAmmo}/{shotgun.magazineSize}");

        // Тест стрельбы и перезарядки
        Debug.Log($"[TEST] {pistol.weaponName} can attack: {pistol.CanAttack()}");
        Debug.Log($"[TEST] {pistol.weaponName} needs reload: {pistol.NeedsReload()}");

        // Израсходуем все патроны
        for (int i = 0; i < pistol.magazineSize; i++)
        {
            pistol.currentAmmo--;
        }

        Debug.Log($"[TEST] After shooting all ammo - Can attack: {pistol.CanAttack()}, Needs reload: {pistol.NeedsReload()}");

        // Перезарядка
        pistol.ForceReload();
        Debug.Log($"[TEST] After reload - Ammo: {pistol.currentAmmo}/{pistol.magazineSize}, Can attack: {pistol.CanAttack()}");
    }

    /// <summary>
    /// Тест системы оружия на персонажах
    /// </summary>
    void TestCharacterWeaponSystem()
    {
        Debug.Log("[TEST] Testing character weapon system...");

        // Найдем персонажей на сцене
        Character[] characters = FindObjectsOfType<Character>();

        if (characters.Length == 0)
        {
            Debug.LogWarning("[TEST] No characters found on scene for weapon system test");
            return;
        }

        // Тестируем систему оружия на первом персонаже
        Character testCharacter = characters[0];
        WeaponSystem weaponSystem = testCharacter.GetComponent<WeaponSystem>();

        if (weaponSystem == null)
        {
            Debug.LogWarning($"[TEST] Character {testCharacter.GetFullName()} doesn't have WeaponSystem component");
            return;
        }

        Debug.Log($"[TEST] Testing weapon system on {testCharacter.GetFullName()}");

        // Проверяем оружие персонажа
        var weapons = weaponSystem.GetAllWeapons();
        Debug.Log($"[TEST] Character has {weapons.Count} weapons");

        foreach (var weapon in weapons)
        {
            Debug.Log($"[TEST] - {weapon.weaponName} ({weapon.weaponType}): Damage={weapon.damage}, Range={weapon.range}");
        }

        // Тест выбора оружия для разных дистанций
        TestWeaponSelection(weaponSystem, 1f);   // Ближняя дистанция
        TestWeaponSelection(weaponSystem, 5f);   // Средняя дистанция
        TestWeaponSelection(weaponSystem, 15f);  // Дальняя дистанция

        // Проверяем информацию о текущем оружии
        string weaponInfo = weaponSystem.GetCurrentWeaponInfo();
        Debug.Log($"[TEST] Current weapon info:\n{weaponInfo}");

        // Статистика
        string stats = weaponSystem.GetWeaponStats();
        Debug.Log($"[TEST] Weapon stats: {stats}");
    }

    /// <summary>
    /// Тест выбора оружия для определенной дистанции
    /// </summary>
    void TestWeaponSelection(WeaponSystem weaponSystem, float distance)
    {
        Vector3 fakeTargetPos = weaponSystem.transform.position + Vector3.forward * distance;
        weaponSystem.SelectBestWeapon(fakeTargetPos, distance);

        Weapon selectedWeapon = weaponSystem.GetCurrentWeapon();
        if (selectedWeapon != null)
        {
            Debug.Log($"[TEST] Distance {distance:F1}m: Selected {selectedWeapon.weaponName} ({selectedWeapon.weaponType})");
        }
        else
        {
            Debug.Log($"[TEST] Distance {distance:F1}m: No weapon selected");
        }
    }

    /// <summary>
    /// Тест создания пули (если есть цели)
    /// </summary>
    void TestBulletCreation()
    {
        Debug.Log("[TEST] Testing bullet creation...");

        // Создаем тестовую пулю
        GameObject bulletObj = new GameObject("TestBullet");
        Bullet bullet = bulletObj.AddComponent<Bullet>();

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + Vector3.forward * 10f;

        bullet.Initialize(startPos, targetPos, null, 25f, 50f, 0.9f);

        Debug.Log("[TEST] Test bullet created and initialized");

        // Удаляем тестовую пулю через 2 секунды
        Destroy(bulletObj, 2f);
    }

    /// <summary>
    /// Информация о системе оружия для UI
    /// </summary>
    [ContextMenu("Show Weapon System Info")]
    public void ShowWeaponSystemInfo()
    {
        Character[] characters = FindObjectsOfType<Character>();

        Debug.Log("=== WEAPON SYSTEM INFO ===");

        foreach (Character character in characters)
        {
            WeaponSystem weaponSystem = character.GetComponent<WeaponSystem>();
            if (weaponSystem != null)
            {
                Debug.Log($"\n{character.GetFullName()}:");
                Debug.Log($"  Current weapon: {weaponSystem.GetCurrentWeapon()?.weaponName ?? "None"}");
                Debug.Log($"  Total weapons: {weaponSystem.GetAllWeapons().Count}");
                Debug.Log($"  Can attack: {weaponSystem.CanAttackWithCurrentWeapon()}");
                Debug.Log($"  Weapon range: {weaponSystem.GetCurrentWeaponRange():F1}");
                Debug.Log($"  Stats: {weaponSystem.GetWeaponStats()}");
            }
        }
    }

    /// <summary>
    /// Тест отображения текста урона с поворотом к камере
    /// </summary>
    public void TestDamageTextDisplay()
    {
        Debug.Log("[TEST] Testing damage text display with camera rotation...");

        // Создаем несколько тестовых текстов урона в разных позициях
        Vector3[] testPositions = {
            transform.position + Vector3.forward * 2f + Vector3.up * 1.8f,
            transform.position + Vector3.right * 3f + Vector3.up * 1.8f,
            transform.position + Vector3.left * 3f + Vector3.up * 1.8f,
            transform.position + Vector3.back * 2f + Vector3.up * 1.8f,
            transform.position + Vector3.up * 1.8f
        };

        string[] damageTexts = { "-25", "-50", "-100", "-15", "-75" };
        Color[] colors = { Color.white, Color.white, Color.white, Color.yellow, Color.cyan };

        for (int i = 0; i < testPositions.Length && i < damageTexts.Length; i++)
        {
            // Создаем текст урона с помощью статического метода
            GameObject damageTextObj = LookAtCamera.CreateBillboardText(
                damageTexts[i],
                testPositions[i],
                colors[i],
                10
            );

            Debug.Log($"[WeaponSystemTest] Created test damage text object: {damageTextObj.name} at position {damageTextObj.transform.position}");

            // Анимируем текст - СТРОГО 1 секунда
            Coroutine animationCoroutine = StartCoroutine(AnimateTestDamageText(damageTextObj, 1.0f));

            // Регистрируем в менеджере для отслеживания
            DamageTextManager.Instance.RegisterDamageText(damageTextObj, animationCoroutine);

            // ПРИНУДИТЕЛЬНОЕ уничтожение ровно через 1 секунду
            StartCoroutine(ForceCleanupAfterDelay(damageTextObj, 1.0f));
        }

        Debug.Log("[TEST] Created 5 test damage texts. They should always face the camera!");
        Debug.Log("[TEST] Damage texts will disappear EXACTLY after 1 second from creation!");
        Debug.Log("[TEST] Check the console for detailed animation logs to track object lifecycle.");
        Debug.Log("[TEST] Press 'C' to force cleanup all damage texts if they don't disappear automatically.");
    }

    /// <summary>
    /// Анимация тестового текста урона
    /// </summary>
    private IEnumerator AnimateTestDamageText(GameObject damageTextObj, float duration)
    {
        Debug.Log($"[WeaponSystemTest] Starting damage text animation for {damageTextObj.name}, duration: {duration}s");

        TextMesh textMesh = damageTextObj.GetComponent<TextMesh>();
        Vector3 startPos = damageTextObj.transform.position;
        Vector3 endPos = new Vector3(startPos.x, 10f, startPos.z);

        Debug.Log($"[WeaponSystemTest] Animation path: {startPos} -> {endPos}");

        Color startColor = textMesh.color;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;

            // Движение вверх
            damageTextObj.transform.position = Vector3.Lerp(startPos, endPos, t);

            // Плавное исчезновение
            Color color = startColor;
            color.a = 1f - t;
            textMesh.color = color;

            // Увеличение размера в начале, затем уменьшение
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.5f;
            damageTextObj.transform.localScale = Vector3.one * scale;

            elapsedTime += Time.deltaTime;

            // Логируем каждые 0.5 секунды
            if (Mathf.FloorToInt(elapsedTime * 2) > Mathf.FloorToInt((elapsedTime - Time.deltaTime) * 2))
            {
                Debug.Log($"[WeaponSystemTest] Animation progress: {t:F2}, position: {damageTextObj.transform.position}, alpha: {color.a:F2}");
            }

            yield return null;
        }

        Debug.Log($"[WeaponSystemTest] Animation completed for {damageTextObj.name}, elapsed time: {elapsedTime:F2}s");

        // Принудительно уничтожаем через менеджер
        if (damageTextObj != null)
        {
            Debug.Log($"[WeaponSystemTest] Requesting cleanup for {damageTextObj.name}");
            DamageTextManager.Instance.ForceCleanupObject(damageTextObj);
        }
    }

    /// <summary>
    /// Принудительная очистка объекта через заданное время
    /// </summary>
    private IEnumerator ForceCleanupAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (obj != null)
        {
            Debug.Log($"[WeaponSystemTest] MANDATORY cleanup after {delay}s for {obj.name}");
            DamageTextManager.Instance.ForceCleanupObject(obj);
        }
    }

    /// <summary>
    /// Принудительно очистить все объекты текста урона
    /// </summary>
    public void ForceCleanupAllDamageTexts()
    {
        Debug.Log("[TEST] Force cleaning up all damage texts...");
        int count = DamageTextManager.Instance.GetActiveDamageTextCount();
        Debug.Log($"[TEST] Found {count} active damage texts to clean up");

        DamageTextManager.Instance.CleanupAllDamageTexts();

        Debug.Log("[TEST] Force cleanup completed!");
    }

    /// <summary>
    /// Показать статистику активных объектов урона
    /// </summary>
    [ContextMenu("Show Damage Text Stats")]
    public void ShowDamageTextStats()
    {
        int count = DamageTextManager.Instance.GetActiveDamageTextCount();
        Debug.Log($"[TEST] Active damage texts: {count}");
    }
}