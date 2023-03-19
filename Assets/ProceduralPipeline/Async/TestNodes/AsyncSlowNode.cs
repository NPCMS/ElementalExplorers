using System;
using UnityEngine;
using XNode;

public class AsyncSlowNode : AsyncExtendedNode
{
    [Input] public float i;
    [Output] public float o;

    public override object GetValue(NodePort port)
    {
        return o;
    }

    public override void CalculateOutputsAsync(Action<bool> callback)
    {
        Debug.Log("Starting Work");
        o = i;
        System.Threading.Thread.Sleep(5000);
        Debug.Log("Ending Work");
        callback.Invoke(true);
    }
}
