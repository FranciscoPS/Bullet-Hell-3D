using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GameObjectPool : MonoBehaviour
{
    public GameObject gameObjectToPool;

    [SerializeField]
    private List<GameObject> pool = new List<GameObject>();

    [SerializeField]
    private int poolDefaultSize = 50;

    private void Start()
    {
        for (int i = 0; i < poolDefaultSize; i++)
        {
            InstancePoolObject();
        }
    }

    GameObject InstancePoolObject()
    {
        GameObject newGameObject = Instantiate(gameObjectToPool);
        pool.Add(newGameObject);
        newGameObject.SetActive(false);
        return newGameObject;
    }

    public GameObject GetGameObjectFromPool()
    {
        GameObject target = pool.FirstOrDefault(gameObject => !gameObject.activeSelf);

        if (target == null)
            target = InstancePoolObject();

        target.SetActive(true);
        return target;
    }

    public GameObject GetGameObjectFromPool(Vector3 position)
    {
        GameObject target = GetGameObjectFromPool();
        target.transform.position = position;
        return target;
    }
}
