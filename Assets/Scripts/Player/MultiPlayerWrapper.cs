using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;

public class MultiPlayerWrapper : NetworkBehaviour
{
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
    }
}
