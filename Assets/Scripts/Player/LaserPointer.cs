using UnityEngine;

public class LaserPointer : MonoBehaviour
{
    public GameObject pointer;
    private LineRenderer lr;
    private MeshRenderer pointerRenderer;
    private LayerMask lm;
    [SerializeField] float maxPointerDistance;
    [SerializeField] private float castRadius;

    private void Start()
    {
        lr = pointer.GetComponent<LineRenderer>();
        pointerRenderer = pointer.GetComponent<MeshRenderer>();
        if (!lr)
        {
            Debug.LogError("No line renderer on pointer when one was expected");
        }
        lm = ~(1 << gameObject.layer); // not player layer
    }

    float getScale(float distance)
    {
        return (1 + (distance * distance) / 3000) * 0.1f; // constant to scale down crosshair size based on distance
    }

    // Update is called once per frame
    void Update()
    {

        Ray ray = new(gameObject.transform.position, gameObject.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxPointerDistance, lm))
        {
            CastSphere(ray);
            return;
        }

        lr.SetPositions(new Vector3[2] { gameObject.transform.position, hit.point });

        if (hit.transform.gameObject.layer == 5) // if ui layer don't use pointer
        {
            pointerRenderer.enabled = false;
            return;
        }

        pointerRenderer.enabled = true;
        pointer.transform.position = hit.point;
        pointer.transform.localScale = getScale(hit.distance) * Vector3.one;
    }

    void CastSphere(Ray ray) // called when ray trace misses
    {
        lr.SetPositions(new Vector3[2] { gameObject.transform.position, gameObject.transform.position + gameObject.transform.forward * maxPointerDistance });
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
        pointer.transform.localScale = getScale(hit.distance) * Vector3.one;
    }
}
