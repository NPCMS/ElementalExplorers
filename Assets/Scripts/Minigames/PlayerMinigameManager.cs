using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMinigameManager : MonoBehaviour
{
    public bool reachedMinigame;

    [SerializeReference] private List<MonoBehaviour> toEnableOnStart;
    [SerializeReference] private List<MonoBehaviour> toDisableOnStart;

    [SerializeField] private AudioSource reachedMinigameSound;

    private SpeakerController speakerController;

    private void Awake()
    {
        speakerController = FindObjectOfType<SpeakerController>();
    }

    public void EnterMinigame()
    {
        reachedMinigame = true;
        
        reachedMinigameSound.Play();

        // play voice line
        StartCoroutine(speakerController.PlayAudio("9 - Tree Encounter"));
        
        foreach (var behaviour in toEnableOnStart)
        {
            behaviour.enabled = true;
        }
        
        foreach (var behaviour in toDisableOnStart)
        {
            behaviour.enabled = false;
        }
        
        Debug.Log("Player entered the minigame");
    }
    
    public void LeaveMinigame()
    {
        reachedMinigame = false;
        
        foreach (var behaviour in toEnableOnStart)
        {
            behaviour.enabled = false;
        }
        
        foreach (var behaviour in toDisableOnStart)
        {
            behaviour.enabled = true;
        }
        
        // @alex @swanny
        // play voice line
        StartCoroutine(speakerController.PlayAudio("10 - Tree defeated"));

        Debug.Log("Player left the minigame");
    }
}
