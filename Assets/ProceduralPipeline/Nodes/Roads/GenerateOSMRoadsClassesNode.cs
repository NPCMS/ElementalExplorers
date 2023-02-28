using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using XNode;

[Serializable]
public class OSMRoadsData
{
    public List<Vector2> footprint;
    public Vector2 center;
    public RoadType roadType;
    public string name;

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

    public OSMRoadsData(List<Vector3> footprint, OSMTags tags)
    {
        this.footprint = new List<Vector2>();
        for (int i = 0; i < footprint.Count; i++)
        {
            this.footprint.Add(new Vector3(footprint[i].x, footprint[i].z));
        }
        name = tags.name == null ? "Unnamed Road" : tags.name;
        MakeRelative();
    }

    public OSMRoadsData(List<Vector3> footprint, Vector2[][] holes, OSMTags tags)
    {
        this.footprint = new List<Vector2>();
        for (int i = 0; i < footprint.Count; i++)
        {
            this.footprint.Add(new Vector2(footprint[i].x, footprint[i].z));
        }
        this.name = tags.name == null ? "Unnamed Road" : tags.name;
        MakeRelative();
    }
}


[CreateNodeMenu("Roads/Generate OSM Roads Data Classes")]
public class GenerateOSMRoadsClassesNode : ExtendedNode
{

    [Input] public OSMRoadWay[] OSMWays;
    [Input] public GlobeBoundingBox boundingBox;
    [Input] public bool debug;

    [Output] public OSMRoadsData[] roadsData;

    private OSMRoadNode[] roadNodesArray;

    // Return the correct value of an output port when requested
    public override object GetValue(NodePort port)
    {
        return port.fieldName == "roadsData" ? roadsData : null;
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
            for (int i = 0; i < osmWay.nodes.Length - 1; i++)
            {
                ulong nodeRef = osmWay.nodes[i];
                
                if (nodesToRequest.Contains(nodeRef)) continue;
                
                nodesToRequest.Add(nodeRef);
                
                // batches requests due to max query size
                if (query.Length > 1900) // if batch is getting too large add batch to list and create a new batch
                {
                    nodeBatches.Add(query);
                    query = "data=[out:json][timeout:" + "1000" + "];(node(id:";
                }
                if (query != "data=[out:json][timeout:" + "1000" + "];(node(id:")
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

        List<OSMRoadsData> roads = new List<OSMRoadsData>();
        
        foreach (OSMRoadWay osmWay in ways)
        {
            bool allNodesFound = true;
            List<Vector3> footprint = new List<Vector3>();
            if (osmWay.nodes != null)
            {
                for (int j = 0; j < osmWay.nodes.Length - 1; j++)
                {
                    ulong nodeRef = osmWay.nodes[j];
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
                        footprint.Add(new Vector3(meterPoint.x, geoPoint.Altitude, meterPoint.y));
                    }
                }
            }
            if (!allNodesFound)
            {
                Debug.LogWarning("not found all the nodes for ways, missing nodes in way:" + osmWay.id);
            }

            // create roads data objects
            roads.Add(new OSMRoadsData(footprint, osmWay.tags));
        }
        if (debug) Debug.Log("Now we have roads + " + roads.Count);
        roadsData = roads.ToArray(); // set output variable to roads list
        callback.Invoke(true); // all processing done so invoke callback, sending data to next node
    }
    
    public override void Release()
    {
        base.Release();
        OSMWays = null;
        roadsData = null;
    }

    public override void CalculateOutputs(Action<bool> callback)
    {
        // get inputs
        OSMRoadWay[] ways = GetInputValue("OSMWays", OSMWays);
        GlobeBoundingBox bb = GetInputValue("boundingBox", boundingBox);

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
    