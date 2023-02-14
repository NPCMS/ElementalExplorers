using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using Valve.VR;

public class SwingAndGrapple : MonoBehaviour
{
    [Header("Player Controller")]
    [Tooltip("The SteamVR boolean action that starts grappling")]
    [SerializeField] private SteamVR_Action_Boolean triggerPull;
    [Tooltip("The SteamVR boolean action that grapples in")]
    [SerializeField] private SteamVR_Action_Boolean aPressed;
    
    [SerializeField] private SteamVR_Input_Sources[] handControllers;
    [SerializeField] private GameObject[] handObjects = new GameObject[2];
    [Tooltip("input to start swing")]
    public SteamVR_Action_Boolean.StateHandler[] callBacksTriggerPullState = new SteamVR_Action_Boolean.StateHandler[2];
    [Tooltip("input to end swing")]
    public SteamVR_Action_Boolean.StateUpHandler[] callBacksTriggerPullStateUp = new SteamVR_Action_Boolean.StateUpHandler[2];
    [Tooltip("input to end swing")]
    public SteamVR_Action_Boolean.StateDownHandler[] callBacksAPressedStateDown = new SteamVR_Action_Boolean.StateDownHandler[2];
    [Tooltip("joystick input")]
    public SteamVR_Action_Vector2 inputAxis;
    [Tooltip("haptic feedback")] 
    public SteamVR_Action_Vibration hapticAction;
    private Vector2 joystickInput;
    [SerializeField] private Vector2 speedXZ;
    [SerializeField] private Transform mainCam;
    private bool _grounded;
    private Transform _body;
    
    [Header("Swinging  & Grappling Settings")]
    [SerializeField] private float maxRopeDistance;

    [SerializeField] private float castRadius;
    
    [SerializeField] private float spring = 7f;
    [SerializeField] private float damper = 6f;
    [SerializeField] private float massScale = 4.5f;
    [SerializeField] [Range(0f, 1f)] private float maxSpringDistance = 0.85f;
    [SerializeField] [Range(0f, 1f)] private float minSpringDistance = 0.45f;

    [SerializeField] private float overshootYAxis;
    
    [SerializeField] private float _grappleTimeDelay;
    
    // references
    private SpringJoint[] springJoints;
    private LineRenderer[] lineRenderers;
    private Rigidbody _rb;
    private SteamVR_Behaviour_Pose[] handPoses;
    [SerializeField] [Tooltip("Layermask specifying buildings")]
    private LayerMask collisionsPreventionMask;
    
    //internal parameters
    private LayerMask _ignoreRaycastLayerMask;
    private readonly Vector3[] attachmentPoints = new Vector3[2] { Vector3.zero, Vector3.zero };
    private bool[] _isGrappling = new bool[2] {false, false};
    private bool[] _isSwinging = new bool[2] { false, false };
    private bool _isFrozen = false;

    [Header("Haptic values")] 
    [Tooltip("how long the controller should vibrate when you fire your grapple")]
    [SerializeField] private float fireDuration;
    [Tooltip("frequency the controller should vibrate at when you fire your grapple")]
    [SerializeField]  [Range(0f, 320f)] private float fireFrequency;
    [Tooltip("amplitude of vibration when you fire your grapple")]
    [SerializeField] private float fireAmplitude;
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
        _body = transform.Find("Body");
        _rb = gameObject.GetComponent<Rigidbody>();
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
        if (_rb.velocity.magnitude > maxJoystickMoveSpeed) return;
        Vector3 force = (joystickInput.y * speedXZ.y * Time.deltaTime * mainCam.forward ) + 
                        (joystickInput.x * speedXZ.x * Time.deltaTime * Vector3.Cross(Vector3.up, mainCam.forward));
            //new Vector3(joystickInput.x * speedXZ.x, 0f, 0);
        
        //Debug.Log(force);
        if(_grounded && _rb.velocity.magnitude < maxJoystickMoveSpeed)
            _rb.AddForce(force.x, 0f, force.z, ForceMode.VelocityChange);
    }

    void Update()
    {
        if(_isFrozen)
            _rb.velocity = Vector3.zero;

        joystickInput = inputAxis.axis;
        
        Ray ray = new Ray(_body.position, Vector3.down);
        _grounded = Physics.Raycast(ray, _body.localScale.y + 0.2f, LayerMask.NameToLayer("Ground"));
    }

    void LateUpdate()
    {
        DrawRope();
    }

    private void DrawRope()
    {
        

        // for (int i = 0; i < 2; i++)
        // {
        //     if (!_isGrappling[i] && !_isSwinging[i])
        //     {
        //         var firePointPosition = handPoses[i].transform.position;
        //         _currentGrapplePosition = firePointPosition;
        //         if (_animationCounter[i] < 1)
        //         {
        //             _animationCounter[i] = 1;
        //         }
        //
        //         if (lineRenderers[i].positionCount > 0)
        //             lineRenderers[i].positionCount = 0;
        //         return;
        //     }
        //
        //     if (lineRenderers[i].positionCount == 0)
        //     {
        //         lineRenderers[i].positionCount = ropeAnimationQuality + 1;
        //     }
        //     
        //     var up = Quaternion.LookRotation((attachmentPoints[i] - handPoses[i].transform.position).normalized) * Vector3.up;
        //     _currentGrapplePosition = Vector3.Lerp(_currentGrapplePosition, attachmentPoints[i], Time.deltaTime * 100f);
        //
        //     handPoses[i].transform.position = _currentGrapplePosition;
        //     if (((_currentGrapplePosition - attachmentPoints[i]).magnitude < 0.1f) && _playParticlesOnce)
        //     {
        //         handPoses[i].GetComponent<ParticleSystem>().Play();
        //         _playParticlesOnce = false;
        //     }
        //     
        //     for (var j = 0; j < ropeAnimationQuality + 1; j++)
        //     {
        //         var delta = j / (float) ropeAnimationQuality;
        //         var offset = up * (waveHeight * Mathf.Sin(delta * waveCount * Mathf.PI) * _animationCounter[i] *
        //                      affectCurve.Evaluate(delta));
        //
        //         lineRenderers[i].SetPosition(j,
        //             Vector3.Lerp(handPoses[i].transform.position, _currentGrapplePosition, delta) + offset);
        //     }
        //
        //     if (_animationCounter[i] > 0)
        //     {
        //         _animationCounter[i] -= 0.01f;
        //     }
        //     else
        //     {
        //         _animationCounter[i] = 0;
        //     }
        // }


        for (int i = 0; i < 2; i++)
        {
            if (_isGrappling[i] || _isSwinging[i])
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
            if (_isSwinging[i])
                return;
            Ray ray = new(handPoses[i].transform.position, handPoses[i].transform.forward);

            if (!Physics.Raycast(ray, out RaycastHit hit, maxRopeDistance))
            {
                if (!Physics.SphereCast(ray, castRadius, out hit, maxRopeDistance))
                {
                    return;
                }
            }
            Pulse(fireDuration, fireFrequency, fireAmplitude, i);
            attachmentPoints[i] = hit.point;
            _isSwinging[i] = true;
            springJoints[i] = gameObject.AddComponent<SpringJoint>();
            springJoints[i].autoConfigureConnectedAnchor = false;
            springJoints[i].connectedAnchor = attachmentPoints[i];

            float distanceFromGrapplePoint = Vector3.Distance(handPoses[i].transform.position, attachmentPoints[i]);

            springJoints[i].maxDistance = distanceFromGrapplePoint * maxSpringDistance;
            springJoints[i].minDistance = distanceFromGrapplePoint * minSpringDistance;
            float mass = _rb.mass;
            springJoints[i].spring = spring * mass;
            springJoints[i].damper = damper * mass;
            springJoints[i].massScale = massScale * mass;
            
        };
    }

    private SteamVR_Action_Boolean.StateUpHandler EndSwing(int i)
    {
        return delegate(SteamVR_Action_Boolean action, SteamVR_Input_Sources source)
        {
            _isSwinging[i] = false;
            Destroy(springJoints[i]);
        };
    }

    private SteamVR_Action_Boolean.StateDownHandler StartGrapple(int i)
    {
        
        return delegate(SteamVR_Action_Boolean action, SteamVR_Input_Sources source)
        {
            
            if(_isGrappling[0] || _isGrappling[1])
                return;

            Pulse( fireDuration, fireFrequency, fireAmplitude, i);

            Ray ray = new(handPoses[i].transform.position, handPoses[i].transform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, maxRopeDistance))
            {
                if (!Physics.SphereCast(ray, castRadius, out hit, maxRopeDistance))
                {
                    return;
                }
            }
            
            for (int j = 0; j < 2; j++)
            {
                _isSwinging[j] = false;
                Destroy(springJoints[j]);
            }
            
            if (hit.transform.gameObject.layer == 5) // 5 if object is in UI layer
                return;
            
            _isGrappling[i] = true;
            _isFrozen = true;
            attachmentPoints[i] = hit.point;
            
            StartCoroutine(ExecuteGrapple(i));
        };
    }

    private IEnumerator ExecuteGrapple(int i)
    {
        Pulse(hitDuration, hitFrequency, hitAmplitude, i);
        yield return new WaitForSeconds(_grappleTimeDelay);
        _isFrozen = false;
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
        _rb.velocity = requiredVelocity;

        StartCoroutine(EndGrapple(i));
    }

    private IEnumerator EndGrapple(int i)
    {
        yield return new WaitForSeconds(1f);
        _isGrappling[i] = false;

    }

    private void Pulse(float duration, float frequency, float amplitude, int hand)
    {
        hapticAction.Execute(0, duration, frequency, amplitude, hand == 0? SteamVR_Input_Sources.LeftHand : SteamVR_Input_Sources.RightHand);
    }
}
