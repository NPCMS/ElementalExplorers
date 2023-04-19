using System;
using System.Collections;
using System.Collections.Generic;
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
        string query = "data=[out:json][timeout:" + maxTime + "][maxsize:" + largestSize + "];way[building](" + bb.south + "," + bb.west + "," + bb.north + "," + bb.east + ");out;";

       
        //string query = "data=[out:json][timeout:" + maxTime + "][maxsize:" + largestSize + "];way[\"building\"](" + bb.south + "," + bb.west + "," +
        //               bb.north + "," + bb.east + ");" +
        //"way[\"building:part\"](" + bb.south + "," + bb.west + "," +
        //               bb.north + "," + bb.east + ");" +
        //               "out;";
       
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
                Debug.Log(result + " " + result.elements.Length);
                wayArray = result.elements;
                string nextQuery = "data=[out:json][timeout:" + maxTime + "][maxsize:" + largestSize + "];way[\"building:part\"](" + bb.south + "," + bb.west + "," + 
                bb.north + "," + bb.east + ");out;";
                string nextSendURL = endpoint + nextQuery;
                UnityWebRequest nextRequest = UnityWebRequest.Get(nextSendURL);

                UnityWebRequestAsyncOperation nextOperation = nextRequest.SendWebRequest();
                nextOperation.completed += _ =>
                {
                    if (nextRequest.result != UnityWebRequest.Result.Success)
                    {
                        Debug.Log(nextRequest.error);
                        callback.Invoke(false);
                    }
                    else
                    {
                        OSMWaysContainer nextResult = JsonUtility.FromJson<OSMWaysContainer>(nextRequest.downloadHandler.text.Replace("building:levels", "levels"));
                        List<OSMWay> ways = new List<OSMWay>(wayArray);
                        List<OSMWay> parts = new List<OSMWay>(nextResult.elements);
                        // for each part check if its node ids match with that of a ways.
                        foreach (OSMWay part in parts)
                        {
                            int numMatches = 0;
                            foreach (OSMWay building in ways)
                            {
                                if (numMatches > 0)
                                {
                                    break;
                                }
                                foreach (ulong node in part.nodes)
                                {
                                    if (numMatches > 0)
                                    {
                                        break;
                                    }
                                    foreach(ulong buildingNode in building.nodes)
                                    {
                                        if(node == buildingNode)
                                        {
                                            numMatches++;
                                            building.parts.Add(part);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        // if it matches, then add it to the way object.

                        wayArray = ways.ToArray();
                        //list.AddRange(nextResult.elements);
                        //wayArray = list.ToArray();
                    }
                };
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
    public List<OSMWay> parts = new List<OSMWay>();
}

[Serializable]
public struct OSMTags
{
    public string name;
    public int levels;
    public int height;
    public string area;
    public string highway;
    public string surface;
    public string amenity;
    public string building;
    public string service;
    public string landuse;
    public string shop;
}