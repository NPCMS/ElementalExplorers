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
        if (newState is HostingState)
        {
            Debug.Log("Menu knows about hosting");
        } else if (newState is ClientConnectedState)
        {
            Debug.Log("Menu knows about client");
        }
    }
}
