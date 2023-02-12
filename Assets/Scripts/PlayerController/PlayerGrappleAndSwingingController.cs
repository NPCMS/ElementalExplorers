using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerGrappleAndSwingingController : MonoBehaviour
{
    [Header("Reference To Player Controller")]
    [SerializeReference]
    [Tooltip("Reference to player controller script on parent, using reference as parent-child structure may change")]
    private PlayerController playerController;

    [Header("Grapple Settings")]
    // external parameters
    [SerializeField]
    [Tooltip("The input used to trigger the grapple")]
    private KeyCode grappleKey;

    [SerializeField] [Tooltip("The distance in m at which the grapple will fire")]
    private float maxGrappleLength;

    [SerializeField] [Tooltip("The time delay where the player is frozen upon grappling, in S")]
    private float grappleTimeDelay;

    [SerializeField]
    [Tooltip("When grappling, how much to overshoot the arc to ensure the player reaches the destination")]
    private float overshootYAxis;

    [SerializeReference]
    [Tooltip(
        "The transform where the line renderer should connect to on the player, usually an empty gameobject that's a correctly positioned child of the grapple gun")]
    private Transform grappleFirePoint;

    [SerializeField]
    [Tooltip("The acutal hook that gets fired, should have a particle system attached to play on impact")]
    private Transform grappleHook;

    [Header("Swinging Settings")]
    // external parameters
    [SerializeField]
    [Tooltip("The input used to trigger the swing")]
    private KeyCode swingKey;

    [SerializeField] [Tooltip("The distance in m at which the grapple will fire")]
    private float maxSwingLength;


    [Header("Rope Animation Settings")] [SerializeField] [Tooltip("Segments in the line renderer, used for animation")]
    private int ropeAnimationQuality;

    [SerializeField] [Tooltip("number of sine waves that should appear in the rope")]
    private float waveCount;

    [SerializeField] [Tooltip("height of sine waves that appear in the rope")]
    private float waveHeight;

    [SerializeField]
    [Tooltip("where should the sine waves appear along the rope, generally, high in middle and low at both ends")]
    private AnimationCurve affectCurve;

    // internal parameters
    private LayerMask _ignoreRaycastLayerMask;

    // grapple animation
    private Vector3 _grappleHitLocation;
    private Vector3 _currentGrapplePosition;
    private float _animationCounter;
    private bool _playParticlesOnce;
    private Vector3 _grappleHookOffsetFromGunTip;

    // references
    private Transform _cameraRef;
    private SpringJoint _springJoint;
    private LineRenderer _lineRenderer;


    // control variables
    private bool _isGrappling;
    private bool _isSwinging;

    // Start is called before the first frame update
    void Start()
    {
        // setup refs
        if (Camera.main != null) _cameraRef = Camera.main.transform;
        _lineRenderer = gameObject.GetComponent<LineRenderer>();

        // setup layer mask
        _ignoreRaycastLayerMask = LayerMask.GetMask("Ignore Raycast");

        // set default grapple hook location
        _grappleHookOffsetFromGunTip = grappleHook.position - grappleFirePoint.position;
    }

    // Update is called once per frame
    void Update()
    {
        // can only be grappling or swinging
        HandleGrapple();
        HandleSwing();
    }

    private void LateUpdate()
    {
        // handles line renderer
        DrawRope();
    }

    // -----------------------------------------------------------------------------------------------------------------
    // GRAPPLING: Grappling is when a user is launched towards their hit point allowing a gain in vertical or horizontal
    // height. It is stronger than swinging and basically is goto where I clicked, should probably have a cooldown
    // credit for some implementation details: https://github.com/DaveGameDevelopment/Grappling-Tutorial-GitHub/blob/main/Grappling%20-%20Tutorial%20(Unity%20Project)/Assets/Grappling.cs
    // -----------------------------------------------------------------------------------------------------------------
    private void HandleGrapple()
    {
        // if swinging then end function
        if (_isSwinging)
            return;

        if ((!_isGrappling) && Input.GetKeyDown(grappleKey))
        {
            StartGrapple();
        }
    }

    private void StartGrapple()
    {
        if (Physics.Raycast(_cameraRef.position, _cameraRef.forward, out var hit, maxGrappleLength))
        {
            // setup params
            _grappleHitLocation = hit.point;
            _playParticlesOnce = true;
            _isGrappling = true;
            // freeze player
            playerController.FreezePlayer();
            // execute grapple after time delay
            Invoke(nameof(ExecuteGrapple), grappleTimeDelay);
        }
    }

    // grapple logic taken from https://github.com/DaveGameDevelopment/Grappling-Tutorial-GitHub/blob/main/Grappling%20-%20Tutorial%20(Unity%20Project)/Assets/Grappling.cs
    private void ExecuteGrapple()
    {
        // unfreeze player
        playerController.UnFreezePlayer();
        // compute lowest point of arc
        var position = transform.position;
        Vector3 lowestPoint = new Vector3(position.x, position.y - 1f, position.z);
        // compute grapple params
        float grapplePointRelativeYPos = _grappleHitLocation.y - lowestPoint.y;
        float highestPointOnArc = grapplePointRelativeYPos + overshootYAxis;

        // if graple location is beneath player then set arc accordingly
        if (grapplePointRelativeYPos < 0) highestPointOnArc = overshootYAxis;

        // set player velocity on controller
        playerController.GrappleToPosition(_grappleHitLocation, highestPointOnArc);


        // in 1 second end the grapple
        Invoke(nameof(EndGrapple), 1f);
    }

    private void EndGrapple()
    {
        _isGrappling = false;
    }

    // -----------------------------------------------------------------------------------------------------------------
    // SWINGING: Swinging is the standard form of movement, it involves spiderman like movement using a spring
    // -----------------------------------------------------------------------------------------------------------------
    private void HandleSwing()
    {
        // if grappling then end function
        if (_isGrappling)
            return;

        if ((!_isSwinging) && Input.GetKeyDown(swingKey))
        {
            StartSwing();
        }

        if (_isSwinging && Input.GetKeyUp(swingKey))
        {
            EndSwing();
        }
    }

    // credit: https://www.youtube.com/watch?v=HPjuTK91MA8
    private void StartSwing()
    {
        if (Physics.Raycast(_cameraRef.position, _cameraRef.forward, out var hit, maxSwingLength))
        {
            // setup params
            _grappleHitLocation = hit.point;
            _playParticlesOnce = true;
            _isSwinging = true;

            // create spring
            GameObject playerControllerGameObject = playerController.gameObject;
            _springJoint = playerControllerGameObject.AddComponent<SpringJoint>();
            _springJoint.autoConfigureConnectedAnchor = false;
            _springJoint.connectedAnchor = _grappleHitLocation;

            float distanceFromGrapplePoint =
                Vector3.Distance(transform.position, _grappleHitLocation);

            // the distance grapple will try to keep from grapple point. 
            _springJoint.maxDistance = distanceFromGrapplePoint * 0.85f;
            _springJoint.minDistance = distanceFromGrapplePoint * 0.45f;

            // get mass
            float mass = playerControllerGameObject.GetComponent<Rigidbody>().mass;
            
            // set joint params
            _springJoint.spring = 6f * mass;
            _springJoint.damper = 8f * mass;
            _springJoint.massScale = 4.5f * mass;
        }
    }

    private void EndSwing()
    {
        _isSwinging = false;
        Destroy(_springJoint);
    }


    // -----------------------------------------------------------------------------------------------------------------
    // ROPE: The rope connects the player to their grapple/swing point the below code is responsible for rendering it in
    // aesthetic manor with animations. It runs independantly of the physics engine and causes the rope to appear to 
    // tighten and have a collision impact (particles)
    // -----------------------------------------------------------------------------------------------------------------
    // credit: https://github.com/affaxltd/rope-tutorial/blob/master/GrapplingRope.cs
    void DrawRope()
    {
        //If not grappling or swinging, don't draw rope
        if (!_isGrappling && !_isSwinging)
        {
            var firePointPosition = grappleFirePoint.position;
            _currentGrapplePosition = firePointPosition;
            grappleHook.position = firePointPosition + _grappleHookOffsetFromGunTip;
            if (_animationCounter < 1)
                _animationCounter = 1;
            if (_lineRenderer.positionCount > 0)
                _lineRenderer.positionCount = 0;
            return;
        }

        if (_lineRenderer.positionCount == 0)
        {
            _lineRenderer.positionCount = ropeAnimationQuality + 1;
        }

        var up = Quaternion.LookRotation((_grappleHitLocation - grappleFirePoint.position).normalized) * Vector3.up;

        _currentGrapplePosition = Vector3.Lerp(_currentGrapplePosition, _grappleHitLocation, Time.deltaTime * 100f);

        // update grapple head position
        grappleHook.position = _currentGrapplePosition;
        // check if grapple has hit yet
        if (((_currentGrapplePosition - _grappleHitLocation).magnitude < 0.1f) && _playParticlesOnce)
        {
            grappleHook.GetComponent<ParticleSystem>().Play();
            _playParticlesOnce = false;
        }

        for (var i = 0; i < ropeAnimationQuality + 1; i++)
        {
            var delta = i / (float) ropeAnimationQuality;
            var offset = up * waveHeight * Mathf.Sin(delta * waveCount * Mathf.PI) * _animationCounter *
                         affectCurve.Evaluate(delta);

            _lineRenderer.SetPosition(i,
                Vector3.Lerp(grappleFirePoint.position, _currentGrapplePosition, delta) + offset);
        }

        if (_animationCounter > 0)
        {
            _animationCounter -= 0.01f;
        }
        else
        {
            _animationCounter = 0;
        }
    }
}