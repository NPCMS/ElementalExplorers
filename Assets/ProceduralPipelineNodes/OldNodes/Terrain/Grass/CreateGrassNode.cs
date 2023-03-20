using System;
using System.Collections.Generic;
using ProceduralPipelineNodes.Nodes.Chunking;
using UnityEngine;
using XNode;
using Random = UnityEngine.Random;

namespace ProceduralPipelineNodes.Nodes.Terrain.Grass
{
	[CreateNodeMenu("Grass/Create Grass")]
	public class CreateGrassNode : ExtendedNode
	{
		[Input] public ChunkContainer chunking;
		[Input] public float grassDensity = 1;
		[Input] public ElevationData elevationData;
		[Input] public float minScale = 0.5f;
		[Input] public float maxScale = 1.5f;
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
			float min = GetInputValue("minScale", minScale); 
			float max = GetInputValue("minScale", maxScale);
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
				
					transforms.Add(Matrix4x4.TRS(pos, Quaternion.Euler(0, Random.value * 360, 0), Vector3.one * Random.Range(min, max)));
				}

				grass.Add(new GrassRenderer.GrassChunk(chunk.chunkParent, transforms, origin + new Vector3(chunks.chunkInfo.chunkWidth / 2, 0, chunks.chunkInfo.chunkWidth / 2)));
			}

			grassChunks = grass.ToArray();
			callback.Invoke(true);
		}

		public override void Release()
		{
			chunking = null;
			elevationData = null;
			grassChunks = null;
		}
	}
}