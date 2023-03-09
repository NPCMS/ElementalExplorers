using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class ElevatorManager : MonoBehaviour
{
    [SerializeField] private Animator outerDoor;
    [SerializeField] private Animator innerDoor;
    [SerializeField] private GameObject invisibleWall;

    // Declare and initialize a new List of GameObjects called currentCollisions.
    List <GameObject> currentCollisions = new();
     
    void OnCollisionEnter (Collision col) {
 
        // Add the GameObject collided with to the list.
        currentCollisions.Add (col.gameObject);
 
        // Print the entire list to the console.
        foreach (GameObject gObject in currentCollisions) {
            print (gObject.name);
        }
    }
 
    void OnCollisionExit (Collision col) {
 
        // Remove the GameObject collided with from the list.
        currentCollisions.Remove (col.gameObject);
 
        // Print the entire list to the console.
        foreach (GameObject gObject in currentCollisions) {
            print (gObject.name);
        }
    }
    
    public List<GameObject> getPlayersInElevator()
    {
        return currentCollisions.FindAll(x => x.CompareTag("Player"));
    }

    public IEnumerator CloseDoors()
    {
        // Enable Invisible Wall
        invisibleWall.SetActive(true);
        
        // Close inner door
        innerDoor.SetTrigger("Close");

        yield return new WaitForSecondsRealtime(0.5f);
        
        // Close outer door
        outerDoor.SetTrigger("Close");
        
        yield return new WaitForSecondsRealtime(2);
        
        // Disable Invisible Wall
        invisibleWall.SetActive(false);
    }

    public IEnumerator OpenDoors()
    {
        // Open outer door
        outerDoor.SetTrigger("Open");

        yield return new WaitForSecondsRealtime(0.5f);
        
        // Open inner door
        innerDoor.SetTrigger("Open");
    }
}
