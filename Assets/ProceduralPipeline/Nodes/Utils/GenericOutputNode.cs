using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[CreateNodeMenu("Utils/Generic Output")]
public class GenericOutputNode : SyncOutputNode {
	[Input] public string inputObject;
	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		return null; // Replace this
	}

	public override void ApplyOutput(PipelineRunner manager)
	{
	}

	public override IEnumerator CalculateOutputs(Action<bool> callback)
	{
		callback.Invoke(true);
		yield break;
	}

	public override void Release()
	{
		inputObject = null;
	}
}