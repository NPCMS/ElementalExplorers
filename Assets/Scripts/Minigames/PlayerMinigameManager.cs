using System.Collections.Generic;
using UnityEngine;

public class PlayerMinigameManager : MonoBehaviour
{
    public bool reachedMinigame;

    [SerializeReference] private List<MonoBehaviour> toEnableOnStart;
    [SerializeReference] private List<MonoBehaviour> toDisableOnStart;

    [SerializeField] private AudioSource reachedMinigameSound;

    public void EnterMinigame()
    {
        reachedMinigame = true;
        
        reachedMinigameSound.Play();

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
        
        Debug.Log("Player left the minigame");
    }
}
