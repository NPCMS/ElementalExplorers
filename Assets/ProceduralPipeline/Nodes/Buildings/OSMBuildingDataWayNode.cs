﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using XNode;
using UnityEngine.Networking;

[CreateNodeMenu("Buildings/OSM Buildings Data Way")]
public class OSMBuildingDataWaysNode : ExtendedNode
{

    [Input] public GlobeBoundingBox boundingBox;
    [Input] public int timeout;
    [Input] public int maxSize;
    [Output] public OSMWay[] wayArray;
    // Use this for initialization
    protected override void Init()
    {
        base.Init();

    }

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

    public override void CalculateOutputs(Action<bool> callback)
    {
        GlobeBoundingBox actualBoundingBox = GetInputValue("boundingBox", boundingBox);
        int actualTimeout = GetInputValue("timeout", timeout);
        int actualMaxSize = GetInputValue("maxSize", maxSize);
        sendRequest(actualBoundingBox, actualTimeout, actualMaxSize, callback);

    }

    public void sendRequest(GlobeBoundingBox boundingBox, int timeout, int maxSize, Action<bool> callback)
    {
        Debug.Log(boundingBox.south);
        Debug.Log(boundingBox.west);
        Debug.Log(boundingBox.north);
        Debug.Log(boundingBox.east);

        string endpoint = "https://overpass.kumi.systems/api/interpreter/?";
        string query = "data=[out:json][timeout:" + timeout + "][maxsize:" + maxSize + "];way[building](" + boundingBox.south + "," + boundingBox.west + "," +
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

                OSMWaysContainer result = JsonUtility.FromJson<OSMWaysContainer>(request.downloadHandler.text);
                wayArray = result.elements;
                Debug.Log(wayArray.Length);
                callback.Invoke(true);
            }
            request.Dispose();
        };
    }

    [System.Serializable]
    public class OSMWaysContainer
    {
        public OSMWay[] elements;
    }
}


[System.Serializable]
public class OSMWay
{
    public int id;
    public int[] nodes;
}

