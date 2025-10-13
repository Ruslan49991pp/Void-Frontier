using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Компонент для отображения информации о выделенном враге в SelectionInfoPanel
/// </summary>
public class SelectionInfoDisplay : MonoBehaviour
{
    [Header("UI References")]
    public GameObject selectionInfoPanel; // Главная панель SelectionInfoPanel
    public GameObject enemyPortrait; // EnemyPortrait префаб внутри панели

    [Header("Enemy Info Elements")]
    public GameObject healthBarPlane; // HealthBar_Plane для отображения здоровья
    public TMP_Text nameLabel; // NameLabel для отображения имени
    public TMP_Text raceLabel; // RaceLabel для отображения расы
    public Image avatarImage; // Avatar изображение для портрета врага

    [Header("Settings")]
    public Color healthBarFullColor = Color.green; // Цвет полного здоровья
    public Color healthBarLowColor = Color.red; // Цвет низкого здоровья
    public float healthBarLowThreshold = 0.3f; // Порог для низкого здоровья

    // Текущий выделенный враг
    private Character currentEnemy;
    private SelectionManager selectionManager;

    // Время последнего клика для определения двойного клика
    private float lastClickTime = 0f;

    // Список портретов рептилоидов для циклического использования
    private string[] reptiloidPortraits = new string[]
    {
        "Icons/Characters/Reptiloids/Enemy_Ico",
        "Icons/Characters/Reptiloids/Enemy_2_ico",
        "Icons/Characters/Reptiloids/Enemy_3_ico"
    };

    // Словарь для запоминания портрета каждого врага
    private static System.Collections.Generic.Dictionary<int, int> enemyPortraitMapping =
        new System.Collections.Generic.Dictionary<int, int>();
    private static int nextReptiloidPortraitIndex = 0;

    void Awake()
    {
        Debug.Log("[SelectionInfoDisplay] Awake called");

        // Находим SelectionManager в сцене
        selectionManager = FindObjectOfType<SelectionManager>();
        Debug.Log($"[SelectionInfoDisplay] SelectionManager found: {selectionManager != null}");

        // Автоматически находим UI элементы если не назначены
        if (selectionInfoPanel == null)
        {
            selectionInfoPanel = gameObject;
            Debug.Log($"[SelectionInfoDisplay] Using self as selectionInfoPanel: {selectionInfoPanel.name}");
        }
        else
        {
            Debug.Log($"[SelectionInfoDisplay] selectionInfoPanel assigned: {selectionInfoPanel.name}");
        }

        // ИСПРАВЛЕНИЕ: Пытаемся найти EnemyPortrait правильно
        // Если enemyPortrait указывает на Avatar, ищем родительский EnemyPortrait
        if (enemyPortrait != null && enemyPortrait.name == "Avatar")
        {
            Debug.LogWarning($"[SelectionInfoDisplay] enemyPortrait points to Avatar, looking for parent EnemyPortrait");
            Transform parent = enemyPortrait.transform.parent;
            if (parent != null && parent.name == "EnemyPortrait")
            {
                enemyPortrait = parent.gameObject;
                Debug.Log($"[SelectionInfoDisplay] Fixed: enemyPortrait now points to {enemyPortrait.name}");
            }
        }

        // Пытаемся найти EnemyPortrait если не назначен
        if (enemyPortrait == null)
        {
            Transform enemyPortraitTransform = transform.Find("EnemyPortrait");
            if (enemyPortraitTransform != null)
            {
                enemyPortrait = enemyPortraitTransform.gameObject;
                Debug.Log($"[SelectionInfoDisplay] Found EnemyPortrait: {enemyPortrait.name}");
            }
            else
            {
                Debug.LogError("[SelectionInfoDisplay] EnemyPortrait not found!");
            }
        }
        else
        {
            Debug.Log($"[SelectionInfoDisplay] EnemyPortrait assigned: {enemyPortrait.name}");
        }

        // Автопоиск UI элементов внутри EnemyPortrait
        if (enemyPortrait != null)
        {
            if (healthBarPlane == null)
            {
                Transform healthBarTransform = FindTransformRecursive(enemyPortrait.transform, "HealthBar_Plane");
                Debug.Log($"[SelectionInfoDisplay] HealthBar_Plane found: {healthBarTransform != null}");
                if (healthBarTransform != null)
                {
                    healthBarPlane = healthBarTransform.gameObject;
                    Debug.Log($"[SelectionInfoDisplay] healthBarPlane assigned: {healthBarPlane.name}");
                }
                else
                {
                    Debug.LogWarning("[SelectionInfoDisplay] HealthBar_Plane not found!");
                }
            }

            if (nameLabel == null)
            {
                Transform nameLabelTransform = FindTransformRecursive(enemyPortrait.transform, "NameLabel");
                if (nameLabelTransform != null)
                {
                    nameLabel = nameLabelTransform.GetComponent<TMP_Text>();
                    Debug.Log($"[SelectionInfoDisplay] NameLabel found: {nameLabel != null}");
                }
            }

            if (raceLabel == null)
            {
                Transform raceLabelTransform = FindTransformRecursive(enemyPortrait.transform, "RaceLabel");
                if (raceLabelTransform != null)
                {
                    raceLabel = raceLabelTransform.GetComponent<TMP_Text>();
                    Debug.Log($"[SelectionInfoDisplay] RaceLabel found: {raceLabel != null}");
                }
            }

            if (avatarImage == null)
            {
                Transform avatarTransform = FindTransformRecursive(enemyPortrait.transform, "Avatar");
                if (avatarTransform != null)
                {
                    Transform imageTransform = avatarTransform.Find("Image");
                    if (imageTransform != null)
                    {
                        avatarImage = imageTransform.GetComponent<Image>();
                        Debug.Log($"[SelectionInfoDisplay] Avatar Image found: {avatarImage != null}");
                    }
                }
            }
        }
        else
        {
            Debug.LogError("[SelectionInfoDisplay] enemyPortrait is NULL in Awake!");
        }

        // Настраиваем кнопку для двойного клика на портрете
        SetupPortraitButton();

        // Скрываем панель по умолчанию
        HidePanel();
    }

    void Start()
    {
        Debug.Log("[SelectionInfoDisplay] Start called");

        // Подписываемся на события выделения
        if (selectionManager != null)
        {
            Debug.Log("[SelectionInfoDisplay] Subscribing to OnSelectionChanged event");
            selectionManager.OnSelectionChanged += OnSelectionChanged;
            Debug.Log("[SelectionInfoDisplay] Successfully subscribed to OnSelectionChanged");
        }
        else
        {
            Debug.LogError("[SelectionInfoDisplay] Cannot subscribe - selectionManager is NULL!");
        }
    }

    void Update()
    {
        // Обновляем здоровье врага каждый кадр если враг выделен
        // Обновляем даже если враг мертв, чтобы показать 0 HP
        if (currentEnemy != null)
        {
            UpdateHealthBar(currentEnemy.GetHealth(), currentEnemy.GetMaxHealth());
        }
    }

    void OnDestroy()
    {
        // Отписываемся от событий
        if (selectionManager != null)
        {
            selectionManager.OnSelectionChanged -= OnSelectionChanged;
        }

        // Отписываемся от событий врага
        if (currentEnemy != null)
        {
            UnsubscribeFromEnemy(currentEnemy);
        }
    }

    /// <summary>
    /// Обработчик изменения выделения
    /// </summary>
    void OnSelectionChanged(System.Collections.Generic.List<GameObject> selectedObjects)
    {
        Debug.Log($"[SelectionInfoDisplay] OnSelectionChanged called, count: {selectedObjects.Count}");

        // Отписываемся от текущего врага
        if (currentEnemy != null)
        {
            UnsubscribeFromEnemy(currentEnemy);
            currentEnemy = null;
        }

        // Проверяем, выделен ли один объект
        if (selectedObjects.Count == 1)
        {
            GameObject selectedObject = selectedObjects[0];
            Character character = selectedObject.GetComponent<Character>();

            Debug.Log($"[SelectionInfoDisplay] Selected object: {selectedObject.name}, has Character: {character != null}");

            // Проверяем, является ли выделенный объект врагом (не союзником)
            if (character != null && !character.IsPlayerCharacter())
            {
                // Это враг - показываем панель
                Debug.Log($"[SelectionInfoDisplay] Enemy detected: {character.GetFullName()}, showing panel");
                currentEnemy = character;
                SubscribeToEnemy(currentEnemy);
                UpdateEnemyInfo();
                ShowPanel();
                return;
            }
            else if (character != null)
            {
                Debug.Log($"[SelectionInfoDisplay] Not an enemy (ally), hiding panel");
            }
        }

        // Если не враг или ничего не выделено - скрываем панель
        Debug.Log($"[SelectionInfoDisplay] Hiding panel");
        HidePanel();
    }

    /// <summary>
    /// Подписка на события врага
    /// </summary>
    void SubscribeToEnemy(Character enemy)
    {
        // Character не имеет события OnHealthChanged
        // Будем обновлять здоровье в Update()
    }

    /// <summary>
    /// Отписка от событий врага
    /// </summary>
    void UnsubscribeFromEnemy(Character enemy)
    {
        // Character не имеет события OnHealthChanged
    }

    /// <summary>
    /// Обновить всю информацию о враге
    /// </summary>
    void UpdateEnemyInfo()
    {
        if (currentEnemy == null) return;

        // Обновляем имя
        if (nameLabel != null)
        {
            nameLabel.text = currentEnemy.GetFullName();
        }

        // Обновляем расу (пока все враги - рептилоиды)
        if (raceLabel != null)
        {
            raceLabel.text = GetRaceDisplayName(currentEnemy);
        }

        // Обновляем здоровье
        UpdateHealthBar(currentEnemy.GetHealth(), currentEnemy.GetMaxHealth());

        // Обновляем портрет (если есть иконка)
        UpdateAvatar();
    }

    /// <summary>
    /// Получить отображаемое имя расы
    /// </summary>
    string GetRaceDisplayName(Character character)
    {
        // Пока все враги - рептилоиды
        // В будущем можно добавить поле race в Character
        if (character != null && !character.IsPlayerCharacter())
        {
            return "Рептилоид";
        }

        return "Неизвестно";
    }

    /// <summary>
    /// Обновить полоску здоровья
    /// </summary>
    void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (healthBarPlane == null)
        {
            Debug.LogWarning("[SelectionInfoDisplay] UpdateHealthBar called but healthBarPlane is NULL!");
            return;
        }

        // Вычисляем процент здоровья
        float healthPercent = maxHealth > 0 ? currentHealth / maxHealth : 0f;

        // Если здоровье меньше или равно 0, устанавливаем процент точно в 0
        if (currentHealth <= 0f)
        {
            healthPercent = 0f;
        }
        else
        {
            healthPercent = Mathf.Clamp01(healthPercent);
        }

        // Получаем RectTransform и Image компоненты
        RectTransform rectTransform = healthBarPlane.GetComponent<RectTransform>();
        Image healthBarImage = healthBarPlane.GetComponent<Image>();

        if (rectTransform != null && healthBarImage != null)
        {
            // Изменяем scale.x чтобы полоска укорачивалась справа налево
            // При 0 HP scale.x будет точно 0
            Vector3 currentScale = rectTransform.localScale;
            rectTransform.localScale = new Vector3(healthPercent, currentScale.y, currentScale.z);

            // Меняем цвет в зависимости от процента здоровья
            Color newColor;
            if (healthPercent <= healthBarLowThreshold)
            {
                // Красный -> Желтый при низком здоровье
                newColor = Color.Lerp(healthBarLowColor, Color.yellow, healthPercent / healthBarLowThreshold);
            }
            else
            {
                // Желтый -> Зеленый при нормальном/высоком здоровье
                newColor = Color.Lerp(Color.yellow, healthBarFullColor, (healthPercent - healthBarLowThreshold) / (1f - healthBarLowThreshold));
            }

            healthBarImage.color = newColor;

            Debug.Log($"[SelectionInfoDisplay] UpdateHealthBar: health={currentHealth}/{maxHealth}, percent={healthPercent:F2}, scale.x={rectTransform.localScale.x}, color={newColor}");
        }
        else
        {
            Debug.LogWarning($"[SelectionInfoDisplay] Missing components on HealthBar_Plane! RectTransform: {rectTransform != null}, Image: {healthBarImage != null}");
        }
    }

    /// <summary>
    /// Обновить портрет/аватар врага
    /// </summary>
    void UpdateAvatar()
    {
        if (avatarImage == null || currentEnemy == null) return;

        // Получаем уникальный ID врага (используем GetInstanceID)
        int enemyID = currentEnemy.GetInstanceID();

        // Проверяем, есть ли у этого врага уже назначенный портрет
        if (!enemyPortraitMapping.ContainsKey(enemyID))
        {
            // Назначаем следующий портрет по кругу
            enemyPortraitMapping[enemyID] = nextReptiloidPortraitIndex;
            nextReptiloidPortraitIndex = (nextReptiloidPortraitIndex + 1) % reptiloidPortraits.Length;
        }

        // Получаем индекс портрета для этого врага
        int portraitIndex = enemyPortraitMapping[enemyID];
        string portraitPath = reptiloidPortraits[portraitIndex];

        // Загружаем Sprite из Resources
        Sprite enemyIcon = Resources.Load<Sprite>(portraitPath);

        if (enemyIcon != null)
        {
            avatarImage.sprite = enemyIcon;
            avatarImage.enabled = true;
            Debug.Log($"[SelectionInfoDisplay] Loaded portrait '{portraitPath}' for enemy {currentEnemy.GetFullName()}");
        }
        else
        {
            avatarImage.enabled = false;
            Debug.LogWarning($"[SelectionInfoDisplay] Failed to load portrait from '{portraitPath}' for enemy {currentEnemy.GetFullName()}");
        }
    }

    /// <summary>
    /// Показать панель
    /// </summary>
    void ShowPanel()
    {
        // Показываем весь SelectionInfoPanel
        if (selectionInfoPanel != null)
        {
            Debug.Log($"[SelectionInfoDisplay] ShowPanel called, activating {selectionInfoPanel.name}");
            selectionInfoPanel.SetActive(true);
            Debug.Log($"[SelectionInfoDisplay] Panel active state: {selectionInfoPanel.activeSelf}");
        }
        else
        {
            Debug.LogError("[SelectionInfoDisplay] selectionInfoPanel is NULL!");
        }
    }

    /// <summary>
    /// Скрыть панель
    /// </summary>
    void HidePanel()
    {
        // Очищаем ссылку на врага чтобы остановить Update()
        currentEnemy = null;

        // Скрываем весь SelectionInfoPanel
        if (selectionInfoPanel != null)
        {
            Debug.Log($"[SelectionInfoDisplay] HidePanel called, deactivating {selectionInfoPanel.name}");
            selectionInfoPanel.SetActive(false);
            Debug.Log($"[SelectionInfoDisplay] Panel active state after hiding: {selectionInfoPanel.activeSelf}");
        }
        else
        {
            Debug.LogError("[SelectionInfoDisplay] selectionInfoPanel is NULL in HidePanel!");
        }
    }

    /// <summary>
    /// Настройка кнопки на портрете для двойного клика
    /// </summary>
    void SetupPortraitButton()
    {
        if (enemyPortrait == null) return;

        // Проверяем, есть ли Image компонент (нужен для raycast)
        Image portraitImage = enemyPortrait.GetComponent<Image>();
        if (portraitImage == null)
        {
            // Добавляем невидимый Image для raycast
            portraitImage = enemyPortrait.AddComponent<Image>();
            portraitImage.color = new Color(0, 0, 0, 0); // Прозрачный
        }
        portraitImage.raycastTarget = true;

        // Добавляем или получаем Button компонент
        Button portraitButton = enemyPortrait.GetComponent<Button>();
        if (portraitButton == null)
        {
            portraitButton = enemyPortrait.AddComponent<Button>();
        }

        // Настраиваем кнопку
        portraitButton.targetGraphic = portraitImage;
        portraitButton.transition = Selectable.Transition.None;

        Navigation nav = portraitButton.navigation;
        nav.mode = Navigation.Mode.None;
        portraitButton.navigation = nav;

        // Добавляем обработчик клика
        portraitButton.onClick.AddListener(OnPortraitClicked);

        Debug.Log("[SelectionInfoDisplay] Portrait button setup completed");
    }

    /// <summary>
    /// Обработчик клика по портрету врага
    /// </summary>
    void OnPortraitClicked()
    {
        if (currentEnemy == null) return;

        float currentTime = Time.time;
        bool isDoubleClick = (currentTime - lastClickTime < 0.5f);
        lastClickTime = currentTime;

        Debug.Log($"[SelectionInfoDisplay] Portrait clicked, isDoubleClick: {isDoubleClick}");

        // Двойной клик - фокус камеры на враге
        if (isDoubleClick)
        {
            CameraController cameraController = FindObjectOfType<CameraController>();
            if (cameraController != null)
            {
                cameraController.SetFocusTarget(currentEnemy.transform);
                cameraController.CenterOnTarget();
                Debug.Log($"[SelectionInfoDisplay] Camera focused on enemy: {currentEnemy.GetFullName()}");
            }
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
