using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyMenuUI : NetworkBehaviour
{
    [SerializeField] private GameObject startGameBtn;
    [SerializeField] private GameObject leaveLobbyBtn;
    [SerializeField] private GameObject lobbyText;
    [SerializeField] private GameObject mainMenuUI;
    [SerializeField] private GameObject player1ConnectedBtn;
    [SerializeField] private GameObject player2ConnectedBtn;
    [SerializeField] private GameObject player1ReadyBtn;
    [SerializeField] private GameObject player2ReadyBtn;
    [SerializeField] private Material activatedMat;
    [SerializeField] private Material disabledMat;

    private bool player1Ready;
    private bool player2Ready;
    private bool player2Connected;
    private bool player1Connected;

    private void Awake()
    {

        NetworkManager.Singleton.OnClientDisconnectCallback += (ulong id) =>
        {
            Debug.Log("Client Disconnected");
            if (id == NetworkManager.LocalClientId)
            {
                NetworkManager.Singleton.Shutdown();
                ReturnToMainMenu();
            }
        };

        startGameBtn.GetComponent<UIInteraction>().AddCallback(() =>
        {
            // startGameServerRpc();
            // return;
            Debug.Log("Start Pressed (1R, 2R, 2C, C|Host)" + player1Ready + player2Ready + player2Connected + (NetworkManager.Singleton.IsConnectedClient || IsHost));
            if (player1Ready && player2Ready && player2Connected && (NetworkManager.Singleton.IsConnectedClient || IsHost))
            {
                Debug.Log("Sending Start Game Server RPC");
                StartGameServerRpc();
            }
        });

        leaveLobbyBtn.GetComponent<UIInteraction>().AddCallback(() =>
        {
            NetworkManager.Singleton.Shutdown();
            ReturnToMainMenu();
        });
        
        player1ReadyBtn.GetComponent<UIInteraction>().AddCallback(() =>
        {
            Debug.Log("Player 1 Ready Pressed");
            // Flip the ready button and tell the other player that it has happened
            if (IsHost)
            {
                player1Ready = !player1Ready;
                SwitchButtonStyle(player1ReadyBtn, "NOT READY", "READY", player1Ready);
                ReadyStatusClientRpc(player1Ready);
            }
        });

        player2ReadyBtn.GetComponent<UIInteraction>().AddCallback(() =>
        {
            Debug.Log("Player 2 Ready Pressed");
            if (!IsHost && NetworkManager.Singleton.IsConnectedClient)
            {
                player2Ready = !player2Ready;
                SwitchButtonStyle(player2ReadyBtn, "NOT READY", "READY", player2Ready);
                ReadyStatusServerRpc(player2Ready);
            }
        });
    }

    private void OnDisable()
    {
        // When disabled reset all UI elements
        SwitchButtonStyle(player1ReadyBtn, "NOT READY", "READY", false);
        SwitchButtonStyle(player2ReadyBtn, "NOT READY", "READY", false);
        SwitchButtonStyle(player1ConnectedBtn, "DISCONNECTED", "CONNECTED", false);
        SwitchButtonStyle(player2ConnectedBtn, "DISCONNECTED", "CONNECTED", false);
        lobbyText.GetComponentInChildren<TMP_Text>().text = "";
        player1Ready = false;
        player2Ready = false;
        player2Connected = false;
    }

    private void SwitchButtonStyle(GameObject button, string falseText, string trueText, bool on) 
    {
        if (on)
        {
            button.GetComponentInChildren<TMP_Text>().text = trueText;
            button.GetComponent<MeshRenderer>().material = activatedMat;
        }
        else
        {
            button.GetComponentInChildren<TMP_Text>().text = falseText;
            button.GetComponent<MeshRenderer>().material = disabledMat;
        }
    }

    private void Update()
    {
        if (IsHost)
        {
            if (!player2Connected && NetworkManager.Singleton.ConnectedClientsList.Count == 2)
            {
                // Player 2 has connected
                player2Connected = true;
                SwitchButtonStyle(player2ConnectedBtn, "DISCONNECTED", "CONNECTED", true);
                HandshakeClientRpc(player1Ready);
            }
            else if (player2Connected && NetworkManager.Singleton.ConnectedClientsList.Count <= 1)
            {
                // Player 2 has disconnected
                player2Connected = false;
                SwitchButtonStyle(player2ConnectedBtn, "DISCONNECTED", "CONNECTED", false);
                SwitchButtonStyle(player2ReadyBtn, "NOT READY", "READY", false);
            }
            
            if (!player1Connected && NetworkManager.Singleton.ConnectedClientsList.Count > 0)
            {
                // Player 1 has connected
                player1Connected = true;
                SwitchButtonStyle(player1ConnectedBtn, "DISCONNECTED", "CONNECTED", true);
            }
        }
    }
    
    public void SetLobbyJoinCode(string joinCode)
    {
        if (IsHost)
        {
            SwitchButtonStyle(player1ConnectedBtn, "DISCONNECTED", "CONNECTED", true);
        }
        lobbyText.GetComponentInChildren<TMP_Text>().text = joinCode;
        SwitchButtonStyle(player1ReadyBtn, "NOT READY", "READY", player1Ready);
        SwitchButtonStyle(player2ReadyBtn, "NOT READY", "READY", player2Ready);
    }

    public void ReturnToMainMenu()
    {
        mainMenuUI.SetActive(true);
        gameObject.SetActive(false);
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartGameServerRpc()
    {
        NetworkManager.SceneManager.LoadScene("Precompute", LoadSceneMode.Single);
    }

    // Tell player 2 that it has connected and send button status
    [ClientRpc]
    private void HandshakeClientRpc(bool player1Rdy)
    {
        Debug.Log("Handshake RPC recieved");
        if (!IsHost)
        {
            Debug.Log("Handshake RPC Processed as non-Host");
            player2Connected = true;
            player1Ready = player1Rdy;
            SwitchButtonStyle(player1ReadyBtn, "NOT READY", "READY", player1Ready);
            SwitchButtonStyle(player1ConnectedBtn, "DISCONNECTED", "CONNECTED", true);
            SwitchButtonStyle(player2ConnectedBtn, "DISCONNECTED", "CONNECTED", true);
            ReadyStatusServerRpc(player2Ready);
        }
    }

    // Player 1 Calls Player 2 with a new ready status
    [ClientRpc]
    private void ReadyStatusClientRpc(bool newReadyStatus)
    {
        Debug.Log("Recieved Ready Button Client RPC");
        if (!IsHost && NetworkManager.Singleton.IsConnectedClient)
        {
            player1Ready = newReadyStatus;
            SwitchButtonStyle(player1ReadyBtn, "NOT READY", "READY", player1Ready);
            Debug.Log("Updated player 2 ready, new ready status" + player1Ready);
        }
    }
    
    // Player 2 Calls Player 1 with a new ready status
    [ServerRpc(RequireOwnership = false)]
    private void ReadyStatusServerRpc(bool newReadyStatus)
    {
        Debug.Log("Recieved Ready Button Server RPC");
        player2Ready = newReadyStatus;
        SwitchButtonStyle(player2ReadyBtn, "NOT READY", "READY", player2Ready);
        Debug.Log("Updated player 2 ready, new ready status" + player2Ready);
    }
}
