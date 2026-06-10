using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private const int MaxCollisionIterations = 3;
    private const float MinMoveSqrMagnitude = 0.000001f;
    private const float MaxBlockingNormalY = 0.5f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private Gun gun;
    [SerializeField] private float moveInputDeadzone = 0.1f;
    [SerializeField] private float collisionSkin = 0.02f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 28f;
    [SerializeField] private float dashDuration = 0.18f;
    [SerializeField] private float dashCooldown = 0.75f;

    private Animator[] animators;

    private Rigidbody rb;
    private Collider playerCollider;
    private PlayerHealth playerHealth;
    private InputAction moveAction;
    private InputAction shootAction;
    private InputAction dashAction;
    private InputAction pauseAction;
    private Vector3 aimDirection = Vector3.forward;
    private Vector3 dashDirection;
    private bool isPaused;
    private bool isDashing;
    private bool isMoving;
    private float dashTimeRemaining;
    private float nextDashReadyTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();
        playerHealth = GetComponent<PlayerHealth>();
        animators = GetComponentsInChildren<Animator>(true);

        rb.isKinematic = true;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        PlayerInput playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        shootAction = playerInput.actions["Shoot"];
        dashAction = playerInput.actions["Dash"];
        pauseAction = playerInput.actions["Pause"];
    }

    private void OnEnable()
    {
        shootAction.Enable();
        dashAction.Enable();
        pauseAction.Enable();
    }

    private void OnDisable()
    {
        EndDash();
        shootAction.Disable();
        dashAction.Disable();
        pauseAction.Disable();
    }

    public void SetGun(Gun assignedGun)
    {
        gun = assignedGun;
    }

    private void OnShoot(InputAction.CallbackContext context) { }

    private void Update()
    {
        /*if (pauseAction.WasPressedThisFrame())
        {
            TogglePause();
            return;
        }*/

        if (isPaused)
            return;

        AimAtMouse();

        if (dashAction.WasPressedThisFrame())
            TryStartDash();

        if (shootAction.IsInProgress())
            gun?.Shoot(aimDirection);
    }

    /*public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
    }*/

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

        bool currentlyMoving =
            moveInput.sqrMagnitude >= moveInputDeadzone * moveInputDeadzone;

        if (currentlyMoving != isMoving)
        {
            isMoving = currentlyMoving;

            foreach (Animator anim in animators)
            {
                if (anim != null)
                    anim.SetBool("isMoving", isMoving);
            }
        }

        if (!currentlyMoving)
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
        if (delta.sqrMagnitude <= MinMoveSqrMagnitude)
            return;

        Vector3 targetPosition = rb.position;
        Vector3 remaining = delta;

        for (int i = 0; i < MaxCollisionIterations; i++)
        {
            float distance = remaining.magnitude;
            if (distance <= 0f)
                break;

            Vector3 direction = remaining / distance;
            if (!TrySweepMovement(direction, distance + collisionSkin, out RaycastHit hit))
            {
                targetPosition += remaining;
                break;
            }

            float moveDistance = Mathf.Min(distance, Mathf.Max(0f, hit.distance - collisionSkin));
            targetPosition += direction * moveDistance;

            float remainingDistance = distance - moveDistance;
            if (remainingDistance <= collisionSkin)
                break;

            Vector3 slide = Vector3.ProjectOnPlane(direction * remainingDistance, hit.normal);
            slide.y = 0f;

            if (slide.sqrMagnitude <= MinMoveSqrMagnitude)
                break;

            remaining = slide;
        }

        rb.MovePosition(targetPosition);
    }

    private bool TrySweepMovement(Vector3 direction, float distance, out RaycastHit closestHit)
    {
        RaycastHit[] hits = rb.SweepTestAll(direction, distance, QueryTriggerInteraction.Ignore);
        closestHit = default;
        bool foundHit = false;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (!IsBlockingHit(hit) || hit.distance >= closestDistance)
                continue;

            closestDistance = hit.distance;
            closestHit = hit;
            foundHit = true;
        }

        return foundHit;
    }

    private bool IsBlockingHit(RaycastHit hit)
    {
        Collider hitCollider = hit.collider;
        if (hitCollider == null || hitCollider == playerCollider || hitCollider.isTrigger)
            return false;

        if (hitCollider.attachedRigidbody == rb)
            return false;

        if (hitCollider.attachedRigidbody != null)
            return false;

        if (Mathf.Abs(hit.normal.y) > MaxBlockingNormalY)
            return false;

        return !Physics.GetIgnoreLayerCollision(gameObject.layer, hitCollider.gameObject.layer);
    }
}
