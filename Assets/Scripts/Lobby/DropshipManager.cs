using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DropshipManager : NetworkBehaviour
{
    [SerializeField] private Animator innerDoor;
    [SerializeField] private GameObject invisibleWall;
    [SerializeField] private Animator movement;
    [SerializeField] private AnimationClip moveDown;
    [SerializeField] private AnimationClip closeDoor;

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
    
    public List<GameObject> GetPlayersInDropship()
    {
        return currentCollisions.FindAll(x => x.CompareTag("Player"));
    }

    public void OpenDoors()
    {
        doorsClosed = false;

        // Open inner door
        innerDoor.SetBool("Open", true);
    }

    public IEnumerator Drop()
    {
        doorsClosed = true;
        
        // Enable Invisible Wall
        invisibleWall.SetActive(true);
        
        // Close inner door
        innerDoor.SetBool("Open", false);
        
        
        yield return new WaitForSecondsRealtime(closeDoor.length);
        
        // Disable Invisible Wall
        invisibleWall.SetActive(false);
        
        movement.SetBool("Up", false);
        
        yield return new WaitForSecondsRealtime(moveDown.length);

        OpenDoors();
    }
}
