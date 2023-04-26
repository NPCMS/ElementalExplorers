using Netcode.ConnectionManagement;
using TMPro;
using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private UIInteraction createLobbyBtn;
    [SerializeField] private UIInteraction joinLobbyBtn;
    [SerializeField] private UIInteraction backBtn;
    [SerializeField] private UIInteraction enterCodeBtn;
    [SerializeField] private TMP_Text lobbyCodeInput;
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject codeMenu;
    private ConnectionManager connectionManager;

    private void Awake()
    {
        createLobbyBtn.AddCallback((RaycastHit hit, SteamInputCore.Button button) =>
        {
            connectionManager.StartHostLobby();
        });
        
        joinLobbyBtn.AddCallback((RaycastHit hit, SteamInputCore.Button button) =>
        {
            mainMenu.SetActive(false);
            codeMenu.SetActive(true);
        });

        backBtn.AddCallback((RaycastHit hit, SteamInputCore.Button button) =>
        {
            mainMenu.SetActive(true);
            codeMenu.SetActive(false);
            lobbyCodeInput.text = "";
        });

        enterCodeBtn.AddCallback((RaycastHit hit, SteamInputCore.Button button) =>
        {
            string joinCode = lobbyCodeInput.text;
            if (joinCode.Length != 6)
            {
                Debug.Log("Invalid lobby code");
                return;
            }

            connectionManager.StartClientLobby(joinCode);
        });
    }
}
