using UnityEngine;
using UnityEngine.Serialization;

public class TeleportMovement : MonoBehaviour
{
    [SerializeField] private SteamInputCore.Hand hand;
    [SerializeField] private SteamInputCore.Button teleportButton;
    [SerializeField] private float maxTeleportDistance = 5;

    private SteamInputCore.SteamInput steamInput;
    
    private bool teleportValid;
    private Vector3 teleportLocation;

    void Update()
    {
        // When holding button show if teleport is allowed
        if (steamInput.GetInput(hand, teleportButton))
        {
            StartTeleport();
        }
        
        // When button released execute the teleport
        if (steamInput.GetInputUp(hand, teleportButton))
        {
            ExecuteTeleport();
        }
    }
    
    private void LateUpdate()
    {
        steamInput.GetInputUp(hand, teleportButton);
    }

    private void StartTeleport()
    {
        teleportValid = false;
        // Cast a ray 
        RaycastHit hit;
        if (!Physics.Raycast(transform.position, transform.forward, out hit, maxTeleportDistance))
        {
            if (!Physics.SphereCast(transform.position, 0.5f, transform.forward, out hit, maxTeleportDistance))
            {
                return;
            }
        }
        
        if (!ValidateTeleport(hit)) return;

        teleportLocation = hit.point;
        teleportValid = true;
    }

    private void ExecuteTeleport()
    {
        if (teleportValid)
        {
            // Move player to the location of the teleport
            
            // Add haptics
            steamInput.Vibrate(hand, 0.1f, 120, 0.6f);
        }
    }

    private bool ValidateTeleport(RaycastHit hit)
    {
        // If object is in UI layer don't grapple to it
        if (hit.transform.gameObject.layer == 5) return false;

        // Only allow teleports to flat surfaces
        double flatnessTol = 0.95;
        if (Vector3.Dot(hit.normal, Vector3.up) < flatnessTol) return false;

        return true;
    }
}
