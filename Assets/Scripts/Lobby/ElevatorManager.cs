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

    public bool leftGauntletOn;
    public bool rightGauntletOn;
    private bool bothGauntletsOn; // set through server rpc. Can't be done using (leftGauntletOn && rightGauntletOn)
    private NetworkVariable<bool> elevatorDown = new();

    private static bool gauntletVoiceLinePlayed;

    private void Start()
    {
        gauntletVoiceLinePlayed = false;
    }

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

        InstructGauntletsClientRpc();

        yield return new WaitForSecondsRealtime(2);
        
        // Disable Invisible Wall
        invisibleWall.SetActive(false);
        if (IsHost)
        {
            elevatorDown.Value = true;
        }
    }

    public IEnumerator OpenDoors()
    {
        SetupBlockingWallsClientRpc();

        if (elevatorDown.Value) yield return new WaitUntil(() => bothGauntletsOn);
        
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

    [ClientRpc]
    private void SetupBlockingWallsClientRpc()
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
    }

    [ClientRpc]
    private void InstructGauntletsClientRpc()
    {
        screen.GetComponentInChildren<TextMeshPro>().text = "PUT ON THE GAUNTLETS";
        
        // play voice line
        if (gauntletVoiceLinePlayed) return;
        
        StartCoroutine(SpeakerController.speakerController.PlayAudio("5 - Gauntlets"));
        gauntletVoiceLinePlayed = true;

    }

    [ServerRpc(RequireOwnership = false)]
    public void BothGauntletsOnServerRpc()
    {
        bothGauntletsOn = true;
    }
    
    private void AppearLocal()
    {
        switch (isLeftElevator)
        {
            case true when MultiPlayerWrapper.isGameHost:
                StartCoroutine(SpeakerController.speakerController.PlayAudio("4 - Left Elevator"));
                break;
            case false when !MultiPlayerWrapper.isGameHost:
                StartCoroutine(SpeakerController.speakerController.PlayAudio("4 - Right Elevator"));
                break;
        }
    }

    private void BlockLocal()
    {
        screen.GetComponentInChildren<TextMeshPro>().text = "USE OTHER ELEVATOR";
        hologramWall.SetActive(true);
    }
}
