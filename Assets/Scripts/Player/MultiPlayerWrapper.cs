using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using VivoxUnity;

public class MultiPlayerWrapper : NetworkBehaviour
{
    [SerializeField] private GameObject singlePlayer;
    private GrappleController[] grapples;
    private RaceController raceController;
    private SettingsMenu settingsMenu;
    [SerializeField] private bool toSinglePlayerOnDestroy = true;

    public static MultiPlayerWrapper localPlayer;
    public static bool isGameHost;

    [SerializeReference] private GameObject playerHead;
    [SerializeReference] private GameObject playerTorso;
    [SerializeReference] private GameObject leftHand;
    [SerializeReference] private GameObject rightHand;
    [SerializeReference] private GameObject leftGauntlet;
    [SerializeReference] private GameObject rightGauntlet;

    // as the player is in multiplayer it can either be a controlled by the user or not
    private void Start()
    {
        if (IsOwner) // if the player object is to be controlled by the user then enable all controls 
        {
            isGameHost = IsHost;
            var init = gameObject.GetComponentInChildren<InitPlayer>();
            init.StartPlayer();
            localPlayer = this;
            
            settingsMenu = FindObjectOfType<SettingsMenu>();
            //settingsMenu.AddVoiceChatCallback(SetVivoxMuteStatus);
        }
        else
        {
            playerHead.SetActive(true);
            playerTorso.SetActive(true);
        }

        // enable multiplayer transforms - this needs to be done for all players so they synchronise correctly
        foreach (var c in gameObject.GetComponentsInChildren<ClientNetworkTransform>())
        {
            c.enabled = true;
        }
    }

    private void OnDestroy()
    {
        //settingsMenu.RemoveVoiceChatCallback(SetVivoxMuteStatus);
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && toSinglePlayerOnDestroy)
        {
            Debug.Log("Instantiating single player");
            Vector3 offset = transform.Find("PlayerOffset").localPosition;
            Instantiate(singlePlayer, gameObject.transform.position + Vector3.up * 0.001f + offset, gameObject.transform.rotation);
            base.OnNetworkDespawn();
        }
    }

    public void ResetPlayerPos()
    {
        var init = gameObject.GetComponentInChildren<InitPlayer>();
        init.gameObject.transform.localPosition = Vector3.zero;
    }
    
    private void SetVivoxMuteStatus(bool muted)
    {
        VivoxVoiceManager vivoxVoiceManager = FindObjectOfType<VivoxVoiceManager>();
        ChannelId channelId = vivoxVoiceManager.TransmittingSession.Channel;
        IChannelSession channelSession = vivoxVoiceManager.LoginSession.GetChannelSession(channelId);
        IReadOnlyDictionary<string, IParticipant> participants = channelSession.Participants;
        foreach (IParticipant participant in participants)
        {
            participant.LocalMute = muted;
        }
    }
    
    public void Reset()
    {
        // Turn off gauntlet models
        GameObject[] playerWrappers = GameObject.FindGameObjectsWithTag("PlayerWrapper");
        foreach (var playerWrapper in playerWrappers)
        {
            MultiPlayerWrapper wrapper = playerWrapper.GetComponent<MultiPlayerWrapper>();

            wrapper.leftHand.SetActive(true);
            wrapper.rightHand.SetActive(true);
            wrapper.leftGauntlet.SetActive(false);
            wrapper.rightGauntlet.SetActive(false);
        }

        // Turn off grapple controller
        foreach (GrappleController controller in GetComponentsInChildren<GrappleController>())
        {
            controller.enabled = false;
        }
    }
}
