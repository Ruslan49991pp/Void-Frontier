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
    
    // Для box selection
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

    // Hover система
    private GameObject currentHoveredObject = null;
    private Dictionary<MeshRenderer, Material> originalMaterials = new Dictionary<MeshRenderer, Material>();
    private Dictionary<MeshRenderer, Material> hoverMaterials = new Dictionary<MeshRenderer, Material>();
    private List<MeshRenderer> currentHighlightedRenderers = new List<MeshRenderer>();

    // Публичные свойства
    public bool IsBoxSelecting => isBoxSelecting;
    
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
        HandleMouseInput();
        UpdateSelectionBox();
        UpdateSelectionIndicatorPositions();
        HandleHover();
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
        
        // Мышь зажата - проверяем движение
        if (Input.GetMouseButton(0) && isMousePressed)
        {
            Vector3 currentMousePos = Input.mousePosition;
            float distance = Vector3.Distance(mouseDownPosition, currentMousePos);
            
            // Если движение больше порога и рамка еще не активна, начинаем box selection
            if (distance > clickThreshold && !isBoxSelecting)
            {
                isBoxSelecting = true;
                selectionBoxUI.SetActive(true);
                
                // НЕ очищаем выделение здесь! Будем очищать в конце, если ничего не найдем
            }
            
            // Обновляем конечную позицию для рамки
            if (isBoxSelecting)
            {
                boxEndPosition = currentMousePos;
            }
        }
        
        // Отпускание ЛКМ - решаем что это было: клик или рамка
        if (Input.GetMouseButtonUp(0) && isMousePressed)
        {
            Vector3 currentMousePos = Input.mousePosition;
            float distance = Vector3.Distance(mouseDownPosition, currentMousePos);
            
            if (isBoxSelecting)
            {
                // Это была рамка
                isBoxSelecting = false;
                selectionBoxUI.SetActive(false);
                PerformBoxSelection();
            }
            else if (distance <= clickThreshold)
            {
                // Это был клик
                PerformClickSelection(mouseDownPosition);
            }
            
            isMousePressed = false;
        }
    }
    
    /// <summary>
    /// Выполнение выделения по клику
    /// </summary>
    void PerformClickSelection(Vector3 mousePosition)
    {
        Ray ray = playerCamera.ScreenPointToRay(mousePosition);
        
        // Используем RaycastAll чтобы получить все объекты на луче
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, selectableLayerMask);
        
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

                LocationObjectInfo objectInfo = hitObject.GetComponent<LocationObjectInfo>();

                if (objectInfo != null)
                {
                    // Найден подходящий объект для выделения
                    if (!Input.GetKey(KeyCode.LeftControl))
                    {
                        ClearSelection();
                    }

                    ToggleSelection(hitObject);
                    return;
                }
            }
        }

        // Если мы дошли до этого места, значит не найдено подходящих объектов
        // Это считается кликом в пустое место - очищаем выделение если не зажат Ctrl
        if (!Input.GetKey(KeyCode.LeftControl))
        {
            ClearSelection();
        }
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
    /// Выполнение выделения области
    /// </summary>
    void PerformBoxSelection()
    {
        Vector2 start = playerCamera.ScreenToWorldPoint(boxStartPosition);
        Vector2 end = playerCamera.ScreenToWorldPoint(boxEndPosition);
        
        Vector2 min = Vector2.Min(start, end);
        Vector2 max = Vector2.Max(start, end);
        
        List<GameObject> newSelections = new List<GameObject>();
        
        // Находим все объекты в области
        LocationObjectInfo[] allObjects = FindObjectsOfType<LocationObjectInfo>();
        
        foreach (LocationObjectInfo objectInfo in allObjects)
        {
            Vector3 worldPos = objectInfo.transform.position;
            Vector2 screenPos = playerCamera.WorldToScreenPoint(worldPos);
            
            Vector2 boxMin = Vector2.Min(boxStartPosition, boxEndPosition);
            Vector2 boxMax = Vector2.Max(boxStartPosition, boxEndPosition);
            
            if (screenPos.x >= boxMin.x && screenPos.x <= boxMax.x &&
                screenPos.y >= boxMin.y && screenPos.y <= boxMax.y)
            {
                newSelections.Add(objectInfo.gameObject);
            }
        }
        
        // Логика выделения в зависимости от результата
        if (newSelections.Count > 0)
        {
            // Есть объекты в рамке
            if (!Input.GetKey(KeyCode.LeftControl))
            {
                // Если Ctrl не зажат, заменяем выделение
                ClearSelection();
            }
            
            // Добавляем новые выделения
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
            
            // Очищаем выделение если не зажат Ctrl
            if (!Input.GetKey(KeyCode.LeftControl))
            {
                ClearSelection();
            }
            else
            {
            }
        }
        
        UpdateSelectionInfo();
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
        foreach (var kvp in selectionIndicators)
        {
            GameObject targetObject = kvp.Key;
            GameObject indicator = kvp.Value;

            if (targetObject != null && indicator != null)
            {
                // Обновляем позицию индикатора над движущимся объектом
                Bounds bounds = GetObjectBounds(targetObject);
                Vector3 newPosition = bounds.center + Vector3.up * (bounds.size.y * 0.5f + selectionIndicatorHeight);
                indicator.transform.position = newPosition;
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

    }

    /// <summary>
    /// Обработка подсветки объектов при hover
    /// </summary>
    void HandleHover()
    {
        if (isBoxSelecting) return; // Не обрабатываем hover во время выделения рамкой

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, selectableLayerMask);

        GameObject hoveredObject = null;

        // Ищем объект для подсветки
        foreach (RaycastHit hit in hits)
        {
            GameObject hitObject = hit.collider.gameObject;

            // Исключаем системные объекты
            if (hitObject.name.Contains("Bounds") || hitObject.name.Contains("Grid") ||
                hitObject.name.Contains("Location") && !hitObject.name.Contains("Test") ||
                hitObject.name.Contains("Plane"))
            {
                continue;
            }

            // Ищем корневой объект для подсветки (префаб)
            GameObject rootObject = FindHoverableRoot(hitObject);
            if (rootObject != null)
            {
                hoveredObject = rootObject;
                break;
            }
        }

        // Обновляем подсветку
        if (hoveredObject != currentHoveredObject)
        {
            // Убираем подсветку с предыдущего объекта
            if (currentHoveredObject != null)
            {
                EndHover(currentHoveredObject);
            }

            // Добавляем подсветку новому объекту, только если он НЕ выделен
            if (hoveredObject != null && !IsSelected(hoveredObject))
            {
                StartHover(hoveredObject);
            }

            currentHoveredObject = hoveredObject;
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


    void OnDestroy()
    {
        try
        {
            // Безопасная очистка выделения при уничтожении
            ClearSelection();

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