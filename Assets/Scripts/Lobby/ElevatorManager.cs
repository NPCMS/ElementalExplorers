using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ElevatorManager : NetworkBehaviour
{
    [SerializeField] private Animator outerDoor;
    [SerializeField] private Animator innerDoor;
    [SerializeField] private GameObject invisibleWall;
    [SerializeField] private Animator movement;
    [SerializeField] private GameObject screen;
    [SerializeReference] private ElevatorTrigger elevator;
    [NonSerialized]
    public bool doorsClosed = true;
    public bool elevatorDown;
    
    public List<GameObject> GetPlayersInElevator()
    {
        return elevator.GetPlayersInElevator();
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
        gameObject.transform.parent.position += Vector3.down * 25;
        Debug.Log(player.name + " " + player.transform.root.name + " " + NetworkManager.LocalClientId);
        player.position += Vector3.down * 25;
    }
}
