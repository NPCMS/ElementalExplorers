using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Linq;

// https://www.youtube.com/watch?v=RE_hr84pGX4

public class Mapbox : MonoBehaviour
{
    [SerializeField] private UIInteraction interaction;
    [SerializeField] private bool updateMap;
    [SerializeField] private int maxZoom, minZoom;
    [SerializeField] private GameObject marker;
    [SerializeField] private GameObject startMarker;
    
    [Header("mapbox parameters")] 
    [SerializeField] private float centerLat;
    [SerializeField] private float centerLon;
    [SerializeField] private int zoom;
    [SerializeField] private int mapWidth, mapHeight;

    private Vector2 selectedCoords;
    private float aspectRatio;
    
    private readonly string accessToken = "pk.eyJ1IjoiZ2UyMDExOCIsImEiOiJjbGcxM3U2Ym0xMWI1M2ltc2JsMG8zNzdyIn0.DaqD9U8J05X5rxiBmPGKIg";
    private readonly int precomputeTileZoom = 15;
    private readonly string mapStyle = "dark-v10";

    private Renderer renderer;
    private GlobeBoundingBox mapBb;
    HashSet<Vector2> displayedTiles = new HashSet<Vector2>();
    string path;
    private void Awake()
    {
        aspectRatio = (float)mapHeight / mapWidth;
        
        path = Application.persistentDataPath + "/chunks/";
        renderer = gameObject.GetComponent<Renderer>();
        interaction = gameObject.GetComponent<UIInteraction>();
        // trigger for zoom in and A to zoom out
        interaction.AddCallback((RaycastHit hit, SteamInputCore.Button button) =>
        {
            Vector3 localCoords = transform.InverseTransformPoint(hit.point) / 10.0f;
            Vector2 changeInCoords = new Vector2(localCoords.z * (float) (mapBb.north - mapBb.south), localCoords.x * (float) (mapBb.east - mapBb.west)); // latitude then longitude
            string startLocName = "StartLocation";
            
            
            if (button == SteamInputCore.Button.Trigger && zoom != maxZoom)
            {
                DestroyOldLocMarker(startLocName);
                displayedTiles.Clear();
                zoom += 2;
                centerLat -= changeInCoords.x;
                centerLon -= changeInCoords.y;
                StartCoroutine(UpdatePosition());
            }
            else if (button == SteamInputCore.Button.Trigger)
            {
                selectedCoords = new Vector2(centerLat - changeInCoords.x, centerLon - changeInCoords.y); // get lat and lon of the start location
                print(selectedCoords);
                foreach (var tile in displayedTiles)
                {
                    GlobeBoundingBox bb = TileCreation.GetBoundingBoxFromTile(tile, precomputeTileZoom);
                    
                    // checks if it is in a precomputed section
                    if (selectedCoords.x < bb.north && selectedCoords.x > bb.south && 
                        selectedCoords.y < bb.east && selectedCoords.y > bb.west)
                    {
                        DestroyOldLocMarker(startLocName);
                        GameObject startLocation = Instantiate(startMarker, hit.point, transform.rotation, transform);
                        startLocation.name = startLocName;
                        float planeSize = 0.4f;
                        startLocation.transform.localScale = new Vector3(planeSize * aspectRatio, 1, planeSize);
                        
                        break;
                    }
                }
                
            }
            else if (button == SteamInputCore.Button.A && zoom != minZoom)
            {
                DestroyOldLocMarker(startLocName);
                displayedTiles.Clear();
                zoom -= 2;
                StartCoroutine(UpdatePosition());
            }
        });
        
    }

    // destroys old start location marker if it exists
    private void DestroyOldLocMarker(string name)
    {
        Transform oldLoc = transform.Find(name);
        if (oldLoc != null)
        {
            Destroy(oldLoc.gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(UpdatePosition());
    }

    // Update is called once per frame
    void Update()
    {
        if (updateMap)
        {
            StartCoroutine(UpdatePosition());
            updateMap = false;
        }
    }

    IEnumerator UpdatePosition()
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
        
        foreach (var name in fileNames)
        {

            string fileName = name.Split('/').Last();
            string[] tileCoords = fileName.Substring(1, fileName.Length - 6).Split(", ");
                 
            Vector2 tile = new Vector2(float.Parse(tileCoords[0]), float.Parse(tileCoords[1]));
                
            int mapTileDisplayZoom = Math.Min(zoom + 4, precomputeTileZoom);

            GlobeBoundingBox tileBb = TileCreation.GetBoundingBoxFromTile(tile, precomputeTileZoom);

            if (mapBb.east < tileBb.west || mapBb.west > tileBb.east ||  mapBb.north < tileBb.south || mapBb.south > tileBb.north)
                continue;
            
            Vector2 tileCenter = new Vector2((float) (tileBb.north + tileBb.south) / 2.0f, (float) (tileBb.east + tileBb.west) / 2.0f);

            Vector2 displayTile = TileCreation.GetTileFromCoord(tileCenter.y, tileCenter.x, mapTileDisplayZoom);
            if (displayedTiles.Contains(displayTile)) 
                continue;

            displayedTiles.Add(displayTile);
            GlobeBoundingBox displayTileBb =
                TileCreation.GetBoundingBoxFromTile(displayTile, mapTileDisplayZoom);
            
            // shrink bounding box within map boundary
            displayTileBb.north = Math.Min(mapBb.north, displayTileBb.north);
            displayTileBb.east = Math.Min(mapBb.east, displayTileBb.east);
            displayTileBb.south = Math.Max(mapBb.south, displayTileBb.south);
            displayTileBb.west = Math.Max(mapBb.west, displayTileBb.west);
            
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
                new Vector3(-deltas.y  * aspectRatio, 0, -deltas.x)), transform.rotation, transform);
            mapMarker.transform.localScale = new Vector3(
                10 * (float) (displayTileBb.east - displayTileBb.west) / (float) (mapBb.east - mapBb.west) * aspectRatio, 
                0.01f,
                10 * (float) (displayTileBb.north - displayTileBb.south) / (float) (mapBb.north - mapBb.south)
                
            );
            // mapMarker.transform.Rotate(0, 0, 90);
            mapMarker.name = tile.ToString();
        }
    }
    
    public Vector2 SelectedCoords
    {
        get => selectedCoords;
        set => selectedCoords = value;
    }
}
