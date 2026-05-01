using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObjectPool enemyBulletPool;
    [SerializeField] private float spawnInterval = 3f;

    private void Start()
    {
        SpawnEnemy();
        InvokeRepeating(nameof(SpawnEnemy), spawnInterval, spawnInterval);
    }

    private void SpawnEnemy()
    {
        GameObject instance = Instantiate(enemyPrefab, transform.position, transform.rotation);
        instance.GetComponent<EnemyAI>().SetPool(enemyBulletPool);
    }
}
