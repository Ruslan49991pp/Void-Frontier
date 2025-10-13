using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// Editor скрипт для создания EquipmentPanel в Canvas_MainUI/Windows/Inventory
/// </summary>
public class CreateEquipmentPanelEditor : EditorWindow
{
    [MenuItem("Tools/Create Equipment Panel")]
    static void CreateEquipmentPanel()
    {
        // Находим Canvas_MainUI
        GameObject canvasMainUI = GameObject.Find("Canvas_MainUI");
        if (canvasMainUI == null)
        {
            Debug.LogError("Canvas_MainUI not found!");
            return;
        }

        // Находим Windows
        Transform windowsTransform = canvasMainUI.transform.Find("Windows");
        if (windowsTransform == null)
        {
            Debug.LogError("Windows not found in Canvas_MainUI!");
            return;
        }

        // Находим Inventory
        Transform inventoryTransform = windowsTransform.Find("Inventory");
        if (inventoryTransform == null)
        {
            Debug.LogError("Inventory not found in Windows!");
            return;
        }

        // Проверяем, не существует ли уже EquipmentPanel
        Transform existingEquipmentPanel = inventoryTransform.Find("EquipmentPanel");
        if (existingEquipmentPanel != null)
        {
            Debug.LogWarning("EquipmentPanel already exists! Deleting old one...");
            DestroyImmediate(existingEquipmentPanel.gameObject);
        }

        // Создаём EquipmentPanel
        GameObject equipmentPanel = new GameObject("EquipmentPanel");
        equipmentPanel.transform.SetParent(inventoryTransform, false);

        RectTransform equipmentRect = equipmentPanel.AddComponent<RectTransform>();
        equipmentRect.anchorMin = new Vector2(0.6f, 0.15f);  // Справа
        equipmentRect.anchorMax = new Vector2(0.95f, 0.85f);
        equipmentRect.anchoredPosition = Vector2.zero;
        equipmentRect.sizeDelta = Vector2.zero;

        // Добавляем фоновое изображение (опционально, для отладки)
        // Image bgImage = equipmentPanel.AddComponent<Image>();
        // bgImage.color = new Color(0.2f, 0.2f, 0.3f, 0.3f);
        // bgImage.raycastTarget = false;

        // Создаём слоты экипировки
        CreateEquipmentSlot(equipmentPanel, "Slot_Head", new Vector2(0.5f, 0.9f));       // Голова
        CreateEquipmentSlot(equipmentPanel, "Slot_Chest", new Vector2(0.5f, 0.7f));      // Грудь
        CreateEquipmentSlot(equipmentPanel, "Slot_LeftHand", new Vector2(0.15f, 0.55f)); // Левая рука
        CreateEquipmentSlot(equipmentPanel, "Slot_RightHand", new Vector2(0.85f, 0.55f));// Правая рука
        CreateEquipmentSlot(equipmentPanel, "Slot_Legs", new Vector2(0.5f, 0.45f));      // Ноги
        CreateEquipmentSlot(equipmentPanel, "Slot_Feet", new Vector2(0.5f, 0.15f));      // Ступни

        Debug.Log("EquipmentPanel created successfully with 6 equipment slots!");

        // Помечаем сцену как изменённую
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );
    }

    static void CreateEquipmentSlot(GameObject parent, string slotName, Vector2 normalizedPosition)
    {
        GameObject slot = new GameObject(slotName);
        slot.transform.SetParent(parent.transform, false);
        slot.layer = 5; // UI layer

        RectTransform slotRect = slot.AddComponent<RectTransform>();
        slotRect.pivot = new Vector2(0.5f, 0.5f);
        slotRect.anchorMin = normalizedPosition;
        slotRect.anchorMax = normalizedPosition;
        slotRect.anchoredPosition = Vector2.zero;
        slotRect.sizeDelta = new Vector2(80, 80); // Размер слота

        // Добавляем Image компонент для фона слота
        Image slotImage = slot.AddComponent<Image>();
        slotImage.color = new Color(0.3f, 0.3f, 0.35f, 0.9f);
        slotImage.raycastTarget = true;

        // Создаём Icon (дочерний объект для отображения иконки предмета)
        GameObject icon = new GameObject("Icon");
        icon.transform.SetParent(slot.transform, false);
        icon.layer = 5;

        RectTransform iconRect = icon.AddComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(5, 5);
        iconRect.offsetMax = new Vector2(-5, -5);

        Image iconImage = icon.AddComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        iconImage.enabled = false; // Скрыто пока нет предмета

        Debug.Log($"Created equipment slot: {slotName} at position {normalizedPosition}");
    }
}
