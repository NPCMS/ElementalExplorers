using UnityEngine;

using RoadNetworkGraph = QuikGraph.UndirectedGraph<RoadNetworkNode, QuikGraph.TaggedEdge<RoadNetworkNode, RoadNetworkEdge>>;

public class MapInfoContainer : MonoBehaviour
{
    public RoadNetworkGraph roadNetwork;
    public GlobeBoundingBox bb;
    public ElevationData elevation;
}
