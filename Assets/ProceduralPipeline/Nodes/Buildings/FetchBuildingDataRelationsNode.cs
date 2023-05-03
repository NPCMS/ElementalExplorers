using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using XNode;

[CreateNodeMenu("Buildings/Buildings Data Relation")]
public class FetchBuildingDataRelationsNode : SyncExtendedNode {

	[Input] public GlobeBoundingBox boundingBox;
	[Input] public int timeout;
	[Input] public int maxSize;

	[Output] public OSMRelation[] relationArray;

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


        string query = "data=[out:json][timeout:" + maxTime + "][maxsize:" + largestSize + "];relation[\"building\"](" + bb.south + "," + bb.west + "," +
                       bb.north + "," + bb.east + ");" +
        "relation[\"building:part\"](" + bb.south + "," + bb.west + "," +
                       bb.north + "," + bb.east + ");" +
                       "out;";

        string sendURL = endpoint + query;


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

				OSMRelationsContainer result = JsonUtility.FromJson<OSMRelationsContainer>(request.downloadHandler.text.Replace("ref", "reference"));
				List<RelationUninitialised> relations = new List<RelationUninitialised>();
				Dictionary<ulong, OSMWay> wayDictionary = new Dictionary<ulong, OSMWay>();
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
                                Debug.Log("inner way was not obtained in a relation");

                            }

                        }
					}
					relationArray[i] = new OSMRelation() {tags = relations[i].tags, innerWays = innerWays.ToArray(), outerWays = outerWays.ToArray()};
				}
				callback.Invoke(true);
			}
			request.Dispose();
		};
	}

    public override void ApplyGUI()
    {
        base.ApplyGUI();
#if UNITY_EDITOR
        EditorGUILayout.LabelField($"{relationArray.Length} relations");
#endif
    }
    public override void Release()
	{
		relationArray = null;
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