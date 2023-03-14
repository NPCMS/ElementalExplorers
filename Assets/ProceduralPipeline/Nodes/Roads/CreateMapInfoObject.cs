using System;
using UnityEngine;
using XNode;
using RoadNetworkGraph = QuikGraph.UndirectedGraph<RoadNetworkNode, QuikGraph.TaggedEdge<RoadNetworkNode, RoadNetworkEdge>>;


public class CreateMapInfoObject : ExtendedNode
{

    [Input] public RoadNetworkGraph networkGraph;
    [Input] public GlobeBoundingBox boundingBox;
    [Input] public ElevationData elevationData;
    [Output] public GameObject result;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "result")
        {
            return result;
        }
        return null;
    }

    public override void CalculateOutputs(Action<bool> callback)
    {
        RoadNetworkGraph roadNetwork = GetInputValue("networkGraph", networkGraph);
        GlobeBoundingBox bb = GetInputValue("boundingBox", boundingBox);
        ElevationData elevation = GetInputValue("elevationData", elevationData);

        result = new GameObject("Road Network Container");
        MapInfoContainer mapInfoContainer = result.AddComponent<MapInfoContainer>();
        mapInfoContainer.roadNetwork = roadNetwork;
        mapInfoContainer.elevation = elevation;
        mapInfoContainer.bb = bb;
        callback(true);
    }
}
