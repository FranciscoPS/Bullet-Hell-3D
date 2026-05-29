using UnityEngine;
using UnityEngine.Events;

public class BossHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 300f;
    [SerializeField] private DamageFlash damageFlash;

    public UnityEvent onDeath;

    private float currentHealth;

    public float HealthRatio => maxHealth > 0f ? currentHealth / maxHealth : 0f;

    private void Awake()
    {
        currentHealth = maxHealth;
        ResolveDamageFlash();
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0f);
        damageFlash?.Play();
        Debug.Log($"[BossHealth] Vida: {currentHealth} / {maxHealth}");

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
        Debug.Log("[BossHealth] El boss ha muerto.");
        onDeath?.Invoke();
        Destroy(gameObject);
    }
}
