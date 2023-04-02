﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[CreateNodeMenu("Buildings/Buildify/Generate Footprints")]
public class GenerateBuildifyFootprintsNode : AsyncExtendedNode
{
	[Input] public OSMBuildingData[] buildingData;

	[Output] public BuildifyFootprintList footprintList;
	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		if (port.fieldName == "footprintList")
		{
			return footprintList;
		}
		return null; // Replace this
	}

	private float[][] VertsToFloatArray(List<Vector3> verts, OSMBuildingData building)
	{
		float[][] arr = new float[verts.Count][];
		for (int i = 0; i < arr.Length; i++)
		{
			arr[i] = new float[3];
			arr[i][0] = verts[i][0] + building.center.x;
            arr[i][1] = verts[i][2] + building.center.y;
            arr[i][2] = verts[i][1] + building.elevation;
		}

		return arr;
	}

	protected override void CalculateOutputsAsync(Action<bool> callback)
	{
		OSMBuildingData[] data = GetInputValue("buildingData", buildingData);
		List<BuildifyFootprint> footprints = new List<BuildifyFootprint>();
		for (int i = 0; i < data.Length; i++)
		{
			OSMBuildingData building = data[i];
			List<Vector3> verts = new List<Vector3>();
			List<int> tris = new List<int>();
			if (!WayToMesh.CreateFootprint(building, verts, tris))
			{
				Debug.Log("Failed building: " + building.name);
				continue;
			}

			float[][] arr = VertsToFloatArray(verts, building);
			footprints.Add(new BuildifyFootprint()
			{
				verts = arr, height = building.buildingHeight, levels = building.buildingLevels, faces = tris.ToArray()
			});
		}

		footprintList = new BuildifyFootprintList() {footprints = footprints.ToArray()};
		callback.Invoke(true);
	}

	protected override void ReleaseData()
	{
		buildingData = null;
		footprintList = null;
	}
}