using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;


public class TeleportMovement : MonoBehaviour
{
    [SerializeField] private SteamInputCore.Hand hand;
    [SerializeField] private SteamInputCore.Button teleportButton;
    [SerializeField] private float maxTeleportDistance = 5;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private MeshRenderer sphereMaterial;
    [SerializeField] private GameObject marker;

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
            // TODO: make parabola
            //lineRenderer.SetPositions(Parabola());
            marker.SetActive(true);
        }
        else
        {
            sphereMaterial.material.color = Color.red;
            marker.SetActive(false);
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
    
    List<Vector3> Parabola(Vector3 start, Vector3 end, float height)
    {
        List<Vector3> points = new List<Vector3>();
        int nPoints = 20;
        for (int i = 0; i < nPoints; nPoints++)
        {
            float t = i / nPoints;
            float parabolicT = t * 2 - 1;
            if ( Mathf.Abs( start.y - end.y ) < 0.1f ) {
                //start and end are roughly level, pretend they are - simpler solution with less steps
                Vector3 travelDirection = end - start;
                Vector3 result = start + t * travelDirection;
                result.y += ( -parabolicT * parabolicT + 1 ) * height;
                points.Add(result);
            } else {
                //start and end are not level, gets more complicated
                Vector3 travelDirection = end - start;
                Vector3 levelDirecteion = end - new Vector3( start.x, end.y, start.z );
                Vector3 right = Vector3.Cross( travelDirection, levelDirecteion );
                Vector3 up = Vector3.Cross( right, travelDirection );
                if ( end.y > start.y ) up = -up;
                Vector3 result = start + t * travelDirection;
                result += ( ( -parabolicT * parabolicT + 1 ) * height ) * up.normalized;
                points.Add(result);
            }
        }
        return points;
    }
}


