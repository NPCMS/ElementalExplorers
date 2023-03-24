using System.Collections.Generic;
using QuikGraph;
using UnityEngine;
using RoadNetworkGraph = QuikGraph.UndirectedGraph<RoadNetworkNode, QuikGraph.TaggedEdge<RoadNetworkNode, RoadNetworkEdge>>;
using RoadNetworkGraphSerialised = QuikGraph.UndirectedGraph<RoadNetworkNodeSerialised, QuikGraph.TaggedEdge<RoadNetworkNodeSerialised, RoadNetworkEdgeSerialised>>;

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

    public SerialisedGameObjectData[] buildingData;
    public RoadNetworkGraphSerialised roads;
    public float[] terrainHeight;
    public double minHeight, maxHeight;
    public GlobeBoundingBox coords;

    public PrecomputeChunk(GameObject[] buildings, ElevationData elevationData, RoadNetworkGraph roads, AssetDatabaseSO assetDatabase)
    {
        this.roads = SerializeRoadGraph(roads);
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

    public static RoadNetworkGraphSerialised SerializeRoadGraph(RoadNetworkGraph graph)
    {
        RoadNetworkGraphSerialised serializedGraph = new RoadNetworkGraphSerialised();
        foreach (var edge in graph.Edges)
        {
            var serializedSource = new RoadNetworkNodeSerialised(edge.Source.location, edge.Source.id);
            var serializedTarget = new RoadNetworkNodeSerialised(edge.Target.location, edge.Target.id);
            var tag = new RoadNetworkEdgeSerialised(edge.Tag.length, edge.Tag.type, edge.Tag.edgePoints);
            var serializedEdge = new TaggedEdge<RoadNetworkNodeSerialised, RoadNetworkEdgeSerialised>(serializedSource, serializedTarget, tag);
            serializedGraph.AddVerticesAndEdge(serializedEdge);
        }
        return serializedGraph;
    }
    
    public RoadNetworkGraph DeserializeRoadGraph()
    {
        RoadNetworkGraph deserializedGraph = new RoadNetworkGraph();
        foreach (var edge in roads.Edges)
        {
            var deserializedSource = new RoadNetworkNode(edge.Source.location, edge.Source.id);
            var deserializedTarget = new RoadNetworkNode(edge.Target.location, edge.Target.id);
            var tag = new RoadNetworkEdge(edge.Tag.length, edge.Tag.type, edge.Tag.edgePoints);
            var serializedEdge = new TaggedEdge<RoadNetworkNode, RoadNetworkEdge>(deserializedSource, deserializedTarget, tag);
            deserializedGraph.AddVerticesAndEdge(serializedEdge);
        }
        return deserializedGraph;
    }
}
