using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;
using RoadNetworkGraph = QuikGraph.UndirectedGraph<RoadNetworkNode, QuikGraph.TaggedEdge<RoadNetworkNode, RoadNetworkEdge>>;

[CreateNodeMenu("Utils/Apply precompute")]
public class LoadTileFromDiskNode : SyncExtendedNode {

	[Input] public PrecomputeChunk chunks;
    [Input] public Material defaultMaterial;
    [Input] public AssetDatabaseSO assetDatabase;
    [Output] public RoadNetworkGraph roads;
	[Output] public GameObjectData[] walls;
    [Output] public GameObjectData[] roofs;
    [Output] public GameObjectData[] buildifyPrefabs;
    [Output] public GlobeBoundingBox boundingBox;
    [Output] public ElevationData elevation;
    [Output] public List<GeoCoordinate> pois;
    
	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port)
    {
        return port.fieldName switch
        {
            "walls" => walls,
            "roofs" => roofs,
            "buildifyPrefabs" => buildifyPrefabs,
            "roads" => roads,
            "elevation" => elevation,
            "boundingBox" => boundingBox,
            "pois" => pois,
            _ => null
        };
    }

    public override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        PrecomputeChunk chunk = GetInputValue("chunks", chunks);
        yield return new WaitForEndOfFrame();
        walls = chunk.CreateGameObjectData(GetInputValue("defaultMaterial", defaultMaterial), GetInputValue("assetDatabase", assetDatabase));
        yield return new WaitForEndOfFrame();
        roofs = chunk.CreateRoofGameObjectData(GetInputValue("defaultMaterial", defaultMaterial), GetInputValue("assetDatabase", assetDatabase));
        yield return new WaitForEndOfFrame();
        buildifyPrefabs = chunk.GetBuildifyData(assetDatabase);
        yield return new WaitForEndOfFrame();
        int width = (int)Mathf.Sqrt(chunk.terrainHeight.Length);
        float[,] height = new float[width, width];
        for (int i = 0; i < chunk.terrainHeight.Length; i++)
        {
            int x = i / width;
            height[i % width, x] = chunk.terrainHeight[i];
        }
        yield return new WaitForEndOfFrame();

        elevation = new ElevationData(height, chunk.coords, chunk.minHeight, chunk.maxHeight);

        roads = chunk.DeserializeRoadGraph();
        boundingBox = elevation.box;
        pois = chunk.GetPois();
        callback.Invoke(true);
    }

    public override void Release()
    {
        walls = null;
        roofs = null;
        buildifyPrefabs = null;
        elevation = null;
        roads = null;
    }
}