using Netcode.SessionManagement;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerManager : NetworkBehaviour
{
    [SerializeField] private GameObject playerWrapper;

    public void Start()
    {
        if (!IsOwner) return;

        SceneManager.activeSceneChanged += (_, current) =>
        {
            if (current.name == "Precompute")
            {
                SpawnPlayerServerRPC(gameObject.GetComponent<NetworkObject>().OwnerClientId, new Vector3(0,0,0), new Quaternion());
            }
        };
        
        
        
        // Spawn the Multiplayer Wrapper
        GameObject singlePlayer = GameObject.FindGameObjectWithTag("Player");
        Vector3 player2Location = new Vector3(-4, 0.4f, -24);
        Quaternion player2Rotation = new Quaternion(0, 70, 0, 0);
        if (IsHost)
        {
            SpawnPlayerServerRPC(gameObject.GetComponent<NetworkObject>().OwnerClientId, singlePlayer.transform.position, singlePlayer.transform.rotation);
        }
        else
        {
            SpawnPlayerServerRPC(gameObject.GetComponent<NetworkObject>().OwnerClientId, player2Location, player2Rotation);
        }
        
        // Get a reference to the local SingleplayerWrapper and destroy it
        DestroyImmediate(singlePlayer);
    }
    
    public override void OnNetworkSpawn()
    {
        gameObject.name = "PlayerManager" + OwnerClientId;

        // Note that this is done here on OnNetworkSpawn in case this NetworkBehaviour's properties are accessed
        // when this element is added to the runtime collection. If this was done in OnEnable() there is a chance
        // that OwnerClientID could be its default value (0).
        if (IsServer)
        {
            var sessionPlayerData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(OwnerClientId);
            if (sessionPlayerData.HasValue)
            {
                var playerData = sessionPlayerData.Value;
                if (!playerData.HasCharacterSpawned)
                {
                    SessionManager<SessionPlayerData>.Instance.SetPlayerData(OwnerClientId, playerData);
                }
            }
        }
    }

    [ServerRpc]
    private void SpawnPlayerServerRPC(ulong clientId, Vector3 position, Quaternion rotation)
    {
        GameObject spawnedPlayer = Instantiate(playerWrapper, position, rotation);
        spawnedPlayer.name += clientId;
        SessionPlayerData sessionPlayerData = new SessionPlayerData(OwnerClientId, true, true, spawnedPlayer);
        SessionManager<SessionPlayerData>.Instance.SetPlayerData(OwnerClientId, sessionPlayerData);
        spawnedPlayer.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
    }
}
