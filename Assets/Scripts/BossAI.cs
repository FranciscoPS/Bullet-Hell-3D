using System.Collections;
using UnityEngine;

/// <summary>
/// Boss Phase 1 AI: chases the player, stops ~8 m away and randomly fires
/// one of three bullet-hell attack patterns using the shared pool system.
/// </summary>
public class BossAI : MonoBehaviour
{
    // ── Movement ─────────────────────────────────────────────────────────────
    [Header("Movement")]
    [SerializeField] private float moveSpeed    = 3f;
    [SerializeField] private float stopDistance = 8f;

    // ── General attack settings ───────────────────────────────────────────────
    [Header("Attack Settings")]
    [SerializeField] private float attackDuration      = 3f;
    [SerializeField] private float timeBetweenAttacks  = 2f;
    [SerializeField] private float bulletForce         = 200f;
    [SerializeField] private float bulletDamage        = 10f;

    // ── Circular attack ───────────────────────────────────────────────────────
    [Header("Circular Attack")]
    [Tooltip("Number of bullets per ring.")]
    [SerializeField] private int   circularBulletCount = 12;
    [Tooltip("Seconds between each ring burst.")]
    [SerializeField] private float circularFireRate    = 0.4f;

    // ── Hexagonal attack ──────────────────────────────────────────────────────
    [Header("Hexagonal Attack")]
    [Tooltip("Number of 6-bullet waves to fire.")]
    [SerializeField] private int   hexWaves        = 5;
    [Tooltip("Seconds between each hexagonal wave.")]
    [SerializeField] private float hexWaveInterval = 0.4f;
    [Tooltip("Extra rotation offset applied to each wave (degrees).")]
    [SerializeField] private float hexWaveRotation = 30f;

    // ── Spiral attack ─────────────────────────────────────────────────────────
    [Header("Spiral Attack")]
    [Tooltip("Number of arms in the spiral.")]
    [SerializeField] private int   spiralArms          = 3;
    [Tooltip("Degrees rotated per second.")]
    [SerializeField] private float spiralRotationSpeed = 120f;
    [Tooltip("Seconds between each bullet volley.")]
    [SerializeField] private float spiralFireRate      = 0.08f;

    // ── Pool ──────────────────────────────────────────────────────────────────
    [Header("Bullet Pool")]
    [SerializeField] private GameObjectPool bulletPool;

    // ── Internal state ────────────────────────────────────────────────────────
    private Transform player;
    private Rigidbody rb;
    private Collider  bossCollider;
    private bool      isAttacking    = false;
    private float     nextAttackTime = 0f;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            Debug.LogError("[BossAI] Falta el componente Rigidbody en el boss.", this);

        bossCollider = GetComponent<Collider>();
        if (bossCollider == null)
            Debug.LogError("[BossAI] Falta un Collider en el boss.", this);

        if (rb != null)
            rb.freezeRotation = true;

        if (bulletPool == null)
            Debug.LogError("[BossAI] 'Bullet Pool' no está asignado en el Inspector.", this);
    }

    private void Start()
    {
        // El jugador puede ser spawneado después que el boss, así que
        // la búsqueda se reintenta en FixedUpdate hasta encontrarlo.
    }

    private void FixedUpdate()
    {
        // Reintenta encontrar al jugador cada frame hasta que aparezca (puede ser spawneado tarde).
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null) return;
            player = playerObj.transform;
            Debug.Log($"[BossAI] Jugador encontrado: {player.name}");
        }

        // Cancela cualquier fuerza horizontal externa cada frame (ej: impacto de balas del jugador).
        // El eje Y se preserva para que la gravedad funcione correctamente.
        if (isAttacking)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > stopDistance)
        {
            ChasePlayer();
        }
        else
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

            if (Time.time >= nextAttackTime)
                StartCoroutine(ExecuteRandomAttack());
        }
    }

    // ── Movement ──────────────────────────────────────────────────────────────

    private void ChasePlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0f;
        rb.linearVelocity = new Vector3(dir.x * moveSpeed, rb.linearVelocity.y, dir.z * moveSpeed);

        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    // ── Attack selection ──────────────────────────────────────────────────────

    private IEnumerator ExecuteRandomAttack()
    {
        isAttacking = true;

        int pattern = Random.Range(0, 3);
        string[] names = { "Circular", "Hexagonal", "Spiral" };
        Debug.Log($"[BossAI] Iniciando ataque: {names[pattern]}");

        switch (pattern)
        {
            case 0: yield return StartCoroutine(CircularAttack());    break;
            case 1: yield return StartCoroutine(HexagonalAttack());   break;
            case 2: yield return StartCoroutine(SpiralAttack());      break;
        }

        Debug.Log($"[BossAI] Ataque {names[pattern]} terminado. Próximo ataque en {timeBetweenAttacks}s.");
        nextAttackTime = Time.time + timeBetweenAttacks;
        isAttacking    = false;
    }

    // ── Circular attack ───────────────────────────────────────────────────────
    // Fires 'circularBulletCount' bullets evenly spread around 360°,
    // repeating every 'circularFireRate' seconds for 'attackDuration' seconds.

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

    // ── Hexagonal attack ──────────────────────────────────────────────────────
    // Fires 6 bullets at 60° intervals per wave.
    // Each wave is rotated by 'hexWaveRotation' degrees relative to the previous.

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

    // ── Spiral attack ─────────────────────────────────────────────────────────
    // Fires 'spiralArms' bullets, rotating the origin angle over time
    // to create a continuous rotating spiral effect.

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

    // ── Shared helpers ────────────────────────────────────────────────────────

    /// <summary>Converts a yaw angle (degrees) to a flat XZ direction vector.</summary>
    private Vector3 AngleToDirection(float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
    }

    /// <summary>Gets a bullet from the pool, sets its damage, and launches it.</summary>
    private void SpawnBullet(Vector3 direction)
    {
        if (bulletPool == null)
        {
            Debug.LogError("[BossAI] bulletPool es null. Asigna el GameObjectPool en el Inspector.", this);
            return;
        }

        GameObject bullet = bulletPool.GetGameObjectFromPool(transform.position);

        Projectile projectile = bullet.GetComponent<Projectile>();
        if (projectile == null)
            Debug.LogWarning("[BossAI] El prefab del pool no tiene el componente Projectile.", bullet);
        else
            projectile.SetDamage(bulletDamage);

        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        if (bulletRb == null)
            Debug.LogWarning("[BossAI] El prefab del pool no tiene Rigidbody. La bala no se moverá.", bullet);

        // Prevent the bullet from colliding with the boss itself
        Collider bulletCollider = bullet.GetComponent<Collider>();
        if (bulletCollider != null && bossCollider != null)
            Physics.IgnoreCollision(bulletCollider, bossCollider);

        bulletRb?.AddForce(direction * bulletForce);
    }
}
