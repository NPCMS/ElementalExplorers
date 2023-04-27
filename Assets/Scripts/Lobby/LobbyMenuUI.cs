using System.Collections.Generic;
using Netcode.ConnectionManagement;
using Netcode.SessionManagement;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.WSA;

public class LobbyMenuUI : NetworkBehaviour
{
    [SerializeField] private GameObject leaveLobbyBtn;
    [SerializeField] private GameObject selectLocationBtn;
    [SerializeField] private GameObject lobbyText;
    [SerializeField] private GameObject selectLocationMenu; 
    [SerializeField] private GameObject connectionMenu;
    [SerializeField] private GameObject map;
    [SerializeField] private TileInfo tileInfo;
    
    private SessionManager<SessionPlayerData> sessionManager;
    private bool switchedToLocationSelect;
    public bool isHost;

    private void Start()
    {
        leaveLobbyBtn.GetComponent<UIInteraction>().AddCallback((RaycastHit hit, SteamInputCore.Button button) =>
        {
            ConnectionManager connectionManager = FindObjectOfType<ConnectionManager>();
            connectionManager.RequestShutdown();
        });
        
        selectLocationBtn.GetComponent<UIInteraction>().AddCallback((RaycastHit hit, SteamInputCore.Button button) =>
        {
            Mapbox mapbox = map.GetComponent<Mapbox>();
            if (mapbox.StartSelected)
            {
                SetPipelineCoordsClientRpc(tileInfo.tiles, tileInfo.selectedCoords);
            }
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

    [ClientRpc]
    private void SetPipelineCoordsClientRpc(List<Vector2Int> tiles, Vector2 selectedCoords)
    {
        tileInfo.tiles = tiles;
        tileInfo.selectedCoords = selectedCoords;
    }
}
