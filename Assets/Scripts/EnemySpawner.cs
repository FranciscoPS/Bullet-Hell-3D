using System.Numerics;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObjectPool enemyPool;
    [SerializeField] private GameObjectPool enemyBulletPool;
    [SerializeField] private float spawnInterval = 3f;

    private void Start()
    {
        SpawnEnemy();
        InvokeRepeating(nameof(SpawnEnemy), spawnInterval, spawnInterval);
    }

    private void SpawnEnemy()
    {
        if (enemyPool == null) return;

        GameObject enemy = enemyPool.GetGameObjectFromPool(transform.position);
        enemy.GetComponent<EnemyAI>().SetPool(enemyBulletPool);
    }
}
