using System;
using TMPro;
using Unity.BossRoom.ConnectionManagement;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using UnityEngine;

public class LobbyMenuUI : NetworkBehaviour
{
    [SerializeField] private GameObject startGameBtn;
    [SerializeField] private GameObject leaveLobbyBtn;
    [SerializeField] private GameObject lobbyText;
    [SerializeField] private GameObject player1ReadyBtn;
    [SerializeField] private GameObject player2ReadyBtn;
    [SerializeField] private Material activatedMat;
    [SerializeField] private Material disabledMat;

    private bool player1Ready;
    private bool player2Ready;
    private bool player2Connected;
    private bool player1Connected;

    private void Start()
    {
        startGameBtn.GetComponent<UIInteraction>().AddCallback(() =>
        {
            SceneLoaderWrapper.Instance.LoadScene("Precompute", useNetworkSceneManager: true);
        });

        leaveLobbyBtn.GetComponent<UIInteraction>().AddCallback(() =>
        {
            ConnectionManager connectionManager = FindObjectOfType<ConnectionManager>();
            connectionManager.RequestShutdown();
        });
        
        player1ReadyBtn.GetComponent<UIInteraction>().AddCallback(player1Pressed);
        player2ReadyBtn.GetComponent<UIInteraction>().AddCallback(player2Pressed);
    }

    private void player1Pressed()
    {
        Debug.Log("Player 1 Ready Pressed");
        // Flip the ready button and tell the other player that it has happened
        if (IsHost)
        {
            player1Ready = !player1Ready;
            SwitchButtonStyle(player1ReadyBtn, "NOT READY", "READY", player1Ready);
        }
    }
    
    private void player2Pressed()
    {
        Debug.Log("Player 2 Ready Pressed");
        if (!IsHost && NetworkManager.Singleton.IsConnectedClient)
        {
            player2Ready = !player2Ready;
            SwitchButtonStyle(player2ReadyBtn, "NOT READY", "READY", player2Ready);
        }
    }

    private void OnDisable()
    {
        // When disabled reset all UI elements
        SwitchButtonStyle(player1ReadyBtn, "NOT READY", "READY", false);
        SwitchButtonStyle(player2ReadyBtn, "NOT READY", "READY", false);
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

    public void setUI(string joinCode, bool ready1, bool ready2)
    {
        //lobbyText.GetComponentInChildren<TMP_Text>().text = localLobby.RelayJoinCode;
        SwitchButtonStyle(player1ReadyBtn, "NOT READY", "READY", ready1);
        SwitchButtonStyle(player2ReadyBtn, "NOT READY", "READY", ready2);
    }
}
