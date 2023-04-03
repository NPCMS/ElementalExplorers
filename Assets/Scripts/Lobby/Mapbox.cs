using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

// https://www.youtube.com/watch?v=RE_hr84pGX4

public class Mapbox : MonoBehaviour
{
    [SerializeField] private float centerLat, centerLon;
    [SerializeField] private float zoom;
    [SerializeField] private float bearing, pitch;
    [SerializeField] private int mapWidth, mapHeight;
    [SerializeField] private bool updateMap;
    
    private string url;
    private string accessToken = "pk.eyJ1IjoiZ2UyMDExOCIsImEiOiJjbGcxM3U2Ym0xMWI1M2ltc2JsMG8zNzdyIn0.DaqD9U8J05X5rxiBmPGKIg";
    private Rect rect;
    private float lastLat, lastLon;
    private float lastZoom;
    private float lastPitch, lastBearing;

    private string mapStyle = "light-v10";

    private Renderer renderer;
    
    // Start is called before the first frame update
    void Start()
    {
        renderer = gameObject.GetComponent<Renderer>();
        StartCoroutine(GetMapBox());
        // mapWidth = gameObject.transform.localScale.x;
        // mapHeight = gameObject.transform.localScale.y;

    }

    

    // Update is called once per frame
    void Update()
    {
        if (updateMap && (!Mathf.Approximately(centerLat, lastLat) || !Mathf.Approximately(centerLon, lastLon) ||
                          zoom != lastZoom || bearing != lastBearing || pitch != lastPitch))
        {
            rect = gameObject.GetComponent<RawImage>().rectTransform.rect;
            //mapWidth = 
            //mapHeight = (int)Math.Round(rect.height);
            StartCoroutine(GetMapBox());
            updateMap = false;
        }
    }
    
    IEnumerator GetMapBox()
    {
        //url = "https://api.mapbox.com/styles/v1/mapbox" +  mapStyle + "/static/" + centerLon + ","
        url = String.Format("https://api.mapbox.com/styles/v1/mapbox/{0}/static/{1},{2},{3},{4},{5}/{6}x{7}?access_token={8}",
            mapStyle, centerLon, centerLat, zoom, bearing, pitch ,mapWidth, mapHeight, accessToken);
        Debug.Log(url);
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(www.error);
        }
        else
        {
            //gameObject.GetComponent<RawImage>().texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
            renderer.material.mainTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;
            lastLat = centerLat;
            lastLon = centerLon;
            lastZoom = zoom;
            lastBearing = bearing;
            lastPitch = pitch;
            
        }
    }
}
