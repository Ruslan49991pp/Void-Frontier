using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.IO;

/// <summary>
/// Editor скрипт для создания префаба BuildSlot
/// </summary>
public static class CreateBuildSlotPrefab
{
    [MenuItem("Tools/Create BuildSlot Prefab")]
    public static void CreatePrefab()
    {
        // Создаем основной объект
        GameObject buildSlot = new GameObject("BuildSlot");
        RectTransform rectTransform = buildSlot.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 80);

        // Добавляем Image компонент для фона
        Image bgImage = buildSlot.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.25f, 1f);

        // Добавляем Button компонент
        Button button = buildSlot.AddComponent<Button>();
        button.targetGraphic = bgImage;

        // Настраиваем визуальный переход кнопки
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.25f, 1f);
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.35f, 1f);
        colors.pressedColor = new Color(0.15f, 0.15f, 0.2f, 1f);
        colors.selectedColor = new Color(0.25f, 0.35f, 0.45f, 1f);
        button.colors = colors;

        // Добавляем BuildSlotUI компонент
        BuildSlotUI slotUI = buildSlot.AddComponent<BuildSlotUI>();

        // Создаем Icon (левая часть)
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(buildSlot.transform, false);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 0);
        iconRect.anchorMax = new Vector2(0, 1);
        iconRect.pivot = new Vector2(0, 0.5f);
        iconRect.anchoredPosition = new Vector2(5, 0);
        iconRect.sizeDelta = new Vector2(70, 70);

        Image iconImage = iconObj.AddComponent<Image>();
        iconImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        iconImage.enabled = false; // По умолчанию выключена

        // Создаем текстовую область (правая часть)
        GameObject textArea = new GameObject("TextArea");
        textArea.transform.SetParent(buildSlot.transform, false);
        RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
        textAreaRect.anchorMin = new Vector2(0, 0);
        textAreaRect.anchorMax = new Vector2(1, 1);
        textAreaRect.pivot = new Vector2(0, 0.5f);
        textAreaRect.anchoredPosition = new Vector2(80, 0);
        textAreaRect.sizeDelta = new Vector2(-85, -10);

        // Создаем NameText
        GameObject nameObj = new GameObject("NameText");
        nameObj.transform.SetParent(textArea.transform, false);
        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.6f);
        nameRect.anchorMax = new Vector2(1, 1);
        nameRect.pivot = new Vector2(0, 1);
        nameRect.anchoredPosition = Vector2.zero;
        nameRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = "Room Name";
        nameText.fontSize = 16;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = Color.white;
        nameText.alignment = TextAlignmentOptions.TopLeft;

        // Создаем InfoText (Cost + Size в одной строке)
        GameObject infoObj = new GameObject("InfoText");
        infoObj.transform.SetParent(textArea.transform, false);
        RectTransform infoRect = infoObj.AddComponent<RectTransform>();
        infoRect.anchorMin = new Vector2(0, 0);
        infoRect.anchorMax = new Vector2(1, 0.6f);
        infoRect.pivot = new Vector2(0, 0);
        infoRect.anchoredPosition = Vector2.zero;
        infoRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI infoText = infoObj.AddComponent<TextMeshProUGUI>();
        infoText.text = "Cost: $100 | Size: 4x10";
        infoText.fontSize = 12;
        infoText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        infoText.alignment = TextAlignmentOptions.TopLeft;

        // Присваиваем ссылки в BuildSlotUI
        slotUI.iconImage = iconImage;
        slotUI.nameText = nameText;
        slotUI.costText = infoText; // Используем infoText для отображения всей информации
        slotUI.button = button;

        // Создаем директорию для префабов если её нет
        string prefabPath = "Assets/Prefabs/UI";
        if (!Directory.Exists(prefabPath))
        {
            Directory.CreateDirectory(prefabPath);
        }

        // Сохраняем как префаб
        string fullPath = $"{prefabPath}/BuildSlot.prefab";
        PrefabUtility.SaveAsPrefabAsset(buildSlot, fullPath);

        Debug.Log($"[CreateBuildSlotPrefab] BuildSlot prefab created at: {fullPath}");

        // Удаляем временный объект из сцены
        Object.DestroyImmediate(buildSlot);

        // Выделяем созданный префаб в Project window
        Object prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);
        EditorGUIUtility.PingObject(prefab);
        Selection.activeObject = prefab;

        AssetDatabase.Refresh();
    }
}
