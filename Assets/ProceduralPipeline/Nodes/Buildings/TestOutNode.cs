using System;
using System.Collections;
using UnityEngine;

public class TestOutNode : SyncOutputNode
{
    [Input] public float i;
    
    public override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        Debug.Log(i);
        yield break;
    }

    public override void Release()
    {
    }

    public override void ApplyOutput(AsyncPipelineManager manager)
    {
    }
}
