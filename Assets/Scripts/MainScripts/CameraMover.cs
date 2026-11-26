using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMover : MonoBehaviour
{
    [Header("Скорости")]
    [SerializeField] private float keySpeed = 10f;        // юнитов в секунду (стрелки)
    [SerializeField] private float edgeSpeed = 15f;       // юнитов в секунду (края экрана)
    [SerializeField] private int edgePixels = 15;         // ширина зоны у края

    private Transform tr;
    private Camera cam;
    private Vector2 lastMousePos;
    private bool dragging;
    private float fixedY;

    void Awake()
    {
        tr = transform;
        cam = GetComponent<Camera>();
        fixedY = tr.position.y;
    }

    void LateUpdate()
    {
        Vector2 delta = ArrowDelta() + EdgeDelta() + DragDelta();

        if (delta.sqrMagnitude > 0f)
        {
            Vector3 pos = tr.position;
            pos.x += delta.x;
            pos.z += delta.y;
            pos.y = fixedY;
            tr.position = pos;
        }
    }

    // Стрелки
    Vector2 ArrowDelta()
    {
        float x = 0f, z = 0f;
        if (Keyboard.current.leftArrowKey.isPressed) x -= 1f;
        if (Keyboard.current.rightArrowKey.isPressed) x += 1f;
        if (Keyboard.current.downArrowKey.isPressed) z -= 1f;
        if (Keyboard.current.upArrowKey.isPressed) z += 1f;

        return new Vector2(x, z).normalized * (keySpeed * Time.deltaTime);
    }

    // Края экрана
    Vector2 EdgeDelta()
    {
        if (dragging) return Vector2.zero;

        Vector2 m = Mouse.current.position.ReadValue();
        float x = 0f, z = 0f;

        if (m.x <= edgePixels) x -= 1f;
        else if (m.x >= Screen.width - edgePixels) x += 1f;

        if (m.y <= edgePixels) z -= 1f;
        else if (m.y >= Screen.height - edgePixels) z += 1f;

        return new Vector2(x, z).normalized * (edgeSpeed * Time.deltaTime);
    }

    // Зажатая средняя кнопка мыши
    Vector2 DragDelta()
    {
        if (!Mouse.current.middleButton.isPressed)
        {
            dragging = false;
            return Vector2.zero;
        }

        Vector2 m = Mouse.current.position.ReadValue();

        if (!dragging)
        {
            dragging = true;
            lastMousePos = m;
            return Vector2.zero;
        }

        Vector2 screenDelta = m - lastMousePos;
        lastMousePos = m;

        // Пиксели -> мировые юниты (для ортографической камеры)
        float unitsPerPixel = cam.orthographic
            ? 2f * cam.orthographicSize / Screen.height
            : 2f * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * tr.position.y / Screen.height;

        return new Vector2(-screenDelta.x, -screenDelta.y) * unitsPerPixel;
    }
}
