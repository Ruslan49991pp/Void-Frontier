using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Система строительства внутренних стен с drag и подтверждением
/// Ghost preview: синий (можно) / красный (нельзя)
/// После размещения: зеленые ghosts
/// После подтверждения: реальные префабы
/// </summary>
public class InteriorWallDragBuilder : MonoBehaviour
{
    [Header("Interior Wall Prefabs")]
    public GameObject intWallPrefab;
    public GameObject intWallCornerPrefab;
    public GameObject intWallInnerCornerPrefab;
    public GameObject intWallTPrefab;
    public GameObject intWallXPrefab;

    [Header("Ghost Materials")]
    public Material validGhostMaterial;      // M_Add_Build_Ghost (синий)
    public Material invalidGhostMaterial;    // M_Add_Int_Ghost (красный)
    public Material confirmedGhostMaterial;  // Зеленый для размещенных стен

    [Header("Settings")]
    public float cellSize = 10f;

    [Header("References")]
    public GridManager gridManager;
    public Button addBuildButton; // Кнопка подтверждения

    private static InteriorWallDragBuilder instance;

    // Активные (подтвержденные) стены
    private Dictionary<Vector2Int, GameObject> activeIntWalls = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, InteriorWallType> wallTypes = new Dictionary<Vector2Int, InteriorWallType>();

    // Размещенные (зеленые ghost) стены - ожидают подтверждения
    private Dictionary<Vector2Int, GameObject> confirmedGhostWalls = new Dictionary<Vector2Int, GameObject>();

    // Drag состояние
    private bool isDragMode = false;
    private bool isDragging = false;
    private Vector2Int? dragStart = null;
    private List<Vector2Int> currentDragLine = new List<Vector2Int>();
    private Dictionary<Vector2Int, GameObject> previewGhostWalls = new Dictionary<Vector2Int, GameObject>();

    // Cursor preview
    private GameObject cursorGhost = null;
    private Vector2Int lastCursorPos = Vector2Int.zero;

    public static InteriorWallDragBuilder Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("InteriorWallDragBuilder");
                instance = go.AddComponent<InteriorWallDragBuilder>();
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            InitializeWithGridManager();
            LoadPrefabs();
            LoadMaterials();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void InitializeWithGridManager()
    {
        if (gridManager == null)
            gridManager = FindObjectOfType<GridManager>();

        if (gridManager != null)
            cellSize = gridManager.cellSize;
    }

    void LoadPrefabs()
    {
        if (intWallPrefab == null)
            intWallPrefab = Resources.Load<GameObject>("Prefabs/Wall_Interior/SM_Int_Wall");

        if (intWallCornerPrefab == null)
            intWallCornerPrefab = Resources.Load<GameObject>("Prefabs/Wall_Interior/SM_Int_Wall_L");

        if (intWallInnerCornerPrefab == null)
            intWallInnerCornerPrefab = Resources.Load<GameObject>("Prefabs/Wall_Interior/SM_Int_Wall_L_Undo");

        if (intWallTPrefab == null)
            intWallTPrefab = Resources.Load<GameObject>("Prefabs/Wall_Interior/SM_Int_Wall_T");

        if (intWallXPrefab == null)
            intWallXPrefab = Resources.Load<GameObject>("Prefabs/Wall_Interior/SM_Int_Wall_X");

        FileLogger.Log("[InteriorWallDragBuilder] Prefabs loaded");
    }

    void LoadMaterials()
    {
        if (validGhostMaterial == null)
        {
            validGhostMaterial = Resources.Load<Material>("Materials/M_Add_Build_Ghost");
            if (validGhostMaterial == null)
                FileLogger.LogError("[InteriorWallDragBuilder] M_Add_Build_Ghost not found!");
        }

        if (invalidGhostMaterial == null)
        {
            invalidGhostMaterial = Resources.Load<Material>("Materials/M_Add_Int_Ghost");
            if (invalidGhostMaterial == null)
                FileLogger.LogError("[InteriorWallDragBuilder] M_Add_Int_Ghost not found!");
        }

        // Создаем зеленый материал для подтвержденных стен
        if (confirmedGhostMaterial == null)
        {
            confirmedGhostMaterial = new Material(Shader.Find("Standard"));
            confirmedGhostMaterial.color = new Color(0.3f, 1f, 0.3f, 0.7f); // Зеленый
            confirmedGhostMaterial.SetFloat("_Mode", 3);
            confirmedGhostMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            confirmedGhostMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            confirmedGhostMaterial.SetInt("_ZWrite", 0);
            confirmedGhostMaterial.EnableKeyword("_ALPHABLEND_ON");
            confirmedGhostMaterial.renderQueue = 3000;
        }

        FileLogger.Log("[InteriorWallDragBuilder] Materials loaded");
    }

    void Update()
    {
        if (!isDragMode) return;

        UpdateCursorPreview();

        if (Input.GetMouseButtonDown(0))
            StartDrag();

        if (isDragging)
            UpdateDrag();

        if (Input.GetMouseButtonUp(0) && isDragging)
            FinishDrag();

        if (Input.GetMouseButtonDown(1))
        {
            if (isDragging)
                CancelDrag();
            else
                DeactivateDragMode();
        }

        // Обновляем состояние кнопки подтверждения
        UpdateAddBuildButton();
    }

    public void ActivateDragMode()
    {
        isDragMode = true;
        SetupAddBuildButton();
        FileLogger.Log("[InteriorWallDragBuilder] Drag mode activated");
    }

    public void DeactivateDragMode()
    {
        isDragMode = false;
        isDragging = false;
        ClearPreviewGhosts();
        ClearCursorGhost();

        // Скрываем кнопку подтверждения
        if (addBuildButton != null)
            addBuildButton.gameObject.SetActive(false);

        FileLogger.Log("[InteriorWallDragBuilder] Drag mode deactivated");
    }

    void UpdateCursorPreview()
    {
        if (isDragging) return;

        Vector2Int? gridPos = GetMouseGridPosition();

        if (!gridPos.HasValue || gridPos.Value == lastCursorPos)
            return;

        lastCursorPos = gridPos.Value;
        ClearCursorGhost();

        // Создаем cursor ghost с правильным цветом
        bool canPlace = CanPlaceWallAt(gridPos.Value);
        cursorGhost = CreateGhostWall(gridPos.Value, canPlace);
    }

    void StartDrag()
    {
        Vector2Int? gridPos = GetMouseGridPosition();
        if (!gridPos.HasValue)
            return;

        isDragging = true;
        dragStart = gridPos.Value;
        currentDragLine.Clear();
        currentDragLine.Add(gridPos.Value);

        ClearCursorGhost();
        UpdateDragGhosts();

        FileLogger.Log($"[InteriorWallDragBuilder] Started drag at {dragStart.Value}");
    }

    void UpdateDrag()
    {
        Vector2Int? currentPos = GetMouseGridPosition();
        if (!currentPos.HasValue || !dragStart.HasValue)
            return;

        List<Vector2Int> newLine = GetLineBetween(dragStart.Value, currentPos.Value);

        if (!AreListsEqual(newLine, currentDragLine))
        {
            currentDragLine = newLine;
            UpdateDragGhosts();
        }
    }

    void FinishDrag()
    {
        if (currentDragLine.Count == 0)
        {
            CancelDrag();
            return;
        }

        FileLogger.Log($"[InteriorWallDragBuilder] Finishing drag, placing {currentDragLine.Count} walls");

        // Размещаем ЗЕЛЕНЫЕ ghost стены
        foreach (Vector2Int pos in currentDragLine)
        {
            if (CanPlaceWallAt(pos) && !confirmedGhostWalls.ContainsKey(pos))
            {
                GameObject greenGhost = CreateConfirmedGhostWall(pos);
                confirmedGhostWalls[pos] = greenGhost;
            }
        }

        // Очищаем drag state
        isDragging = false;
        dragStart = null;
        currentDragLine.Clear();
        ClearPreviewGhosts();

        FileLogger.Log($"[InteriorWallDragBuilder] Drag finished, total confirmed ghosts: {confirmedGhostWalls.Count}");
    }

    void CancelDrag()
    {
        isDragging = false;
        dragStart = null;
        currentDragLine.Clear();
        ClearPreviewGhosts();
        FileLogger.Log("[InteriorWallDragBuilder] Drag cancelled");
    }

    void UpdateDragGhosts()
    {
        ClearPreviewGhosts();

        foreach (Vector2Int pos in currentDragLine)
        {
            if (!confirmedGhostWalls.ContainsKey(pos) && !previewGhostWalls.ContainsKey(pos))
            {
                bool canPlace = CanPlaceWallAt(pos);
                previewGhostWalls[pos] = CreateGhostWall(pos, canPlace);
            }
        }
    }

    /// <summary>
    /// Создать ghost стену (синий если можно, красный если нельзя)
    /// </summary>
    GameObject CreateGhostWall(Vector2Int gridPos, bool canPlace)
    {
        if (intWallPrefab == null) return null;

        GameObject ghost = Instantiate(intWallPrefab);
        ghost.name = "IntWall_Ghost_Preview";

        Vector3 worldPos = GridToWorld(gridPos);
        ghost.transform.position = worldPos;
        ghost.transform.rotation = Quaternion.identity;

        // Применяем материал в зависимости от возможности размещения
        Material ghostMat = canPlace ? validGhostMaterial : invalidGhostMaterial;
        ApplyMaterialToGhost(ghost, ghostMat);

        // Убираем коллайдеры
        RemoveColliders(ghost);

        return ghost;
    }

    /// <summary>
    /// Создать подтвержденную (зеленую) ghost стену
    /// </summary>
    GameObject CreateConfirmedGhostWall(Vector2Int gridPos)
    {
        if (intWallPrefab == null) return null;

        GameObject ghost = Instantiate(intWallPrefab);
        ghost.name = "IntWall_Ghost_Confirmed";

        Vector3 worldPos = GridToWorld(gridPos);
        ghost.transform.position = worldPos;
        ghost.transform.rotation = Quaternion.identity;

        // Применяем зеленый материал
        ApplyMaterialToGhost(ghost, confirmedGhostMaterial);

        // Убираем коллайдеры
        RemoveColliders(ghost);

        return ghost;
    }

    void ApplyMaterialToGhost(GameObject ghost, Material mat)
    {
        if (mat == null) return;

        Renderer[] renderers = ghost.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            Material[] mats = new Material[renderer.materials.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = mat;
            }
            renderer.materials = mats;
        }
    }

    void RemoveColliders(GameObject obj)
    {
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            Destroy(collider);
        }
    }

    void ClearPreviewGhosts()
    {
        foreach (var ghost in previewGhostWalls.Values)
        {
            if (ghost != null)
                Destroy(ghost);
        }
        previewGhostWalls.Clear();
    }

    void ClearCursorGhost()
    {
        if (cursorGhost != null)
        {
            Destroy(cursorGhost);
            cursorGhost = null;
        }
    }

    /// <summary>
    /// Подтвердить строительство - конвертировать зеленые ghosts в реальные стены
    /// </summary>
    public void ConfirmBuild()
    {
        if (confirmedGhostWalls.Count == 0)
        {
            FileLogger.Log("[InteriorWallDragBuilder] Nothing to confirm");
            return;
        }

        FileLogger.Log($"[InteriorWallDragBuilder] Confirming {confirmedGhostWalls.Count} walls");

        // Конвертируем все зеленые ghosts в реальные стены
        List<Vector2Int> positions = new List<Vector2Int>(confirmedGhostWalls.Keys);

        foreach (Vector2Int pos in positions)
        {
            // Удаляем зеленый ghost
            GameObject ghost = confirmedGhostWalls[pos];
            if (ghost != null)
                Destroy(ghost);

            // Создаем реальную стену
            InteriorWallType wallType = DetermineWallType(pos);
            wallTypes[pos] = wallType;

            GameObject wall = CreateRealWall(pos, wallType);
            if (wall != null)
            {
                activeIntWalls[pos] = wall;
            }
        }

        // Очищаем confirmed ghosts
        confirmedGhostWalls.Clear();

        // Обновляем все стены (для правильных соединений)
        UpdateAllWalls();

        FileLogger.Log($"[InteriorWallDragBuilder] Build confirmed, total walls: {activeIntWalls.Count}");
    }

    /// <summary>
    /// Создать реальную стену
    /// </summary>
    GameObject CreateRealWall(Vector2Int gridPos, InteriorWallType wallType)
    {
        GameObject prefabToUse = GetPrefabForType(wallType);
        if (prefabToUse == null) return null;

        GameObject wall = Instantiate(prefabToUse);
        wall.name = $"IntWall_{gridPos.x}_{gridPos.y}_{wallType}";

        Vector3 worldPos = GridToWorld(gridPos);
        wall.transform.position = worldPos;

        float rotation = GetRotationForWall(gridPos, wallType);
        wall.transform.rotation = Quaternion.Euler(0, rotation, 0);
        wall.transform.localScale = Vector3.one;

        // Коллайдер
        BoxCollider collider = wall.GetComponent<BoxCollider>();
        if (collider == null)
            collider = wall.AddComponent<BoxCollider>();
        collider.isTrigger = false;

        // Компоненты
        InteriorWallComponent wallComp = wall.AddComponent<InteriorWallComponent>();
        wallComp.gridPosition = gridPos;
        wallComp.wallType = wallType;

        LocationObjectInfo locationInfo = wall.AddComponent<LocationObjectInfo>();
        locationInfo.objectType = "InteriorWall";
        locationInfo.objectName = $"Внутренняя стена ({gridPos.x}, {gridPos.y})";
        locationInfo.health = 50f;
        locationInfo.isDestructible = true;

        return wall;
    }

    void UpdateAllWalls()
    {
        List<Vector2Int> positions = new List<Vector2Int>(activeIntWalls.Keys);

        foreach (Vector2Int pos in positions)
        {
            InteriorWallType newType = DetermineWallType(pos);
            InteriorWallType oldType = wallTypes.ContainsKey(pos) ? wallTypes[pos] : InteriorWallType.Straight;

            if (newType != oldType)
            {
                GameObject oldWall = activeIntWalls[pos];
                if (oldWall != null)
                    Destroy(oldWall);

                GameObject newWall = CreateRealWall(pos, newType);
                if (newWall != null)
                {
                    activeIntWalls[pos] = newWall;
                    wallTypes[pos] = newType;
                }
            }
        }
    }

    InteriorWallType DetermineWallType(Vector2Int gridPos)
    {
        int neighborCount = 0;
        bool hasTop = activeIntWalls.ContainsKey(gridPos + Vector2Int.up) || confirmedGhostWalls.ContainsKey(gridPos + Vector2Int.up);
        bool hasBottom = activeIntWalls.ContainsKey(gridPos + Vector2Int.down) || confirmedGhostWalls.ContainsKey(gridPos + Vector2Int.down);
        bool hasLeft = activeIntWalls.ContainsKey(gridPos + Vector2Int.left) || confirmedGhostWalls.ContainsKey(gridPos + Vector2Int.left);
        bool hasRight = activeIntWalls.ContainsKey(gridPos + Vector2Int.right) || confirmedGhostWalls.ContainsKey(gridPos + Vector2Int.right);

        if (hasTop) neighborCount++;
        if (hasBottom) neighborCount++;
        if (hasLeft) neighborCount++;
        if (hasRight) neighborCount++;

        if (neighborCount == 4) return InteriorWallType.Cross;
        if (neighborCount == 3) return InteriorWallType.T;
        if (neighborCount == 2)
        {
            if ((hasTop && hasRight) || (hasRight && hasBottom) || (hasBottom && hasLeft) || (hasLeft && hasTop))
                return InteriorWallType.Corner;
        }
        return InteriorWallType.Straight;
    }

    GameObject GetPrefabForType(InteriorWallType wallType)
    {
        switch (wallType)
        {
            case InteriorWallType.Straight: return intWallPrefab;
            case InteriorWallType.Corner: return intWallCornerPrefab;
            case InteriorWallType.InnerCorner: return intWallInnerCornerPrefab;
            case InteriorWallType.T: return intWallTPrefab;
            case InteriorWallType.Cross: return intWallXPrefab;
            default: return intWallPrefab;
        }
    }

    float GetRotationForWall(Vector2Int gridPos, InteriorWallType wallType)
    {
        bool hasTop = activeIntWalls.ContainsKey(gridPos + Vector2Int.up) || confirmedGhostWalls.ContainsKey(gridPos + Vector2Int.up);
        bool hasBottom = activeIntWalls.ContainsKey(gridPos + Vector2Int.down) || confirmedGhostWalls.ContainsKey(gridPos + Vector2Int.down);
        bool hasLeft = activeIntWalls.ContainsKey(gridPos + Vector2Int.left) || confirmedGhostWalls.ContainsKey(gridPos + Vector2Int.left);
        bool hasRight = activeIntWalls.ContainsKey(gridPos + Vector2Int.right) || confirmedGhostWalls.ContainsKey(gridPos + Vector2Int.right);

        switch (wallType)
        {
            case InteriorWallType.Straight:
                return (hasTop || hasBottom) ? 0f : 90f;
            case InteriorWallType.Corner:
                if (hasTop && hasRight) return 0f;
                if (hasRight && hasBottom) return 90f;
                if (hasBottom && hasLeft) return 180f;
                if (hasLeft && hasTop) return 270f;
                return 0f;
            case InteriorWallType.T:
                if (!hasTop) return 180f;
                if (!hasRight) return 270f;
                if (!hasBottom) return 0f;
                if (!hasLeft) return 90f;
                return 0f;
            case InteriorWallType.Cross:
                return 0f;
            default:
                return 0f;
        }
    }

    List<Vector2Int> GetLineBetween(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> line = new List<Vector2Int>();
        int x0 = start.x, y0 = start.y, x1 = end.x, y1 = end.y;
        int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            line.Add(new Vector2Int(x0, y0));
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
        return line;
    }

    bool CanPlaceWallAt(Vector2Int gridPos)
    {
        if (activeIntWalls.ContainsKey(gridPos) || confirmedGhostWalls.ContainsKey(gridPos))
            return false;

        if (!IsInsideRoom(gridPos))
            return false;

        if (gridManager != null)
        {
            GridCell cell = gridManager.GetCell(gridPos);
            if (cell == null) return false;
            if (cell.isOccupied && cell.objectType == "Wall")
                return false;
        }

        return true;
    }

    bool IsInsideRoom(Vector2Int gridPos)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                Vector2Int checkPos = gridPos + new Vector2Int(dx, dy);
                Vector3 worldPos = GridToWorld(checkPos);
                Collider[] colliders = Physics.OverlapSphere(worldPos, cellSize * 0.3f);
                foreach (Collider col in colliders)
                {
                    if (col.gameObject.name.Contains("FloorTile"))
                        return true;
                }
            }
        }
        return false;
    }

    Vector2Int? GetMouseGridPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float distance;

        if (groundPlane.Raycast(ray, out distance))
        {
            Vector3 worldPos = ray.GetPoint(distance);
            if (gridManager != null)
            {
                Vector2Int gridPos = gridManager.WorldToGrid(worldPos);
                if (gridManager.IsValidGridPosition(gridPos))
                    return gridPos;
            }
        }
        return null;
    }

    Vector3 GridToWorld(Vector2Int gridPos)
    {
        if (gridManager != null)
            return gridManager.GridToWorld(gridPos);
        return new Vector3(gridPos.x * cellSize + cellSize * 0.5f, 0, gridPos.y * cellSize + cellSize * 0.5f);
    }

    bool AreListsEqual(List<Vector2Int> a, List<Vector2Int> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i]) return false;
        }
        return true;
    }

    void SetupAddBuildButton()
    {
        if (addBuildButton == null)
        {
            GameObject canvasMainUI = GameObject.Find("Canvas_MainUI");
            if (canvasMainUI != null)
            {
                Button[] allButtons = canvasMainUI.GetComponentsInChildren<Button>(true);
                foreach (Button btn in allButtons)
                {
                    if (btn.gameObject.name == "AddBuild")
                    {
                        addBuildButton = btn;
                        break;
                    }
                }
            }
        }

        if (addBuildButton != null)
        {
            // Удаляем старые listeners
            addBuildButton.onClick.RemoveAllListeners();
            addBuildButton.onClick.AddListener(ConfirmBuild);
            addBuildButton.gameObject.SetActive(true);
            addBuildButton.interactable = false;
        }
    }

    void UpdateAddBuildButton()
    {
        if (addBuildButton != null)
        {
            bool shouldBeInteractable = confirmedGhostWalls.Count > 0;
            if (addBuildButton.interactable != shouldBeInteractable)
            {
                addBuildButton.interactable = shouldBeInteractable;
            }
        }
    }

    public bool IsDragModeActive()
    {
        return isDragMode;
    }

    public bool CanConfirmBuild()
    {
        return confirmedGhostWalls.Count > 0;
    }
}

public enum InteriorWallType
{
    Straight,
    Corner,
    InnerCorner,
    T,
    Cross
}

public class InteriorWallComponent : MonoBehaviour
{
    public Vector2Int gridPosition;
    public InteriorWallType wallType;
}
