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

            // Проверяем, НЕ является ли это персонажем или врагом
            Character character = selectedObject.GetComponent<Character>();
            if (character != null)
            {
                // Это персонаж или враг - не показываем панель объектов
                HidePanel();
                return;
            }

            // Проверяем наличие LocationObjectInfo или RoomInfo
            LocationObjectInfo locationInfo = selectedObject.GetComponent<LocationObjectInfo>();
            RoomInfo roomInfo = selectedObject.GetComponent<RoomInfo>();

            // Если есть информация об объекте - показываем панель
            if (locationInfo != null || roomInfo != null)
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

        string infoText = "";

        // Проверяем RoomInfo
        RoomInfo roomInfo = currentObject.GetComponent<RoomInfo>();
        if (roomInfo != null)
        {
            infoText = GetRoomInfoText(roomInfo);
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

        // Выводим информацию в TextObjectInfo
        textObjectInfo.text = infoText;
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

        // Дополнительные свойства
        if (locationInfo.isDestructible)
        {
            text += "\nDestructible";
        }

        if (locationInfo.canBeScavenged)
        {
            text += "\nSalvageable";
        }

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
