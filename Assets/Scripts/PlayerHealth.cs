using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private DamageFlash damageFlash;

    [Header("Death")]
    [SerializeField] private Animator animator;
    [SerializeField] private float deathAnimationDuration = 4f;
    [SerializeField] private GameObject visualRoot;

    public UnityEvent onDeath;
    public bool IsInvulnerable { get; private set; }

    private float currentHealth;
    private bool isDead;
    private Animator[] animators;

    private PlayerMovement playerMovement;

    private void Awake()
    {
        currentHealth = maxHealth;

        animators = GetComponentsInChildren<Animator>(true);

        Debug.Log("Animators encontrados: " + animators.Length);

        foreach (Animator a in animators)
        {
            Debug.Log(a.name);
        }

        playerMovement = GetComponent<PlayerMovement>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogWarning("[PlayerHealth] No se encontr� Animator. Asigna uno en el Inspector para reproducir la animaci�n de muerte.");
        }
        else
        {
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            if (!AnimatorHasParameter(animator, "isDead"))
            {
                Debug.LogWarning("[PlayerHealth] El Animator no contiene el par�metro 'isDead'. Crea un par�metro bool llamado 'isDead' o actualiza el c�digo.");
            }
        }
        ResolveDamageFlash();
    }

    public void TakeDamage(float amount)
    {
        if (isDead)
            return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        Debug.Log($"Vida: {currentHealth}/{maxHealth}");
        if (IsInvulnerable)
            return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0f);
        damageFlash?.Play();
        Debug.Log($"[PlayerHealth] Vida: {currentHealth} / {maxHealth}");

        if (currentHealth <= 0f)
        {
            Die();
        }
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
        Debug.Log("DIE LLAMADO");

        if (isDead)
            return;

        isDead = true;

        FindAnyObjectByType<EndGameUIController>()?.ShowPlayerDefeat();

        Animator[] animators = GetComponentsInChildren<Animator>(true);

        foreach (Animator anim in animators)
        {
            anim.SetTrigger("Death");
        }

        if (playerMovement != null)
            playerMovement.enabled = false;

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

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    public float GetHealth()
    {
        return currentHealth;
    }

    public bool IsDead()
    {
        return isDead;
    }

    private bool AnimatorHasParameter(Animator anim, string paramName)
    {
        foreach (AnimatorControllerParameter p in anim.parameters)
        {
            if (p.name == paramName)
                return true;
        }
        return false;
    }
}