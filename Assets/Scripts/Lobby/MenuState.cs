using System.Collections;
using Netcode.ConnectionManagement;
using Netcode.ConnectionManagement.ConnectionState;
using Netcode.SceneManagement;
using Unity.Netcode;
using UnityEngine;
using SessionPlayerData = Netcode.SessionManagement.SessionPlayerData;

public class MenuState : NetworkBehaviour
{
    [SerializeField] private LobbyMenuUI _lobbyMenuUI;
    [SerializeField] private MainMenuUI _mainMenuUI;
    [SerializeField] private GameObject _loadingUI; 
    [SerializeField] private GameObject _rejectedUI;

    private ConnectionManager _connectionManager;
    private Netcode.SessionManagement.SessionManager<SessionPlayerData> _sessionManager;

    void Awake()
    { 
       _connectionManager = FindObjectOfType<ConnectionManager>();
       _connectionManager.AddStateCallback += ChangedStateCallback;
       _sessionManager = Netcode.SessionManagement.SessionManager<SessionPlayerData>.Instance;
       _mainMenuUI.enabled = true;
    }

    public override void OnDestroy()
    {
        _connectionManager.AddStateCallback -= ChangedStateCallback;
        base.OnDestroy();
    }

    public void ChangedStateCallback(ConnectionState newState)
    {
        if (newState is HostingState || newState is ClientConnectedState)
        {
            _mainMenuUI.gameObject.SetActive(false);
            _lobbyMenuUI.gameObject.SetActive(true);
            _loadingUI.SetActive(false);
            _lobbyMenuUI.SetUI(_connectionManager.joinCode, false, false);
        } 
        else if (newState is ClientConnectingState || newState is StartingHostState)
        {
            _mainMenuUI.gameObject.SetActive(false);
            _lobbyMenuUI.gameObject.SetActive(false);
            _loadingUI.SetActive(true);
        }
        else if (newState is OfflineState)
        {
            if (_connectionManager.joinCodeRejection)
            {
                Debug.LogWarning("Join Code Rejected");
                StartCoroutine(ReturnAfterJoinFailed());
            }
            else
            {
                _mainMenuUI.gameObject.SetActive(true);
                _lobbyMenuUI.gameObject.SetActive(false);
                _loadingUI.SetActive(false);
            }
           
        }
    }

    private IEnumerator ReturnAfterJoinFailed()
    {
        _rejectedUI.SetActive(true);
        _lobbyMenuUI.gameObject.SetActive(false);
        _loadingUI.SetActive(false);

        yield return new WaitForSecondsRealtime(2);
        
        _rejectedUI.SetActive(false);
        _mainMenuUI.gameObject.SetActive(true);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestStartGameServerRpc()
    {
        if (_sessionManager.GetConnectedCount() == 2)
        {
            SceneLoaderWrapper.Instance.LoadScene("Precompute", useNetworkSceneManager: true);
        }
    }
}
