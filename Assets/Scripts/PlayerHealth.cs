using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    public UnityEvent onDeath;
    public bool IsInvulnerable { get; private set; }

    private float currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (IsInvulnerable)
            return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0f);
        Debug.Log($"[PlayerHealth] Vida: {currentHealth} / {maxHealth}");

        if (currentHealth <= 0f)
            Die();
    }

    public void SetInvulnerable(bool isInvulnerable)
    {
        IsInvulnerable = isInvulnerable;
    }

    private void Die()
    {
        Debug.Log("[PlayerHealth] El jugador ha muerto.");
        onDeath?.Invoke();
        gameObject.SetActive(false);
    }
}
