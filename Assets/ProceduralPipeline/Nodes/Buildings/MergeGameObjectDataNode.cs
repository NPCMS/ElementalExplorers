using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[CreateNodeMenu("Buildings/Merge GameObjectData Arrays")]
public class MergeGameObjectDataNode : AsyncExtendedNode {
	[Input] public GameObjectData[] in1;
	[Input] public GameObjectData[] in2;
	[Output] public GameObjectData[] output;
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
		List<GameObjectData> data = new List<GameObjectData>(GetInputValue("in1", in1));
		data.AddRange(GetInputValue("in2", in2));
		output = data.ToArray();
		callback.Invoke(true);
	}

	protected override void ReleaseData()
	{
		in1 = null;
		in2 = null;
		output = null;
	}
}