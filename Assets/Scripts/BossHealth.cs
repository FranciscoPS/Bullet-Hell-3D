using UnityEngine;
using UnityEngine.Events;

public class BossHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 300f;
    [SerializeField] private DamageFlash damageFlash;

    public UnityEvent onDeath;
    public bool IsInvulnerable { get; private set; }

    private float currentHealth;
    private AudioSource audioSource;

    public float HealthRatio => maxHealth > 0f ? currentHealth / maxHealth : 0f;

    private void Awake()
    {
        currentHealth = maxHealth;
        ResolveDamageFlash();

        audioSource = GetComponent<AudioSource>();
    }

    public void TakeDamage(float amount)
    {
        if (IsInvulnerable)
            return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0f);

        damageFlash?.Play();

        if (audioSource != null)
        {
            audioSource.PlayOneShot(audioSource.clip);
        }

        Debug.Log($"[BossHealth] Vida: {currentHealth} / {maxHealth}");

        if (currentHealth <= 0f)
            Die();
    }

    public void SetInvulnerable(bool isInvulnerable)
    {
        IsInvulnerable = isInvulnerable;
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
        FindAnyObjectByType<EndGameUIController>()?.ShowBossVictory();
        onDeath?.Invoke();
        Destroy(gameObject);
    }
}