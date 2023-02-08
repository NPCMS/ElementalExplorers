using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using XNode;

public class RemoveGrassOnWaterNode : ExtendedNode
{
	private const float WaterRadius = 100;
	[Input] public GrassRenderer.GrassChunk[] grassChunks;
	[Input] public ChunkContainer chunking;
	[Input] public Texture2D waterMask;
	[Output] public GrassRenderer.GrassChunk[] outputChunks;

	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		if (port.fieldName == "outputChunks")
		{
			return outputChunks;
		}
		return null; // Replace this
	}

	public override void CalculateOutputs(Action<bool> callback)
	{
		ChunkContainer chunkContainer = GetInputValue("chunking", chunking);
		GrassRenderer.GrassChunk[] chunks = GetInputValue("grassChunks", grassChunks);
		Texture2D water = GetInputValue("waterMask", waterMask);
		
		float metersPerPixel = chunkContainer.fullWidth / water.width;
		for (int i = 0; i < water.width; i++)
		{
			for (int j = 0; j < water.height; j++)
			{
				if (water.GetPixel(i, j).r > 0.6f)
				{
					Vector3 worldPosition = new Vector3(i + 0.5f, 0, j + 0.5f) * metersPerPixel;
					Vector2Int coord = chunkContainer.GetChunkCoordFromPosition(worldPosition);
					GrassRenderer.GrassChunk chunk = chunks[coord.y + coord.x * chunkContainer.chunkInfo.chunkWidthCount];
					for (int k = 0; k < chunk.transforms.Count; k++)
					{
						Vector3 pos = chunk.transforms[k].GetPosition();
						pos.y = 0;
						Vector3 dir = pos - worldPosition;
						if (dir.sqrMagnitude < metersPerPixel * metersPerPixel)
						{
							chunk.transforms.RemoveAt(k);
							k--;
						}
					}
				}
			}
		}

		outputChunks = chunks;
		callback.Invoke(true);
	}
}