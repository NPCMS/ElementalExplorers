using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[CreateNodeMenu("Utils/Get Tile From Queue")]
public class GetTileFromQueue : SyncInputNode 
{
	[Output] public Vector2Int tileIndex;
	[Output] public string filepath;
	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		if (port.fieldName == "tileIndex")
		{
			return tileIndex;
		}
		else if (port.fieldName == "filepath")
		{
			return filepath;
		}
		return null; // Replace this
	}

	public override void ApplyInputs(AsyncPipelineManager manager)
	{
		tileIndex = manager.PopTile();
		filepath = ChunkIO.GetFilePath(tileIndex.ToString() + ".rfm");
	}

	public override IEnumerator CalculateOutputs(Action<bool> callback)
	{
		callback.Invoke(true);
		yield break;
	}

	public override void Release()
	{
	}
}