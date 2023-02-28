using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using XNode;

[Serializable]
public class OSMRoadsData
{
    public List<Vector2> footprint;
    public List<float> elevations;
    public Vector2[][] holes;
    public Vector2 center;
    public RoadType roadType;
    public string name;
    public float elevation;

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

        foreach (var hole in holes)
        {
            for (int j = 0; j < hole.Length; j++)
            {
                hole[j] -= center;
            }
        }
    }

    public OSMRoadsData(List<Vector3> footprint, OSMTags tags)
    {
        holes = Array.Empty<Vector2[]>();
        this.footprint = new List<Vector2>();
        elevations = new List<float>();
        for (int i = 0; i < footprint.Count; i++)
        {
            this.footprint.Add(new Vector3(footprint[i].x, footprint[i].z));
            elevations.Add(footprint[i].y);
        }
        name = tags.name == null ? "Unnamed Road" : tags.name;
        MakeRelative();

        SetElevation(footprint);
    }

    public OSMRoadsData(List<Vector3> footprint, Vector2[][] holes, OSMTags tags)
    {
        this.holes = holes;
        this.footprint = new List<Vector2>();
        for (int i = 0; i < footprint.Count; i++)
        {
            this.footprint.Add(new Vector2(footprint[i].x, footprint[i].z));
        }
        this.name = tags.name == null ? "Unnamed Road" : tags.name;
        MakeRelative();
        SetElevation(footprint);
    }

    private void SetElevation(List<Vector3> footprint)
    {
        elevation = float.MaxValue;
        float maxElevation = float.MinValue;
        foreach (Vector3 node in footprint)
        {
            elevation = Mathf.Min(node.y, elevation);
            maxElevation = Mathf.Max(node.y, maxElevation);
        }
        this.elevation = maxElevation;

    }
}


[CreateNodeMenu("Roads/Generate OSM Roads Data Classes")]
public class GenerateOSMRoadsClassesNode : ExtendedNode
{

    [Input] public OSMRoadNode[] OSMNodes;
    [Input] public OSMRoadWay[] OSMWays;
    [Input] public OSMRoadRelation[] OSMRelations;
    [Input] public GlobeBoundingBox boundingBox;
    [Input] public ElevationData elevationData;
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

                // set height of road nodes
                for (int i = 0; i < result.elements.Length; i++)
                {
                    result.elements[i].altitude = GetHeightOfPoint(result.elements[i], null);
                }
                
                // add nodes to the node dict
                foreach (OSMRoadNode osmNode in result.elements)
                {
                    if(!nodesDict.ContainsKey(osmNode.id))
                    {
                        nodesDict.Add(osmNode.id, new GeoCoordinate(osmNode.lat, osmNode.lon, osmNode.altitude));
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

    private void CreateRoadsFromWays(OSMRoadWay[] ways, GlobeBoundingBox bb, ElevationData elevation, Action<bool> callback)
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

    private static float GetHeightOfPoint(OSMRoadNode node, ElevationData elevation)
    {
        // float x = Mathf.InverseLerp((float)elevation.box.west, (float)elevation.box.east, (float)node.lon);
        // float y = Mathf.InverseLerp((float)elevation.box.south, (float)elevation.box.north, (float)node.lat);
        // int res = elevation.height.GetLength(0) - 1;
        //
        // return elevation.height[(int)(y * res), (int)(x * res)] * (float)(elevation.maxHeight - elevation.minHeight) + (float)elevation.minHeight;
        return 150;
    }
/*
    private void AddRoadsFromRelations(OSMRoadRelation[] relations, Dictionary<ulong, GeoCoordinate> nodesDict, List<OSMRoadsData> roads, GlobeBoundingBox bb, ElevationData elevation)
    {
        List<string> stringsToSend = new List<string>();
        const string endpoint = "https://overpass.kumi.systems/api/interpreter/?";
        string query = "data=[out:json][timeout:" + "1000" + "];(node(id:";
        const string endOfQuery = ");out body;>;out skel qt;";   
        foreach (OSMRoadRelation osmRelation in relations)
        {
            List<List<Vector2>> innerFootprints = new List<List<Vector2>>();
            List<List<Vector3>> outerFootprints = new List<List<Vector3>>();
            bool allNodesFound = true;

            foreach (OSMWay way in osmRelation.innerWays)
            {
                List<Vector2> inner = new List<Vector2>();
                for (int i = 0; i < way.nodes.Length - 1; i++)
                {
                    ulong id = way.nodes[i];
                    if (!nodesDict.ContainsKey(id))
                    {
                        if (query.Length > 1900)
                        {
                            stringsToSend.Add(query);
                            query = "data=[out:json][timeout:" + "1000" + "];(node(";
                        }
                        if (!allNodesFound)
                        {
                            query += ",";
                        }
                        allNodesFound = false;
                        query += id;
                    }
                    else
                    {
                        GeoCoordinate coord = nodesDict[id];
                        Vector2 meterPoint = ConvertGeoCoordToMeters(coord, bb);
                        inner.Add(meterPoint);
                    }
                   
                }
                innerFootprints.Add(inner);
            }

            allNodesFound = true;
            foreach (OSMWay way in osmRelation.outerWays)
            {
                List<Vector3> outer = new List<Vector3>();
                for (int i = 0; i < way.nodes.Length - 1; i++)
                {
                    ulong id = way.nodes[i];
                    if (!nodesDict.ContainsKey(id))
                    {
                         if (query.Length > 1900)
                         {
                             stringsToSend.Append(query);
                             query = "data=[out:json][timeout:" + "1000" + "];(node(";
                         }
                         if (!allNodesFound)
                         {
                             query += ",";
                         }
                         allNodesFound = false;
                         query += id;
                    }
                    else{
                        GeoCoordinate coord = nodesDict[id];
                        Vector2 meterPoint = ConvertGeoCoordToMeters(coord, bb);
                        outer.Add(new Vector3(meterPoint.x, coord.Altitude, meterPoint.y));
                    }
                    
                }
                outerFootprints.Add(outer);
            }

            Vector2[][] holes = new Vector2[innerFootprints.Count][];
            for (int i = 0; i < innerFootprints.Count; i++)
            {
                holes[i] = innerFootprints[i].ToArray();
            }

            if (allNodesFound)
            {
                //Debug.Log(outerFootprints.Count);
                foreach (List<Vector3> building in outerFootprints)
                {
                    if (debug)
                    {
                        Debug.Log("Add road " + osmRelation.tags.name);
                    }
                    roads.Add(new OSMRoadsData(building, holes, osmRelation.tags));
                }
            }
            else
            {
                if (debug)
                    {
                        Debug.Log("all outer nodes not found :(");
                        Debug.Log(osmRelation.tags.name);
                    }
                
            }
        }
        //TODO send requests.
        foreach (string OSMquery in stringsToSend)
        {
            //TODO send request and add nodes to dict.
            string sendURL = endpoint + OSMquery + endOfQuery;
            UnityWebRequest request = UnityWebRequest.Get(sendURL);
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            OSMRoadNode[] roadNodesArray = new OSMRoadNode[]{};
            operation.completed += (AsyncOperation operation) =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(request.error);
                }
                else
                {

                    OSMRoadsNodeContainer result = JsonUtility.FromJson<OSMRoadsNodeContainer>(request.downloadHandler.text);
                    Debug.Log("should be nodes here");
                    Debug.Log(result);
                    roadNodesArray = result.elements;
                    for (int i = 0; i < roadNodesArray.Length; i++)
                    {
                    
                        roadNodesArray[i].altitude = GetHeightOfPoint(roadNodesArray[i], elevation);
                        OSMRoadNode node = roadNodesArray[i];
                        if(debug)
                        {
                            Debug.Log("id :- " + node.id + " longitude:- " + node.lon + " latitude:- " + node.lat + " altitude:- " + node.altitude);
                            Debug.Log(node.tags);
                        }
                    }
                    //Debug.Log("get this many nodes from server :- " + roadNodesArray.Length);
                    if(debug)
                    {
                        Debug.Log(result);
                    }
                }
                request.Dispose();
            };
            foreach (OSMRoadNode osmNode in roadNodesArray)
            {
                nodesDict.Add(unchecked((ulong)osmNode.id), new GeoCoordinate(osmNode.lat, osmNode.lon, osmNode.altitude));
                Debug.Log("added a node to dict in relations");
            }
        }
        //TODO iterate again, actually building this time.

        foreach (OSMRoadRelation osmRelation in relations)
        {
            List<List<Vector2>> innerFootprints = new List<List<Vector2>>();
            List<List<Vector3>> outerFootprints = new List<List<Vector3>>();
            bool allNodesFound = true;

            foreach (OSMWay way in osmRelation.innerWays)
            {
                List<Vector2> inner = new List<Vector2>();
                for (int i = 0; i < way.nodes.Length - 1; i++)
                {
                    ulong id = way.nodes[i];
                    if (!nodesDict.ContainsKey(id))
                    {
                        allNodesFound = false;
                    }
                    else
                    {
                        GeoCoordinate coord = nodesDict[id];
                        Vector2 meterPoint = ConvertGeoCoordToMeters(coord, bb);
                        inner.Add(meterPoint);
                    }
                   
                }
                innerFootprints.Add(inner);
            }

            allNodesFound = true;
            foreach (OSMWay way in osmRelation.outerWays)
            {
                List<Vector3> outer = new List<Vector3>();
                for (int i = 0; i < way.nodes.Length - 1; i++)
                {
                    ulong id = way.nodes[i];
                    if (!nodesDict.ContainsKey(id))
                    {
                         Debug.Log("node not found in relation");
                    }
                    else{
                        GeoCoordinate coord = nodesDict[id];
                        Vector2 meterPoint = ConvertGeoCoordToMeters(coord, bb);
                        outer.Add(new Vector3(meterPoint.x, coord.Altitude, meterPoint.y));
                    }   
                }
                outerFootprints.Add(outer);
            }

            Vector2[][] holes = new Vector2[innerFootprints.Count][];
            for (int i = 0; i < innerFootprints.Count; i++)
            {
                holes[i] = innerFootprints[i].ToArray();
            }

            if (allNodesFound)
            {
                //Debug.Log(outerFootprints.Count);
                foreach (List<Vector3> building in outerFootprints)
                {
                    if (debug)
                    {
                        Debug.Log("Add road " + osmRelation.tags.name);
                    }
                    roads.Add(new OSMRoadsData(building, holes, osmRelation.tags));
                }
            }
            else
            {

                Debug.Log("all outer nodes not found :(");
                Debug.Log(osmRelation.tags.name);
                    
            }
        }
    }
*/
    public override void Release()
    {
        base.Release();
        OSMNodes = null;
        OSMWays = null;
        OSMRelations = null;
        roadsData = null;
    }

    public override void CalculateOutputs(Action<bool> callback)
    {
        // get inputs
        OSMRoadWay[] ways = GetInputValue("OSMWays", OSMWays);
        GlobeBoundingBox bb = GetInputValue("boundingBox", boundingBox);
        ElevationData elevation = GetInputValue("elevationData", elevationData);

        // create a road from each way in the list
        CreateRoadsFromWays(ways, bb, elevation, callback);
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
    