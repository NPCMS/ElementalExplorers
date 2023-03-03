using Netcode.ConnectionManagement;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class LobbyMenuUI : NetworkBehaviour
{
    [SerializeField] private GameObject startGameBtn;
    [SerializeField] private GameObject leaveLobbyBtn;
    [SerializeField] private GameObject lobbyText;
    [SerializeField] private Material activatedMat;
    [SerializeField] private Material disabledMat;
    [SerializeField] private MenuState menuState;

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
    }

    private void OnDisable()
    {
        // When disabled reset all UI elements
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
    }
}
