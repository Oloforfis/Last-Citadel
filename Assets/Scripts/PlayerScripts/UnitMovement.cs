using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class UnitMovement : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float rotationSpeed = 20f;
    [SerializeField] private LayerMask groundLayer;

    private float verticalVelocity = 0f;
    [SerializeField] private float gravity = -20f;

    private CharacterController controller;
    private Vector3 targetPosition;
    private Camera mainCamera;

    // Действия ввода
    private InputAction moveAction;
    private InputAction rightClickAction;

    // Состояния
    private bool isMovingToClickPoint = false;
    private bool isUsingManualWASD = false;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        mainCamera = Camera.main;
        targetPosition = transform.position;

        var actions = new InputSystem_Actions();

        moveAction = actions.Player.Move;
        rightClickAction = actions.UI.RightClick;

        rightClickAction.performed += OnRightClick;
        actions.Enable();
    }

    void OnDestroy()
    {
        if (moveAction != null) moveAction.Disable();
        if (rightClickAction != null) rightClickAction.Disable();
    }

    void Update()
    {
        HandleInput();
        Move();
        AlwaysLookAtMouse();
        ApplyGravity();
    }

    void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;
        controller.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
    }

    void HandleInput()
    {
        Vector2 inputVector = moveAction.ReadValue<Vector2>();
        bool isWASDPressed = inputVector.magnitude >= 0.1f;

        if (isWASDPressed)
        {
            isUsingManualWASD = true;
        }
    }

    void OnRightClick(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        isUsingManualWASD = false;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
        {
            targetPosition = new Vector3(hit.point.x, transform.position.y, hit.point.z);
            isMovingToClickPoint = true;
        }
    }

    void Move()
    {
        if (PlayerActionState.BlockMovement) return;

        Vector2 inputVector = moveAction.ReadValue<Vector2>();
        Vector3 moveDirection = new Vector3(inputVector.x, 0f, inputVector.y).normalized;

        // 1. Приоритет: Ручное управление WASD
        if (moveDirection.magnitude >= 0.1f)
        {
            // Двигаем строго по вектору WASD (относительно мира)
            controller.Move(moveDirection * speed * PlayerActionState.SpeedMultiplier * Time.deltaTime);
            isMovingToClickPoint = false; // Прерываем путь к точке при ручном вводе
            return;
        }

        // 2. Авто-навигация к точке клика
        if (!isUsingManualWASD && isMovingToClickPoint)
        {
            Vector3 directionToTarget = (targetPosition - transform.position).normalized;

            if (directionToTarget.magnitude > 0.1f)
            {
                controller.Move(directionToTarget * speed * PlayerActionState.SpeedMultiplier * Time.deltaTime);

                if (Vector3.Distance(transform.position, targetPosition) < 0.3f)
                {
                    transform.position = targetPosition;
                    isMovingToClickPoint = false;
                }
            }
            else
            {
                isMovingToClickPoint = false;
            }
            return;
        }
    }

    void AlwaysLookAtMouse()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
        {
            Vector3 lookPoint = new Vector3(hit.point.x, transform.position.y, hit.point.z);

            Vector3 directionToMouse = (lookPoint - transform.position).normalized;

            if (directionToMouse.magnitude < 0.01f) return;

            float angle = Mathf.Atan2(directionToMouse.x, directionToMouse.z) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0f, angle, 0f);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}
