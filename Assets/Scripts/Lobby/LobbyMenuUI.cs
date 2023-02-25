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
        startGameBtn.GetComponent<UIInteraction>().AddCallback(() =>
        {
            Debug.Log("Start Pressed (1R, 2R, 2C, C|Host)" + player1Ready + player2Ready + player2Connected + (NetworkManager.Singleton.IsConnectedClient || IsHost));
            if (player1Ready && player2Ready && player2Connected && (NetworkManager.Singleton.IsConnectedClient || IsHost))
            {
            }
        });

        leaveLobbyBtn.GetComponent<UIInteraction>().AddCallback(() =>
        {
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
            }
        });

        player2ReadyBtn.GetComponent<UIInteraction>().AddCallback(() =>
        {
            Debug.Log("Player 2 Ready Pressed");
            if (!IsHost && NetworkManager.Singleton.IsConnectedClient)
            {
                player2Ready = !player2Ready;
                SwitchButtonStyle(player2ReadyBtn, "NOT READY", "READY", player2Ready);
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
        player1Connected = false;
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
}
