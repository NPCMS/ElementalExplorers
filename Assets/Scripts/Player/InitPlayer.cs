using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SpatialTracking;
using Valve.VR;

public class InitPlayer : MonoBehaviour
{

    [SerializeField] private GameObject hud;

    public void Start()
    {
        // registers this players body with the race controller
        var rcGameObject = GameObject.FindGameObjectWithTag("RaceController");
        var network = gameObject.GetComponentInParent<NetworkObject>();
        if (rcGameObject == null || network == null) return;
        var rc = rcGameObject.GetComponent<RaceController>();
        rc.playerBodies.Add(network.OwnerClientId, new RaceController.PlayerObjects(gameObject));
    }

    // enables all controls / hud for the player
    public void StartPlayer()
    {
        EnableChildren(gameObject); // child objects must be enabled first so get components applies to their components as well
        hud.SetActive(false);
        gameObject.GetComponentInChildren<Camera>().enabled = true;
        gameObject.GetComponentInChildren<PlayerVRController>().enabled = true;
        gameObject.GetComponentInChildren<TrackedPoseDriver>().enabled = true;
        gameObject.GetComponentInChildren<Grapple>().enabled = true;
        gameObject.GetComponentInChildren<UIInteractVR>().enabled = true;
        foreach (var c in gameObject.GetComponentsInChildren<LaserPointer>())
        {
            c.enabled = true;
        }

        foreach (var c in gameObject.GetComponentsInChildren<SteamVR_Behaviour_Pose>())
        {
            c.enabled = true;
        }
    }

    public void StartRace()
    {
        var playerRaceController = gameObject.GetComponentInChildren<PlayerRaceController>();
        playerRaceController.enabled = true;
        playerRaceController.raceStarted = true;
        Invoke(nameof(ConnectPlayerTracker), 2);
        hud.SetActive(true);
    }

    // function to set up player tracking with hud. This is scuffed but required as players load in a different times.
    // ideally the race will be started when both players are loaded and this can be tidied
    private void ConnectPlayerTracker()
    {
        var rc = GameObject.FindGameObjectWithTag("RaceController").GetComponent<RaceController>();
        ulong userID = GetComponentInParent<NetworkObject>().OwnerClientId;
        Debug.Log(userID);
        foreach (var k in rc.playerBodies.Keys)
        {
            Debug.Log(k);
        }
        Debug.Log("Done!");
        var otherUid = rc.playerBodies.Keys.First(uid => uid != userID);
        hud.GetComponent<HUDController>().TrackPlayer(rc.playerBodies[otherUid].body.transform);
    }
    
    // recursively finds the first disabled object the the tree and enables it (disabled objects under another disabled object won't be enabled)
    private static void EnableChildren(GameObject gameObject)
    {
        for (int c = 0; c < gameObject.transform.childCount; c++)
        {
            GameObject child = gameObject.transform.GetChild(c).gameObject;
            if (!child.activeSelf)
            {
                child.SetActive(true);
            }
            else
            {
                EnableChildren(child);
            }
        }
    }
}
