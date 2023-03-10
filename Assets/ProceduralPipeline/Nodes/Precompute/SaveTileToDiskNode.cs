using System;
using UnityEngine;
using XNode;

[CreateNodeMenu("Precompute/Save Tile to Disk")]
public class SaveTileToDiskNode : OutputNode {
	[Input] public Vector2Int tile;
	[Input] public GameObject[] buildings;
	[Input] public OSMRoadsData[] roads;
	[Input] public ElevationData elevation;
	[Input] public AssetDatabaseSO assetdatabase;

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
		GameObject[] buildingGos = GetInputValue("buildings", buildings);
		PrecomputeChunk chunk = new PrecomputeChunk(buildingGos, GetInputValue("elevation", elevation), GetInputValue("roads", roads), GetInputValue("assetdatabase", assetdatabase));
        ChunkIO.Save(GetInputValue("tile", tile).ToString() + ".rfm", chunk);

		for (int i = 0; i < buildingGos.Length; i++)
		{
			DestroyImmediate(buildingGos[i]);
		}
    }

	public override void Release()
	{
		base.Release();
		buildings = null;
		roads = null;
		elevation = null;
	}
}