using System;
using System.Collections;
using System.Collections.Generic;
using Netcode.ConnectionManagement;
using Netcode.ConnectionManagement.ConnectionState;
using Netcode.SceneManagement;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using SessionPlayerData = Netcode.SessionManagement.SessionPlayerData;

public class MenuState : NetworkBehaviour
{
    [SerializeField] private LobbyMenuUI _lobbyMenuUI;
    [SerializeField] private MainMenuUI _mainMenuUI;
    [SerializeField] private GameObject _loadingUI; 
    [SerializeField] private GameObject _rejectedUI;
    [SerializeField] private ElevatorManager leftElevator;
    [SerializeField] private ElevatorManager rightElevator;

    private ConnectionManager _connectionManager;
    private Netcode.SessionManagement.SessionManager<SessionPlayerData> _sessionManager;

    void Awake()
    { 
       _connectionManager = FindObjectOfType<ConnectionManager>();
       _connectionManager.AddStateCallback = ChangedStateCallback;
       _sessionManager = Netcode.SessionManagement.SessionManager<SessionPlayerData>.Instance;
       _mainMenuUI.enabled = true;
    }
    
    private string secondSceneName = "TutorialZone";

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            SceneLoaderWrapper.Instance.LoadScene(secondSceneName, false, LoadSceneMode.Additive);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            SceneLoaderWrapper.Instance.UnloadAdditiveScenes();
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            Debug.Log("Elevator Up");
            leftElevator.MoveUp();
            rightElevator.MoveUp();
        }
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            Debug.Log("Elevator Down");
            leftElevator.MoveDown();
            rightElevator.MoveDown();
        }

        List<GameObject> leftElevatorPlayers = leftElevator.GetPlayersInElevator();
        bool hostInLeftElevator = false;
        foreach (GameObject player in leftElevatorPlayers)
        {
            ulong id = player.GetComponent<NetworkObject>().OwnerClientId;
            if (id == 0)
            {
                hostInLeftElevator = true;
            }
        }
        List<GameObject> rightElevatorPlayers = rightElevator.GetPlayersInElevator();
        bool nonHostInRightElevator = false;
        foreach (GameObject player in rightElevatorPlayers)
        {
            ulong id = player.GetComponent<NetworkObject>().OwnerClientId;
            if (id != 0)
            {
                nonHostInRightElevator = true;
            }
        }
        if (leftElevatorPlayers.Count == 1 && hostInLeftElevator)
        {
            StartCoroutine(leftElevator.CloseDoors());
        }
        
        if (rightElevatorPlayers.Count == 1 && nonHostInRightElevator)
        {
            StartCoroutine(rightElevator.CloseDoors());
        }
    }

    public override void OnDestroy()
    {
        _connectionManager.AddStateCallback = null;
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
    
    [ServerRpc(RequireOwnership = false)]
    public void RequestPlayersReadyServerRpc()
    {
        if (_sessionManager.GetConnectedCount() == 2)
        {
            PlayersReadyClientRpc();
        }
    }
    
    [ClientRpc]
    public void PlayersReadyClientRpc()
    {
        // Open Doors
        leftElevator.OpenDoors();
        rightElevator.OpenDoors();
    }
}
