using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Death")]
    [SerializeField] private Animator animator;
    [SerializeField] private float deathAnimationDuration = 4f;
    [SerializeField] private GameObject visualRoot;

    public UnityEvent onDeath;

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
            Debug.LogWarning("[PlayerHealth] No se encontró Animator. Asigna uno en el Inspector para reproducir la animación de muerte.");
        }
        else
        {
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            if (!AnimatorHasParameter(animator, "isDead"))
            {
                Debug.LogWarning("[PlayerHealth] El Animator no contiene el parámetro 'isDead'. Crea un parámetro bool llamado 'isDead' o actualiza el código.");
            }
        }
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
        Debug.Log("DIE LLAMADO");

        if (isDead)
            return;

        isDead = true;

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