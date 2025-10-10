using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Система размещения главных объектов в модулях
/// </summary>
public class MainObjectPlacementSystem : MonoBehaviour
{
    [Header("References")]
    public ShipBuildingSystem buildingSystem;
    public GridManager gridManager;
    public Camera playerCamera;

    [Header("Placement Settings")]
    private bool placementMode = false;
    private MainObjectData selectedObjectData;
    private GameObject previewObject;
    private RoomInfo targetRoom;

    private static MainObjectPlacementSystem instance;
    public static MainObjectPlacementSystem Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("MainObjectPlacementSystem");
                instance = go.AddComponent<MainObjectPlacementSystem>();
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (buildingSystem == null)
            buildingSystem = FindObjectOfType<ShipBuildingSystem>();

        if (gridManager == null)
            gridManager = FindObjectOfType<GridManager>();

        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    void Update()
    {
        // Блокируем размещение если игра на паузе (но не пауза строительства)
        bool isPaused = GamePauseManager.Instance != null && GamePauseManager.Instance.IsPaused();
        bool isBuildModePause = GamePauseManager.Instance != null && GamePauseManager.Instance.IsBuildModePause();

        if (placementMode && (!isPaused || isBuildModePause))
        {
            UpdatePreview();
            HandleInput();
        }
    }

    /// <summary>
    /// Начать размещение главного объекта
    /// </summary>
    public void StartPlacement(MainObjectData objectData)
    {
        selectedObjectData = objectData;
        placementMode = true;
        targetRoom = null;

        FileLogger.Log($"[MainObjectPlacement] Started placement mode for {objectData.objectName}");

        // Создаем призрак объекта если есть префаб
        if (objectData.prefab != null)
        {
            CreatePreview();
        }
    }

    /// <summary>
    /// Остановить размещение
    /// </summary>
    public void StopPlacement()
    {
        placementMode = false;
        selectedObjectData = null;
        targetRoom = null;

        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
        }

        FileLogger.Log($"[MainObjectPlacement] Stopped placement mode");
    }

    /// <summary>
    /// Создать призрак объекта
    /// </summary>
    void CreatePreview()
    {
        if (selectedObjectData.prefab != null)
        {
            previewObject = Instantiate(selectedObjectData.prefab);
            previewObject.name = "MainObject_Preview";

            // Делаем полупрозрачным
            Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                Material ghostMat = new Material(renderer.material);
                Color color = ghostMat.color;
                color.a = 0.5f;
                ghostMat.color = color;
                renderer.material = ghostMat;
            }

            // Убираем коллайдеры
            Collider[] colliders = previewObject.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                Destroy(col);
            }
        }
    }

    /// <summary>
    /// Обновление предпросмотра
    /// </summary>
    void UpdatePreview()
    {
        if (playerCamera == null) return;

        // Raycast для определения на какую комнату наведена мышь
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Проверяем попали ли в комнату
            RoomInfo roomInfo = hit.collider.GetComponentInParent<RoomInfo>();

            if (roomInfo != null)
            {
                targetRoom = roomInfo;

                // Обновляем позицию призрака если он есть
                if (previewObject != null)
                {
                    Vector3 roomCenter = gridManager.GridToWorld(roomInfo.gridPosition);
                    roomCenter.x += roomInfo.roomSize.x * gridManager.cellSize * 0.5f;
                    roomCenter.z += roomInfo.roomSize.y * gridManager.cellSize * 0.5f;
                    roomCenter.y = 1f; // Немного выше пола

                    previewObject.transform.position = roomCenter;

                    // Меняем цвет в зависимости от того, можно ли разместить
                    bool canPlace = roomInfo.CanPlaceMainObject();
                    UpdatePreviewColor(canPlace);
                }
            }
            else
            {
                targetRoom = null;
                if (previewObject != null)
                {
                    UpdatePreviewColor(false);
                }
            }
        }
    }

    /// <summary>
    /// Обновить цвет призрака
    /// </summary>
    void UpdatePreviewColor(bool canPlace)
    {
        if (previewObject == null) return;

        Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            Material mat = renderer.material;
            Color color = canPlace ? Color.green : Color.red;
            color.a = 0.5f;
            mat.color = color;
        }
    }

    /// <summary>
    /// Обработка ввода
    /// </summary>
    void HandleInput()
    {
        // Проверяем не над UI ли мышь
        bool isPointerOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

        // ЛКМ - разместить объект
        if (Input.GetMouseButtonDown(0) && !isPointerOverUI)
        {
            TryPlaceObject();
        }

        // ПКМ - отменить размещение
        if (Input.GetMouseButtonDown(1))
        {
            StopPlacement();
        }
    }

    /// <summary>
    /// Попытка разместить объект
    /// </summary>
    void TryPlaceObject()
    {
        if (targetRoom == null)
        {
            FileLogger.Log($"[MainObjectPlacement] No room selected");
            return;
        }

        if (!targetRoom.CanPlaceMainObject())
        {
            FileLogger.Log($"[MainObjectPlacement] Cannot place object in room {targetRoom.roomName} - already has main object");
            return;
        }

        // Создаем главный объект
        GameObject mainObjectGO;

        if (selectedObjectData.prefab != null)
        {
            mainObjectGO = Instantiate(selectedObjectData.prefab);
        }
        else
        {
            // Fallback - создаем простой куб
            mainObjectGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mainObjectGO.transform.localScale = Vector3.one * 2f;
        }

        mainObjectGO.name = $"MainObject_{selectedObjectData.objectType}";

        // Позиционируем в центре комнаты
        Vector3 roomCenter = gridManager.GridToWorld(targetRoom.gridPosition);
        roomCenter.x += targetRoom.roomSize.x * gridManager.cellSize * 0.5f;
        roomCenter.z += targetRoom.roomSize.y * gridManager.cellSize * 0.5f;
        roomCenter.y = 1f;
        mainObjectGO.transform.position = roomCenter;

        // Добавляем компонент главного объекта
        ModuleMainObject mainObjectComp = mainObjectGO.AddComponent<ModuleMainObject>();
        mainObjectComp.objectType = selectedObjectData.objectType;
        mainObjectComp.objectName = selectedObjectData.objectName;
        mainObjectComp.maxHealth = selectedObjectData.maxHealth;
        mainObjectComp.currentHealth = selectedObjectData.maxHealth;

        // Устанавливаем в комнату
        if (targetRoom.SetMainObject(mainObjectComp))
        {
            FileLogger.Log($"[MainObjectPlacement] Successfully placed {selectedObjectData.objectName} in room {targetRoom.roomName}");

            // Родительским объектом делаем комнату для удобства
            mainObjectGO.transform.SetParent(targetRoom.transform);

            // Завершаем режим размещения
            StopPlacement();
        }
        else
        {
            FileLogger.Log($"[MainObjectPlacement] Failed to place object");
            Destroy(mainObjectGO);
        }
    }

    /// <summary>
    /// Получить список доступных главных объектов
    /// </summary>
    public List<MainObjectData> GetAvailableMainObjects()
    {
        if (buildingSystem != null)
        {
            return buildingSystem.availableMainObjects;
        }
        return new List<MainObjectData>();
    }

    /// <summary>
    /// Проверить активен ли режим размещения
    /// </summary>
    public bool IsPlacementActive()
    {
        return placementMode;
    }
}
