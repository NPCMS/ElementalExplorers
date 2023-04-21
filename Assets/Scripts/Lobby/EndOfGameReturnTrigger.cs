using System;
using System.Collections.Generic;
using Netcode.ConnectionManagement;
using Netcode.ConnectionManagement.ConnectionState;
using Unity.Netcode;
using UnityEngine;

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
        if (_connectionManager.m_CurrentState is OfflineState || IsHost && GetPlayersInTeleporter().Count == 2)
        {
            if (IsHost)
            {
                // todo load spaceship scene and reset game / load end of game loop / close doors / other crap
            }
        }
    }
 
    private void OnTriggerExit (Collider col) {
 
        // Remove the GameObject collided with from the list.
        currentCollisions.Remove(col.gameObject);
    }
    
    private List<GameObject> GetPlayersInTeleporter()
    {
        return currentCollisions.FindAll(x => x.CompareTag("Player"));
    }
}
