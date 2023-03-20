using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using XNode;

[CreateNodeMenu("Nature/MapBox Water Mask")]
public class MapboxWaterMaskNode : SyncExtendedNode
{
    public const string APIKey = "pk.eyJ1IjoidXEyMDA0MiIsImEiOiJjbGVvaGVrYWYwMmVlM3lwNDFrY2kwbGFsIn0.EB_JkuEbpyJXdUyCoU0Pjg";
    [Input] public GlobeBoundingBox boundingBox;
    [Input] public int resolution = 512;
    [Input] public int zoom = 15;
    [Output] public Texture2D waterMask;

    // Use this for initialization
    protected override void Init()
    {
        base.Init();
    }

    // Return the correct value of an output port when requested
    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "waterMask")
        {
            return waterMask;
        }
        return null;
    }

    public override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        //get inputs
        int res = GetInputValue("resolution", resolution);
        GlobeBoundingBox box = GetInputValue("boundingBox", boundingBox);
        //create url
        string url = $"https://api.mapbox.com/styles/v1/uq20042/cleoh7yt6002101o752ifp68r/static/{(box.east + box.west) / 2.0},{(box.north + box.south) / 2.0},{GetInputValue("zoom", zoom)},0,0/{res}x{res}?attribution=false&logo=false&access_token={APIKey}";
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);

        //make async request
        UnityWebRequestAsyncOperation operation = request.SendWebRequest();
        //process and invoke callback on async complete
        operation.completed += (AsyncOperation operation) =>
        {
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(request.error);
                callback.Invoke(false);
            }
            else
            {
                waterMask = DownloadHandlerTexture.GetContent(request);
                callback.Invoke(true);
            }

            request.Dispose();
        };
        yield break;
    }

    public override void Release()
    {
        waterMask = null;
    }
}