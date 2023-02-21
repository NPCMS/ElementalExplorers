using System;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using UnityEngine;

public class MultiPlayerWrapper : NetworkBehaviour
{
    private HandGrappleAndSwinging[] grapples;
    private RaceController raceController;
    
    // as the player is in multiplayer it can either be a controlled by the user or not
    private void Start()
    {
        if (IsOwner) // if the player object is to be controlled by the user then enable all controls 
        {
            var init = gameObject.GetComponentInChildren<InitPlayer>();
            init.StartPlayer();
            init.StartRace();
        }

        // enable multiplayer transforms - this needs to be done for all players so they synchronise correctly
        foreach (var c in gameObject.GetComponentsInChildren<ClientNetworkTransform>())
        {
            c.enabled = true;
        }
        
        // Get other scripts
        var rcGameObject = GameObject.FindGameObjectWithTag("RaceController");
        raceController = rcGameObject.GetComponent<RaceController>();
        grapples = gameObject.GetComponentsInChildren<HandGrappleAndSwinging>();

        // Add grapple begin and end callbacks
        foreach (HandGrappleAndSwinging grapple in grapples)
        {
            grapple.AddBeginCallback((grapplePoint, hand) =>
            {
                raceController.BeginGrappleServerRpc(grapplePoint, hand);
            });
            grapple.AddEndCallback((hand) =>
            {
                raceController.EndGrappleServerRpc(hand);
            });
        }
        
        raceController.grappleDataList.OnListChanged += UpdateGrappleDrawer;
    }
    
    private void UpdateGrappleDrawer(NetworkListEvent<RaceController.GrappleData> changedGrapple)
    {
        // Sorry I had to do this casting - Alex
        ulong clientId = (ulong)Math.Floor((double)(changedGrapple.Index / 2));
        if (clientId != NetworkManager.LocalClientId)
        {
            RaceController.PlayerObjects value;
            raceController.playerBodies.TryGetValue(clientId, out value);
            GameObject hand = value.hands[changedGrapple.Index - (int)clientId];
            GrappleDrawer drawer = hand.GetComponent<GrappleDrawer>();
            if (changedGrapple.Value.connected)
            {
                Vector3 endPoint = new Vector3(changedGrapple.Value.x, changedGrapple.Value.y, changedGrapple.Value.z);
                drawer.Enable(hand.transform.position, endPoint);
            }
            else
            {
                drawer.Disable();
            }
        }
    }
}
