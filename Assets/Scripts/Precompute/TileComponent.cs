using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileComponent : MonoBehaviour
{
    private TerrainData terrainData;
    private Terrain terrain;
    private ElevationData elevationData;

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
        if (left != null)
        {
            StitchToLeft(left);
        }
        if (bottom != null)
        {
            StitchToBottom(bottom);
        }
        terrain.SetNeighbors(left != null ? left.terrain : null, top != null ? top.terrain : null, right != null ? right.terrain : null, bottom != null ? bottom.terrain : null);
    }

    //https://gamedev.stackexchange.com/questions/175457/unity-seamless-terrain
    private void StitchToLeft(TileComponent leftNeighbor)
    {
        int resolution = terrainData.heightmapResolution;

        // Take the last x-column of neighbors heightmap array
        // 1 pixel wide (single x value), resolution pixels tall (all y values)
        float[,] edgeValues = leftNeighbor.terrainData.GetHeights(resolution - 1, 0, 1, resolution);
        float[,] thisEdgeValues = terrainData.GetHeights(0, 0, 1, resolution);
        for (int i = 0; i < edgeValues.GetLength(0); i++)
        {
            for (int j = 0; j < edgeValues.GetLength(1); j++)
            {
                float worldHeight = edgeValues[i, j] * (leftNeighbor.terrainData.size.y) + leftNeighbor.transform.position.y;
                float thisWorldHeight = thisEdgeValues[i, j] * terrainData.size.y + transform.position.y;
                thisEdgeValues[i, j] = ((worldHeight + thisWorldHeight) / 2f - transform.position.y) / terrainData.size.y;
                edgeValues[i, j] = ((worldHeight + thisWorldHeight) / 2f - leftNeighbor.transform.position.y) / leftNeighbor.terrainData.size.y;
            }
        }

        // Stitch with other terrain by setting same heightmap values on the edge
        terrainData.SetHeights(0, 0, thisEdgeValues);
        leftNeighbor.terrainData.SetHeights(resolution - 1, 0, edgeValues);
    }

    //https://gamedev.stackexchange.com/questions/175457/unity-seamless-terrain
    private void StitchToBottom(TileComponent bottomNeighbor)
    {
        int resolution = terrainData.heightmapResolution;

        // Take the top y-column of neighbors heightmap array
        // resolution pixels wide (all x values), 1 pixel tall (single y value)
        float[,] edgeValues = bottomNeighbor.terrainData.GetHeights(0, resolution - 1, resolution, 1);
        float[,] thisEdgeValues = terrainData.GetHeights(0, 0, resolution, 1);
        for (int i = 0; i < edgeValues.GetLength(0); i++)
        {
            for (int j = 0; j < edgeValues.GetLength(1); j++)
            {
                float worldHeight = edgeValues[i, j] * (bottomNeighbor.terrainData.size.y) + bottomNeighbor.transform.position.y;
                float thisWorldHeight = thisEdgeValues[i, j] * terrainData.size.y + transform.position.y;
                edgeValues[i, j] = ((worldHeight + thisWorldHeight) / 2f - bottomNeighbor.transform.position.y) / bottomNeighbor.terrainData.size.y;
                thisEdgeValues[i, j] = ((worldHeight + thisWorldHeight) / 2f - transform.position.y) / terrainData.size.y;
            }
        }

        // Stitch with other terrain by setting same heightmap values on the edge
        terrainData.SetHeights(0, 0, thisEdgeValues);
        bottomNeighbor.terrainData.SetHeights(0, resolution - 1, edgeValues);
    }

    public void SetMaterial(Material terrainMaterial, Texture2D waterMask)
    {
        Material mat = new Material(terrainMaterial);
        mat.SetTexture("_WaterMask", waterMask);
        terrain.materialTemplate = mat;
    }
}
