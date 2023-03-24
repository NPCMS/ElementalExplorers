using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[CreateNodeMenu("Utils/Transforms To Instances")]
public class TransformsToInstancesNode : AsyncExtendedNode {
    [Input] public Matrix4x4[] transforms;
    [Input] public int instanceIndex;
    [Output] public InstanceData instances;

    // Use this for initialization
    protected override void Init()
    {
        base.Init();

    }

    // Return the correct value of an output port when requested
    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "instances")
        {
            return instances;
        }
        return null; // Replace this
    }

    protected override void CalculateOutputsAsync(Action<bool> callback)
    {
        Matrix4x4[] input = GetInputValue("transforms", transforms);
        instances = new InstanceData(GetInputValue("instanceIndex", instanceIndex), input);
        callback.Invoke(true);
    }

	protected override void ReleaseData()
	{
        transforms = null;
        instances = null;
	}
}