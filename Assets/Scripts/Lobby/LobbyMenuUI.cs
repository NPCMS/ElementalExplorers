using System;
using Netcode.ConnectionManagement;
using Netcode.SessionManagement;
using TMPro;
using UnityEngine;

public class LobbyMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject leaveLobbyBtn;
    [SerializeField] private GameObject selectLocationBtn;
    [SerializeField] private GameObject lobbyText;
    [SerializeField] private GameObject selectLocationMenu; 
    [SerializeField] private GameObject connectionMenu;

    
    private SessionManager<SessionPlayerData> sessionManager;
    private bool switchedToLocationSelect;
    public bool isHost;
    public bool locationSelected;

    private void Start()
    {
        leaveLobbyBtn.GetComponent<UIInteraction>().AddCallback((RaycastHit hit, SteamInputCore.Button button) =>
        {
            ConnectionManager connectionManager = FindObjectOfType<ConnectionManager>();
            connectionManager.RequestShutdown();
        });
        
        selectLocationBtn.GetComponent<UIInteraction>().AddCallback((RaycastHit hit, SteamInputCore.Button button) =>
        {
            locationSelected = true;
        });
        sessionManager = SessionManager<SessionPlayerData>.Instance;
    }

    private void OnEnable()
    {
        connectionMenu.SetActive(true);
        selectLocationMenu.SetActive(false);
    }

    private void OnDisable()
    {
        // When disabled reset all UI elements
        lobbyText.GetComponentInChildren<TMP_Text>().text = "";
        locationSelected = false;
        isHost = false;
    }

    private void Update()
    {
        if (sessionManager.GetConnectedCount() == 2 && !switchedToLocationSelect && isHost)
        {
            switchedToLocationSelect = true;
            connectionMenu.SetActive(false);
            selectLocationMenu.SetActive(true);
        }
    }

    public void SetUI(string joinCode)
    {
        lobbyText.GetComponentInChildren<TMP_Text>().text = joinCode;
    }
}
