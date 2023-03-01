using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileComponent : MonoBehaviour
{
    private TerrainData terrainData;
    private Terrain terrain;

    private void Start()
    {
    }

    //Applies elevation to terrain
    public void SetTerrainElevation(ElevationData elevation)
    {
        terrainData = new TerrainData();
        Debug.Assert(elevation.height.GetLength(0) == elevation.height.GetLength(1), "Heightmap is not square, run through upsample node before output");
        terrainData.heightmapResolution = elevation.height.GetLength(0);
        double width = GlobeBoundingBox.LatitudeToMeters(elevation.box.north - elevation.box.south);
        terrainData.size = new Vector3((float)width, (float)(elevation.maxHeight - elevation.minHeight), (float)width);
        terrainData.SetHeights(0, 0, elevation.height);
        GameObject go = Terrain.CreateTerrainGameObject(terrainData);
        go.transform.SetParent(transform, true);
        terrain = go.GetComponent<Terrain>();
        transform.position = new Vector3(0, (float)elevation.minHeight, 0);
    }

    public float GetTerrainWidth()
    {
        return terrainData.size.x;
    }

    public void SetTerrainOffset(Vector2 offset)
    {
        transform.position += new Vector3(offset.x, 0, -offset.y);
    }

    public void SetNeighbours(TileComponent bottom, TileComponent top, TileComponent left, TileComponent right)
    {
        terrain.SetNeighbors(left != null ? left.terrain : null, top != null ? top.terrain : null, right != null ? right.terrain : null, bottom != null ? bottom.terrain : null);
    }
}
