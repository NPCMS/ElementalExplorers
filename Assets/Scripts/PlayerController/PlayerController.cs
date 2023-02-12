using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // external parameters
    [SerializeField] private float maxGroundedMoveSpeed;
    [SerializeField] private float groundedAcceleration;

    // internal parameters
    private LayerMask _ignoreRaycastLayerMask;

    // references
    private Transform _playerTransform;
    private Rigidbody _playerRigidbody;
    private Transform _cameraRef;


    // control variables
    public bool _isGrounded;


    // Start is called before the first frame update
    void Start()
    {
        // init get components here for performance
        _playerTransform = gameObject.GetComponent<Transform>();
        _playerRigidbody = gameObject.GetComponent<Rigidbody>();
        _cameraRef = Camera.main.transform;

        // setup layer mask
        _ignoreRaycastLayerMask = LayerMask.GetMask("Ignore Raycast");
    }

    // Update is called once per frame
    void Update()
    {
        // check if grounded
        _isGrounded = CheckIfPlayerGrounded();

        // if grounded then perform ground movement
        if (_isGrounded)
        {
            CharacterMovementGrounded();
        }
    }

    private bool CheckIfPlayerGrounded()
    {
        // raycasts down by player height but a small offset to determine if hit ground
        if (Physics.Raycast(_playerTransform.position, (Vector3.down * (_playerTransform.localScale.y / 2)) * 1.001f,
                _ignoreRaycastLayerMask))
        {
            return true;
        }

        return false;
    }

    // handles static on floor movement (z is forward)
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

        // clamp max speed (kinda forced solution but easier than manualy handling velocity with curves)
        Vector3 rigidbodyVelocity = _playerRigidbody.velocity;
        if (rigidbodyVelocity.magnitude > maxGroundedMoveSpeed)
        {
            // clamp
            _playerRigidbody.velocity = rigidbodyVelocity.normalized * maxGroundedMoveSpeed;
        }
        
    }

    // sets up grapple and associated artwork
    private void StartGrapple()
    {
    }

    // cleans up grapple
    private void EndGrapple()
    {
    }
}