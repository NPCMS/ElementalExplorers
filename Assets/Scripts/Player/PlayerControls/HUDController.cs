using TMPro;
using UnityEngine;

public class HUDController : MonoBehaviour
{
    [SerializeField] private Transform playerPos;
    [SerializeField] private Transform playerArrow;
    [SerializeField] private Transform checkpointArrow;
    [SerializeField] private TextMeshPro timer;
    
    public Transform otherPlayerPos = new RectTransform();
    private bool trackingPlayer;
    public Transform checkpointPos = new RectTransform();
    private bool trackingCheckpoint;
    private Transform cam;
    private bool racing;

    public void Start()
    {
        racing = false;
    }

    public void Update()
    {
        if (racing)
        {
            Vector3 position = playerPos.position;
            Vector3 forward = cam.forward;
            if (trackingPlayer)
            {
                try
                {
                    playerArrow.localRotation = GetArrowDirection(position, otherPlayerPos.position, forward);
                }
                catch (MissingReferenceException)
                {
                    UnTrackPlayer();
                }
            }

            if (trackingCheckpoint)
            {
                checkpointArrow.localRotation = GetArrowDirection(position, checkpointPos.position, forward);
            }
        }
    }

    public void StartRacing()
    {
        cam = GetComponentInParent<Camera>().transform;
        GameObject rcGameObject = GameObject.FindWithTag("RaceController");
        if (rcGameObject == null) return;
        RaceController rc = rcGameObject.GetComponent<RaceController>();
        rc.hudController = this;
        rc.TrackCheckpoint();
    }

    public void TrackPlayer(Transform player)
    {
        otherPlayerPos = player;
        playerArrow.gameObject.SetActive(true);
        trackingPlayer = true;
    }

    public void TrackCheckpoint(Transform checkpoint)
    {
        checkpointPos = checkpoint;
        checkpointArrow.gameObject.SetActive(true); 
        trackingCheckpoint = true;
    }

    public void UnTrackPlayer()
    {
        otherPlayerPos = new RectTransform();
        playerArrow.gameObject.SetActive(false);
        trackingPlayer = false;
    }

    public void UnTrackCheckpoint()
    {
        checkpointPos = new RectTransform();
        checkpointArrow.gameObject.SetActive(false);
        trackingCheckpoint = false;
    }

    public void UpdateTime(float time)
    {
        if (time > 60)
        {
            timer.text = Mathf.Floor(time / 60) + "m " + (time % 60).ToString("0.00") + "s";
        }
        else
        {
            timer.text = time.ToString("0.00") + "s";
        }
    }
    
    private static Quaternion GetArrowDirection(Vector3 from, Vector3 to, Vector3 camForward)
    {
        Vector2 targetDirection = new Vector2(to.x - from.x, to.z - from.z).normalized;
        Vector2 lookDir = new Vector2(camForward.x, camForward.z).normalized;
        float angle = Mathf.Atan2(targetDirection.x * lookDir.y - targetDirection.y * lookDir.x, targetDirection.x * lookDir.x + targetDirection.y * lookDir.y);
        return Quaternion.Euler(0, 0, -angle * 180.0f / Mathf.PI);
    }
}
