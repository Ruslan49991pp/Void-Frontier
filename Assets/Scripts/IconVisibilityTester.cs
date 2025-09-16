using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Тестер видимости иконок - создает простые тестовые иконки
/// </summary>
public class IconVisibilityTester : MonoBehaviour
{
    [Header("Test Keys")]
    public KeyCode createTestIconKey = KeyCode.I;

    private Canvas testCanvas;
    private GameObject testContainer;
    private int iconCount = 0;

    void Start()
    {
        CreateTestUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(createTestIconKey))
        {
            CreateTestIcon();
        }
    }

    void CreateTestUI()
    {
        // Создаем тестовый Canvas
        GameObject canvasGO = new GameObject("TestIconCanvas");
        testCanvas = canvasGO.AddComponent<Canvas>();
        testCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        testCanvas.sortingOrder = 200;

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Создаем контейнер справа от красного
        testContainer = new GameObject("TestContainer");
        testContainer.transform.SetParent(testCanvas.transform, false);

        RectTransform containerRect = testContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 1);
        containerRect.anchorMax = new Vector2(0, 1);
        containerRect.pivot = new Vector2(0, 1);
        containerRect.anchoredPosition = new Vector2(250, -10); // Справа от основной панели
        containerRect.sizeDelta = new Vector2(150, 400);

        // Синий фон для контраста
        Image containerBg = testContainer.AddComponent<Image>();
        containerBg.color = new Color(0, 0, 1, 0.8f);

        Debug.Log("IconVisibilityTester: Test UI created");

        // Создаем первую тестовую иконку сразу
        CreateTestIcon();
    }

    void CreateTestIcon()
    {
        iconCount++;

        GameObject iconGO = new GameObject($"TestIcon_{iconCount}");
        iconGO.transform.SetParent(testContainer.transform, false);

        RectTransform iconRect = iconGO.AddComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(130, 40);

        // Ручное позиционирование
        iconRect.anchorMin = new Vector2(0, 1);
        iconRect.anchorMax = new Vector2(0, 1);
        iconRect.pivot = new Vector2(0, 1);
        iconRect.anchoredPosition = new Vector2(10, -10 - ((iconCount - 1) * 50));

        // Оранжевый фон
        Image iconBg = iconGO.AddComponent<Image>();
        iconBg.color = new Color(1, 0.5f, 0, 1f);

        // Текст иконки
        GameObject textGO = new GameObject("IconText");
        textGO.transform.SetParent(iconGO.transform, false);

        Text iconText = textGO.AddComponent<Text>();
        iconText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        iconText.fontSize = 12;
        iconText.color = Color.white;
        iconText.text = $"Test Icon #{iconCount}";
        iconText.alignment = TextAnchor.MiddleCenter;

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Debug.Log($"IconVisibilityTester: Created test icon {iconCount} at position {iconRect.anchoredPosition}");
    }

    void OnGUI()
    {
        GUI.Label(new Rect(400, 10, 300, 20), $"Press {createTestIconKey} to create test icon");
        GUI.Label(new Rect(400, 30, 300, 20), $"Test icons created: {iconCount}");
        GUI.Label(new Rect(400, 50, 300, 20), "Blue panel should show orange test icons");
    }
}