using System;
using QuikGraph.Serialization;
using Unity.VisualScripting;
using UnityEngine;

using RoadNetworkGraph = QuikGraph.UndirectedGraph<RoadNetworkNode, QuikGraph.TaggedEdge<RoadNetworkNode, RoadNetworkEdge>>;

public class MapInfoContainer : MonoBehaviour
{
    public RoadNetworkGraph roadNetwork;
    public GlobeBoundingBox bb;
    public ElevationData elevation;

    private void Start()
    {
        Debug.Log("Road network size: " + roadNetwork.EdgeCount);
    }
}
