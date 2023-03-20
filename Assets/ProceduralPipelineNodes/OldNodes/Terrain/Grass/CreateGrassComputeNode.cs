using ProceduralPipelineNodes.Nodes.Chunking;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using XNode;
using Random = UnityEngine.Random;

namespace ProceduralPipelineNodes.Nodes.Terrain.Grass
{
	[CreateNodeMenu("Grass/Create Grass Compute")]
	public class CreateGrassComputeNode : ExtendedNode
	{

		[Input] public ComputeShader computeShader;
		[Input] public ChunkContainer chunking;
		[Input] public float grassDensity = 1;
		[Input] public Texture2D buildingMask;
		[Input] public Texture2D waterMask;
		[Input] public Texture2D clumpingTexture;
		[Input] public float clumpingAmount;
		[Input] public ElevationData elevationData;
		[Input] public float minScale = 1.0f;
		[Input] public float maxScale = 1.0f;
		//[Input] public float jitterAmount = 1f;
		[Input] public float jitterScale = 0.1f;

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

		private Texture2D CreateTerrainHeightMap(ElevationData elevation)
		{
			int width = elevation.height.GetLength(0);
			Texture2D height = new Texture2D(width, width, GraphicsFormat.R16_UNorm, TextureCreationFlags.None);
			for (int i = 0; i < width; i++)
			{
				for (int j = 0; j < width; j++)
				{
					height.SetPixel(i, j, new Color(elevation.height[j,i],0,0));
				}
			}

			height.Apply();
			return height;
		}

		public override void CalculateOutputs(Action<bool> callback)
		{
			ComputeShader compute = GetInputValue("computeShader", computeShader); 
			float min = GetInputValue("minScale", minScale); 
			float max = GetInputValue("maxScale", maxScale); 
			ChunkContainer chunks = GetInputValue("chunking", chunking);
			ElevationData elevation = GetInputValue("elevationData", elevationData);
			float density = GetInputValue("grassDensity", grassDensity);
			//float jitter = GetInputValue("jitterAmount", jitterAmount);
			float sJitter = GetInputValue("sizeJitter", jitterScale);

			int resolution = (int)(chunks.fullWidth * density);
		
			ComputeBuffer positionsBuffer = new ComputeBuffer(resolution * resolution, 4 * 4);
			int kernelHandle = compute.FindKernel("CSMain");
			Texture2D clumping = GetInputValue("clumpingTexture", clumpingTexture);
			compute.SetTexture(kernelHandle, "_BuildingMask", GetInputValue("buildingMask", buildingMask));
			compute.SetTexture(kernelHandle, "_WaterMask", GetInputValue("waterMask", waterMask));
			compute.SetTexture(kernelHandle, "_Clumping", clumping);
			compute.SetTexture(kernelHandle, "_Heightmap", CreateTerrainHeightMap(elevation));
			compute.SetFloat("_Height", (float) (elevation.maxHeight - elevation.minHeight));
			compute.SetFloat("_HeightOffset", (float) elevation.minHeight);
			compute.SetInt("_Resolution", resolution);
			compute.SetFloat("_ClumpingAmount", GetInputValue("clumpingAmount", clumpingAmount));
			compute.SetFloat("_FullWidth", chunks.fullWidth);
			compute.SetBuffer(kernelHandle, "_Positions", positionsBuffer);
			compute.Dispatch(kernelHandle, resolution / 8, resolution / 8, 1);
		
			Vector4[] positions = new Vector4[resolution * resolution];
			positionsBuffer.GetData(positions);
			List<GrassRenderer.GrassChunk> grassChunks = new List<GrassRenderer.GrassChunk>();
			foreach (ChunkData chunk in chunks.chunks)
			{
				grassChunks.Add(new GrassRenderer.GrassChunk(chunk.chunkParent, new List<Matrix4x4>(), chunk.chunkParent.position + new Vector3(chunks.chunkInfo.chunkWidth / 2, 0, chunks.chunkInfo.chunkWidth / 2)));
			}

			for (int i = 0; i < positions.Length; i++)
			{
				Vector4 pos = positions[i];
				if (pos.w < 0.05f)
				{
					continue;
				}

				//pos.x += Random.Range(-jitter, jitter);
				//pos.z += Random.Range(-jitter, jitter);
				Vector2Int coord = chunks.GetChunkCoordFromPosition(pos);
				// pos.y = (float)elevation.SampleHeightFromPosition(pos);
				grassChunks[chunks.chunkInfo.chunkWidthCount * coord.x + coord.y].transforms.Add(Matrix4x4.TRS(pos, Quaternion.Euler(0, Random.value * 360, 0), Vector3.one * (Mathf.Lerp(minScale, maxScale, positions[i].w) + Random.Range(-sJitter, sJitter))));
			}

			this.grassChunks = grassChunks.ToArray();

			positionsBuffer.Dispose();

			callback.Invoke(true);
		}

		public override void Release()
		{
			clumpingTexture = null;
			chunking = null;
			elevationData = null;
			grassChunks = null;
			buildingMask = null;
			waterMask = null;
		}
	}
}