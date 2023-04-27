using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.VisualScripting;

[CreateAssetMenu(fileName = "SelectedTiles", menuName = "TileNames")]
public class TileInfo : ScriptableObject
{
    public List<Vector2Int> tiles = new();

    public Vector2 selectedCoords;

    public double geoLat()
    {
        return selectedCoords.x;
    }

    public double geoLon()
    {
        return selectedCoords.y;
    }

    public void Clear()
    {
        tiles.Clear();
    }
    
    public void Add(Vector2Int tile)
    {
        tiles.Add(tile);
    }

    public void Reset()
    {
        tiles.Clear();
        selectedCoords = new Vector2(-1, -1);
        Debug.Log("tiles reset");
    }
}
