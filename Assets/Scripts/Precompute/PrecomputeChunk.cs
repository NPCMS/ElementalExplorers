using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using VContainer.Unity;

[System.Serializable]
public class PrecomputeChunk
{
    [System.Serializable]
    public class GameObjectData
    {
        public Vector3Serializable localPos;
        public Vector3Serializable localEulerAngles;
        public Vector3Serializable localScale;
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
        public Vector3Serializable localScale;
        public string prefabName;

        public PrefabData(Transform prefab, string name)
        {
            localPos = prefab.localPosition;
            localEulerAngles = prefab.localEulerAngles;
            localScale = prefab.localScale;
            prefabName = name;
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
            return new OSMRoadsData(list);
        }
    }

    public GameObjectData[] buildingData;
    public OSMRoadsDataSerializable[] roads;
    public float[] terrainHeight;
    public double minHeight, maxHeight;
    public GlobeBoundingBox coords;

    public PrecomputeChunk(GameObject[] buildings, ElevationData elevationData, OSMRoadsData[] roads, AssetDatabaseSO assetDatabase)
    {
        this.roads = roads == null ? new OSMRoadsDataSerializable[0] : new OSMRoadsDataSerializable[roads.Length];
        for (int i = 0; i < this.roads.Length; i++)
        {
            this.roads[i] = roads[i];
        }
        buildingData = new GameObjectData[buildings.Length];
        for (int i = 0; i < buildingData.Length; i++)
        {
            if (buildings[i].GetComponent<MeshFilter>() == null)
            {
                continue;
            }
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
        data.localPos = parent.localPosition;
        data.localEulerAngles = parent.eulerAngles;
        data.localScale = parent.localScale;
        data.meshInfo = new SerializableMeshInfo(parent.GetComponent<MeshFilter>().sharedMesh);
        data.materialName = parent.TryGetComponent(out MeshRenderer renderer) && renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "Default";
        List<GameObjectData> children = new List<GameObjectData>();
        List<PrefabData> prefabChildren = new List<PrefabData>();
        for (int j = 0; j < parent.childCount; j++)
        {
            Transform child = parent.GetChild(j);
            string name = child.name;
            name = name.Length > 7 ? name.Remove(name.Length - 7) : name;
            if (assetDatabase.TryGetPrefab(name, out GameObject prefab))
            {
                prefabChildren.Add(new PrefabData(child, name));
            }
            else
            {
                if (child.GetComponent<MeshFilter>() == null)
                {
                    continue;
                }
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
        go.transform.localScale = data.localScale;
        go.AddComponent<MeshRenderer>().sharedMaterial = assetDatabase.TryGetMaterial(data.materialName, out Material material) ? material : mat;
        Mesh mesh = data.meshInfo.GetMesh();
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshCollider>().sharedMesh = mesh;
        foreach (GameObjectData child in data.children)
        {
            GameObject childGO = GameObjectFromSerialisedData(child, go.transform, mat, assetDatabase);
            childGO.isStatic = true;
        }

        foreach (PrefabData prefabChild in data.prefabChildren)
        {
            if (assetDatabase.TryGetPrefab(prefabChild.prefabName, out GameObject prefab))
            {
                GameObject childGO = Object.Instantiate(prefab, go.transform);
                childGO.transform.localPosition = prefabChild.localPos;
                childGO.transform.localEulerAngles = prefabChild.localEulerAngles;
                childGO.transform.localScale = prefabChild.localScale;
                childGO.isStatic = true;
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
            // building.isStatic = true;
            buildings[i] = building;
        }

        return buildings;
    }
}
