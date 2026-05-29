using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private Gun gun;
    [SerializeField] private float moveInputDeadzone = 0.1f;
    [SerializeField] private float collisionSkin = 0.02f;

    private Rigidbody rb;
    private InputAction moveAction;
    private InputAction shootAction;
    private Vector3 aimDirection = Vector3.forward;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

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

        if (shootAction.IsInProgress())
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
        if (moveInput.sqrMagnitude < moveInputDeadzone * moveInputDeadzone)
            return;

        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        MoveWithCollision(moveDirection * moveSpeed * Time.fixedDeltaTime);
    }

    private void MoveWithCollision(Vector3 delta)
    {
        float distance = delta.magnitude;
        if (distance <= 0f)
            return;

        Vector3 direction = delta / distance;
        Vector3 targetPosition = rb.position;

        if (rb.SweepTest(direction, out RaycastHit hit, distance + collisionSkin, QueryTriggerInteraction.Ignore))
        {
            float moveToContact = Mathf.Max(0f, hit.distance - collisionSkin);
            targetPosition += direction * moveToContact;

            Vector3 remaining = delta - direction * moveToContact;
            Vector3 slide = Vector3.ProjectOnPlane(remaining, hit.normal);
            targetPosition += slide;
        }
        else
        {
            targetPosition += delta;
        }

        rb.MovePosition(targetPosition);
    }
}
