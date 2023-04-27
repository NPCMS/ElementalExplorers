using UnityEngine;
using Valve.VR;

public class PlayerController : MonoBehaviour
{
    // external parameters
    [Header("Ground Movement Settings")] [Tooltip("joystick input")] [SerializeField]
    SteamVR_Action_Vector2 inputAxis;

    [SerializeReference] private GrappleController leftHand;
    [SerializeReference] private GrappleController rightHand;
    [SerializeReference] private Transform vrCameraRef;
    [SerializeReference] private Collider _playerCollider; // player collider isn't on player body :(
    [SerializeField] private bool grapplingDefault = false;

    [SerializeField] [Tooltip("Speed at which player will accelerate on the ground usind WASD style movement")]
    private float groundedAcceleration;

    [SerializeField]
    [Tooltip(
        "Custom drag implementation, each frame drag is calculated as velocity^2 * drag, then velocity is updated to be velocity - (1 - deltaTime * drag)")]
    private float groundedXZDrag;

    [SerializeField] [Tooltip("How much you want to increase drag on water")]
    private float onWaterMultiplier;
    
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
    
    private Terrain _oldTerrain; // used to see if the terrain mask should be updated or not
    private Texture2D _oldMask; // cached because expensive operation
    private bool _onWater;

    // Start is called before the first frame update
    void Start()
    {
        // init get components here for performance
        _playerRigidbody = gameObject.GetComponent<Rigidbody>();
        _disableGroundCheck = false;
        leftHand.enabled = grapplingDefault;
        rightHand.enabled = grapplingDefault;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // check if grounded
        _isGrounded = CheckIfPlayerGrounded();

        // check if swinging / grappling
        bool applyGroundMechanics = ApplyGroundMechanics();
        
        if (_isGrounded)
            CheckIfOnWater();
        
        // if grounded then perform ground movement
        if (_isGrounded && applyGroundMechanics)
            CharacterMovementGrounded();
    }
    
    // Set Grappling Enabled
    public void SetGrappling(bool grappling)
    {
        leftHand.enabled = grappling;
        rightHand.enabled = grappling;
    }

    private bool ApplyGroundMechanics()
    {
        return !leftHand.isGrappling && !rightHand.isGrappling && !leftHand.isSwinging && !rightHand.isSwinging;
    }

    private void CheckIfOnWater()
    {
        Vector3 playerPosition = transform.position;
        RaycastHit hit;
        
        if (!Physics.Raycast(playerPosition, Vector3.down, out hit, playerPosition.y + 0.1f))
        {
            _onWater = false;
            return;
        }

        GameObject objectHit = hit.transform.gameObject;
        if (objectHit.TryGetComponent<Terrain>(out Terrain terrain)) // checks if on terrain or not
        {
            if (terrain != _oldTerrain)
            {
                _oldTerrain = terrain;
                _oldMask = (Texture2D)terrain.materialTemplate.GetTexture("_WaterMask");
            }

            int x = (int)(hit.textureCoord.x * _oldMask.width);
            int y = (int)(hit.textureCoord.y * _oldMask.height);

            var pixel = _oldMask.GetPixel(x, y);

            _onWater = pixel.r == 0f; // mask rgb values are 1 if on ground and 0 if on water
        }
        else
            _onWater = false;
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
        float dragForceMagnitude = Mathf.Pow(xzVelocity.magnitude, 2) * groundedXZDrag * (_onWater? onWaterMultiplier : 1f);
        xzVelocity = xzVelocity * (1 - Time.deltaTime * groundedXZDrag);
        // update velocity
        _playerRigidbody.velocity = new Vector3(xzVelocity.x, _playerRigidbody.velocity.y, xzVelocity.y);
    }

}