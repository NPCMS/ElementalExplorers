using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;


public class GrappleController : MonoBehaviour
{
    [Header("Reference To Player")]
    [SerializeReference]
    [Tooltip("Reference to player using reference as parent-child structure may change")]
    private GameObject playerGameObject;

    [Header("Input Settings")] [SerializeField]
    private SteamInputCore.Hand grappleHand;

    [SerializeField] private SteamInputCore.Button grappleButton;

    [Header("Grapple Settings")] [SerializeField]
    private float maxGrappleLength = 300f;

    [SerializeField] private float grappleStrength = 100f;

    [Header("Grapple Force Falloff Settings")]
    [Tooltip(
        "Curve that controls falloff of grapple when spammed, should be a curve between (0,1) and (1, max falloff value)")]
    [SerializeField]
    private AnimationCurve falloffCurve;

    [SerializeField] private Gradient falloffColour;
    [SerializeReference] private MeshRenderer gauntletMesh;
    [SerializeField] private float frequencyWindowLength = 2f;
    private float maxGrapplesPerSec = 3f;
    private float minGrapplesPerSec = 1f;
    public int currentGrapplesInWindow = 0;
    private float grappleFrequencyMultiplier = 1;

    [Header("Correction Force Falloff Settings")]
    [Tooltip(
        "Curve that controls falloff of correction force applied when you get too close to a building, " +
        "should be a curve between (0,1) and (1, max falloff distance)")]
    [SerializeField]
    private AnimationCurve correctionFalloffCurve;
    [SerializeField] private float correctionForceMultiplier = 100;
    [SerializeField] private float maximumDistanceForCorrectionForce = 3;
    private ForceMode forceMode = ForceMode.Impulse;


    [Header("Constraints on Movement")] [SerializeField]
    private float maxAerialXZVelocity = 5;

    [SerializeField] private float maxAerialYVelocity = 15;

    [Header("Hand Motion Settings")] [SerializeField]
    private float thresholdToRegisterGrapple = 10;

    [Header("Rope Animation Settings")] [SerializeReference]
    private LineRenderer lineRenderer;

    [Header("Audio Sources")] [SerializeField]
    private AudioSource grappleFire;

    [SerializeField] private AudioSource grappleReel;

    // control variables
    [FormerlySerializedAs("_isGrappling")] public bool isGrappling;
    [FormerlySerializedAs("_isSwinging")] public bool isSwinging;

    [FormerlySerializedAs("_grappleBroken")]
    public bool grappleBroken;

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
        // update controller velocity used for grapple pull in
        UpdateControllerMotionVector();
        // check if hand mvoement threshold has been met to trigger grapple
        CheckForHandMoveIfGrappling();
        // check how frequently the player is grappling to prevent spam
        UpdateGrappleForceMultipler();
        //apply force to prevent collisions with buildings and other colliders.
        ApplyCorrectionForce();
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

        if ((!isGrappling) && !grappleBroken && _steamInput.GetInput(grappleHand, grappleButton))
        {
            StartGrapple();
        }

        if ((isGrappling) && _steamInput.GetInputUp(grappleHand, grappleButton))
        {
            EndGrapple();
        }
    }

    private void StartGrapple()
    {
        RaycastHit hit;
        grappleFire.pitch = Random.Range(0.95f, 1.05f);
        grappleFire.Play();
        int playerLayer = 1 << 6;
        if (!Physics.Raycast(transform.position, transform.forward, out hit, maxGrappleLength, ~playerLayer))
        {
            if (!Physics.SphereCast(transform.position, 0.5f, transform.forward, out hit, maxGrappleLength, ~playerLayer))
                return;
        }

        if (hit.transform.gameObject.layer == 5) return; // if object is in UI layer don't grapple to it
        grappleReel.pitch = Random.Range(0.95f, 1.05f);
        grappleReel.Play();
        // setup params
        _grappleHitLocation = hit.point;
        isGrappling = true;
        // increment grapple counter
        currentGrapplesInWindow++;
        Invoke(nameof(DecrementGrappleCount), 3f);
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
            Debug.Log("Adding Force: " + playerToGrapple.normalized * (grappleStrength * grappleFrequencyMultiplier));
            _playerRigidbodyRef.AddForce(playerToGrapple.normalized * (grappleStrength * grappleFrequencyMultiplier),
                ForceMode.Impulse);
            _controllerMotionVelocites.Clear();
            _controllerMotionVector = Vector3.zero;
            grappleBroken = true;

            // do haptic
            _steamInput.Vibrate(grappleHand, 0.25f, 100, 0.8f);


            EndGrapple();
        }
    }


    // -----------------------------------------------------------------------------------------------------------------
    // GRAPPLE FREQUENCY CODE: calculates the frequency at which the grapple is fired
    // -----------------------------------------------------------------------------------------------------------------

    private void DecrementGrappleCount()
    {
        currentGrapplesInWindow--;
    }

    private void UpdateGrappleForceMultipler()
    {
        float grappleFrequency = currentGrapplesInWindow / frequencyWindowLength;
        float normalisedFrequency = Mathf.Clamp((grappleFrequency - minGrapplesPerSec) /
                                                (maxGrapplesPerSec - minGrapplesPerSec), 0f, 1f);
        grappleFrequencyMultiplier = falloffCurve.Evaluate(normalisedFrequency);
        Color gauntletCol = falloffColour.Evaluate(normalisedFrequency);
        gauntletCol = new Color(gauntletCol.r * 50, gauntletCol.g * 50, gauntletCol.b * 50);
        gauntletMesh.materials[1].SetColor("_GlowColour", gauntletCol);
    }


    // -----------------------------------------------------------------------------------------------------------------
    // GRAPPLE CORRECTION FORCE CODE: calculates the correction force applied to the player to prevent collisions.
    // -----------------------------------------------------------------------------------------------------------------

    private void ApplyCorrectionForce()
    {
        if (_playerRigidbodyRef.velocity.magnitude < 5f)
            return;

        Vector3 playerPos = playerGameObject.transform.position;
        Vector3 rayDirection = _playerRigidbodyRef.velocity.normalized;
        rayDirection.y = 0;
        // raycast in velocity direction to check for future collisions
        // Physics.SphereCast()
        // Do not apply correction force to no raycast and player layers
        int ignoreLayers = (1 << 6) + (1 << 2);
        if (!Physics.SphereCast(playerPos, 0.5f, rayDirection,
                out var hit, maximumDistanceForCorrectionForce, ~ ignoreLayers)) return;

        // breakout cases
        if (hit.transform.gameObject.name == "Terrain")
            return;
        if (hit.distance > 3)
            return;
        if (_playerRigidbodyRef.velocity.magnitude < 3)
            return;
        
        // get D (distance)
        float distance = Vector3.Distance(hit.point, playerPos);
        // normalise D[0,maximumDistanceForCorrectionForce] => [0,1]
        float normalisedDistance = distance / maximumDistanceForCorrectionForce;
        // sample fall off curve to check for force
        float multplier = correctionFalloffCurve.Evaluate(normalisedDistance);
        Vector3 forceVector = hit.normal * (multplier * correctionForceMultiplier);
        forceVector.y = 0;
        // apply force
        _playerRigidbodyRef.AddForce(forceVector, forceMode);
    }
}