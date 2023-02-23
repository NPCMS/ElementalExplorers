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

    private NetworkVariable<int> numClients = new NetworkVariable<int>(0);
    private NetworkVariable<bool> player1Ready = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> player2Ready = new NetworkVariable<bool>(false);

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
            // startGameServerRpc();
            // return;
            if (player1Ready.Value && player2Ready.Value && numClients.Value == 2 && NetworkManager.Singleton.IsConnectedClient)
            {
                Debug.Log("Sending Start Game Server RPC");
                StartGameServerRpc();
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
            if (!IsHost && NetworkManager.Singleton.IsConnectedClient)
            {
                Debug.Log("Sending Player Ready Server RPC");
                PlayerReadyServerRpc();
            }
        });


        // Callbacks for when network variables change
        player1Ready.OnValueChanged += (bool previous, bool current) =>
        {
            SwitchButtonStyle(player1ReadyBtn, "NOT READY", "READY", current);
        };
        
        player2Ready.OnValueChanged += (bool previous, bool current) =>
        {
            SwitchButtonStyle(player2ReadyBtn, "NOT READY", "READY", current);
        };
        
        numClients.OnValueChanged += (int previous, int current) =>
        {
            SwitchButtonStyle(player1ConnectedBtn, "DISCONNECTED", "CONNECTED", current >= 1);
            SwitchButtonStyle(player2ConnectedBtn, "DISCONNECTED", "CONNECTED", current == 2);
        };
    }

    private void OnEnable()
    {
        SwitchButtonStyle(player1ReadyBtn, "NOT READY", "READY", player1Ready.Value);
        SwitchButtonStyle(player2ReadyBtn, "NOT READY", "READY", player2Ready.Value);

    }

    private void OnDisable()
    {
        SwitchButtonStyle(player1ReadyBtn, "NOT READY", "READY", false);
        SwitchButtonStyle(player2ReadyBtn, "NOT READY", "READY", false);
        lobbyText.GetComponentInChildren<TMP_Text>().text = "";
        SwitchButtonStyle(player1ConnectedBtn, "DISCONNECTED", "CONNECTED", false);
        SwitchButtonStyle(player2ConnectedBtn, "DISCONNECTED", "CONNECTED", false);
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
            numClients.Value = NetworkManager.Singleton.ConnectedClientsList.Count;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayerReadyServerRpc()
    {
        player2Ready.Value = !player2Ready.Value;
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartGameServerRpc()
    {
        NetworkManager.SceneManager.LoadScene("Precompute", LoadSceneMode.Single);
    }

    public void SetLobbyJoinCode(string joinCode)
    {
        lobbyText.GetComponentInChildren<TMP_Text>().text = joinCode;
    }
    
    public void DisconnectedFromServer()
    {
        mainMenuUI.SetActive(true);
        gameObject.SetActive(false);
    }
}
