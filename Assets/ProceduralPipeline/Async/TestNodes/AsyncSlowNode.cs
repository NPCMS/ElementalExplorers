using System;
using System.Collections;
using UnityEngine;
using XNode;

public class AsyncSlowNode : SyncExtendedNode
{
    [Input] public float i;
    [Output] public float o;

    public override object GetValue(NodePort port)
    {
        return o;
    }

    // public override void CalculateOutputsAsync(Action<bool> callback)
    // {
    //     Debug.Log("Starting Work");
    //     o = i;
    //     System.Threading.Thread.Sleep(5000);
    //     Debug.Log("Ending Work");
    //     callback.Invoke(true);
    // }

    public override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        Debug.Log("Starting Work");
        o = i;
        System.Threading.Thread.Sleep(5000);
        Debug.Log("Ending Work");
        callback.Invoke(true);
        yield break;
    }
}
