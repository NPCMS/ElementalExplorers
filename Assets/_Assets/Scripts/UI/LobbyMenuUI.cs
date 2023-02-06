using System;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    private NetworkVariable<int> numClients = new NetworkVariable<int>(0);
    private NetworkVariable<bool> player1Ready = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> player2Ready = new NetworkVariable<bool>(false);
    private bool connected = false;

    private void Awake()
    {
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
            gameObject.SetActive(false);
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


        // Callbacks for when network variables change
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
            switchButtonStyle(player1ConnectedBtn, "DISCONNECTED", "CONNECTED", current >= 1);
            switchButtonStyle(player2ConnectedBtn, "DISCONNECTED", "CONNECTED", current == 2);
            if (!IsHost && current == 1)
            {
                NetworkManager.Singleton.Shutdown();
                mainMenuUI.SetActive(true);
                gameObject.SetActive(false);
            }
        };
    }

    private void OnEnable()
    {
        switchButtonStyle(player1ReadyBtn, "NOT READY", "READY", player1Ready.Value);
        switchButtonStyle(player2ReadyBtn, "NOT READY", "READY", player2Ready.Value);
    }

    private void OnDisable()
    {
        connected = false;
        player1Ready.Value = false;
        player2Ready.Value = false;
        numClients.Value = 0;
        lobbyText.GetComponentInChildren<TMP_Text>().text = "";
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
        if (IsHost && connected)
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
        NetworkManager.SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
    }

    public void connectedToServer(string joinCode)
    {
        connected = true;
        lobbyText.GetComponentInChildren<TMP_Text>().text = joinCode;
    }
}
