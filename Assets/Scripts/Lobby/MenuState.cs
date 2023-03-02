using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Netcode.ConnectionManagement;
using Netcode.ConnectionManagement.ConnectionState;
using Netcode.SceneManagement;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Netcode;
using UnityEngine;
using SessionPlayerData = Netcode.SessionManagement.SessionPlayerData;

public class MenuState : NetworkBehaviour
{
    [SerializeField] private LobbyMenuUI _lobbyMenuUI;
    [SerializeField] private MainMenuUI _mainMenuUI;
    
    private ConnectionManager _connectionManager;
    private global::Netcode.SessionManagement.SessionManager<SessionPlayerData> _sessionManager;

    void Awake()
    { 
       _connectionManager = FindObjectOfType<ConnectionManager>();
       _connectionManager.AddStateCallback += ChangedStateCallback;
       _sessionManager = Netcode.SessionManagement.SessionManager<SessionPlayerData>.Instance;
       _mainMenuUI.enabled = true;
    }

    void OnDestroy()
    {
        _connectionManager.AddStateCallback -= ChangedStateCallback;
    }

    public void ChangedStateCallback(ConnectionState newState)
    {
        if (newState is HostingState || newState is ClientConnectedState)
        {
            _mainMenuUI.gameObject.SetActive(false);
            _lobbyMenuUI.gameObject.SetActive(true);
            _lobbyMenuUI.SetUI(_connectionManager.joinCode, false, false);
        } 
        else if (newState is ClientConnectingState || newState is StartingHostState)
        {
            _mainMenuUI.gameObject.SetActive(false);
            _lobbyMenuUI.gameObject.SetActive(false);
        }
        else
        {
            _mainMenuUI.gameObject.SetActive(true);
            _lobbyMenuUI.gameObject.SetActive(false);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestStartGameServerRpc()
    {
        if (_sessionManager.GetConnectedCount() == 2)
        {
            SceneLoaderWrapper.Instance.LoadScene("Precompute", useNetworkSceneManager: true);
        }
    }
}
