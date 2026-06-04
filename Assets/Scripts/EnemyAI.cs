using System.Collections;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    private const int MaxCollisionIterations = 3;
    private const float MinMoveSqrMagnitude = 0.000001f;
    private const float MaxBlockingNormalY = 0.5f;

    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float shootRange = 8f;
    [SerializeField] private int projectilesPerBurst = 3;
    [SerializeField] private float timeBetweenShots = 0.3f;
    [SerializeField] private float timeBetweenBursts = 2f;
    [SerializeField] private float collisionSkin = 0.02f;

    private Gun gun;
    private Transform player;
    private Rigidbody rb;
    private Collider enemyCollider;
    private bool isShooting = false;
    private float nextBurstTime = 0f;
    private bool shouldChase;
    private Vector3 chaseDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        enemyCollider = GetComponent<Collider>();
        gun = GetComponent<Gun>();
    }

    public void SetPool(GameObjectPool bulletPool)
    {
        gun.SetPool(bulletPool);
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    private void Update()
    {
        if (player == null)
            return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;

        if (distance > shootRange)
        {
            shouldChase = toPlayer.sqrMagnitude > 0.001f;
            chaseDirection = shouldChase ? toPlayer.normalized : Vector3.zero;
            if (shouldChase)
                transform.rotation = Quaternion.LookRotation(chaseDirection);
        }
        else
        {
            shouldChase = false;
            chaseDirection = Vector3.zero;

            if (toPlayer.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(toPlayer.normalized);

            if (!isShooting && Time.time >= nextBurstTime)
                StartCoroutine(ShootBurst());
        }
    }

    private void FixedUpdate()
    {
        if (player == null || !shouldChase || chaseDirection.sqrMagnitude < 0.001f)
            return;

        MoveWithCollision(chaseDirection * moveSpeed * Time.fixedDeltaTime);
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
                slide = GetWallFollowSlide(hit.normal, remainingDistance);

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
        if (hitCollider == null || hitCollider == enemyCollider || hitCollider.isTrigger)
            return false;

        if (hitCollider.attachedRigidbody == rb)
            return false;

        if (Mathf.Abs(hit.normal.y) > MaxBlockingNormalY)
            return false;

        return !Physics.GetIgnoreLayerCollision(gameObject.layer, hitCollider.gameObject.layer);
    }

    private Vector3 GetWallFollowSlide(Vector3 normal, float distance)
    {
        normal.y = 0f;
        if (normal.sqrMagnitude <= MinMoveSqrMagnitude)
            return Vector3.zero;

        normal.Normalize();
        Vector3 tangent = Vector3.Cross(Vector3.up, normal);
        if (Vector3.Dot(tangent, chaseDirection) < 0f)
            tangent = -tangent;

        return tangent * distance;
    }

    private IEnumerator ShootBurst()
    {
        isShooting = true;

        if (gun == null)
        {
            isShooting = false;
            yield break;
        }

        for (int i = 0; i < projectilesPerBurst; i++)
        {
            if (player != null)
            {
                Vector3 diff = player.position - transform.position;
                diff.y = 0f;
                Vector3 direction = diff.sqrMagnitude > 0.001f ? diff.normalized : transform.forward;

                gun.Shoot(direction);
            }
            yield return new WaitForSeconds(timeBetweenShots);
        }

        nextBurstTime = Time.time + timeBetweenBursts;
        isShooting = false;
    }
}
