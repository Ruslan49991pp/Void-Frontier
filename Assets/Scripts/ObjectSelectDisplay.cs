using UnityEngine;
using TMPro;

/// <summary>
/// Компонент для отображения информации о выделенном объекте (стена, комната, модуль) в панели ObjectSelect
/// </summary>
public class ObjectSelectDisplay : MonoBehaviour
{
    [Header("UI References")]
    public GameObject objectSelectPanel; // Главная панель ObjectSelect
    public TMP_Text textObjectInfo; // TextObjectInfo для отображения информации

    // Текущий выделенный объект
    private GameObject currentObject;
    private SelectionManager selectionManager;

    void Awake()
    {
        // Автоматически находим UI элементы если не назначены
        if (objectSelectPanel == null)
        {
            objectSelectPanel = gameObject;
        }

        // Пытаемся найти TextObjectInfo если не назначен
        if (textObjectInfo == null)
        {
            Transform textTransform = FindTransformRecursive(transform, "TextObjectInfo");
            if (textTransform != null)
            {
                textObjectInfo = textTransform.GetComponent<TMP_Text>();
            }
        }

        // НЕ скрываем панель здесь, иначе OnEnable() не вызовется
        // Только очищаем текст
        if (textObjectInfo != null)
        {
            textObjectInfo.text = "";
        }
    }

    private bool isSubscribed = false; // Флаг подписки на события

    void Start()
    {
        // Находим SelectionManager в сцене если еще не нашли
        if (selectionManager == null)
        {
            selectionManager = FindObjectOfType<SelectionManager>();
        }

        // Подписываемся на события выделения ОДИН РАЗ
        if (selectionManager != null && !isSubscribed)
        {
            selectionManager.OnSelectionChanged += OnSelectionChanged;
            isSubscribed = true;
        }

        // Скрываем панель после инициализации
        if (objectSelectPanel != null)
        {
            objectSelectPanel.SetActive(false);
        }
    }

    void OnDestroy()
    {
        // Отписываемся от событий только при уничтожении компонента
        if (selectionManager != null && isSubscribed)
        {
            selectionManager.OnSelectionChanged -= OnSelectionChanged;
            isSubscribed = false;
        }
    }

    /// <summary>
    /// Обработчик изменения выделения
    /// </summary>
    void OnSelectionChanged(System.Collections.Generic.List<GameObject> selectedObjects)
    {
        // Сбрасываем текущий объект
        currentObject = null;

        // Проверяем, выделен ли один объект
        if (selectedObjects.Count == 1)
        {
            GameObject selectedObject = selectedObjects[0];

            // КРИТИЧЕСКИ ВАЖНО: Проверяем что объект не был уничтожен
            if (ReferenceEquals(selectedObject, null) || selectedObject == null)
            {
                Debug.Log($"[ObjectSelectDisplay] [OnSelectionChanged] Selected object is destroyed, hiding panel");
                HidePanel();
                return;
            }

            // Проверяем, НЕ является ли это персонажем или врагом
            Character character = selectedObject.GetComponent<Character>();
            if (character != null)
            {
                // Это персонаж или враг - не показываем панель объектов
                HidePanel();
                return;
            }

            // Проверяем наличие LocationObjectInfo, RoomInfo или Item
            LocationObjectInfo locationInfo = selectedObject.GetComponent<LocationObjectInfo>();
            RoomInfo roomInfo = selectedObject.GetComponent<RoomInfo>();
            Item item = null;

            // ЗАЩИТА: Безопасно получаем Item компонент
            try
            {
                item = selectedObject.GetComponent<Item>();
                if (item != null && ReferenceEquals(item, null))
                {
                    Debug.Log($"[ObjectSelectDisplay] [OnSelectionChanged] Item component is destroyed, treating as null");
                    item = null;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ObjectSelectDisplay] [OnSelectionChanged] Exception getting Item component: {ex.Message}");
                item = null;
            }

            // Если есть информация об объекте - показываем панель
            if (locationInfo != null || roomInfo != null || item != null)
            {
                currentObject = selectedObject;
                UpdateObjectInfo();
                ShowPanel();
                return;
            }
        }

        // Если ничего не выделено или выделено несколько объектов - скрываем панель
        HidePanel();
    }

    /// <summary>
    /// Обновить всю информацию об объекте
    /// </summary>
    void UpdateObjectInfo()
    {
        if (currentObject == null || textObjectInfo == null)
        {
            return;
        }

        // КРИТИЧЕСКИ ВАЖНО: Проверяем что currentObject не был уничтожен
        if (ReferenceEquals(currentObject, null))
        {
            Debug.Log($"[ObjectSelectDisplay] [UpdateObjectInfo] currentObject is destroyed, clearing and hiding panel");
            HidePanel();
            return;
        }

        string infoText = "";

        try
        {
            // Проверяем RoomInfo
            RoomInfo roomInfo = currentObject.GetComponent<RoomInfo>();
            if (roomInfo != null)
            {
                infoText = GetRoomInfoText(roomInfo);
            }
            else
            {
                // Проверяем Item
                Item item = null;
                try
                {
                    item = currentObject.GetComponent<Item>();
                    // Дополнительная проверка что Item не уничтожен
                    if (item != null && ReferenceEquals(item, null))
                    {
                        Debug.Log($"[ObjectSelectDisplay] [UpdateObjectInfo] Item component is destroyed, treating as null");
                        item = null;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[ObjectSelectDisplay] [UpdateObjectInfo] Exception getting Item component: {ex.Message}");
                    item = null;
                }

                if (item != null && item.itemData != null)
                {
                    infoText = GetItemInfoText(item);
                }
                else
                {
                    // Проверяем LocationObjectInfo
                    LocationObjectInfo locationInfo = currentObject.GetComponent<LocationObjectInfo>();
                    if (locationInfo != null)
                    {
                        infoText = GetLocationInfoText(locationInfo);
                    }
                }
            }

            // Выводим информацию в TextObjectInfo
            textObjectInfo.text = infoText;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ObjectSelectDisplay] [UpdateObjectInfo] Exception: {ex.Message}");
            Debug.LogError($"[ObjectSelectDisplay] Stack trace: {ex.StackTrace}");
            HidePanel();
        }
    }

    /// <summary>
    /// Получить текст информации о комнате
    /// </summary>
    string GetRoomInfoText(RoomInfo roomInfo)
    {
        string text = "";

        // Имя комнаты
        if (!string.IsNullOrEmpty(roomInfo.roomName))
        {
            text += $"Room: {roomInfo.roomName}\n";
        }

        // Тип комнаты
        if (!string.IsNullOrEmpty(roomInfo.roomType))
        {
            text += $"Type: {roomInfo.roomType}\n";
        }

        // Размер
        text += $"Size: {roomInfo.roomSize.x}x{roomInfo.roomSize.y}\n";

        // Позиция
        text += $"Position: ({roomInfo.gridPosition.x}, {roomInfo.gridPosition.y})\n";

        // Здоровье стен
        text += $"Wall Health: {roomInfo.currentWallHealth:F0}/{roomInfo.maxWallHealth:F0}";

        // Главный объект
        if (roomInfo.mainObject != null)
        {
            text += $"\n\nMain Object: {roomInfo.mainObject.objectName}";
        }
        else
        {
            text += "\n\nCan install main object";
        }

        return text;
    }

    /// <summary>
    /// Получить текст информации об объекте локации
    /// </summary>
    string GetLocationInfoText(LocationObjectInfo locationInfo)
    {
        string text = "";

        Debug.Log($"[ObjectSelectDisplay] Getting info for object: {locationInfo.objectName}, Type: {locationInfo.objectType}");

        // Имя объекта
        if (!string.IsNullOrEmpty(locationInfo.objectName))
        {
            text += $"{locationInfo.objectName}\n";
        }

        // Тип объекта
        if (!string.IsNullOrEmpty(locationInfo.objectType))
        {
            text += $"Type: {locationInfo.objectType}\n";
        }

        // Здоровье
        text += $"Health: {locationInfo.health:F0} HP";

        // Металл для астероидов
        Debug.Log($"[ObjectSelectDisplay] Checking asteroid metal: IsOfType('Asteroid')={locationInfo.IsOfType("Asteroid")}, maxMetalAmount={locationInfo.maxMetalAmount}");

        if (locationInfo.IsOfType("Asteroid") && locationInfo.maxMetalAmount > 0)
        {
            text += $"\nMetal: {locationInfo.metalAmount}/{locationInfo.maxMetalAmount}";
            Debug.Log($"[ObjectSelectDisplay] Added metal info to text: {locationInfo.metalAmount}/{locationInfo.maxMetalAmount}");

            if (locationInfo.metalAmount > 0)
            {
                text += "\n<color=yellow>Right-click to mine</color>";
            }
            else
            {
                text += "\n<color=red>Depleted</color>";
            }
        }
        else
        {
            Debug.Log($"[ObjectSelectDisplay] NOT showing metal info - not an asteroid or no metal");
        }

        // Дополнительные свойства
        if (locationInfo.isDestructible)
        {
            text += "\nDestructible";
        }

        if (locationInfo.canBeScavenged && !locationInfo.IsOfType("Asteroid"))
        {
            text += "\nSalvageable";
        }

        Debug.Log($"[ObjectSelectDisplay] Final text:\n{text}");
        return text;
    }

    /// <summary>
    /// Получить текст информации о предмете
    /// </summary>
    string GetItemInfoText(Item item)
    {
        string text = "";
        ItemData itemData = item.itemData;

        // Имя предмета
        if (!string.IsNullOrEmpty(itemData.itemName))
        {
            text += $"ITEM: {itemData.itemName}\n";
        }

        // Тип и редкость
        text += $"Type: {itemData.itemType}\n";
        text += $"Rarity: {itemData.rarity}\n";

        // Описание
        if (!string.IsNullOrEmpty(itemData.description))
        {
            text += $"\n{itemData.description}\n";
        }

        // Характеристики оружия
        if (itemData.damage > 0)
            text += $"\nDamage: {itemData.damage}";

        // Характеристики брони
        if (itemData.armor > 0)
            text += $"\nArmor: {itemData.armor}";

        // Лечение
        if (itemData.healing > 0)
            text += $"\nHealing: {itemData.healing}";

        // Слот экипировки
        if (itemData.equipmentSlot != EquipmentSlot.None)
            text += $"\nSlot: {itemData.GetEquipmentSlotName()}";

        // Вес и ценность
        text += $"\nWeight: {itemData.weight}";
        text += $"\nValue: {itemData.value}";

        // Стек
        if (itemData.maxStackSize > 1)
            text += $"\nMax Stack: {itemData.maxStackSize}";

        return text;
    }

    /// <summary>
    /// Показать панель
    /// </summary>
    void ShowPanel()
    {
        if (objectSelectPanel != null)
        {
            objectSelectPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Скрыть панель
    /// </summary>
    void HidePanel()
    {
        // Очищаем ссылку на объект
        currentObject = null;

        // Скрываем панель
        if (objectSelectPanel != null)
        {
            objectSelectPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Рекурсивный поиск Transform по имени
    /// </summary>
    Transform FindTransformRecursive(Transform parent, string name)
    {
        if (parent.name == name)
            return parent;

        foreach (Transform child in parent)
        {
            Transform result = FindTransformRecursive(child, name);
            if (result != null)
                return result;
        }

        return null;
    }
}
