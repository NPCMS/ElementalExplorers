using System;
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
		int bbWidth = (int)GlobeBoundingBox.LatitudeToMeters(bb.north - bb.south);
        int width = GetInputValue("chunkWidth", chunkWidth);
        int chunks = 1 + bbWidth / width;
        initialisedChunks = new ChunkContainer(new ChunkData[chunks, chunks], width, bbWidth);
        for (int i = 0; i < chunks; i++)
        {
            float chunkWidth = (i + 1) * width > bbWidth ? bbWidth - i * width : width;
            for (int j = 0; j < chunks; j++)
            {
                GameObject chunkParent = new GameObject(i + ", " + j);
                chunkParent.transform.parent = parent.transform;
                chunkParent.transform.position = new Vector3(i * width, 0, j * width);
                float chunkHeight = (j + 1) * width > bbWidth ? bbWidth - j * width : width;
                initialisedChunks.chunks[i, j] = new ChunkData(new Vector2Int(i, j), chunkParent.transform, chunkWidth, chunkHeight);
            }
        }

        callback.Invoke(true);
    }

    public override void Release()
    {
        initialisedChunks = null;
    }
}

[System.Serializable]
public class ChunkData
{
    public ChunkData(Vector2Int chunkCoordinates, Transform chunkParent, float width, float height)
    {
        this.chunkCoordinates = chunkCoordinates;
        this.chunkParent = chunkParent;
        this.width = width;
        this.height = height;
        this.worldPosition = chunkParent.transform.position;
    }

    public Vector2Int chunkCoordinates;
    public Transform chunkParent;
    public float width, height;
    public Vector3 worldPosition;
}

[System.Serializable]
public class ChunkContainer
{
    public ChunkContainer(ChunkData[,] chunks, int chunkWidth, float fullWidth)
    {
        this.chunks = chunks;
        this.fullWidth = fullWidth;
        this.chunkInfo = new ChunkingInfo(chunks.GetLength(0), chunkWidth);
    }

    public ChunkData[,] chunks;
    public ChunkingInfo chunkInfo;
    public float fullWidth;

    public Vector2Int GetChunkCoordFromPosition(Vector3 pos)
    {
        int x = Mathf.FloorToInt(Mathf.Clamp(pos.x / chunkInfo.chunkWidth, 0, chunkInfo.chunkWidthCount - 1));
        int y = Mathf.FloorToInt(Mathf.Clamp(pos.z / chunkInfo.chunkWidth, 0, chunkInfo.chunkWidthCount - 1));
        return new Vector2Int(x, y);
    }
}

[System.Serializable]
public struct ChunkingInfo
{
    public int chunkWidthCount;
    public int chunkWidth;

    public ChunkingInfo(int chunkWidthCount, int chunkWidth)
    {
        this.chunkWidthCount = chunkWidthCount;
        this.chunkWidth = chunkWidth;
    }
}