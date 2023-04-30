using UnityEngine;

// script attached to each gauntlet
public class SuitUpPlayerOnGauntlet : MonoBehaviour
{
    [SerializeField] private GameObject gauntletRim;
    [SerializeReference] private ElevatorManager elevator;
    [SerializeField] private bool isLeft;
    
    // when player touches gauntlet
    private void OnTriggerEnter(Collider collision)
    {
        // check its a player hand
        if (!collision.gameObject.CompareTag("PlayerHand"))
            return;
        
        // get swap hands script on player and change models
        collision.gameObject.GetComponentInParent<SuitUpPlayerOnPlayer>().SwitchToGauntlet();

        if (elevator == null)
        {
            Debug.LogWarning("Elevator reference not set");
        }
        else
        {
            if (isLeft) elevator.leftGauntletOn = true;
            else elevator.rightGauntletOn = true;
            if (elevator.leftGauntletOn && elevator.rightGauntletOn) elevator.BothGauntletsOn();
        }
        
        // disable gauntlet on table
        gameObject.SetActive(false);
        gauntletRim.SetActive(false);
    }
}
