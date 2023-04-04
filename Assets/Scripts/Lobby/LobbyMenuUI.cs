using Netcode.ConnectionManagement;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class LobbyMenuUI : NetworkBehaviour
{
    [SerializeField] private GameObject leaveLobbyBtn;
    [SerializeField] private GameObject lobbyText;
    [SerializeField] private Material activatedMat;
    [SerializeField] private Material disabledMat;
    [SerializeField] private MenuState menuState;

    private void Start()
    {
        leaveLobbyBtn.GetComponent<UIInteraction>().AddCallback((RaycastHit hit, SteamInputCore.Button button) =>
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

    public void SetUI(string joinCode)
    {
        lobbyText.GetComponentInChildren<TMP_Text>().text = joinCode;
    }
}
