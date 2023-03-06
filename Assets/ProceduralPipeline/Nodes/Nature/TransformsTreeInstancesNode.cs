using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using XNode;

[System.Serializable]
public struct TerrainPropInstance
{
    public Matrix4x4 matrix;
    public int prototypeIndex;
}
[CreateNodeMenu("Terrain/Transforms to Tree Instances")]
public class TransformsTreeInstancesNode : ExtendedNode {
	[Input] public Matrix4x4[] transforms;
	[Input] public int prototypeIndex;
	[Output] public TerrainPropInstance[] treeInstances;

	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		if (port.fieldName == "treeInstances")
		{
			return treeInstances;
		}
		return null; // Replace this
	}

	public override void CalculateOutputs(Action<bool> callback)
	{
		Matrix4x4[] mats = GetInputValue("transforms", transforms);
		treeInstances = new TerrainPropInstance[mats.Length];
		int index = GetInputValue("prototypeIndex", prototypeIndex);
		for (int i = 0; i < mats.Length; i++)
		{
            TerrainPropInstance tree = new TerrainPropInstance() { prototypeIndex = index, matrix = mats[i] };
			treeInstances[i] = tree;
		}
		callback.Invoke(true);
	}

	public override void Release()
	{
		base.Release();
		transforms = null;
		treeInstances = null;
	}
}