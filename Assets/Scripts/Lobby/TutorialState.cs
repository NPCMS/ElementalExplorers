using System.Collections.Generic;
using Netcode.ConnectionManagement;
using Netcode.ConnectionManagement.ConnectionState;
using Netcode.SceneManagement;
using Unity.Netcode;
using UnityEngine;

public class TutorialState : NetworkBehaviour
{
    List <GameObject> currentCollisions = new();
    private ConnectionManager _connectionManager;
    private HashSet<ulong> finishedPipelinePlayers = new();

    private void Awake()
    {
        _connectionManager = FindObjectOfType<ConnectionManager>();
    }
    
    private void Update()
    {
        // FindObjectsOfType<SpeakerController>().ForEach(x => x.PlayAudio("Tutorial into"));
    }
    

    void OnTriggerEnter (Collider col) {
 
        // Add the GameObject collided with to the list.
        currentCollisions.Add(col.gameObject);
        if (_connectionManager.m_CurrentState is OfflineState || IsHost && GetPlayersInTeleporter().Count == 2)
        {
            if (IsHost)
            {
                TeleportPlayerClientRpc();
            }
        }
    }
 
    void OnTriggerExit (Collider col) {
 
        // Remove the GameObject collided with from the list.
        currentCollisions.Remove(col.gameObject);
    }
    
    public List<GameObject> GetPlayersInTeleporter()
    {
        return currentCollisions.FindAll(x => x.CompareTag("Player"));
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayerFinishedPipelineServerRpc(ServerRpcParams serverRpcParams = default)
    {
        finishedPipelinePlayers.Add(serverRpcParams.Receive.SenderClientId);
        if (finishedPipelinePlayers.Count == 2)
        {
            EnableTeleporterClientRpc();
        }
    }

    [ClientRpc]
    public void EnableTeleporterClientRpc()
    {
        GetComponent<BoxCollider>().enabled = true;
        GetComponent<MeshRenderer>().enabled = true;
    }

    [ClientRpc]
    public void TeleportPlayerClientRpc()
    {
        if (IsHost)
        {
            MultiPlayerWrapper.localPlayer.ResetPlayerPos();
            MultiPlayerWrapper.localPlayer.transform.position =
                GameObject.FindGameObjectWithTag("Player1Spawn").transform.position;
            RaceController.Instance.StartRace();
        }
        else
        {
            MultiPlayerWrapper.localPlayer.ResetPlayerPos();
            MultiPlayerWrapper.localPlayer.transform.position =
                GameObject.FindGameObjectWithTag("Player2Spawn").transform.position;
        }
        SceneLoaderWrapper.Instance.UnloadAdditiveScenes();
    }
}
