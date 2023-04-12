using System;
using System.Collections;
using System.Collections.Generic;
using XNode;
using RoadNetworkGraph = QuikGraph.UndirectedGraph<RoadNetworkNode, QuikGraph.TaggedEdge<RoadNetworkNode, RoadNetworkEdge>>;

[CreateNodeMenu("World/Output BBox")]
public class OutputBBox : SyncOutputNode 
{
    [Input] public GlobeBoundingBox bbox;

    // Return the correct value of an output port when requested
    public override object GetValue(NodePort port)
    {
        return null;
    }

    public override void ApplyOutput(PipelineRunner manager)
    {
        GlobeBoundingBox boundingBox = GetInputValue("bbox", bbox);
        manager.AddBoundingBox(boundingBox);
    }

    public override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        callback.Invoke(true);
        yield break;
    }

    public override void Release()
    {
        
    }
}