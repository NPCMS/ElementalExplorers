using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class InitPlayer : MonoBehaviour
{

    [SerializeReference] private GameObject hud;
    [SerializeReference] private List<GameObject> objectsEnabledOnStart;
    [SerializeReference] private List<MonoBehaviour> scriptsEnabledOnStart;

    public void Start()
    {
        // registers this players body with the race controller, this is for every player, not just the user's player
        var rcGameObject = GameObject.FindGameObjectWithTag("RaceController");
        var network = gameObject.GetComponentInParent<NetworkObject>();
        if (rcGameObject == null || network == null) return;
        var rc = rcGameObject.GetComponent<RaceController>();
        rc.playerBodies.Add(network.OwnerClientId, new RaceController.PlayerObjects(gameObject));
    }

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
        var rc = GameObject.FindGameObjectWithTag("RaceController").GetComponent<RaceController>();
        ulong userID = GetComponentInParent<NetworkObject>().OwnerClientId;
        try
        {
            var otherUid = rc.playerBodies.Keys.FirstOrDefault(uid => uid != userID);
            hud.GetComponent<HUDController>().TrackPlayer(rc.playerBodies[otherUid].body.transform);
        }
        catch (Exception)
        {
            Debug.Log("No player found to track");
        }
    }
}
