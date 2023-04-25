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
    
    private void Awake()
    {
        _connectionManager = FindObjectOfType<ConnectionManager>();
        SceneManager.sceneLoaded += (_, _) =>
        {
            if (!IsHost) return;
            ReturnToSpaceshipClientRpc();
        };
    }
    
    private void OnTriggerEnter (Collider col) {
        Debug.Log("End trigger collider: " + col.gameObject.name);
        // Add the GameObject collided with to the list.
        currentCollisions.Add(col.gameObject);
        if (_connectionManager.m_CurrentState is OfflineState || IsHost && GetPlayersInTeleporter().Count == 2)
        {
            SceneLoaderWrapper.Instance.LoadScene("SpaceshipScene", true, LoadSceneMode.Additive);
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
        Debug.Log("Players in dropship: " + res.Count);
        return res;
    }
}
