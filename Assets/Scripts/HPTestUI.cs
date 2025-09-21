using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Простой UI для тестирования системы HP персонажей
/// </summary>
public class HPTestUI : MonoBehaviour
{
    [Header("Settings")]
    public bool enableTestUI = true;

    private Canvas testCanvas;
    private GameObject testPanel;
    private bool isUICreated = false;

    void Start()
    {
        if (enableTestUI)
        {
            // Создаем UI с задержкой
            Invoke("CreateTestUI", 2f);
        }
    }

    void Update()
    {
        // Создаем UI если его еще нет
        if (enableTestUI && !isUICreated && Time.time > 3f)
        {
            CreateTestUI();
        }

        // Горячие клавиши для тестирования
        if (enableTestUI && isUICreated)
        {
            HandleHotkeys();
        }
    }

    void CreateTestUI()
    {
        if (isUICreated) return;

        Debug.Log("HPTestUI: Creating test UI for HP system");

        // Создаем Canvas
        GameObject canvasGO = new GameObject("HPTestCanvas");
        testCanvas = canvasGO.AddComponent<Canvas>();
        testCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        testCanvas.sortingOrder = 200; // Поверх всего остального

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGO.AddComponent<GraphicRaycaster>();

        // Создаем панель
        testPanel = new GameObject("HPTestPanel");
        testPanel.transform.SetParent(testCanvas.transform, false);

        Image panelBg = testPanel.AddComponent<Image>();
        panelBg.color = new Color(0, 0, 0, 0.8f);

        RectTransform panelRect = testPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(0, 0);
        panelRect.pivot = new Vector2(0, 0);
        panelRect.anchoredPosition = new Vector2(10, 10);
        panelRect.sizeDelta = new Vector2(300, 200);

        // Создаем текст с инструкциями
        CreateInstructionText();

        // Создаем кнопки
        CreateTestButtons();

        isUICreated = true;
        Debug.Log("HPTestUI: Test UI created successfully");
    }

    void CreateInstructionText()
    {
        GameObject textGO = new GameObject("Instructions");
        textGO.transform.SetParent(testPanel.transform, false);

        Text instructionText = textGO.AddComponent<Text>();
        instructionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        instructionText.fontSize = 14;
        instructionText.color = Color.white;
        instructionText.alignment = TextAnchor.UpperLeft;
        instructionText.text = "HP Test Controls:\n\n" +
                              "1 - Damage all characters (10 HP)\n" +
                              "2 - Heal all characters (15 HP)\n" +
                              "3 - Set random HP to all\n" +
                              "4 - Restore all to full HP\n" +
                              "0 - Toggle this panel";

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 60);
        textRect.offsetMax = new Vector2(-10, -10);
    }

    void CreateTestButtons()
    {
        // Кнопка урона
        CreateButton("Damage All (-10 HP)", new Vector2(10, 10), new Vector2(130, 25), () => DamageAllCharacters(10f));

        // Кнопка лечения
        CreateButton("Heal All (+15 HP)", new Vector2(150, 10), new Vector2(130, 25), () => HealAllCharacters(15f));

        // Кнопка случайного HP
        CreateButton("Random HP", new Vector2(10, 35), new Vector2(130, 25), () => SetRandomHealthAll());

        // Кнопка полного восстановления
        CreateButton("Full Heal", new Vector2(150, 35), new Vector2(130, 25), () => FullHealAll());
    }

    void CreateButton(string text, Vector2 position, Vector2 size, System.Action onClick)
    {
        GameObject buttonGO = new GameObject($"Button_{text}");
        buttonGO.transform.SetParent(testPanel.transform, false);

        Image buttonBg = buttonGO.AddComponent<Image>();
        buttonBg.color = new Color(0.2f, 0.4f, 0.8f, 0.8f);

        Button button = buttonGO.AddComponent<Button>();
        button.targetGraphic = buttonBg;
        button.onClick.AddListener(() => onClick());

        RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.anchorMin = Vector2.zero;
        buttonRect.anchorMax = Vector2.zero;
        buttonRect.pivot = Vector2.zero;
        buttonRect.anchoredPosition = position;
        buttonRect.sizeDelta = size;

        // Текст кнопки
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);

        Text buttonText = textGO.AddComponent<Text>();
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 10;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.text = text;

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    void HandleHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            DamageAllCharacters(10f);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            HealAllCharacters(15f);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SetRandomHealthAll();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            FullHealAll();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            TogglePanel();
        }
    }

    void DamageAllCharacters(float damage)
    {
        Character[] characters = FindObjectsOfType<Character>();
        foreach (Character character in characters)
        {
            character.TakeDamage(damage);
        }
        Debug.Log($"HPTestUI: Damaged all {characters.Length} characters by {damage} HP");
    }

    void HealAllCharacters(float amount)
    {
        Character[] characters = FindObjectsOfType<Character>();
        foreach (Character character in characters)
        {
            character.Heal(amount);
        }
        Debug.Log($"HPTestUI: Healed all {characters.Length} characters by {amount} HP");
    }

    void SetRandomHealthAll()
    {
        Character[] characters = FindObjectsOfType<Character>();
        foreach (Character character in characters)
        {
            float randomHealth = Random.Range(10f, character.GetMaxHealth());
            character.SetHealth(randomHealth);
        }
        Debug.Log($"HPTestUI: Set random health for all {characters.Length} characters");
    }

    void FullHealAll()
    {
        Character[] characters = FindObjectsOfType<Character>();
        foreach (Character character in characters)
        {
            character.SetHealth(character.GetMaxHealth());
        }
        Debug.Log($"HPTestUI: Fully healed all {characters.Length} characters");
    }

    void TogglePanel()
    {
        if (testPanel != null)
        {
            testPanel.SetActive(!testPanel.activeSelf);
        }
    }

    void OnDestroy()
    {
        if (testCanvas != null)
        {
            DestroyImmediate(testCanvas.gameObject);
        }
    }
}