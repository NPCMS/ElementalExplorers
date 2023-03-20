using System;
using ProceduralPipelineNodes.Nodes.Buildings;
using UnityEngine;
using UnityEngine.Networking;
using XNode;

namespace ProceduralPipelineNodes.Nodes.Roads
{
	[CreateNodeMenu("Legacy/Roads/OSM Roads Nodes Data")]
	public class OSMRoadsNodesDataNode : ExtendedNode {

		[Input] public GlobeBoundingBox boundingBox;
		[Input] public int timeout;
		[Input] public int maxSize;
		[Input] public bool debug;
	
		[Output] public OSMRoadNode[] roadNodesArray;

		// Return the correct value of an output port when requested
		public override object GetValue(NodePort port) {
			if(port.fieldName == "roadNodesArray")
			{
				return roadNodesArray;
			}
			else
			{
				return null; 
			}
		}

		public override void CalculateOutputs(Action<bool> callback)
		{
			GlobeBoundingBox actualBoundingBox = GetInputValue("boundingBox", boundingBox);
			int actualTimeout = GetInputValue("timeout", timeout);
			int actualMaxSize = GetInputValue("maxSize", maxSize);
			sendRequest(actualBoundingBox, actualTimeout, actualMaxSize, callback);
		}

		private float GetHeightOfPoint(OSMRoadNode node, ElevationData elevation)
		{
			float x = Mathf.InverseLerp((float)elevation.box.west, (float)elevation.box.east, (float)node.lon);
			float y = Mathf.InverseLerp((float)elevation.box.south, (float)elevation.box.north, (float)node.lat);
			int res = elevation.height.GetLength(0) - 1;

			return elevation.height[(int)(y * res), (int)(x * res)] * (float)(elevation.maxHeight - elevation.minHeight) + (float)elevation.minHeight;
		}

		public void sendRequest(GlobeBoundingBox boundingBox, int timeout, int maxSize, Action<bool> callback)
		{
			string endpoint = "https://overpass.kumi.systems/api/interpreter/?";
			string query = "data=[out:json][timeout:" + timeout + "][maxsize:" + maxSize + "];node(" + boundingBox.south + "," + boundingBox.west + "," +
			               boundingBox.north + "," + boundingBox.east + ");out body;>;out skel qt;";
			string sendURL = endpoint + query;


			UnityWebRequest request = UnityWebRequest.Get(sendURL);
			UnityWebRequestAsyncOperation operation = request.SendWebRequest();
			operation.completed += (AsyncOperation operation) =>
			{
				if (request.result != UnityWebRequest.Result.Success)
				{
					Debug.Log(request.error);
					callback.Invoke(false);
				}
				else
				{

					OSMRoadsNodeContainer result = JsonUtility.FromJson<OSMRoadsNodeContainer>(request.downloadHandler.text);
					roadNodesArray = result.elements;
					for (int i = 0; i < roadNodesArray.Length; i++)
					{
                    
						OSMRoadNode node = roadNodesArray[i];
						if(debug)
						{
							Debug.Log("id :- " + node.id + " longitude:- " + node.lon + " latitude:- " + node.lat);
							Debug.Log(node.tags);
						}
                    
                    
					}
					Debug.Log("get this many nodes from server :- " + roadNodesArray.Length);
					if(debug)
					{
						Debug.Log(result);
					}
                
					callback.Invoke(true);
				}
				request.Dispose();
			};
		}

		public override void Release()
		{
			base.Release();
			roadNodesArray = null;
		}
	}

	[System.Serializable]
	public struct OSMRoadNode
	{
		public ulong id;
		public double lon;
		public double lat;
		public OSMTags tags;
	}

	[System.Serializable]
	public struct OSMRoadsNodeContainer
	{
		public OSMRoadNode[] elements;
	}
}