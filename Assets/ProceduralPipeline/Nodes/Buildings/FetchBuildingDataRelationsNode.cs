using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using XNode;

[CreateNodeMenu("Buildings/Buildings Data Relation")]
public class FetchBuildingDataRelationsNode : SyncExtendedNode {

	[Input] public GlobeBoundingBox boundingBox;
	[Input] public int timeout;
	[Input] public int maxSize;
	
	[Output] public OSMRelation[] relationArray;
	private Queue<ulong> missingWayIds = new Queue<ulong>();
	private List<RelationUninitialised> relations = new List<RelationUninitialised>();
	Dictionary<ulong, OSMWay> wayDictionary = new Dictionary<ulong, OSMWay>();


	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port)
	{
		if(port.fieldName == "relationArray")
		{
			return relationArray;
		}
		return null;
	}

	public override IEnumerator CalculateOutputs(Action<bool> callback)
	{
		GlobeBoundingBox actualBoundingBox = GetInputValue("boundingBox", boundingBox);
		int actualTimeout = GetInputValue("timeout", timeout);
		int actualMaxSize = GetInputValue("maxSize", maxSize);
		SendRequest(actualBoundingBox, actualTimeout, actualMaxSize, callback);
		yield break;
	}

	public void SendRequest(GlobeBoundingBox bb, int maxTime, int largestSize, Action<bool> callback)
	{
		
        string endpoint = "https://overpass.kumi.systems/api/interpreter/?";
        //string query = "data=[out:json][timeout:" + maxTime + "][maxsize:" + largestSize + "];way[building](" + bb.south + "," + bb.west + "," +
        //               bb.north + "," + bb.east + ");out;";


        string query = "data=[out:json][timeout:" + maxTime + "][maxsize:" + largestSize + "];relation[building](" + bb.south + "," + bb.west + "," +
                       bb.north + "," + bb.east + ");"+ "out;";

        string sendURL = endpoint + query;


        UnityWebRequest request = UnityWebRequest.Get(sendURL);
		UnityWebRequestAsyncOperation operation = request.SendWebRequest();
		operation.completed += _ =>
		{
			if (request.result != UnityWebRequest.Result.Success)
			{
				Debug.LogWarning(request.error);
				callback.Invoke(false);
			}
			else
			{
				//Debug.LogWarning(request.downloadHandler.text);

				OSMRelationsContainer result = JsonUtility.FromJson<OSMRelationsContainer>(request.downloadHandler.text.Replace("ref", "reference"));
				foreach (RelationOrWay element in result.elements)
				{
					if (element.type == "relation")
					{
						relations.Add(new RelationUninitialised() {tags = element.tags, ways = element.members});
					}
					else if (element.type == "way") 
					{
						wayDictionary.Add(element.id, new OSMWay() {id = element.id, nodes = element.nodes});
					}
				}

				relationArray = new OSMRelation[relations.Count];
				List<OSMWay> innerWays = new List<OSMWay>();
				List<OSMWay> outerWays = new List<OSMWay>();
				for(int i = 0; i < relations.Count; i++)
				{
					innerWays.Clear();
					outerWays.Clear();
					foreach (RelationWay way in relations[i].ways)
					{
						if (way.role == "outer")
						{
							if(wayDictionary.ContainsKey(way.reference))
							{
                                outerWays.Add(wayDictionary[way.reference]);

                            }
							else
							{
								missingWayIds.Enqueue(way.reference);
								Debug.Log("outer way was not obtained in a relation");
							}
                        }
						else
						{
							if (wayDictionary.ContainsKey(way.reference))
							{
								innerWays.Add(wayDictionary[way.reference]);
							}
                            else
                            {
	                            missingWayIds.Enqueue(way.reference);
                                Debug.Log("inner way was not obtained in a relation");

                            }

                        }
					}
					relationArray[i] = new OSMRelation() {tags = relations[i].tags, innerWays = innerWays.ToArray(), outerWays = outerWays.ToArray()};
				}
				ObtainMissingWays(boundingBox,callback);
				//callback.Invoke(true);
			}
			request.Dispose();
		};
	}

	private void ObtainMissingWays(GlobeBoundingBox bb, Action<bool> callback, int maxTime = 180, int largestSize = 1000000)
	{
		const int batchSize = 150;
		string endpoint = "https://overpass.kumi.systems/api/interpreter/?";
		StringBuilder builder = new StringBuilder();
		if (missingWayIds.Count > 0)
		{
			ulong way = missingWayIds.Dequeue();

			builder.Append(way);
			for (int i = 0; i < batchSize && missingWayIds.Count > 0; i++)
			{
				builder.Append(",");
				way = missingWayIds.Dequeue();
				builder.Append(way);	
			}

			string query = $"data=[out:json][timeout:{timeout}][maxsize:{maxSize}];(way(id:{builder}););out;";
			string sendURL = endpoint + query;
			if(sendURL.Length > 1999)
			{
				Debug.Log("URL to send is too long");
			}


			UnityWebRequest request = UnityWebRequest.Get(sendURL);
			UnityWebRequestAsyncOperation operation = request.SendWebRequest();
			operation.completed += _ =>
			{
				Debug.Log("sending additional request in relations to get their ways!");
				if (request.result != UnityWebRequest.Result.Success)
				{
					Debug.Log(request.error);
				}
				else
				{
					FetchBuildingDataWaysNode.OSMWaysContainer result =
						JsonUtility.FromJson<FetchBuildingDataWaysNode.OSMWaysContainer>(request.downloadHandler.text);
					foreach (OSMWay osmWay in result.elements)
					{
						if (!wayDictionary.ContainsKey(osmWay.id))
						{
							wayDictionary.Add(osmWay.id, new OSMWay() { id = osmWay.id, nodes = osmWay.nodes });
						}
						//ways.Add(osmWay);
					}

					if (missingWayIds.Count > 0)
					{
						ObtainMissingWays(boundingBox, callback);
					}
					else
					{
						FinaliseRelations(callback);
					}
				}
				request.Dispose();
			};
		}
		else
		{
			callback.Invoke(true);
		}
	}

	private void FinaliseRelations(Action<bool> callback)
	{
		Debug.Log("finalising relations");
		relationArray = new OSMRelation[relations.Count];
		List<OSMWay> innerWays = new List<OSMWay>();
		List<OSMWay> outerWays = new List<OSMWay>();
		for(int i = 0; i < relations.Count; i++)
		{
			innerWays.Clear();
			outerWays.Clear();
			foreach (RelationWay way in relations[i].ways)
			{
				if (way.role == "outer")
				{
					if(wayDictionary.ContainsKey(way.reference))
					{
						outerWays.Add(wayDictionary[way.reference]);

					}
					else
					{
						//missingWayIds.Enqueue(way.reference);
						Debug.LogWarning("outer way was not obtained in a relation. This should not happen");
					}
				}
				else
				{
					if (wayDictionary.ContainsKey(way.reference))
					{
						innerWays.Add(wayDictionary[way.reference]);
					}
					else
					{
						//missingWayIds.Enqueue(way.reference);
						Debug.LogWarning("inner way was not obtained in a relation. This should not happen");
					}

				}
			}
			relationArray[i] = new OSMRelation() {tags = relations[i].tags, innerWays = innerWays.ToArray(), outerWays = outerWays.ToArray()};
		}
		callback.Invoke(true);
	}
	
	
	public override void Release()
	{
		relationArray = null;
		// missingWayIds = null;
		// relations = null;
		// wayDictionary = null;
	}
}


[Serializable]
public struct OSMRelation
{
	public OSMTags tags;
	public OSMWay[] innerWays;
	public OSMWay[] outerWays;
}

[Serializable]
public struct RelationWay
{
	public string role;
	public ulong reference;
}

[Serializable]
public class RelationOrWay
{
	public string type;
	public ulong id;
	public ulong[] nodes;
	public RelationWay[] members;
	public OSMTags tags;
}

[Serializable]
public class RelationUninitialised
{
	public OSMTags tags;
	public RelationWay[] ways;
}

[Serializable]
public class OSMRelationsContainer
{
	public RelationOrWay[] elements;
}