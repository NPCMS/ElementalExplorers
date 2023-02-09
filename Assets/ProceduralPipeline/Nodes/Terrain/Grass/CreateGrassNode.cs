using System;
using System.Collections.Generic;
using UnityEngine;
using XNode;
using Random = UnityEngine.Random;

[CreateNodeMenu("Terrain/Create Grass")]
public class CreateGrassNode : ExtendedNode
{
	[Input] public ChunkContainer chunking;
	[Input] public float grassDensity = 1;
	[Input] public ElevationData elevationData;
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
		ChunkContainer chunks = GetInputValue("chunking", chunking);
		List<GrassRenderer.GrassChunk> grass = new List<GrassRenderer.GrassChunk>();
		ElevationData elevation = GetInputValue("elevationData", elevationData);
		float density = GetInputValue("grassDensity", grassDensity);
		foreach (ChunkData chunk in chunks.chunks)
		{
			Vector3 origin = chunk.worldPosition;
			List<Matrix4x4> transforms = new List<Matrix4x4>();
			int instances = (int)(density * chunk.width * chunk.height);
			for (int i1 = 0; i1 < instances; i1++)
			{
				Vector3 pos = origin + new Vector3(Random.value * chunk.width, 0,
					Random.value * chunk.height);
				pos.y = (float)elevation.SampleHeightFromPosition(pos);
				
				transforms.Add(Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one));
			}

			grass.Add(new GrassRenderer.GrassChunk(transforms, origin + new Vector3(chunks.chunkInfo.chunkWidth / 2, 0, chunks.chunkInfo.chunkWidth / 2)));
		}

		grassChunks = grass.ToArray();
		callback.Invoke(true);
	}
}