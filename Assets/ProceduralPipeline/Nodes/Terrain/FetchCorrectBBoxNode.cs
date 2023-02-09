using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using XNode;

[CreateNodeMenu("Terrain/Bing Fetch Bounding Box")]
public class FetchCorrectBBoxNode : ExtendedNode
{
	public const string APIKey = "AtK3XHD1AaSGDXOTdtiNlf24CbNMdvGM6fRpHynP6a4RHuc3m7goqqxgunAXuEI3";

	[Input] public double longitude;
	[Input] public double latitude;
	[Input] public double width;

	[Output] public GlobeBoundingBox boundingBox;

	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) 
	{
		if (port.fieldName == "boundingBox")
		{
			return boundingBox;
		}
		return null;
	}


	public override void CalculateOutputs(Action<bool> callback)
	{
		GlobeBoundingBox box = new GlobeBoundingBox(GetInputValue("latitude", latitude), GetInputValue("longitude", longitude), GetInputValue("width", width));

		string metaURL = $"https://dev.virtualearth.net/REST/v1/Imagery/Map/Aerial?mapArea={box.south},{box.west},{box.north},{box.east}&mapSize=1024,1024&&mapLayer=Basemap&format=png&mapMetadata=1&key={APIKey}";
		UnityWebRequest request = UnityWebRequest.Get(metaURL);

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
				BingMetaData data = JsonUtility.FromJson<BingMetaData>(request.downloadHandler.text);
				double[] bb = data.resourceSets[0].resources[0].bbox;
				boundingBox = new GlobeBoundingBox(bb[2], bb[3], bb[0], bb[1]);
				callback.Invoke(true);
			}

			request.Dispose();
		};
	}

	public override void Release()
	{
	}

	[Serializable]
	private class BingMetaData
	{
		public BingResourceMetaData[] resourceSets;
	}

	[Serializable]
	private class BingResourceMetaData
	{
		public BingMeta[] resources;
	}

	[Serializable]
	private class BingMeta
	{
		public double[] bbox;
	}

}