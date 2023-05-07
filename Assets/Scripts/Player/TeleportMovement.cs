using UnityEngine;
using UnityEngine.SceneManagement;


public class TeleportMovement : MonoBehaviour
{
    [SerializeField] private SteamInputCore.Hand hand;
    [SerializeField] private SteamInputCore.Button teleportButton;
    [SerializeField] private GameObject pointer;
    [SerializeField] private GameObject marker;
    [SerializeField] private LineRenderer parabolaRenderer;

    private SteamInputCore.SteamInput steamInput;
    private bool teleportValid;
    private int parabolaPoints;
    private Vector3[] linePositions = new Vector3[0];
    
    [SerializeField] private int maxParabolaPoints = 100;
    [SerializeField] private float velocity = 2f;
    [SerializeField] private float maxSegmentDistance = 0.2f;
    [SerializeField] private float lerpAmount;
    [SerializeField] private float teleportCooldown = 0.5f;

    [SerializeField] private AudioSource teleportSfx;
    
    private bool hasTeleportedRecently;
    
    private void Start()
    {
        steamInput = SteamInputCore.GetInput();
    }

    void Update()
    {
        // When button released execute the teleport
        if (steamInput.GetInputUp(hand, teleportButton) && teleportValid)
        {
            ExecuteTeleport(linePositions[parabolaPoints]);
        }
        
        // When holding button show if teleport is allowed
        bool buttonDown = steamInput.GetInput(hand, teleportButton);
        if (buttonDown)
        {
            linePositions = GravCast(transform.position, Vector3.Lerp(transform.forward,Vector3.up, lerpAmount));
        }
        else
        {
            parabolaPoints = 0;
        }
        DisplayTeleportParabola(buttonDown, linePositions);
    }
    private void LateUpdate()
    {
        steamInput.GetInputUp(hand, teleportButton);
    }

    private void DisplayTeleportParabola(bool buttonDown, Vector3[] linePositions)
    {
        parabolaRenderer.positionCount = parabolaPoints;
        if (buttonDown)
        {
            parabolaRenderer.SetPositions(linePositions);
            pointer.SetActive(false);
            marker.transform.position = linePositions[parabolaPoints];
            // marker.transform.localRotation = Quaternion.identity;
            marker.SetActive(teleportValid);
        }
        else
        {
            pointer.SetActive(true);
            marker.SetActive(false);
        }
    }

    private void ExecuteTeleport(Vector3 teleportLocation)
    {
        // Move player to the location of the teleport
        Transform player = gameObject.transform.parent.parent;
        Vector3 feetPosition = player.position - Vector3.up * player.Find("Body").transform.lossyScale.y;
        Vector3 translation = teleportLocation - feetPosition;
        player.position += translation;
        // Add haptics
        steamInput.Vibrate(hand, 0.1f, 120, 0.6f);
        teleportSfx.Play();
        hasTeleportedRecently = true;
        Invoke(nameof(CanTeleportAgain), teleportCooldown);
    }

    private void CanTeleportAgain()
    {
        hasTeleportedRecently = false;
    }

    // This function is adapted from: https://answers.unity.com/questions/1464706/is-there-way-to-curve-raycast.html
    Vector3[] GravCast(Vector3 startPos, Vector3 direction)
    {
        RaycastHit hit;
        Vector3[] vectors = new Vector3[maxParabolaPoints + 1];
        vectors[0] = startPos;
        Ray ray = new Ray(startPos, direction);
        for (int i = 1; i < maxParabolaPoints; i++)
        {
            if(Physics.Raycast(ray,out hit, maxSegmentDistance))
            {
                teleportValid = ValidateTeleport(hit);
                parabolaPoints = i;
                vectors[i] = hit.point;
                return vectors;
            }
            // Debug.DrawRay(ray.origin, ray.direction, Color.blue);
            ray = new Ray(ray.origin + ray.direction * maxSegmentDistance, ray.direction + Physics.gravity / maxParabolaPoints / velocity);
            vectors[i] = ray.origin;
        }

        parabolaPoints = maxParabolaPoints;
        teleportValid = false;
        return vectors;
    }
    
    private bool ValidateTeleport(RaycastHit hit)
    {
        if (hasTeleportedRecently) return false;
        // If in controlled teleportation scene and
        // if object is not in teleport layer don't move to it
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene is "SpaceshipScene" or "TutorialZone" && hit.transform.gameObject.layer != 10) return false;

        // If object is in UI layer don't grapple to it
        if (hit.transform.gameObject.layer == 5) return false;
        
        // If there is something directly above the hit point then teleport is invalid
        if (Physics.SphereCast(new Ray(hit.point, Vector3.up), 0.3f, 2f)) return false;
        
        // Only allow teleports to flat surfaces
        const double flatnessTol = 0.95;
        return !(Vector3.Dot(hit.normal, Vector3.up) < flatnessTol);
    }
}