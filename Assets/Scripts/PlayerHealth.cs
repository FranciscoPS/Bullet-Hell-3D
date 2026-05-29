using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Death")]
    [SerializeField] private Animator animator;
    [SerializeField] private float deathAnimationDuration = 2f;
    [SerializeField] private GameObject visualRoot;

    public UnityEvent onDeath;

    private float currentHealth;
    private bool isDead;

    private PlayerMovement playerMovement;

    private void Awake()
    {
        currentHealth = maxHealth;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        playerMovement = GetComponent<PlayerMovement>();
    }

    public void TakeDamage(float amount)
    {
        if (isDead)
            return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        Debug.Log($"Vida: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        Debug.Log("Jugador muerto");

        onDeath?.Invoke();

        if (playerMovement != null)
            playerMovement.enabled = false;

        if (animator != null)
            animator.SetBool("isDead", true);

        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        yield return new WaitForSeconds(deathAnimationDuration);

        if (visualRoot != null)
            visualRoot.SetActive(false);

        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }
    }

    public float GetHealth()
    {
        return currentHealth;
    }

    public bool IsDead()
    {
        return isDead;
    }
}