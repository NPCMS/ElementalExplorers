using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[CreateNodeMenu("Utils/Instatiate From GameObjectData")]
public class InstantiateFromDataNode : SyncExtendedNode
{
    [Input] public GameObjectData[] objectData;
    [Output] public GameObject[] output;


    protected override void Init()
    {
        base.Init();

    }

    // Return the correct value of an output port when requested
    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "output")
        {
            return output;
        }
        return null; // Replace this
    }

    // public override IEnumerator CalculateOutputs(Action<bool> callback)
    // {
    //     SyncYieldingWait syncYield = new SyncYieldingWait();
    //
    //     GameObjectData[] data = GetInputValue("objectData", objectData);
    //     output = new GameObject[data.Length];
    //     for (int j = 0; j < data.Length; j++)
    //     {
    //         output[j] = data[j].Instantiate(null);
    //         if (syncYield.YieldIfTimePassed()) yield return null;
    //     }
    //
    //     callback.Invoke(true);
    // }
    
    public override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        int batchSize = 1;
        int batchCounter = 0;

        GameObjectData[] data = GetInputValue("objectData", objectData);
        output = new GameObject[data.Length];
        for (int j = 0; j < data.Length; j++)
        {
            output[j] = data[j].Instantiate(null);
            batchCounter++;
            if (batchCounter == batchSize)
            {
                yield return null;
                batchCounter = 0;
                if (Time.smoothDeltaTime > 1.0f / 60f) // too slow
                {
                    if (batchSize > 1)
                    {
                        batchSize = Math.Max(1, (int)(batchSize * 1 / (Time.smoothDeltaTime * 60f)));
                    }
                }
                else
                {
                    batchSize += 1;
                }
                Debug.Log("Timings: " + Time.smoothDeltaTime + " Batch size: " + batchSize);
            }
        }

        callback.Invoke(true);
    }

    public override void Release()
    {
        objectData = null;
        output = null;
    }
}