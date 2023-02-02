using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using XNode;

public class BingElevationNode : ExtendedNode
{
    public const string APIKey = "AtK3XHD1AaSGDXOTdtiNlf24CbNMdvGM6fRpHynP6a4RHuc3m7goqqxgunAXuEI3";
    private const int Width = 32;
    [Input] public GlobeBoundingBox boundingBox;
	[Output] public ElevationData elevationData;

    private Texture2D preview;

	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port)
	{
		if (port.fieldName == "elevationData")
		{
			return elevationData;
		}
		return null;
	}

    public override void CalculateOutputs(Action<bool> callback)
    {
        GlobeBoundingBox box = GetInputValue("boundingBox", boundingBox);
        //initialise url
        string url = $"https://dev.virtualearth.net/REST/v1/Elevation/Bounds?bounds={box.south},{box.west},{box.north},{box.east}&rows={Width}&cols={Width}&heights=ellipsoid&key={APIKey}";
        UnityWebRequest request = UnityWebRequest.Get(url);

        UnityWebRequestAsyncOperation operation = request.SendWebRequest();

        //on complete, finish execution and invoke callback
        operation.completed += (AsyncOperation operation) =>
        {
            //Failure
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(request.error);
                callback.Invoke(false);
            }
            else
            {
                //Convert into ElevationData and send to callback function
                BingElevationWhole elevation = JsonUtility.FromJson<BingElevationWhole>(request.downloadHandler.text);

                //write elevations
                float[,] height = new float[Width, Width];
                double minHeight = double.MaxValue;
                double maxHeight = double.MinValue;
                for (int y = 0; y < Width; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        int e = elevation.resourceSets[0].resources[0].elevations[x * Width + y];
                        minHeight = Math.Min(e, minHeight);
                        maxHeight = Math.Max(e, maxHeight);
                        height[x, y] = e;
                    }
                }

                elevationData = new ElevationData(height, box, minHeight, maxHeight);


                //create preview
                preview = new Texture2D(elevationData.height.GetLength(0), elevationData.height.GetLength(1));
                for (int i = 0; i < elevationData.height.GetLength(0); i++)
                {
                    for (int j = 0; j < elevationData.height.GetLength(1); j++)
                    {
                        float h = elevationData.height[i, j];
                        preview.SetPixel(i, j, new Color(h, h, h));
                    }
                }
                preview.Apply();
                callback.Invoke(true);
            }

            request.Dispose();
        };
    }

    public override void ApplyGUI()
    {
        base.ApplyGUI();

        EditorGUILayout.LabelField(new GUIContent(preview), GUILayout.Width(128), GUILayout.Height(128));
    }

    [System.Serializable]
    public class BingElevationWhole
    {
        public BingElevationResources[] resourceSets;
    }

    [System.Serializable]
    public class BingElevationResources
    {
        public BingElevationResource[] resources;
    }

    [System.Serializable]
    public class BingElevationResource
    {
        public int[] elevations;
    }
}