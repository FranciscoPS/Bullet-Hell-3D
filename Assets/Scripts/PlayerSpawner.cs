using Unity.Cinemachine;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObjectPool bulletPool;
    [SerializeField] private CinemachineCamera cinemachineCamera;

    private void Start()
    {
        GameObject instance = Instantiate(playerPrefab, transform.position, transform.rotation);

        Gun gun = instance.GetComponent<Gun>();
        gun.SetPool(bulletPool);
        instance.GetComponent<PlayerMovement>().SetGun(gun);

        cinemachineCamera.Follow = instance.transform;
    }
}
