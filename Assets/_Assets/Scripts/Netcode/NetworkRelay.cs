using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

public class NetworkRelay : MonoBehaviour
{
    [SerializeField] private LobbyMenuUI lobbyMenuUI;
    public int lobbyNumber = 0;

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void CreateRelay()
    {
        try
        {
            lobbyNumber += 1;
            int prevLobbyNumber = lobbyNumber;

            // Send an API request to Relay to open a server
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);

            // Get the join code
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log("Join Code is: " + joinCode);

            if (prevLobbyNumber != lobbyNumber)
            {
                Debug.LogWarning("Host is attempting to connect to a stale lobby: Stale=" + prevLobbyNumber + ", Current=" + lobbyNumber);
            } else
            {
                lobbyMenuUI.connectedToServer(joinCode, lobbyNumber);

                // Get the relay service info and give it to the network manager
                // This may need to be changed if the version of NGO is updated!!!
                RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

                // Call the network manger to start the host
                NetworkManager.Singleton.StartHost();
            }
        } catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void JoinRelay(string joinCode)
    {
        try
        {
            Debug.Log("Joining Relay with " + joinCode);
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);


            // Get the relay service info and give it to the network manager
            // This may need to be changed if the version of NGO is updated!!!
            RelayServerData relayServerData = new(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartClient();
            lobbyMenuUI.connectedToServer(joinCode, lobbyNumber);
            
        } catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }
}
