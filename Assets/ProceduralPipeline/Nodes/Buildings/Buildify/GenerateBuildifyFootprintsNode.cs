using System;
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

	private float[][] VertsToFloatArray(List<Vector3> verts)
	{
		float[][] arr = new float[verts.Count][];
		for (int i = 0; i < arr.Length; i++)
		{
			arr[i] = new float[3];
			arr[i][0] = verts[i][0];
			arr[i][1] = verts[i][1];
			arr[i][2] = verts[i][2];
		}

		return arr;
	}

	protected override void CalculateOutputsAsync(Action<bool> callback)
	{
		OSMBuildingData[] data = GetInputValue("buildingData", buildingData);
		BuildifyFootprint[] footprints = new BuildifyFootprint[data.Length];
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

			float[][] arr = VertsToFloatArray(verts);
			footprints[i] = new BuildifyFootprint()
			{
				verts = arr, height = building.buildingHeight, levels = building.buildingLevels, faces = tris.ToArray()
			};
		}

		footprintList = new BuildifyFootprintList() {footprints = footprints};
		callback.Invoke(true);
	}

	protected override void ReleaseData()
	{
		buildingData = null;
		footprintList = null;
	}
}