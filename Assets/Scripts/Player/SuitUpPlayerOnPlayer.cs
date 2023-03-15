 using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuitUpPlayerOnPlayer : MonoBehaviour
{
    [SerializeReference] private GameObject oldHand;
    [SerializeReference] private GameObject gauntlet;
    
    public void SwitchToGauntlet()
    {
        // enable gauntlet
        oldHand.SetActive(false);
        gauntlet.SetActive(true);
        
        // play animation and sounds
        
        // Enable grappling movement
        GetComponent<HandGrappleAndSwinging>().enabled = true;
    }
}
