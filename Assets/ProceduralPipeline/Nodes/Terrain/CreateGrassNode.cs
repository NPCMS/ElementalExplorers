using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[CreateNodeMenu("Terrain/Create Grass")]
public class CreateGrassNode : ExtendedNode {
	[Input] public ChunkContainer chunking;
	[Input] public int batchesPerChunk = 3;
	[Output] public GrassRenderer.GrassChunk[] grassChunks;
	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		if (port.fieldName == "grassChunks")
		{
			return grassChunks;
        }
		return null; // Replace this
	}

	public override void CalculateOutputs(Action<bool> callback)
	{
		ChunkingInfo chunkInfo = GetInputValue("chunking", chunking).chunkInfo;
		int width = chunkInfo.chunkWidth;
		grassChunks = new GrassRenderer.GrassChunk[chunkInfo.chunkWidthCount * chunkInfo.chunkWidthCount];
		for (int i = 0; i < chunkInfo.chunkWidthCount; i++)
		{
			for (int j = 0; j < chunkInfo.chunkWidthCount; j++)
			{
				for (int batch = 0; batch < batchesPerChunk; batch++)
				{

				}
			}
		}
		callback.Invoke(true);
	}
}