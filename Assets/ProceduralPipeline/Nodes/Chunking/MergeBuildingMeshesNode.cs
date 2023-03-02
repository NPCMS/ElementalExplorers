using System;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[CreateNodeMenu("Chunking/Merge Building Meshes by Chunk")]
public class MergeBuildingMeshesNode : ExtendedNode {

	[Input] public ChunkContainer chunks;
	[Input] public Material material;
	[Output] public ChunkContainer outChunks;
	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		if (port.fieldName == "outChunks")
		{
			return outChunks;
		}
		return null; // Replace this
	}

	public override void CalculateOutputs(Action<bool> callback)
	{
		ChunkContainer c = GetInputValue("chunks", chunks);
		Material mat = GetInputValue("material", material);
		foreach (ChunkData chunk in c.chunks)
		{
			GameObject parent = chunk.chunkParent.gameObject;
			MeshFilter filter = parent.AddComponent<MeshFilter>();
            MeshRenderer renderer = parent.AddComponent<MeshRenderer>();
			renderer.sharedMaterial = mat;
			Mesh mesh = new Mesh();
			List<CombineInstance> meshes = new List<CombineInstance>();
			Transform[] children = new Transform[parent.transform.childCount];
			for (int i = 0; i < children.Length; i++)
			{
				Transform child = parent.transform.GetChild(i);
				children[i] = child;
				CombineInstance instance = new CombineInstance();
				instance.mesh = child.GetComponent<MeshFilter>().sharedMesh;
				instance.transform = Matrix4x4.TRS(child.position - parent.transform.position, Quaternion.identity, Vector3.one);
				meshes.Add(instance);
			}
			for (int i = 0; i < children.Length; i++)
            {
				DestroyImmediate(children[i].gameObject);
            }
            mesh.CombineMeshes(meshes.ToArray());
			mesh.RecalculateBounds();
			mesh.RecalculateNormals();
			mesh.RecalculateTangents();
			parent.AddComponent<MeshCollider>().sharedMesh = mesh;
			filter.sharedMesh = mesh;
            parent.isStatic = true;
        }

		outChunks = c;
		callback.Invoke(true);
	}

	public override void Release()
	{
		chunks = null;
		outChunks = null;
	}
}