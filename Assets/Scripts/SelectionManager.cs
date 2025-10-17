using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectionManager : MonoBehaviour
{
    [Header("Selection Settings")]
    public Camera playerCamera;
    public LayerMask selectableLayerMask = -1;
    public Color selectionBoxColor = new Color(0.8f, 0.8f, 1f, 0.25f);
    public Color selectionBoxBorderColor = new Color(0.8f, 0.8f, 1f, 1f);
    public float selectionIndicatorHeight = 2f;

    [Header("Item Pickup Settings")]
    [Tooltip("Включить подбор предметов при клике ПКМ на ресурс")]
    public bool enableRightClickPickup = true;

    [Header("Hover Highlight")]
    public Color hoverColor = Color.cyan;
    
    [Header("Visual Indicators")]
    public GameObject selectionIndicatorPrefab;
    public Color selectionIndicatorColor = Color.yellow;
    
    [Header("UI")]
    public RectTransform selectionInfoPanel;
    public Text selectionInfoText;
    public Canvas uiCanvas;
    
    // Внутренние переменные
    private List<GameObject> selectedObjects = new List<GameObject>();
    private Dictionary<GameObject, GameObject> selectionIndicators = new Dictionary<GameObject, GameObject>();
    
    // Переменные для box selection и кликов
    private bool isBoxSelecting = false;
    private bool isMousePressed = false;
    private Vector3 boxStartPosition;
    private Vector3 boxEndPosition;
    private Vector3 mouseDownPosition;
    private float clickThreshold = 5f; // Пикселей для определения клика vs рамки

    // UI для рамки выделения
    private GameObject selectionBoxUI;
    private Image selectionBoxImage;
    
    // События
    public System.Action<List<GameObject>> OnSelectionChanged;

    // Флаг для предотвращения обработки клика другими системами
    private bool rightClickHandledThisFrame = false;

    // Hover система
    private GameObject currentHoveredObject = null;
    private Dictionary<MeshRenderer, Material> originalMaterials = new Dictionary<MeshRenderer, Material>();
    private Dictionary<MeshRenderer, Material> hoverMaterials = new Dictionary<MeshRenderer, Material>();
    private List<MeshRenderer> currentHighlightedRenderers = new List<MeshRenderer>();

    // Публичные свойства
    public bool IsBoxSelecting => isBoxSelecting;
    public bool RightClickHandledThisFrame => rightClickHandledThisFrame;
    
    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;



        InitializeUI();
        CreateSelectionIndicatorPrefab();

        // Отложенная диагностика (даем время объектам создаться)
        Invoke("DiagnoseSelectableObjects", 1f);
    }
    
    void Update()
    {
        // Сбрасываем флаг в начале каждого кадра
        rightClickHandledThisFrame = false;

        // Блокируем ввод если открыт инвентарь или меню паузы
        if (!InventoryUI.IsAnyInventoryOpen && !IsGamePaused())
        {
            HandleMouseInput();
            UpdateSelectionBox();
            HandleHover();
        }

        UpdateSelectionIndicatorPositions();
    }

    /// <summary>
    /// Проверить находится ли игра на паузе
    /// </summary>
    bool IsGamePaused()
    {
        return GamePauseManager.Instance != null && GamePauseManager.Instance.IsPaused();
    }
    
    /// <summary>
    /// Инициализация UI элементов
    /// </summary>
    void InitializeUI()
    {
        if (uiCanvas == null)
            uiCanvas = FindObjectOfType<Canvas>();
            
        if (uiCanvas == null)
        {
            GameObject canvasGO = new GameObject("SelectionCanvas");
            uiCanvas = canvasGO.AddComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        
        // Убрали панель информации о выделении сверху слева - теперь будет только снизу

        // Создаем UI элемент для рамки выделения
        GameObject boxGO = new GameObject("SelectionBox");
        boxGO.transform.SetParent(uiCanvas.transform, false);

        selectionBoxUI = boxGO;
        selectionBoxImage = boxGO.AddComponent<Image>();
        selectionBoxImage.color = selectionBoxColor;

        RectTransform boxRect = boxGO.GetComponent<RectTransform>();
        boxRect.anchorMin = Vector2.zero;
        boxRect.anchorMax = Vector2.zero;
        boxRect.pivot = Vector2.zero;

        selectionBoxUI.SetActive(false);
    }
    
    /// <summary>
    /// Создание префаба индикатора выделения если его нет
    /// </summary>
    void CreateSelectionIndicatorPrefab()
    {
        if (selectionIndicatorPrefab == null)
        {
            GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            prefab.name = "SelectionIndicator";
            prefab.transform.localScale = Vector3.one * 0.6f;
            
            // Убираем коллайдер
            DestroyImmediate(prefab.GetComponent<Collider>());
            
            // Настраиваем материал
            Renderer renderer = prefab.GetComponent<Renderer>();
            Material material = new Material(Shader.Find("Standard"));
            material.color = selectionIndicatorColor;
            material.SetFloat("_Mode", 3); // Transparent mode
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            renderer.material = material;
            
            prefab.SetActive(false);
            selectionIndicatorPrefab = prefab;
        }
    }
    
    /// <summary>
    /// Обработка ввода мыши
    /// </summary>
    void HandleMouseInput()
    {
        // Нажатие ЛКМ - запоминаем позицию
        if (Input.GetMouseButtonDown(0))
        {

            isMousePressed = true;
            mouseDownPosition = Input.mousePosition;
            boxStartPosition = Input.mousePosition;
        }

        // Движение мыши при зажатой ЛКМ
        if (isMousePressed && Input.GetMouseButton(0))
        {
            boxEndPosition = Input.mousePosition;
            float distance = Vector3.Distance(mouseDownPosition, Input.mousePosition);

            // Начинаем box selection только если мышь двинулась достаточно далеко
            if (distance > clickThreshold && !isBoxSelecting)
            {
                // Проверяем, есть ли под курсором юниты (персонажи)
                if (HasCharactersInArea())
                {
                    isBoxSelecting = true;
                    selectionBoxUI.SetActive(true);
                }
            }
        }

        // Отпускание ЛКМ
        if (Input.GetMouseButtonUp(0) && isMousePressed)
        {
            // Проверяем, был ли клик по UI элементу
            bool isPointerOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                                   UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

            if (isBoxSelecting)
            {

                // Выполняем box selection только для юнитов
                PerformBoxSelection();
                isBoxSelecting = false;
                selectionBoxUI.SetActive(false);
            }
            else if (!isPointerOverUI)
            {
                // Обычное клик-выделение - только если НЕ кликнули по UI
                PerformClickSelection(mouseDownPosition);
            }
            isMousePressed = false;
        }

        // ПКМ - взаимодействие с предметами (подбор ресурсов)
        if (Input.GetMouseButtonDown(1))
        {
            bool isPointerOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                                   UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

            if (!isPointerOverUI)
            {
                HandleRightClick();
            }
        }
    }
    
    /// <summary>
    /// Проверка наличия юнитов (персонажей) в области
    /// </summary>
    bool HasCharactersInArea()
    {
        // Простое определение - возвращаем true если в сцене есть Character'ы
        Character[] characters = FindObjectsOfType<Character>();
        return characters.Length > 0;
    }

    /// <summary>
    /// Обновление визуальной рамки выделения
    /// </summary>
    void UpdateSelectionBox()
    {
        if (!isBoxSelecting || selectionBoxUI == null) return;

        Vector2 start = boxStartPosition;
        Vector2 end = boxEndPosition;

        Vector2 min = Vector2.Min(start, end);
        Vector2 max = Vector2.Max(start, end);

        RectTransform boxRect = selectionBoxUI.GetComponent<RectTransform>();
        boxRect.anchoredPosition = min;
        boxRect.sizeDelta = max - min;
    }

    /// <summary>
    /// Выполнение box selection только для юнитов
    /// </summary>
    void PerformBoxSelection()
    {
        Vector2 start = playerCamera.ScreenToWorldPoint(boxStartPosition);
        Vector2 end = playerCamera.ScreenToWorldPoint(boxEndPosition);

        Vector2 min = Vector2.Min(start, end);
        Vector2 max = Vector2.Max(start, end);

        List<GameObject> newSelections = new List<GameObject>();

        // Находим только Character'ов в области
        Character[] allCharacters = FindObjectsOfType<Character>();

        foreach (Character character in allCharacters)
        {
            // Рамкой можно выделять только союзников
            if (!character.IsPlayerCharacter())
            {
                continue;
            }

            Vector3 worldPos = character.transform.position;
            Vector2 screenPos = playerCamera.WorldToScreenPoint(worldPos);

            Vector2 boxMin = Vector2.Min(boxStartPosition, boxEndPosition);
            Vector2 boxMax = Vector2.Max(boxStartPosition, boxEndPosition);

            if (screenPos.x >= boxMin.x && screenPos.x <= boxMax.x &&
                screenPos.y >= boxMin.y && screenPos.y <= boxMax.y)
            {
                newSelections.Add(character.gameObject);
            }
        }

        // Логика выделения
        if (newSelections.Count > 0)
        {
            // Есть юниты в рамке
            if (!Input.GetKey(KeyCode.LeftControl))
            {
                // Если Ctrl не зажат, заменяем выделение
                ClearSelection();
            }

            // Добавляем новых юнитов
            foreach (GameObject obj in newSelections)
            {
                if (!selectedObjects.Contains(obj))
                {
                    AddToSelection(obj);
                }
            }
        }
        else
        {
            // В рамку ничего не попало
            if (!Input.GetKey(KeyCode.LeftControl))
            {
                ClearSelection();
            }
        }

        UpdateSelectionInfo();
    }

    /// <summary>
    /// Выполнение выделения по клику (для зданий и модулей)
    /// </summary>
    void PerformClickSelection(Vector3 mousePosition)
    {
        Ray ray = playerCamera.ScreenPointToRay(mousePosition);

        // Используем RaycastAll чтобы получить все объекты на луче, включая триггеры
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, selectableLayerMask, QueryTriggerInteraction.Collide);

        if (hits.Length > 0)
        {
            // Ищем первый подходящий объект для выделения
            foreach (RaycastHit rayHit in hits)
            {
                GameObject hitObject = rayHit.collider.gameObject;

                // Исключаем системные объекты из выделения
                if (hitObject.name.Contains("Bounds") || hitObject.name.Contains("Grid") ||
                    hitObject.name.Contains("Location") && !hitObject.name.Contains("Test") ||
                    hitObject.name.Contains("Plane"))
                {
                    continue;
                }

                // Проверяем, является ли это персонажом
                Character character = hitObject.GetComponent<Character>();
                GameObject characterObject = hitObject;

                if (character == null)
                {
                    // Ищем компонент Character в родительских объектах
                    character = hitObject.GetComponentInParent<Character>();
                    if (character != null)
                    {
                        characterObject = character.gameObject;
                    }
                }

                if (character != null)
                {

                    // Для персонажей поддерживаем Ctrl+клик для множественного выделения только у союзников
                    // Врагов можно выделять только по одному для просмотра информации
                    if (character.IsPlayerCharacter())
                    {

                        // Союзников можно выделять группами
                        if (!Input.GetKey(KeyCode.LeftControl))
                        {
                            ClearSelection();
                        }
                    }
                    else
                    {
                        // Врагов всегда выделяем по одному (очищаем предыдущее выделение)
                        ClearSelection();
                    }


                    ToggleSelection(characterObject);
                    return;
                }

                // Проверяем, является ли это предметом инвентаря
                Item item = hitObject.GetComponent<Item>();
                if (item != null)
                {
                    // Предметы выделяем по одному
                    ClearSelection();
                    ToggleSelection(hitObject);
                    return;
                }

                LocationObjectInfo objectInfo = hitObject.GetComponent<LocationObjectInfo>();
                if (objectInfo != null)
                {
                    GameObject targetObject = hitObject;

                    // Проверяем, является ли это полом комнаты
                    RoomInfo roomInfo = hitObject.GetComponentInParent<RoomInfo>();
                    if (roomInfo != null)
                    {
                        // Если это пол комнаты, выделяем родительскую комнату
                        targetObject = roomInfo.gameObject;
                    }

                    // Найден подходящий объект для выделения
                    // Для зданий/модулей всегда выделяем только один объект
                    ClearSelection();
                    ToggleSelection(targetObject);
                    return;
                }
            }
        }

        // Если мы дошли до этого места, значит не найдено подходящих объектов
        // Попробуем исправить компоненты комнат если их не было найдено
        CheckAndFixRoomComponents();

        // Это считается кликом в пустое место - всегда очищаем выделение
        ClearSelection();
    }
    
    
    
    /// <summary>
    /// Переключение выделения объекта
    /// </summary>
    public void ToggleSelection(GameObject obj)
    {


        if (selectedObjects.Contains(obj))
        {

            RemoveFromSelection(obj);
        }
        else
        {

            AddToSelection(obj);
        }
    }
    
    /// <summary>
    /// Добавление объекта к выделению
    /// </summary>
    public void AddToSelection(GameObject obj)
    {
        if (!selectedObjects.Contains(obj))
        {
            // Убираем hover подсветку если объект был подсвечен
            if (currentHoveredObject == obj)
            {
                EndHover(obj);
            }

            // Очищаем любые сохраненные hover материалы для этого объекта
            ClearHoverMaterialsForObject(obj);

            selectedObjects.Add(obj);
            CreateSelectionIndicator(obj);
            SetObjectSelectionState(obj, true);
            UpdateSelectionInfo();
            OnSelectionChanged?.Invoke(selectedObjects);
        }
    }
    
    /// <summary>
    /// Удаление объекта из выделения
    /// </summary>
    public void RemoveFromSelection(GameObject obj)
    {
        if (selectedObjects.Remove(obj))
        {
            RemoveSelectionIndicator(obj);
            if (obj != null) // Проверяем перед обращением к объекту
            {
                SetObjectSelectionState(obj, false);

                // Если курсор находится над этим объектом, применяем hover подсветку
                if (currentHoveredObject == obj)
                {
                    StartHover(obj);
                }
            }
            UpdateSelectionInfo();
            OnSelectionChanged?.Invoke(selectedObjects);
        }
    }
    
    /// <summary>
    /// Очистка всего выделения
    /// </summary>
    public void ClearSelection()
    {
        // Создаем копию списка для безопасной итерации
        var objectsToProcess = new List<GameObject>(selectedObjects);

        foreach (GameObject obj in objectsToProcess)
        {
            if (obj != null) // Проверяем, что объект не был уничтожен
            {
                RemoveSelectionIndicator(obj);
                SetObjectSelectionState(obj, false);

                // Если курсор находится над этим объектом, применяем hover подсветку
                if (currentHoveredObject == obj)
                {
                    StartHover(obj);
                }
            }
            else
            {
                // Удаляем null-ссылки из словаря индикаторов
                if (selectionIndicators.ContainsKey(obj))
                {
                    selectionIndicators.Remove(obj);
                }
            }
        }

        selectedObjects.Clear();
        UpdateSelectionInfo();
        OnSelectionChanged?.Invoke(selectedObjects);
    }
    
    /// <summary>
    /// Создание визуального индикатора выделения
    /// </summary>
    void CreateSelectionIndicator(GameObject targetObject)
    {
        if (selectionIndicators.ContainsKey(targetObject))
            return;
            
        GameObject indicator = Instantiate(selectionIndicatorPrefab);
        indicator.SetActive(true);
        
        // Позиционируем над объектом
        Bounds bounds = GetObjectBounds(targetObject);
        Vector3 position = bounds.center + Vector3.up * (bounds.size.y * 0.5f + selectionIndicatorHeight);
        indicator.transform.position = position;
        
        selectionIndicators[targetObject] = indicator;
    }
    
    /// <summary>
    /// Удаление визуального индикатора выделения
    /// </summary>
    void RemoveSelectionIndicator(GameObject targetObject)
    {
        if (selectionIndicators.TryGetValue(targetObject, out GameObject indicator))
        {
            if (indicator != null)
            {
                try
                {
                    DestroyImmediate(indicator);
                }
                catch (System.Exception)
                {
                    // Error handled silently
                }
            }
            selectionIndicators.Remove(targetObject);
        }
    }

    /// <summary>
    /// Обновление позиций индикаторов выделения
    /// </summary>
    void UpdateSelectionIndicatorPositions()
    {
        // Создаем список уничтоженных объектов для удаления из словаря
        List<GameObject> destroyedObjects = new List<GameObject>();

        try
        {
            foreach (var kvp in selectionIndicators)
            {
                GameObject targetObject = kvp.Key;
                GameObject indicator = kvp.Value;

                // КРИТИЧЕСКИ ВАЖНО: Проверяем что объект не был уничтожен
                // Unity уничтоженные объекты != null, но ReferenceEquals(obj, null) == true
                bool isDestroyed = ReferenceEquals(targetObject, null);

                if (isDestroyed)
                {
                    destroyedObjects.Add(targetObject);

                    // Удаляем индикатор если он существует
                    if (indicator != null)
                    {
                        DestroyImmediate(indicator);
                    }
                    continue;
                }

                if (targetObject != null && indicator != null)
                {
                    // Обновляем позицию индикатора над движущимся объектом
                    Bounds bounds = GetObjectBounds(targetObject);
                    Vector3 newPosition = bounds.center + Vector3.up * (bounds.size.y * 0.5f + selectionIndicatorHeight);
                    indicator.transform.position = newPosition;
                }
                else if (targetObject == null)
                {
                    destroyedObjects.Add(targetObject);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SelectionManager] [UpdateSelectionIndicatorPositions] Exception while updating indicators: {ex.Message}");
            Debug.LogError($"[SelectionManager] Stack trace: {ex.StackTrace}");
        }

        // Удаляем уничтоженные объекты из словаря
        foreach (GameObject destroyedObj in destroyedObjects)
        {
            if (selectionIndicators.ContainsKey(destroyedObj))
            {
                selectionIndicators.Remove(destroyedObj);
            }
        }
    }

    /// <summary>
    /// Получение границ объекта
    /// </summary>
    Bounds GetObjectBounds(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
            return renderer.bounds;
            
        Collider collider = obj.GetComponent<Collider>();
        if (collider == null)
        {
            collider = obj.GetComponentInChildren<Collider>();
        }
        if (collider != null)
            return collider.bounds;
            
        return new Bounds(obj.transform.position, Vector3.one);
    }
    
    /// <summary>
    /// Установка состояния выделения для объекта
    /// </summary>
    void SetObjectSelectionState(GameObject obj, bool selected)
    {
        // Проверяем, что объект не был уничтожен
        if (obj == null)
        {
            return;
        }

        // Обновляем состояние персонажа если это персонаж
        Character character = obj.GetComponent<Character>();
        if (character != null)
        {
            character.SetSelected(selected);
            return;
        }
        
        // Логирование для остальных объектов
        LocationObjectInfo objectInfo = obj.GetComponent<LocationObjectInfo>();
        if (objectInfo != null)
        {
        }
    }
    
    /// <summary>
    /// Обновление информации о выделении в UI - теперь только для GameUI
    /// </summary>
    void UpdateSelectionInfo()
    {
        // Очищаем список от уничтоженных объектов
        selectedObjects.RemoveAll(obj => obj == null);

        // Информация о выделении теперь отображается только в нижней панели GameUI
        // Здесь больше ничего не делаем
    }
    
    /// <summary>
    /// Получение списка выделенных объектов
    /// </summary>
    public List<GameObject> GetSelectedObjects()
    {
        return new List<GameObject>(selectedObjects);
    }
    
    /// <summary>
    /// Проверка, выделен ли объект
    /// </summary>
    public bool IsSelected(GameObject obj)
    {
        return selectedObjects.Contains(obj);
    }
    
    /// <summary>
    /// Диагностика всех объектов с коллайдерами в сцене
    /// </summary>
    void DiagnoseSelectableObjects()
    {


        // Проверяем персонажей
        Character[] allCharacters = FindObjectsOfType<Character>();


        foreach (Character character in allCharacters)
        {
            GameObject obj = character.gameObject;
            Collider collider = obj.GetComponent<Collider>();
            if (collider == null)
            {
                collider = obj.GetComponentInChildren<Collider>();
            }
            bool inMask = (selectableLayerMask & (1 << obj.layer)) != 0;

            // Debug logging disabled
        }

        LocationObjectInfo[] allObjects = FindObjectsOfType<LocationObjectInfo>();

        foreach (LocationObjectInfo objectInfo in allObjects)
        {
            GameObject obj = objectInfo.gameObject;

            Collider collider = obj.GetComponent<Collider>();
            if (collider != null)
            {
                // Проверяем, попадает ли в LayerMask
                bool inMask = (selectableLayerMask & (1 << obj.layer)) != 0;
            }
            else
            {
                // Object has no collider
            }
        }

        // Проверяем комнаты без LocationObjectInfo и добавляем их
        CheckAndFixRoomComponents();


    }

    /// <summary>
    /// Проверить и исправить компоненты комнат
    /// </summary>
    void CheckAndFixRoomComponents()
    {
        // Ищем все объекты с RoomInfo
        RoomInfo[] allRooms = FindObjectsOfType<RoomInfo>();

        foreach (RoomInfo roomInfo in allRooms)
        {
            GameObject roomObj = roomInfo.gameObject;

            // Проверяем, есть ли у комнаты пол с компонентами выделения
            Transform floorTransform = roomObj.transform.Find("Floor");
            Transform selectionFloorTransform = roomObj.transform.Find("SelectionFloor");

            // Для больших комнат - проверяем обычный пол
            if (floorTransform != null)
            {
                GameObject floor = floorTransform.gameObject;

                // Проверяем наличие LocationObjectInfo на полу
                LocationObjectInfo locationInfo = floor.GetComponent<LocationObjectInfo>();
                if (locationInfo == null)
                {
                    // Добавляем недостающий компонент к полу
                    locationInfo = floor.AddComponent<LocationObjectInfo>();
                    locationInfo.objectName = roomInfo.roomName;
                    locationInfo.objectType = roomInfo.roomType;
                    locationInfo.health = 500f;
                    locationInfo.isDestructible = true;
                }

                // Проверяем наличие коллайдера на полу
                BoxCollider floorCollider = floor.GetComponent<BoxCollider>();
                if (floorCollider == null)
                {
                    floorCollider = floor.AddComponent<BoxCollider>();
                    floorCollider.isTrigger = false; // Не триггер для raycast
                }
            }
            // Для маленьких комнат - проверяем SelectionFloor
            else if (selectionFloorTransform != null)
            {
                GameObject selectionFloor = selectionFloorTransform.gameObject;

                // Проверяем наличие LocationObjectInfo
                LocationObjectInfo locationInfo = selectionFloor.GetComponent<LocationObjectInfo>();
                if (locationInfo == null)
                {
                    locationInfo = selectionFloor.AddComponent<LocationObjectInfo>();
                    locationInfo.objectName = roomInfo.roomName;
                    locationInfo.objectType = roomInfo.roomType;
                    locationInfo.health = 500f;
                    locationInfo.isDestructible = true;
                }

                // Проверяем наличие коллайдера
                BoxCollider selectionCollider = selectionFloor.GetComponent<BoxCollider>();
                if (selectionCollider == null)
                {
                    selectionCollider = selectionFloor.AddComponent<BoxCollider>();
                    selectionCollider.isTrigger = false;
                }
            }
        }
    }

    /// <summary>
    /// Обработка подсветки объектов при hover
    /// </summary>
    void HandleHover()
    {
        if (isBoxSelecting) return; // Не обрабатываем hover во время выделения рамкой

        try
        {
            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, selectableLayerMask, QueryTriggerInteraction.Collide);

            GameObject hoveredObject = null;

            // Ищем объект для подсветки
            foreach (RaycastHit hit in hits)
            {
                GameObject hitObject = hit.collider.gameObject;

                // ЗАЩИТА: Проверяем что объект не был уничтожен
                if (ReferenceEquals(hitObject, null) || hitObject == null)
                {
                    continue;
                }

                // Исключаем системные объекты
                if (hitObject.name.Contains("Bounds") || hitObject.name.Contains("Grid") ||
                    hitObject.name.Contains("Location") && !hitObject.name.Contains("Test") ||
                    hitObject.name.Contains("Plane"))
                {
                    continue;
                }

                // Ищем корневой объект для подсветки (префаб)
                GameObject rootObject = FindHoverableRoot(hitObject);
                if (rootObject != null && !ReferenceEquals(rootObject, null))
                {
                    // Проверяем, является ли это полом комнаты
                    RoomInfo roomInfo = rootObject.GetComponentInParent<RoomInfo>();
                    if (roomInfo != null)
                    {
                        // Если это пол комнаты, подсвечиваем родительскую комнату
                        hoveredObject = roomInfo.gameObject;
                    }
                    else
                    {
                        hoveredObject = rootObject;
                    }
                    break;
                }
            }

            // Проверяем что currentHoveredObject не был уничтожен
            if (!ReferenceEquals(currentHoveredObject, null) && currentHoveredObject == null)
            {
                currentHoveredObject = null;
            }

            // Обновляем подсветку
            if (hoveredObject != currentHoveredObject)
            {
                // Убираем подсветку с предыдущего объекта
                if (currentHoveredObject != null && !ReferenceEquals(currentHoveredObject, null))
                {
                    EndHover(currentHoveredObject);
                }

                // Добавляем подсветку новому объекту, только если он НЕ выделен
                if (hoveredObject != null && !ReferenceEquals(hoveredObject, null) && !IsSelected(hoveredObject))
                {
                    StartHover(hoveredObject);
                }

                currentHoveredObject = hoveredObject;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SelectionManager] [HandleHover] Exception: {ex.Message}");
            Debug.LogError($"[SelectionManager] Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Найти корневой объект для подсветки (префаб)
    /// </summary>
    GameObject FindHoverableRoot(GameObject hitObject)
    {
        // Проверяем, может ли сам объект быть подсвечен
        if (CanObjectBeHighlighted(hitObject))
        {
            return hitObject;
        }

        // Ищем в родительских объектах
        Transform current = hitObject.transform.parent;
        while (current != null)
        {
            if (CanObjectBeHighlighted(current.gameObject))
            {
                return current.gameObject;
            }
            current = current.parent;
        }

        return null;
    }

    /// <summary>
    /// Проверить, может ли объект быть подсвечен
    /// </summary>
    bool CanObjectBeHighlighted(GameObject obj)
    {
        // Проверяем наличие MeshRenderer в объекте или его детях
        MeshRenderer[] renderers = obj.GetComponentsInChildren<MeshRenderer>();
        if (renderers.Length == 0) return false;

        // ПЕРСОНАЖИ НЕ УЧАСТВУЮТ В HOVER СИСТЕМЕ - у них своя система цветов
        if (obj.GetComponent<Character>() != null)
        {
            return false;
        }

        // Специальная логика для SM_Cockpit - он должен подсвечиваться целиком
        if (obj.name == "SM_Cockpit")
        {
            return true;
        }

        // Для других объектов - проверяем наличие LocationObjectInfo
        if (obj.GetComponent<LocationObjectInfo>() != null)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Начать подсветку объекта и всех его дочерних MeshRenderer'ов
    /// </summary>
    void StartHover(GameObject obj)
    {
        // Проверяем, что объект не выделен (дополнительная проверка)
        if (IsSelected(obj)) return;

        // Получаем все MeshRenderer в объекте и его детях
        MeshRenderer[] renderers = obj.GetComponentsInChildren<MeshRenderer>();

        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer == null) continue;

            // Сохраняем оригинальный материал если еще не сохранили
            if (!originalMaterials.ContainsKey(renderer))
            {
                originalMaterials[renderer] = renderer.material;
            }

            // Создаем или получаем hover материал
            if (!hoverMaterials.ContainsKey(renderer))
            {
                Material hoverMat = new Material(originalMaterials[renderer]);
                hoverMat.color = hoverColor;
                hoverMaterials[renderer] = hoverMat;
            }

            renderer.material = hoverMaterials[renderer];
            currentHighlightedRenderers.Add(renderer);
        }
    }

    /// <summary>
    /// Завершить подсветку объекта и всех его дочерних MeshRenderer'ов
    /// </summary>
    void EndHover(GameObject obj)
    {
        // Восстанавливаем материалы всех подсвеченных рендереров
        foreach (MeshRenderer renderer in currentHighlightedRenderers)
        {
            if (renderer != null && originalMaterials.ContainsKey(renderer))
            {
                renderer.material = originalMaterials[renderer];
            }
        }

        currentHighlightedRenderers.Clear();
    }

    /// <summary>
    /// Очистить hover материалы для конкретного объекта
    /// </summary>
    void ClearHoverMaterialsForObject(GameObject obj)
    {
        MeshRenderer[] renderers = obj.GetComponentsInChildren<MeshRenderer>();

        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer == null) continue;

            // Восстанавливаем оригинальный материал
            if (originalMaterials.ContainsKey(renderer))
            {
                renderer.material = originalMaterials[renderer];
            }

            // Удаляем записи о hover материалах
            if (originalMaterials.ContainsKey(renderer))
            {
                originalMaterials.Remove(renderer);
            }

            if (hoverMaterials.ContainsKey(renderer))
            {
                Material hoverMat = hoverMaterials[renderer];
                if (hoverMat != null)
                {
                    DestroyImmediate(hoverMat);
                }
                hoverMaterials.Remove(renderer);
            }

            // Убираем из списка подсвеченных
            currentHighlightedRenderers.Remove(renderer);
        }
    }


    /// <summary>
    /// Обработка ПКМ - взаимодействие с предметами и объектами
    /// </summary>
    void HandleRightClick()
    {
        // Проверяем, есть ли выделенные персонажи
        if (selectedObjects.Count == 0)
        {
            return;
        }

        // Получаем ВСЕХ выделенных персонажей игрока
        List<Character> selectedCharacters = new List<Character>();
        foreach (GameObject obj in selectedObjects)
        {
            Character character = obj.GetComponent<Character>();
            if (character != null && character.IsPlayerCharacter())
            {
                selectedCharacters.Add(character);
            }
        }

        if (selectedCharacters.Count == 0)
        {
            return;
        }

        // Первый персонаж (для совместимости со старым кодом подбора предметов)
        Character selectedCharacter = selectedCharacters[0];

        // Raycast для поиска объекта под курсором
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, selectableLayerMask, QueryTriggerInteraction.Collide);

        foreach (RaycastHit hit in hits)
        {
            GameObject hitObject = hit.collider.gameObject;

            // Проверяем, является ли это астероидом для добычи
            LocationObjectInfo locationInfo = hitObject.GetComponent<LocationObjectInfo>();
            if (locationInfo != null && locationInfo.IsOfType("Asteroid"))
            {
                if (locationInfo.metalAmount > 0)
                {
                    // Находим или создаем MiningManager
                    MiningManager miningManager = FindObjectOfType<MiningManager>();
                    if (miningManager == null)
                    {
                        GameObject miningManagerObj = new GameObject("MiningManager");
                        miningManager = miningManagerObj.AddComponent<MiningManager>();
                    }

                    // Начинаем добычу для ВСЕХ выделенных персонажей
                    foreach (Character character in selectedCharacters)
                    {
                        miningManager.StartMining(character, hitObject);
                    }

                    // ВАЖНО: Устанавливаем флаг чтобы другие системы не обрабатывали этот клик
                    rightClickHandledThisFrame = true;
                    return;
                }
                else
                {
                    return;
                }
            }

            // Проверяем, является ли это предметом
            Item item = hitObject.GetComponent<Item>();
            if (item != null && !ReferenceEquals(item, null))
            {
                // Проверяем, включен ли подбор по ПКМ
                if (!enableRightClickPickup)
                {
                    continue; // Пропускаем этот предмет и продолжаем поиск других объектов
                }

                if (item.canBePickedUp)
                {
                    // Отправляем персонажа к предмету
                    CharacterMovement movement = selectedCharacter.GetComponent<CharacterMovement>();
                    if (movement != null)
                    {
                        Vector3 itemPosition = item.transform.position;
                        float distance = Vector3.Distance(selectedCharacter.transform.position, itemPosition);

                        // Если персонаж уже рядом - подбираем сразу
                        if (distance <= item.pickupRange)
                        {
                            PickupItem(selectedCharacter, item);
                        }
                        else
                        {
                            // Отправляем персонажа к предмету
                            movement.MoveTo(itemPosition);

                            // Запускаем корутину для подбора предмета после движения
                            StartCoroutine(WaitForMovementAndPickup(movement, selectedCharacter, item));
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[SelectionManager] Character {selectedCharacter.GetFullName()} has no CharacterMovement component");
                    }

                    // ВАЖНО: Устанавливаем флаг чтобы другие системы не обрабатывали этот клик
                    rightClickHandledThisFrame = true;
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Корутина ожидания завершения движения и подбора предмета
    /// </summary>
    System.Collections.IEnumerator WaitForMovementAndPickup(CharacterMovement movement, Character character, Item item)
    {
        // Даем персонажу время начать движение (CharacterMovement использует StartMovementAfterDelay)
        yield return null;
        yield return null;

        // ЗАЩИТА: Проверяем что item не был уничтожен
        if (ReferenceEquals(item, null) || item == null || item.gameObject == null)
        {
            yield break;
        }

        // Ждем пока персонаж движется
        while (movement != null && movement.IsMoving())
        {
            // ЗАЩИТА: Проверяем каждый кадр что item еще существует
            if (ReferenceEquals(item, null) || item == null || item.gameObject == null)
            {
                yield break;
            }
            yield return null;
        }

        // Проверяем что персонаж и предмет все еще существуют
        if (character != null && !ReferenceEquals(item, null) && item != null && item.gameObject != null)
        {
            // Проверяем дистанцию после движения
            float finalDistance = Vector3.Distance(character.transform.position, item.transform.position);

            if (finalDistance <= item.pickupRange)
            {
                PickupItem(character, item);
                // ВАЖНО: Останавливаем корутину после подбора, т.к. предмет будет уничтожен
                yield break;
            }
            else
            {
                Debug.LogWarning($"[SelectionManager] {character.GetFullName()} reached destination but still too far from item");
            }
        }
        else
        {
            Debug.LogWarning($"[SelectionManager] Character or item no longer exists after movement");
        }
    }

    /// <summary>
    /// Подобрать предмет персонажем
    /// </summary>
    void PickupItem(Character character, Item item)
    {
        if (character == null || item == null)
            return;

        // ВАЖНО: Снимаем выделение с предмета ПЕРЕД подбором
        // чтобы UpdateSelectionIndicatorPositions() не пытался обратиться к уничтоженному объекту
        if (IsSelected(item.gameObject))
        {
            RemoveFromSelection(item.gameObject);
        }

        // Освобождаем клетку в GridManager
        GridManager gridManager = FindObjectOfType<GridManager>();
        if (gridManager != null)
        {
            Vector2Int gridPos = gridManager.WorldToGrid(item.transform.position);
            gridManager.FreeCell(gridPos);
        }

        // Подбираем предмет (ВАЖНО: после этого item будет уничтожен!)
        item.PickUp(character);
    }

    void OnDestroy()
    {
        try
        {
            // Безопасная очистка выделения при уничтожении (без уведомления UI)
            if (selectedObjects != null)
            {
                foreach (var obj in selectedObjects)
                {
                    if (obj != null)
                    {
                        RemoveSelectionIndicator(obj);
                    }
                }
                selectedObjects.Clear();
            }

            // Очистка hover состояния
            if (currentHoveredObject != null)
            {
                EndHover(currentHoveredObject);
            }

            // Уничтожение созданных hover материалов
            foreach (var material in hoverMaterials.Values)
            {
                if (material != null)
                {
                    DestroyImmediate(material);
                }
            }
        }
        catch (System.Exception)
        {
            // Error handled silently during cleanup
        }

        // Принудительная очистка словарей
        if (selectionIndicators != null)
        {
            selectionIndicators.Clear();
        }

        if (originalMaterials != null)
        {
            originalMaterials.Clear();
        }

        if (hoverMaterials != null)
        {
            hoverMaterials.Clear();
        }

        if (currentHighlightedRenderers != null)
        {
            currentHighlightedRenderers.Clear();
        }

        // Очистка списка выделенных объектов
        if (selectedObjects != null)
        {
            selectedObjects.Clear();
        }
    }
}