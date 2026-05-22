using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BlobShadowProjector : MonoBehaviour
{
    public Transform target;
    public LayerMask groundLayer;

    public float maxDistance = 10f;

    public float minSize = 0.5f;
    public float maxSize = 2f;

    public float offset = 0.05f;

    private DecalProjector projector;

    void Awake()
    {
        projector = GetComponent<DecalProjector>();
    }

    void Update()
    {
        Ray ray = new Ray(target.position + Vector3.up, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, groundLayer))
        {
            transform.position = hit.point + hit.normal * offset;

            transform.rotation = Quaternion.LookRotation(-hit.normal);

            float distance = Vector3.Distance(target.position, hit.point);

            float size = Mathf.Lerp(maxSize, minSize, distance / maxDistance);

            projector.size = new Vector3(size, size, projector.size.z);
        }
    }
}