using System.Collections;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float shootRange = 8f;
    [SerializeField] private int projectilesPerBurst = 3;
    [SerializeField] private float timeBetweenShots = 0.3f;
    [SerializeField] private float timeBetweenBursts = 2f;

    private Gun gun;
    private Transform player;
    private Rigidbody rb;
    private Collider enemyCollider;
    private bool isShooting = false;
    private float nextBurstTime = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
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
            if (!isShooting && Time.time >= nextBurstTime)
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
            if (player != null)
            {
                Vector3 direction = (player.position - transform.position).normalized;
                direction.y = 0f;
                gun.Shoot(direction);
            }
            yield return new WaitForSeconds(timeBetweenShots);
        }

        nextBurstTime = Time.time + timeBetweenBursts;
        isShooting = false;
    }
}
