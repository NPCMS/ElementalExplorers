using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class HandGrappleAndSwinging : MonoBehaviour
{
    [Header("Reference To Player")]
    [SerializeReference]
    [Tooltip("Reference to player using reference as parent-child structure may change")]
    private GameObject playerGameObject;

    [Header("Grapple Settings")] [SerializeField] [Tooltip("The distance in m at which the grapple will fire")]
    private float maxGrappleLength;

    [SerializeField] private SteamInputCore.Hand grappleHand;
    [SerializeField] private SteamInputCore.Button grappleButton;
    [SerializeField] private float grappleStrength = 100f;
    [SerializeField] private float maxAerialXZVelocity = 5;
    [SerializeField] private float maxAerialYVelocity = 15;
    

    [Header("Swinging Settings")] [SerializeField] [Tooltip("The input used to trigger the swing")]
    private KeyCode swingKey;

    [SerializeField] [Tooltip("The distance in m at which the grapple will fire")]
    private float maxSwingLength;

    [Header("Rope Animation Settings")]
    [SerializeReference] private LineRenderer lineRenderer;
    [SerializeField] [Tooltip("Segments in the line renderer, used for animation")]
    private int ropeAnimationQuality;

    [SerializeField]
    [Tooltip("The acutal hook that gets fired, should have a particle system attached to play on impact")]
    private Transform grappleHook;
    
    [SerializeField] [Tooltip("number of sine waves that should appear in the rope")]
    private float waveCount;

    [SerializeField] [Tooltip("height of sine waves that appear in the rope")]
    private float waveHeight;

    [SerializeField]
    [Tooltip("where should the sine waves appear along the rope, generally, high in middle and low at both ends")]
    private AnimationCurve affectCurve;

    [Header("sound FX")] [SerializeReference]
    private AudioSource grappleFireSound;

    // grapple animation
    private Vector3 _grappleHitLocation;
    private Vector3 _currentGrapplePosition;
    private float _animationCounter;
    private bool _playParticlesOnce;

    // references
    private Transform _cameraRef;
    private SpringJoint _springJoint;
    private Rigidbody _playerRigidbodyRef;
    private SteamInputCore.SteamInput steamInput;

    // control variables
    public bool _isGrappling;
    public bool _isSwinging;

    // Start is called before the first frame update
    void Start()
    {
        // setup refs
        if (Camera.main != null) _cameraRef = Camera.main.transform;
        _playerRigidbodyRef = playerGameObject.gameObject.GetComponent<Rigidbody>();
        steamInput = SteamInputCore.GetInput();
        
    }

    // Update is called once per frame
    void Update()
    {
        // can only be grappling or swinging
        HandleGrapple();
        // HandleSwing();
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

        if ((!_isGrappling) && steamInput.GetInputDown(grappleHand, grappleButton))
        {
            StartGrapple();
        }
    }

    private void StartGrapple()
    {
        if (Physics.Raycast(transform.position, transform.forward, out var hit, maxGrappleLength))
        {
            // setup params
            _grappleHitLocation = hit.point;
            _playParticlesOnce = true;
            _isGrappling = true;
            
            // add haptics
            steamInput.Vibrate(grappleHand, 0.05f, 120, 0.6f);
            // play sound
            grappleFireSound.Play();
            
            ExecuteGrapple();
        }
    }

    // grapple logic taken from https://github.com/DaveGameDevelopment/Grappling-Tutorial-GitHub/blob/main/Grappling%20-%20Tutorial%20(Unity%20Project)/Assets/Grappling.cs
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
        Invoke(nameof(EndGrapple), 0.2f);
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
            GameObject playerControllerGameObject = playerGameObject.gameObject;
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
            _springJoint.spring = 7f * mass;
            _springJoint.damper = 6f * mass;
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
            var firePointPosition = transform.position;
            _currentGrapplePosition = firePointPosition;
            grappleHook.position = firePointPosition;
            if (_animationCounter < 1)
                _animationCounter = 1;
            if (lineRenderer.positionCount > 0)
                lineRenderer.positionCount = 0;
            return;
        }

        if (lineRenderer.positionCount == 0)
        {
            lineRenderer.positionCount = ropeAnimationQuality + 1;
        }

        var up = Quaternion.LookRotation((_grappleHitLocation - grappleHook.position).normalized) * Vector3.up;

        _currentGrapplePosition = Vector3.Lerp(_currentGrapplePosition, _grappleHitLocation, Time.deltaTime * 100f);

        // update grapple head position
        // grappleHook.position = _currentGrapplePosition;
        // check if grapple has hit yet
        if (((_currentGrapplePosition - _grappleHitLocation).magnitude < 0.1f) && _playParticlesOnce)
        {
            // grappleHook.GetComponent<ParticleSystem>().Play();
            _playParticlesOnce = false;
        }

        for (var i = 0; i < ropeAnimationQuality + 1; i++)
        {
            var delta = i / (float) ropeAnimationQuality;
            var offset = up * waveHeight * Mathf.Sin(delta * waveCount * Mathf.PI) * _animationCounter *
                         affectCurve.Evaluate(delta);

            lineRenderer.SetPosition(i,
                Vector3.Lerp(grappleHook.position, _currentGrapplePosition, delta) + offset);
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
}