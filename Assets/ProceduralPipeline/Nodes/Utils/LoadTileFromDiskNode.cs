using System;
using UnityEngine;
using XNode;
using RoadNetworkGraph = QuikGraph.UndirectedGraph<RoadNetworkNode, QuikGraph.TaggedEdge<RoadNetworkNode, RoadNetworkEdge>>;

[CreateNodeMenu("Utils/Load Tile From Disk")]
public class LoadTileFromDiskNode : AsyncExtendedNode {

	[Input] public string filepath;
    [Input] public Material defaultMaterial;
    [Input] public AssetDatabaseSO assetDatabase;
	[Output] public GameObjectData[] gameobjects;
    [Output] public RoadNetworkGraph roads;
    [Output] public GlobeBoundingBox boundingBox;
    [Output] public ElevationData elevation;
	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {

        if (port.fieldName == "gameobjects")
        {
            return gameobjects;
        }
        else if (port.fieldName == "roads")
        {
            return roads;
        }
        else if (port.fieldName == "elevation")
        {
            return elevation;
        }
        else if (port.fieldName == "boundingBox")
        {
            return boundingBox;
        }
        return null; // Replace this
	}

    protected override void CalculateOutputsAsync(Action<bool> callback)
    {
        PrecomputeChunk chunk = ChunkIO.LoadInASync(GetInputValue("filepath", filepath));
        gameobjects = chunk.CreateGameObjectData(GetInputValue("defaultMaterial", defaultMaterial), GetInputValue("assetDatabase", assetDatabase));

        int width = (int)Mathf.Sqrt(chunk.terrainHeight.Length);
        float[,] height = new float[width, width];
        for (int i = 0; i < chunk.terrainHeight.Length; i++)
        {
            int x = i / width;
            height[i % width, x] = chunk.terrainHeight[i];
        }

        elevation = new ElevationData(height, chunk.coords, chunk.minHeight, chunk.maxHeight);

        roads = chunk.DeserializeRoadGraph();
        boundingBox = elevation.box;
        callback.Invoke(true);
    }

    protected override void ReleaseData()
    {
        gameobjects = null;
        elevation = null;
        roads = null;
    }
}