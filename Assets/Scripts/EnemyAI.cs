using System.Collections;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float shootRange = 8f;
    [SerializeField] private int projectilesPerBurst = 3;
    [SerializeField] private float timeBetweenShots = 0.3f;
    [SerializeField] private float shootForce = 120f;
    [SerializeField] private Transform muzzle;
    [SerializeField] private GameObjectPool pool;

    private Transform player;
    private Rigidbody rb;
    private Collider enemyCollider;
    private bool isShooting = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        enemyCollider = GetComponent<Collider>();
    }

    public void SetPool(GameObjectPool bulletPool)
    {
        pool = bulletPool;
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    private void FixedUpdate()
    {
        if (player == null || isShooting)
            return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > shootRange)
        {
            ChasePlayer();
        }
        else
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            StartCoroutine(ShootBurst());
        }
    }

    private void ChasePlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0f;

        rb.linearVelocity = new Vector3(direction.x * moveSpeed, rb.linearVelocity.y, direction.z * moveSpeed);

        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    private IEnumerator ShootBurst()
    {
        isShooting = true;

        for (int i = 0; i < projectilesPerBurst; i++)
        {
            FireProjectile();
            yield return new WaitForSeconds(timeBetweenShots);
        }

        isShooting = false;
    }

    private void FireProjectile()
    {
        if (player == null || pool == null) return;

        Transform spawnPoint = muzzle != null ? muzzle : transform;

        Vector3 directionToPlayer = (player.position - spawnPoint.position).normalized;
        spawnPoint.rotation = Quaternion.LookRotation(directionToPlayer);

        GameObject projectile = pool.GetGameObjectFromPool(spawnPoint.position);

        if (enemyCollider != null)
        {
            Collider projectileCollider = projectile.GetComponent<Collider>();
            if (projectileCollider != null)
                Physics.IgnoreCollision(projectileCollider, enemyCollider);
        }

        Rigidbody projRb = projectile.GetComponent<Rigidbody>();
        projRb?.AddForce(directionToPlayer * shootForce);
    }
}
