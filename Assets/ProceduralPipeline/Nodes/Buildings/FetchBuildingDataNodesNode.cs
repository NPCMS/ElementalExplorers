using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using XNode;

[CreateNodeMenu("Buildings/Buildings Data Nodes")]
public class FetchBuildingDataNodesNode : SyncExtendedNode
{

	[Input] public ElevationData elevationData;
	[Input] public int timeout;
	[Input] public int maxSize;
	[Output] public OSMNode[] nodeArray;

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port)
	{
		if (port.fieldName == "nodeArray")
		{
			return nodeArray;
		}
		else
		{
			return null;
		}
	}

	public override IEnumerator CalculateOutputs(Action<bool> callback)
	{
		int actualTimeout = GetInputValue("timeout", timeout);
		int actualMaxSize = GetInputValue("maxSize", maxSize);
		ElevationData elevation = GetInputValue("elevationData", elevationData);
		SendRequest(elevation.box, actualTimeout, actualMaxSize, elevation, callback);
		yield break;
	}

	private float GetHeightOfPoint(OSMNode node, ElevationData elevation)
	{
		float x = Mathf.InverseLerp((float)elevation.box.west, (float)elevation.box.east, (float)node.lon);
		float y = Mathf.InverseLerp((float)elevation.box.south, (float)elevation.box.north, (float)node.lat);
		float width = (float)GlobeBoundingBox.LatitudeToMeters(elevation.box.north - elevation.box.south);
		return (float)elevation.SampleHeightFromPosition(new Vector3(x * width, 0, y * width));
	}

	public void SendRequest(GlobeBoundingBox bb, int maxTime, int largestSize, ElevationData elevation, Action<bool> callback)
	{
		string endpoint = "https://overpass.kumi.systems/api/interpreter/?";
		string query = "data=[out:json][timeout:" + maxTime + "][maxsize:" + largestSize + "];node(" + bb.south + "," + bb.west + "," +
		               bb.north + "," + bb.east + ");out;";
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

				OSMNodesContainer result = JsonUtility.FromJson<OSMNodesContainer>(request.downloadHandler.text);
				nodeArray = result.elements;
				for (int i = 0; i < nodeArray.Length; i++)
				{
					nodeArray[i].altitude = GetHeightOfPoint(nodeArray[i], elevation);
				}
				callback.Invoke(true);
			}
			request.Dispose();
		};
	}

	public override void Release()
	{
		elevationData = null;
		nodeArray = null;
	}
}

[Serializable]
public struct OSMNode
{
	public ulong id;
	public double lat;
	public double lon;
	public float altitude;
}

[Serializable]
public class OSMNodesContainer
{
	public OSMNode[] elements;
}

// container for a lat long pair
[Serializable]
public struct GeoCoordinate
{
	public double Latitude;
	public double Longitude;
	public float Altitude;

	public GeoCoordinate(double latitude, double longitude, float altitude)
	{
		Latitude = latitude;
		Longitude = longitude;
		Altitude = altitude;
	}

	public override string ToString()
	{
		return $"{nameof(Latitude)}: {Latitude}, {nameof(Longitude)}: {Longitude}, {nameof(Altitude)}: {Altitude}";
	}
}