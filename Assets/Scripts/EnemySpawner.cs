using System.Numerics;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObjectPool enemyPool;
    [SerializeField] private GameObjectPool enemyBulletPool;
    [SerializeField] private float spawnInterval = 3f;

    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();

        SpawnEnemy();
        InvokeRepeating(nameof(SpawnEnemy), spawnInterval, spawnInterval);
    }

    private void SpawnEnemy()
    {
        if (enemyPool == null) return;

        GameObject enemy = enemyPool.GetGameObjectFromPool(transform.position);
        enemy.GetComponent<EnemyAI>().SetPool(enemyBulletPool);

        if (audioSource != null)
        {
            audioSource.Play();
        }
    }
}