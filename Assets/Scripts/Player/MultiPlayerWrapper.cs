using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;

public class MultiPlayerWrapper : NetworkBehaviour
{
    // Start is called when a player is spawned in
    private void Start()
    {
        if (IsOwner) // if the player object is to be controlled by the player then enable all controls 
        {
            var init = gameObject.GetComponentInChildren<InitPlayer>();
            init.StartPlayer();
            init.StartRace();
        }

        // enable multiplayer transforms
        foreach (var c in gameObject.GetComponentsInChildren<ClientNetworkTransform>())
        {
            c.enabled = true;
        }
    }
}
