using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class AppendInstancesToListNode : ExtendedNode {
	[Input] public InstanceData input;
	[Input] public InstanceData toAppend;
	[Output] public InstanceData output;
	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		if (port.fieldName == "output")
		{
			return output;
		}
		return null; // Replace this
	}

	public override void CalculateOutputs(Action<bool> callback)
	{
		InstanceData main = GetInputValue("input", input);
		List<Matrix4x4> transforms = new List<Matrix4x4>(main.instances);
        transforms.AddRange(GetInputValue("toAppend", toAppend).instances);
		output = new InstanceData(main.instancerIndex, transforms.ToArray());
	}

	public override void Release()
	{
		base.Release();
		output = null;
        input = null;
        toAppend = null;
	}
}