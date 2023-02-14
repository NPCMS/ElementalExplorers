using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SpatialTracking;
using Valve.VR;

public class MultiPlayerManager : NetworkBehaviour
{
    // Start is called when a player is spawned in
    void Start()
    {
        if (!IsOwner)
        {
            foreach (var c in gameObject.GetComponentsInChildren<Camera>())
            {
                c.enabled = false;
            }
            foreach (var c in gameObject.GetComponentsInChildren<SteamVR_Behaviour_Pose>())
            {
                c.enabled = false;
            }
            foreach (var c in gameObject.GetComponentsInChildren<TrackedPoseDriver>())
            {
                c.enabled = false;
            }
            foreach (var c in gameObject.GetComponentsInChildren<PlayerVRController>())
            {
                c.enabled = false;
            }
            foreach (var c in gameObject.GetComponentsInChildren<Grapple>())
            {
                c.enabled = false;
            }
            foreach (var c in gameObject.GetComponentsInChildren<UIInteractVR>())
            {
                c.enabled = false;
            }
        }
        // enable multiplayer transforms
        foreach (var c in gameObject.GetComponentsInChildren<ClientNetworkTransform>())
        {
            c.enabled = true;
        }
    }
}
