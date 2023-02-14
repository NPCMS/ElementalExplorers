using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using XNode;

[Serializable]
public class OSMRoadsData
{
    public List<Vector2> footprint;
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

        for (int i = 0; i < holes.Length; i++)
        {
            for (int j = 0; j < holes[i].Length; j++)
            {
                holes[i][j] -= center;
            }
        }
    }

    public OSMRoadsData(List<Vector3> footprint, OSMTags tags)
    {
        holes = new Vector2[0][];
        this.footprint = new List<Vector2>();
        for (int i = 0; i < footprint.Count; i++)
        {
            this.footprint.Add(new Vector3(footprint[i].x, footprint[i].z));
        }
        this.name = tags.name == null ? "Unnamed Road" : tags.name;
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
    [Input] public bool debug;

    [Output] public OSMRoadsData[] roadsData;

    // Return the correct value of an output port when requested
    public override object GetValue(NodePort port)
    {

        if (port.fieldName == "roadsData")
        {
            return roadsData;
        }
        return null;
    }

    private void AddRoadsFromWays(OSMRoadWay[] ways, Dictionary<ulong, GeoCoordinate> nodesDict, List<OSMRoadsData> roads, GlobeBoundingBox bb)
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
                        break;
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
            if (allNodesFound)
            {
                roads.Add(new OSMRoadsData(footprint, osmWay.tags));
            }
            else{
                Debug.Log("failure from a way road");
            }
        }
    }

    private void AddRoadsFromRelations(OSMRoadRelation[] relations, Dictionary<ulong, GeoCoordinate> nodesDict, List<OSMRoadsData> roads, GlobeBoundingBox bb)
    {
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
                        Debug.Log("not found key");
                        allNodesFound = false;
                        break;
                        
                    }
                    GeoCoordinate coord = nodesDict[id];
                    Vector2 meterPoint = ConvertGeoCoordToMeters(coord, bb);
                    inner.Add(meterPoint);
                }
                innerFootprints.Add(inner);
            }

            foreach (OSMWay way in osmRelation.outerWays)
            {
                List<Vector3> outer = new List<Vector3>();
                for (int i = 0; i < way.nodes.Length - 1; i++)
                {
                    ulong id = way.nodes[i];
                    if (!nodesDict.ContainsKey(id))
                    {
                        allNodesFound = false;
                        break;
                    }
                    GeoCoordinate coord = nodesDict[id];
                    Vector2 meterPoint = ConvertGeoCoordToMeters(coord, bb);
                    outer.Add(new Vector3(meterPoint.x, coord.Altitude, meterPoint.y));
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
    }

    public override void CalculateOutputs(Action<bool> callback)
    {
        // get inputs
        OSMRoadNode[] nodes = GetInputValue("OSMNodes", OSMNodes);
        OSMRoadWay[] ways = GetInputValue("OSMWays", OSMWays);
        OSMRoadRelation[] relations = GetInputValue("OSMRelations", OSMRelations);
        GlobeBoundingBox bb = GetInputValue("boundingBox", boundingBox);

        // output list
        List<OSMRoadsData> roads = new List<OSMRoadsData>();

        // load osm nodes into dict
        Dictionary<ulong, GeoCoordinate> nodesDict = new Dictionary<ulong, GeoCoordinate>();
        foreach (OSMRoadNode osmNode in nodes)
        {
            nodesDict.Add(unchecked((ulong)osmNode.id), new GeoCoordinate(osmNode.lat, osmNode.lon, osmNode.altitude));
        }

        // 2- iterate ways
        AddRoadsFromWays(ways, nodesDict, roads, bb);
        if (debug)
        {
            Debug.Log("1 " + roads.Count);
        }
        
        // 3- iterate relations
        AddRoadsFromRelations(relations, nodesDict, roads, bb);
        if (debug)
        {
            Debug.Log("2 " + roads.Count);
        }

        // done
        roadsData = roads.ToArray();
        Debug.Log(roadsData.Length);
        callback.Invoke(true);
    }

    private Vector2 ConvertGeoCoordToMeters(GeoCoordinate coord, GlobeBoundingBox bb)
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
    