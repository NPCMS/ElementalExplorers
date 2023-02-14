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
    [SerializeField] private NetworkRelay networkRelay;

    private NetworkVariable<int> numClients = new NetworkVariable<int>(0);
    private NetworkVariable<bool> player1Ready = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> player2Ready = new NetworkVariable<bool>(false);
    private bool connected = false;

    private void Awake()
    {

        NetworkManager.Singleton.OnClientDisconnectCallback += (ulong id) =>
        {
            NetworkManager.Singleton.Shutdown();
            mainMenuUI.SetActive(true);
            gameObject.SetActive(false);
        };

        startGameBtn.GetComponent<UIInteraction>().AddCallback(() =>
        {
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
        switchButtonStyle(player1ReadyBtn, "NOT READY", "READY", false);
        switchButtonStyle(player2ReadyBtn, "NOT READY", "READY", false);
        lobbyText.GetComponentInChildren<TMP_Text>().text = "";
        switchButtonStyle(player1ConnectedBtn, "DISCONNECTED", "CONNECTED", false);
        switchButtonStyle(player2ConnectedBtn, "DISCONNECTED", "CONNECTED", false);
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

    public void connectedToServer(string joinCode, int connectingLobbyNumber)
    {
        connected = true;
        lobbyText.GetComponentInChildren<TMP_Text>().text = joinCode;
    }

    public void disconnectedFromServer()
    {
        mainMenuUI.SetActive(true);
        gameObject.SetActive(false);
    }
}
