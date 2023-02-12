using System;
using System.Collections.Generic;
using UnityEngine;
using XNode;
using Random = UnityEngine.Random;

[CreateNodeMenu("Grass/Create Grass Compute")]
public class CreateGrassComputeNode : ExtendedNode
{

	[Input] public ComputeShader computeShader;
	[Input] public ChunkContainer chunking;
	[Input] public float grassDensity = 1;
	[Input] public Texture2D clumpingTexture;
	[Input] public float clumpingAmount;
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
		ComputeShader compute = GetInputValue("computeShader", computeShader); 
		float min = GetInputValue("minScale", minScale); 
		float max = GetInputValue("minScale", maxScale);
		ChunkContainer chunks = GetInputValue("chunking", chunking);
		ElevationData elevation = GetInputValue("elevationData", elevationData);
		float density = GetInputValue("grassDensity", grassDensity);

		int resolution = (int)(chunks.fullWidth * density);
		
		ComputeBuffer positionsBuffer = new ComputeBuffer(resolution * resolution, 3 * 4);
		int kernelHandle = compute.FindKernel("CSMain");
		Texture2D clumping = GetInputValue("clumpingTexture", clumpingTexture);
		compute.SetTexture(kernelHandle, "_Clumping", clumping);
		compute.SetInt("_Resolution", resolution);
		Debug.Log(GetInputValue("clumpingAmount", clumpingAmount));
		compute.SetFloat("_ClumpingAmount", GetInputValue("clumpingAmount", clumpingAmount));
		compute.SetFloat("_FullWidth", chunks.fullWidth);
		compute.SetBuffer(kernelHandle, "_Positions", positionsBuffer);
		compute.Dispatch(kernelHandle, resolution / 8, resolution / 8, 1);

		Vector3[] positions = new Vector3[resolution * resolution];
		positionsBuffer.GetData(positions);
		List<GrassRenderer.GrassChunk> grassChunks = new List<GrassRenderer.GrassChunk>();
		foreach (ChunkData chunk in chunks.chunks)
		{
			grassChunks.Add(new GrassRenderer.GrassChunk(chunk.chunkParent, new List<Matrix4x4>(), chunk.chunkParent.position + new Vector3(chunks.chunkInfo.chunkWidth / 2, 0, chunks.chunkInfo.chunkWidth / 2)));
		}

		for (int i = 0; i < positions.Length; i++)
		{
			Vector3 pos = positions[i];
			Vector2Int coord = chunks.GetChunkCoordFromPosition(pos);
			pos.y = (float)elevation.SampleHeightFromPosition(pos);
			grassChunks[chunks.chunkInfo.chunkWidthCount * coord.x + coord.y].transforms.Add(Matrix4x4.TRS(pos, Quaternion.Euler(0, Random.value * 360, 0), Random.Range(minScale, maxScale) * Vector3.one));
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
	}
}