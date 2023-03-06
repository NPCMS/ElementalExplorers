using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class OutputTileNode : OutputNode {
	[Input] public ElevationData elevation;
	[Input] public Vector2Int tileIndex;
	[Input] public GameObject[] children;
	[Input] public Texture2D waterMask;
	[Input] public Texture2D grassMask;
	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		return null; // Replace this
	}

	public override void ApplyOutput(ProceduralManager manager)
	{
		manager.CreateTile(GetInputValue("elevation", elevation), GetInputValue("children", children), GetInputValue("tileIndex", tileIndex), GetInputValue("waterMask", waterMask), GetInputValue("grassMask", grassMask));
	}

	public override void CalculateOutputs(Action<bool> callback)
	{
		callback.Invoke(true);
	}

	public override void Release()
	{
		base.Release();
		elevation = null;
	}
}