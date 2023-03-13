using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XNode;
using Random = System.Random;

[CreateNodeMenu("Buildings/Generate OSM Building GameObjects")]
public class GenerateOSMBuildingGameObjectsNode : ExtendedNode {

	[Input] public OSMBuildingData[] buildingData;
    [Input] public Material material;
    [Input] public ElevationData elevationData;
    [Output] public GameObject[] buildingGameObjects;
    public ElevationData elevation;

    // Return the correct value of an output port when requested
    public override object GetValue(NodePort port) {

        if (port.fieldName == "buildingGameObjects")
        {
            return buildingGameObjects;
        }
        return null;
    }

	public override void CalculateOutputs(Action<bool> callback)
	{
        // setup inputs
        OSMBuildingData[] buildings = GetInputValue("buildingData", buildingData);
        elevation = GetInputValue("elevationData", elevationData);
        
        // setup outputs
        List<GameObject> gameObjects = new List<GameObject>();

        // create parent game object
        //GameObject buildingsParent = new GameObject("Buildings");

        Material mat = GetInputValue("material", material);
        // iterate through building classes
        foreach (OSMBuildingData building in buildings)
        {
            GameObject buildingGO = CreateGameObjectFromBuildingData(building, null, mat);
            gameObjects.Add(buildingGO);
        }

        buildingGameObjects = gameObjects.ToArray();
        callback.Invoke(true);
	}

    //Add empties representing the nodes
    private static void AddNodes(OSMBuildingData building, GameObject buildingGO)
    {
        foreach (Vector2 node in building.footprint)
        {
            GameObject nodeGO = new GameObject("Node");
            nodeGO.transform.parent = buildingGO.transform;
            nodeGO.transform.localPosition = new Vector3(node.x, 0, node.y);
        }

        foreach (Vector2[] hole in building.holes)
        {
            foreach (Vector2 v in hole)
            {
                GameObject nodeGO = new GameObject("Hole Node");
                nodeGO.transform.parent = buildingGO.transform;
                nodeGO.transform.localPosition = new Vector3(v.x, 0, v.y);
            }
        }
    }

    private GameObject CreateGameObjectFromBuildingData(OSMBuildingData buildingData, Transform parent, Material mat)
    {
        // create new game object
        GameObject temp = new GameObject();
        AddNodes(buildingData, temp);

        temp.transform.parent = parent;
        MeshFilter meshFilter = temp.AddComponent<MeshFilter>();
        // triangulate mesh
        bool success = WayToMesh.TryCreateBuilding(buildingData, out Mesh buildingMesh);
        temp.name = success ? buildingData.name : "Failed Building";
        // Calculate UVs
        #if UNITY_EDITOR
        Vector2[] tempMeshUVs = Unwrapping.GeneratePerTriangleUV(buildingMesh);
        Vector2[] finalUVsForMesh = new Vector2[buildingMesh.vertices.Length];
        // uvs are calculated per tri so need to merge
        for (var index = 0; index < buildingMesh.triangles.Length; index++)
        {
            finalUVsForMesh[buildingMesh.triangles[index]] = tempMeshUVs[index];
        }

        buildingMesh.uv = finalUVsForMesh;
        #endif

        // set mesh filter
        meshFilter.sharedMesh = buildingMesh;
        // add collider and renderer
        temp.AddComponent<MeshCollider>().sharedMesh = buildingMesh;
        
        Random rnd = new Random();
        int seed = rnd.Next(0, BuildingAssets.materialsPaths.Count);

        temp.AddComponent<MeshRenderer>().sharedMaterial =
            Resources.Load(BuildingAssets.materialsPaths[seed]) as Material;
        //Debug.Log(temp.GetComponent<MeshRenderer>().sharedMaterial);
        // apply transform updates
        temp.transform.position = new Vector3(buildingData.center.x, buildingData.elevation, buildingData.center.y);
        AbstractDescentParser parser = new DetachedHouseDescentParser(buildingData.grammar, temp, buildingData);
        parser.Parse(elevation);


        return temp;
    }

    public override void Release()
    {
        buildingData = null;
        buildingGameObjects = null;
    }
}