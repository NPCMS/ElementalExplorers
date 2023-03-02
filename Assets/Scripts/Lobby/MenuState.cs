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

    // Lobby Data
    public struct LobbyData : INetworkSerializable
    {
        public bool player1Ready;
        public bool player2Ready;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref player1Ready);
            serializer.SerializeValue(ref player2Ready);
        }
    }
    
    void Start()
    { 
       _connectionManager = FindObjectOfType<ConnectionManager>();
       _sessionManager = SessionManager<SessionPlayerData>.Instance;
       _connectionManager.AddCallback(ChangedStateCallback);
       _mainMenuUI.enabled = true;
       menuState = false;
    }

    void OnDestroy()
    {
        _connectionManager.RemoveCallbacks();
    }

    public void ChangedStateCallback(ConnectionState newState)
    {
        if (newState is HostingState || newState is ClientConnectedState)
        {
            Debug.Log("Menu has switched to lobby");
            _mainMenuUI.enabled = false;
            _lobbyMenuUI.enabled = true;
            _lobbyMenuUI.setUI(false, false, false);
        } 
        else if (newState is ClientConnectingState || newState is StartingHostState)
        {
            Debug.Log("Menu has disabled UI");
            _mainMenuUI.enabled = false;
            _lobbyMenuUI.enabled = false;
        }
        else
        {
            Debug.Log("Menu has switched to main menu");
            _mainMenuUI.enabled = true;
            _lobbyMenuUI.enabled = false;
        }
    }
}
