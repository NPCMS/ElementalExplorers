using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using UnityEngine;

public class ElevatorManager : NetworkBehaviour
{
    [SerializeField] private Animator outerDoor;
    [SerializeField] private Animator innerDoor;
    [SerializeField] private GameObject invisibleWall;
    [SerializeField] private Animator movement;
    [SerializeField] private GameObject screen;
    [NonSerialized]
    public bool doorsClosed = true;
    public bool elevatorDown;

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

        doorsClosed = true;
        
        // Disable Invisible Wall
        invisibleWall.SetActive(false);
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

    public IEnumerator MoveDown()
    {
        yield return new WaitWhile(() => IsPlaying(innerDoor));
        yield return new WaitWhile(() => IsPlaying(outerDoor));

        TeleportPlayersServerRpc();
        
        yield return new WaitForSecondsRealtime(5);

        StartCoroutine(OpenDoors());

        elevatorDown = true;
    }

    public IEnumerator MoveUp()
    {
        yield return new WaitWhile(() => IsPlaying(innerDoor));
        yield return new WaitWhile(() => IsPlaying(outerDoor));
        
        movement.SetBool("Up", true);
    }

    public void SetEnterElevator(bool active)
    {
        screen.SetActive(active);
    }

    private bool IsPlaying(Animator anim)
    {
        return anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f;
    }

    [ServerRpc(RequireOwnership = false)]
    private void TeleportPlayersServerRpc()
    {
        TeleportPlayersClientRpc();
    }

    [ClientRpc]
    private void TeleportPlayersClientRpc()
    {
        Transform player = GetPlayersInElevator()[0].transform.parent;
        gameObject.transform.position += Vector3.down * 25;
        Debug.Log(player.name + " " + player.transform.root.name);
        player.Find("VRCamera").position += Vector3.down * 25;
        player.Find("Body").position += Vector3.down * 25;
        player.Find("LeftHand").position += Vector3.down * 25;
        player.Find("RightHand").position += Vector3.down * 25;
    }
}
