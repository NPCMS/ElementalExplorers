using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using QuikGraph;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using XNode;

public class CalculatePOI : SyncExtendedNode
{
    [Input] public GlobeBoundingBox bbox;
    [Output] public List<GeoCoordinate> pointsOfInterestOutput;
    //public GlobeBoundingBox bbox;

    public override object GetValue(NodePort port)
    {
        Debug.Log("getting values in POI node");
        if (port.fieldName == "pointsOfInterestOutput")
        {
            return pointsOfInterestOutput;

        }
        else
        {
            return null;
        }
    }

    public override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        Debug.Log("entered POI node.");
        GlobeBoundingBox boundingBox = GetInputValue("bbox", bbox);
        Debug.Log("Calculating outputs in POI node.");
        string APIkey = "5ae2e3f221c38a28845f05b6f7b0368355c2ce05ccf0a858a60e2c6f";
        var lon_min = boundingBox.west.ToString();
        var lon_max = boundingBox.east.ToString();
        var lat_min = boundingBox.south.ToString();
        var lat_max = boundingBox.north.ToString();
        
        var queryToSend =
            "http://api.opentripmap.com/0.1/en/places/bbox?lon_min=" + lon_min + "&lat_min=" + lat_min +
            "&lon_max=" + lon_max + "&lat_max=" + lat_max + "&format=json&apikey=" + APIkey;
        SendRequest(queryToSend, callback);
        yield break;
    }

    public override void Release()
    {
        // todo this should be removed later as it should be set by the previous nodes in the pipeline
        Debug.Log("release");
    }

    public void SendRequest(string query, Action<bool> callback)
    {
        Debug.Log("sending request to get POI");
        UnityWebRequest request = UnityWebRequest.Get(query);
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
                //Debug.Log(request.downloadHandler.text);
                
                string resultString =  request.downloadHandler.text ;
                resultString.Remove(0,1);
                Debug.Log(resultString);
                
                PointOfInterest[] result = JsonConvert.DeserializeObject<PointOfInterest[]>(resultString);
                Debug.Log("got this many POIs from server" + result.Length);
                List <GeoCoordinate> pois = new List<GeoCoordinate>();
                result = cullPoiToReasonableNumber(result, 15);
                for (int i = 0; i < result.Length; i++)
                {
                    pois.Add(new GeoCoordinate(result[i].point.lat, result[i].point.lon, 20f));
                }
                
                pointsOfInterestOutput = pois;
                Debug.Log("finished with POI with number of POIs:- " + pois.Count);
                callback.Invoke(true);
            }
            request.Dispose();
        };
    }
    private PointOfInterest[] cullPoiToReasonableNumber(PointOfInterest[] pois, int limit)
    {
        List<PointOfInterest> poisToReturn = new List<PointOfInterest>();
        
        //first pass getting the most important sites. h for heritage site.
        foreach (var poi in pois)
        {
            if (poisToReturn.Count >= limit)
            {
                return poisToReturn.ToArray();
            }
            if (poi.rate == "3" || poi.rate == "3h")
            {
                poisToReturn.Add(poi);
            }
        }
        
        //second pass getting the second most important sites.
        foreach (var poi in pois)
        {
            if (poisToReturn.Count > limit)
            {
                return poisToReturn.ToArray();
            }
            if (poi.rate == "2" || poi.rate == "2h")
            {
                poisToReturn.Add(poi);
            }
        }
        
        //third pass getting the third most important sites.
        foreach (var poi in pois)
        {
            if (poisToReturn.Count > limit)
            {
                return poisToReturn.ToArray();
            }
            if (poi.rate == "3" || poi.rate == "3h")
            {
                poisToReturn.Add(poi);
            }
        }
        
        //final pass in case pois don't have a rating value
        foreach (var poi in pois)
        {
            if (poisToReturn.Count > limit)
            {
                return poisToReturn.ToArray();
            }
            if (!poisToReturn.Contains(poi))
            {
                poisToReturn.Add(poi);
            }
        }
        return poisToReturn.ToArray();
    }
}




[Serializable]
public class Point
{
    public double lon { get; set; }
    public double lat { get; set; }
}

[Serializable]
public class PointOfInterest
{
    public string xid { get; set; }
    public string name { get; set; }
    public string rate { get; set; }
    public string osm { get; set; }
    public string wikidata { get; set; }
    public string kinds { get; set; }
    public Point point { get; set; }
}





    public static class JsonHelper
    {
    public static T[] FromJson<T>(string json)
    {
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
        return wrapper.Items;
    }

    public static string ToJson<T>(T[] array)
    {
        Wrapper<T> wrapper = new Wrapper<T>();
        wrapper.Items = array;
        return JsonUtility.ToJson(wrapper);
    }

    public static string ToJson<T>(T[] array, bool prettyPrint)
    {
        Wrapper<T> wrapper = new Wrapper<T>();
        wrapper.Items = array;
        return JsonUtility.ToJson(wrapper, prettyPrint);
    }

    [Serializable]
    private class Wrapper<T>
    {
        public T[] Items;
    }
}



[Serializable]
public class PoiList
{
    public POIContainer[] pois;
}

[Serializable]
public class POIContainer
{
    // public string name;
    // public string osm;
    // public string xid;
    // public string wikidata;
    // public string kind;
    // public int rate;
    public CoordinateContainer point;
}

[Serializable]
public class CoordinateContainer
{
    public double lon;
    public double lat;
}
