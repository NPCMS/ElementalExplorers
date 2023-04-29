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

    public bool useDefault = true;

    private readonly List<Vector2Int> defaultTiles = new List<Vector2Int>()
    {
        new Vector2Int(16146, 10903),
        new Vector2Int(16146, 10904),
        new Vector2Int(16147, 10903),
        new Vector2Int(16147, 10904),
    };

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
        useDefault = true;
    }
    
    public void Add(Vector2Int tile)
    {
        tiles.Add(tile);
    }

    public void SetTiles(params Vector2Int[] tiles)
    {
        this.tiles = tiles.ToList();
        useDefault = false;
    }

    public List<Vector2Int> GetTiles()
    {
        return useDefault ? defaultTiles : tiles;
    }

    public void Reset()
    {
        tiles.Clear();
        useDefault = true;
        selectedCoords = new Vector2(-1, -1);
        Debug.Log("tiles reset");
    }
}
