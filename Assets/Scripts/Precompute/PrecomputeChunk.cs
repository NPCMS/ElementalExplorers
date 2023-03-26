using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

[System.Serializable]
public class PrecomputeChunk
{
    [System.Serializable]
    public class SerialisedGameObjectData
    {
        public Vector3Serializable localPos;
        public Vector3Serializable localEulerAngles;
        public Vector3Serializable localScale;
        public SerializableMeshInfo meshInfo;
        public List<SerialisedGameObjectData> children;
        public List<SerialisedPrefabData> prefabChildren;
        public string materialName;
    }

    [System.Serializable]
    public class SerialisedPrefabData
    {
        public Vector3Serializable localPos;
        public Vector3Serializable localEulerAngles;
        public Vector3Serializable localScale;
        public string prefabName;

        public SerialisedPrefabData(Transform prefab, string name)
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

    public SerialisedGameObjectData[] buildingData;
    public BuildifyCityData buildifyData;
    public OSMRoadsDataSerializable[] roads;
    public float[] terrainHeight;
    public double minHeight, maxHeight;
    public GlobeBoundingBox coords;

    public PrecomputeChunk(GameObject[] buildings, BuildifyCityData buildifyData, ElevationData elevationData, OSMRoadsData[] roads, AssetDatabaseSO assetDatabase)
    {
        this.roads = roads == null ? new OSMRoadsDataSerializable[0] : new OSMRoadsDataSerializable[roads.Length];
        for (int i = 0; i < this.roads.Length; i++)
        {
            this.roads[i] = roads[i];
        }
        buildingData = new SerialisedGameObjectData[buildings.Length];
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

        this.buildifyData = buildifyData;
        minHeight = elevationData.minHeight;
        maxHeight = elevationData.maxHeight;
        coords = elevationData.box;
    }

    private SerialisedGameObjectData CreateBuildingData(Transform parent, AssetDatabaseSO assetDatabase)
    {
        SerialisedGameObjectData data = new SerialisedGameObjectData();
        data.localPos = parent.localPosition;
        data.localEulerAngles = parent.eulerAngles;
        data.localScale = parent.localScale;
        data.meshInfo = new SerializableMeshInfo(parent.GetComponent<MeshFilter>().sharedMesh);
        data.materialName = parent.TryGetComponent(out MeshRenderer renderer) && renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "Default";
        List<SerialisedGameObjectData> children = new List<SerialisedGameObjectData>();
        List<SerialisedPrefabData> prefabChildren = new List<SerialisedPrefabData>();
        for (int j = 0; j < parent.childCount; j++)
        {
            Transform child = parent.GetChild(j);
            string name = child.name;
            name = name.Length > 7 ? name.Remove(name.Length - 7) : name;
            if (assetDatabase.TryGetPrefab(name, out GameObject prefab))
            {
                prefabChildren.Add(new SerialisedPrefabData(child, name));
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

    private GameObject GameObjectFromSerialisedData(SerialisedGameObjectData data, Transform parent, Material mat, AssetDatabaseSO assetDatabase)
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
        foreach (SerialisedGameObjectData child in data.children)
        {
            GameObject childGO = GameObjectFromSerialisedData(child, go.transform, mat, assetDatabase);
            childGO.isStatic = true;
        }

        foreach (SerialisedPrefabData prefabChild in data.prefabChildren)
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

    private GameObjectData GameObjectDataFromSerialisedData(SerialisedGameObjectData data, Material defaultMaterial, AssetDatabaseSO assetDatabase)
    {
        Material mat = assetDatabase.TryGetMaterial(data.materialName, out Material material) ? material : defaultMaterial;
        SerializableMeshInfo mesh = data.meshInfo;
        GameObjectData goData = new MeshGameObjectData(data.localPos, data.localEulerAngles, data.localScale, mesh, mat);
        foreach (SerialisedGameObjectData child in data.children)
        {
            GameObjectData childGO = GameObjectDataFromSerialisedData(child, mat, assetDatabase);
            goData.children.Add(childGO);
        }

        foreach (SerialisedPrefabData prefabChild in data.prefabChildren)
        {
            if (assetDatabase.TryGetPrefab(prefabChild.prefabName, out GameObject prefab))
            {
                GameObjectData prefabChildData = new PrefabGameObjectData(prefabChild.localPos, prefabChild.localEulerAngles, prefabChild.localScale, prefab);
                goData.children.Add(prefabChildData);
            }
            else
            {
                throw new System.Exception("Prefab not found");
            }
        }
        return goData;
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

    public GameObjectData[] CreateGameObjectData(Material defaultMaterial, AssetDatabaseSO assetDatabase)
    {
        GameObjectData[] gos = new GameObjectData[buildingData.Length];
        for (int i = 0; i < buildingData.Length; i++)
        {
            GameObjectData go = GameObjectDataFromSerialisedData(buildingData[i], defaultMaterial, assetDatabase);
            gos[i] = go;
        }

        return gos;
    }

    public static PrefabGameObjectData[] GetBuildifyData(BuildifyCityData city, AssetDatabaseSO assetDatabase)
    {
        List<PrefabGameObjectData> data = new List<PrefabGameObjectData>();
        Dictionary<string, List<SerialisableTransform>> transforms =
            new Dictionary<string, List<SerialisableTransform>>();
        foreach (BuildifyBuildingData building in city.buildings)
        {
            foreach (BuildifyPrefabData prefab in building.prefabs)
            {
                if (!transforms.ContainsKey(prefab.name))
                {
                    transforms.Add(prefab.name, new List<SerialisableTransform>());
                }
                transforms[prefab.name].AddRange(prefab.transforms);
            }
        }

        foreach (KeyValuePair<string,List<SerialisableTransform>> prefab in transforms)
        {
            if (assetDatabase.TryGetPrefab(prefab.Key, out GameObject reference))
            {
                foreach (SerialisableTransform transform in prefab.Value)
                {
                    data.Add(new PrefabGameObjectData(
                        new Vector3(transform.position[0],transform.position[1],transform.position[2]), 
                        new Vector3(transform.eulerAngles[0] * Mathf.Rad2Deg, -transform.eulerAngles[1]* Mathf.Rad2Deg,transform.eulerAngles[2]* Mathf.Rad2Deg), 
                        new Vector3(transform.scale[0], transform.scale[1], transform.scale[2]), reference));
                }
            }
            else
            {
                throw new Exception("Reference not found: " + prefab.Key);
            }
        }

        return data.ToArray();
    }
    
    public GameObjectData[] GetBuildifyData(AssetDatabaseSO assetDatabase)
    {
        PrefabGameObjectData[] prefabs = GetBuildifyData(buildifyData, assetDatabase);
        GameObjectData[] data = new GameObjectData[prefabs.Length];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = prefabs[i];
        }

        return data;
    }
}
