using System.Collections.Generic;
using UnityEngine;

public class PipelineComunicator : MonoBehaviour
{
    public void FinishedPipelineCallback()
    {
        RaceController.Instance.minigameLocations = new List<GameObject>(GameObject.FindGameObjectsWithTag("Minigame"));
        FindObjectOfType<TutorialState>().PlayerFinishedPipelineServerRpc();
    }
}
