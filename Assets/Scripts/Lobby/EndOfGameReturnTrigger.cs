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
    
    private void Awake()
    {
        _connectionManager = FindObjectOfType<ConnectionManager>();
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
