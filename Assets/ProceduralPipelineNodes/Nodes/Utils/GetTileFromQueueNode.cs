using System;
using System.Collections;
using UnityEngine;
using XNode;

[CreateNodeMenu("Utils/Get Tile From Queue")]
public class GetTileFromQueue : SyncInputNode 
{
	[Output] public Vector2Int tileIndex;
	[Output] public string filepath;

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port)
	{
		return port.fieldName switch
		{
			"tileIndex" => tileIndex,
			"filepath" => filepath,
			_ => null
		};
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