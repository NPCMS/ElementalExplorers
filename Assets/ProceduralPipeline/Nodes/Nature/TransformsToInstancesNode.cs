using System;
using UnityEngine;
using XNode;

public class TransformsToInstancesNode : ExtendedNode {
	[Input] public Matrix4x4[] transforms;
	[Input] public int instanceIndex;
	[Output] public InstanceData instances;

	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		if (port.fieldName == "instances")
		{
			return instances;
		}
		return null; // Replace this
	}

	public override void CalculateOutputs(Action<bool> callback)
	{
		Matrix4x4[] input = GetInputValue("transforms", transforms);
		instances = new InstanceData(GetInputValue("instanceIndex", instanceIndex), input);
		callback.Invoke(true);
	}

	public override void Release()
	{
		base.Release();
		transforms = null;
		instances = null;
	}
}