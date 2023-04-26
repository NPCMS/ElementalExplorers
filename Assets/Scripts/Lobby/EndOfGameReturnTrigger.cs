using System;
using System.Collections.Generic;
using Netcode.ConnectionManagement;
using Netcode.ConnectionManagement.ConnectionState;
using Netcode.SceneManagement;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Netcode.SessionManagement;

public class EndOfGameReturnTrigger : NetworkBehaviour
{
    
    private List<GameObject> currentCollisions = new();
    private ConnectionManager _connectionManager;

    [SerializeField] private GameObject newPlayer;

    private readonly HashSet<ulong> readyPlayers = new();

    private void Awake()
    {
        _connectionManager = FindObjectOfType<ConnectionManager>();
        SceneManager.sceneLoaded += (_, _) =>
        {
            ReturnToSpaceshipServerRpc();
        };
    }
    
    private void OnTriggerEnter (Collider col) {
        // Add the GameObject collided with to the list.
        currentCollisions.Add(col.gameObject);
        Debug.Log("Tests: " + MultiPlayerWrapper.isGameHost + " - " + GetPlayersInTeleporter().Count);
        if (_connectionManager.m_CurrentState is OfflineState || (MultiPlayerWrapper.isGameHost && GetPlayersInTeleporter().Count == 2))
        {
            SceneLoaderWrapper.Instance.LoadScene("SpaceshipScene", true, LoadSceneMode.Additive);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReturnToSpaceshipServerRpc(ServerRpcParams serverRpcParams = default)
    {
        readyPlayers.Add(serverRpcParams.Receive.SenderClientId);
        if (readyPlayers.Count == 2)
        {
            ReturnToSpaceshipClientRpc();
        }
    }
    
    [ClientRpc]
    private void ReturnToSpaceshipClientRpc()
    {
        if (IsHost)
        {
            var p1Pos = GameObject.FindGameObjectWithTag("Player1RespawnPoint").transform.position;
            MultiPlayerWrapper.localPlayer.ResetPlayerPos();
            MultiPlayerWrapper.localPlayer.transform.position = p1Pos;
            MultiPlayerWrapper.localPlayer.GetComponentInChildren<Rigidbody>().velocity = Vector3.zero;
        }
        else
        {
            var p2Pos = GameObject.FindGameObjectWithTag("Player2RespawnPoint").transform.position;
            MultiPlayerWrapper.localPlayer.ResetPlayerPos();
            MultiPlayerWrapper.localPlayer.transform.position = p2Pos;
            MultiPlayerWrapper.localPlayer.GetComponentInChildren<Rigidbody>().velocity = Vector3.zero;
        }
        SceneLoaderWrapper.Instance.UnloadAdditiveScenes();
    }
 
    private void OnTriggerExit (Collider col) {
 
        // Remove the GameObject collided with from the list.
        currentCollisions.Remove(col.gameObject);
    }
    
    private List<GameObject> GetPlayersInTeleporter()
    {
        var res = currentCollisions.FindAll(x => x.CompareTag("Player"));
        return res;
    }
}
