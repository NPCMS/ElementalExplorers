using System;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[CreateNodeMenu("Utils/Merge Transforms Lists")]
public class MergeTransformsArrayNode : AsyncExtendedNode {
	[Input] public Matrix4x4[] data1;
	[Input] public Matrix4x4[] data2;
	[Output] public Matrix4x4[] output;
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

	protected override void CalculateOutputsAsync(Action<bool> callback)
	{
		List<Matrix4x4> transforms = new List<Matrix4x4>(GetInputValue("data1", data1));
		transforms.AddRange(GetInputValue("data2", data2));
		output = transforms.ToArray();
		callback.Invoke(true);
	}

	protected override void ReleaseData()
	{
		data1 = null;
		data2 = null;
		output = null;
	}
}