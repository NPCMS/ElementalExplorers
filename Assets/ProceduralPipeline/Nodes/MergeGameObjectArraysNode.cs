using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class MergeGameObjectArraysNode : ExtendedNode {
	[Input] public GameObject[] go1;
    [Input] public GameObject[] go2;
	[Output] public GameObject[] output;

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
		var list = new List<GameObject>();
		list.AddRange(GetInputValue("go1", go1));
		list.AddRange(GetInputValue("go2", go2));
        output = list.ToArray();
		callback.Invoke(true);
	}

	public override void Release()
	{
		base.Release();
		output = null;
	}
}