using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;
using XNode;

[CreateNodeMenu("Buildings/Create Chunks")]
public class CreateChunksNode : ExtendedNode
{
	[Input] public GlobeBoundingBox boundingBox;
    [Input] public int chunkWidth = 500;
    [Output] public ChunkContainer initialisedChunks;
	
	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		if (port.fieldName == "initialisedChunks")
		{
			return initialisedChunks;
		}
		return null; // Replace this
	}

	public override void CalculateOutputs(Action<bool> callback)
	{
        GameObject parent = new GameObject("Chunks");
		GlobeBoundingBox bb = GetInputValue("boundingBox", boundingBox);
		int bbWidth = (int)GlobeBoundingBox.LatitudeToMeters(bb.north - bb.south) + 1;
        int width = GetInputValue("chunkWidth", chunkWidth);
        int chunks = 1 + bbWidth / width;
        initialisedChunks = new ChunkContainer(new ChunkData[chunks, chunks], width);
        for (int i = 0; i < chunks; i++)
        {
            for (int j = 0; j < chunks; j++)
            {
                GameObject chunkParent = new GameObject(i + ", " + j);
                chunkParent.transform.parent = parent.transform;
                chunkParent.transform.position = new Vector3(i * width, 0, j * width);
                initialisedChunks.chunks[i,j] = new ChunkData(new Vector2Int(i, j), chunkParent.transform);
            }
        }
        callback.Invoke(true);
    }
}

[System.Serializable]
public class ChunkData
{
    public ChunkData(Vector2Int chunkCoordinates, Transform chunkParent)
    {
        this.chunkCoordinates = chunkCoordinates;
        this.chunkParent = chunkParent;
    }

    public Vector2Int chunkCoordinates;
    public Transform chunkParent;
}

[System.Serializable]
public class ChunkContainer
{
    public ChunkContainer(ChunkData[,] chunks, int chunkWidth)
    {
        this.chunks = chunks;
        this.chunkWidthCount = chunks.GetLength(0);
        this.chunkWidth = chunkWidth;
    }

    public ChunkData[,] chunks;
    public int chunkWidthCount;
    public int chunkWidth;

    public Vector2Int GetChunkFromPosition(Vector3 pos)
    {
        return new Vector2Int(Mathf.FloorToInt(pos.x / chunkWidth), Mathf.FloorToInt(pos.z / chunkWidth));
    }
}