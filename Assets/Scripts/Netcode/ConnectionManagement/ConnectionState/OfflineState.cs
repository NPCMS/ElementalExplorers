using System;
using Unity.BossRoom.ConnectionManagement;
using Unity.BossRoom.Utils;
using Unity.Multiplayer.Samples.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

namespace UUnity.BossRoom.ConnectionManagement
{
    /// <summary>
    /// Connection state corresponding to when the NetworkManager is shut down. From this state we can transition to the
    /// ClientConnecting sate, if starting as a client, or the StartingHost state, if starting as a host.
    /// </summary>
    class OfflineState : ConnectionState
    {
        [Inject]
        ProfileManager m_ProfileManager;

        const string k_MainMenuSceneName = "MenuScene";

        public override void Enter()
        {
            m_ConnectionManager.NetworkManager.Shutdown();
            if (SceneManager.GetActiveScene().name != k_MainMenuSceneName)
            {
                SceneLoaderWrapper.Instance.LoadScene(k_MainMenuSceneName, useNetworkSceneManager: false);
            }
        }

        public override void Exit() { }

        public override void StartClientLobby(string joinCode)
        {
            var connectionMethod = new ConnectionMethodRelay(joinCode, m_ConnectionManager, m_ProfileManager);
            m_ConnectionManager.m_ClientReconnecting.Configure(connectionMethod);
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_ClientConnecting.Configure(connectionMethod));
        }

        public override void StartHostLobby(string joinCode)
        {
            var connectionMethod = new ConnectionMethodRelay(joinCode, m_ConnectionManager, m_ProfileManager);
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_StartingHost.Configure(connectionMethod));
        }
    }
}
