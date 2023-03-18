using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;


public class GrappleController : MonoBehaviour
{
    [Header("Reference To Player")]
    [SerializeReference]
    [Tooltip("Reference to player using reference as parent-child structure may change")]
    private GameObject playerGameObject;

    [Header("Input Settings")]
    [SerializeField] private SteamInputCore.Hand grappleHand;
    [SerializeField] private SteamInputCore.Button grappleButton;
    
    [Header("Grapple Settings")] 
    [SerializeField] private float maxGrappleLength = 300f;
    [SerializeField] private float grappleStrength = 100f;
    [SerializeField] private float grappleCooldown = 1f;

    [Header("Constraints on Movement")]
    [SerializeField] private float maxAerialXZVelocity = 5;
    [SerializeField] private float maxAerialYVelocity = 15;

    [Header("Hand Motion Settings")] 
    [SerializeField] private float thresholdToRegisterGrapple = 10;
    
    [Header("Rope Animation Settings")] 
    [SerializeReference] private LineRenderer lineRenderer;

    [Header("Audio Sources")] 
    [SerializeField] private AudioSource grappleFire;
    [SerializeField] private AudioSource grappleHit;
    [SerializeField] private AudioSource grappleReel;

    // control variables
    [FormerlySerializedAs("_isGrappling")] public bool isGrappling;
    [FormerlySerializedAs("_isSwinging")] public bool isSwinging;
    [FormerlySerializedAs("_grappleBroken")] public bool grappleBroken;
    private bool _grappleOnCooldown;
    
    // grapple animation
    private Vector3 _grappleHitLocation;
    private Vector3 _currentGrapplePosition;
    private float _animationCounter;

    // references
    private SpringJoint _springJoint;
    private Rigidbody _playerRigidbodyRef;
    private SteamInputCore.SteamInput _steamInput;
    
    // controller motion parameters
    private Vector3 _controllerLastFramePos;
    private Vector3 _controllerMotionVector = Vector3.zero;
    private float _timePeriodForMotionCalculation = 0.1f;
    private HashSet<Vector3> _controllerMotionVelocites = new HashSet<Vector3>();
    
    
    // Start is called before the first frame update
    void Start()
    {
        // setup refs
        _playerRigidbodyRef = playerGameObject.gameObject.GetComponent<Rigidbody>();
        _steamInput = SteamInputCore.GetInput();

        // init
        _controllerLastFramePos = transform.position;
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
        _steamInput.GetInputUp(grappleHand, grappleButton);
    }

    // -----------------------------------------------------------------------------------------------------------------
    // GRAPPLING: Grappling is when a user is launched towards their hit point allowing a gain in vertical or horizontal
    // height. It is stronger than swinging and basically is goto where I clicked, should probably have a cooldown
    // -----------------------------------------------------------------------------------------------------------------
    private void HandleGrapple()
    {
        // if swinging then end function
        if (isSwinging)
            return;

        if (_steamInput.GetInputDown(grappleHand, grappleButton))
        {
            grappleBroken = false;
        }
        
        if ((!isGrappling) && !grappleBroken && _steamInput.GetInput(grappleHand, grappleButton) && !_grappleOnCooldown)
        {
            StartGrapple();
            StartCoroutine(nameof(StartGrappleCooldown));
        }

        if ((isGrappling) && _steamInput.GetInputUp(grappleHand, grappleButton))
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
        grappleReel.Play();
        // setup params
        _grappleHitLocation = hit.point;
        isGrappling = true;
        grappleHit.transform.position = hit.transform.position;
        grappleHit.Play();
        // add haptics
        _steamInput.Vibrate(grappleHand, 0.1f, 120, 0.6f);
    }
    
    private void EndGrapple()
    {
        grappleBroken = true;
        if (!isGrappling)
            return;

        isGrappling = false;
    }

    private IEnumerator StartGrappleCooldown()
    {
        // TODO: play animation for visual feedback
        _grappleOnCooldown = true;
        yield return new WaitForSeconds(grappleCooldown);
        _grappleOnCooldown = false;
    } 

    // -----------------------------------------------------------------------------------------------------------------
    // ROPE: The rope connects the player to their grapple/swing point
    // -----------------------------------------------------------------------------------------------------------------

    void DrawRope()
    {
        // if player grappling
        if (isGrappling)
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

    // -----------------------------------------------------------------------------------------------------------------
    // HAND MOTION: calculations for tracking the velocity of the hand over a given time frame
    // -----------------------------------------------------------------------------------------------------------------
    
    private void UpdateControllerMotionVector()
    {
        Vector3 positionDifference = _controllerLastFramePos - transform.localPosition;
        Vector3 velocity = positionDifference / Time.deltaTime;
        _controllerMotionVelocites.Add(velocity);
        StartCoroutine(RemoveVelocityFromControllerMotionVector(velocity));
        _controllerMotionVector = CalculateControllerAcceleration();
        Debug.DrawLine(transform.position, transform.position + _controllerMotionVector);
        _controllerLastFramePos = transform.localPosition;
    }
    
    private IEnumerator RemoveVelocityFromControllerMotionVector(Vector3 vel)
    {
        yield return new WaitForSeconds(_timePeriodForMotionCalculation);
        _controllerMotionVelocites.Remove(vel);
    }

    private Vector3 CalculateControllerAcceleration()
    {
        Vector3 result = Vector3.zero;
        foreach (Vector3 vel in _controllerMotionVelocites)
        {
            result += vel;
        }

        return (result / _controllerMotionVelocites.Count) / _timePeriodForMotionCalculation;
    }

    private void CheckForHandMoveIfGrappling()
    {
        if (!isGrappling)
            return;

        // calculate is hand pull is in valid directions
        float dot = Vector3.Dot(_controllerMotionVector.normalized,
            (_grappleHitLocation - transform.position).normalized);
        if (dot > 0.75f && _controllerMotionVector.magnitude > thresholdToRegisterGrapple)
        {
            // // clamp velocity on XZ
            var velocity = _playerRigidbodyRef.velocity;
            Vector2 xzVel = new Vector2(velocity.x, velocity.z);
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
            _playerRigidbodyRef.AddForce(playerToGrapple.normalized * grappleStrength, ForceMode.Impulse);
            _controllerMotionVelocites.Clear();
            _controllerMotionVector = Vector3.zero;
            grappleBroken = true;

            // do haptic
            _steamInput.Vibrate(grappleHand, 0.25f, 100, 0.8f);


            EndGrapple();
        }
    }
    
}