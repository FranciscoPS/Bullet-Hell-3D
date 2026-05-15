using System.Collections;
using UnityEngine;

public class BossAI : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed    = 3f;
    [SerializeField] private float stopDistance = 8f;

    [Header("Attack Settings")]
    [SerializeField] private float attackDuration      = 3f;
    [SerializeField] private float timeBetweenAttacks  = 2f;
    [SerializeField] private float bulletForce         = 200f;
    [SerializeField] private float bulletDamage        = 10f;

    [Header("Circular Attack")]
    [Tooltip("Number of bullets per ring.")]
    [SerializeField] private int   circularBulletCount = 12;
    [Tooltip("Seconds between each ring burst.")]
    [SerializeField] private float circularFireRate    = 0.4f;

    [Header("Hexagonal Attack")]
    [Tooltip("Number of 6-bullet waves to fire.")]
    [SerializeField] private int   hexWaves        = 5;
    [Tooltip("Seconds between each hexagonal wave.")]
    [SerializeField] private float hexWaveInterval = 0.4f;
    [Tooltip("Extra rotation offset applied to each wave (degrees).")]
    [SerializeField] private float hexWaveRotation = 30f;

    [Header("Spiral Attack")]
    [Tooltip("Number of arms in the spiral.")]
    [SerializeField] private int   spiralArms          = 3;
    [Tooltip("Degrees rotated per second.")]
    [SerializeField] private float spiralRotationSpeed = 120f;
    [Tooltip("Seconds between each bullet volley.")]
    [SerializeField] private float spiralFireRate      = 0.08f;

    [Header("Bullet Pool")]
    [SerializeField] private GameObjectPool bulletPool;

    private Transform player;
    private Rigidbody rb;
    private Collider  bossCollider;
    private bool      isAttacking    = false;
    private float     nextAttackTime = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        bossCollider = GetComponent<Collider>();

        if (rb != null)
            rb.freezeRotation = true;
    }

    private void Start() { }

    private void FixedUpdate()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null) return;
            player = playerObj.transform;
        }

        if (isAttacking)
            return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > stopDistance)
        {
            ChasePlayer();
        }
        else
        {
            if (Time.time >= nextAttackTime)
                StartCoroutine(ExecuteRandomAttack());
        }
    }

    private void ChasePlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0f;
        rb.MovePosition(transform.position + dir * moveSpeed * Time.fixedDeltaTime);

        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    private IEnumerator ExecuteRandomAttack()
    {
        isAttacking = true;

        int pattern = Random.Range(0, 3);

        switch (pattern)
        {
            case 0: yield return StartCoroutine(CircularAttack());    break;
            case 1: yield return StartCoroutine(HexagonalAttack());   break;
            case 2: yield return StartCoroutine(SpiralAttack());      break;
        }

        nextAttackTime = Time.time + timeBetweenAttacks;
        isAttacking    = false;
    }

    private IEnumerator CircularAttack()
    {
        float elapsed = 0f;
        while (elapsed < attackDuration)
        {
            float angleStep = 360f / circularBulletCount;
            for (int i = 0; i < circularBulletCount; i++)
                SpawnBullet(AngleToDirection(i * angleStep));

            yield return new WaitForSeconds(circularFireRate);
            elapsed += circularFireRate;
        }
    }

    private IEnumerator HexagonalAttack()
    {
        float offset = 0f;
        for (int wave = 0; wave < hexWaves; wave++)
        {
            for (int i = 0; i < 6; i++)
                SpawnBullet(AngleToDirection(i * 60f + offset));

            offset += hexWaveRotation;
            yield return new WaitForSeconds(hexWaveInterval);
        }
    }

    private IEnumerator SpiralAttack()
    {
        float elapsed      = 0f;
        float currentAngle = 0f;
        float armStep      = 360f / spiralArms;

        while (elapsed < attackDuration)
        {
            for (int arm = 0; arm < spiralArms; arm++)
                SpawnBullet(AngleToDirection(currentAngle + arm * armStep));

            currentAngle += spiralRotationSpeed * spiralFireRate;
            yield return new WaitForSeconds(spiralFireRate);
            elapsed += spiralFireRate;
        }
    }

    private Vector3 AngleToDirection(float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
    }

    private void SpawnBullet(Vector3 direction)
    {
        if (bulletPool == null) return;

        GameObject bullet = bulletPool.GetGameObjectFromPool(transform.position);

        bullet.GetComponent<Projectile>()?.SetDamage(bulletDamage);

        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();

        Collider bulletCollider = bullet.GetComponent<Collider>();
        if (bulletCollider != null && bossCollider != null)
            Physics.IgnoreCollision(bulletCollider, bossCollider);

        bulletRb?.AddForce(direction * bulletForce);
    }
}
