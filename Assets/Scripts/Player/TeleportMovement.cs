using UnityEngine;
using UnityEngine.SceneManagement;


public class TeleportMovement : MonoBehaviour
{
    [SerializeField] private SteamInputCore.Hand hand;
    [SerializeField] private SteamInputCore.Button teleportButton;
    [SerializeField] private float maxTeleportDistance = 5;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private MeshRenderer sphereMaterial;


    private SteamInputCore.SteamInput steamInput;
    
    private bool teleportValid;
    private Vector3 teleportLocation;

    private void Start()
    {
        steamInput = SteamInputCore.GetInput();
    }

    void Update()
    {
        // When holding button show if teleport is allowed
        bool buttonDown = steamInput.GetInput(hand, teleportButton);
        if (buttonDown)
        {
            StartTeleport();
        }
        
        DisplayColour(buttonDown);
        
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
            return;
        }
        
        if (!ValidateTeleport(hit)) return;

        teleportLocation = hit.point;
        teleportValid = true;
    }

    private void DisplayColour(bool buttonDown)
    {
        if (buttonDown && teleportValid)
        {
            sphereMaterial.material.color = Color.cyan;
            lineRenderer.startColor = Color.cyan;
            lineRenderer.endColor = Color.cyan;
        }
        else
        {
            sphereMaterial.material.color = Color.red;
            lineRenderer.startColor = Color.red;
            lineRenderer.endColor = Color.red;
        }
    }

    private void ExecuteTeleport()
    {
        if (teleportValid)
        {
            // Move player to the location of the teleport
            Transform player = gameObject.transform.parent.parent;
            Vector3 feetPosition = player.position - Vector3.up * player.Find("Body").transform.lossyScale.y;
            Vector3 translation = teleportLocation - feetPosition;
            player.position += translation;
                
            // Add haptics
            steamInput.Vibrate(hand, 0.1f, 120, 0.6f);
        }
    }

    private bool ValidateTeleport(RaycastHit hit)
    {
        // If in controlled teleportation scene and
        // if object is not in teleport layer don't grapple to it
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene is "SpaceshipScene" or "TutorialZone" && hit.transform.gameObject.layer != 10) return false;

        // If object is in UI layer don't grapple to it
        if (hit.transform.gameObject.layer == 5) return false;
        
        // Only allow teleports to flat surfaces
        const double flatnessTol = 0.95;
        return !(Vector3.Dot(hit.normal, Vector3.up) < flatnessTol);
    }
}
