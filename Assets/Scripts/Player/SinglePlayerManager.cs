using UnityEngine;
using UnityEngine.SpatialTracking;
using Valve.VR;

public class SinglePlayerManager : MonoBehaviour
{
    // Start is called when a player is spawned in
    private void Start()
    {
        foreach (var c in gameObject.GetComponentsInChildren<Camera>())
        {
            c.enabled = true;
        }
        foreach (var c in gameObject.GetComponentsInChildren<SteamVR_Behaviour_Pose>())
        {
            c.enabled = true;
        }
        foreach (var c in gameObject.GetComponentsInChildren<TrackedPoseDriver>())
        {
            c.enabled = true;
        }
        foreach (var c in gameObject.GetComponentsInChildren<PlayerVRController>())
        {
            c.enabled = true;
        }
        foreach (var c in gameObject.GetComponentsInChildren<Grapple>())
        {
            c.enabled = true;
        }
        foreach (var c in gameObject.GetComponentsInChildren<UIInteractVR>())
        {
            c.enabled = true;
        }
        foreach (var c in gameObject.GetComponentsInChildren<LaserPointer>())
        {
            c.enabled = true;
        }
        foreach (var c in gameObject.GetComponentsInChildren<PlayerRaceController>())
        {
            c.enabled = true;
            c.raceStarted = true;
        }
        EnableChildren(gameObject);
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
