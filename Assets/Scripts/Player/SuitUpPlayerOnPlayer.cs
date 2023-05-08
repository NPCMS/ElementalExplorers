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
        
        if (!GetComponentInParent<MultiPlayerWrapper>() != MultiPlayerWrapper.localPlayer) return;
        // play animation and sounds
        
        // Enable grappling movement
        GetComponent<GrappleController>().enabled = true;
    }
}
