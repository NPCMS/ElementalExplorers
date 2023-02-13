using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using XNode;

[CreateNodeMenu("Grass/Mask Grass")]
public class MaskGrassNode : ExtendedNode
{
	[Input] public GrassRenderer.GrassChunk[] grassChunks;
	[Input] public ChunkContainer chunking;
	[Input] public Texture2D buildingMask;
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
        Texture2D building = GetInputValue("buildingMask", buildingMask);
        Texture2D water = GetInputValue("waterMask", waterMask);

		FilterByTexture(chunkContainer, chunks, water);
        FilterByTexture(chunkContainer, chunks, building, 0.01f);

        outputChunks = chunks;
		callback.Invoke(true);
	}

	private static void FilterByTexture(ChunkContainer chunkContainer, GrassRenderer.GrassChunk[] chunks, Texture2D texture, float threshold = 0.6f)
	{
		float metersPerPixel = chunkContainer.fullWidth / texture.width;
		for (int i = 0; i < texture.width; i++)
		{
			for (int j = 0; j < texture.height; j++)
			{
				if (texture.GetPixel(i, j).r > threshold)
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
	}

	public override void Release()
	{
		grassChunks = null;
		chunking = null;
		buildingMask = null;
		waterMask = null;
		outputChunks = null;
	}
}