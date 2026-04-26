using System.Collections;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private float lifetime = 3;
    public Rigidbody rb;
    private Coroutine lifetimeRoutine;

    private void OnEnable()
    {
        rb.linearVelocity = Vector3.zero;

        if (lifetimeRoutine != null)
            StopCoroutine(lifetimeRoutine);

        lifetimeRoutine = StartCoroutine(LifetimeRoutine());
    }

    private void OnCollisionEnter(Collision collision)
    {
        gameObject.SetActive(false);
    }

    IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(lifetime);
        gameObject.SetActive(false);
    }
}
