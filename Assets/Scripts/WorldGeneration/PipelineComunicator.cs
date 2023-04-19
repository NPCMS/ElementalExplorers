using UnityEngine;

public class PipelineComunicator : MonoBehaviour
{
    public void FinishedPipelineCallback()
    {
        FindObjectOfType<TutorialState>().PlayerFinishedPipelineServerRpc();
    }
}
