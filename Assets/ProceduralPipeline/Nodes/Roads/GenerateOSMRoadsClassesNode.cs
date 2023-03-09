using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuikGraph;
using UnityEngine;
using UnityEngine.Networking;
using XNode;

[Serializable]
public class OSMRoadsData
{
    public List<Vector2> footprint;
    public Vector2 center;
    public RoadType roadType;
    public String name;

    private void MakeRelative()
    {
        center = Vector2.zero;
        foreach (Vector2 node in footprint)
        {
            center += node;
        }

        center /= footprint.Count;

        for (int i = 0; i < footprint.Count; i++)
        {
            footprint[i] -= center;
        }
    }

    public OSMRoadsData(List<Vector2> footprint)
    {
        this.footprint = footprint;
        this.name = "Road";
        MakeRelative();
    }
}


[CreateNodeMenu("Roads/Generate OSM Roads Data Classes")]
public class GenerateOSMRoadsClassesNode : ExtendedNode
{

    [Input] public OSMRoadWay[] OSMWays;
    [Input] public GlobeBoundingBox boundingBox;
    [Input] public bool debug;
    [Input] public int timeout;

    [Output] public UndirectedGraph<RoadNetworkNode, TaggedEdge<RoadNetworkNode, RoadNetworkEdge>> roadsGraph;

    private OSMRoadNode[] roadNodesArray;
    private int timeoutValue = 0;

    // Return the correct value of an output port when requested
    public override object GetValue(NodePort port)
    {
        return port.fieldName == "roadsGraph" ? roadsGraph : null;
    }

    private void RequestNodesForWays(List<string> stringsToSend, OSMRoadWay[] ways,  Dictionary<ulong, GeoCoordinate> nodesDict, GlobeBoundingBox bb, Action<bool> callback)
    {
        const string endpoint = "https://overpass.kumi.systems/api/interpreter/?";
        const string endOfQuery = "););out body;>;out skel qt;";
        // send first OSM query in list
        var osmQuery = stringsToSend[0];
        stringsToSend.Remove(osmQuery);
        string sendURL = endpoint + osmQuery + endOfQuery;
        if (debug) Debug.Log("sending request: " + sendURL);
        UnityWebRequest request = UnityWebRequest.Get(sendURL);
        UnityWebRequestAsyncOperation operation =  request.SendWebRequest();

        operation.completed += _ =>
        {
            // if strings to send has more string to send, send them recursively
            // else all strings must be sent so go to next step
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning(request.error);
            }
            else
            {
                OSMRoadsNodeContainer result = JsonUtility.FromJson<OSMRoadsNodeContainer>(request.downloadHandler.text);

                if (debug) Debug.Log("Received " + result.elements.Length + " nodes from the server");

                // add nodes to the node dict
                foreach (OSMRoadNode osmNode in result.elements)
                {
                    if(!nodesDict.ContainsKey(osmNode.id))
                    {
                        nodesDict.Add(osmNode.id, new GeoCoordinate(osmNode.lat, osmNode.lon, 0));
                    }
                    else
                    {
                        Debug.LogWarning("Node already in node dict, this should never happen");
                    }
                }
                
                // if there are still requests to be sent send the next one
                if (stringsToSend.Count > 0)
                {
                    RequestNodesForWays(stringsToSend, ways, nodesDict, bb, callback);
                }
                else
                { // all nodes have been requested and added to nodesDict
                    if (debug) Debug.Log("All nodes processed");
                    CreateRoadsFromNodes(ways, nodesDict, bb, callback);
                }
            }
            request.Dispose();
        };
    }

    private void CreateRoadsFromWays(OSMRoadWay[] ways, GlobeBoundingBox bb, Action<bool> callback)
    {
        List<string> nodeBatches = new List<string>();
        string query = "data=[out:json][timeout:" + "1000" + "];(node(id:";
        HashSet<ulong> nodesToRequest = new HashSet<ulong>();

        foreach (OSMRoadWay osmWay in ways)
        {
            // -1 as there is a node repeat too close the polygon
            if (osmWay.nodes == null) continue;
            
            // Debug.Log(osmWay.nodes.Length);
            foreach (ulong nodeRef in osmWay.nodes)
            {
                if (nodesToRequest.Contains(nodeRef)) continue;
                
                nodesToRequest.Add(nodeRef);
                
                // batches requests due to max query size
                if (query.Length > 1900) // if batch is getting too large add batch to list and create a new batch
                {
                    nodeBatches.Add(query);
                    query = "data=[out:json][timeout:" + this.timeoutValue + "];(node(id:";
                }
                if (query != "data=[out:json][timeout:" + this.timeoutValue + "];(node(id:")
                {
                    query += ",";
                }
                query += nodeRef;
            }
        }
        nodeBatches.Add(query); // adds the final batch

        if (debug) Debug.Log("Sending " + nodeBatches.Count + " requests");
        RequestNodesForWays(nodeBatches, ways, new Dictionary<ulong, GeoCoordinate>(), bb, callback);
    }

    private void CreateRoadsFromNodes(OSMRoadWay[] ways, Dictionary<ulong, GeoCoordinate> nodesDict, GlobeBoundingBox bb, Action<bool> callback)
    {
        if (debug)
        {
            Debug.Log("Creating roads from nodes");
            Debug.Log("Nodes loaded: " + nodesDict.Count + " for " + ways.Length + " ways");
        }

        var roadGraph = new UndirectedGraph<RoadNetworkNode, TaggedEdge<RoadNetworkNode, RoadNetworkEdge>>();
        
        foreach (OSMRoadWay osmWay in ways)
        {
            bool allNodesFound = true;
            List<Vector2> footprint = new List<Vector2>();
            if (osmWay.nodes == null) continue;
            if (osmWay.tags.area == "yes")
            {
                if (debug) Debug.Log("removing from osm way list area");
                continue;
            }

            foreach (var nodeRef in osmWay.nodes)
            {
                if (!nodesDict.ContainsKey(nodeRef))
                {
                    allNodesFound = false;
                }
                else
                {
                    // lookup node
                    GeoCoordinate geoPoint = nodesDict[nodeRef];
                    // convert to meters
                    Vector2 meterPoint = ConvertGeoCoordToMeters(geoPoint, bb);
                    // add to footprint
                    footprint.Add(new Vector2(meterPoint.x, meterPoint.y));
                }
            }

            if (!allNodesFound)
            {
                Debug.LogWarning("not found all the nodes for ways, missing nodes in way:" + osmWay.id);
                continue;
            }
            
            // add nodes to graph
            for (int i = 1; i < osmWay.nodes.Length; i++)
            {
                var v1 = footprint[i - 1];
                var v2 = footprint[i];
                var n1 = new RoadNetworkNode(v1, osmWay.nodes[i-1]);
                var n2 = new RoadNetworkNode(v2, osmWay.nodes[i]);
                roadGraph.AddVerticesAndEdge(new TaggedEdge<RoadNetworkNode, RoadNetworkEdge>(
                    n1, n2, new RoadNetworkEdge(Vector2.Distance(v1, v2), new RoadType(), new Vector2[]{})
                ));
            }
        }
        if (debug) Debug.Log("Road graph created with " + roadGraph.VertexCount + " nodes and " + roadGraph.EdgeCount + " edges");
        
        MergeRoads(roadGraph);
        
        if (debug) Debug.Log("Merged road graph with " + roadGraph.VertexCount + " nodes and " + roadGraph.EdgeCount + " edges");
        
        roadsGraph = roadGraph;
        
        Debug.Log(roadGraph.EdgeCount + " * " + roadGraph.VertexCount);
        
        callback.Invoke(true); // all processing done so invoke callback, sending data to next node
    }

    
    
    
    private void MergeRoads(UndirectedGraph<RoadNetworkNode, TaggedEdge<RoadNetworkNode, RoadNetworkEdge>> roadGraph)
    {
        var nodesToMerge = roadGraph.Vertices.Where(node => roadGraph.AdjacentDegree(node) == 2).ToList();
        foreach (RoadNetworkNode node in nodesToMerge)
        {
            var e1 = roadGraph.AdjacentEdge(node, 0);
            var e2 = roadGraph.AdjacentEdge(node, 1);
            RoadType resultantType = e1.Tag.type; //TODO change this when tags are working properly
            float newLength = e1.Tag.length + e2.Tag.length;
            List<Vector2> newEdgePoints = new List<Vector2>();
            RoadNetworkNode source;
            RoadNetworkNode target;
            // new edge is from e1 node to e2 node with node merged in the middle
            if (e1.Source.Equals(node))
            {
                source = e1.Target;
                for (int i = e1.Tag.edgePoints.Length - 1; i >= 0; i--)
                {
                    newEdgePoints.Add(e1.Tag.edgePoints[i]);
                }
            }
            else
            {
                source = e1.Source;
                newEdgePoints.AddRange(e1.Tag.edgePoints);
            }
            newEdgePoints.Add(node.location);
            if (e2.Source.Equals(node))
            {
                target = e2.Target;
                newEdgePoints.AddRange(e2.Tag.edgePoints);
            }
            else
            {
                target = e2.Source;
                for (int i = e2.Tag.edgePoints.Length - 1; i >= 0; i--)
                {
                    newEdgePoints.Add(e2.Tag.edgePoints[i]);
                }
            }

            roadGraph.RemoveVertex(node);
            roadGraph.AddEdge(new TaggedEdge<RoadNetworkNode, RoadNetworkEdge>(source, target,
                new RoadNetworkEdge(newLength, resultantType, newEdgePoints.ToArray())));
        }
    }

    public override void Release()
    {
        base.Release();
        OSMWays = null;
        roadsGraph = null;
    }

    public override void CalculateOutputs(Action<bool> callback)
    {
        // get inputs
        OSMRoadWay[] ways = GetInputValue("OSMWays", OSMWays);
        GlobeBoundingBox bb = GetInputValue("boundingBox", boundingBox);
        timeoutValue = GetInputValue("timeout", timeout);
        // create a road from each way in the list
        CreateRoadsFromWays(ways, bb, callback);
    }

    private static Vector2 ConvertGeoCoordToMeters(GeoCoordinate coord, GlobeBoundingBox bb)
    {
        double width = GlobeBoundingBox.LatitudeToMeters(bb.north - bb.south);
        float verticalDst = Mathf.InverseLerp((float)bb.south, (float)bb.north, (float)coord.Latitude) * (float)width;
        float horizontalDst = Mathf.InverseLerp((float)bb.west, (float)bb.east, (float)coord.Longitude) * (float)width;
        return new Vector2(horizontalDst, verticalDst);
    }
}


[Serializable]
public struct RoadType
{
    public string surface;
    public string type;
    public string highwayType;

}

[Serializable]
public struct RoadNetworkEdge
{
    public float length;
    public RoadType type;
    public Vector2[] edgePoints;

    public RoadNetworkEdge(float length, RoadType type, Vector2[] edgePoints)
    {
        this.length = length;
        this.type = type;
        this.edgePoints = edgePoints;
    }
}

[Serializable]
public struct RoadNetworkNode
{
    public Vector2 location;
    public ulong id;

    public RoadNetworkNode(Vector2 location, ulong id)
    {
        this.location = location;
        this.id = id;
    }

    public bool Equals(RoadNetworkNode other)
    {
        return id == other.id;
    }

    public override bool Equals(object obj)
    {
        return obj is RoadNetworkNode other && Equals(other);
    }

    public override int GetHashCode()
    {
        return id.GetHashCode();
    }
}