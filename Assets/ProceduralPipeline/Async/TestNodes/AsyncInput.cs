using System;
using System.Collections;
using UnityEngine;
using XNode;

[NodeTint(0.2f, 0.6f, 0.2f)]
public class AsyncInput : SyncInputNode
{
    [Output] public float o;

    public override object GetValue(NodePort port)
    {
        return o;
    }

    public override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        Debug.Log("Input");
        callback.Invoke(true);
        yield break;
    }

    public override void ApplyInputs(AsyncPipelineManager manager)
    {
        
    }
}
