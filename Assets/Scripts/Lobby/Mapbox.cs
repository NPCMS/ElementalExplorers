using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.IO;
using System.Linq;
using Unity.Mathematics;

// https://www.youtube.com/watch?v=RE_hr84pGX4

public class Mapbox : MonoBehaviour
{
    [SerializeField] private UIInteraction interaction;
    [SerializeField] private bool updateMap;
    [SerializeField] private int maxZoom, minZoom;
    [SerializeField] private GameObject marker;
    
    [Header("mapbox parameters")] 
    [SerializeField] private float centerLat;
    [SerializeField] private float centerLon;
    [SerializeField] private int zoom;
    [SerializeField] private int mapWidth, mapHeight;
    
    
    private readonly string accessToken = "pk.eyJ1IjoiZ2UyMDExOCIsImEiOiJjbGcxM3U2Ym0xMWI1M2ltc2JsMG8zNzdyIn0.DaqD9U8J05X5rxiBmPGKIg";
    private readonly int precomputeTileZoom = 15;
    private readonly string mapStyle = "light-v10";

    private Renderer renderer;
    private GlobeBoundingBox mapBb;
    
    string path;
    private void Awake()
    {
        path = Application.persistentDataPath + "/chunks/";
        renderer = gameObject.GetComponent<Renderer>();
        interaction = gameObject.GetComponent<UIInteraction>();
        // trigger for zoom in and A to zoom out
        interaction.AddCallback((RaycastHit hit, SteamInputCore.Button button) =>
        {
            Vector3 localCoords = transform.InverseTransformPoint(hit.point) / 10.0f;

            if (button == SteamInputCore.Button.Trigger && zoom != maxZoom)
            {
                zoom++;
                UpdatePosition(localCoords);
            }
            else if (button == SteamInputCore.Button.A && zoom != minZoom)
            {
                zoom--;
                UpdatePosition(Vector3.zero);
            }
        });
        
    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(GetMapBox());
    }

    // Update is called once per frame
    void Update()
    {
        if (updateMap)
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
        
        Vector2 changeInCoords = new Vector2(rayCastHit.z * (float) (mapBb.north - mapBb.south), rayCastHit.x * (float) (mapBb.east - mapBb.west)); // latitude then longitude

        centerLat -= changeInCoords.x;

        centerLon -= changeInCoords.y;
        
        StartCoroutine(GetMapBox());

    }

    IEnumerator GetMapBox()
    {
        string url = String.Format("https://api.mapbox.com/styles/v1/mapbox/{0}/static/{1},{2},{3},0,0/{4}x{5}?access_token={6}",
            mapStyle, centerLon, centerLat, zoom ,mapWidth, mapHeight, accessToken);
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(www.error);
            yield break;
        }

        // cleans up old markers
        for (int i = transform.childCount-1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
        
        renderer.material.mainTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;
            
        // get bounding box
        mapBb = TileCreation.GetBoundingBoxFromTile(TileCreation.GetTileFromCoord(centerLon, centerLat, zoom), zoom);

        double width = mapBb.east - mapBb.west;
        double height = mapBb.north - mapBb.south;
            
        // offsets bounding box to fit the map
        mapBb.north = centerLat + height / 2;
        mapBb.south = centerLat - height / 2;
        mapBb.east = centerLon + width / 2;
        mapBb.west = centerLon - width / 2;
            
        string[] fileNames = Directory.GetFiles(path);
        HashSet<Vector2> displayedTiles = new HashSet<Vector2>();
        foreach (var name in fileNames)
        {

            string fileName = name.Split('/').Last();
            string[] tileCoords = fileName.Substring(1, fileName.Length - 6).Split(", ");
                 
            Vector2 tile = new Vector2(float.Parse(tileCoords[0]), float.Parse(tileCoords[1]));
                
            int mapTileDisplayZoom = Math.Min(zoom + 4, precomputeTileZoom);

            GlobeBoundingBox tileBb = TileCreation.GetBoundingBoxFromTile(tile, precomputeTileZoom);

            if (!(tileBb.north <= mapBb.north) || !(tileBb.south >= mapBb.south) ||
                !(tileBb.east <= mapBb.east) || !(tileBb.west >= mapBb.west)) continue; // checks to see if it is in the map region
                
            Vector2 tileCenter = new Vector2((float) (tileBb.north + tileBb.south) / 2.0f, (float) (tileBb.east + tileBb.west) / 2.0f);

            Vector2 displayTile = TileCreation.GetTileFromCoord(tileCenter.y, tileCenter.x, mapTileDisplayZoom);
            if (displayedTiles.Contains(displayTile)) 
                continue;

            displayedTiles.Add(displayTile);
            GlobeBoundingBox displayTileBb =
                TileCreation.GetBoundingBoxFromTile(displayTile, mapTileDisplayZoom);
                    
            Vector2 displayTileCenter = new Vector2(
                (float) (displayTileBb.north + displayTileBb.south) / 2.0f,
                (float) (displayTileBb.east + displayTileBb.west) / 2.0f
            );
                    
            // get difference between the tile center and map center to shift marker to correct position
            Vector2 deltas = new Vector2(displayTileCenter.x - centerLat, displayTileCenter.y - centerLon); 
                    
            // translate into local space (plane is by default 10x10)
            deltas.x *= 10 / (float) height;
            deltas.y *= 10 / (float) width;

            GameObject mapMarker = Instantiate(marker, transform.TransformPoint(
                new Vector3(-deltas.y, 0, -deltas.x)), Quaternion.identity, this.transform);
            mapMarker.transform.localScale = new Vector3(
                10 * (float) (displayTileBb.north - displayTileBb.south)/(float) (mapBb.north - mapBb.south), 
                10 * (float) (displayTileBb.east - displayTileBb.west)/(float) (mapBb.east - mapBb.west),
                0.01f
            );
            mapMarker.name = tile.ToString();
        }
    }
}
