using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.EventSystems;
using UnityEngine;
using XNode;

[CreateNodeMenu("Buildings/Generate OSM Building GameObjects")]
public class GenerateOSMBuildingGameObjectsNode : ExtendedNode {

	[Input] public OSMBuildingData[] buildingData;
    [Input] public Material material;
    [Output] public GameObject[] buildingGameObjects;

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
        
        // setup outputs
        List<GameObject> gameObjects = new List<GameObject>();

        // create parent game object
        GameObject buildingsParent = new GameObject("Buildings");


        Material mat = GetInputValue("material", material);
        // iterate through building classes
        foreach (OSMBuildingData building in buildings)
        {
            GameObject buildingGO = CreateGameObjectFromBuildingData(building, buildingsParent.transform, mat);
            foreach (Vector2 node in building.footprint)
            {
                GameObject nodeGO = new GameObject("Node");
                nodeGO.transform.position = new Vector3(node.x, 0, node.y);
                nodeGO.transform.parent = buildingGO.transform;
            }
            gameObjects.Add(buildingGO);
        }

        buildingGameObjects = gameObjects.ToArray();
        callback.Invoke(true);
	}

    private GameObject CreateGameObjectFromBuildingData(OSMBuildingData buildingData, Transform parent, Material mat)
    {
        // create new game object
        GameObject temp = new GameObject();

        temp.transform.parent = parent;
        MeshFilter meshFilter = temp.AddComponent<MeshFilter>();
        // triangulate mesh
        Mesh buildingMesh =  WayToMesh.CreateBuilding(buildingData.footprint.ToArray(), buildingData.buildingHeight);
        // set mesh filter
        meshFilter.sharedMesh = buildingMesh;
        // add collider and renderer
        temp.AddComponent<MeshCollider>().sharedMesh = buildingMesh;
        temp.AddComponent<MeshRenderer>().sharedMaterial = mat;
        // apply transform updates
        temp.transform.position = new Vector3(buildingData.center.x, 0, buildingData.center.y);
        return temp;
    }
}