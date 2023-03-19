using System;
using System.Collections;
using UnityEngine;

[NodeTint(0.6f, 0.2f, 0.2f)]
public class AsyncOutputNode : SyncOutputNode
{

    [Input] public float i;
    
    public override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        Debug.Log("Output");
        yield break;
    }

    public override void ApplyOutput(AsyncPipelineManager manager)
    {
        
    }
}
