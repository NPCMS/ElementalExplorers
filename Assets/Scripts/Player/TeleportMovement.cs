using UnityEngine;
using UnityEngine.Serialization;

public class TeleportMovement : MonoBehaviour
{
    [SerializeField] private SteamInputCore.Hand hand;
    [SerializeField] private SteamInputCore.Button teleportButton;
    [SerializeField] private float maxTeleportLength = 5;

    private bool _isTeleporting;
    private SteamInputCore.SteamInput steamInput;
    
    private Vector3 _teleportHitLocation;

    void Start()
    {
        
    }

    void Update()
    {
        Teleport();
    }
    
    private void LateUpdate()
    {
        steamInput.GetInputUp(hand, teleportButton);
    }

    private void Teleport()
    {
        if (!_isTeleporting && steamInput.GetInput(hand, teleportButton))
        {
            StartTeleport();
        }
        
        if (_isTeleporting && steamInput.GetInputUp(hand, teleportButton))
        {
            ExecuteTeleport();
        }
    }

    private void StartTeleport()
    {
        RaycastHit hit;
        if (!Physics.Raycast(transform.position, transform.forward, out hit, maxTeleportLength))
        {
            if (!Physics.SphereCast(transform.position, 0.5f, transform.forward, out hit, maxTeleportLength))
                return;
        }
        
        if (hit.transform.gameObject.layer == 5) return; // if object is in UI layer don't grapple to it
        
        _teleportHitLocation = hit.point;
        _isTeleporting = true;
        // add haptics
        steamInput.Vibrate(hand, 0.1f, 120, 0.6f);
    }
    
    private bool ValidTeleportLocation()
    {
        return true;
    }
    
    private void ExecuteTeleport()
    {
        if (!_isTeleporting)
            return;

        _isTeleporting = false;
    }
}
