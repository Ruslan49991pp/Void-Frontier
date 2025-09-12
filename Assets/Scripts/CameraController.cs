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
    public KeyCode focusKey = KeyCode.F;   // ������� ��� �������� � ������
    
    [Header("Mouse Edge Scrolling")]
    public bool enableEdgeScrolling = true; // �������� ��������� ����� ������
    public float edgeScrollSpeed = 15f;     // �������� ��������� ����� ������
    public float edgeBorderSize = 50f;      // ������ ������� � �������� ��� ���������
    
    [Header("Selection Integration")]
    public SelectionManager selectionManager;

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
    }

    void Update()
    {
        HandleInput();
        HandleZoom();

        if (Input.GetKeyDown(focusKey))
            CenterOnTarget();
    }

    void LateUpdate()
    {
        // ������ ���������� ������ � ������� �������
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
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
            targetPosition += move.normalized * panSpeed * Time.deltaTime;
        }
        else
        {
            // 1.5) ��������� ���� � ����� ������ (������ ���� ��� WASD �����)
            Vector3 edgeInput = GetEdgeScrollInput();
            if (edgeInput.sqrMagnitude > 0.0001f)
            {
                Vector3 edgeMove = (right * edgeInput.x + forward * edgeInput.z);
                targetPosition += edgeMove.normalized * edgeScrollSpeed * Time.deltaTime;
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
        if (focusTarget == null) return;
        // ��������� ������� �������� � ������ ���� ���, ����� ��������� ������������� ��������� ������
        targetPosition = focusTarget.position + offsetFromFocus;
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
        
        // �������� ������� ��� ����� �������
        if (mousePos.x <= edgeBorderSize)
            edgeInput.x = -1f; // �����
        else if (mousePos.x >= Screen.width - edgeBorderSize)
            edgeInput.x = 1f;  // ������
            
        if (mousePos.y <= edgeBorderSize)
            edgeInput.z = -1f; // ����
        else if (mousePos.y >= Screen.height - edgeBorderSize)
            edgeInput.z = 1f;  // �����
            
        return edgeInput;
    }
    
    // ����� �������� �� UI: ��������� Ship ����� ���
    public void SetFocusTarget(Transform t)
    {
        focusTarget = t;
        if (t != null) offsetFromFocus = transform.position - t.position;
    }
}
