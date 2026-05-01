using UnityEngine;

public class Gun : MonoBehaviour
{
    [SerializeField] private GameObjectPool pool;
    [SerializeField] private float shootForce = 150f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private Transform muzzle;

    private Collider ownerCollider;

    private void Awake()
    {
        ownerCollider = GetComponentInParent<Collider>();
    }

    public void SetPool(GameObjectPool bulletPool)
    {
        pool = bulletPool;
    }

    public void Shoot(Vector3 direction)
    {
        Transform spawnPoint = muzzle != null ? muzzle : transform;
        GameObject projectile = pool.GetGameObjectFromPool(spawnPoint.position);

        projectile.GetComponent<Projectile>()?.SetDamage(damage);

        if (ownerCollider != null)
        {
            Collider projectileCollider = projectile.GetComponent<Collider>();
            if (projectileCollider != null)
                Physics.IgnoreCollision(projectileCollider, ownerCollider);
        }

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        rb?.AddForce(direction * shootForce);
    }

    public void Shoot()
    {
        Shoot(transform.forward);
    }
}
