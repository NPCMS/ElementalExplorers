using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class HandGrappleAndSwinging : MonoBehaviour
{
    [Header("Reference To Player")]
    [SerializeReference]
    [Tooltip("Reference to player using reference as parent-child structure may change")]
    private GameObject playerGameObject;

    [Header("Grapple Settings")] [SerializeField]
    private float maxGrappleLength;

    [SerializeField] private SteamInputCore.Hand grappleHand;
    [SerializeField] private SteamInputCore.Button grappleButton;
    [SerializeField] private float grappleStrength = 100f;
    [SerializeField] private float maxAerialXZVelocity = 5;
    [SerializeField] private float maxAerialYVelocity = 15;

    [Header("Rope Animation Settings")] [SerializeReference]
    private LineRenderer lineRenderer;

    [Header("Hand Motion Settings")] [SerializeField]
    private float handMotionStrength = 100f;

    // grapple animation
    private Vector3 _grappleHitLocation;
    private Vector3 _currentGrapplePosition;
    private float _animationCounter;
    private bool _playParticlesOnce;

    // references
    private SpringJoint _springJoint;
    private Rigidbody _playerRigidbodyRef;
    private SteamInputCore.SteamInput steamInput;

    // control variables
    public bool _isGrappling;
    public bool _isSwinging;
    public bool _grappleBroken;

    // controller motion parameters
    private Vector3 controllerLastFramePos;
    private Vector3 controllerMotionVector = Vector3.zero;
    private float timePeriodForMotionCalculation = 0.1f;
    private HashSet<Vector3> controllerMotionVelocites = new HashSet<Vector3>();


    [SerializeField] private SteamInputCore.Hand hand;
    private readonly List<Action<Vector3, SteamInputCore.Hand>> beginCallbacks = new();
    private readonly List<Action<SteamInputCore.Hand>> endCallbacks = new();

    [Header("Audio Sources")] 
    [SerializeField] private AudioSource grappleFire;
    [SerializeField] private AudioSource grappleHit;


    // Start is called before the first frame update
    void Start()
    {
        // setup refs
        _playerRigidbodyRef = playerGameObject.gameObject.GetComponent<Rigidbody>();
        steamInput = SteamInputCore.GetInput();

        // init
        controllerLastFramePos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        // can only be grappling or swinging
        HandleGrapple();
        UpdateControllerMotionVector();
        CheckForHandMoveIfGrappling();
    }

    private void LateUpdate()
    {
        DrawRope();
        steamInput.GetInputUp(grappleHand, grappleButton);
    }

    // -----------------------------------------------------------------------------------------------------------------
    // GRAPPLING: Grappling is when a user is launched towards their hit point allowing a gain in vertical or horizontal
    // height. It is stronger than swinging and basically is goto where I clicked, should probably have a cooldown
    // -----------------------------------------------------------------------------------------------------------------
    private void HandleGrapple()
    {
        // if swinging then end function
        if (_isSwinging)
            return;

        if (steamInput.GetInputDown(grappleHand, grappleButton))
        {
            _grappleBroken = false;
        }
        
        if ((!_isGrappling) && !_grappleBroken && steamInput.GetInput(grappleHand, grappleButton))
        {
            StartGrapple();
        }

        if ((_isGrappling) && steamInput.GetInputUp(grappleHand, grappleButton))
        {
            
            EndGrapple();
        }
    }

    private void StartGrapple()
    {
        RaycastHit hit;
        grappleFire.Play();
        if (!Physics.Raycast(transform.position, transform.forward, out hit, maxGrappleLength))
        {
            if (!Physics.SphereCast(transform.position, 0.5f, transform.forward, out hit, maxGrappleLength))
                return;
        }

        if (hit.transform.gameObject.layer == 5) return; // if object is in UI layer don't grapple to it
        // setup params
        _grappleHitLocation = hit.point;
        _playParticlesOnce = true;
        _isGrappling = true;
        grappleHit.transform.position = hit.transform.position;
        grappleHit.Play();
        // add haptics
        steamInput.Vibrate(grappleHand, 0.1f, 120, 0.6f);

        foreach (var callback in beginCallbacks)
        {
            callback(hit.point, hand);
        }
    }

    private void ExecuteGrapple()
    {
        // compute vector from player to points
        Vector3 grappleDirection = (_grappleHitLocation - transform.position).normalized;

        // set y velocity to 0
        var velocity = _playerRigidbodyRef.velocity;
        if (velocity.y < 0)
        {
            velocity = new Vector3(velocity.x, 0, velocity.z);
        }

        _playerRigidbodyRef.velocity = velocity;

        _playerRigidbodyRef.AddForce(grappleDirection * grappleStrength, ForceMode.Impulse);


        // clamp velocity on XZ
        Vector2 xzVel = new Vector2(_playerRigidbodyRef.velocity.x, _playerRigidbodyRef.velocity.z);
        if (xzVel.magnitude > maxAerialXZVelocity)
        {
            xzVel = xzVel.normalized * maxAerialXZVelocity;
        }

        // clamp velocity on Y
        float yVel = _playerRigidbodyRef.velocity.y;
        if (yVel > maxAerialYVelocity)
        {
            yVel = Mathf.Sign(yVel) * maxAerialYVelocity;
        }

        _playerRigidbodyRef.velocity = new Vector3(xzVel.x, yVel, xzVel.y);


        // in 1 second end the grapple
        //Invoke(nameof(EndGrapple), 0.2f);
    }

    private void EndGrapple()
    {
        _grappleBroken = true;
        if (!_isGrappling)
            return;

        _isGrappling = false;
        foreach (var callback in endCallbacks)
        {
            callback(hand);
        }
    }

    // -----------------------------------------------------------------------------------------------------------------
    // ROPE: The rope connects the player to their grapple/swing point the below code is responsible for rendering it in
    // aesthetic manor with animations. It runs independantly of the physics engine and causes the rope to appear to 
    // tighten and have a collision impact (particles)
    // -----------------------------------------------------------------------------------------------------------------

    // credit: https://github.com/affaxltd/rope-tutorial/blob/master/GrapplingRope.cs
    void DrawRope()
    {
        // if player grappling
        if (_isGrappling)
        {
            // if line renderer not present then add points
            if (lineRenderer.positionCount == 0)
                lineRenderer.positionCount = 2;
            // put first point on players hand and second at grapple location
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, _grappleHitLocation);
        }
        else
        {
            // if renderer is still active make inactive
            if (lineRenderer.positionCount > 0)
                lineRenderer.positionCount = 0;
        }
    }

    private void UpdateControllerMotionVector()
    {
        Vector3 positionDifference = controllerLastFramePos - transform.localPosition;
        Vector3 velocity = positionDifference / Time.deltaTime;
        controllerMotionVelocites.Add(velocity);
        StartCoroutine(RemoveVelocityFromControllerMotionVector(velocity));
        controllerMotionVector = CalculateControllerAcceleration();
        Debug.DrawLine(transform.position, transform.position + controllerMotionVector);
        controllerLastFramePos = transform.localPosition;
    }

    private IEnumerator RemoveVelocityFromControllerMotionVector(Vector3 vel)
    {
        yield return new WaitForSeconds(timePeriodForMotionCalculation);
        controllerMotionVelocites.Remove(vel);
    }

    private Vector3 CalculateControllerAcceleration()
    {
        Vector3 result = Vector3.zero;
        foreach (Vector3 vel in controllerMotionVelocites)
        {
            result += vel;
        }

        return (result / controllerMotionVelocites.Count) / timePeriodForMotionCalculation;
    }

    private void CheckForHandMoveIfGrappling()
    {
        if (!_isGrappling)
            return;

        // calculate is hand pull is in valid directions
        float dot = Vector3.Dot(controllerMotionVector.normalized,
            (_grappleHitLocation - transform.position).normalized);
        if (dot > 0.75f && controllerMotionVector.magnitude > 10)
        {
            // // clamp velocity on XZ
            Vector2 xzVel = new Vector2(_playerRigidbodyRef.velocity.x, _playerRigidbodyRef.velocity.z);
            if (xzVel.magnitude > maxAerialXZVelocity)
            {
                xzVel = xzVel.normalized * maxAerialXZVelocity;
            }
            
            // clamp velocity on Y
            float yVel = _playerRigidbodyRef.velocity.y;
            if (MathF.Abs(yVel) > maxAerialYVelocity)
            {
                yVel = Mathf.Sign(yVel) * maxAerialYVelocity;
            }
            
            // reset y vel if negative
            if (yVel < 0)
            {
                yVel = 0;
            }

            _playerRigidbodyRef.velocity = new Vector3(xzVel.x, yVel, xzVel.y);

            Vector3 playerToGrapple = _grappleHitLocation - playerGameObject.transform.position;

            // add hand force
            _playerRigidbodyRef.AddForce(playerToGrapple.normalized * handMotionStrength, ForceMode.Impulse);
            controllerMotionVelocites.Clear();
            controllerMotionVector = Vector3.zero;
            _grappleBroken = true;

            // do haptic
            steamInput.Vibrate(grappleHand, 0.25f, 100, 0.8f);


            EndGrapple();
        }
    }


    public void AddBeginCallback(Action<Vector3, SteamInputCore.Hand> a)
    {
        beginCallbacks.Add(a);
    }

    public void AddEndCallback(Action<SteamInputCore.Hand> a)
    {
        endCallbacks.Add(a);
    }
}