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
        // Тест создания оружия ближнего боя
        TestMeleeWeaponCreation();

        // Тест создания огнестрельного оружия
        TestRangedWeaponCreation();

        // Тест системы оружия на персонажах
        TestCharacterWeaponSystem();
    }

    /// <summary>
    /// Тест создания оружия ближнего боя
    /// </summary>
    void TestMeleeWeaponCreation()
    {
        // Создаем разные типы оружия ближнего боя
        MeleeWeapon knife = MeleeWeapon.CreatePresetWeapon(MeleeWeaponCategory.Knife, ItemRarity.Common);
        MeleeWeapon sword = MeleeWeapon.CreatePresetWeapon(MeleeWeaponCategory.Sword, ItemRarity.Rare);
        MeleeWeapon axe = MeleeWeapon.CreatePresetWeapon(MeleeWeaponCategory.Axe, ItemRarity.Epic);

        // Тест расчета урона
        float knifeDamage = knife.CalculateFinalDamage();
    }

    /// <summary>
    /// Тест создания огнестрельного оружия
    /// </summary>
    void TestRangedWeaponCreation()
    {
        // Создаем разные типы огнестрельного оружия
        RangedWeapon pistol = RangedWeapon.CreatePresetWeapon(RangedWeaponCategory.Pistol, ItemRarity.Common);
        RangedWeapon rifle = RangedWeapon.CreatePresetWeapon(RangedWeaponCategory.Rifle, ItemRarity.Uncommon);
        RangedWeapon shotgun = RangedWeapon.CreatePresetWeapon(RangedWeaponCategory.Shotgun, ItemRarity.Legendary);

        // Тест стрельбы и перезарядки
        bool canAttack = pistol.CanAttack();
        bool needsReload = pistol.NeedsReload();

        // Израсходуем все патроны
        for (int i = 0; i < pistol.magazineSize; i++)
        {
            pistol.currentAmmo--;
        }

        // Перезарядка
        pistol.ForceReload();
    }

    /// <summary>
    /// Тест системы оружия на персонажах
    /// </summary>
    void TestCharacterWeaponSystem()
    {
        // Найдем персонажей на сцене
        Character[] characters = FindObjectsOfType<Character>();

        if (characters.Length == 0)
        {
            return;
        }

        // Тестируем систему оружия на первом персонаже
        Character testCharacter = characters[0];
        WeaponSystem weaponSystem = testCharacter.GetComponent<WeaponSystem>();

        if (weaponSystem == null)
        {
            return;
        }

        // Проверяем оружие персонажа
        var weapons = weaponSystem.GetAllWeapons();

        // Тест выбора оружия для разных дистанций
        TestWeaponSelection(weaponSystem, 1f);   // Ближняя дистанция
        TestWeaponSelection(weaponSystem, 5f);   // Средняя дистанция
        TestWeaponSelection(weaponSystem, 15f);  // Дальняя дистанция

        // Проверяем информацию о текущем оружии
        string weaponInfo = weaponSystem.GetCurrentWeaponInfo();

        // Статистика
        string stats = weaponSystem.GetWeaponStats();
    }

    /// <summary>
    /// Тест выбора оружия для определенной дистанции
    /// </summary>
    void TestWeaponSelection(WeaponSystem weaponSystem, float distance)
    {
        Vector3 fakeTargetPos = weaponSystem.transform.position + Vector3.forward * distance;
        weaponSystem.SelectBestWeapon(fakeTargetPos, distance);

        Weapon selectedWeapon = weaponSystem.GetCurrentWeapon();
    }

    /// <summary>
    /// Тест создания пули (если есть цели)
    /// </summary>
    void TestBulletCreation()
    {
        // Создаем тестовую пулю
        GameObject bulletObj = new GameObject("TestBullet");
        Bullet bullet = bulletObj.AddComponent<Bullet>();

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + Vector3.forward * 10f;

        bullet.Initialize(startPos, targetPos, null, 25f, 50f, 0.9f);

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

        foreach (Character character in characters)
        {
            WeaponSystem weaponSystem = character.GetComponent<WeaponSystem>();
            if (weaponSystem != null)
            {
                string info = $"{character.GetFullName()}: {weaponSystem.GetCurrentWeapon()?.weaponName ?? "None"}";
            }
        }
    }

    /// <summary>
    /// Тест отображения текста урона с поворотом к камере
    /// </summary>
    public void TestDamageTextDisplay()
    {
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

            // Анимируем текст - СТРОГО 1 секунда
            Coroutine animationCoroutine = StartCoroutine(AnimateTestDamageText(damageTextObj, 1.0f));

            // Регистрируем в менеджере для отслеживания
            DamageTextManager.Instance.RegisterDamageText(damageTextObj, animationCoroutine);

            // ПРИНУДИТЕЛЬНОЕ уничтожение ровно через 1 секунду
            StartCoroutine(ForceCleanupAfterDelay(damageTextObj, 1.0f));
        }
    }

    /// <summary>
    /// Анимация тестового текста урона
    /// </summary>
    private IEnumerator AnimateTestDamageText(GameObject damageTextObj, float duration)
    {
        TextMesh textMesh = damageTextObj.GetComponent<TextMesh>();
        Vector3 startPos = damageTextObj.transform.position;
        Vector3 endPos = new Vector3(startPos.x, 10f, startPos.z);

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
            yield return null;
        }

        // Принудительно уничтожаем через менеджер
        if (damageTextObj != null)
        {
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
            DamageTextManager.Instance.ForceCleanupObject(obj);
        }
    }

    /// <summary>
    /// Принудительно очистить все объекты текста урона
    /// </summary>
    public void ForceCleanupAllDamageTexts()
    {
        DamageTextManager.Instance.CleanupAllDamageTexts();
    }

    /// <summary>
    /// Показать статистику активных объектов урона
    /// </summary>
    [ContextMenu("Show Damage Text Stats")]
    public void ShowDamageTextStats()
    {
        int count = DamageTextManager.Instance.GetActiveDamageTextCount();
    }
}