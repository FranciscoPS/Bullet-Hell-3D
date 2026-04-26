using UnityEngine;

public class Gun : MonoBehaviour
{
    [SerializeField] private GameObjectPool pool;
    [SerializeField] private float shootForce = 150f;
    [SerializeField] private Transform muzzle;

    private Collider playerCollider;

    private void Awake()
    {
        playerCollider = GetComponentInParent<Collider>();
    }

    public void Shoot()
    {
        Transform spawnPoint = muzzle != null ? muzzle : transform;
        GameObject projectile = pool.GetGameObjectFromPool(spawnPoint.position);

        if (playerCollider != null)
        {
            Collider projectileCollider = projectile.GetComponent<Collider>();
            if (projectileCollider != null)
                Physics.IgnoreCollision(projectileCollider, playerCollider);
        }

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        rb?.AddForce(spawnPoint.forward * shootForce);
    }
}
