using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using XNode;

[CreateNodeMenu("Precompute/Save Tile to Disk")]
public class SaveTileToDiskNode : OutputNode {
	[Input] public Vector2Int tile;
	[Input] public GameObject[] buildings;
	[Input] public OSMRoadsData[] roads;
	[Input] public ElevationData elevation;

	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		return null; // Replace this
	}

	public override void CalculateOutputs(Action<bool> callback)
	{
		callback.Invoke(true);
	}

	public override void ApplyOutput(ProceduralManager manager)
    {
        PrecomputeChunk chunk = new PrecomputeChunk(GetInputValue("buildings", buildings), GetInputValue("elevation", elevation), GetInputValue("roads", roads));
        ChunkIO.Save(GetInputValue("tile", tile).ToString() + ".rfm", chunk);
    }

	public override void Release()
	{
		base.Release();
		buildings = null;
		roads = null;
		elevation = null;
	}
}