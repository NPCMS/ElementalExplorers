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
        GetComponentInChildren<Rigidbody>().transform.position = Vector3.zero; // we are not really sure why this works but it does
    }

    // called to start the race for the player. This is called by the multiplayer wrapper on load at the moment
    public void StartRace()
    {
        var playerRaceController = gameObject.GetComponentInChildren<PlayerRaceController>();
        playerRaceController.enabled = true;
        playerRaceController.raceStarted = true;
        hud.SetActive(true);
    }
}
