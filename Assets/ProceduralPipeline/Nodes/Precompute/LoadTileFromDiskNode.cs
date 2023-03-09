using System;
using UnityEngine;
using XNode;


[CreateNodeMenu("Precompute/Load Tile from Disk")]
public class LoadTileFromDiskNode : ExtendedNode
{
    [Input] public Vector2Int tile;
    [Input] public Material buildingMaterial;
    [Output] public GameObject[] buildings;
    [Output] public OSMRoadsData[] roads;
    [Output] public GlobeBoundingBox boundingBox;
    [Output] public ElevationData elevation;

    // Use this for initialization
    protected override void Init()
    {
        base.Init();

    }

    // Return the correct value of an output port when requested
    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "buildings")
        {
            return buildings;
        }
        else if (port.fieldName == "roads")
        {
            return roads;
        }
        else if (port.fieldName == "elevation")
        {
            return elevation;
        }
        else if (port.fieldName == "boundingBox")
        {
            return boundingBox;
        }
        return null; // Replace this
    }

    public override void CalculateOutputs(Action<bool> callback)
    {
        PrecomputeChunk chunk = ChunkIO.LoadIn(GetInputValue("tile", tile).ToString() + ".rfm");
        Material mat = GetInputValue("buildingMaterial", buildingMaterial);
        buildings = new GameObject[chunk.buildingData.Length];
        for (int i = 0; i < buildings.Length; i++)
        {
            GameObject go = new GameObject(i.ToString());
            go.transform.position = chunk.buildingData[i].localPos;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            go.AddComponent<MeshFilter>().sharedMesh = chunk.buildingData[i].meshInfo.GetMesh();
            buildings[i] = go;
        }

        int width = (int)Mathf.Sqrt(chunk.terrainHeight.Length);
        float[,] height = new float[width, width];
        for (int i = 0; i < chunk.terrainHeight.Length; i++)
        {
            int x = i / width;
            height[i % width, x] = chunk.terrainHeight[i];
        }

        elevation = new ElevationData(height, chunk.coords, chunk.minHeight, chunk.maxHeight);

        roads = new OSMRoadsData[chunk.roads.Length];
        for (int i = 0; i < roads.Length; i++)
        {
            roads[i] = chunk.roads[i];
        }
        boundingBox = elevation.box;

        callback.Invoke(true);
    }
}