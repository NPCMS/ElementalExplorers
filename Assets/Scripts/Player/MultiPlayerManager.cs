using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SpatialTracking;
using Valve.VR;

public class MultiPlayerManager : NetworkBehaviour
{
    // Start is called when a player is spawned in
    private void Start()
    {
        if (IsOwner) // if the player object is to be controlled by the player then enable all controls 
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
            EnableChildren(gameObject);
        }

        // enable multiplayer transforms
        foreach (var c in gameObject.GetComponentsInChildren<ClientNetworkTransform>())
        {
            c.enabled = true;
        }
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
