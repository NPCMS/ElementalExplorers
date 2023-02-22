using UnityEngine;

public class GrappleDrawer : MonoBehaviour
{
    [SerializeReference] private LineRenderer lineRenderer;

    public bool rendering;
    public Vector3 endPoint;

    void Update()
    {
        if (rendering)
        {
            // put first point on players hand and second at grapple location
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, endPoint);
        }
    }

    public void Enable(Vector3 fire, Vector3 end)
    {
        Debug.Log("Grapple draw");
        // if line renderer not present then add points
        lineRenderer.positionCount = 2;
        endPoint = end;
        rendering = true;
    }

    public void Disable()
    {
        lineRenderer.positionCount = 0;
        rendering = false;
    }
}
