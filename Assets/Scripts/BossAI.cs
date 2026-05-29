using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class BossAI : MonoBehaviour
{
    private enum BossState
    {
        WaitingPlayer,
        Chasing,
        InRange,
        Dashing
    }

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float stopDistance = 8f;
    [SerializeField] private float collisionSkin = 0.02f;

    [Header("Phase 1 Attack")]
    [SerializeField] private float attackDuration = 3f;
    [SerializeField] private float timeBetweenAttacks = 2f;
    [SerializeField] private float bulletForce = 200f;
    [SerializeField] private float bulletDamage = 10f;

    [Header("Circular Attack")]
    [Tooltip("Number of bullets per ring.")]
    [SerializeField] private int circularBulletCount = 12;
    [Tooltip("Seconds between each ring burst.")]
    [SerializeField] private float circularFireRate = 0.4f;

    [Header("Hexagonal Attack")]
    [Tooltip("Number of 6-bullet waves to fire.")]
    [SerializeField] private int hexWaves = 5;
    [Tooltip("Seconds between each hexagonal wave.")]
    [SerializeField] private float hexWaveInterval = 0.4f;
    [Tooltip("Extra rotation offset applied to each wave (degrees).")]
    [SerializeField] private float hexWaveRotation = 30f;

    [Header("Spiral Attack")]
    [Tooltip("Number of arms in the spiral.")]
    [SerializeField] private int spiralArms = 3;
    [Tooltip("Degrees rotated per second.")]
    [SerializeField] private float spiralRotationSpeed = 120f;
    [Tooltip("Seconds between each bullet volley.")]
    [SerializeField] private float spiralFireRate = 0.08f;

    [Header("Bullet Pool")]
    [SerializeField] private GameObjectPool bulletPool;

    [Header("Phase 2 - Dash")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDamage = 20f;
    [SerializeField] private float dashContactRadius = 1.5f;
    [SerializeField] private string wallTag = "Wall";
    [Range(0f, 1f)]
    [SerializeField] private float phaseTwoDashChance = 0.35f;

    [Header("Phase 3 - Seeking Projectile")]
    [SerializeField] private int seekingProjectileCount = 5;
    [SerializeField] private float seekingProjectileDuration = 2.2f;
    [SerializeField] private float seekingProjectileSpeed = 13f;
    [SerializeField] private float seekingTurnRate = 120f;
    [SerializeField] private float seekingStartDelay = 0.2f;
    [Range(0f, 20f)]
    [SerializeField] private float seekingInaccuracy = 7f;

    private Transform player;
    private Rigidbody rb;
    private Collider bossCollider;
    private BossHealth bossHealth;

    private BossState state = BossState.WaitingPlayer;
    private bool isAttacking;
    private float nextAttackTime;
    private Vector3 chaseDirection;

    private bool phaseTwoTriggered;
    private bool pendingPhaseTwoEntryDash;
    private bool isPhaseThree;
    private bool phaseThreeTriggered;
    private bool pendingPhaseThreeEntryDash;

    private Vector3 dashDirection;
    private bool dashDamageDealt;

    private Coroutine attackCoroutine;
    private Coroutine dashCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        bossCollider = GetComponent<Collider>();
        bossHealth = GetComponent<BossHealth>();

        rb.isKinematic = true;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        Animator animator = GetComponent<Animator>();
        if (animator != null)
            animator.applyRootMotion = false;

        NavMeshAgent navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent != null)
            navMeshAgent.enabled = false;
    }

    private void Start()
    {
        TryAssignPlayer();
    }

    private void OnDisable()
    {
        SetPlayerCollisionIgnored(false);
        SetBossInvulnerable(false);
    }

    private void Update()
    {
        if (!TryAssignPlayer())
        {
            state = BossState.WaitingPlayer;
            chaseDirection = Vector3.zero;
            return;
        }

        UpdatePhaseTriggers();

        // Absolute priorities: guaranteed dash on phase transitions.
        if (state != BossState.Dashing && pendingPhaseThreeEntryDash)
        {
            StartPhaseEntryDash(includeSeekersAfterDash: true, isPhaseThreeEntry: true);
            return;
        }

        if (state != BossState.Dashing && pendingPhaseTwoEntryDash)
        {
            StartPhaseEntryDash(includeSeekersAfterDash: false, isPhaseThreeEntry: false);
            return;
        }

        // During dash, do not run chase/in-range logic.
        if (state == BossState.Dashing)
            return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;
        bool inRange = distance <= stopDistance;

        // Attack scheduler (shared by in-range patterns and random phase dash),
        // so dash can also trigger while out of range.
        if (!isAttacking && attackCoroutine == null && Time.time >= nextAttackTime)
        {
            if (phaseTwoTriggered && Random.value <= phaseTwoDashChance)
            {
                StartRandomPhaseDash();
                return;
            }

            if (inRange)
            {
                attackCoroutine = StartCoroutine(ExecuteRandomAttack());
                return;
            }

            // Out of range and no dash this tick: wait next interval before re-rolling.
            nextAttackTime = Time.time + timeBetweenAttacks;
        }

        if (!inRange)
        {
            if (toPlayer.sqrMagnitude > 0.001f)
            {
                chaseDirection = toPlayer.normalized;
                transform.rotation = Quaternion.LookRotation(chaseDirection);
                state = BossState.Chasing;
            }
            else
            {
                chaseDirection = Vector3.zero;
                state = BossState.InRange;
            }
            return;
        }

        chaseDirection = Vector3.zero;
        state = BossState.InRange;

        if (toPlayer.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(toPlayer.normalized);
    }

    private void FixedUpdate()
    {
        if (state != BossState.Chasing || player == null || chaseDirection.sqrMagnitude < 0.001f)
            return;

        MoveWithCollision(chaseDirection * moveSpeed * Time.fixedDeltaTime);
    }

    private bool TryAssignPlayer()
    {
        if (player != null)
            return true;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
            return false;

        player = playerObj.transform;
        return true;
    }

    private void UpdatePhaseTriggers()
    {
        if (!phaseTwoTriggered && bossHealth != null && bossHealth.HealthRatio <= 0.5f)
        {
            phaseTwoTriggered = true;
            pendingPhaseTwoEntryDash = true;
            Debug.Log("<color=red>[BOSS] FASE 2 ACTIVADA - EMBESTIDA DESBLOQUEADA</color>");
        }

        if (!phaseThreeTriggered && bossHealth != null && bossHealth.HealthRatio <= 0.25f)
        {
            isPhaseThree = true;
            phaseThreeTriggered = true;
            // Fase 3 tiene prioridad: no queremos arrastrar una embestida de entrada de fase 2.
            pendingPhaseTwoEntryDash = false;
            pendingPhaseThreeEntryDash = true;
            Debug.Log("<color=red>[BOSS] FASE 3 ACTIVADA - PROYECTILES BUSCADORES DESBLOQUEADOS</color>");
        }
    }

    private void StartPhaseEntryDash(bool includeSeekersAfterDash, bool isPhaseThreeEntry)
    {
        if (player == null || state == BossState.Dashing)
            return;

        CancelCurrentAttackAndChase();
        PrepareDashDirection();
        if (isPhaseThreeEntry)
            pendingPhaseThreeEntryDash = false;
        else
            pendingPhaseTwoEntryDash = false;
        state = BossState.Dashing;

        dashCoroutine = StartCoroutine(ExecutePhaseEntryDash(includeSeekersAfterDash));
    }

    private void StartRandomPhaseDash()
    {
        if (player == null || state == BossState.Dashing)
            return;

        CancelCurrentAttackAndChase();
        PrepareDashDirection();
        state = BossState.Dashing;
        dashCoroutine = StartCoroutine(ExecutePhaseEntryDash(isPhaseThree));
    }

    private void PrepareDashDirection()
    {
        dashDirection = player.position - transform.position;
        dashDirection.y = 0f;

        if (dashDirection.sqrMagnitude < 0.001f)
            dashDirection = transform.forward;

        dashDirection.Normalize();
        transform.rotation = Quaternion.LookRotation(dashDirection);
    }

    private void CancelCurrentAttackAndChase()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        isAttacking = false;
        chaseDirection = Vector3.zero;
    }

    private IEnumerator ExecutePhaseEntryDash(bool includeSeekersAfterDash)
    {
        isAttacking = true;
        yield return StartCoroutine(ExecuteDashSequence(includeSeekersAfterDash));
        isAttacking = false;
        dashCoroutine = null;
        nextAttackTime = Time.time + timeBetweenAttacks;

        // Let Update decide if chase or in-range on the next frame.
        state = BossState.InRange;
    }

    private void TryDealDashDamage()
    {
        if (dashDamageDealt || player == null)
            return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.magnitude <= dashContactRadius)
        {
            PlayerHealth ph = player.GetComponent<PlayerHealth>()
                           ?? player.GetComponentInParent<PlayerHealth>();
            ph?.TakeDamage(dashDamage);
            dashDamageDealt = true;
        }
    }

    private IEnumerator ExecuteDashSequence(bool includeSeekersAfterDash)
    {
        dashDamageDealt = false;
        SetPlayerCollisionIgnored(true);
        SetBossInvulnerable(true);

        while (true)
        {
            bool hitWall = MoveDashStepUntilWall(dashDirection, dashSpeed * Time.fixedDeltaTime);
            TryDealDashDamage();

            if (hitWall)
                break;

            yield return new WaitForFixedUpdate();
        }

        SetBossInvulnerable(false);
        SetPlayerCollisionIgnored(false);

        if (includeSeekersAfterDash)
            yield return StartCoroutine(SeekingProjectileAttack());
    }

    // Returns true only when the step collides with an object tagged as wall.
    private bool MoveDashStepUntilWall(Vector3 direction, float stepDistance)
    {
        if (stepDistance <= 0f || direction.sqrMagnitude < 0.001f)
            return false;

        direction.Normalize();
        Vector3 targetPosition = rb.position;

        RaycastHit[] hits = rb.SweepTestAll(direction, stepDistance + collisionSkin, QueryTriggerInteraction.Ignore);
        if (hits != null && hits.Length > 0)
        {
            float nearestWallDistance = float.MaxValue;
            bool foundWall = false;

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider == null)
                    continue;

                if (IsWallCollider(hit.collider) && hit.distance < nearestWallDistance)
                {
                    nearestWallDistance = hit.distance;
                    foundWall = true;
                }
            }

            if (foundWall)
            {
                float moveToContact = Mathf.Max(0f, nearestWallDistance - collisionSkin);
                targetPosition += direction * moveToContact;
                rb.MovePosition(targetPosition);
                return true;
            }
        }

        targetPosition += direction * stepDistance;
        rb.MovePosition(targetPosition);
        return false;
    }

    private bool IsWallCollider(Collider col)
    {
        if (col == null)
            return false;

        Transform current = col.transform;
        while (current != null)
        {
            if (current.CompareTag(wallTag))
                return true;
            current = current.parent;
        }

        return false;
    }

    private void SetPlayerCollisionIgnored(bool ignored)
    {
        if (bossCollider == null || player == null)
            return;

        Collider[] playerColliders = player.GetComponentsInChildren<Collider>();
        for (int i = 0; i < playerColliders.Length; i++)
        {
            Collider c = playerColliders[i];
            if (c == null)
                continue;
            Physics.IgnoreCollision(bossCollider, c, ignored);
        }
    }

    private void SetBossInvulnerable(bool isInvulnerable)
    {
        bossHealth?.SetInvulnerable(isInvulnerable);
    }

    private IEnumerator ExecuteRandomAttack()
    {
        isAttacking = true;

        int pattern = Random.Range(0, 3);

        switch (pattern)
        {
            case 0: yield return StartCoroutine(CircularAttack());  break;
            case 1: yield return StartCoroutine(HexagonalAttack()); break;
            case 2: yield return StartCoroutine(SpiralAttack());    break;
        }

        nextAttackTime = Time.time + timeBetweenAttacks;
        isAttacking = false;
        attackCoroutine = null;
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
        float elapsed = 0f;
        float currentAngle = 0f;
        float armStep = 360f / spiralArms;

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
            proj.SetSeeking(
                player,
                seekingProjectileDuration,
                seekingProjectileSpeed,
                seekingTurnRate,
                seekingStartDelay,
                seekingInaccuracy);
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
