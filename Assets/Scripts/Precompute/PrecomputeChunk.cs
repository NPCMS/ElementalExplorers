using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PrecomputeChunk
{
    [System.Serializable]
    public class BuildingData
    {
        public Vector3 localPos;
        public SerializableMeshInfo meshInfo;
    }

    public BuildingData[] buildingData;
    public float[] terrainHeight;
    public double minHeight, maxHeight;
    public GlobeBoundingBox coords;

    public PrecomputeChunk(GameObject[] buildings, ElevationData elevationData)
    {
        buildingData = new BuildingData[buildings.Length];
        for (int i = 0; i < buildingData.Length; i++)
        {
            BuildingData data = new BuildingData();
            data.localPos = buildings[i].transform.position;
            data.meshInfo = new SerializableMeshInfo(buildings[i].GetComponent<MeshFilter>().sharedMesh);
            buildingData[i] = data;
        }

        int width = elevationData.height.GetLength(0);
        terrainHeight = new float[width * width];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < width; j++)
            {
                terrainHeight[i + j * width] = elevationData.height[i, j];
            }
        }

        minHeight = elevationData.minHeight;
        maxHeight = elevationData.maxHeight;
        coords = elevationData.box;
    }
}
