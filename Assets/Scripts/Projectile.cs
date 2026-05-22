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
    private float seekingElapsed;
    private bool isSeeking;

    public void SetDamage(float amount)
    {
        damage = amount;
    }

    public void SetSeeking(Transform target, float duration, float speed)
    {
        seekingTarget = target;
        seekingDuration = duration;
        seekingSpeed = speed;
        seekingElapsed = 0f;
        isSeeking = true;
    }

    private void OnEnable()
    {
        rb.linearVelocity = Vector3.zero;
        isSeeking = false;
        seekingTarget = null;

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

            Vector3 dir = (seekingTarget.position - transform.position);
            if (dir.sqrMagnitude > 0.001f)
            {
                dir.Normalize();
                rb.linearVelocity = dir * seekingSpeed;
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
