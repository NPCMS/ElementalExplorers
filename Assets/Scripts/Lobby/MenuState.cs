using System;
using System.Collections;
using System.Collections.Generic;
using Netcode.ConnectionManagement;
using Netcode.ConnectionManagement.ConnectionState;
using Netcode.SceneManagement;
using Unity.Netcode;
using VivoxUnity;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using ConnectionState = Netcode.ConnectionManagement.ConnectionState.ConnectionState;
using SessionPlayerData = Netcode.SessionManagement.SessionPlayerData;

public class MenuState : NetworkBehaviour
{
    [FormerlySerializedAs("_lobbyMenuUI")] [SerializeField] private LobbyMenuUI lobbyMenuUI;
    [FormerlySerializedAs("_mainMenuUI")] [SerializeField] private MainMenuUI mainMenuUI;
    [FormerlySerializedAs("_loadingUI")] [SerializeField] private GameObject loadingUI; 
    [FormerlySerializedAs("_rejectedUI")] [SerializeField] private GameObject rejectedUI;
    [SerializeField] private ElevatorManager leftElevator;
    [SerializeField] private ElevatorManager rightElevator;
    [SerializeField] private GameObject singlePlayer;

    private ConnectionManager connectionManager;
    private Netcode.SessionManagement.SessionManager<SessionPlayerData> sessionManager;

    private VivoxVoiceManager vivoxVoiceManager;

    private bool leftReadyToMove;
    private bool rightReadyToMove;
    private bool loadedTutorial;
    private bool initialDoorsOpen;
    private bool saidTeleport;

    private bool firstGameLoop = true;

    private SpeakerController speakerController;
    private const string SecondSceneName = "TutorialZone";
    private const string GameSceneName = "ASyncPipeline";
    

    void Awake()
    { 
       connectionManager = FindObjectOfType<ConnectionManager>();
       speakerController = FindObjectOfType<SpeakerController>();
       vivoxVoiceManager = FindObjectOfType<VivoxVoiceManager>();
       sessionManager = Netcode.SessionManagement.SessionManager<SessionPlayerData>.Instance;
       
       mainMenuUI.enabled = true;
       
       connectionManager.AddStateCallback = ChangedStateCallback;
       vivoxVoiceManager.OnUserLoggedInEvent += OnUserLoggedIn;
       SceneManager.sceneLoaded += StartMainGameCallback;
       
       // If AsyncPipeline is loaded: Teleport local player, UnloadAdditiveScenes
       // Else: Enable single player wrapper
       if (SceneManager.GetSceneByName("ASyncPipeline").IsValid())
       {
           if (MultiPlayerWrapper.isGameHost)
           {
               var p1Pos = GameObject.FindGameObjectWithTag("Player1RespawnPoint").transform.position;
               MultiPlayerWrapper.localPlayer.ResetPlayerPos();
               MultiPlayerWrapper.localPlayer.transform.position = p1Pos;
           }
           else
           {
               var p2Pos = GameObject.FindGameObjectWithTag("Player2RespawnPoint").transform.position;
               MultiPlayerWrapper.localPlayer.ResetPlayerPos();
               MultiPlayerWrapper.localPlayer.transform.position = p2Pos;
           }
           
           MultiPlayerWrapper.localPlayer.GetComponentInChildren<Rigidbody>().velocity = Vector3.zero;
           MultiPlayerWrapper.localPlayer.Reset();
           Invoke(nameof(CallUnloadAdditiveScenes), 1.5f); 
       }
       else
       {
           singlePlayer.SetActive(true);
       }
       
       Invoke(nameof(WelcomeToTheBridge), 5);
       
       // Fix logic when reconnecting to the lobby
       if (!(connectionManager.m_CurrentState is OfflineState))
       {
           firstGameLoop = false;
           ChangedStateCallback(connectionManager.m_CurrentState);
       }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= StartMainGameCallback;
        vivoxVoiceManager.OnUserLoggedInEvent -= OnUserLoggedIn;
        connectionManager.AddStateCallback = null;
        base.OnDestroy();
    }

    private void CallUnloadAdditiveScenes()
    {
        SceneLoaderWrapper.Instance.UnloadAdditiveScenes();
    }

    public void StartMainGameCallback(Scene scene, LoadSceneMode sceneMode)
    {
        if (scene.name == SecondSceneName)
        {
            StartCoroutine(StartMainGame()); // sorry. I hate this as well
        }
    }

    private IEnumerator StartMainGame()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        GameObject.FindGameObjectWithTag("TutorialExit").transform.position = Vector3.zero;
        yield return new WaitForSecondsRealtime(0.5f);
        SceneLoaderWrapper.Instance.LoadScene(GameSceneName, true, LoadSceneMode.Additive);
    }
    
    // Update is called once per frame
    void Update()
    {
        if (connectionManager.m_CurrentState is OfflineState || IsHost)
        {
            // When both players have joined open elevator doors
            if (lobbyMenuUI.locationSelected && !initialDoorsOpen)
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

            // If only the host is in the correct elevator then close the door
            if (leftElevatorPlayers.Count == 1 && hostInLeftElevator && !leftReadyToMove)
            {
                StartCoroutine(leftElevator.CloseDoors());
                leftReadyToMove = true;
            }

            // If only the client is in the correct elevator then close the door
            if (rightElevatorPlayers.Count == 1 && nonHostInRightElevator && !rightReadyToMove)
            {
                StartCoroutine(rightElevator.CloseDoors());
                rightReadyToMove = true;
            }

            // If both players are in the correct elevator then move them down
            if (leftReadyToMove && rightReadyToMove && !loadedTutorial)
            {
                // all players move themselves and all local elevators down 25m
                MoveLiftsDownClientRpc();
                
                loadedTutorial = true;
                
                // load the main game area / runs pipeline
                SceneLoaderWrapper.Instance.LoadScene(SecondSceneName, true, LoadSceneMode.Additive);
            }

            // Say teleport
            if (IsHost && sessionManager.GetConnectedCount() == 2 && !saidTeleport)
            {
                saidTeleport = true;
                Invoke(nameof(StartTeleport), 1);
            }
        }
    }

    public void ChangedStateCallback(ConnectionState newState)
    {
        if (newState is HostingState || newState is ClientConnectedState)
        {
            mainMenuUI.gameObject.SetActive(false);
            lobbyMenuUI.gameObject.SetActive(true);
            loadingUI.SetActive(false);
            lobbyMenuUI.SetUI(connectionManager.joinCode);
            lobbyMenuUI.isHost = newState is HostingState;
            if (newState is ClientConnectedState) Invoke(nameof(StartTeleport), 0.5f);
            if (firstGameLoop) vivoxVoiceManager.Login(NetworkManager.LocalClientId.ToString());
        } 
        else if (newState is ClientConnectingState || newState is StartingHostState)
        {
            mainMenuUI.gameObject.SetActive(false);
            lobbyMenuUI.gameObject.SetActive(false);
            loadingUI.SetActive(true);
        }
        else if (newState is OfflineState)
        {
            if (connectionManager.joinCodeRejection)
            {
                Debug.LogWarning("Join Code Rejected");
                StartCoroutine(ReturnAfterJoinFailed());
            }
            else
            {
                mainMenuUI.gameObject.SetActive(true);
                lobbyMenuUI.gameObject.SetActive(false);
                loadingUI.SetActive(false);
            }
            vivoxVoiceManager.Logout();
        }
    }

    private IEnumerator ReturnAfterJoinFailed()
    {
        rejectedUI.SetActive(true);
        lobbyMenuUI.gameObject.SetActive(false);
        loadingUI.SetActive(false);

        yield return new WaitForSecondsRealtime(2);
        
        rejectedUI.SetActive(false);
        mainMenuUI.gameObject.SetActive(true);
    }
    
    private void WelcomeToTheBridge()
    {
        // play voice line
        StartCoroutine(speakerController.PlayAudio("1 - Welcome"));
        // play voice line
        StartCoroutine(speakerController.PlayAudio("2 - Vignette"));
    }

    private void StartTeleport()
    {
        StartCoroutine(speakerController.PlayAudio("3 - Select a Location"));
    }
    
    private void GotoElevator()
    {
        // @Alex @Swanny not sure the way to differentiate elevators
        // play voice line
        StartCoroutine(speakerController.PlayAudio("4 - Left Elevator"));
        //StartCoroutine(speakerController.PlayAudio("4 - Right Elevator"));
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

    [ClientRpc]
    public void MoveLiftsDownClientRpc()
    {
        StartCoroutine(MoveLiftsRoutine());
    }

    public IEnumerator MoveLiftsRoutine()
    {
        yield return new WaitWhile(() => rightElevator.innerDoor.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f);
        yield return new WaitWhile(() => rightElevator.outerDoor.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f);
        yield return new WaitWhile(() => leftElevator.innerDoor.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f);
        yield return new WaitWhile(() => leftElevator.outerDoor.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f);
        
        yield return new WaitForSecondsRealtime(5);
        
        leftElevator.transform.position += Vector3.down * 35;
        rightElevator.transform.position += Vector3.down * 35;

        MultiPlayerWrapper.localPlayer.transform.position += Vector3.down * 35;

        yield return new WaitForSecondsRealtime(1);
        
        StartCoroutine(leftElevator.OpenDoors());
        StartCoroutine(rightElevator.OpenDoors());
    }
}
