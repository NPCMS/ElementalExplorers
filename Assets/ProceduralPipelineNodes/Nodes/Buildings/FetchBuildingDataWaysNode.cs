using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using XNode;

[CreateNodeMenu("Buildings/Buildings Data Ways")]
public class FetchBuildingDataWaysNode : SyncExtendedNode
{
    [Input] public GlobeBoundingBox boundingBox;
    [Input] public int timeout;
    [Input] public int maxSize;
    [Output] public OSMWay[] wayArray;

    // Return the correct value of an output port when requested
    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "wayArray")
        {
            return wayArray;
        }
        else
        {
            return null;
        }
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
        string query = "data=[out:json][timeout:" + maxTime + "][maxsize:" + largestSize + "];way[building](" + bb.south + "," + bb.west + "," +
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

                OSMWaysContainer result = JsonUtility.FromJson<OSMWaysContainer>(request.downloadHandler.text.Replace("building:levels", "levels"));
                wayArray = result.elements;
                callback.Invoke(true);
            }
            request.Dispose();
        };
    }

    public override void Release()
    {
        wayArray = null;
    }

    [Serializable]
    public class OSMWaysContainer
    {
        public OSMWay[] elements;
    }
}


[Serializable]
public class OSMWay
{
    public ulong id;
    public ulong[] nodes;
    public OSMTags tags;
}

[Serializable]
public struct OSMTags
{
    public string name;
    public int levels;
    public int height;
    public string area;
}