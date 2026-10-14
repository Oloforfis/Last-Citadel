using UnityEngine;
using UnityEngine.UIElements;

public class CameraMover : MonoBehaviour
{
    [Header("Настройки скорости")]
    [Tooltip("Скорость движения камеры")]
    [SerializeField] private float moveSpeed = 1.0f;

    [Tooltip("Скорость перетаскивания мышью")]
    [SerializeField] private float moveDragSpeed = 0.02f;

    [Header("Движение от края экрана")]
    [Tooltip("Включить движение камеры при подведении курсора к краю экрана")]
    [SerializeField] private bool enableEdgePanning = true;

    [Tooltip("Ширина «активной зоны» у края экрана, в пикселях")]
    [SerializeField] private int edgeZonePixels = 15;

    [Tooltip("Множитель скорости при движении от края экрана")]
    [SerializeField] private float edgePanSpeedMultiplier = 1.5f;

    [Header("Движение зажатой средней кнопкой мыши")]
    [Tooltip("Включить перетаскивание камеры зажатом колёсике мыши")]
    [SerializeField] private bool enableMouseDragPanning = true;

    [Header("Движение стрелками клавиатуры")]
    [Tooltip("Включить движение камеры стрелками")]
    [SerializeField] private bool enableArrowKeyPanning = true;

    [Header("Ограничения")]
    [Tooltip("Не выходить за край карты")]
    [SerializeField] private bool clampToBounds = false;

    [SerializeField] private Vector2 minBounds = new Vector2(-100f, -100f); // X, Y 
    [SerializeField] private Vector2 maxBounds = new Vector2(100f, 100f);   // X, Y 

    private Transform _cachedTransform;
    private bool _isDragging;
    private Vector3 _lastMouseScreenPosition;
    private float _fixedZ;

    private void Awake()
    {
        _cachedTransform = transform;
    }

    private void LateUpdate()
    {
        Vector2 displacement = Vector2.zero;

        displacement += GetArrowKeyDelta();
        displacement += GetEdgePanDelta();
        displacement += GetMouseDragDelta();

        if (displacement.sqrMagnitude > 0.0001f)
        {
            Vector3 position = _cachedTransform.position;
            position.x += displacement.x;
            position.y += displacement.y;
            position.z = _fixedZ;

            if (clampToBounds)
            {
                position = ClampToAllowedBounds(position);
            }

            _cachedTransform.position = position;
        }
    }

    private Vector2 GetArrowKeyDelta()
    {
        if (!enableArrowKeyPanning)
            return Vector2.zero;

        float horizontal = 0f; // влево / вправо
        float vertical = 0f;   // вниз / вверх

        if (Input.GetKey(KeyCode.LeftArrow)) horizontal -= 1f;
        if (Input.GetKey(KeyCode.RightArrow)) horizontal += 1f;
        if (Input.GetKey(KeyCode.DownArrow)) vertical -= 1f;
        if (Input.GetKey(KeyCode.UpArrow)) vertical += 1f;

        Vector2 direction = new Vector2(horizontal, vertical).normalized;
        return direction * (moveSpeed * Time.deltaTime);
    }

    private Vector2 GetEdgePanDelta()
    {
        if (!enableEdgePanning || _isDragging)
            return Vector3.zero;

        Vector3 mousePos = Input.mousePosition;
        float horizontal = 0f;
        float vertical = 0f;

        if (mousePos.x <= edgeZonePixels)
            horizontal -= 1f;
        else if (mousePos.x >= Screen.width - edgeZonePixels)
            horizontal += 1f;

        if (mousePos.y <= edgeZonePixels)
            vertical -= 1f;
        else if (mousePos.y >= Screen.height - edgeZonePixels)
            vertical += 1f;

        Vector2 direction = new Vector2(horizontal, vertical).normalized;
        return direction * (moveSpeed * edgePanSpeedMultiplier * Time.deltaTime);
    }

    private Vector2 GetMouseDragDelta()
    {
        if (!enableMouseDragPanning)
            return Vector2.zero;

        // Средняя кнопка мыши — это button 2 (колесо).
        if (Input.GetMouseButtonDown(2))
        {
            _isDragging = true;
            _lastMouseScreenPosition = Input.mousePosition;
            return Vector2.zero;
        }

        if (Input.GetMouseButtonUp(2))
        {
            _isDragging = false;
            return Vector2.zero;
        }

        if (!_isDragging || !Input.GetMouseButton(2))
        {
            _isDragging = false;
            return Vector2.zero;
        }

        Vector3 currentMouseScreenPosition = Input.mousePosition;
        Vector3 screenDelta = currentMouseScreenPosition - _lastMouseScreenPosition;
        _lastMouseScreenPosition = currentMouseScreenPosition;

        // Двигаем камеру в направлении, противоположном движению мыши,
        // чтобы эффект был похож на «хватание мира».
        float worldUnitsPerPixel = GetWorldUnitsPerPixel();
        Vector2 worldDelta = new Vector2(-screenDelta.x, -screenDelta.y) * worldUnitsPerPixel;
        return worldDelta;
    }

    private float GetWorldUnitsPerPixel()
    {
        var cam = GetComponent<Camera>();

        if (cam != null && cam.orthographic && Screen.height > 0)
        {
            return (2f * cam.orthographicSize) / Screen.height;
        }

        float halfHeightAtDepth = Mathf.Tan(cam != null ? cam.fieldOfView * 0.5f * Mathf.Deg2Rad : 30f * Mathf.Deg2Rad)
                                  * Mathf.Abs(_fixedZ);
        return (2f * halfHeightAtDepth) / Screen.height;
    }

    private Vector3 ClampToAllowedBounds(Vector3 position)
    {
        position.x = Mathf.Clamp(position.x, minBounds.x, maxBounds.x);
        position.y = Mathf.Clamp(position.y, minBounds.y, maxBounds.y);
        return position;
    }

    private void OnValidate()
    {
        edgeZonePixels = Mathf.Max(1, edgeZonePixels);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        edgePanSpeedMultiplier = Mathf.Max(0f, edgePanSpeedMultiplier);

        Vector2 lo = new Vector2(Mathf.Min(minBounds.x, maxBounds.x), Mathf.Min(minBounds.y, maxBounds.y));
        Vector2 hi = new Vector2(Mathf.Max(minBounds.x, maxBounds.x), Mathf.Max(minBounds.y, maxBounds.y));
        minBounds = lo;
        maxBounds = hi;
    }
}
