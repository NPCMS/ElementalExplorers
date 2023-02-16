using UnityEngine;
using UnityEngine.Serialization;

public class HUDController : MonoBehaviour
{
    [SerializeField] private Transform playerPos;
    
    [SerializeField] private Vector3 otherPlayerPos;
    [SerializeField] private Vector3 checkpointPos;
    [SerializeField] private Transform playerArrow;
    [SerializeField] private Transform checkpointArrow;
    
    private Transform cam;

    public void Start()
    {
        cam = GetComponentInParent<Camera>().transform;
    }

    public void Update()
    {
        var position = playerPos.position;
        Vector2 targetDirection = new Vector2(otherPlayerPos.x - position.x, otherPlayerPos.z - position.z).normalized;
        var forward = cam.forward;
        Vector2 lookDir = new Vector2(forward.x, forward.z).normalized;
        float angle = Mathf.Atan2(targetDirection.x * lookDir.y - targetDirection.y * lookDir.x, targetDirection.x * lookDir.x + targetDirection.y * lookDir.y);
        playerArrow.localRotation = Quaternion.Euler(0, 0, -angle * 180.0f / Mathf.PI);
        
        targetDirection = new Vector2(checkpointPos.x - position.x, checkpointPos.z - position.z).normalized;
        lookDir = new Vector2(forward.x, forward.z).normalized;
        angle = Mathf.Atan2(targetDirection.x * lookDir.y - targetDirection.y * lookDir.x, targetDirection.x * lookDir.x + targetDirection.y * lookDir.y);
        checkpointArrow.localRotation = Quaternion.Euler(0, 0, -angle * 180.0f / Mathf.PI);
    }
}
