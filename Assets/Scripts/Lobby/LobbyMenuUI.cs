using System.Linq;
using Netcode.ConnectionManagement;
using Netcode.SessionManagement;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class LobbyMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject selectLocationBtn;
    [SerializeField] private GameObject lobbyText;
    [SerializeField] private GameObject selectLocationMenu; 
    [SerializeField] private GameObject connectionMenu;
    [SerializeField] private GameObject selectedLocationUI;
    [SerializeField] private GameObject map;
    [SerializeField] private TileInfo tileInfo;
    [SerializeField] private GameObject scoreScreen;

    public bool locationSelected;
    
    private SessionManager<SessionPlayerData> sessionManager;
    private bool switchedToLocationSelect;
    public bool notFirstGame;
    public bool isHost;

    private void Start()
    {
        selectLocationBtn.GetComponent<UIInteraction>().AddCallback((RaycastHit hit, SteamInputCore.Button button) =>
        {
            Mapbox mapbox = map.GetComponent<Mapbox>();
            Debug.Log("Select location button pressed");
            if (mapbox.StartSelected)
            {
                Debug.Log("Start Location selected");
                locationSelected = true;
                
                RPCManager.Instance.CallSetPipelineCoords(tileInfo.GetTiles().ToArray(), tileInfo.selectedCoords);
                selectLocationMenu.SetActive(false);
                selectedLocationUI.SetActive(true);
            }
        });
        sessionManager = SessionManager<SessionPlayerData>.Instance;

        if (notFirstGame)
        {
            scoreScreen.SetActive(true);
            connectionMenu.SetActive(false);
            selectLocationMenu.SetActive(false);
        }
    }

    public void RemoveScoreScreen()
    {
        connectionMenu.SetActive(!isHost);
        selectLocationMenu.SetActive(isHost);
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
        if (sessionManager.GetConnectedCount() == 2 && !switchedToLocationSelect && isHost && !notFirstGame)
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
