using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;
using RoadNetworkGraph = QuikGraph.UndirectedGraph<RoadNetworkNode, QuikGraph.TaggedEdge<RoadNetworkNode, RoadNetworkEdge>>;

[CreateNodeMenu("Output/Save Tile to Disk")]
public class SaveTileToDiskNode : SyncOutputNode {
    [Input] public Vector2Int tile;
    [Input] public GameObject[] buildings;
    [Input] public RoadNetworkGraph roads;
    [Input] public GameObject[] roofs;
    [Input] public BuildifyCityData prefabs;
    [Input] public ElevationData elevation;
    [Input] public AssetDatabaseSO assetdatabase;
    [Input] public List<GeoCoordinate> pois;

    // Return the correct value of an output port when requested
    public override object GetValue(NodePort port) {
        return null; // Replace this
    }

    public override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        callback.Invoke(true);
        yield break;
    }

    public override void ApplyOutput(PipelineRunner manager)
    {
        GameObject[] buildingGos = GetInputValue("buildings", buildings);
        GameObject[] roofGos = GetInputValue("roofs", roofs);
        BuildifyCityData buildifyData = GetInputValue("prefabs", prefabs);
        PrecomputeChunk chunk = new PrecomputeChunk(buildingGos, roofGos, buildifyData, GetInputValue("elevation", elevation), GetInputValue("roads", roads), GetInputValue("assetdatabase", assetdatabase), GetInputValue("pois", pois));
        ChunkIO.Save(GetInputValue("tile", tile).ToString() + ".rfm", chunk);

        for (int i = 0; i < buildingGos.Length; i++)
        {
            DestroyImmediate(buildingGos[i]);
        }

        for (int i = 0; i < roofGos.Length; i++)
        {
            DestroyImmediate(roofGos[i]);
        }
    }

    public override void Release()
    {
        buildings = null;
        roads = null;
        prefabs = null;
        elevation = null;
    }
}