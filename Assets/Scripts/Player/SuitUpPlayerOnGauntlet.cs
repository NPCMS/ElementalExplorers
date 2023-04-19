using UnityEngine;

// script attached to each gauntlet
public class SuitUpPlayerOnGauntlet : MonoBehaviour
{
    [SerializeField] private GameObject gauntletRim; 
    
    // when player touches gauntlet
    private void OnTriggerEnter(Collider collision)
    {
        // check its a player hand
        if (!collision.gameObject.CompareTag("PlayerHand"))
            return;
        
        // get swap hands script on player and change models
        collision.gameObject.GetComponentInParent<SuitUpPlayerOnPlayer>().SwitchToGauntlet();
        
        // disable gauntlet on table
        gameObject.SetActive(false);
        gauntletRim.SetActive(false);
    }
}
