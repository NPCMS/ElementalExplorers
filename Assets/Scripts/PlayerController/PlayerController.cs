using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // external parameters
    [Header("Ground Movement Settings")]
    [SerializeField]
    [Tooltip("Speed at which player will accelerate on the ground usind WASD style movement")]
    private float groundedAcceleration;

    [SerializeField]
    [Tooltip(
        "Custom drag implementation, each frame drag is calculated as velocity^2 * drag, then velocity is updated to be velocity - (1 - deltaTime * drag)")]
    private float groundedXZDrag;
    
    [Header("Post Processing References")]
    [SerializeReference]
    [Tooltip("Reference to colour property of anime speed lines asset")]
    private Material animeSpeedLinesMaterial;


    // internal parameters
    private LayerMask _ignoreRaycastLayerMask;
    private static readonly int Colour = Shader.PropertyToID("_Colour");

    // references
    private Transform _playerTransform;
    private Rigidbody _playerRigidbody;
    private Collider _playerCollider;
    private Transform _cameraRef;
    private SpringJoint _springJoint;
    private LineRenderer _lineRenderer;

    // control variables
    public bool _isGrounded;

    // prevents the value of _isGrounded updating whilst true, used for grapple and swinging to prevent velocity clamping
    // whilst the user gets going
    private bool _lockIsGrounded;
    private bool _isFrozen; // used for effects, freezing player when grappling adds umph


    // Start is called before the first frame update
    void Start()
    {
        // init get components here for performance
        _playerTransform = gameObject.GetComponent<Transform>();
        _playerRigidbody = gameObject.GetComponent<Rigidbody>();
        _playerCollider = gameObject.GetComponent<Collider>();
        if (Camera.main != null) _cameraRef = Camera.main.transform;

        // setup layer mask
        _ignoreRaycastLayerMask = LayerMask.GetMask("Ignore Raycast");
        
        // turn off speed lines
        animeSpeedLinesMaterial.SetColor(Colour, new Color(1, 1, 1, 0));
    }

    // Update is called once per frame
    void Update()
    {
        // check if grounded
        if (!_lockIsGrounded)
            _isGrounded = CheckIfPlayerGrounded();

        // if grounded then perform ground movement
        if (_isGrounded && (!_isFrozen))
            CharacterMovementGrounded();

        // if frozen, no movement
        if (_isFrozen)
            _playerRigidbody.velocity = Vector3.zero;

        // apply post processing
        ApplyPostProcessingEffects();
    }

    // guess what this function does?
    private bool CheckIfPlayerGrounded()
    {
        var bounds = _playerCollider.bounds;
        return Physics.CheckCapsule(bounds.center, new Vector3(bounds.center.x, bounds.min.y - 0.1f, bounds.center.z),
            0.01f);
    }

    // handles WASD style movement (z is forward)
    private void CharacterMovementGrounded()
    {
        // get inputs, range [-1,1]
        float forwardInput = Input.GetAxis("Vertical");
        float lateralInput = Input.GetAxis("Horizontal");

        // get camera directions
        Vector3 cameraForward = _cameraRef.forward;
        Vector3 cameraRight = _cameraRef.right;
        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward = Vector3.Normalize(cameraForward);
        cameraRight = Vector3.Normalize(cameraRight);

        // create impulse vector
        Vector3 impulseVector = cameraForward * forwardInput + cameraRight * lateralInput;
        // adjust to match acceleration
        impulseVector *= groundedAcceleration;

        // Add impulse
        _playerRigidbody.AddForce(impulseVector, ForceMode.Impulse);

        // apply drag only on XZ plane
        var velocity = _playerRigidbody.velocity;
        Vector2 xzVelocity = new Vector2(velocity.x, velocity.z);
        // calculate drag
        float dragForceMagnitude = Mathf.Pow(xzVelocity.magnitude, 2) * groundedXZDrag;
        xzVelocity = xzVelocity * (1 - Time.deltaTime * groundedXZDrag);
        // update velocity
        _playerRigidbody.velocity = new Vector3(xzVelocity.x, _playerRigidbody.velocity.y, xzVelocity.y);
    }

    private void ApplyPostProcessingEffects()
    {
        // max speed reasonably achievable rn is ~ 20 m/s
        float speedProportion = Mathf.Clamp((_playerRigidbody.velocity.magnitude - 18) / (20.0f), 0f, 1f);
        animeSpeedLinesMaterial.SetColor(Colour, new Color(1,1,1, speedProportion));
    }

    private void LockIsGrounded()
    {
        _lockIsGrounded = true;
    }

    private void UnlockIsGrounded()
    {
        _lockIsGrounded = false;
    }

    // -----------------------------------------------------------------------------------------------------------------
    // PUBLIC CONTROL METHODS
    // -----------------------------------------------------------------------------------------------------------------

    public void FreezePlayer()
    {
        _isFrozen = true;
    }

    public void UnFreezePlayer()
    {
        _isFrozen = false;
    }

    // pelts the player to a point, can guarantee they hit the point using some maths
    // credit: https://github.com/DaveGameDevelopment/Grappling-Tutorial-GitHub/blob/main/Grappling%20-%20Tutorial%20(Unity%20Project)/Assets/PlayerMovementGrappling.cs
    public void GrappleToPosition(Vector3 position, float trajectoryHeight)
    {
        // Get values to prevent repeated access
        Vector3 playerPos = _playerTransform.position;


        // MATHS:
        float gravity = Physics.gravity.y;
        float displacementY = position.y - playerPos.y;

        // must pre check the sign of this value to ensure it's not negative otherwise we sqrt a negative num and break
        // the calculation
        float potentiallyNegativeSqrt = 2 * (displacementY - trajectoryHeight) / gravity;
        if (potentiallyNegativeSqrt <= 0)
            potentiallyNegativeSqrt = 0;


        Vector3 displacementXZ = new Vector3(position.x - playerPos.x, 0f, position.z - playerPos.z);

        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * trajectoryHeight);
        Vector3 velocityXZ = displacementXZ / (Mathf.Sqrt(-2 * trajectoryHeight / gravity)
                                               + Mathf.Sqrt(potentiallyNegativeSqrt));
        Vector3 requiredVelocity = velocityXZ + velocityY;

        // apply calculated velocity
        _playerRigidbody.velocity = requiredVelocity;
        // prevent ground physics (clamping and user input) from interfering for first 0.3s of grapple
        _isGrounded = false;
        LockIsGrounded();
        Invoke(nameof(UnlockIsGrounded), 0.3f);
    }
}