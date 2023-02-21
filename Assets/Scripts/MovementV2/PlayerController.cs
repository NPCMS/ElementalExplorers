using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using Valve.VR;

public class PlayerController : MonoBehaviour
{
    // external parameters
    [Header("Ground Movement Settings")] [Tooltip("joystick input")] [SerializeField]
    SteamVR_Action_Vector2 inputAxis;

    [SerializeReference] private HandGrappleAndSwinging leftHand;
    [SerializeReference] private HandGrappleAndSwinging rightHand;
    [SerializeReference] private Transform vrCameraRef;
    [SerializeReference] private Collider _playerCollider; // player collider isn't on player body :(

    [SerializeField] [Tooltip("Speed at which player will accelerate on the ground usind WASD style movement")]
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
    private static readonly int Colour = Shader.PropertyToID("_Colour");

    // references
    private Rigidbody _playerRigidbody;
    private SpringJoint _springJoint;
    private LineRenderer _lineRenderer;

    // control variables
    public bool _isGrounded;

    // prevents the value of _isGrounded updating whilst true, used for grapple and swinging to prevent velocity clamping
    // whilst the user gets going
    private bool _disableGroundCheck;


    // Start is called before the first frame update
    void Start()
    {
        // init get components here for performance
        _playerRigidbody = gameObject.GetComponent<Rigidbody>();
        _disableGroundCheck = false;
    }

    // Update is called once per frame
    void Update()
    {
        // check if grounded
        _isGrounded = CheckIfPlayerGrounded();

        // check if swinging / grappling
        bool applyGroundMechanics = ApplyGroundMechanics();
        
        // if grounded then perform ground movement
        if (_isGrounded && applyGroundMechanics)
            CharacterMovementGrounded();
    }

    private bool ApplyGroundMechanics()
    {
        return !leftHand._isGrappling && !rightHand._isGrappling && !leftHand._isSwinging && !rightHand._isSwinging;
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
        float forwardInput = inputAxis.axis.y;
        float lateralInput = inputAxis.axis.x;

        // get camera directions
        Vector3 cameraForward = vrCameraRef.forward;
        Vector3 cameraRight = vrCameraRef.right;
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

}