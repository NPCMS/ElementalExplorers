using System.Collections;
using System.Collections.Generic;
using Netcode.SceneManagement;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TestManager : MonoBehaviour
{
    private bool loadedScene;
    private string secondSceneName = "SeamlessTestB";
    private string firstSceneName = "SeamlessTestA";

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A) && !loadedScene)
        {
            // Load Second Scene
            SceneLoaderWrapper.Instance.LoadScene(secondSceneName, false, LoadSceneMode.Additive);
            
           
        } else if (Input.GetKeyDown(KeyCode.S))
        { 
            Scene secondScene = SceneManager.GetSceneByName(secondSceneName);
            Scene firstScene = SceneManager.GetSceneByName(firstSceneName);
            MatchEntryToExit(firstScene, secondScene);
            MovePlayersToNewScene(secondScene);

            // Set the Active scene to scene B
            SceneManager.SetActiveScene(secondScene);

            loadedScene = true;
        } 
        else if (Input.GetKeyDown(KeyCode.D) && loadedScene)
        {
            SceneLoaderWrapper.Instance.UnloadAdditiveScenes();
        }
    }

    void MatchEntryToExit(Scene firstScene, Scene secondScene)
    {
        // Get the first scene exit point
        Vector3 exitPos = GetConnectionPoint(firstScene, false);
        
        // Get the second scene entry point
        Vector3 entryPos = GetConnectionPoint(secondScene, true);

        // Move Second Scene entry point to first scene exit point
        List<GameObject> secondObjects = new List<GameObject>();
        secondScene.GetRootGameObjects(secondObjects);
        foreach (var o in secondObjects)
        {
            o.transform.position = o.transform.position + exitPos - entryPos;
        }
    }

    Vector3 GetConnectionPoint(Scene scene, bool entry)
    {
        string pointName;
        if (entry)
        {
            pointName = "Entry";
        }
        else
        {
            pointName = "Exit";
        }
        
        // Get the first scene exit point
        List<GameObject> sceneObjects = new List<GameObject>();
        scene.GetRootGameObjects(sceneObjects);
        GameObject connection = sceneObjects.Find(x => x.name == pointName);
        return connection.transform.position;
    }

    void MovePlayersToNewScene(Scene secondScene)
    {
        // Move the Players to the other scene
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var player in players)
        {
            SceneManager.MoveGameObjectToScene(player, secondScene);
        }
    }
}
