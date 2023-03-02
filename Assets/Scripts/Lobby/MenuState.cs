using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Unity.BossRoom.ConnectionManagement;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Netcode;
using UnityEngine;

public class MenuState : NetworkBehaviour
{
    [SerializeField] private LobbyMenuUI _lobbyMenuUI;
    [SerializeField] private MainMenuUI _mainMenuUI;
    
    private ConnectionManager _connectionManager;
    private SessionManager<SessionPlayerData> _sessionManager;
    private bool menuState;

    void Awake()
    { 
       _connectionManager = FindObjectOfType<ConnectionManager>();
       _connectionManager.AddCallback(ChangedStateCallback);
       //_sessionManager = SessionManager<SessionPlayerData>.Instance;
       _mainMenuUI.enabled = true;
       menuState = false;
    }

    void OnDestroy()
    {
        _connectionManager = FindObjectOfType<ConnectionManager>();
        _connectionManager.RemoveCallbacks();
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
}
