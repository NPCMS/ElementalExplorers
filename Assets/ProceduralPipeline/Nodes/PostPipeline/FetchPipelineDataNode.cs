using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;
using RoadNetworkGraph = QuikGraph.UndirectedGraph<RoadNetworkNode, QuikGraph.TaggedEdge<RoadNetworkNode, RoadNetworkEdge>>;

[CreateNodeMenu("PostPipeline/Fetch Pipeline info")]
public class FetchPipelineDataNode : SyncInputNode
{
    [Output] public RoadNetworkGraph roadNetwork;
    [Output] public ElevationData elevationData;
    [Output] public List<GeoCoordinate> pois;

    private Dictionary<Vector2Int, ElevationData> elevationDataDict;
    
    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "elevationData") return elevationData;
        if (port.fieldName == "roadNetwork") return roadNetwork;
        if (port.fieldName == "pois") return pois;
        return null;
    }

    public override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        // convert Dictionary<Vector2Int, ElevationData> to ElevationData
        Vector2Int topLeft = new Vector2Int(elevationDataDict.Keys.Min(k => k.x), elevationDataDict.Keys.Min(k => k.y));
        
        int width = elevationDataDict.Keys.Max(k => k.x) - elevationDataDict.Keys.Min(k => k.x) + 1;
        int height = elevationDataDict.Keys.Max(k => k.y) - elevationDataDict.Keys.Min(k => k.y) + 1;
        int resolution = elevationDataDict.Values.First().height.GetLength(0);

        var newBoundingBox = new GlobeBoundingBox(
            elevationDataDict.Values.Max(b => b.box.north),
            elevationDataDict.Values.Max(b => b.box.east),
            elevationDataDict.Values.Min(b => b.box.south),
            elevationDataDict.Values.Min(b => b.box.west)
        );
        
        float[,] newHeights = new float[width * resolution, height * resolution];

        yield return null;

        foreach (KeyValuePair<Vector2Int,ElevationData> elevationPair in elevationDataDict)
        {
            var xOffset = resolution * (elevationPair.Key.x - topLeft.x);
            var yOffset = resolution * (1 - (elevationPair.Key.y - topLeft.y));
            if (yOffset < 0)
                throw new Exception("The above line expects a 2x2 anything else will break it, please fix this");
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    var d = (float)(elevationPair.Value.minHeight + elevationPair.Value.height[y, x] * (elevationPair.Value.maxHeight - elevationPair.Value.minHeight));
                    newHeights[yOffset + y, xOffset + x] = d;
                }
            }
        }

        double newMax = elevationDataDict.Values.Max(e => e.maxHeight);
        double newMin = elevationDataDict.Values.Min(e => e.minHeight);
        
        for (int y = 0; y < resolution * height; y++)
        {
            for (int x = 0; x < resolution * width; x++)
            {
                newHeights[y, x] = (newHeights[y, x] - (float)newMin) / (float)(newMax - newMin);
            }
        }

        elevationData = new ElevationData(newHeights, newBoundingBox, newMin, newMax);
        
        callback.Invoke(true);
    }

    public override void Release()
    {
        elevationData = null;
        roadNetwork = null;
        pois = null;
    }

    public override void ApplyInputs(PipelineRunner manager)
    {
        elevationDataDict = manager.FetchElevationData();
        roadNetwork = manager.FetchRoadNetworkGraph();
        pois = manager.FetchPois();
    }
}
