using System;
using System.Collections;
using System.Collections.Generic;
using Netcode.ConnectionManagement;
using Netcode.ConnectionManagement.ConnectionState;
using Netcode.SceneManagement;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialState : NetworkBehaviour
{
    List <GameObject> currentCollisions = new();
    private ConnectionManager _connectionManager;
    private string nextScene = "OSMData";

    private void Awake()
    {
        _connectionManager = FindObjectOfType<ConnectionManager>();
    }

    void OnTriggerEnter (Collider col) {
 
        // Add the GameObject collided with to the list.
        currentCollisions.Add (col.gameObject);
    }
 
    void OnTriggerExit (Collider col) {
 
        // Remove the GameObject collided with from the list.
        currentCollisions.Remove (col.gameObject);
    }
    
    public List<GameObject> GetPlayersInElevator()
    {
        return currentCollisions.FindAll(x => x.CompareTag("Player"));
    }

    public void Update()
    {
        if (_connectionManager.m_CurrentState is OfflineState || IsHost && GetPlayersInElevator().Count == 2)
        {
            SceneLoaderWrapper.Instance.LoadScene(nextScene, true);
        }
    }
}
