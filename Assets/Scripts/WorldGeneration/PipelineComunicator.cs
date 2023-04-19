using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PipelineComunicator : MonoBehaviour
{
    public void FinishedPipelineCallback()
    {
        Debug.LogWarning("Sending ready rpc for pipeline");
        FindObjectOfType<TutorialState>().PlayerFinishedPipelineServerRpc();
    }
}
