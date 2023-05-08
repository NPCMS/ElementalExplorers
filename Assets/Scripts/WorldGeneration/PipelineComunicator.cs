using System.Collections.Generic;
using UnityEngine;

public class PipelineComunicator : MonoBehaviour
{
    public void FinishedPipelineCallback()
    {
        Debug.Log(RaceController.Instance);
        Debug.Log(GameObject.FindGameObjectsWithTag("Minigame"));
        Debug.Log(GameObject.FindGameObjectsWithTag("Minigame").Length);
        FindObjectOfType<TutorialState>().PlayerFinishedPipelineServerRpc();
    }
}
