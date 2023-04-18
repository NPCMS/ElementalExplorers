using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.Newtonsoft.Json.Utilities;
using XNode;

public class FilterPrefabsNode : SyncExtendedNode
{
	[Input] public BuildifyCityData cityData;

	[Input] public GameObject[] stage;
	[Input] public ElevationData elevationData;

	[Input] public float checkDistance = 0.1f;

	[Input] public float checkBufferDistance = 0.05f;

	[Output] public BuildifyCityData culled;

	[Output] public GameObject[] passthrough;
	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		if (port.fieldName == "culled")
		{
			return culled;
		}

		if (port.fieldName == "passthrough")
		{
			return passthrough;
		}
		return null; // Replace this
	}

	public override IEnumerator CalculateOutputs(Action<bool> callback)
	{
		SyncYieldingWait wait = new SyncYieldingWait();
		BuildifyCityData city = GetInputValue("cityData", cityData);
		float cDst = GetInputValue("checkDistance", checkDistance);
		float cbDst = GetInputValue("checkBufferDistance", checkBufferDistance);
		List<SerialisableTransform> transforms = new List<SerialisableTransform>();
		Transform temp = new GameObject().transform;
		ElevationData elevation = GetInputValue("elevationData", elevationData);
		foreach (BuildifyBuildingData building in city.buildings)
		{
			foreach (BuildifyPrefabData prefab in building.prefabs)
			{
				foreach (SerialisableTransform transform in prefab.transforms)
                {
					Vector3 angles = new Vector3(transform.eulerAngles[0] * Mathf.Rad2Deg, -transform.eulerAngles[1] * Mathf.Rad2Deg, transform.eulerAngles[2] * Mathf.Rad2Deg);
					temp.localEulerAngles = Vector3.zero;
                    temp.localPosition = new Vector3(transform.position[0], transform.position[1], transform.position[2]) + Vector3.up * 2.95f;
                    temp.localEulerAngles = angles;
					if (elevation.SampleHeightFromPositionAccurate(temp.position) <= temp.position.y && !Physics.Raycast(temp.position + temp.forward * cbDst, -temp.forward, cDst))
					{
						transforms.Add(transform);
					}
				}

				prefab.transforms = transforms.ToArray();
				transforms.Clear();

				if (wait.YieldIfTimePassed())
				{
					yield return new WaitForEndOfFrame();
				}
			}
			transforms.Clear();
		}

		culled = city;
		passthrough = GetInputValue("stage", stage);

		DestroyImmediate(temp.gameObject);

		callback.Invoke(true);
	}

	public override void Release()
	{
		culled = null;
		passthrough = null;
		stage = null;
		cityData = null;
	}
}