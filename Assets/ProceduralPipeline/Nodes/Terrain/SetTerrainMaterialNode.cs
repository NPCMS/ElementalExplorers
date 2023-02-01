using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class SetTerrainMaterialNode : OutputNode 
{

	[Input] public Texture2D albedo;
	[Input] public Texture2D waterMask;
	[Input] public Material material;

	// Use this for initialization
	protected override void Init() {
		base.Init();
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) 
	{
		return null; // Replace this
	}

    public override void ApplyOutput(ProceduralManager manager)
    {
        throw new NotImplementedException();
    }

    public override void CalculateOutputs(Action<bool> callback)
    {
        throw new NotImplementedException();
    }
}