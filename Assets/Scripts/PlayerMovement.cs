using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private Gun gun;
    [SerializeField] private float moveInputDeadzone = 0.1f;
    [SerializeField] private float collisionSkin = 0.02f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 28f;
    [SerializeField] private float dashDuration = 0.18f;
    [SerializeField] private float dashCooldown = 0.75f;

    private Rigidbody rb;
    private PlayerHealth playerHealth;
    private InputAction moveAction;
    private InputAction shootAction;
    private InputAction dashAction;
    private bool ownsDashAction;
    private Vector3 aimDirection = Vector3.forward;
    private Vector3 dashDirection;
    private bool isDashing;
    private float dashTimeRemaining;
    private float nextDashReadyTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerHealth = GetComponent<PlayerHealth>();

        rb.isKinematic = true;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        PlayerInput playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        shootAction = playerInput.actions["Shoot"];
        dashAction = playerInput.actions["Dash"];
    }

    private void OnEnable()
    {
        shootAction.Enable();
        dashAction.Enable();
    }

    private void OnDisable()
    {
        EndDash();
        shootAction.Disable();
        dashAction.Disable();
    }

    private void OnDestroy()
    {
        if (ownsDashAction)
            dashAction.Dispose();
    }

    public void SetGun(Gun assignedGun)
    {
        gun = assignedGun;
    }

    private void OnShoot(InputAction.CallbackContext context) { }

    private void Update()
    {
        AimAtMouse();

        if (dashAction.WasPressedThisFrame())
            TryStartDash();

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
        if (isDashing)
        {
            UpdateDash();
            return;
        }

        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        if (moveInput.sqrMagnitude < moveInputDeadzone * moveInputDeadzone)
            return;

        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        MoveWithCollision(moveDirection * moveSpeed * Time.fixedDeltaTime);
    }

    private void TryStartDash()
    {
        if (isDashing || Time.time < nextDashReadyTime)
            return;

        float duration = Mathf.Max(0f, dashDuration);
        float speed = Mathf.Max(0f, dashSpeed);
        if (duration <= 0f || speed <= 0f)
            return;

        Vector3 direction = GetDashDirection();
        if (direction.sqrMagnitude < 0.001f)
            return;

        dashDirection = direction.normalized;
        dashTimeRemaining = duration;
        isDashing = true;
        playerHealth?.SetInvulnerable(true);
    }

    private Vector3 GetDashDirection()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y);

        if (direction.sqrMagnitude >= moveInputDeadzone * moveInputDeadzone)
            return direction;

        return aimDirection.sqrMagnitude > 0.001f ? aimDirection : transform.forward;
    }

    private void UpdateDash()
    {
        float stepTime = Mathf.Min(Time.fixedDeltaTime, dashTimeRemaining);
        MoveWithCollision(dashDirection * Mathf.Max(0f, dashSpeed) * stepTime);

        dashTimeRemaining -= stepTime;
        if (dashTimeRemaining <= 0f)
            EndDash();
    }

    private void EndDash()
    {
        if (!isDashing)
            return;

        isDashing = false;
        dashTimeRemaining = 0f;
        nextDashReadyTime = Time.time + Mathf.Max(0f, dashCooldown);
        playerHealth?.SetInvulnerable(false);
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
