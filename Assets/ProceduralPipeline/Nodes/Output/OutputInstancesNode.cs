using System;
using System.Collections;
using UnityEngine;
using XNode;

[CreateNodeMenu("Output/Output Instances")]
public class OutputInstancesNode : SyncOutputNode
{
    [Input] public InstanceData instances;
    [Input] public Vector2Int tileIndex;

    // Use this for initialization
    protected override void Init()
    {
        base.Init();

    }

    // Return the correct value of an output port when requested
    public override object GetValue(NodePort port)
    {
        return null; // Replace this
    }

    public override void ApplyOutput(PipelineRunner manager)
    {
        Vector2Int tile = GetInputValue("tileIndex", tileIndex);
        manager.SetInstances(GetInputValue("instances", instances), tile);
    }

    public override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        callback.Invoke(true);
        yield break;
    }

    public override void Release()
    {
        instances = null;
    }
}