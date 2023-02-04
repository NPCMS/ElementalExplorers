using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyMenuUI : NetworkBehaviour
{
    [SerializeField] private GameObject startGameBtn;
    [SerializeField] private GameObject leaveLobbyBtn;
    [SerializeField] private GameObject lobbyText;
    [SerializeField] private GameObject lobbyUI;
    [SerializeField] private GameObject mainMenuUI;
    [SerializeField] private GameObject player1ConnectedBtn;
    [SerializeField] private GameObject player2ConnectedBtn;
    [SerializeField] private GameObject player1ReadyBtn;
    [SerializeField] private GameObject player2ReadyBtn;
    [SerializeField] private Material activatedMat;
    [SerializeField] private Material disabledMat;

    private NetworkVariable<int> numClients = new NetworkVariable<int>(1);
    private NetworkVariable<bool> player1Ready = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> player2Ready = new NetworkVariable<bool>(false);

    private void OnEnable()
    {
        if (IsHost) {
            player1Ready.Value = false;
            player2Ready.Value = false;
        }

        switchButtonStyle(player1ReadyBtn, "NOT READY", "READY", player1Ready.Value);
        switchButtonStyle(player2ReadyBtn, "NOT READY", "READY", player2Ready.Value);
        switchButtonStyle(player1ConnectedBtn, "NOT READY", "READY", numClients.Value == 2);

        startGameBtn.GetComponent<UIInteraction>().AddCallback(() =>
        {
            Debug.Log(numClients.Value);
            Debug.Log(player1Ready.Value);
            Debug.Log(player2Ready.Value);
            if (player1Ready.Value && player2Ready.Value && numClients.Value == 2)
            {
                startGameServerRpc();
            }
        });

        leaveLobbyBtn.GetComponent<UIInteraction>().AddCallback(() =>
        {
            NetworkManager.Singleton.Shutdown();
            mainMenuUI.SetActive(true);
            lobbyUI.SetActive(false);
        });

        player1ReadyBtn.GetComponent<UIInteraction>().AddCallback(() =>
        {
            if (IsHost)
            {
                player1Ready.Value = !player1Ready.Value;
            }   
        });

        player2ReadyBtn.GetComponent<UIInteraction>().AddCallback(() =>
        {
            if (!IsHost)
            {
                playerReadyServerRpc();
            }
        });

        switchButtonStyle(player1ConnectedBtn, "DISCONNECTED", "CONNECTED", true);
        player1Ready.OnValueChanged += (bool previous, bool current) =>
        {
            switchButtonStyle(player1ReadyBtn, "NOT READY", "READY", current);
        };
        player2Ready.OnValueChanged += (bool previous, bool current) =>
        {
            switchButtonStyle(player2ReadyBtn, "NOT READY", "READY", current);
        };
        numClients.OnValueChanged += (int previous, int current) =>
        {
            switchButtonStyle(player2ConnectedBtn, "DISCONNECTED", "CONNECTED", current == 2);
            if (!IsHost && current == 2)
            {
                NetworkManager.Singleton.Shutdown();
                mainMenuUI.SetActive(true);
                lobbyUI.SetActive(false);
            }
        };

    }
    public void setJoinCodeText(string joinCode)
    {
        lobbyText.GetComponentInChildren<TMP_Text>().text = joinCode;
    }

    private void switchButtonStyle(GameObject button, string falseText, string trueText, bool on) 
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
            numClients.Value = NetworkManager.Singleton.ConnectedClientsList.Count;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void playerReadyServerRpc()
    {
        player2Ready.Value = !player2Ready.Value;
    }

    [ServerRpc(RequireOwnership = false)]
    private void startGameServerRpc()
    {
        NetworkManager.SceneManager.LoadScene("GameTestScene", LoadSceneMode.Single);
    }
}
