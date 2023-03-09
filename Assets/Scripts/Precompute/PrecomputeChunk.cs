using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PrecomputeChunk
{
    [System.Serializable]
    public class GameObjectData
    {
        public Vector3Serializable localPos;
        public SerializableMeshInfo meshInfo;
        public List<GameObjectData> children;
        public List<PrefabData> prefabChildren;
        public int materialIndex;
    }

    [System.Serializable]
    public class PrefabData
    {
        public Vector3Serializable localPos;
        public Vector3Serializable localEulerAngles;
        public int materialIndex;
        public int prefabIndex;
    }

    [System.Serializable]
    public class OSMRoadsDataSerializable
    {
        public List<Vector3Serializable> footprint;
        public Vector3Serializable center;
        public RoadType roadType;
        public string name;

        public OSMRoadsDataSerializable(List<Vector3Serializable> footprint, Vector3Serializable center, RoadType roadType, string name)
        {
            this.footprint = footprint;
            this.center = center;
            this.roadType = roadType;
            this.name = name;
        }

        public static implicit operator OSMRoadsDataSerializable(OSMRoadsData data)
        {
            List<Vector3Serializable> list = new List<Vector3Serializable>();
            for (int i = 0; i < data.footprint.Count; i++)
            {
                list.Add(data.footprint[i]);
            }
            return new OSMRoadsDataSerializable(list, data.center, data.roadType, data.name);
        }
        public static implicit operator OSMRoadsData(OSMRoadsDataSerializable data)
        {
            List<Vector2> list = new List<Vector2>();
            for (int i = 0; i < data.footprint.Count; i++)
            {
                list.Add(data.footprint[i]);
            }
            return new OSMRoadsData(list, data.center, data.roadType, data.name);
        }
    }

    public GameObjectData[] buildingData;
    public OSMRoadsDataSerializable[] roads;
    public float[] terrainHeight;
    public double minHeight, maxHeight;
    public GlobeBoundingBox coords;

    public PrecomputeChunk(GameObject[] buildings, ElevationData elevationData, OSMRoadsData[] roads)
    {
        this.roads = new OSMRoadsDataSerializable[roads.Length];
        for (int i = 0; i < roads.Length; i++)
        {
            this.roads[i] = roads[i];
        }
        buildingData = new GameObjectData[buildings.Length];
        for (int i = 0; i < buildingData.Length; i++)
        {
            buildingData[i] = CreateBuildingData(buildings[i].transform);
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

    private GameObjectData CreateBuildingData(Transform parent)
    {
        GameObjectData data = new GameObjectData();
        data.localPos = parent.position;
        data.meshInfo = new SerializableMeshInfo(parent.GetComponent<MeshFilter>().sharedMesh);
        List<GameObjectData> children = new List<GameObjectData>();
        List<PrefabData> prefabChildren = new List<PrefabData>();
        for (int j = 0; j < parent.childCount; j++)
        {
            Transform child = parent.GetChild(j);
            if (child.GetComponent<MeshFilter>() == null)
            {
                continue;
            }
            if (child.tag == "Mesh")
            {
                children.Add(CreateBuildingData(child));
            }
            else
            {
                children.Add(CreateBuildingData(child));
            }
        }
        data.children = children;
        data.prefabChildren = prefabChildren;
        return data;
    }
}
