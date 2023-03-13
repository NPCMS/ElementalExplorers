using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using XNode;

[CreateNodeMenu("Chunking/Merge General Meshes By Material")]
public class GeneralChunkMeshMerge : ExtendedNode
{
	[Input] public ChunkContainer chunkContainer;

	[Input] public GameObject[] toChunk;

	[Output] public ChunkContainer outputContainer;
	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		if (port.fieldName == "outputContainer")
		{
			return outputContainer;
		}
		return null; // Replace this
	}

	private void AddInstance(Dictionary<Material, List<CombineInstance>> instances, Dictionary<Material, bool> hasCollider, Dictionary<Material, Material[]> exampleRenderer, GameObject go, Transform parent)
	{
		if (go.TryGetComponent(out MeshRenderer renderer))
		{
			if (!instances.ContainsKey(renderer.sharedMaterial))
			{
				instances.Add(renderer.sharedMaterial, new List<CombineInstance>());
                hasCollider.Add(renderer.sharedMaterial, go.GetComponentInChildren<Collider>() != null);
                exampleRenderer.Add(renderer.sharedMaterial, renderer.sharedMaterials);
            }
			else
			{
                hasCollider[renderer.sharedMaterial] |= go.GetComponentInChildren<Collider>() != null;
            }


            Matrix4x4 transform = Matrix4x4.TRS(go.transform.position - parent.position, go.transform.rotation, go.transform.localScale);
			instances[renderer.sharedMaterial].Add(new CombineInstance() {mesh = go.GetComponent<MeshFilter>().sharedMesh, transform = transform});
		}

		foreach (Transform child in go.transform)
		{
			AddInstance(instances, hasCollider, exampleRenderer, child.gameObject, parent);
		}
	}

	public override void CalculateOutputs(Action<bool> callback)
	{
		ChunkContainer chunks = GetInputValue("chunkContainer", chunkContainer);
		GameObject[] gos = GetInputValue("toChunk", toChunk);
		Dictionary<Vector2Int, List<GameObject>> parented = new Dictionary<Vector2Int, List<GameObject>>();
        foreach (GameObject go in gos)
		{
			Vector2Int index = chunks.GetChunkCoordFromPosition(go.transform.position);
			if (!parented.ContainsKey(index))
			{
				parented.Add(index, new List<GameObject>());
			}
			parented[index].Add(go);
		}

		foreach (KeyValuePair<Vector2Int, List<GameObject>> pair in parented)
		{
			Transform parent = chunks.chunks[pair.Key.x, pair.Key.y].chunkParent;
			Dictionary<Material, List<CombineInstance>> instances = new Dictionary<Material, List<CombineInstance>>();
            Dictionary<Material, bool> hasCollider = new Dictionary<Material, bool>();
            Dictionary<Material, Material[]> exampleRenderer = new Dictionary<Material, Material[]>();
            foreach (GameObject go in pair.Value)
			{
				AddInstance(instances, hasCollider, exampleRenderer, go, parent);
				DestroyImmediate(go);
			}

			foreach (KeyValuePair<Material,List<CombineInstance>> merge in instances)
			{
				GameObject mergeGO = new GameObject(merge.Key.name);
				mergeGO.transform.parent = parent;
				mergeGO.transform.localPosition = Vector3.zero;
				Mesh mesh = new Mesh();
				mesh.indexFormat = IndexFormat.UInt32;
				mesh.CombineMeshes(merge.Value.ToArray(), true, true);
				mesh.RecalculateBounds();
				mesh.RecalculateNormals();
				mesh.RecalculateTangents();
				mergeGO.AddComponent<MeshFilter>().sharedMesh = mesh;
				mergeGO.AddComponent<MeshRenderer>().sharedMaterials = exampleRenderer[merge.Key];
				if (hasCollider[merge.Key])
				{
					mergeGO.AddComponent<MeshCollider>().sharedMesh = mesh;
				}
			}
		}

		outputContainer = chunks;
		
		callback.Invoke(true);
	}

	public override void Release()
	{
		base.Release();
		chunkContainer = null;
		toChunk = null;
		outputContainer = null;
	}
}