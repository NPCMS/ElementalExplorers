using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMinigameManager : MonoBehaviour
{
    public bool reachedMinigame;

    [SerializeReference] private List<MonoBehaviour> toEnableOnStart;
    [SerializeReference] private List<MonoBehaviour> toDisableOnStart;
    [SerializeReference] private Material standardSkybox;
    [SerializeReference] private Material minigameSkybox;
    private GameObject clouds; 
    
    [SerializeField] private AudioSource reachedMinigameSound;

    private void Awake()
    {
        SceneManager.sceneLoaded += OnEnterAsync;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnEnterAsync;
    }

    private void OnEnterAsync(Scene secondScene, LoadSceneMode loadSceneMode)
    {
        if (secondScene.name == "ASyncPipeline")
        {
            clouds = GameObject.FindGameObjectWithTag("Clouds");
        } 
    }

    public void EnterMinigame()
    {
        reachedMinigame = true;
        
        // Turn off clouds
        clouds.SetActive(false);
        
        reachedMinigameSound.Play();

        // play voice line
        StartCoroutine(SpeakerController.speakerController.PlayAudioNow("9 - Tree Encounter"));
        
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
        
        // Turn on clouds
        clouds.SetActive(true);
        
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

        Debug.Log("Player left the minigame");
    }
}
