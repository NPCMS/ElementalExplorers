using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Animations;

public class TileCreation : MonoBehaviour
{
    
    void Start()
    {
        Debug.Log(-2.606024 + " " + 51.447977);
        Vector2Int tile = GetTileFromCoord(-2.606024, 51.447977, 14.4f);
        Debug.Log(tile);
        GlobeBoundingBox box = GetBoundingBoxFromTile(tile, 14.4f);
        Debug.Log(box.west + "  " + box.south + " " + box.east +" " + box.north);
        Debug.Log(GlobeBoundingBox.LatitudeToMeters(box.north - box.south));
    }

    //https://towardsdatascience.com/map-tiles-locating-areas-nested-parent-tiles-coordinates-and-bounding-boxes-e54de570d0bd
    public static Vector2Int GetTileFromCoord(double longitude, double latitude, float level = 12)
    {
        double latRad = latitude * Math.PI / 180.0;

        int n = (int) Math.Pow(2, level);

        int xTile = (int) (n * ((longitude + 180.0) / 360.0));
        int yTile = (int)(n * (1 - (Math.Log(Math.Tan(latRad) + (1.0 / Math.Cos(latRad)))) / Math.PI) / 2.0);
        
        return new Vector2Int(xTile, yTile);
    }

    private static double LongitudeFromTile(Vector2Int tile, int n)
    {
        return 360.0 * tile.x / n - 180.0;
    }

    private static double LatitudeFromTile(Vector2Int tile, int n)
    {
        double m = Math.PI - 2.0 * Math.PI * tile.y / n;
        return (180.0 / Math.PI) * Math.Atan(0.5 * (Math.Exp(m) - Math.Exp(-m)));
    }

    public static GlobeBoundingBox GetBoundingBoxFromTile(Vector2Int tile, float level = 12)
    {
        int n = (int) Math.Pow(2, level);
        double south = LatitudeFromTile(tile, n);
        double east = LongitudeFromTile(tile, n);
        double north = LatitudeFromTile(tile + Vector2Int.up, n);
        double west = LongitudeFromTile(tile + Vector2Int.right, n);
        GlobeBoundingBox box = new GlobeBoundingBox(south, west, north, east);
        return box;
    }
}
