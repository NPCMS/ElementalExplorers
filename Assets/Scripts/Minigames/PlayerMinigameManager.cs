using System.Collections.Generic;
using UnityEngine;

public class PlayerMinigameManager : MonoBehaviour
{
    public bool reachedMinigame;

    [SerializeReference] private List<MonoBehaviour> toEnableOnStart;
    [SerializeReference] private List<MonoBehaviour> toDisableOnStart;
    [SerializeReference] private Material standardSkybox;
    [SerializeReference] private Material minigameSkybox;

    [SerializeField] private AudioSource reachedMinigameSound;

    public void EnterMinigame()
    {
        reachedMinigame = true;
        
        reachedMinigameSound.Play();

        // play voice line
        StartCoroutine(SpeakerController.speakerController.PlayAudio("9 - Tree Encounter"));
        
        foreach (var behaviour in toEnableOnStart)
        {
            behaviour.enabled = true;
        }
        
        foreach (var behaviour in toDisableOnStart)
        {
            behaviour.enabled = false;
        }
        
        // change skybox on client
        RenderSettings.skybox = minigameSkybox;
        
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
        
        
        // change skybox on client
        RenderSettings.skybox = standardSkybox;
        
        // @alex @swanny
        // play voice line
        StartCoroutine(SpeakerController.speakerController.PlayAudio("10 - Tree defeated"));

        Debug.Log("Player left the minigame");
    }
}
