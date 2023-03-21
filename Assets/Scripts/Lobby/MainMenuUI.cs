using Netcode.ConnectionManagement;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private UIInteraction createLobbyBtn;
    [SerializeField] private UIInteraction joinLobbyBtn;
    [SerializeField] private UIInteraction backBtn;
    [SerializeField] private UIInteraction enterCodeBtn;
    [SerializeField] private UIInteraction leaveLobbyBtn;
    [SerializeField] private UIInteraction selectLocBtn;
    [SerializeField] private TMP_Text lobbyCodeInput;
    [SerializeField] private TMP_Text locationTxt;
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject codeMenu;
    [SerializeField] private GameObject locationMenu;
    private ConnectionManager connectionManager;
    
    private async void Start()
    {
        connectionManager = FindObjectOfType<ConnectionManager>();
        if (connectionManager == null)
        {
            throw new UnityException("Main Menu UI could not find the Connection Manager");
        }
        
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }
    
    private void Awake()
    {
        createLobbyBtn.AddCallback(() =>
        {
            connectionManager.StartHostLobby();
        });
        
        joinLobbyBtn.AddCallback(() =>
        {
            mainMenu.SetActive(false);
            codeMenu.SetActive(true);
        });

        backBtn.AddCallback(() =>
        {
            mainMenu.SetActive(true);
            codeMenu.SetActive(false);
            lobbyCodeInput.text = "";
        });

        enterCodeBtn.AddCallback(() =>
        {
            string joinCode = lobbyCodeInput.text;
            if (joinCode.Length != 6)
            {
                Debug.Log("Invalid lobby code");
                return;
            }
            
            locationMenu.SetActive(true);
            codeMenu.SetActive(false);
            //connectionManager.StartClientLobby(joinCode);
        });
        
        leaveLobbyBtn.AddCallback(() =>
        {
            locationTxt.text = "";
            lobbyCodeInput.text = "";
            mainMenu.SetActive(true);
            locationMenu.SetActive(false);
        });
        
        enterCodeBtn.AddCallback(() =>
        {
            if(locationTxt.text == "")
                Debug.Log("No city selected");
        });
    }
}
