using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObjectPool enemyBulletPool;

    private void Start()
    {
        GameObject instance = Instantiate(enemyPrefab, transform.position, transform.rotation);
        instance.GetComponent<EnemyAI>().SetPool(enemyBulletPool);
    }
}
