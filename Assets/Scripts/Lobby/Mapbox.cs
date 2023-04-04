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
    [SerializeField] private UIInteraction interaction;
    [SerializeField] private bool updateMap;
    [SerializeField] private float maxZoom, minZoom;

    [Header("mapbox parameters")] 
    [SerializeField] private float centerLat;
    [SerializeField] private float centerLon;
    [SerializeField] private float zoom;
    [SerializeField] private float bearing, pitch;
    [SerializeField] private int mapWidth, mapHeight;
    
    private string url;
    private string accessToken = "pk.eyJ1IjoiZ2UyMDExOCIsImEiOiJjbGcxM3U2Ym0xMWI1M2ltc2JsMG8zNzdyIn0.DaqD9U8J05X5rxiBmPGKIg";
    private float lastLat, lastLon;
    private float lastZoom;
    private float lastPitch, lastBearing;

    private string mapStyle = "light-v10";

    private Renderer renderer;

    private void Awake()
    {
        renderer = gameObject.GetComponent<Renderer>();
        interaction = gameObject.GetComponent<UIInteraction>();
        // trigger for zoom in and A to zoom out
        interaction.AddCallback((RaycastHit hit, SteamInputCore.Button button) =>
        {
            Vector3 localCoords = transform.InverseTransformPoint(hit.point);
            if (button == SteamInputCore.Button.Trigger && zoom != maxZoom)
            {
                zoom++;
                UpdatePosition(localCoords);
            }
            else if (button == SteamInputCore.Button.A && zoom != minZoom)
            {
                zoom--;
                UpdatePosition(localCoords);
            }
        });
        
    }

    // Start is called before the first frame update
    void Start()
    {
        
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
            //rect = gameObject.GetComponent<RawImage>().rectTransform.rect;
            //mapWidth = 
            //mapHeight = (int)Math.Round(rect.height);
            StartCoroutine(GetMapBox());
            updateMap = false;
        }

        
    }

    void UpdatePosition(Vector3 rayCastHit)
    {
        //Vector2 changeInCoords = new Vector2(rayCastHit.y, rayCastHit.x); // latitude then longitude

        float maxTileWidth = 360; // width in lon when zoom is 0

        float currentTileWidth = maxTileWidth / Mathf.Pow(2, zoom);
        
        centerLat += currentTileWidth * rayCastHit.y;

        centerLon += currentTileWidth * rayCastHit.x;

        StartCoroutine(GetMapBox());

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
