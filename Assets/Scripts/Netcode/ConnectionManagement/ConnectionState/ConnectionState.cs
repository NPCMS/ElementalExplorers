using Netcode.Infrastructure.PubSub;
using Unity.Netcode;
using VContainer;

namespace Netcode.ConnectionManagement.ConnectionState
{
    /// <summary>
    /// Base class representing a connection state.
    /// </summary>
    public abstract class ConnectionState
    {
        [Inject]
        protected ConnectionManager m_ConnectionManager;

        [Inject]
        protected IPublisher<ConnectStatus> m_ConnectStatusPublisher;

        public abstract void Enter();

        public abstract void Exit();

        public virtual void OnClientConnected(ulong clientId) { }
        public virtual void OnClientDisconnect(ulong clientId) { }

        public virtual void OnServerStarted() { }
    
        public virtual void StartClientLobby(string joinCode) { }
    
        public virtual void StartHostLobby() { }

        public virtual void OnUserRequestedShutdown() { }

        public virtual void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response) { }

        public virtual void OnTransportFailure() { }
    }
}

