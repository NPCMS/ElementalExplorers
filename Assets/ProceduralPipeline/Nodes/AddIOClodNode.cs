using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class AddIOClodNode : ExtendedNode
{
	[Input] public GameObject[] input;
	[Input] public bool setStatic;
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
		bool isStatic = GetInputValue("setStatic", setStatic);
		for (int i = 0; i < go.Length; i++)
		{
			MeshFilter[] filters = go[i].GetComponentsInChildren<MeshFilter>();
			foreach (MeshFilter filter in filters)
			{
				GameObject lodGO = filter.gameObject;
				if (isStatic)
				{
					GameObject parent = new GameObject();
					parent.transform.position = lodGO.transform.position;
					lodGO.name = "Lod_0";
					lodGO.transform.SetParent(parent.transform, true);
					lodGO = parent;
				}
				lodGO.layer = 8;
				lodGO.AddComponent<IOClod>().Static = isStatic;
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