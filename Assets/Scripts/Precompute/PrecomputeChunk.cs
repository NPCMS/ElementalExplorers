using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public class PrecomputeChunk
{
    [System.Serializable]
    public class GameObjectData
    {
        public Vector3Serializable localPos;
        public Vector3Serializable localEulerAngles;
        public SerializableMeshInfo meshInfo;
        public List<GameObjectData> children;
        public List<PrefabData> prefabChildren;
        public string materialName;
    }

    [System.Serializable]
    public class PrefabData
    {
        public Vector3Serializable localPos;
        public Vector3Serializable localEulerAngles;
        public string prefabName;

        public PrefabData(Transform prefab)
        {
            localPos = prefab.localPosition;
            localEulerAngles = prefab.localEulerAngles;
            prefabName = prefab.name;
        }
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

    public PrecomputeChunk(GameObject[] buildings, ElevationData elevationData, OSMRoadsData[] roads, AssetDatabaseSO assetDatabase)
    {
        this.roads = new OSMRoadsDataSerializable[roads.Length];
        for (int i = 0; i < roads.Length; i++)
        {
            this.roads[i] = roads[i];
        }
        buildingData = new GameObjectData[buildings.Length];
        for (int i = 0; i < buildingData.Length; i++)
        {
            buildingData[i] = CreateBuildingData(buildings[i].transform, assetDatabase);
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

    private GameObjectData CreateBuildingData(Transform parent, AssetDatabaseSO assetDatabase)
    {
        GameObjectData data = new GameObjectData();
        data.localPos = parent.position;
        data.meshInfo = new SerializableMeshInfo(parent.GetComponent<MeshFilter>().sharedMesh);
        data.materialName = parent.GetComponent<MeshRenderer>().sharedMaterial.name;
        List<GameObjectData> children = new List<GameObjectData>();
        List<PrefabData> prefabChildren = new List<PrefabData>();
        for (int j = 0; j < parent.childCount; j++)
        {
            Transform child = parent.GetChild(j);
            if (child.GetComponent<MeshFilter>() == null)
            {
                continue;
            }
            if (assetDatabase.TryGetPrefab(child.name, out GameObject prefab))
            {
                prefabChildren.Add(new PrefabData(child));
            }
            else
            {
                children.Add(CreateBuildingData(child, assetDatabase));
            }
        }
        data.children = children;
        data.prefabChildren = prefabChildren;
        return data;
    }

    private GameObject GameObjectFromSerialisedData(GameObjectData data, Transform parent, Material mat, AssetDatabaseSO assetDatabase)
    {
        GameObject go = new GameObject(data.ToString());
        go.transform.parent = parent;
        go.transform.localPosition = data.localPos;
        go.transform.localEulerAngles = data.localEulerAngles;
        go.AddComponent<MeshRenderer>().sharedMaterial = assetDatabase.TryGetMaterial(data.materialName, out Material material) ? material : mat;
        go.AddComponent<MeshFilter>().sharedMesh = data.meshInfo.GetMesh();
        foreach (GameObjectData child in data.children)
        {
            GameObject childGO = GameObjectFromSerialisedData(child, go.transform, mat, assetDatabase);
        }

        foreach (PrefabData prefabChild in data.prefabChildren)
        {
            if (assetDatabase.TryGetPrefab(prefabChild.prefabName, out GameObject prefab))
            {
                GameObject childGO = Object.Instantiate(prefab, go.transform);
                childGO.transform.localPosition = prefabChild.localPos;
                childGO.transform.localEulerAngles = prefabChild.localEulerAngles;
            }
        }
        return go;
    }

    public GameObject[] CreateBuildings(Material buildingMaterial, AssetDatabaseSO assetDatabase)
    {
        GameObject[] buildings = new GameObject[buildingData.Length];
        for (int i = 0; i < buildings.Length; i++)
        {
            GameObject building = GameObjectFromSerialisedData(buildingData[i], null, buildingMaterial, assetDatabase);
            buildings[i] = building;
        }

        return buildings;
    }
}
