using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.EventSystems;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using PathCreation;
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
        GameObject roadsParent = new GameObject("Roads");

        Material mat = GetInputValue("material", material);
        // iterate through road classes
        foreach (OSMRoadsData road in roads)
        {
            
            GameObject roadGO = CreateGameObjectFromRoadData(road, roadsParent.transform, mat);
            //GameObject roadGO = new GameObject();
            gameObjects.Add(roadGO);
        }

        roadsGameObjects = gameObjects.ToArray();
        callback.Invoke(true);
    }

    //Add empties representing the nodes
    private static void AddNodes(OSMRoadsData road, GameObject roadGO)
    {
        Vector2[] vertices = road.footprint.ToArray();
        if (vertices.Length > 1)
        {
            Debug.Log("roads have this many nodes:- " + vertices.Length);
            VertexPath vertexPath = RoadCreator.GeneratePath(vertices, false);
        }
        
        // foreach (Vector2 node in road.footprint)
        // {
        //     GameObject nodeGO = new GameObject("RoadNode");
        //     nodeGO.transform.parent = roadGO.transform;
        //     nodeGO.transform.localPosition = new Vector3(node.x, 0, node.y);
        // }

        // foreach (Vector2[] hole in road.holes)
        // {
        //     foreach (Vector2 v in hole)
        //     {
        //         GameObject nodeGO = new GameObject("Road Hole Node");
        //         nodeGO.transform.parent = roadGO.transform;
        //         nodeGO.transform.localPosition = new Vector3(v.x, 0, v.y);
        //     }
        // }
    }

    private GameObject CreateGameObjectFromRoadData(OSMRoadsData roadData, Transform parent, Material mat)
    {
        // create new game object
        GameObject temp = new GameObject(roadData.name);
        AddNodes(roadData, temp);

        temp.transform.parent = parent;
        MeshFilter meshFilter = temp.AddComponent<MeshFilter>();
        // triangulate mesh
        //bool success = WayToMesh.TryCreateRoad(roadData, out Mesh roadMesh);
        bool success = true;
        temp.name = success ? roadData.name : "Failed Road";
        // set mesh filter
        //meshFilter.sharedMesh = roadMesh;
        // add collider and renderer
        //temp.AddComponent<MeshCollider>().sharedMesh = buildingMesh;
        //temp.AddComponent<MeshRenderer>().sharedMaterial = mat;
        // apply transform updates
        //temp.transform.position = new Vector3(roadData.center.x, roadData.elevation, roadData.center.y);
        return temp;
    }
}