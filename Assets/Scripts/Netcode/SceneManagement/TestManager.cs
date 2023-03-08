using System.Collections;
using System.Collections.Generic;
using Netcode.SceneManagement;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TestManager : MonoBehaviour
{
    private string secondSceneName = "SeamlessTestB";

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            SceneLoaderWrapper.Instance.LoadScene(secondSceneName, false, LoadSceneMode.Additive);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            SceneLoaderWrapper.Instance.UnloadAdditiveScenes();
        }
    }
}
