using System;
using System.Collections.Generic;
using System.Linq;
using Priority_Queue;
using QuikGraph;
using UnityEngine;
using XNode;

[CreateNodeMenu("Race Generator")]
public class RaceRouteNode : ExtendedNode
{
    [Input] public UndirectedGraph<RoadNetworkNode, TaggedEdge<RoadNetworkNode, RoadNetworkEdge>> networkGraph;
    [Input] public GeoCoordinate start;
    [Input] public GeoCoordinate end;
    [Input] public bool debug;
    [Input] public GlobeBoundingBox boundingBox;
    [Output] public GameObject[] raceObjects;

    public override void CalculateOutputs(Action<bool> callback)
    {
        // get inputs
        UndirectedGraph<RoadNetworkNode, TaggedEdge<RoadNetworkNode, RoadNetworkEdge>> roadNetwork =
            GetInputValue("networkGraph", networkGraph);
        GeoCoordinate s = GetInputValue("start", start);
        GeoCoordinate e = GetInputValue("end", end);
        GlobeBoundingBox bb = GetInputValue("boundingBox", boundingBox);
        
        Debug.Log("Creating race");
        
        // create a road from each way in the list
        raceObjects = CreateRace(roadNetwork, s, e, bb);
        callback(true);
    }

    public override object GetValue(NodePort port)
    {
        return port.fieldName == "raceObjects" ? raceObjects : null;
    }

    private GameObject[] CreateRace(UndirectedGraph<RoadNetworkNode, TaggedEdge<RoadNetworkNode, RoadNetworkEdge>> roadNetwork,
        GeoCoordinate s, GeoCoordinate e, GlobeBoundingBox bb)
    {
        (RoadNetworkNode startNode, RoadNetworkNode endNode) = GetRaceStartEnd(roadNetwork, s, e);

        Debug.Log(startNode.location.ToString("0.00000") + " to " + endNode.location.ToString("0.00000"));
        
        List<RoadNetworkNode> path = AStar(roadNetwork, startNode, endNode);
        List<GameObject> raceItems = new List<GameObject>();
        
        foreach (RoadNetworkNode node in path)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var location = bb.ConvertGeoCoordToMeters(node.location);
            marker.transform.position = new Vector3(location.x, 0, location.y);
            marker.name = "Marker";
            raceItems.Add(marker);
        }
        return raceItems.ToArray();
    }

    private List<RoadNetworkNode> AStar(UndirectedGraph<RoadNetworkNode, TaggedEdge<RoadNetworkNode, RoadNetworkEdge>> roadNetwork,
        RoadNetworkNode startNode, RoadNetworkNode endNode)
    {
        // dict of type: node -> prev node, distance
        var visitedNodes = new Dictionary<RoadNetworkNode, Tuple<RoadNetworkNode, float>>();
        var openNodes = new StablePriorityQueue<RoadNetworkNode>(roadNetwork.VertexCount);
        
        openNodes.Enqueue(startNode, 0);
        visitedNodes[startNode] = new Tuple<RoadNetworkNode, float>(null, 0);

        while (openNodes.Count > 0)
        {
            RoadNetworkNode nextNode = openNodes.Dequeue();

            if (nextNode.Equals(endNode))
            {
                List<RoadNetworkNode> path = new List<RoadNetworkNode>();
                RoadNetworkNode currentNode = nextNode;
                while (currentNode != null)
                {
                    path.Insert(0, currentNode);
                    var prevNode = visitedNodes[currentNode].Item1;
                    currentNode = prevNode;
                }
                return path;
            }
            
            foreach (var edge in roadNetwork.AdjacentEdges(nextNode))
            {
                var neighbour = edge.Source.Equals(nextNode) ? edge.Target : edge.Source;

                float currentScore = visitedNodes[nextNode].Item2 + RoadEdgeWeight(edge);
                if (currentScore < GetDefaultDistance(visitedNodes, neighbour))
                {
                    visitedNodes[neighbour] = new Tuple<RoadNetworkNode, float>(nextNode, currentScore);
                    if (!openNodes.Contains(neighbour))
                    {
                        openNodes.Enqueue(neighbour, currentScore + RoadEdgeWeight(edge));
                    }
                }
            }
        }

        throw new Exception("Can't find a path in the route generator (Should never happen)");
    }

    private static float RoadEdgeWeight(TaggedEdge<RoadNetworkNode, RoadNetworkEdge> edge)
    {
        return edge.Tag.length;
    }

    private static float nodeHeuristicWeight(RoadNetworkNode edge)
    {
        return 0;
    }

    private (RoadNetworkNode, RoadNetworkNode) GetRaceStartEnd(
        UndirectedGraph<RoadNetworkNode, TaggedEdge<RoadNetworkNode, RoadNetworkEdge>> roadNetwork,
        GeoCoordinate s, GeoCoordinate e)
    {
        RoadNetworkNode startNode = default;
        float startNodeDistance = float.MaxValue;
        RoadNetworkNode endNode = default;
        float endNodeDistance = float.MaxValue;

        foreach (RoadNetworkNode node in roadNetwork.Vertices)
        {
            float startDistance = (float)((node.location.x - s.Latitude) * (node.location.x - s.Latitude) +
                (node.location.y - s.Longitude) * (node.location.y - s.Longitude));
            float endDistance = (float)((node.location.x - e.Latitude) * (node.location.x - e.Latitude) +
                (node.location.y - e.Longitude) * (node.location.y - e.Longitude));
            if (startDistance < startNodeDistance)
            {
                startNodeDistance = startDistance;
                startNode = node;
            }

            if (endDistance < endNodeDistance)
            {
                endNodeDistance = endDistance;
                endNode = node;
            }
        }

        return (startNode, endNode);
    }
    
    private static float GetDefaultDistance(IReadOnlyDictionary<RoadNetworkNode, Tuple<RoadNetworkNode, float>> dictionary, RoadNetworkNode key)
    {
        return dictionary.TryGetValue(key, out var val) ? val.Item2 : float.MaxValue;
    }
}