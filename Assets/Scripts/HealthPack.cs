using UnityEngine;

public class HealthPack : MonoBehaviour
{
    [Header("Health Pack")]
    [SerializeField] private float healAmount = 15f;

    [Header("Lifetime")]
    [SerializeField] private float lifeTime = 10f;

    private bool collected;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected)
            return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

        if (playerHealth == null)
            return;

        collected = true;

        playerHealth.Heal(healAmount);

        Destroy(gameObject);
    }
}