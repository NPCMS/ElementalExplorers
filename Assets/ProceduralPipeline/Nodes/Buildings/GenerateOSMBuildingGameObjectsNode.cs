using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class GenerateOSMBuildingGameObjectsNode : ExtendedNode {

	[Input] public OSMBuildingData[] buildingData;
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

        // iterate through building classes
        foreach (OSMBuildingData building in buildings)
        {
             gameObjects.Add(CreateGameObjectFromBuildingData(building));
        }

        buildingGameObjects = gameObjects.ToArray();
        callback.Invoke(true);
	}

    private GameObject CreateGameObjectFromBuildingData(OSMBuildingData buildingData)
    {

        // call create game object from building
        //  triangulate
        //  create object
        //  + mesh renderer
        //  + collider
        //  + set transform
        return null;
    }
}