using System;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[CreateNodeMenu("Utils/Combine GameObject Lists")]
public class CombineGameObjectsListNode : AsyncExtendedNode
{
	[Input] public GameObject[] go1;
	[Input] public GameObject[] go2;

	[Output] public GameObject[] comb;
	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		if (port.fieldName == "comb")
		{
			return comb;
		}
		return null; // Replace this
	}

	protected override void CalculateOutputsAsync(Action<bool> callback)
	{
		var l = new List<GameObject>(GetInputValue("go1", go1));
		l.AddRange(GetInputValue("go2", go2));
		comb = l.ToArray();
		callback.Invoke(true);
	}

	protected override void ReleaseData()
	{
		go1 = null;
		go2 = null;
		comb = null;
	}
}