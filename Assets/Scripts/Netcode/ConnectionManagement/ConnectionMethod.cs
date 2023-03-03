using System.Threading.Tasks;
using Netcode.Infrastructure;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace Netcode.ConnectionManagement
{
    /// <summary>
    /// ConnectionMethod contains all setup needed to setup NGO to be ready to start a connection, either host or client side.
    /// Please override this abstract class to add a new transport or way of connecting.
    /// </summary>
    public abstract class ConnectionMethodBase
    {
        public string joinCode;
        protected ConnectionManager m_ConnectionManager;
        readonly ProfileManager m_ProfileManager;

        public abstract Task SetupHostConnectionAsync();

        public abstract Task SetupClientConnectionAsync();

        public ConnectionMethodBase(string jCode, ConnectionManager connectionManager, ProfileManager profileManager)
        {
            joinCode = jCode;
            m_ConnectionManager = connectionManager;
            m_ProfileManager = profileManager;
        }

        protected void SetConnectionPayload(string playerId)
        {
            var payload = JsonUtility.ToJson(new ConnectionPayload()
            {
                playerId = playerId,
                isDebug = Debug.isDebugBuild
            });

            var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

            m_ConnectionManager.NetworkManager.NetworkConfig.ConnectionData = payloadBytes;
        }

        protected string GetPlayerId()
        {
            if (Unity.Services.Core.UnityServices.State != ServicesInitializationState.Initialized)
            {
                return ClientPrefs.GetGuid() + m_ProfileManager.Profile;
            }

            return AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerId : ClientPrefs.GetGuid() + m_ProfileManager.Profile;
        }
    }

    /// <summary>
    /// UTP's Relay connection setup
    /// </summary>
    class ConnectionMethodRelay : ConnectionMethodBase
    {

        public ConnectionMethodRelay(string jCode, ConnectionManager connectionManager, ProfileManager profileManager)
            : base(jCode, connectionManager, profileManager)
        {
            joinCode = jCode;
            m_ConnectionManager = connectionManager;
        }

        public override async Task SetupClientConnectionAsync()
        {
            Debug.Log("Setting up Unity Relay client");

            SetConnectionPayload(GetPlayerId());

            Debug.Log($"Setting Unity Relay client with join code {joinCode}");

            // Create client joining allocation from join code
            var joinedAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            m_ConnectionManager.joinCode = joinCode;
            Debug.Log($"client: {joinedAllocation.ConnectionData[0]} {joinedAllocation.ConnectionData[1]}, " +
                      $"host: {joinedAllocation.HostConnectionData[0]} {joinedAllocation.HostConnectionData[1]}, " +
                      $"client: {joinedAllocation.AllocationId}");
        
            // Configure UTP with allocation
            var utp = (UnityTransport)m_ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            utp.SetRelayServerData(new RelayServerData(joinedAllocation, OnlineState.k_DtlsConnType));
        }

        public override async Task SetupHostConnectionAsync()
        {
            Debug.Log("Setting up Unity Relay host");

            SetConnectionPayload(GetPlayerId()); // Need to set connection payload for host as well, as host is a client too

            // Create relay allocation
            Allocation hostAllocation = await RelayService.Instance.CreateAllocationAsync(m_ConnectionManager.MaxConnectedPlayers, region: null);
            var jCode = await RelayService.Instance.GetJoinCodeAsync(hostAllocation.AllocationId);

            Debug.Log($"server: connection data: {hostAllocation.ConnectionData[0]} {hostAllocation.ConnectionData[1]}, " +
                      $"allocation ID:{hostAllocation.AllocationId}, region:{hostAllocation.Region}");

            joinCode = jCode;
            m_ConnectionManager.joinCode = joinCode;
            Debug.Log("Join Code is: " + joinCode);

            // Setup UTP with relay connection info
            var utp = (UnityTransport)m_ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            utp.SetRelayServerData(new RelayServerData(hostAllocation, OnlineState.k_DtlsConnType)); // This is with DTLS enabled for a secure connection
        }
    }
}