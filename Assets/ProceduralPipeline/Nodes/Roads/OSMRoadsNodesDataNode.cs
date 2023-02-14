using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;
using UnityEngine.Networking;

[CreateNodeMenu("Roads/OSM Roads Nodes Data")]
public class OSMRoadsNodesDataNode : ExtendedNode {

	[Input] public GlobeBoundingBox boundingBox;
    [Input] public ElevationData elevationData;
	[Input] public int timeout;
	[Input] public int maxSize;
    [Input] public bool debug;
	
	[Output] public OSMRoadNode[] roadNodesArray;


	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

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
        ElevationData elevation = GetInputValue("elevationData", elevationData);
        sendRequest(actualBoundingBox, actualTimeout, actualMaxSize, callback, elevation);
    }

    private float GetHeightOfPoint(OSMRoadNode node, ElevationData elevation)
	{
		float x = Mathf.InverseLerp((float)elevation.box.west, (float)elevation.box.east, (float)node.lon);
        float y = Mathf.InverseLerp((float)elevation.box.south, (float)elevation.box.north, (float)node.lat);
		int res = elevation.height.GetLength(0) - 1;

        return elevation.height[(int)(y * res), (int)(x * res)] * (float)(elevation.maxHeight - elevation.minHeight) + (float)elevation.minHeight;
    }

	public void sendRequest(GlobeBoundingBox boundingBox, int timeout, int maxSize, Action<bool> callback, ElevationData elevation)
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
                    
					roadNodesArray[i].altitude = GetHeightOfPoint(roadNodesArray[i], elevation);
                    OSMRoadNode node = roadNodesArray[i];
                    if(debug)
                    {
                        Debug.Log("id :- " + node.id + " longitude:- " + node.lon + " latitude:- " + node.lat + " altitude:- " + node.altitude);
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

}

[System.Serializable]
public struct OSMRoadNode
{
    public int id;
    public double lon;
    public double lat;
	public OSMTags tags;
    public float altitude;
}

[System.Serializable]
public struct OSMRoadsNodeContainer
{
	public OSMRoadNode[] elements;
}