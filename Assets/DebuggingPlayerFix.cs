using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebuggingPlayerFix : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        //reset player pos in gameplay
        if (Input.GetKeyDown(KeyCode.M))
        {
            MultiPlayerWrapper.localPlayer.ResetPlayerPos();
            MultiPlayerWrapper.localPlayer.transform.position = new Vector3(700f, 700f, 700f); 
            //reset player pos in spaceship
        } else if (Input.GetKeyDown(KeyCode.S))
        {
            MultiPlayerWrapper.localPlayer.ResetPlayerPos();
            MultiPlayerWrapper.localPlayer.transform.position = new Vector3(-5.21000004f,5005.3501f,-20.5100002f);
        }
        //reset player pos in tutorial
        else if (Input.GetKeyDown(KeyCode.T))
        {
            MultiPlayerWrapper.localPlayer.ResetPlayerPos();
            MultiPlayerWrapper.localPlayer.transform.position = new Vector3(171f, 4960f, 22f);
        }
    }
}
