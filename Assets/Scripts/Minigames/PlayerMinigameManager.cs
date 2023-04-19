using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMinigameManager : MonoBehaviour
{
    public bool reachedMinigame;

    public void EnterMinigame()
    {
        reachedMinigame = true;
        // todo disable movement, enable minigame controls
        Debug.Log("Player entered the minigame");
    }
}
