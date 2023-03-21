using ProceduralPipelineNodes.Nodes.Buildings;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using XNode;

[CreateNodeMenu("Roads/Fetch Ways Data")]
public class RoadFetchWaysNode : SyncExtendedNode
{
    [Input] public GlobeBoundingBox boundingBox;
    [Input] public int timeout;
    [Input] public int maxSize;
    [Input] public bool debug;
    [Output] public OSMRoadWay[] wayArray;

    // Return the correct value of an output port when requested
    public override object GetValue(NodePort port)
    {
        return port.fieldName == "wayArray" ? wayArray : null;
    }

    public override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        GlobeBoundingBox actualBoundingBox = GetInputValue("boundingBox", boundingBox);
        int actualTimeout = GetInputValue("timeout", timeout);
        int actualMaxSize = GetInputValue("maxSize", maxSize);
        SendRequest(actualBoundingBox, actualTimeout, actualMaxSize, callback);
        yield break;
    }

    private void SendRequest(GlobeBoundingBox bb, int maxTimeout, int largestSize, Action<bool> callback)
    {
        string endpoint = "https://overpass.kumi.systems/api/interpreter/?";
        string query = "data=[out:json][timeout:" + maxTimeout + "][maxsize:" + largestSize + "];way[highway](" + bb.south + "," + bb.west + "," +
                       bb.north + "," + bb.east + ");out body;>;out skel qt;";
        string sendURL = endpoint + query;


        UnityWebRequest request = UnityWebRequest.Get(sendURL);
        UnityWebRequestAsyncOperation operation = request.SendWebRequest();
        operation.completed += _ =>
        {
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(sendURL);
                Debug.Log(request.error);
                callback.Invoke(false);
            }
            else
            {
                OSMRoadWayContainer result = JsonUtility.FromJson<OSMRoadWayContainer>(request.downloadHandler.text);

                wayArray = result.elements;
                if(debug)
                {
                    foreach(OSMRoadWay way in wayArray)
                    {
				
                
                        Debug.Log("id :- " + way.id);
                        Debug.Log(way.tags);
                        Debug.Log("num of nodes:- " + way.nodes.Length);
                
                    }
                }
                Debug.Log("this many ways from server :- " + wayArray.Length);
			
                callback.Invoke(true);
            }
            request.Dispose();
        };
    }

    public override void Release()
    {
        wayArray = null;
    }
}

[Serializable]
public class OSMRoadWayContainer
{
    public OSMRoadWay[] elements;
}

[Serializable]
public class OSMRoadWay
{
    public ulong id;
    public ulong[] nodes;
    public OSMTags tags;
}