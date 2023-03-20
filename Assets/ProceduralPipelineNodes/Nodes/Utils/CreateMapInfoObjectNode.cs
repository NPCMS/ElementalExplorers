using System;
using System.Collections;
using UnityEngine;
using XNode;
using RoadNetworkGraph = QuikGraph.UndirectedGraph<RoadNetworkNode, QuikGraph.TaggedEdge<RoadNetworkNode, RoadNetworkEdge>>;


[CreateNodeMenu("Util/Create Map Info Object")]
public class CreateMapInfoObjectNode : AbstractUtilsNode
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

    public override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        RoadNetworkGraph roadNetwork = GetInputValue("networkGraph", networkGraph).Clone();
        GlobeBoundingBox bb = GetInputValue("boundingBox", boundingBox);
        ElevationData elevation = GetInputValue("elevationData", elevationData);

        result = new GameObject("Road Network Container");
        MapInfoContainer mapInfoContainer = result.AddComponent<MapInfoContainer>();
        mapInfoContainer.roadNetwork = roadNetwork;
        mapInfoContainer.elevation = elevation;
        mapInfoContainer.bb = bb;
        callback(true);
        yield break;
    }

    public override void Release()
    {
        networkGraph = null;
        elevationData = null;
        result = null;
    }
}