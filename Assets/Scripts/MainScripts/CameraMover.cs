using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMover : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Скорость передвижения")]
    public float moveSpeed = 20f;

    [Tooltip("Чувствительность при перетаскивании колсиком")]
    public float dragSensitivity = 0.5f;

    [Header("Настройки края экрана")]
    [Tooltip("Зона передвижения за край")]
    public float edgeThreshold = 20f;

    [Tooltip("Двигать, если зажата СКМ?")]
    public bool ignoreEdgeWhenDragging = true;

    private Vector3 moveDirection;

    private void Update()
    {
        moveDirection = Vector3.zero;

        var mouse = Mouse.current;
        var keyboard = Keyboard.current;

        bool isMiddleMouseHeld = mouse.middleButton.isPressed;

        // Средняя кнопка мыши
        if(isMiddleMouseHeld)
        {
            Vector2 mouseDelta = mouse.delta.ReadValue();
            moveDirection -= new Vector3(mouseDelta.x, 0f, mouseDelta.y) * dragSensitivity;
        }

        // Края экрана
        if (!isMiddleMouseHeld || !ignoreEdgeWhenDragging)
        {
            Vector2 mousePos = mouse.position.ReadValue();

            if (mousePos.x >= Screen.width - edgeThreshold)
                moveDirection += Vector3.right;
            else if (mousePos.x <= edgeThreshold)
                moveDirection += Vector3.left;

            if (mousePos.y >= Screen.height - edgeThreshold)
                moveDirection += Vector3.forward;
            else if (mousePos.y <= edgeThreshold)
                moveDirection += Vector3.back;
        }

        // Стрелочки
        if (keyboard != null)
        {
            if (keyboard[Key.UpArrow].isPressed) moveDirection += Vector3.forward;
            if (keyboard[Key.DownArrow].isPressed) moveDirection += Vector3.back;
            if (keyboard[Key.LeftArrow].isPressed) moveDirection += Vector3.left;
            if (keyboard[Key.RightArrow].isPressed) moveDirection += Vector3.right;
        }

        transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
    }
}