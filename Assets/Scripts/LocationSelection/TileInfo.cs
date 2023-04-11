using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(fileName = "SelectedTiles", menuName = "TileNames")]
public class TileInfo : ScriptableObject
{
    public List<Vector2Int> tiles;

    public List<Vector2Int> GetTiles()
    {
        return tiles;
    }

    public void SetTiles(string[] tiles)
    {
        foreach (var tile in tiles)
        {
            string[] latLong = tile.Split(' ');
            this.tiles.Add(new Vector2Int(int.Parse(latLong[0]), int.Parse(latLong[1])));
        }
    }

    
}
