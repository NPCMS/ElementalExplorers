using System.Collections.Generic;
using UnityEngine;

public class PipelineComunicator : MonoBehaviour
{
    public void FinishedPipelineCallback()
    {
        Debug.Log(RaceController.Instance);
        Debug.Log(GameObject.FindGameObjectsWithTag("Minigame"));
        Debug.Log(GameObject.FindGameObjectsWithTag("Minigame").Length);
        RaceController.Instance.minigameLocations = new List<GameObject>(GameObject.FindGameObjectsWithTag("Minigame"));
        FindObjectOfType<TutorialState>().PlayerFinishedPipelineServerRpc();
    }
}
