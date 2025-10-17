using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;
using TMPro;

/// <summary>
/// Editor скрипт для настройки панели ресурсов
/// </summary>
public class ResourcePanelSetup
{
    private const string PREFAB_FOLDER = "Assets/Prefabs/UI";
    private const string RESOURCE_SLOT_PREFAB_PATH = "Assets/Prefabs/UI/ResourceSlot.prefab";

    /// <summary>
    /// Создать префаб слота ресурса
    /// </summary>
    [MenuItem("Tools/Resources/Create Resource Slot Prefab")]
    public static void CreateResourceSlotPrefab()
    {
        // Создаем папку, если её нет
        if (!Directory.Exists(PREFAB_FOLDER))
        {
            Directory.CreateDirectory(PREFAB_FOLDER);
            AssetDatabase.Refresh();
        }

        // Создаем корневой объект
        GameObject slotObj = new GameObject("ResourceSlot");

        // Добавляем RectTransform
        RectTransform rootRect = slotObj.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(60, 60);

        // Добавляем фоновое изображение
        Image backgroundImage = slotObj.AddComponent<Image>();
        backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // Создаем иконку
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(slotObj.transform, false);

        Image iconImage = iconObj.AddComponent<Image>();
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.1f, 0.3f);
        iconRect.anchorMax = new Vector2(0.9f, 0.9f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;
        iconImage.color = Color.white;

        // Создаем текст количества (TextMeshPro)
        GameObject textObj = new GameObject("QuantityText");
        textObj.transform.SetParent(slotObj.transform, false);

        TextMeshProUGUI quantityText = textObj.AddComponent<TextMeshProUGUI>();
        quantityText.fontSize = 14;
        quantityText.fontStyle = FontStyles.Bold;
        quantityText.color = Color.white;
        quantityText.alignment = TextAlignmentOptions.BottomRight;

        // Включаем автоматический размер
        quantityText.enableAutoSizing = false;

        // Включаем Outline для лучшей читаемости
        quantityText.outlineWidth = 0.2f;
        quantityText.outlineColor = Color.black;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 0.3f);
        textRect.offsetMin = new Vector2(2, 2);
        textRect.offsetMax = new Vector2(-2, -2);

        // Добавляем скрипт ResourceSlotUI
        ResourceSlotUI slotUI = slotObj.AddComponent<ResourceSlotUI>();
        slotUI.iconImage = iconImage;
        slotUI.quantityText = quantityText;
        slotUI.backgroundImage = backgroundImage;

        // Сохраняем как префаб
        PrefabUtility.SaveAsPrefabAsset(slotObj, RESOURCE_SLOT_PREFAB_PATH);

        // Удаляем временный объект
        Object.DestroyImmediate(slotObj);

        Debug.Log($"Resource Slot Prefab created at: {RESOURCE_SLOT_PREFAB_PATH}");

        // Выделяем префаб
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RESOURCE_SLOT_PREFAB_PATH);
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = prefab;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// Настроить ResourcePanelUI на выделенном объекте
    /// </summary>
    [MenuItem("Tools/Resources/Setup Resource Panel on Selected")]
    public static void SetupResourcePanelOnSelected()
    {
        GameObject selectedObject = Selection.activeGameObject;
        if (selectedObject == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a GameObject in the Hierarchy first", "OK");
            return;
        }

        // Проверяем, есть ли уже компонент
        ResourcePanelUI existingPanel = selectedObject.GetComponent<ResourcePanelUI>();
        if (existingPanel != null)
        {
            Debug.LogWarning($"ResourcePanelUI already exists on {selectedObject.name}");
            return;
        }

        // Добавляем компонент
        ResourcePanelUI panel = selectedObject.AddComponent<ResourcePanelUI>();

        // Загружаем ResourceManager
        ResourceManager resourceManager = Resources.Load<ResourceManager>("ResourceManager");
        if (resourceManager != null)
        {
            panel.resourceManager = resourceManager;
            Debug.Log("ResourceManager assigned automatically");
        }
        else
        {
            Debug.LogWarning("ResourceManager not found. Create it via Tools/Resources/Create Resource Manager");
        }

        // Загружаем префаб слота
        GameObject slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RESOURCE_SLOT_PREFAB_PATH);
        if (slotPrefab != null)
        {
            panel.resourceSlotPrefab = slotPrefab;
            Debug.Log("Resource Slot Prefab assigned automatically");
        }
        else
        {
            Debug.LogWarning("Resource Slot Prefab not found. Create it via Tools/Resources/Create Resource Slot Prefab");
        }

        // Устанавливаем resourceSlotsParent на себя
        panel.resourceSlotsParent = selectedObject.transform;

        // Добавляем GridLayoutGroup для автоматической раскладки иконок
        GridLayoutGroup gridLayout = selectedObject.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = selectedObject.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(60, 60);
            gridLayout.spacing = new Vector2(5, 5);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 10; // 10 иконок в ряд
            gridLayout.childAlignment = TextAnchor.UpperLeft;
            Debug.Log("Added GridLayoutGroup for automatic icon layout");
        }

        Debug.Log($"ResourcePanelUI successfully set up on {selectedObject.name}");
        EditorUtility.SetDirty(selectedObject);
    }

    /// <summary>
    /// Найти Resource_Panel и настроить её
    /// </summary>
    [MenuItem("Tools/Resources/Find and Setup Resource Panel")]
    public static void FindAndSetupResourcePanel()
    {
        // Ищем все объекты с именем "Resource_Panel" в сцене
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        GameObject resourcePanel = null;

        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Resource") && obj.name.Contains("Panel"))
            {
                resourcePanel = obj;
                break;
            }
        }

        if (resourcePanel == null)
        {
            EditorUtility.DisplayDialog("Not Found",
                "Resource_Panel not found in scene. Please create it in Canvas_MainUI or select it manually and use 'Setup Resource Panel on Selected'",
                "OK");
            return;
        }

        Debug.Log($"Found Resource Panel: {resourcePanel.name}");
        Selection.activeGameObject = resourcePanel;
        SetupResourcePanelOnSelected();
    }
}
