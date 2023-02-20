using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using Valve.VR;

public class Grapple : MonoBehaviour
{
    [Header("Player Controller")]
    [Tooltip("The SteamVR boolean action that starts grappling")]
    [SerializeField] private SteamVR_Action_Boolean triggerPull;
    [Tooltip("The SteamVR boolean action that grapples in")]
    [SerializeField] private SteamVR_Action_Boolean aPressed;
    
    [SerializeField] private SteamVR_Input_Sources[] handControllers;
    [SerializeField] private GameObject[] handObjects = new GameObject[2];
    [Tooltip("input to start swing")]
    private readonly SteamVR_Action_Boolean.StateHandler[] callBacksTriggerPullState = new SteamVR_Action_Boolean.StateHandler[2];
    [Tooltip("input to end swing")]
    private readonly SteamVR_Action_Boolean.StateUpHandler[] callBacksTriggerPullStateUp = new SteamVR_Action_Boolean.StateUpHandler[2];
    [Tooltip("input to end swing")]
    private readonly SteamVR_Action_Boolean.StateDownHandler[] callBacksAPressedStateDown = new SteamVR_Action_Boolean.StateDownHandler[2];
    [Tooltip("joystick input")]
    public SteamVR_Action_Vector2 inputAxis;
    [Tooltip("haptic feedback")] 
    public SteamVR_Action_Vibration hapticAction;
    private Vector2 joystickInput;
    [SerializeField] private Vector2 speedXZ;
    [SerializeField] private Transform mainCam;
    private bool grounded;
    private Transform body;
    
    [Header("Swinging  & Grappling Settings")]
    [SerializeField] private float maxRopeDistance;

    [SerializeField] private float castRadius;
    
    [SerializeField] private float spring = 7f;
    [SerializeField] private float damper = 6f;
    [SerializeField] private float massScale = 4.5f;
    [SerializeField] [Range(0f, 1f)] private float maxSpringDistance = 0.85f;
    [SerializeField] [Range(0f, 1f)] private float minSpringDistance = 0.45f;

    [SerializeField] private float overshootYAxis;
    
    [FormerlySerializedAs("_grappleTimeDelay")] [SerializeField] private float grappleTimeDelay;
    
    // references
    private SpringJoint[] springJoints;
    private LineRenderer[] lineRenderers;
    private Rigidbody rb;
    private SteamVR_Behaviour_Pose[] handPoses;
    [SerializeField] [Tooltip("Layermask specifying buildings")]
    private LayerMask collisionsPreventionMask;
    
    //internal parameters
    private LayerMask ignoreRaycastLayerMask;
    private readonly Vector3[] attachmentPoints = {Vector3.zero, Vector3.zero};
    private readonly bool[] isGrappling = {false, false};
    private readonly bool[] isSwinging = {false, false};
    private bool isFrozen = false;
    private LayerMask lm;

    [Header("Haptic values")] 
    [Tooltip("how long the controller should vibrate when you fire your grapple")]
    [SerializeField] private float fireDuration;
    [Tooltip("frequency the controller should vibrate at when you fire your grapple")]
    [SerializeField]  [Range(0f, 320f)] private float fireFrequency;
    [Tooltip("amplitude of vibration when you fire your grapple")]
    [SerializeField] [Range(0f, 1f)] private float fireAmplitude;
    [Tooltip("how long the controller should vibrate when you hit your grapple")]
    [SerializeField] private float hitDuration;
    [Tooltip("frequency the controller should vibrate at when you hit your grapple")]
    [SerializeField]  [Range(0f, 320f)] private float hitFrequency;
    [Tooltip("amplitude of vibration when you hit your grapple")]
    [SerializeField] [Range(0f, 1f)] private float hitAmplitude;
    
    
    // animation
    [Header("Animation")]
    private float[] _animationCounter = new float[2];
    [SerializeField] [Tooltip("Segments in the line renderer, used for animation")]
    private int ropeAnimationQuality;

    [SerializeField] [Tooltip("number of sine waves that should appear in the rope")]
    private float waveCount;

    [SerializeField] [Tooltip("height of sine waves that appear in the rope")]
    private float waveHeight;

    private Vector3 _currentGrapplePosition;
    [SerializeField]
    [Tooltip("where should the sine waves appear along the rope, generally, high in middle and low at both ends")]
    private AnimationCurve affectCurve;
    private bool _playParticlesOnce;
    
    [Header("Player Joystick Parameters")]
    [SerializeField] private float maxJoystickMoveSpeed;
    
    private void Start()
    {
        body = transform.Find("Body");
        rb = gameObject.GetComponent<Rigidbody>();
        springJoints = new SpringJoint[2];
        if (triggerPull == null || aPressed == null)
        {
            Debug.LogError("[SteamVR] Boolean action not set.", this);
            return;
        }
        
        if (handControllers.Length != 2)
        {
            Debug.LogError("[SteamVR] hands not added", this);
        }
        
        lineRenderers = new LineRenderer[2] { 
            handObjects[0].transform.Find("GrappleCable").gameObject.GetComponent<LineRenderer>(),
            handObjects[1].transform.Find("GrappleCable").gameObject.GetComponent<LineRenderer>() 
        };

        lm = ~((1 << gameObject.layer) | (1 << 2)); // not player layer or ignore raycast layer
        
        handPoses = new SteamVR_Behaviour_Pose[2] { handObjects[0].GetComponent<SteamVR_Behaviour_Pose>(), handObjects[1].GetComponent<SteamVR_Behaviour_Pose>() };

        // setup listeners
        for (int i = 0; i < 2; i++)
        {
            callBacksTriggerPullState[i] = StartSwing(i);
            triggerPull[handControllers[i]].onState += callBacksTriggerPullState[i];
            callBacksTriggerPullStateUp[i] = EndSwing(i);
            triggerPull[handControllers[i]].onStateUp += callBacksTriggerPullStateUp[i];
            callBacksAPressedStateDown[i] = StartGrapple(i);
            aPressed[handControllers[i]].onStateDown += callBacksAPressedStateDown[i];
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < 2; i++)
        {
            triggerPull[handControllers[i]].onState -= callBacksTriggerPullState[i];
            triggerPull[handControllers[i]].onStateUp -= callBacksTriggerPullStateUp[i];
            aPressed[handControllers[i]].onStateDown -= callBacksAPressedStateDown[i];
        }
    }

    private void FixedUpdate()
    {
        if (rb.velocity.magnitude > maxJoystickMoveSpeed) return;
        Vector3 force = (joystickInput.y * speedXZ.y * Time.deltaTime * mainCam.forward ) + 
                        (joystickInput.x * speedXZ.x * Time.deltaTime * Vector3.Cross(Vector3.up, mainCam.forward));
            //new Vector3(joystickInput.x * speedXZ.x, 0f, 0);
        
        //Debug.Log(force);
        if(grounded && rb.velocity.magnitude < maxJoystickMoveSpeed)
            rb.AddForce(force.x, 0f, force.z, ForceMode.VelocityChange);
    }

    void Update()
    {
        if(isFrozen)
            rb.velocity = Vector3.zero;

        joystickInput = inputAxis.axis;
        
        Ray ray = new Ray(body.position, Vector3.down);
        grounded = Physics.Raycast(ray, body.localScale.y + 0.2f, LayerMask.NameToLayer("Ground"));
    }

    void LateUpdate()
    {
        DrawRope();
    }

    private void DrawRope()
    {
        for (var i = 0; i < 2; i++)
        {
            if (isGrappling[i] || isSwinging[i])
            {
                lineRenderers[i].positionCount = 2;
                lineRenderers[i].SetPosition(0, handPoses[i].transform.position);
                lineRenderers[i].SetPosition(1, attachmentPoints[i]);
            }
            else
            {
                lineRenderers[i].positionCount = 0;
            }
        }
    }

    private SteamVR_Action_Boolean.StateHandler StartSwing(int i)
    {
        
        return delegate(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
        {
            if (isSwinging[i])
                return;
            Ray ray = new(handPoses[i].transform.position, handPoses[i].transform.forward);

            if (!Physics.Raycast(ray, out var hit, maxRopeDistance, lm))
            {
                if (!Physics.SphereCast(ray, castRadius, out hit, maxRopeDistance, lm))
                {
                    return;
                }
            }
            if (hit.transform.gameObject.layer == 5) return; // 5 if object is in UI layer
            Pulse(fireDuration, fireFrequency, fireAmplitude, i);
            attachmentPoints[i] = hit.point;
            isSwinging[i] = true;
            springJoints[i] = gameObject.AddComponent<SpringJoint>();
            springJoints[i].autoConfigureConnectedAnchor = false;
            springJoints[i].connectedAnchor = attachmentPoints[i];

            float distanceFromGrapplePoint = Vector3.Distance(handPoses[i].transform.position, attachmentPoints[i]);

            springJoints[i].maxDistance = distanceFromGrapplePoint * maxSpringDistance;
            springJoints[i].minDistance = distanceFromGrapplePoint * minSpringDistance;
            float mass = rb.mass;
            springJoints[i].spring = spring * mass;
            springJoints[i].damper = damper * mass;
            springJoints[i].massScale = massScale * mass;
            
        };
    }

    private SteamVR_Action_Boolean.StateUpHandler EndSwing(int i)
    {
        return delegate(SteamVR_Action_Boolean action, SteamVR_Input_Sources source)
        {
            isSwinging[i] = false;
            Destroy(springJoints[i]);
        };
    }

    private SteamVR_Action_Boolean.StateDownHandler StartGrapple(int i)
    {
        
        return delegate(SteamVR_Action_Boolean action, SteamVR_Input_Sources source)
        {
            
            if(isGrappling[0] || isGrappling[1])
                return;

            Ray ray = new(handPoses[i].transform.position, handPoses[i].transform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, maxRopeDistance, lm))
            {
                if (!Physics.SphereCast(ray, castRadius, out hit, maxRopeDistance, lm))
                {
                    return;
                }
            }
            if (hit.transform.gameObject.layer == 5) return; // 5 if object is in UI layer
            
            Pulse( fireDuration, fireFrequency, fireAmplitude, i);
            
            for (int j = 0; j < 2; j++)
            {
                isSwinging[j] = false;
                Destroy(springJoints[j]);
            }
            
            isGrappling[i] = true;
            isFrozen = true;
            attachmentPoints[i] = hit.point;
            
            StartCoroutine(ExecuteGrapple(i));
        };
    }

    private IEnumerator ExecuteGrapple(int i)
    {
        Pulse(hitDuration, hitFrequency, hitAmplitude, i);
        yield return new WaitForSeconds(grappleTimeDelay);
        isFrozen = false;
        Vector3 lowestPoint = new Vector3(handPoses[i].transform.position.x, handPoses[i].transform.position.y - 1f, handPoses[i].transform.position.z);
        // compute grapple params
        float grapplePointRelativeYPos = attachmentPoints[i].y - lowestPoint.y;
        float highestPointOnArc = grapplePointRelativeYPos + overshootYAxis;
        
        // if graple location is beneath player then set arc accordingly
        if (grapplePointRelativeYPos < 0) highestPointOnArc = overshootYAxis;

        // set player velocity on controller
        float gravity = Physics.gravity.y;
        Vector3 playerPos = transform.position;
        float displacementY = attachmentPoints[i].y - playerPos.y;
        
        float potentiallyNegativeSqrt = 2 * (displacementY - highestPointOnArc) / gravity;
        if (potentiallyNegativeSqrt <= 0)
            potentiallyNegativeSqrt = 0;
        
        Vector3 displacementXZ = new Vector3(attachmentPoints[i].x - playerPos.x, 0f, attachmentPoints[i].z - playerPos.z);

        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * highestPointOnArc);
        Vector3 velocityXZ = displacementXZ / (Mathf.Sqrt(-2 * highestPointOnArc / gravity)
                                               + Mathf.Sqrt(potentiallyNegativeSqrt));
        Vector3 requiredVelocity = velocityXZ + velocityY;
        rb.velocity = requiredVelocity;

        StartCoroutine(EndGrapple(i));
    }

    private IEnumerator EndGrapple(int i)
    {
        yield return new WaitForSeconds(1f);
        isGrappling[i] = false;

    }

    private void Pulse(float duration, float frequency, float amplitude, int hand)
    {
        hapticAction.Execute(0, duration, frequency, amplitude, hand == 0? SteamVR_Input_Sources.LeftHand : SteamVR_Input_Sources.RightHand);
    }
}