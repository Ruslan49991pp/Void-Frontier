using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Простой тестовый UI для проверки кнопки строительства
/// Добавьте этот скрипт на пустой GameObject в сцене для быстрого тестирования
/// </summary>
public class TestUI : MonoBehaviour
{
    private Canvas canvas;
    private Button buildButton;
    private Text statusText;
    private bool buildMode = false;

    void Start()
    {
        CreateSimpleUI();
    }

    void CreateSimpleUI()
    {
        // Создаем Canvas
        GameObject canvasGO = new GameObject("TestCanvas");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Создаем EventSystem если его нет
        var eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Создаем кнопку
        CreateButton();
        CreateStatusText();
    }

    void CreateButton()
    {
        GameObject buttonGO = new GameObject("ShipBuildButton");
        buttonGO.transform.SetParent(canvas.transform, false);

        // RectTransform для позиционирования
        RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.8f, 0.1f);
        buttonRect.anchorMax = new Vector2(0.95f, 0.25f);
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        // Фон кнопки
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.6f, 0.2f, 1f);

        // Компонент Button
        buildButton = buttonGO.AddComponent<Button>();
        buildButton.image = buttonImage;

        // Текст кнопки
        GameObject textGO = new GameObject("ButtonText");
        textGO.transform.SetParent(buttonGO.transform, false);

        Text buttonText = textGO.AddComponent<Text>();
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 16;
        buttonText.color = Color.white;
        buttonText.text = "СТРОИТЬ";
        buttonText.alignment = TextAnchor.MiddleCenter;

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Обработчик клика
        buildButton.onClick.AddListener(ToggleBuildMode);
    }

    void CreateStatusText()
    {
        GameObject textGO = new GameObject("StatusText");
        textGO.transform.SetParent(canvas.transform, false);

        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.05f, 0.8f);
        textRect.anchorMax = new Vector2(0.5f, 0.95f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        statusText = textGO.AddComponent<Text>();
        statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusText.fontSize = 14;
        statusText.color = Color.white;
        statusText.text = "Тестовый UI загружен\nНажмите кнопку СТРОИТЬ";
        statusText.alignment = TextAnchor.UpperLeft;
    }

    void ToggleBuildMode()
    {
        buildMode = !buildMode;

        if (buildMode)
        {
            buildButton.image.color = new Color(0.6f, 0.2f, 0.2f, 1f);
            buildButton.GetComponentInChildren<Text>().text = "ОТМЕНА";
            statusText.text = "Режим строительства АКТИВЕН!\n\nЭто тестовый UI.\nДля полного функционала используйте GameUI.";
        }
        else
        {
            buildButton.image.color = new Color(0.2f, 0.6f, 0.2f, 1f);
            buildButton.GetComponentInChildren<Text>().text = "СТРОИТЬ";
            statusText.text = "Режим строительства выключен\n\nНажмите кнопку СТРОИТЬ";
        }

    }

    void Update()
    {
        // Показываем статус нажатия клавиш
        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleBuildMode();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (buildMode)
            {
                ToggleBuildMode();
            }
        }
    }
}