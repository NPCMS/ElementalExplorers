using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using XNode;

[CreateNodeMenu("Buildings/Generate Building Classes")]
public class GenerateBuildingClassesNode : SyncExtendedNode {

	[Input] public OSMNode[] OSMNodes;
	[Input] public OSMWay[] OSMWays;
	[Input] public OSMRelation[] OSMRelations;
	[Input] public ElevationData elevationData;

	[Output] public OSMBuildingData[] buildingData;

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
	
		if (port.fieldName == "buildingData")
		{
			return buildingData;
		}
		return null;
	}

	private bool CheckBuilding(OSMBuildingData building, GlobeBoundingBox bb)
	{
		double width = GlobeBoundingBox.LatitudeToMeters(bb.north - bb.south);
		return Mathf.Min(building.center.x, building.center.y) >= 0 && Mathf.Max(building.center.x, building.center.y) < width;
	}

	private void AddBuildingsFromWays(OSMWay[] ways, Dictionary<ulong, GeoCoordinate> nodesDict, List<OSMBuildingData> buildings, GlobeBoundingBox bb)
	{
		
		foreach (OSMWay osmWay in ways)
		{
            List<Vector3> footprint = new List<Vector3>();
			bool allNodesFound = true;
			// -1 as there is a node repeat to close the polygon
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

			// 3 - create building data objects
			if (allNodesFound)
			{
				Debug.Log(osmWay.tags.name + " " + osmWay.tags.height + " " + osmWay.tags.levels);
				OSMBuildingData building = new OSMBuildingData(footprint, osmWay.tags);
				if (CheckBuilding(building, bb))
				{
					buildings.Add(building);
				}

				foreach (OSMWay part in osmWay.parts)
				{
					List<Vector3> partFootprint = new List<Vector3>();
					bool allpartnodesfound = true;
					// -1 as there is a node repeat to close the polygon
					for (int i = 0; i < part.nodes.Length - 1; i++)
					{
						ulong noderef = part.nodes[i];
						if (!nodesDict.ContainsKey(noderef))
						{
							allpartnodesfound = false;
							break;
						}
						//increase height by the building height here.
						// lookup node
						GeoCoordinate geopoint = nodesDict[noderef];
						geopoint.Altitude += building.buildingHeight + 3f;
						// convert to meters
						Vector2 meterpoint = ConvertGeoCoordToMeters(geopoint, bb);
						// add to footprint
						partFootprint.Add(new Vector3(meterpoint.x, geopoint.Altitude, meterpoint.y));
					}

					// 3 - create building data objects
					if (allpartnodesfound)
					{
						OSMBuildingData partclass = new OSMBuildingData(partFootprint, part.tags);
						if (CheckBuilding(partclass, bb))
						{
							buildings.Add(partclass);
						}
					}
				}

			}

		}
	}

	private void AddBuildingsFromRelations(OSMRelation[] relations, Dictionary<ulong, GeoCoordinate> nodesDict, List<OSMBuildingData> buildings, GlobeBoundingBox bb)
	{
		foreach (OSMRelation osmRelation in relations)
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
					OSMBuildingData build = new OSMBuildingData(building, holes, osmRelation.tags);
					if (CheckBuilding(build, bb))
					{
						buildings.Add(build);
					}
				}
			}
			else
			{
				Debug.Log("all outer nodes not found :(");
			}
		}
	}

	private HashSet<ulong> GetMissingNodeList(Dictionary<ulong, GeoCoordinate> nodesDict, OSMWay[] ways, OSMRelation[] relations)
	{
		HashSet<ulong> missingNodeList = new HashSet<ulong>();
		foreach (OSMWay way in ways)
		{
			foreach (ulong node in way.nodes)
			{
				if (!nodesDict.ContainsKey(node) && !missingNodeList.Contains(node))
				{
					missingNodeList.Add(node);
				}
			}
		}
		foreach (OSMRelation relation in relations)
		{
			foreach (OSMWay way in relation.innerWays)
			{
				foreach (ulong node in way.nodes)
				{
					if (!nodesDict.ContainsKey(node) && !missingNodeList.Contains(node))
					{
						missingNodeList.Add(node);
					}
				}
			}
			foreach (OSMWay way in relation.outerWays)
			{
				foreach (ulong node in way.nodes)
				{
					if (!nodesDict.ContainsKey(node) && !missingNodeList.Contains(node))
					{
						missingNodeList.Add(node);
					}
				}
			}
		}

		return missingNodeList;
	}



	private float GetHeightOfPoint(OSMNode node, ElevationData elevation)
	{
		float x = Mathf.InverseLerp((float)elevation.box.west, (float)elevation.box.east, (float)node.lon);
		float y = Mathf.InverseLerp((float)elevation.box.south, (float)elevation.box.north, (float)node.lat);
		float width = (float)GlobeBoundingBox.LatitudeToMeters(elevation.box.north - elevation.box.south);
		return (float)elevation.SampleHeightFromPosition(new Vector3(x * width, 0, y * width));
	}

	private void GetMissingNodes(Dictionary<ulong, GeoCoordinate> nodesDict, Queue<ulong> missingNodes, Action<bool> callback, ElevationData elevation, int timeout = 180, int maxSize = 1000000)
	{
		Debug.Log("acquiring new missing nodes");
		const int batchSize = 250;
		string endpoint = "https://overpass.kumi.systems/api/interpreter/?";
		StringBuilder builder = new StringBuilder();
		if (missingNodes.Count > 0)
		{
			ulong node = missingNodes.Dequeue();

			builder.Append(node);
			for (int i = 0; i < batchSize && missingNodes.Count > 0; i++)
			{
				builder.Append(",");
				node = missingNodes.Dequeue();
				builder.Append(node);	
			}

			string query = $"data=[out:json][timeout:{timeout}][maxsize:{maxSize}];(node(id:{builder}););out;";
			string sendURL = endpoint + query;
			if(sendURL.Length > 1999)
			{
				Debug.Log("URL to send is too long");
			}
			Debug.Log(sendURL);


			UnityWebRequest request = UnityWebRequest.Get(sendURL);
			UnityWebRequestAsyncOperation operation = request.SendWebRequest();
			operation.completed += _ =>
			{
				if (request.result != UnityWebRequest.Result.Success)
				{
					Debug.Log(request.error);
					callback.Invoke(false);
				}
				else
				{
					OSMNodesContainer result =
						JsonUtility.FromJson<OSMNodesContainer>(request.downloadHandler.text);
					foreach (OSMNode osmNode in result.elements)
					{

						nodesDict.Add(osmNode.id,
							new GeoCoordinate(osmNode.lat, osmNode.lon, GetHeightOfPoint(osmNode, elevation)));
					}

					if (missingNodes.Count > 0)
					{
						GetMissingNodes(nodesDict, missingNodes, callback, elevation);
					}
					else
					{
						CreateClasses(nodesDict, callback);

					}
				}
				Debug.Log("exited the additional nodes function");
				request.Dispose();
			};
		}
		else
		{
			CreateClasses(nodesDict, callback);
		}
	}

	public override IEnumerator CalculateOutputs(Action<bool> callback)
	{
		// get inputs
		OSMNode[] nodes = GetInputValue("OSMNodes", OSMNodes);
		OSMWay[] ways = GetInputValue("OSMWays", OSMWays);
		OSMRelation[] relations = GetInputValue("OSMRelations", OSMRelations);
		ElevationData elevation = GetInputValue("elevationData", elevationData);

		// load osm nodes into dict
		Dictionary<ulong, GeoCoordinate> nodesDict = new Dictionary<ulong, GeoCoordinate>();
		foreach (OSMNode osmNode in nodes)
		{
			nodesDict.Add(osmNode.id, new GeoCoordinate(osmNode.lat, osmNode.lon, osmNode.altitude));
		}

		HashSet<ulong> missingNodes = GetMissingNodeList(nodesDict, ways, relations);
		GetMissingNodes(nodesDict, new Queue<ulong>(missingNodes), callback, elevation);
		yield break;
	}

	private void CreateClasses(Dictionary<ulong, GeoCoordinate> nodesDict, Action<bool> callback)
	{
		OSMNode[] nodes = GetInputValue("OSMNodes", OSMNodes);
		OSMWay[] ways = GetInputValue("OSMWays", OSMWays);
		OSMRelation[] relations = GetInputValue("OSMRelations", OSMRelations);
		ElevationData elevation = GetInputValue("elevationData", elevationData);
		// output list
		List<OSMBuildingData> buildings = new List<OSMBuildingData>();
		// 2- iterate ways
		AddBuildingsFromWays(ways, nodesDict, buildings, elevation.box);
		// 3- iterate relations
		AddBuildingsFromRelations(relations, nodesDict, buildings, elevation.box);

		// done
		buildingData = buildings.ToArray();
		callback.Invoke(true);
	}

	private float UnclampedInverseLerp(double a, double b, double v)
	{
		return (float)((v - a) / (b - a));

	}

	private Vector2 ConvertGeoCoordToMeters(GeoCoordinate coord, GlobeBoundingBox bb)
	{
		double width = GlobeBoundingBox.LatitudeToMeters(bb.north - bb.south);
		float verticalDst = UnclampedInverseLerp(bb.south, bb.north, coord.Latitude) * (float)width;
		float horizontalDst = UnclampedInverseLerp(bb.west, bb.east, coord.Longitude) * (float)width;
		return new Vector2(horizontalDst, verticalDst);
	}

	public override void Release()
	{
		OSMNodes = null;
		OSMWays = null;
		OSMRelations = null;
		buildingData = null;
		elevationData = null;
	}
}

[Serializable]
public class OSMBuildingData
{
	public List<Vector2> footprint;
	public Vector2[][] holes;
	public Vector2 center;
	public float buildingHeight;
	public int buildingLevels;
	public string name;
	public float elevation;
	public List<string> grammar;

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

	public OSMBuildingData(List<Vector3> footprint, OSMTags tags)
	{
		holes = new Vector2[0][];
		this.footprint = new List<Vector2>();
		for (int i = 0; i < footprint.Count; i++)
		{
			this.footprint.Add(new Vector2(footprint[i].x, footprint[i].z));
		}
		this.name = tags.name == null ? "Unnamed Building" : tags.name;

		this.grammar = this.name == "Unnamed Building" ? Grammars.detachedHouse : Grammars.relations;
		MakeRelative();
		SetHeightAndLevels(tags.height, tags.levels);
		SetElevation(footprint);
	}

	public OSMBuildingData(List<Vector3> footprint, Vector2[][] holes, OSMTags tags)
	{
		this.holes = holes;
		this.footprint = new List<Vector2>();
		for (int i = 0; i < footprint.Count; i++)
		{
			this.footprint.Add(new Vector2(footprint[i].x, footprint[i].z));
		}
		this.name = tags.name == null ? "Unnamed Building" : tags.name;
		this.grammar = Grammars.relations;
		MakeRelative();
		SetHeightAndLevels(tags.height, tags.levels);
		SetElevation(footprint);
	}

	private void SetElevation(List<Vector3> footPrint)
	{
		elevation = float.MaxValue;
		float maxElevation = float.MinValue;
		foreach (Vector3 node in footPrint)
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
            this.buildingLevels = (int)((height * 1.5f) / 3.0f);
        }
        else if (hasLevels)
        {
            this.buildingLevels = (int)(levels * 1.5f);
        }
        else
        {
            this.buildingLevels = 6;
        }

  //      if (hasHeight)
		//{
		//	this.buildingHeight = height * 1.5f;
		//}
		//else if (hasLevels)
		//{
		//	this.buildingHeight = levels * 3 * 1.5f;
		//}
		//else
		//{
		//	this.buildingHeight = 20 * 1.5f;
  //      }
        this.buildingHeight = levels * 3;

        Debug.Log(name +" " + this.buildingLevels + " " + this.buildingHeight);
	}
}
