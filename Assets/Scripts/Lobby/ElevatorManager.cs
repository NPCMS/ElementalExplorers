using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class ElevatorManager : NetworkBehaviour
{
    [SerializeField] private Animator outerDoor;
    [SerializeField] private Animator innerDoor;
    [SerializeField] private GameObject invisibleWall;
    [SerializeField] private Animator movement; 

    [NonSerialized]
    public bool doorsClosed = true;

    // Declare and initialize a new List of GameObjects called currentCollisions.
    List <GameObject> currentCollisions = new();

    void OnTriggerEnter (Collider col) {
 
        // Add the GameObject collided with to the list.
        currentCollisions.Add (col.gameObject);
    }
 
    void OnTriggerExit (Collider col) {
 
        // Remove the GameObject collided with from the list.
        currentCollisions.Remove (col.gameObject);
    }
    
    public List<GameObject> GetPlayersInElevator()
    {
        return currentCollisions.FindAll(x => x.CompareTag("Player"));
    }

    public IEnumerator CloseDoors()
    {
        // Enable Invisible Wall
        invisibleWall.SetActive(true);
        
        // Close inner door
        innerDoor.SetBool("Open", false);

        yield return new WaitForSecondsRealtime(0.5f);
        
        // Close outer door
        outerDoor.SetBool("Open", false);
        
        yield return new WaitForSecondsRealtime(2);
        
        // Disable Invisible Wall
        invisibleWall.SetActive(false);

        doorsClosed = true;
    }

    public IEnumerator OpenDoors()
    {
        doorsClosed = false;
        
        // Open outer door
        outerDoor.SetBool("Open", true);

        yield return new WaitForSecondsRealtime(0.5f);
        
        // Open inner door
        innerDoor.SetBool("Open", true);
    }

    public void MoveDown()
    {
        if (doorsClosed)
        {
            movement.SetBool("Up", false);
        }

    }

    public void MoveUp()
    {
        if (doorsClosed)
        {
            movement.SetBool("Up", true);
        }
    }
}
