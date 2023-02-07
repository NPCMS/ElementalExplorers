using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

[CreateNodeMenu("Buildings/Chunk Data")]
public class ChunkDataNode : ExtendedNode {

	[Input] public GameObject[] buildings;
	[Input] public int chunkWidth = 100;
	[Output] public ChunkData[] chunkOutputs;

	protected override void Init() {
		base.Init();
		
	}

	public override object GetValue(NodePort port) {
		if (port.fieldName == "chunkOutputs")
		{
			return chunkOutputs;
		}
		return null; 
	}

	private Vector2Int GetChunk(Vector3 pos, int width)
	{
		return new Vector2Int(Mathf.FloorToInt(pos.x / width), Mathf.FloorToInt(pos.z / width));

    }

	public override void CalculateOutputs(Action<bool> callback)
	{
		GameObject[] gameObjects = GetInputValue("buildings", buildings);
		int width = GetInputValue("chunkWidth", chunkWidth);
		Dictionary<Vector2Int, ChunkData> chunks = new Dictionary<Vector2Int, ChunkData>();
		foreach (GameObject go in gameObjects)
		{
			Vector2Int chunk = GetChunk(go.transform.position, width);
			if (chunks.ContainsKey(chunk))
			{
				ChunkData data = chunks[chunk];
				go.transform.parent = data.ChunkParent;
			}
			else
			{
				GameObject parent = new GameObject(chunk.ToString());
				parent.transform.parent = go.transform.parent;
				parent.transform.position = new Vector3(chunk.x * width, 0, chunk.y * width);
				go.transform.parent = parent.transform;
				ChunkData data = new ChunkData(chunk, width, parent.transform);
				chunks.Add(chunk, data);
			}
		}

		chunkOutputs = chunks.Values.ToArray();
	}
}

[System.Serializable]
public class ChunkData
{
	public ChunkData(Vector2Int chunkCoordinates, int width, Transform chunkParent)
	{
		ChunkCoordinates = chunkCoordinates;
		Width = width;
		ChunkParent = chunkParent;
	}

	public Vector2Int ChunkCoordinates { get; private set; }
    public int Width { get; private set; }
    public Transform ChunkParent { get; private set; }
}