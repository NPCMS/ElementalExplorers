using System;
using System.Collections.Generic;
using System.Linq;
using Netcode.SessionManagement;
using Unity.Netcode;
using UnityEngine;

public class InitPlayer : MonoBehaviour
{

    [SerializeReference] private GameObject hud;
    [SerializeReference] private List<GameObject> objectsEnabledOnStart;
    [SerializeReference] private List<MonoBehaviour> scriptsEnabledOnStart;

    // enables all controls / objects for the player so that the user can control this player 
    public void StartPlayer()
    {
        foreach (var c in objectsEnabledOnStart)
        {
            c.SetActive(true);
        }

        foreach (var c in scriptsEnabledOnStart)
        {
            c.enabled = true;
        }
    }

    // called to start the race for the player. This is called by the multiplayer wrapper on load at the moment
    public void StartRace()
    {
        var playerRaceController = gameObject.GetComponentInChildren<PlayerRaceController>();
        playerRaceController.enabled = true;
        playerRaceController.raceStarted = true;
        Invoke(nameof(ConnectPlayerTracker), 2); // delay while waiting for players, ideally players won't start the race on load 
        hud.SetActive(true);
    }

    // function to set up player tracking with hud. This is scuffed but required as players load in a different times.
    // ideally the race will be started when both players are loaded and this can be tidied
    private void ConnectPlayerTracker()
    {
        ulong userID = GetComponentInParent<NetworkObject>().OwnerClientId;
        try
        {
            SessionManager<SessionPlayerData> sm = SessionManager<SessionPlayerData>.Instance;
            var otherUid = sm.GetConnectedPlayerDataServerRpc().Keys.FirstOrDefault(uid => uid != userID);
            Transform otherPlayer = sm.GetPlayerData(otherUid).Value.SpawnedPlayer.transform.GetChild(0).Find("Body");
            hud.GetComponent<HUDController>().TrackPlayer(otherPlayer);
        }
        catch (Exception)
        {
            Debug.Log("No player found to track");
        }
    }
}
