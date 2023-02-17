using UnityEngine;
using UnityEngine.SpatialTracking;
using Valve.VR;

public class InitPlayer : MonoBehaviour
{
    // enables all controls / hud for the player
    public void StartPlayer()
    {
        EnableChildren(gameObject); // child objects must be enabled first so get components applies to their components as well
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
        gameObject.GetComponentInChildren<PlayerRaceController>().enabled = true;
        gameObject.GetComponentInChildren<PlayerRaceController>().raceStarted = true;
    }
    
    private static void EnableChildren(GameObject gameObject)
    {
        for (int c = 0; c < gameObject.transform.childCount; c++)
        {
            GameObject child = gameObject.transform.GetChild(c).gameObject;
            child.SetActive(true);
            EnableChildren(child);
        }
    }
}
