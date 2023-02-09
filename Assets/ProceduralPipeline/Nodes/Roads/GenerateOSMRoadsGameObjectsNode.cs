using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.EventSystems;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using XNode;

[CreateNodeMenu("Roads/Generate OSM Road GameObjects")]
public class GenerateOSMRoadsGameObjectsNode : ExtendedNode
{

    [Input] public OSMRoadsData[] roadsData;
    [Input] public Material material;
    [Output] public GameObject[] roadsGameObjects;

    // Return the correct value of an output port when requested
    public override object GetValue(NodePort port)
    {

        if (port.fieldName == "roadsGameObjects")
        {
            return roadsGameObjects;
        }
        return null;
    }

    public override void CalculateOutputs(Action<bool> callback)
    {
        // setup inputs
        OSMRoadsData[] roads = GetInputValue("roadsData", roadsData);

        // setup outputs
        List<GameObject> gameObjects = new List<GameObject>();

        // create parent game object
        GameObject buildingsParent = new GameObject("Roads");

        Material mat = GetInputValue("material", material);
        // iterate through building classes
        foreach (OSMRoadsData road in roads)
        {
            GameObject buildingGO = CreateGameObjectFromRoadData(road, buildingsParent.transform, mat);
            gameObjects.Add(buildingGO);
        }

        roadsGameObjects = gameObjects.ToArray();
        callback.Invoke(true);
    }

    //Add empties representing the nodes
    private static void AddNodes(OSMRoadsData building, GameObject buildingGO)
    {
        foreach (Vector2 node in building.footprint)
        {
            GameObject nodeGO = new GameObject("RoadNode");
            nodeGO.transform.parent = buildingGO.transform;
            nodeGO.transform.localPosition = new Vector3(node.x, 0, node.y);
        }

        foreach (Vector2[] hole in building.holes)
        {
            foreach (Vector2 v in hole)
            {
                GameObject nodeGO = new GameObject("Road Hole Node");
                nodeGO.transform.parent = buildingGO.transform;
                nodeGO.transform.localPosition = new Vector3(v.x, 0, v.y);
            }
        }
    }

    private GameObject CreateGameObjectFromRoadData(OSMRoadsData roadData, Transform parent, Material mat)
    {
        // create new game object
        GameObject temp = new GameObject();
        AddNodes(roadData, temp);

        temp.transform.parent = parent;
        MeshFilter meshFilter = temp.AddComponent<MeshFilter>();
        // triangulate mesh
        bool success = WayToMesh.TryCreateRoad(roadData, out Mesh buildingMesh);
        temp.name = success ? roadData.name : "Failed Road";
        // set mesh filter
        meshFilter.sharedMesh = buildingMesh;
        // add collider and renderer
        //temp.AddComponent<MeshCollider>().sharedMesh = buildingMesh;
        temp.AddComponent<MeshRenderer>().sharedMaterial = mat;
        // apply transform updates
        temp.transform.position = new Vector3(roadData.center.x, roadData.elevation, roadData.center.y);
        return temp;
    }
}