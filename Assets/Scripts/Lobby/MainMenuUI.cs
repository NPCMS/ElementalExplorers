using System;
using TMPro;
using Unity.BossRoom.ConnectionManagement;
using Unity.BossRoom.UnityServices.Lobbies;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using VContainer;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private UIInteraction createLobbyBtn;
    [SerializeField] private UIInteraction joinLobbyBtn;
    [SerializeField] private TMP_Text lobbyCodeInput;
    [SerializeField] private GameObject lobbyUI;
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
            lobbyUI.SetActive(true);
            gameObject.SetActive(false);
        });

        joinLobbyBtn.AddCallback(() =>
        {
            string joinCode = lobbyCodeInput.text;
            if (joinCode.Length != 6)
            {
                Debug.Log("Invalid lobby code");
                return;
            }

            //localLobby.RelayJoinCode = joinCode;
            connectionManager.StartClientLobby();
            lobbyUI.SetActive(true);
            gameObject.SetActive(false);
        });
    }
}
