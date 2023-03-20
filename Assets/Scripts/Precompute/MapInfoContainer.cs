using UnityEngine;

using RoadNetworkGraph = QuikGraph.UndirectedGraph<ProceduralPipelineNodes.Nodes.Roads.RoadNetworkNode, QuikGraph.TaggedEdge<ProceduralPipelineNodes.Nodes.Roads.RoadNetworkNode, ProceduralPipelineNodes.Nodes.Roads.RoadNetworkEdge>>;

public class MapInfoContainer : MonoBehaviour
{
    public RoadNetworkGraph roadNetwork;
    public GlobeBoundingBox bb;
    public ElevationData elevation;
}
