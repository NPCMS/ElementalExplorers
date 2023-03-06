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
                SpawnPlayerServerRPC(gameObject.GetComponent<NetworkObject>().OwnerClientId);
            }
        };
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
    private void SpawnPlayerServerRPC(ulong clientId)
    {
        GameObject spawnedPlayer = Instantiate(playerWrapper, new Vector3(107, 60, 680), new Quaternion());
        spawnedPlayer.name += clientId;
        SessionPlayerData sessionPlayerData = new SessionPlayerData(OwnerClientId, true, true, spawnedPlayer);
        SessionManager<SessionPlayerData>.Instance.SetPlayerData(OwnerClientId, sessionPlayerData);
        spawnedPlayer.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
    }
}
