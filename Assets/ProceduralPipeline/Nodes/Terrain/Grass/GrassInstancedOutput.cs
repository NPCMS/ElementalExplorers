using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using XNode;

[CreateNodeMenu("Output/Grass Instanced Output")]
public class GrassInstancedOutput : OutputNode
{

	[Input] public ElevationData elevation;
	[Input] public Texture2D clumping;
	[Input] public Texture2D mask;
	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		return null; // Replace this
	}

	private Texture2D CreateTerrainHeightMap(ElevationData elevation)
	{
		int width = elevation.height.GetLength(0);
		Texture2D height = new Texture2D(width, width, GraphicsFormat.R16_UNorm, TextureCreationFlags.None);
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < width; j++)
			{
				height.SetPixel(i, j, new Color(elevation.height[j,i],0,0));
			}
		}

		height.Apply();
		return height;
	}

	public override void CalculateOutputs(Action<bool> callback)
	{
		callback.Invoke(true);
	}

	public override void ApplyOutput(ProceduralManager manager)
	{
		ElevationData data = GetInputValue("elevation", elevation);
		Texture2D height = CreateTerrainHeightMap(data);
		manager.ApplyInstancedGrass((float)GlobeBoundingBox.LatitudeToMeters(data.box.north - data.box.south),
			GetInputValue("clumping", clumping), GetInputValue("mask", mask), height, (float)data.minHeight, (float)data.maxHeight);
	}

	public override void Release()
	{
		base.Release();
		elevation = null;
		clumping = null;
		mask = null;
	}
}