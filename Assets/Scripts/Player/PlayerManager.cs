using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerManager : NetworkBehaviour
{
    [SerializeField] private GameObject playerWrapper;
    [SerializeField] private GameObject raceController;

    public void Start()
    {
        if (!IsOwner) return;

        SceneManager.activeSceneChanged += (_, current) =>
        {
            if (IsHost) // spawn race controller
            {
                GameObject rc = Instantiate(raceController);
                rc.GetComponent<NetworkObject>().Spawn();
            }
            
            if (current.name == "Precompute")
            {
                SpawnPlayerServerRPC(gameObject.GetComponent<NetworkObject>().OwnerClientId);
            }
        };
    }

    [ServerRpc]
    private void SpawnPlayerServerRPC(ulong clientId)
    {
        GameObject spawnedPlayer = Instantiate(playerWrapper, new Vector3(107, 60, 680), new Quaternion());
        spawnedPlayer.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
    }
}
