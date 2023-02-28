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

    private void SendAdditionalRequests(List<string> stringsToSend, ElevationData elevation, OSMRoadWay[] ways,  Dictionary<ulong, GeoCoordinate> nodesDict, 
    List<OSMRoadsData> roads, GlobeBoundingBox bb, Action<bool> callback)
    {
        string endpoint = "https://overpass.kumi.systems/api/interpreter/?";
        string endOfQuery = "););out body;>;out skel qt;";
        OSMRoadNode[] myRoadNodesArray;
        // send first OSMquery in list
        Debug.Log("sending request");
        var osmQuery = stringsToSend[0];
        stringsToSend.Remove(osmQuery);
        string sendURL = endpoint + osmQuery + endOfQuery;
        Debug.Log(sendURL);
        UnityWebRequest request = UnityWebRequest.Get(sendURL);
        UnityWebRequestAsyncOperation operation =  request.SendWebRequest();

        operation.completed += _ =>
        {
            // if strings to send has more string to send, send them recursively
            // else all strings must be sent so go to next step
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(request.error);
            }
            else
            {
                Debug.Log("Received data");
                OSMRoadsNodeContainer result = JsonUtility.FromJson<OSMRoadsNodeContainer>(request.downloadHandler.text);
                // Debug.Log("should be nodes here");
                Debug.Log(result);
                myRoadNodesArray = result.elements;
                for (int i = 0; i < myRoadNodesArray.Length; i++)
                {
                    myRoadNodesArray[i].altitude = GetHeightOfPoint(myRoadNodesArray[i], elevation);
                    OSMRoadNode node = myRoadNodesArray[i];
                    if(debug)
                    {
                        Debug.Log("id :- " + node.id + " longitude:- " + node.lon + " latitude:- " + node.lat + " altitude:- " + node.altitude);
                        Debug.Log(node.tags);
                    }
                }
                Debug.Log("get this many nodes from server :- " + myRoadNodesArray.Length);
                Debug.Log(result);
                foreach (OSMRoadNode osmNode in myRoadNodesArray)
                {
                    if(!nodesDict.ContainsKey(osmNode.id))
                    {
                        nodesDict.Add(osmNode.id, new GeoCoordinate(osmNode.lat, osmNode.lon, osmNode.altitude));
                    }
                }
                
                if (stringsToSend.Count > 0) {
                    SendAdditionalRequests(stringsToSend, elevation, ways, nodesDict, roads, bb, callback);
                } else {
                    Debug.Log("All nodes processed");
                    ContinueAddingWays(ways, nodesDict, roads, bb, elevation, callback);
                }
            }
            request.Dispose();
        };
        
        // this.roadNodesArray = myRoadNodesArray;
        // //continuation
        // continueAddingWays(ways, nodesDict, roads, bb, elevation);
    }
/*
    private void OldAddRoadsFromWays(OSMRoadWay[] ways, Dictionary<ulong, GeoCoordinate> nodesDict, List<OSMRoadsData> roads, GlobeBoundingBox bb, ElevationData elevationData)
    {
        foreach (OSMRoadWay osmWay in ways)
        {
            List<Vector3> footprint = new List<Vector3>();
            bool allNodesFound = true;
            // -1 as there is a node repeat too close the polygon
            if (osmWay.nodes != null)
            {
                for (int i = 0; i < osmWay.nodes.Length - 1; i++)
                {
                    ulong nodeRef = osmWay.nodes[i];
                    if (!nodesDict.ContainsKey(nodeRef))
                    {
                        allNodesFound = false;
                        continue;
                        
                    }
                    // lookup node
                    GeoCoordinate geoPoint = nodesDict[nodeRef];
                    // convert to meters
                    Vector2 meterPoint = ConvertGeoCoordToMeters(geoPoint, bb);
                    // add to footprint
                    footprint.Add(new Vector3(meterPoint.x, geoPoint.Altitude, meterPoint.y));
                }
            }
            

            // 3 - create roads data objects
            roads.Add(new OSMRoadsData(footprint, osmWay.tags));
           
        }
    }
*/

    private void AddRoadsFromWays(OSMRoadWay[] ways, Dictionary<ulong, GeoCoordinate> nodesDict, List<OSMRoadsData> roads, GlobeBoundingBox bb, ElevationData elevation, Action<bool> callback)
    {
        List<string> stringsToSend = new List<string>();
        string query = "data=[out:json][timeout:" + "1000" + "];(node(id:";
            
        foreach (OSMRoadWay osmWay in ways)
        {
            // -1 as there is a node repeat too close the polygon
            if (osmWay.nodes == null) continue;
            for (int i = 0; i < osmWay.nodes.Length - 1; i++)
            {
                ulong nodeRef = osmWay.nodes[i];
                if (nodesDict.ContainsKey(nodeRef)) continue;
                if (query.Length > 1900)
                {
                    stringsToSend.Add(query);
                    query = "data=[out:json][timeout:" + "1000" + "];(node(id:";
                }
                if (query != "data=[out:json][timeout:" + "1000" + "];(node(id:")
                {
                    query += ",";
                }
                query += nodeRef;
            }
        }
        Debug.Log(query);
        Debug.Log("old size is + " + nodesDict.Count);
        stringsToSend.Add(query);
        Debug.Log("queries length:- " + stringsToSend.Count);
        // OSMRoadNode[] roadNodesArray = null;
        SendAdditionalRequests(stringsToSend, elevation, ways, nodesDict, roads, bb, callback);
        Debug.Log("Sent requests");
        // end of function
    }

    private void ContinueAddingWays(OSMRoadWay[] ways, Dictionary<ulong, GeoCoordinate> nodesDict, List<OSMRoadsData> roads, GlobeBoundingBox bb, 
    ElevationData elevation, Action<bool> callback)
    {
        Debug.Log("continuing adding ways");
        Debug.Log("new size is + " + nodesDict.Count);
        Debug.Log("before we had roads + " + roads.Count);

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
                Debug.Log("not found all the nodes for ways");
            }

            // 3 - create roads data objects
            roads.Add(new OSMRoadsData(footprint, osmWay.tags));
            Debug.Log("added road");
            var name = osmWay.tags;
            Debug.Log(name);
        }
        Debug.Log("Now we have roads + " + roads.Count);
        roadsData = roads.ToArray();
        Debug.Log(roadsData.Length);
        callback.Invoke(true);

    }

    private static float GetHeightOfPoint(OSMRoadNode node, ElevationData elevation)
    {
        float x = Mathf.InverseLerp((float)elevation.box.west, (float)elevation.box.east, (float)node.lon);
        float y = Mathf.InverseLerp((float)elevation.box.south, (float)elevation.box.north, (float)node.lat);
        int res = elevation.height.GetLength(0) - 1;

        return elevation.height[(int)(y * res), (int)(x * res)] * (float)(elevation.maxHeight - elevation.minHeight) + (float)elevation.minHeight;
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
        OSMRoadNode[] nodes = GetInputValue("OSMNodes", OSMNodes);
        OSMRoadWay[] ways = GetInputValue("OSMWays", OSMWays);
        OSMRoadRelation[] relations = GetInputValue("OSMRelations", OSMRelations);
        GlobeBoundingBox bb = GetInputValue("boundingBox", boundingBox);
        ElevationData elevation = GetInputValue("elevationData", elevationData);

        // output list
        List<OSMRoadsData> roads = new List<OSMRoadsData>();

        // load osm nodes into dict
        Dictionary<ulong, GeoCoordinate> nodesDict = new Dictionary<ulong, GeoCoordinate>();
        foreach (OSMRoadNode osmNode in nodes)
        {
            nodesDict.Add(osmNode.id, new GeoCoordinate(osmNode.lat, osmNode.lon, osmNode.altitude));
        }

        // 2- iterate ways
        //OldAddRoadsFromWays(ways, nodesDict, roads, bb, elevation);
        AddRoadsFromWays(ways, nodesDict, roads, bb, elevation, callback);
        if (debug)
        {
            Debug.Log("1 " + roads.Count);
        }
        
        // // 3- iterate relations
        // AddRoadsFromRelations(relations, nodesDict, roads, bb, elevation);
        // if (debug)
        // {
        //     Debug.Log("2 " + roads.Count);
        // }

        // done
       
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
    