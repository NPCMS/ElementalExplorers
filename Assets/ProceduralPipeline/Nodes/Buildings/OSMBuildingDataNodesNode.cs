using System;
using UnityEngine;
using UnityEngine.Assertions.Must;
using XNode;
using UnityEngine.Networking;

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
}

[CreateNodeMenu("Buildings/OSM Buildings Data Nodes")]
public class OSMBuildingDataNodesNode : ExtendedNode
{

	[Input] public GlobeBoundingBox boundingBox;
	[Input] public ElevationData elevationData;
	[Input] public int timeout;
	[Input] public int maxSize;
	[Output] public OSMNode[] nodeArray;
	// Use this for initialization
	protected override void Init()
	{
		base.Init();

	}

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

	public override void CalculateOutputs(Action<bool> callback)
	{
        GlobeBoundingBox actualBoundingBox = GetInputValue("boundingBox", boundingBox);
        int actualTimeout = GetInputValue("timeout", timeout);
        int actualMaxSize = GetInputValue("maxSize", maxSize);
		ElevationData elevation = GetInputValue("elevationData", elevationData);
        sendRequest(actualBoundingBox, actualTimeout, actualMaxSize, elevation, callback);

	}

	private float GetHeightOfPoint(OSMNode node, ElevationData elevation)
	{
		float x = Mathf.InverseLerp((float)elevation.box.west, (float)elevation.box.east, (float)node.lon);
        float y = Mathf.InverseLerp((float)elevation.box.south, (float)elevation.box.north, (float)node.lat);
        float width = (float)GlobeBoundingBox.LatitudeToMeters(elevation.box.north - elevation.box.south);
        return (float)elevation.SampleHeightFromPosition(new Vector3(x * width, 0, y * width));
    }

	public void sendRequest(GlobeBoundingBox boundingBox, int timeout, int maxSize, ElevationData elevation, Action<bool> callback)
	{
        string endpoint = "https://overpass.kumi.systems/api/interpreter/?";
		string query = "data=[out:json][timeout:" + timeout + "][maxsize:" + maxSize + "];node(" + boundingBox.south + "," + boundingBox.west + "," +
			boundingBox.north + "," + boundingBox.east + ");out;";
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

	[System.Serializable]
	public class OSMNodesContainer
	{
		public OSMNode[] elements;
	}
}

[System.Serializable]
public struct OSMNode
{
    public ulong id;
    public double lat;
    public double lon;
	public float altitude;
}



