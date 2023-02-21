using UnityEngine;

public class GrappleDrawer : MonoBehaviour
{
    [SerializeReference] private LineRenderer lineRenderer;
    [SerializeField] [Tooltip("Segments in the line renderer, used for animation")]
    private int ropeAnimationQuality;
    [SerializeField] [Tooltip("height of sine waves that appear in the rope")]
    private float waveHeight;
    [Tooltip("where should the sine waves appear along the rope, generally, high in middle and low at both ends")]
    private AnimationCurve affectCurve;
    [SerializeField] [Tooltip("number of sine waves that should appear in the rope")]
    private float waveCount;

    private Vector3 currentGrapplePosition;
    private bool rendering;
    private bool playParticlesOnce;
    private Vector3 endPoint;
    private Vector3 firePoint;
    private float _animationCounter = 1;
    
    void Update()
    {
        if (rendering)
        {
            if (lineRenderer.positionCount == 0)
            {
                lineRenderer.positionCount = ropeAnimationQuality + 1;
            }

            var up = Quaternion.LookRotation((endPoint - firePoint).normalized) * Vector3.up;

            currentGrapplePosition = Vector3.Lerp(currentGrapplePosition, endPoint, Time.deltaTime * 100f);

            // update grapple head position
            // firePoint = _currentGrapplePosition;
            // check if grapple has hit yet
            if (((currentGrapplePosition - endPoint).magnitude < 0.1f) && playParticlesOnce)
            {
                // grappleHook.GetComponent<ParticleSystem>().Play();
                playParticlesOnce = false;
            }

            for (var i = 0; i < ropeAnimationQuality + 1; i++)
            {
                var delta = i / (float)ropeAnimationQuality;
                var offset = up * (waveHeight * Mathf.Sin(delta * waveCount * Mathf.PI) * _animationCounter *
                             affectCurve.Evaluate(delta));

                lineRenderer.SetPosition(i,
                    Vector3.Lerp(firePoint, currentGrapplePosition, delta) + offset);
            }

            if (_animationCounter > 0)
            {
                _animationCounter -= 0.08f;
            }
            else
            {
                _animationCounter = 0;
            }
        }
        else
        {
            _animationCounter = 1;
        }
    }

    public void Enable(Vector3 fire, Vector3 end)
    {
        endPoint = end;
        firePoint = fire;
        rendering = true;
        playParticlesOnce = false;
        currentGrapplePosition = firePoint;
        lineRenderer.enabled = true;
    }

    public void Disable()
    {
        rendering = false;
        lineRenderer.enabled = false;
    }
}
