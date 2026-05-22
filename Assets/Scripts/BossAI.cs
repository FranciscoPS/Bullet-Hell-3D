using System.Collections;
using UnityEngine;

public class BossAI : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed    = 3f;
    [SerializeField] private float stopDistance = 8f;
    [SerializeField] private float collisionSkin = 0.02f;

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

    [Header("Phase 2 — Embestida")]
    [SerializeField] private float dashSpeed         = 20f;
    [SerializeField] private float dashDuration      = 0.4f;
    [SerializeField] private float dashDamage        = 20f;
    [SerializeField] private float dashContactRadius = 1.5f;

    [Header("Phase 3 — Proyectil Buscador")]
    [SerializeField] private int   seekingProjectileCount = 5;
    [SerializeField] private float seekingProjectileDuration = 3f;
    [SerializeField] private float seekingProjectileSpeed = 15f;

    private Transform player;
    private Rigidbody rb;
    private Collider  bossCollider;
    private bool      isAttacking    = false;
    private float     nextAttackTime = 0f;
    private bool      isPhaseTwo        = false;
    private bool      phaseTwoTriggered = false;
    private bool      isPhaseThree       = false;
    private bool      phaseThreeTriggered = false;
    private BossHealth bossHealth;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        bossCollider = GetComponent<Collider>();
        bossHealth   = GetComponent<BossHealth>();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }
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

        if (!phaseTwoTriggered && bossHealth != null && bossHealth.HealthRatio <= 0.5f)
        {
            isPhaseTwo        = true;
            phaseTwoTriggered = true;
            Debug.Log("<color=red>[BOSS] ¡FASE 2 ACTIVADA! — EMBESTIDA DESBLOQUEADA</color>");
        }

        if (!phaseThreeTriggered && bossHealth != null && bossHealth.HealthRatio <= 0.25f)
        {
            isPhaseThree = true;
            phaseThreeTriggered = true;
            Debug.Log("<color=red>[BOSS] ¡FASE 3 ACTIVADA! — PROYECTILES BUSCADORES DESBLOQUEADOS</color>");
        }

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;

        if (distance > stopDistance)
        {
            ChasePlayer();
        }
        else
        {
            if (toPlayer.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(toPlayer.normalized);

            if (!isAttacking && Time.time >= nextAttackTime)
                StartCoroutine(ExecuteRandomAttack());
        }
    }

    private void ChasePlayer()
    {
        Vector3 diff = player.position - transform.position;
        diff.y = 0f;
        if (diff.sqrMagnitude < 0.001f) return;
        Vector3 dir = diff.normalized;
        MoveWithCollision(dir * moveSpeed * Time.fixedDeltaTime);
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

        if (isPhaseTwo)
            yield return StartCoroutine(DashAttack());

        if (isPhaseThree)
            yield return StartCoroutine(SeekingProjectileAttack());

        nextAttackTime = Time.time + timeBetweenAttacks;
        isAttacking    = false;
    }

    private IEnumerator DashAttack()
    {
        if (player == null) yield break;

        Vector3 dashDir = player.position - transform.position;
        dashDir.y = 0f;
        if (dashDir.sqrMagnitude < 0.001f) yield break;
        dashDir.Normalize();

        bool damageDealt = false;
        float elapsed    = 0f;

        while (elapsed < dashDuration)
        {
            MoveWithCollision(dashDir * dashSpeed * Time.fixedDeltaTime);

            if (!damageDealt && player != null)
            {
                Vector3 toPlayer = player.position - transform.position;
                toPlayer.y = 0f;
                if (toPlayer.magnitude <= dashContactRadius)
                {
                    PlayerHealth ph = player.GetComponent<PlayerHealth>()
                                   ?? player.GetComponentInParent<PlayerHealth>();
                    ph?.TakeDamage(dashDamage);
                    damageDealt = true;
                }
            }

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
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

    private IEnumerator SeekingProjectileAttack()
    {
        if (player == null) yield break;

        for (int i = 0; i < seekingProjectileCount; i++)
        {
            SpawnSeekingBullet();
            yield return new WaitForSeconds(0.15f);
        }
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

    private void SpawnSeekingBullet()
    {
        if (bulletPool == null || player == null) return;

        GameObject bullet = bulletPool.GetGameObjectFromPool(transform.position);

        Projectile proj = bullet.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.SetDamage(bulletDamage);
            proj.SetSeeking(player, seekingProjectileDuration, seekingProjectileSpeed);
        }

        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();

        Collider bulletCollider = bullet.GetComponent<Collider>();
        if (bulletCollider != null && bossCollider != null)
            Physics.IgnoreCollision(bulletCollider, bossCollider);

        Vector3 initialDir = (player.position - transform.position).normalized;
        initialDir.y = 0f;
        bulletRb?.AddForce(initialDir * (bulletForce * 0.5f));
    }
}
