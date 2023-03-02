using TMPro;
using Unity.BossRoom.ConnectionManagement;
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
    [SerializeField] private MenuState menuState;
    
    // Lobby Data
    public struct LobbyData : INetworkSerializable
    {
        public bool player1Ready;
        public bool player2Ready;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref player1Ready);
            serializer.SerializeValue(ref player2Ready);
        }
    }

    private NetworkVariable<LobbyData> lobbyData = new NetworkVariable<LobbyData>(
        new LobbyData
        {
            player1Ready = false,
            player2Ready = false,
        }, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Start()
    {
        startGameBtn.GetComponent<UIInteraction>().AddCallback(() =>
        {
            menuState.RequestStartGameServerRpc();
        });

        leaveLobbyBtn.GetComponent<UIInteraction>().AddCallback(() =>
        {
            ConnectionManager connectionManager = FindObjectOfType<ConnectionManager>();
            connectionManager.RequestShutdown();
        });

        lobbyData.OnValueChanged += (value, newValue) =>
        {
            SwitchButtonStyle(player1ReadyBtn, "NOT READY", "READY", newValue.player1Ready);
            SwitchButtonStyle(player2ReadyBtn, "NOT READY", "READY", newValue.player2Ready);
            player1ReadyBtn.GetComponent<UIInteraction>().enabled = true;
            player2ReadyBtn.GetComponent<UIInteraction>().enabled = true;
        };
        
        player1ReadyBtn.GetComponent<UIInteraction>().AddCallback(player1Pressed);
        player2ReadyBtn.GetComponent<UIInteraction>().AddCallback(player2Pressed);
    }

    private void player1Pressed()
    {
        // Flip the ready button and tell the other player that it has happened
        if (IsHost)
        {
            ChangeReadyServerRPC(!lobbyData.Value.player1Ready, lobbyData.Value.player2Ready);
            player1ReadyBtn.GetComponent<UIInteraction>().enabled = false;
        }
    }
    
    private void player2Pressed()
    {
        if (!IsHost)
        {
            ChangeReadyServerRPC(lobbyData.Value.player1Ready, !lobbyData.Value.player2Ready);
            player2ReadyBtn.GetComponent<UIInteraction>().enabled = false;
        }
    }

    private void OnDisable()
    {
        // When disabled reset all UI elements
        SwitchButtonStyle(player1ReadyBtn, "NOT READY", "READY", false);
        SwitchButtonStyle(player2ReadyBtn, "NOT READY", "READY", false);
        lobbyText.GetComponentInChildren<TMP_Text>().text = "";
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

    public void SetUI(string joinCode, bool ready1, bool ready2)
    {
        lobbyText.GetComponentInChildren<TMP_Text>().text = joinCode;
        SwitchButtonStyle(player1ReadyBtn, "NOT READY", "READY", ready1);
        SwitchButtonStyle(player2ReadyBtn, "NOT READY", "READY", ready2);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ChangeReadyServerRPC(bool player1, bool player2)
    {
        lobbyData.Value = new()
        {
            player1Ready = player1,
            player2Ready = player2
        };
    }
}
