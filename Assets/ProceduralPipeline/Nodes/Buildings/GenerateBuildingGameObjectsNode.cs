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
    [Output] public GameObject[] roofs;

    // Return the correct value of an output port when requested
    public override object GetValue(NodePort port) {

        if (port.fieldName == "buildingGameObjects")
        {
            return buildingGameObjects;
        }
        else if (port.fieldName == "roofs")
        {
            return roofs;
        }
        return null;
    }

    public override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        // setup inputs
        OSMBuildingData[] buildings = GetInputValue("buildingData", buildingData);
        ElevationData elevation = GetInputValue("elevationData", elevationData);
    
        // setup outputs
        List<GameObject> gameObjects = new List<GameObject>();
        List<GameObject> roofGos = new List<GameObject>();

        // create parent game object
        //GameObject buildingsParent = new GameObject("Buildings");
        // iterate through building classes
        foreach (OSMBuildingData building in buildings)
        { 
            CreateGameObjectFromBuildingData(building, null, gameObjects, roofGos);
        }

        buildingGameObjects = gameObjects.ToArray();
        roofs = roofGos.ToArray();
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

    private void CreateGameObjectFromBuildingData(OSMBuildingData osmBuildingData, Transform parent, List<GameObject> buildings, List<GameObject> roofs)
    {
        // create new game object
        GameObject temp = new GameObject();
        //AddNodes(osmBuildingData, temp);

        temp.transform.parent = parent;
        MeshFilter meshFilter = temp.AddComponent<MeshFilter>();
        // triangulate mesh
        bool success = WayToMesh.TryCreateBuilding(osmBuildingData, out Mesh buildingMesh);
        temp.name = success ? osmBuildingData.name : "Failed Building";
        // Calculate UVs
#if UNITY_EDITOR
        // Vector2[] tempMeshUVs = Unwrapping.GeneratePerTriangleUV(buildingMesh);
        // Vector2[] finalUVsForMesh = new Vector2[buildingMesh.vertices.Length];
        // // uvs are calculated per tri so need to merge
        // for (var index = 0; index < buildingMesh.triangles.Length; index++)
        // {
        //     finalUVsForMesh[buildingMesh.triangles[index]] = tempMeshUVs[index];
        // }
        //
        // buildingMesh.uv = finalUVsForMesh;
#endif

        // set mesh filter
        meshFilter.sharedMesh = buildingMesh;
        // add collider and renderer
        temp.AddComponent<MeshCollider>().sharedMesh = buildingMesh;

        Random rnd = new Random();
        int seed = rnd.Next(0, BuildingAssets.materialsPaths.Count);

        Material mat = Resources.Load<Material>(BuildingAssets.materialsPaths[seed]);
        temp.AddComponent<MeshRenderer>().sharedMaterial =
            mat;
        temp.transform.position = new Vector3(osmBuildingData.center.x, osmBuildingData.elevation, osmBuildingData.center.y);
        if (success)
        {
            GameObject roof;
            if (DataToObjects.CreateRoof(temp, String.Empty, elevationData, osmBuildingData, out roof))
            {
                roof.transform.parent = parent;
                roof.name = osmBuildingData.name + " Roof";
                roofs.Add(roof);
            }

            success = WayToMesh.CreateRoofMesh(osmBuildingData, out Mesh roofMesh);
            if (success)
            {
                roof = new GameObject();
                roof.transform.position = new Vector3(osmBuildingData.center.x, osmBuildingData.elevation, osmBuildingData.center.y);
                roof.AddComponent<MeshFilter>().sharedMesh = roofMesh;
                roof.AddComponent<MeshRenderer>().sharedMaterial = mat;
                roof.transform.parent = parent;
                roof.name = osmBuildingData.name + " Roof";
            }

            roofs.Add(roof);
        }
        //Debug.Log(temp.GetComponent<MeshRenderer>().sharedMaterial);
        // apply transform updates

        ////TODO case statement on grammar.

        //if (osmBuildingData.grammar == Grammars.detachedHouse)
        //{
        //    AbstractDescentParser parser = new DetachedHouseDescentParser(osmBuildingData.grammar, temp, osmBuildingData);
        //    parser.Parse(elevation);
        //}
        //else
        //{
        //AbstractDescentParser parser = new RelationsDescentParser(osmBuildingData.grammar, temp, osmBuildingData);
        //    parser.Parse(elevation);
        //}


        buildings.Add(temp);
    }

    public override void Release()
    {
        buildingData = null;
        roofs = null;
        buildingGameObjects = null;
    }
}