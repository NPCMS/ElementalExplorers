using System.Collections.Generic;
using UnityEngine;

public class ElevatorTrigger : MonoBehaviour
{
    // Declare and initialize a new List of GameObjects called currentCollisions.
    List <GameObject> currentCollisions = new();
    void OnTriggerEnter (Collider col) {
        // Add the GameObject collided with to the list.
        currentCollisions.Add(col.gameObject);
    }
 
    void OnTriggerExit (Collider col) {
        // Remove the GameObject collided with from the list.
        currentCollisions.Remove(col.gameObject);
    }
    
    public List<GameObject> GetPlayersInElevator()
    {
        return currentCollisions.FindAll(x => x.CompareTag("Player"));
    }
}
