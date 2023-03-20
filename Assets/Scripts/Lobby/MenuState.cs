using System.Collections;
using System.Collections.Generic;
using Netcode.ConnectionManagement;
using Netcode.ConnectionManagement.ConnectionState;
using Netcode.SceneManagement;
using Unity.Netcode;
using VivoxUnity;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using ConnectionState = Netcode.ConnectionManagement.ConnectionState.ConnectionState;
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

    private VivoxVoiceManager vivoxVoiceManager;

    private bool leftReadyToMove;
    private bool rightReadyToMove;
    private bool loadedTutorial;
    private bool initialDoorsOpen;
    private bool saidTeleport;
    
    void Awake()
    { 
       _connectionManager = FindObjectOfType<ConnectionManager>();
       _connectionManager.AddStateCallback = ChangedStateCallback;
       _sessionManager = Netcode.SessionManagement.SessionManager<SessionPlayerData>.Instance;
       _mainMenuUI.enabled = true;

       vivoxVoiceManager = FindObjectOfType<VivoxVoiceManager>();
       
       vivoxVoiceManager.OnUserLoggedInEvent += OnUserLoggedIn;
       
       Invoke(nameof(WelcomeToTheBridge), 5);
    }

    private void WelcomeToTheBridge()
    {
        FindObjectOfType<SpeakerController>().PlayAudio("Welcome to bridge");
    }

    private void StartTeleport()
    {
        FindObjectOfType<SpeakerController>().PlayAudio("Start teleport");
    }
    
    private void GotoElevator()
    {
        FindObjectOfType<SpeakerController>().PlayAudio("Goto elevator");
    }
    
    private string secondSceneName = "TutorialZone";

    // Update is called once per frame
    void Update()
    {
        if (_connectionManager.m_CurrentState is OfflineState || IsHost)
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                SceneLoaderWrapper.Instance.LoadScene(secondSceneName, true, LoadSceneMode.Additive);
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
                StartCoroutine(leftElevator.MoveDown());
                StartCoroutine(rightElevator.MoveDown());
            }

            if (_sessionManager.GetConnectedCount() == 2 && !initialDoorsOpen)
            {
                initialDoorsOpen = true;
                Invoke(nameof(GotoElevator), 2);
                StartCoroutine(leftElevator.OpenDoors());
                StartCoroutine(rightElevator.OpenDoors());
            }

            List<GameObject> leftElevatorPlayers = leftElevator.GetPlayersInElevator();
            bool hostInLeftElevator = false;
            foreach (GameObject player in leftElevatorPlayers)
            {
                ulong id = player.GetComponentInParent<NetworkObject>().OwnerClientId;
                if (id == 0)
                {
                    hostInLeftElevator = true;
                }
            }

            List<GameObject> rightElevatorPlayers = rightElevator.GetPlayersInElevator();
            bool nonHostInRightElevator = false;
            foreach (GameObject player in rightElevatorPlayers)
            {
                ulong id = player.GetComponentInParent<NetworkObject>().OwnerClientId;
                if (id != 0)
                {
                    nonHostInRightElevator = true;
                }
            }

            if (leftElevatorPlayers.Count == 1 && hostInLeftElevator && !leftReadyToMove)
            {
                StartCoroutine(leftElevator.CloseDoors());
                leftReadyToMove = true;
            }

            if (rightElevatorPlayers.Count == 1 && nonHostInRightElevator && !rightReadyToMove)
            {
                StartCoroutine(rightElevator.CloseDoors());
                rightReadyToMove = true;
            }

            if (leftReadyToMove && rightReadyToMove && !loadedTutorial)
            {
                StartCoroutine(leftElevator.MoveDown());
                StartCoroutine(rightElevator.MoveDown());
                loadedTutorial = true;
                SceneLoaderWrapper.Instance.LoadScene(secondSceneName, true, LoadSceneMode.Additive);
            }

            if (IsHost && _sessionManager.GetConnectedCount() == 2 && !saidTeleport)
            {
                saidTeleport = true;
                Invoke(nameof(StartTeleport), 1);
            }
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
            _lobbyMenuUI.SetUI(_connectionManager.joinCode);
            if (newState is ClientConnectedState)
            {
                Invoke(nameof(StartTeleport), 1);
            }
            
            vivoxVoiceManager.Login(NetworkManager.LocalClientId.ToString());
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
            vivoxVoiceManager.Logout();
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
            
            // Load next Scene
            SceneLoaderWrapper.Instance.LoadScene(secondSceneName, true, LoadSceneMode.Additive);
        }
    }
    
    [ClientRpc]
    public void PlayersReadyClientRpc()
    {
        // Open Doors
        StartCoroutine(leftElevator.OpenDoors());
        StartCoroutine(rightElevator.OpenDoors());
    }
    
    private void OnUserLoggedIn()
    {
        string lobbyChannelName = "lobbyChannel";
        var lobbyChannel = vivoxVoiceManager.ActiveChannels.FirstOrDefault(ac => ac.Channel.Name == lobbyChannelName);
        if ((vivoxVoiceManager && vivoxVoiceManager.ActiveChannels.Count == 0) 
            || lobbyChannel == null)
        {
            vivoxVoiceManager.JoinChannel(lobbyChannelName, ChannelType.NonPositional, VivoxVoiceManager.ChatCapability.TextAndAudio);
        }
        else
        {
            if (lobbyChannel.AudioState == VivoxUnity.ConnectionState.Disconnected)
            {
                // Ask for hosts since we're already in the channel and part added won't be triggered.

                lobbyChannel.BeginSetAudioConnected(true, true, ar =>
                {
                    Debug.Log("Now transmitting into lobby channel");
                });
            }

        }
    }
}
