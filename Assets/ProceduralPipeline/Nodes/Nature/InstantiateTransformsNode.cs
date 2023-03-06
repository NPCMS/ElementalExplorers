using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[CreateNodeMenu("Terrain/Instantiate Transforms")]
public class InstantiateTransformsNode : ExtendedNode
{
	[Input] public Matrix4x4[] transforms;

	[Input] public Mesh mesh;

	[Input] public Material material;
	[Output] public GameObject[] output;

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
		Matrix4x4[] matrixes = GetInputValue("transforms", transforms);
		GameObject parent = new GameObject(mesh.name);
		parent.isStatic = true;
		Mesh m = GetInputValue("mesh", mesh);
		Material mat = GetInputValue("material", material);
		output = new GameObject[matrixes.Length];
		for (int i = 0; i < matrixes.Length; i++)
		{
			GameObject go = new GameObject(i.ToString());
			go.AddComponent<MeshFilter>().sharedMesh = m;
			go.AddComponent<MeshRenderer>().sharedMaterial = mat;
			go.transform.parent = parent.transform;
			Matrix4x4 matrix = matrixes[i];
			go.transform.SetPositionAndRotation(matrix.GetPosition(), matrix.GetRotation());
			go.transform.localScale = matrix.GetScale();
			go.isStatic = true;
			output[i] = go;
		}
		callback.Invoke(true);
	}

	public override void Release()
	{
		base.Release();
		output = null;
		transforms = null;
	}
}