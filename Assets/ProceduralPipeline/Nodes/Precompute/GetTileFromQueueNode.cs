using System;
using UnityEngine;
using XNode;

[CreateNodeMenu("Precompute/Get Tile from Queue")]
public class GetTileFromQueueNode : InputNode {
	[Output] public Vector2Int tile;

	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		if (port.fieldName == "tile")
		{
			return tile;
		}
		return null; // Replace this
	}

	public override void ApplyInputs(ProceduralManager manager)
	{
		tile = manager.PopTile();
	}

	public override void CalculateOutputs(Action<bool> callback)
	{
		callback.Invoke(true);
	}
}