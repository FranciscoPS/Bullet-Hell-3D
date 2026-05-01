using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private Gun gun;

    private Rigidbody rb;
    private InputAction moveAction;
    private InputAction shootAction;
    private Vector3 aimDirection = Vector3.forward;

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
        shootAction.Enable();
    }

    private void OnDisable()
    {
        shootAction.Disable();
    }

    public void SetGun(Gun assignedGun)
    {
        gun = assignedGun;
    }

    private void OnShoot(InputAction.CallbackContext context) { }

    private void Update()
    {
        AimAtMouse();

        if (shootAction.IsPressed())
            gun?.Shoot(aimDirection);
    }

    private void AimAtMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane groundPlane = new Plane(Vector3.up, transform.position);

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 worldPoint = ray.GetPoint(distance);
            Vector3 dir = (worldPoint - transform.position);
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.01f)
            {
                aimDirection = dir.normalized;
                transform.rotation = Quaternion.LookRotation(aimDirection);
            }
        }
    }

    private void FixedUpdate()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        rb.linearVelocity = new Vector3(moveDirection.x * moveSpeed, rb.linearVelocity.y, moveDirection.z * moveSpeed);
    }
}

