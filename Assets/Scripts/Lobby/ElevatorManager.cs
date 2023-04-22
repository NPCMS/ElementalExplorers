using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ElevatorManager : NetworkBehaviour
{
    [SerializeField] public Animator outerDoor;
    [SerializeField] public Animator innerDoor;
    [SerializeField] private GameObject invisibleWall;
    [SerializeField] private GameObject hologramWall;
    [SerializeField] private GameObject screen;
    [SerializeReference] private ElevatorTrigger elevator;
    [SerializeField] private bool isLeftElevator;

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

        // Disable Invisible Wall
        invisibleWall.SetActive(false);
    }

    public IEnumerator OpenDoors()
    {
        if (MultiPlayerWrapper.isGameHost)
        {
            if (isLeftElevator) AppearLocal();
            else BlockLocal();
        }
        else
        {
            if (isLeftElevator) BlockLocal();
            else AppearLocal();
        }
        
        // Open outer door
        outerDoor.SetBool("Open", true);

        yield return new WaitForSecondsRealtime(0.5f);
        
        // Open inner door
        innerDoor.SetBool("Open", true);
    }

    private bool IsPlaying(Animator anim)
    {
        return anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f;
    }

    public void AppearLocal()
    {
        
    }

    public void BlockLocal()
    {
        screen.GetComponentInChildren<TextMeshPro>().text = "USE OTHER ELEVATOR";
        hologramWall.SetActive(true);
    }
}
