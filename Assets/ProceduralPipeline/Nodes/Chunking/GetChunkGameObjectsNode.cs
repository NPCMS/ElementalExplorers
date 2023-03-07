using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[CreateNodeMenu("Chunking/Get Chunk GameObject Parents")]
public class GetChunkGameObjectsNode : ExtendedNode {

	[Input] public ChunkContainer chunkContainer;
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
		ChunkContainer chunks = GetInputValue("chunkContainer", chunkContainer);
		List<GameObject> gos = new List<GameObject>();
		foreach (ChunkData chunk in chunks.chunks)
		{
			gos.Add(chunk.chunkParent.gameObject);
		}

		output = gos.ToArray();
		callback.Invoke(true);
	}

	public override void Release()
	{
		base.Release();
		chunkContainer = null;
		output = null;
	}
}