using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class AddIOClodNode : ExtendedNode
{
	[Input] public GameObject[] input;
	[Output] public GameObject[] output;
	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		if (port.fieldName == "output")
		{
			return output;
		}
		return null; // Replace this
	}

	public override void CalculateOutputs(Action<bool> callback)
	{
		GameObject[] go = GetInputValue("input", input);
		for (int i = 0; i < go.Length; i++)
		{
			MeshFilter[] filters = go[i].GetComponentsInChildren<MeshFilter>();
			foreach (MeshFilter filter in filters)
			{
				filter.gameObject.layer = 8;
				filter.gameObject.AddComponent<IOClod>();
			}
			// go[i].layer = 8;
			// go[i].AddComponent<IOClod>();
		}

		output = go;
		callback.Invoke(true);
	}

	public override void Release()
	{
		base.Release();
		input = null;
		output = null;
	}
}