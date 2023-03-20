using ProceduralPipelineNodes.Nodes.Chunking;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[CreateNodeMenu("Optimisation/Create Chunks")]
public class CreateChunksNode : SyncExtendedNode {
    [Input] public GlobeBoundingBox boundingBox;
    [Input] public int chunkWidth = 500;
    [Output] public ChunkContainer initialisedChunks;
    // Return the correct value of an output port when requested
    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "initialisedChunks")
        {
            return initialisedChunks;
        }
        return null; // Replace this
    }

    public override IEnumerator CalculateOutputs(Action<bool> callback)
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
                chunkParent.isStatic = true;
                chunkParent.transform.parent = parent.transform;
                chunkParent.transform.position = new Vector3(i * width, 0, j * width);
                float chunkHeight = (j + 1) * width > bbWidth ? bbWidth - j * width : width;
                initialisedChunks.chunks[i, j] = new ChunkData(new Vector2Int(i, j), chunkParent.transform, chunkWidth, chunkHeight);
            }
        }

        callback.Invoke(true);
        yield break;
    }

    public override void Release()
    {
        initialisedChunks = null;
    }

}