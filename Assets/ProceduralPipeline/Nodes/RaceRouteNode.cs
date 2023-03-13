using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using Priority_Queue;
using QuikGraph;
using UnityEngine;
using Valve.Newtonsoft.Json.Utilities;
using XNode;

using RoadNetworkGraph = QuikGraph.UndirectedGraph<RoadNetworkNode, QuikGraph.TaggedEdge<RoadNetworkNode, RoadNetworkEdge>>;

[CreateNodeMenu("Race Generator")]
public class RaceRouteNode : ExtendedNode
{
    [Input] public RoadNetworkGraph networkGraph;
    [Input] public GeoCoordinate start;
    [Input] public GeoCoordinate end;
    [Input] public GlobeBoundingBox boundingBox;
    [Input] public ElevationData elevationData;
    [Input] public GeoCoordinate[] pointsOfInterest;
    [Input] public GameObject startPrefab;
    [Input] public GameObject endPrefab;
    [Input] public GameObject checkpointPrefab;
    [Input] public float checkpointMinSpacing;
    [Input] public bool debug;
    [Output] public GameObject[] raceObjects;

    public override void CalculateOutputs(Action<bool> callback)
    {
        // get inputs
        RoadNetworkGraph roadNetwork = GetInputValue("networkGraph", networkGraph);
        GeoCoordinate s = GetInputValue("start", start);
        GeoCoordinate e = GetInputValue("end", end);
        GlobeBoundingBox bb = GetInputValue("boundingBox", boundingBox);
        ElevationData elevation = GetInputValue("elevationData", elevationData);
        GeoCoordinate[] poi = GetInputValue("pointsOfInterest", pointsOfInterest);
        GameObject startPref = GetInputValue("startPrefab", startPrefab);
        GameObject endPref = GetInputValue("endPrefab", endPrefab);
        GameObject checkpointPref = GetInputValue("checkpointPrefab", checkpointPrefab);
        float minSpacing = GetInputValue("checkpointMinSpacing", checkpointMinSpacing);
        if (minSpacing < 20) Debug.LogError("Small checkpoint spacing, this is not good :(");
        Debug.Log("Creating race");
        roadNetwork = roadNetwork.Clone(); // any modifications made to the graph won't effect other copies of the graph
        
        // create a road from each way in the list
        var path = CreateRacePath(roadNetwork, s, e, poi);
        raceObjects = CreateRaceObjectsFromPath(roadNetwork, path, startPref, endPref, checkpointPref, bb, elevation, minSpacing).ToArray();
        callback(true);
    }

    public override object GetValue(NodePort port)
    {
        return port.fieldName == "raceObjects" ? raceObjects : null;
    }

    private List<RoadNetworkNode> CreateRacePath(RoadNetworkGraph roadNetwork, GeoCoordinate s, GeoCoordinate e, GeoCoordinate[] poi)
    {
        RemoveDisconnectedComponentsFromNetwork(roadNetwork);
        RoadNetworkNode startNode = GetClosestRoadNode(roadNetwork, s);
        RoadNetworkNode endNode = GetClosestRoadNode(roadNetwork, e);

        if (debug) Debug.Log(startNode.location.ToString("0.00000") + " to " + endNode.location.ToString("0.00000"));

        List<RoadNetworkNode> networkPOI = new List<RoadNetworkNode>(new RoadNetworkNode[poi.Length + 2]);
        networkPOI[0] = startNode;
        networkPOI[^1] = endNode;
        for (int i = 0; i < poi.Length; i++)
        {
            networkPOI[i + 1] = GetClosestRoadNode(roadNetwork, poi[i]);
        }
        
        List<RoadNetworkNode> routeOverview = GetRouteOverview(networkPOI, 2 + (int)(poi.Length * 0.5f));
        
        List<RoadNetworkNode> path = new List<RoadNetworkNode>();
        for (int i = 1; i < routeOverview.Count; i++)
        {
            path.AddRange(AStar(roadNetwork, routeOverview[i-1], routeOverview[i]));
            path.RemoveAt(path.Count - 1);
        }
        path.Add(endNode);
        
        // creates markers of path through the city
        return path;
    }
    
    // creates the race!!!. Places Start, Finish and checkpoints in between
    private static List<GameObject> CreateRaceObjectsFromPath(RoadNetworkGraph roadNetwork, List<RoadNetworkNode> path,
        GameObject s, GameObject e, GameObject cp, GlobeBoundingBox bb, ElevationData elevation, float minSpacing)
    {
        // create parent game object
        GameObject raceParent = new GameObject("Race Objects");
        List<GameObject> raceItems = new List<GameObject>();
        // start point
        GameObject startObject = Instantiate(s, raceParent.transform, true);
        var startLocation = bb.ConvertGeoCoordToMeters(path[0].location);
        var startWorldPos = new Vector3(startLocation.x, 0, startLocation.y);
        startWorldPos.y = (float)elevation.SampleHeightFromPosition(startWorldPos);
        startObject.transform.position = startWorldPos;
        raceItems.Add(startObject);
        // finish line
        GameObject endObject = Instantiate(e, raceParent.transform, true);
        var endLocation = bb.ConvertGeoCoordToMeters(path[^1].location);
        var endWorldPos = new Vector3(endLocation.x, 0, endLocation.y);
        endWorldPos.y = (float)elevation.SampleHeightFromPosition(endWorldPos);
        endObject.transform.position = endWorldPos;
        raceItems.Add(endObject);
        
        // make a footprint of the race so checkpoints can be distributed evenly
        List<Vector2> footprint = new List<Vector2>();
        for (int i = 0; i < path.Count - 1; i++)
        {
            // add footprint from node i to node i + 1
            var n1 = path[i];
            var n2 = path[i + 1];
            
            var success = roadNetwork.TryGetEdge(n1, n2, out var edge);
            if (!success)
            {
                Debug.LogError("Couldn't find edge in path when there should be");
                return new List<GameObject>();
            }
            footprint.Add(n1.location);
            
            // if n2 -> n1. Add in reverse direction
            // else n1 -> n2. Add normally
            footprint.AddRange(edge.Source.Equals(n1) ? edge.Tag.edgePoints : edge.Tag.edgePoints.Reverse());

            if (i + 1 == path.Count - 1) footprint.Add(n2.location); // if next node is the final node
        }
        // make sure next checkpoint is a sensible distance away from the previous checkpoint

        var checkpointLocations = GetCheckpointPosesFromPath(minSpacing, footprint);
        Debug.Log(String.Join(", ", checkpointLocations));
        for (var i = 0; i < checkpointLocations.Count; i++)
        {
            var checkpointLocation = checkpointLocations[i];
            GameObject checkpoint = Instantiate(cp, raceParent.transform, true);
            var loc = bb.ConvertGeoCoordToMeters(checkpointLocation);
            var worldPos = new Vector3(loc.x, 0, loc.y);
            worldPos.y = (float)elevation.SampleHeightFromPosition(worldPos);
            checkpoint.transform.position = worldPos;
            checkpoint.GetComponent<CheckpointController>().checkpoint = i + 1;
            raceItems.Add(checkpoint);
        }

        endObject.GetComponent<CheckpointController>().checkpoint = checkpointLocations.Count + 1;

        return raceItems;
    }

    private static List<Vector2> GetCheckpointPosesFromPath(float minSpacing, List<Vector2> footprint)
    {
        List<Vector2> checkpointLocationsForward = new List<Vector2>();
        int forwardIndex = 0;
        List<Vector2> checkpointLocationsBackwards = new List<Vector2>();
        int backwardsIndex = footprint.Count - 1;
        bool placingCheckpoints = true;
        while
            (placingCheckpoints) // keep adding checkpoints until they can't be added anymore. Meets in the middle so finish and start aren't in weird places
        {
            // expand forwards from the start
            float runningTotal = 0;
            while (placingCheckpoints)
            {
                runningTotal +=
                    (float)GlobeBoundingBox.HaversineDistance(footprint[forwardIndex], footprint[forwardIndex + 1]);
                if (runningTotal > minSpacing) // if it's time to place a checkpoint
                {
                    // if next position forward is the next position backwards
                    if ((float)GlobeBoundingBox.HaversineDistance(footprint[forwardIndex + 1], footprint[backwardsIndex]) < minSpacing)
                    {
                        placingCheckpoints = false;
                    }
                    else // place a checkpoint at forwardIndex + 1 pos
                    {
                        checkpointLocationsForward.Add(footprint[forwardIndex + 1]);
                        forwardIndex++;
                        break; // stop placing checkpoints in the forward direction
                    }
                }

                forwardIndex++;
            }

            // expand backwards from the end
            runningTotal = 0;
            while (placingCheckpoints)
            {
                runningTotal +=
                    (float)GlobeBoundingBox.HaversineDistance(footprint[backwardsIndex], footprint[backwardsIndex - 1]);
                if (runningTotal > minSpacing) // if it's time to place a checkpoint
                {
                    // if next position backwards is the next position forwards
                    if ((float)GlobeBoundingBox.HaversineDistance(footprint[forwardIndex], footprint[backwardsIndex - 1]) < minSpacing)
                    {
                        placingCheckpoints = false;
                    }
                    else // place a checkpoint at backwards - 1 pos
                    {
                        checkpointLocationsBackwards.Add(footprint[backwardsIndex - 1]);
                        backwardsIndex--;
                        break; // stop placing checkpoints in the backwards direction
                    }
                }

                backwardsIndex--;
            }
        }

        checkpointLocationsBackwards.Reverse();
        checkpointLocationsForward.AddRange(checkpointLocationsBackwards);
        return checkpointLocationsForward;
    }

    public static List<RoadNetworkNode> AStar(RoadNetworkGraph roadNetwork, GeoCoordinate startNode, GeoCoordinate endNode)
    {
        var s = GetClosestRoadNode(roadNetwork, startNode);
        var e = GetClosestRoadNode(roadNetwork, endNode);
        return AStar(roadNetwork, s, e);
    }
    
    private static List<RoadNetworkNode> AStar(RoadNetworkGraph roadNetwork, RoadNetworkNode startNode, RoadNetworkNode endNode)
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
                        openNodes.Enqueue(neighbour, currentScore + NodeHeuristicWeight(neighbour));
                    }
                }
            }
        }

        throw new Exception("Can't find a path in the route generator (Should never happen)");
    }

    private static float RoadEdgeWeight(TaggedEdge<RoadNetworkNode, RoadNetworkEdge> edge)
    {
        float x = 0.5f * (edge.Source.location.x + edge.Target.location.x);
        float y = 0.5f * (edge.Source.location.y + edge.Target.location.y);
        return edge.Tag.length;
    }

    private static float NodeHeuristicWeight(RoadNetworkNode node)
    {
        // double distanceFromLocation = GlobeBoundingBox.HaversineDistance(new GeoCoordinate(51.452813, -2.606636, 0), new GeoCoordinate(node.location.x, node.location.y, 0));
        // TODO change distance to point to target location (This is currently acting like dijkstra's algorithm)
        return 0;
    }

    // Uses greedily finds a path visiting nodesToVisit landmarks. Start must be first node in list, End must be last node in list
    private static List<RoadNetworkNode> GetRouteOverview(List<RoadNetworkNode> nodesIn, int nodesToVisit)
    {
        if (nodesToVisit < 2) Debug.LogError("Visiting less than 2 nodes as points of interest makes no sense");
        float[,] distanceMatrix = new float[nodesIn.Count, nodesIn.Count];
        for (int i = 0; i < nodesIn.Count; i++)
        {
            for (int j = 0; j < nodesIn.Count; j++)
            {
                var a = new GeoCoordinate(nodesIn[i].location.x, nodesIn[i].location.y, 0f);
                var n = new GeoCoordinate(nodesIn[j].location.x, nodesIn[j].location.y, 0f);
                distanceMatrix[i,j] = (float)GlobeBoundingBox.HaversineDistance(a, n);
            }
        }

        List<int> path = new List<int> {0}; // start at the start
        int currentNode = 0;

        // greedily add the closest node as a location to visit
        // the first node visited will always be the start point
        // the last visited node will always be the end point
        for (int i = 1; i < nodesToVisit - 1; i++)
        {
            int closestNode = -1;
            float smallestDistance = float.MaxValue;
            for (int n = 0; n < nodesIn.Count; n++)
            {
                if (path.Contains(n)) continue; // node already visited
                if (!(distanceMatrix[currentNode, n] < smallestDistance)) continue; // node is not the closest
                smallestDistance = distanceMatrix[currentNode, n];
                closestNode = n;
            }
            path.Add(closestNode);
            currentNode = closestNode;
        }
        path.Add(nodesIn.Count - 1); // Adds the final node to the list
        return new List<RoadNetworkNode>(path.Select(n => nodesIn[n]));
    }

    // Gets the closest road node to a geolocation
    private static RoadNetworkNode GetClosestRoadNode(RoadNetworkGraph roadNetwork, GeoCoordinate s)
    {
        RoadNetworkNode baseNode = default;
        float baseNodeDistance = float.MaxValue;

        foreach (RoadNetworkNode node in roadNetwork.Vertices)
        {
            float nodeDistance = (float)((node.location.x - s.Latitude) * (node.location.x - s.Latitude) +
                (node.location.y - s.Longitude) * (node.location.y - s.Longitude));
            if (nodeDistance < baseNodeDistance)
            {
                baseNodeDistance = nodeDistance;
                baseNode = node;
            }
        }

        return baseNode;
    }
    
    // gets the distance from a dictionary. If the key doesn't exist then return the maximum possible value
    private static float GetDefaultDistance(IReadOnlyDictionary<RoadNetworkNode, Tuple<RoadNetworkNode, float>> dictionary, RoadNetworkNode key)
    {
        return dictionary.TryGetValue(key, out var val) ? val.Item2 : float.MaxValue;
    }

    private static void RemoveDisconnectedComponentsFromNetwork(RoadNetworkGraph roadNetwork)
    {
        // get the component each node of the graph belongs to
        var roadComponents = new QuikGraph.Algorithms.ConnectedComponents.ConnectedComponentsAlgorithm<RoadNetworkNode, TaggedEdge<RoadNetworkNode, RoadNetworkEdge>>(roadNetwork);
        roadComponents.Compute();
        // roadComponents.Components is a map of RoadNetworkNode to component number
        // Loops through the dict to pick the component with the most nodes
        var componentCounts = new Dictionary<int, int>();
        foreach (int componentN in roadComponents.Components.Values)
        {
            if (componentCounts.ContainsKey(componentN))
            {
                componentCounts[componentN] += 1;
            }
            else
            {
                componentCounts[componentN] = 1;
            }
        }

        int largestComponent = componentCounts.First(kv => kv.Value == componentCounts.Max(kv1 => kv1.Value)).Key;

        // removes all components from the graph which don't belong to the largest component
        foreach (RoadNetworkNode node in roadNetwork.Vertices.ToArray())
        {
            if (roadComponents.Components[node] != largestComponent) roadNetwork.RemoveVertex(node);
        }
        
    }
}