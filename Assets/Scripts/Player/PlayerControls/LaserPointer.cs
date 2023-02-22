using UnityEngine;

public class LaserPointer : MonoBehaviour
{
    [SerializeReference] private GameObject pointer;
    [SerializeReference] private LineRenderer lr;
    [SerializeReference] private MeshRenderer pointerRenderer;
    private LayerMask lm;
    [SerializeField] private float maxPointerDistance;
    [SerializeField] private float castRadius;

    private void Start()
    {
        pointer.SetActive(true);
        lr.enabled = true;
        lm = ~((1 << gameObject.layer) | (1 << 2)); // not player layer or ignore raycast layer
    }

    private static float GetPointerScale(float distance)
    {
        return (1 + (distance * distance) / 3000) * 0.1f; // constant to scale down cross-hair size based on distance
    }

    // Update is called once per frame
    private void Update()
    {
        var gameObjectTransform = gameObject.transform;
        Ray ray = new(gameObjectTransform.position, gameObjectTransform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxPointerDistance, lm))
        {
            CastSphere(ray);
            return;
        }

        lr.SetPositions(new [] { gameObjectTransform.position, hit.point });

        if (hit.transform.gameObject.layer == 5) // if ui layer don't use pointer
        {
            pointerRenderer.enabled = false;
            return;
        }

        pointerRenderer.enabled = true;
        pointer.transform.position = hit.point;
        pointer.transform.localScale = GetPointerScale(hit.distance) * Vector3.one;
    }

    private void CastSphere(Ray ray) // called when ray trace misses
    {
        var o = gameObject;
        var position = o.transform.position;
        lr.SetPositions(new [] { position, position + o.transform.forward * maxPointerDistance });
        if (!Physics.SphereCast(ray, castRadius, out RaycastHit hit, maxPointerDistance, lm)) // if misses all objects
        {
            pointerRenderer.enabled = false;
            return;
        }

        if (hit.transform.gameObject.layer == 5) // if ui layer don't use pointer
        {
            pointerRenderer.enabled = false;
            return;
        }

        pointerRenderer.enabled = true;
        pointer.transform.position = hit.point;
        pointer.transform.localScale = GetPointerScale(hit.distance) * Vector3.one;
    }
}
