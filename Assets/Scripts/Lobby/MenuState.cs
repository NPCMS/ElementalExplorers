using System.Collections;
using System.Collections.Generic;
using Netcode.ConnectionManagement;
using Netcode.ConnectionManagement.ConnectionState;
using Netcode.SceneManagement;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using SessionPlayerData = Netcode.SessionManagement.SessionPlayerData;

public class MenuState : NetworkBehaviour
{
    [FormerlySerializedAs("_lobbyMenuUI")] [SerializeField] private LobbyMenuUI lobbyMenuUI;
    [FormerlySerializedAs("_mainMenuUI")] [SerializeField] private MainMenuUI mainMenuUI;
    [FormerlySerializedAs("_loadingUI")] [SerializeField] private GameObject loadingUI; 
    [FormerlySerializedAs("_rejectedUI")] [SerializeField] private GameObject rejectedUI;
    [SerializeField] private ElevatorManager leftElevator;
    [SerializeField] private ElevatorManager rightElevator;

    private ConnectionManager connectionManager;
    private Netcode.SessionManagement.SessionManager<SessionPlayerData> sessionManager;

    private bool leftReadyToMove;
    private bool rightReadyToMove;
    private bool loadedTutorial;
    private bool initialDoorsOpen;
    private bool saidTeleport;

    private SpeakerController speakerController;
    private const string SecondSceneName = "TutorialZone";

    void Awake()
    { 
       connectionManager = FindObjectOfType<ConnectionManager>();
       connectionManager.AddStateCallback = ChangedStateCallback;
       sessionManager = Netcode.SessionManagement.SessionManager<SessionPlayerData>.Instance;
       mainMenuUI.enabled = true;

       speakerController = FindObjectOfType<SpeakerController>();

       Invoke(nameof(WelcomeToTheBridge), 5);
    }

    // Update is called once per frame
    void Update()
    {
        if (connectionManager.m_CurrentState is OfflineState || IsHost)
        {
            // When both players have joined open elevator doors
            if (sessionManager.GetConnectedCount() == 2 && !initialDoorsOpen)
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
                StartCoroutine(leftElevator.MoveDown());
                StartCoroutine(rightElevator.MoveDown());
                loadedTutorial = true;
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

    public override void OnDestroy()
    {
        connectionManager.AddStateCallback = null;
        base.OnDestroy();
    }

    public void ChangedStateCallback(ConnectionState newState)
    {
        if (newState is HostingState || newState is ClientConnectedState)
        {
            mainMenuUI.gameObject.SetActive(false);
            lobbyMenuUI.gameObject.SetActive(true);
            loadingUI.SetActive(false);
            lobbyMenuUI.SetUI(connectionManager.joinCode);
            if (newState is ClientConnectedState)
            {
                Invoke(nameof(StartTeleport), 0.5f);
            }
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
        StartCoroutine(speakerController.PlayAudio("Welcome to bridge"));
    }

    private void StartTeleport()
    {
        StartCoroutine(speakerController.PlayAudio("Start teleport"));
    }
    
    private void GotoElevator()
    {
        StartCoroutine(speakerController.PlayAudio("Goto elevator"));
    }
}
