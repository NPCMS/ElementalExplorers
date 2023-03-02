using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Netcode.SceneManagement;
using Unity.BossRoom.ConnectionManagement;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Netcode;
using UnityEngine;
using SessionPlayerData = Netcode.SessionPlayerData;

public class MenuState : NetworkBehaviour
{
    [SerializeField] private LobbyMenuUI _lobbyMenuUI;
    [SerializeField] private MainMenuUI _mainMenuUI;
    
    private ConnectionManager _connectionManager;
    private global::Netcode.SessionManager<SessionPlayerData> _sessionManager;

    void Awake()
    { 
       _connectionManager = FindObjectOfType<ConnectionManager>();
       _connectionManager.AddCallback(ChangedStateCallback);
       _sessionManager = Netcode.SessionManager<SessionPlayerData>.Instance;
       _mainMenuUI.enabled = true;
    }

    public void ChangedStateCallback(ConnectionState newState)
    {
        if (newState is HostingState || newState is ClientConnectedState)
        {
            Debug.Log("Menu has switched to lobby");
            _mainMenuUI.gameObject.SetActive(false);
            _lobbyMenuUI.gameObject.SetActive(true);
            _lobbyMenuUI.SetUI(_connectionManager.joinCode, false, false);
        } 
        else if (newState is ClientConnectingState || newState is StartingHostState)
        {
            Debug.Log("Menu has disabled UI");
            _mainMenuUI.gameObject.SetActive(false);
            _lobbyMenuUI.gameObject.SetActive(false);
        }
        else
        {
            Debug.Log("Menu has switched to main menu");
            _mainMenuUI.gameObject.SetActive(true);
            _lobbyMenuUI.gameObject.SetActive(false);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestStartGameServerRpc()
    {
        Debug.Log("Requested to start the game with " + _sessionManager.GetConnectedCount() + " players connected");
        if (_sessionManager.GetConnectedCount() == 2)
        {
            SceneLoaderWrapper.Instance.LoadScene("Precompute", useNetworkSceneManager: true);
        }
    }
}
