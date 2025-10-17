using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("References")]
    public Camera cam;                     // ���� ������ �� ���������, ����� Camera.main
    public Transform focusTarget;          // �������� ���� Ship (��� ������ SetFocusTarget)
    public BoxCollider boundsCollider;     // �����������: ������ � BoxCollider, �������� ������� �������

    [Header("Movement")]
    public float panSpeed = 20f;           // �������� WASD
    public float dragSpeed = 0.6f;         // �������� ��� ������������� ������� �������
    public float smoothTime = 0.08f;       // ����������� ��������

    [Header("Zoom (Orthographic)")]
    public float zoomSpeed = 10f;
    public float minOrthoSize = 6f;
    public float maxOrthoSize = 30f;

    [Header("Focus / Controls")]
    // public KeyCode focusKey = KeyCode.F;   // ������� ��� �������� � ������ - ������
    
    [Header("Mouse Edge Scrolling")]
    public bool enableEdgeScrolling = true; // �������� ��������� ����� ������
    public float edgeScrollSpeed = 15f;     // �������� ��������� ����� ������
    public float edgeBorderSize = 5f;       // ������ ������� � �������� ��� ��������� (���������� � 50f)
    
    [Header("Selection Integration")]
    public SelectionManager selectionManager;
    public ShipBuildingSystem buildingSystem;

    [Header("Follow Mode")]
    private bool isFollowingTarget = false;
    private Transform followTarget = null;

    // ����������
    private Vector3 targetPosition;
    private Vector3 velocity = Vector3.zero;
    private Vector3 offsetFromFocus = Vector3.zero;
    private Vector2 boundsX = new Vector2(-50, 50);
    private Vector2 boundsZ = new Vector2(-50, 50);

    void Start()
    {
        if (cam == null) cam = GetComponent<Camera>() ?? Camera.main;
        targetPosition = transform.position;

        if (focusTarget != null)
            offsetFromFocus = transform.position - focusTarget.position;

        if (boundsCollider != null)
            SetBoundsFromCollider(boundsCollider);

        // Автоматически находим SelectionManager если не назначен
        if (selectionManager == null)
            selectionManager = FindObjectOfType<SelectionManager>();

        // Автоматически находим ShipBuildingSystem если не назначен (поиск будет повторяться при необходимости)
        TryFindBuildingSystem();

        // Принудительно устанавливаем минимальный размер edge border
        edgeBorderSize = 5f;

    }

    void Update()
    {
        // Ищем ShipBuildingSystem если еще не найден
        if (buildingSystem == null)
            TryFindBuildingSystem();

        // Блокируем ввод если открыт инвентарь или игра на паузе (но НЕ во время строительства)
        if (!InventoryUI.IsAnyInventoryOpen && !IsGamePausedExceptBuildMode())
        {
            HandleInput();
        }

        // if (Input.GetKeyDown(focusKey))
        //     CenterOnTarget(); // ������ ������ Center
    }

    void LateUpdate()
    {
        // Блокируем зум если открыт инвентарь или игра на паузе (но НЕ во время строительства)
        if (!InventoryUI.IsAnyInventoryOpen && !IsGamePausedExceptBuildMode())
        {
            // Обрабатываем зум после того, как ShipBuildingSystem обработал ввод
            HandleZoom();
        }

        // Если активен режим следования за целью
        if (isFollowingTarget && followTarget != null)
        {
            // Используем разумное смещение камеры относительно персонажа (сверху-сзади)
            Vector3 desiredOffset = new Vector3(0, 10, -8);
            targetPosition = followTarget.position + desiredOffset;
            ClampToBounds();
        }

        // Камера движется плавно (используем unscaledDeltaTime чтобы работало даже на паузе строительства)
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
    }

    void HandleInput()
    {
        // Проверяем, активно ли box selection - если да, не двигаем камеру
        bool isSelectionActive = selectionManager != null && selectionManager.IsBoxSelecting;
        
        if (isSelectionActive)
        {
            // Во время box selection камера не двигается
            return;
        }
        
        // 1) WASD / Arrow keys (���������� ���������)
        float h = Input.GetAxisRaw("Horizontal"); // A/D, Left/Right
        float v = Input.GetAxisRaw("Vertical");   // W/S, Up/Down

        // ����� ������������ �� ��������� XZ, � ����������� "�����/�����" ������, �� ��� ������������ ������������
        Vector3 right = transform.right; right.y = 0; right.Normalize();
        Vector3 forward = transform.forward; forward.y = 0; forward.Normalize();

        Vector3 move = (right * h + forward * v);
        bool hasWASDInput = move.sqrMagnitude > 0.0001f;
        
        if (hasWASDInput)
        {
            targetPosition += move.normalized * panSpeed * Time.unscaledDeltaTime;
        }
        else
        {
            // 1.5) ��������� ���� � ����� ������ (������ ���� ��� WASD �����)
            Vector3 edgeInput = GetEdgeScrollInput();
            if (edgeInput.sqrMagnitude > 0.0001f)
            {
                Vector3 edgeMove = (right * edgeInput.x + forward * edgeInput.z);
                targetPosition += edgeMove.normalized * edgeScrollSpeed * Time.unscaledDeltaTime;
            }
        }

        // 2) ������������� ������� ������� ���� (������ ��������)
        if (Input.GetMouseButton(2))
        {
            float mx = -Input.GetAxis("Mouse X") * dragSpeed;
            float my = -Input.GetAxis("Mouse Y") * dragSpeed;
            Vector3 drag = right * mx + forward * my;
            targetPosition += drag;
        }

        ClampToBounds();
    }

    void HandleZoom()
    {
        if (cam == null) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            // Повторная попытка найти ShipBuildingSystem если не найден
            TryFindBuildingSystem();

            // Проверяем, активен ли режим строительства или ролик уже использован для поворота
            bool isBuildingMode = buildingSystem != null && buildingSystem.IsBuildingModeActive();
            bool scrollWheelUsed = buildingSystem != null && buildingSystem.IsScrollWheelUsedThisFrame();

            // Если режим строительства активен или ролик уже использован, блокируем зум камеры
            if (isBuildingMode || scrollWheelUsed)
            {

                return;
            }



            if (cam.orthographic)
            {
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - scroll * zoomSpeed, minOrthoSize, maxOrthoSize);
            }
            else
            {
                // ��� ����������� ����� �������� ������ ����� � forward
                targetPosition += transform.forward * scroll * zoomSpeed;
            }
            ClampToBounds();
        }
    }

    public void CenterOnTarget()
    {
        if (focusTarget == null)
        {

            return;
        }




        // Используем разумное смещение камеры относительно персонажа (сверху-сзади)
        Vector3 desiredOffset = new Vector3(0, 10, -8);
        targetPosition = focusTarget.position + desiredOffset;


        ClampToBounds();

    }

    private void ClampToBounds()
    {
        float x = Mathf.Clamp(targetPosition.x, boundsX.x, boundsX.y);
        float z = Mathf.Clamp(targetPosition.z, boundsZ.x, boundsZ.y);
        targetPosition = new Vector3(x, targetPosition.y, z);
    }

    // ������� ��� �������� ����������� ������ �� BoxCollider'� �������
    public void SetBoundsFromCollider(BoxCollider bc)
    {
        if (bc == null) return;
        Vector3 centerWorld = bc.transform.TransformPoint(bc.center);
        Vector3 sizeWorld = Vector3.Scale(bc.size, bc.transform.lossyScale);
        float halfX = sizeWorld.x * 0.5f;
        float halfZ = sizeWorld.z * 0.5f;
        boundsX.x = centerWorld.x - halfX;
        boundsX.y = centerWorld.x + halfX;
        boundsZ.x = centerWorld.z - halfZ;
        boundsZ.y = centerWorld.z + halfZ;
    }

    // ����� ��� ����������� ��������� ���� � ����� ������
    private Vector3 GetEdgeScrollInput()
    {
        if (!enableEdgeScrolling) return Vector3.zero;

        Vector3 mousePos = Input.mousePosition;
        Vector3 edgeInput = Vector3.zero;

        // Debug variables removed to eliminate warnings

        // �������� ������� ��� ����� �������
        if (mousePos.x <= edgeBorderSize)
        {
            edgeInput.x = -1f; // �����
        }
        else if (mousePos.x >= Screen.width - edgeBorderSize)
        {
            edgeInput.x = 1f;  // ������
        }

        if (mousePos.y <= edgeBorderSize)
        {
            edgeInput.z = -1f; // ����
        }
        else if (mousePos.y >= Screen.height - edgeBorderSize)
        {
            edgeInput.z = 1f;  // �����
        }

        // Debug logging disabled

        return edgeInput;
    }
    
    /// <summary>
    /// Попытаться найти ShipBuildingSystem
    /// </summary>
    void TryFindBuildingSystem()
    {
        if (buildingSystem == null)
        {
            buildingSystem = FindObjectOfType<ShipBuildingSystem>();
            if (buildingSystem != null)
            {

            }
            // Не выводим предупреждение - поиск будет повторяться автоматически
        }
    }

    // ����� �������� �� UI: ��������� Ship ����� ���
    public void SetFocusTarget(Transform t)
    {
        focusTarget = t;
        if (t != null) offsetFromFocus = transform.position - t.position;
    }

    /// <summary>
    /// Проверить находится ли игра на паузе
    /// </summary>
    bool IsGamePaused()
    {
        return GamePauseManager.Instance != null && GamePauseManager.Instance.IsPaused();
    }

    /// <summary>
    /// Проверить находится ли игра на паузе, исключая паузу режима строительства
    /// </summary>
    bool IsGamePausedExceptBuildMode()
    {
        if (GamePauseManager.Instance == null) return false;

        // Если игра не на паузе, то все в порядке
        if (!GamePauseManager.Instance.IsPaused()) return false;

        // Если пауза активна, проверяем - это пауза строительства или обычная пауза
        // Если это пауза строительства, возвращаем false (камера может двигаться)
        // Если это обычная пауза, возвращаем true (камера заблокирована)
        return !GamePauseManager.Instance.IsBuildModePause();
    }

    /// <summary>
    /// Начать следование камеры за целью (при зажатии ЛКМ на портрете)
    /// </summary>
    public void StartFollowingTarget(Transform target)
    {
        if (target == null) return;

        followTarget = target;
        isFollowingTarget = true;

        Debug.Log($"[CameraController] Started following {target.name}");
    }

    /// <summary>
    /// Остановить следование камеры за целью (при отпускании ЛКМ)
    /// </summary>
    public void StopFollowingTarget()
    {
        isFollowingTarget = false;
        followTarget = null;

        Debug.Log($"[CameraController] Stopped following target");
    }

    /// <summary>
    /// Проверить, следует ли камера за целью
    /// </summary>
    public bool IsFollowingTarget()
    {
        return isFollowingTarget;
    }
}
