using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using XNode;

public class BingSatelliteNode : ExtendedNode
{
	public const string APIKey = "AtK3XHD1AaSGDXOTdtiNlf24CbNMdvGM6fRpHynP6a4RHuc3m7goqqxgunAXuEI3";
    public const string MapType = "Aerial";
    [Input] public GlobeBoundingBox boundingBox;
	[Input] public int resolution = 1024;
	[Output] public Texture2D satelliteImage;

    // Use this for initialization
    protected override void Init() {
		base.Init();
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) 
	{
		if (port.fieldName == "satelliteImage")
		{
			return satelliteImage;
		}
		return null;
	}

    public override void CalculateOutputs(Action<bool> callback)
    {
		//get inputs
		int res = GetInputValue("resolution", resolution);
        GlobeBoundingBox box = GetInputValue("boundingBox", boundingBox);
		//create url
        string url = $"https://dev.virtualearth.net/REST/v1/Imagery/Map/{MapType}?mapArea={box.south},{box.west},{box.north},{box.east}&mapSize={res},{res}&format=png&mapMetadata=0&key={APIKey}";
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
				//download texture initialising output
				satelliteImage = DownloadHandlerTexture.GetContent(request);
				callback.Invoke(true);
			}

			request.Dispose();
		};
    }

    public override void ApplyGUI()
    {
        base.ApplyGUI();

		EditorGUILayout.LabelField(new GUIContent(satelliteImage), GUILayout.Width(128), GUILayout.Height(128));
	}
}