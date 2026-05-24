using System.Collections;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 3f;
    public Rigidbody rb;
    private Coroutine lifetimeRoutine;
    private float damage;

    private Transform seekingTarget;
    private float seekingDuration;
    private float seekingSpeed;
    private float seekingTurnRateDeg;
    private float seekingStartDelay;
    private float seekingAimOffsetDeg;
    private float seekingElapsed;
    private bool isSeeking;

    public void SetDamage(float amount)
    {
        damage = amount;
    }

    public void SetSeeking(
        Transform target,
        float duration,
        float speed,
        float turnRateDeg = 120f,
        float startDelay = 0.2f,
        float inaccuracyDeg = 7f)
    {
        seekingTarget = target;
        seekingDuration = Mathf.Max(0f, duration);
        seekingSpeed = Mathf.Max(0f, speed);
        seekingTurnRateDeg = Mathf.Max(0f, turnRateDeg);
        seekingStartDelay = Mathf.Max(0f, startDelay);
        seekingAimOffsetDeg = Random.Range(-Mathf.Abs(inaccuracyDeg), Mathf.Abs(inaccuracyDeg));
        seekingElapsed = 0f;
        isSeeking = true;
    }

    private void OnEnable()
    {
        rb.linearVelocity = Vector3.zero;
        isSeeking = false;
        seekingTarget = null;
        seekingDuration = 0f;
        seekingSpeed = 0f;
        seekingTurnRateDeg = 0f;
        seekingStartDelay = 0f;
        seekingAimOffsetDeg = 0f;

        if (lifetimeRoutine != null)
            StopCoroutine(lifetimeRoutine);

        lifetimeRoutine = StartCoroutine(LifetimeRoutine());
    }

    private void FixedUpdate()
    {
        if (isSeeking && seekingTarget != null)
        {
            seekingElapsed += Time.fixedDeltaTime;
            if (seekingElapsed >= seekingDuration)
            {
                isSeeking = false;
                return;
            }

            // Telegraph: a short straight flight gives the player time to react.
            if (seekingElapsed < seekingStartDelay)
                return;

            Vector3 dir = (seekingTarget.position - transform.position);
            if (dir.sqrMagnitude > 0.001f)
            {
                dir.Normalize();
                dir = Quaternion.AngleAxis(seekingAimOffsetDeg, Vector3.up) * dir;

                Vector3 currentDir = rb.linearVelocity.sqrMagnitude > 0.001f
                    ? rb.linearVelocity.normalized
                    : transform.forward;

                float turnStepRad = seekingTurnRateDeg * Mathf.Deg2Rad * Time.fixedDeltaTime;
                Vector3 nextDir = Vector3.RotateTowards(currentDir, dir, turnStepRad, 0f);
                rb.linearVelocity = nextDir * seekingSpeed;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        EnemyHealth enemy = collision.gameObject.GetComponent<EnemyHealth>();
        if (enemy == null)
            enemy = collision.gameObject.GetComponentInParent<EnemyHealth>();
        enemy?.TakeDamage(damage);

        BossHealth boss = collision.gameObject.GetComponent<BossHealth>();
        if (boss == null)
            boss = collision.gameObject.GetComponentInParent<BossHealth>();
        boss?.TakeDamage(damage);

        PlayerHealth player = collision.gameObject.GetComponent<PlayerHealth>();
        if (player == null)
            player = collision.gameObject.GetComponentInParent<PlayerHealth>();
        player?.TakeDamage(damage);

        gameObject.SetActive(false);
    }

    IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(lifetime);
        gameObject.SetActive(false);
    }
}
