using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObjectPool bulletPool;

    private void Start()
    {
        GameObject instance = Instantiate(playerPrefab, transform.position, transform.rotation);

        Gun gun = instance.GetComponent<Gun>();
        gun.SetPool(bulletPool);
        instance.GetComponent<PlayerMovement>().SetGun(gun);
    }
}
