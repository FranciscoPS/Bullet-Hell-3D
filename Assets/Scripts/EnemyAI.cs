using System.Collections;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
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
