using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 30f;
    [SerializeField] private DamageFlash damageFlash;

    [Header("Drops")]
    [SerializeField] private GameObject dropPrefab;
    [SerializeField][Range(0f, 100f)] private float dropChance = 30f;

    private float currentHealth;
    private bool isDead;
    private EnemyAI enemyAI;
    private bool enemyAIInitiallyEnabled;
    private Collider[] colliders;
    private bool[] collidersInitiallyEnabled;
    private Coroutine deathRoutine;

    private void Awake()
    {
        enemyAI = GetComponent<EnemyAI>();
        enemyAIInitiallyEnabled = enemyAI != null && enemyAI.enabled;
        colliders = GetComponentsInChildren<Collider>(true);
        collidersInitiallyEnabled = new bool[colliders.Length];

        for (int i = 0; i < colliders.Length; i++)
            collidersInitiallyEnabled[i] = colliders[i] != null && colliders[i].enabled;

        ResolveDamageFlash();
    }

    private void OnEnable()
    {
        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
            deathRoutine = null;
        }

        currentHealth = maxHealth;
        isDead = false;
        SetGameplayEnabled(true);
    }

    public void TakeDamage(float amount)
    {
        if (isDead)
            return;

        currentHealth -= amount;
        damageFlash?.Play();

        if (currentHealth <= 0f)
            Die();
    }

    private void ResolveDamageFlash()
    {
        if (damageFlash != null)
            return;

        if (!TryGetComponent(out damageFlash))
            damageFlash = gameObject.AddComponent<DamageFlash>();
    }

    private void Die()
    {
        isDead = true;
        SetGameplayEnabled(false);

        TryDropItem();

        if (deathRoutine != null)
            StopCoroutine(deathRoutine);

        deathRoutine = StartCoroutine(DeactivateAfterFlash());
    }
    private void TryDropItem()
    {
        if (dropPrefab == null)
            return;

        float roll = Random.Range(0f, 100f);

        if (roll <= dropChance)
        {
            Instantiate(
                dropPrefab,
                transform.position,
                Quaternion.identity);
        }
    }

    private IEnumerator DeactivateAfterFlash()
    {
        float delay = damageFlash != null ? damageFlash.TotalDuration : 0f;
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        deathRoutine = null;
        gameObject.SetActive(false);
    }

    private void SetGameplayEnabled(bool enabled)
    {
        if (enemyAI != null)
            enemyAI.enabled = enabled && enemyAIInitiallyEnabled;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = enabled && collidersInitiallyEnabled[i];
        }
    }
}
