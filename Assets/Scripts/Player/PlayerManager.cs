using System;
using System.Collections;
using System.Runtime.CompilerServices;
using Netcode.SessionManagement;
using Unity.Netcode;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.SceneManagement;
using NetworkEvent = Unity.Netcode.NetworkEvent;

public class PlayerManager : NetworkBehaviour
{
    [SerializeField] private GameObject playerWrapper;
    private GameObject spawnedPlayer;

    public void Start()
    {
        if (!IsOwner) return;

        SceneManager.activeSceneChanged += (_, current) =>
        {
            if (current.name == "OSMData")
            {
                Vector3 position = new Vector3();
                if (IsHost)
                {
                    position = GameObject.FindWithTag("Player1Spawn").transform.position;
                }
                else
                {
                    position = GameObject.FindWithTag("Player2Spawn").transform.position;
                }
                Debug.Log("Calling server to spawn wrapper");
                SpawnPlayerServerRPC(gameObject.GetComponent<NetworkObject>().OwnerClientId, position, new Quaternion());
            }

            StartCoroutine(Alive());
        };

        // Spawn the Multiplayer Wrapper
        GameObject singlePlayer = GameObject.FindGameObjectWithTag("Player");
        Vector3 player2Location = new Vector3(-4, 0.4f, -24);
        Quaternion player2Rotation = new Quaternion(0, 70, 0, 0);
        if (IsHost)
        {
            SpawnPlayerServerRPC(gameObject.GetComponent<NetworkObject>().OwnerClientId, singlePlayer.GetComponentInChildren<Rigidbody>().transform.position, singlePlayer.transform.rotation);
        }
        else
        {
            SpawnPlayerServerRPC(gameObject.GetComponent<NetworkObject>().OwnerClientId, player2Location, player2Rotation);
        }
        
        // Get a reference to the local SingleplayerWrapper and destroy it
        DestroyImmediate(singlePlayer);
    }

    private IEnumerator Alive()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(5);
            KeepAliveServerRpc();
        }
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
        if (spawnedPlayer != null)
        {
            spawnedPlayer.GetComponent<NetworkObject>().Despawn();
        }
        spawnedPlayer = Instantiate(playerWrapper, position, rotation);
        spawnedPlayer.name += clientId;
        SessionPlayerData sessionPlayerData = new SessionPlayerData(OwnerClientId, true, true, spawnedPlayer);
        SessionManager<SessionPlayerData>.Instance.SetPlayerData(OwnerClientId, sessionPlayerData);
        spawnedPlayer.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
    }

    [ServerRpc]
    private void KeepAliveServerRpc()
    {
        bool aBool = true;
    }
}
