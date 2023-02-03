using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using XNode;

[Serializable]
public class OSMBuildingData
{
	public List<Vector2> footprint;
	public Vector2 center;
	public float buildingHeight;
	public int buildingLevels;
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

	}

	public OSMBuildingData(List<Vector3> footprint, OSMWay.OSMTags tags)
	{
		this.footprint = new List<Vector2>();
		for (int i = 0; i < footprint.Count; i++)
		{
			this.footprint.Add(new Vector2(footprint[i].x, footprint[i].z));
		}
        this.name = tags.name == null ? "Unnamed Building" : tags.name;
        MakeRelative();
		SetHeightAndLevels(tags.height, tags.levels);
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

		buildingHeight += maxElevation - elevation;
	}

	private void SetHeightAndLevels(int height, int levels)
	{
		bool hasHeight = height > 0;
		bool hasLevels = levels > 0;

        if (hasHeight)
		{
            this.buildingHeight = height;
        }
		else if (hasLevels)
		{
			this.buildingHeight = levels * 3;
		}
		else
		{
			this.buildingHeight = 10;
        }

        if (hasLevels)
        {
            this.buildingLevels = levels;
        }
        else if (hasHeight)
        {
            this.buildingLevels = (int)buildingHeight / 3;
        }
        else
        {
            this.buildingLevels = 3;
        }


    }
}


[CreateNodeMenu("Buildings/Generate OSM Building Data Classes")]
public class GenerateOSMBuildingClassesNode : ExtendedNode {

	[Input] public OSMNode[] OSMNodes;
	[Input] public OSMWay[] OSMWays;
	[Input] public GlobeBoundingBox boundingBox;

	[Output] public OSMBuildingData[] buildingData;

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		
		if (port.fieldName == "buildingData")
		{
			return buildingData;
		}
		return null;
	}

	public override void CalculateOutputs(Action<bool> callback)
	{
		// get inputs
        OSMNode[] nodes = GetInputValue("OSMNodes", OSMNodes);
        OSMWay[] ways = GetInputValue("OSMWays", OSMWays);
		GlobeBoundingBox bb = GetInputValue("boundingBox", boundingBox);

		// output list
        List<OSMBuildingData> buildings = new List<OSMBuildingData>();

		// load osm nodes into dict
        Dictionary<ulong, GeoCoordinate> nodesDict = new Dictionary<ulong, GeoCoordinate>();
		foreach (OSMNode osmNode in nodes)
		{
			nodesDict.Add(osmNode.id, new GeoCoordinate(osmNode.lat, osmNode.lon, osmNode.altitude));
		}

		Debug.Log(ways.Length);

		// 2- iterate ways
		foreach (OSMWay osmWay in ways)
		{
			List<Vector3> footprint = new List<Vector3>();
            bool allNodesFound = true;
            foreach (ulong nodeRef in osmWay.nodes)
			{
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

            // 3 - create building data objects
            if (allNodesFound)
            {
                buildings.Add(new OSMBuildingData(footprint, osmWay.tags));
            }
        }


		// done
		buildingData = buildings.ToArray();
		Debug.Log(buildingData.Length);
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