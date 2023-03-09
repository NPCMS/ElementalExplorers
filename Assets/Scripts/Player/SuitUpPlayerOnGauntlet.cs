using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// script attached to each gauntlet
public class SuitUpPlayerOnGauntlet : MonoBehaviour
{
    // when player touches gauntlet
    private void OnCollisionEnter(Collision collision)
    {
        // check its a player hand
        if (!(collision.gameObject.CompareTag("PlayerHand")))
            return;
        
        // get swap hands script on player and change models
        collision.gameObject.GetComponent<SuitUpPlayerOnPlayer>().SwitchToGauntlet();
        
        // disable gauntlet on table
        gameObject.SetActive(false);
    }
}
