using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private Gun gun;

    private Rigidbody rb;
    private InputAction moveAction;
    private InputAction shootAction;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        PlayerInput playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        shootAction = playerInput.actions["Shoot"];
    }

    private void OnEnable()
    {
        shootAction.performed += OnShoot;
    }

    private void OnDisable()
    {
        shootAction.performed -= OnShoot;
    }

    private void OnShoot(InputAction.CallbackContext context)
    {
        gun?.Shoot();
    }

    private void FixedUpdate()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        rb.linearVelocity = new Vector3(moveDirection.x * moveSpeed, rb.linearVelocity.y, moveDirection.z * moveSpeed);

        if (moveDirection.magnitude >= 0.1f)
            transform.rotation = Quaternion.LookRotation(moveDirection);
    }
}

