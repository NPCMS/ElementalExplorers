using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[CreateNodeMenu("Buildings/Buildify/Buildify")]
public class BuildifyNode : AsyncExtendedNode
{
	[Input] public BuildifyFootprintList footprintList;

	[Output] public BuildifyCityData city;
	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		if (port.fieldName == "city")
		{
			return city;
		}
		return null; // Replace this
	}

	private BuildifyCityData Buildify(BuildifyFootprintList list)
	{
		throw new NotImplementedException();
	}

	protected override void CalculateOutputsAsync(Action<bool> callback)
	{
		BuildifyFootprintList list = GetInputValue("footprintList", footprintList);
		city = Buildify(list);
		callback.Invoke(true);
	}

	protected override void ReleaseData()
	{
		footprintList = null;
		city = null;
	}
}