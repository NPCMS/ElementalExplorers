using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XNode;
using Random = System.Random;

[NodeWidth(250)]
[CreateNodeMenu("Buildings/Generate Building GameObjects")]
public class GenerateBuildingGameObjectsNode : SyncExtendedNode {

    [Input] public OSMBuildingData[] buildingData;
    [Input] public ElevationData elevationData;
    [Output] public GameObject[] buildingGameObjects;
    private ElevationData elevation;

    // Return the correct value of an output port when requested
    public override object GetValue(NodePort port) {

        if (port.fieldName == "buildingGameObjects")
        {
            return buildingGameObjects;
        }
        return null;
    }

    public override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        // setup inputs
        OSMBuildingData[] buildings = GetInputValue("buildingData", buildingData);
        elevation = GetInputValue("elevationData", elevationData);
    
        // setup outputs
        List<GameObject> gameObjects = new List<GameObject>();

        // create parent game object
        //GameObject buildingsParent = new GameObject("Buildings");
        // iterate through building classes
        foreach (OSMBuildingData building in buildings)
        {
            GameObject buildingGo = CreateGameObjectFromBuildingData(building, null);
            gameObjects.Add(buildingGo);
        }

        buildingGameObjects = gameObjects.ToArray();
        callback.Invoke(true);
        yield break;
    }

    //Add empties representing the nodes
    private static void AddNodes(OSMBuildingData building, GameObject buildingGo)
    {
        foreach (Vector2 node in building.footprint)
        {
            GameObject nodeGo = new GameObject("Node");
            nodeGo.transform.parent = buildingGo.transform;
            nodeGo.transform.localPosition = new Vector3(node.x, 0, node.y);
        }

        foreach (Vector2[] hole in building.holes)
        {
            foreach (Vector2 v in hole)
            {
                GameObject nodeGo = new GameObject("Hole Node");
                nodeGo.transform.parent = buildingGo.transform;
                nodeGo.transform.localPosition = new Vector3(v.x, 0, v.y);
            }
        }
    }

    private GameObject CreateGameObjectFromBuildingData(OSMBuildingData osmBuildingData, Transform parent)
    {
        // create new game object
        GameObject temp = new GameObject();
        AddNodes(osmBuildingData, temp);

        temp.transform.parent = parent;
        MeshFilter meshFilter = temp.AddComponent<MeshFilter>();
        // triangulate mesh
        bool success = WayToMesh.TryCreateBuilding(osmBuildingData, out Mesh buildingMesh);
        temp.name = success ? osmBuildingData.name : "Failed Building";
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

        temp.AddComponent<MeshRenderer>().material =
            Resources.Load<Material>(BuildingAssets.materialsPaths[seed]);
        //Debug.Log(temp.GetComponent<MeshRenderer>().sharedMaterial);
        // apply transform updates
        temp.transform.position = new Vector3(osmBuildingData.center.x, osmBuildingData.elevation, osmBuildingData.center.y);

        ////TODO case statement on grammar.

        //if (osmBuildingData.grammar == Grammars.detachedHouse)
        //{
        //    AbstractDescentParser parser = new DetachedHouseDescentParser(osmBuildingData.grammar, temp, osmBuildingData);
        //    parser.Parse(elevation);
        //}
        //else
        //{
        //    AbstractDescentParser parser = new RelationsDescentParser(osmBuildingData.grammar, temp, osmBuildingData);
        //    parser.Parse(elevation);
        //}





        return temp;
    }

    public override void Release()
    {
        buildingData = null;
        buildingGameObjects = null;
    }
}