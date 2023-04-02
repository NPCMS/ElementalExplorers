using System;
using System.Collections;
using UnityEngine;
using XNode;
using RoadNetworkGraph = QuikGraph.UndirectedGraph<RoadNetworkNode, QuikGraph.TaggedEdge<RoadNetworkNode, RoadNetworkEdge>>;

[CreateNodeMenu("Output/Save Tile to Disk")]
public class SaveTileToDiskNode : SyncOutputNode {
    [Input] public Vector2Int tile;
    [Input] public GameObject[] buildings;
    [Input] public OSMRoadsData[] roads;
    [Input] public ElevationData elevation;
    [Input] public AssetDatabaseSO assetdatabase;

    // Return the correct value of an output port when requested
    public override object GetValue(NodePort port) {
        return null; // Replace this
    }

    public override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        callback.Invoke(true);
        yield break;
    }

    public override void ApplyOutput(AsyncPipelineManager manager)
    {
        GameObject[] buildingGos = GetInputValue("buildings", buildings);
        PrecomputeChunk chunk = new PrecomputeChunk(buildingGos, GetInputValue("elevation", elevation), GetInputValue("roads", roads), GetInputValue("assetdatabase", assetdatabase));
        ChunkIO.Save(GetInputValue("tile", tile).ToString() + ".rfm", chunk);

        for (int i = 0; i < buildingGos.Length; i++)
        {
            DestroyImmediate(buildingGos[i]);
        }
    }

    public override void Release()
    {
        buildings = null;
        roads = null;
        elevation = null;
    }
}