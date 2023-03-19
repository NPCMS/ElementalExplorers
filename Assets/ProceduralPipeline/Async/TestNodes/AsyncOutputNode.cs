using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
