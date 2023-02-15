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
            if (current.name == "SampleScene")
            {
                SpawnPlayerServerRPC(gameObject.GetComponent<NetworkObject>().OwnerClientId);
            }

            if (IsHost) // spawn race controller
            {
                GameObject rc = Instantiate(raceController);
                rc.GetComponent<NetworkObject>().Spawn();
            }
        };
    }

    [ServerRpc]
    private void SpawnPlayerServerRPC(ulong clientId)
    {
        GameObject spawnedPlayer = Instantiate(playerWrapper);
        spawnedPlayer.transform.position = Vector3.up * 3;
        spawnedPlayer.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
    }
}
