using System;
using System.Collections;
using XNode;
using RoadNetworkGraph = QuikGraph.UndirectedGraph<RoadNetworkNode, QuikGraph.TaggedEdge<RoadNetworkNode, RoadNetworkEdge>>;

[CreateNodeMenu("Roads/Output Graph")]
public class OutputRoadGraphNode : SyncOutputNode 
{
    [Input] public RoadNetworkGraph roadNetwork;

    // Return the correct value of an output port when requested
    public override object GetValue(NodePort port)
    {
        return null;
    }

    public override void ApplyOutput(PipelineRunner manager)
    {
        RoadNetworkGraph roads = GetInputValue("roadNetwork", roadNetwork);
        manager.AddRoadNetworkSection(roads);
    }

    public override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        callback.Invoke(true);
        yield break;
    }

    public override void Release()
    {
        roadNetwork = null;
    }
}