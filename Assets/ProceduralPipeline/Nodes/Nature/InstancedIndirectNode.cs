using System;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class InstancedIndirectNode : ExtendedNode {
	[Input] public ChunkContainer chunking;
	[Input] public Matrix4x4[] transforms;
	[Input] public Mesh mesh;
	[Input] public Material material;
	[Output] public ChunkContainer outputChunking;

	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		if (port.fieldName == "outputChunking")
		{
			return outputChunking;
		}
		return null; // Replace this
	}

	public override void CalculateOutputs(Action<bool> callback)
	{
		ChunkContainer chunks = GetInputValue("chunking", chunking);
		Dictionary<Vector2Int, List<Matrix4x4>> instances = new Dictionary<Vector2Int, List<Matrix4x4>>();
		Matrix4x4[] mats = GetInputValue("transforms", transforms);

		Material mat = GetInputValue("material", material);
        Mesh msh = GetInputValue("mesh", mesh);
        for (int i = 0; i < mats.Length; i++)
		{
			Vector2Int chunk = chunks.GetChunkCoordFromPosition(mats[i].GetPosition());
			if (!instances.ContainsKey(chunk))
			{
				instances.Add(chunk, new List<Matrix4x4>());
			}

			instances[chunk].Add(mats[i]);
		}

		foreach (KeyValuePair<Vector2Int, List<Matrix4x4>> chunk in instances)
		{
			GameObject parent = chunks.chunks[chunk.Key.x, chunk.Key.y].chunkParent.gameObject;
			List<Matrix4x4> chunkTransforms = chunk.Value;
            DrawMeshInstancedIndirect instancer = parent.AddComponent<DrawMeshInstancedIndirect>();
			instancer.Setup(chunkTransforms.ToArray(), msh, mat, chunks.chunkInfo.chunkWidth);
        }
		outputChunking = chunks;
		callback.Invoke(true);
	}
}