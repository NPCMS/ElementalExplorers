using UnityEngine;
using UnityEngine.Serialization;

public class HUDController : MonoBehaviour
{
    [SerializeField] private Transform playerPos;
    
    public Transform otherPlayerPos = new RectTransform();
    private bool trackingPlayer;
    public Transform checkpointPos = new RectTransform();
    private bool trackingCheckpoint;
    [SerializeField] private Transform playerArrow;
    [SerializeField] private Transform checkpointArrow;
    
    private Transform cam;

    public void Start()
    {
        cam = GetComponentInParent<Camera>().transform;
    }

    public void Update()
    {
        Vector3 position = playerPos.position;
        Vector3 forward = cam.forward;
        if (trackingPlayer)
        {
            playerArrow.localRotation = GetArrowDirection(position, otherPlayerPos.position, forward);
        }
        if (trackingCheckpoint)
        {
            checkpointArrow.localRotation = GetArrowDirection(position, checkpointPos.position, forward);
        }
    }

    public void TrackPlayer(Transform player)
    {
        otherPlayerPos = player;
        trackingPlayer = true;
    }

    public void TrackCheckpoint(Transform checkpoint)
    {
        checkpointPos = checkpoint;
        trackingCheckpoint = true;
    }

    public void UnTrackPlayer()
    {
        otherPlayerPos = new RectTransform();
        trackingPlayer = false;
    }

    public void UnTrackCheckpoint()
    {
        checkpointPos = new RectTransform();
        trackingCheckpoint = false;
    }
    
    private static Quaternion GetArrowDirection(Vector3 from, Vector3 to, Vector3 camForward)
    {
        Vector2 targetDirection = new Vector2(to.x - from.x, to.z - from.z).normalized;
        Vector2 lookDir = new Vector2(camForward.x, camForward.z).normalized;
        float angle = Mathf.Atan2(targetDirection.x * lookDir.y - targetDirection.y * lookDir.x, targetDirection.x * lookDir.x + targetDirection.y * lookDir.y);
        return Quaternion.Euler(0, 0, -angle * 180.0f / Mathf.PI);
    }
}
