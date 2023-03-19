using System;
using System.Collections;
using System.Threading;
using UnityEngine;
using XNode;

public class AsyncSlowNode : SyncYeildingNode
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
        for (int j = 0; j < 10000000; j++)
        {
            if (YieldIfTimePassed()) yield return new WaitForEndOfFrame();
        }
        Debug.Log("Ending Work");
        callback.Invoke(true);
    }
}
