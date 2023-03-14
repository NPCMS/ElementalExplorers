using System;
using UnityEngine;
using XNode;

public class OutputInstancesNode : OutputNode {
	[Input] public InstanceData instances;
	[Input] public Vector2 tileIndex;

	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		return null; // Replace this
	}

	public override void CalculateOutputs(Action<bool> callback)
	{
		callback.Invoke(true);
	}

	public override void ApplyOutput(ProceduralManager manager)
	{
		Vector2 tile = GetInputValue("tileIndex", tileIndex);
		manager.SetInstances(GetInputValue("instances", instances), new Vector2Int((int)tile.x, (int)tile.y));
	}

	public override void Release()
	{
		base.Release();
		instances = null;
	}
}