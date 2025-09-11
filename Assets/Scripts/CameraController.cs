using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("References")]
    public Camera cam;                     // если ничего не назначено, будет Camera.main
    public Transform focusTarget;          // перетащи сюда Ship (или вызови SetFocusTarget)
    public BoxCollider boundsCollider;     // опционально: объект с BoxCollider, задающий границы локации

    [Header("Movement")]
    public float panSpeed = 20f;           // скорость WASD
    public float dragSpeed = 0.6f;         // скорость при перетягивании средней кнопкой
    public float smoothTime = 0.08f;       // сглаживание движения

    [Header("Zoom (Orthographic)")]
    public float zoomSpeed = 10f;
    public float minOrthoSize = 6f;
    public float maxOrthoSize = 30f;

    [Header("Focus / Controls")]
    public KeyCode focusKey = KeyCode.F;   // клавиша для возврата к фокусу

    // внутреннее
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
        // плавно перемещаем камеру к целевой позиции
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }

    void HandleInput()
    {
        // 1) WASD / Arrow keys
        float h = Input.GetAxisRaw("Horizontal"); // A/D, Left/Right
        float v = Input.GetAxisRaw("Vertical");   // W/S, Up/Down

        // хотим перемещаться по плоскости XZ, в направлении "вперёд/право" камеры, но без вертикальной составляющей
        Vector3 right = transform.right; right.y = 0; right.Normalize();
        Vector3 forward = transform.forward; forward.y = 0; forward.Normalize();

        Vector3 move = (right * h + forward * v);
        if (move.sqrMagnitude > 0.0001f)
            targetPosition += move.normalized * panSpeed * Time.deltaTime;

        // 2) Перетягивание средней кнопкой мыши (нажать колесико)
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
                // для перспективы можно сдвигать камеру вдоль её forward
                targetPosition += transform.forward * scroll * zoomSpeed;
            }
            ClampToBounds();
        }
    }

    public void CenterOnTarget()
    {
        if (focusTarget == null) return;
        // сохраняем текущее смещение и ставим цель так, чтобы сохранить относительное положение камеры
        targetPosition = focusTarget.position + offsetFromFocus;
        ClampToBounds();
    }

    private void ClampToBounds()
    {
        float x = Mathf.Clamp(targetPosition.x, boundsX.x, boundsX.y);
        float z = Mathf.Clamp(targetPosition.z, boundsZ.x, boundsZ.y);
        targetPosition = new Vector3(x, targetPosition.y, z);
    }

    // утилита для быстрого выставления границ из BoxCollider'а локации
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

    // можно вызывать из UI: назначить Ship через код
    public void SetFocusTarget(Transform t)
    {
        focusTarget = t;
        if (t != null) offsetFromFocus = transform.position - t.position;
    }
}
