using System.Collections;
using UnityEngine;

public class SpikeTrap : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float damageAmount = 10f;

    [SerializeField] private float damageInterval = 1f;

    private Coroutine damageRoutine;

    private void OnTriggerEnter(Collider other)
    {
        PlayerHealth playerHealth =
            other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
            return;

        damageRoutine = StartCoroutine(DamageOverTime(playerHealth));
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerHealth playerHealth =
            other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
            return;

        if (damageRoutine != null)
        {
            StopCoroutine(damageRoutine);
            damageRoutine = null;
        }
    }

    private IEnumerator DamageOverTime(PlayerHealth playerHealth)
    {
        while (playerHealth != null && !playerHealth.IsDead())
        {
            playerHealth.TakeDamage(damageAmount);

            yield return new WaitForSeconds(damageInterval);
        }

        damageRoutine = null;
    }
}