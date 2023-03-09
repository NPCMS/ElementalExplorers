using System;
using UnityEngine;
using XNode;

[CreateNodeMenu("Terrain/Scatter Transforms")]
public class ScatterTransformsNode : ExtendedNode
{
	[Input] public ComputeShader scatterShader;
	[Input] public Texture2D mask;
	[Input] public Texture2D heightmap;
	[Input] public ElevationData elevation;
	[Input] public float cellSize = 5;
	[Input] public float scale = 1;
	[Input] public float scaleJitter = 0.25f;

	[Output] public Matrix4x4[] transforms;

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		if (port.fieldName == "transforms")
		{
			return transforms;
		}
		return null; // Replace this
	}

	public override void CalculateOutputs(Action<bool> callback)
	{
		ElevationData elevationData = GetInputValue("elevation", elevation);
		float width = (float)GlobeBoundingBox.LatitudeToMeters(elevationData.box.north - elevationData.box.south);
		float cell = GetInputValue("cellSize", cellSize);
		int instanceWidth = Mathf.FloorToInt(width / cell);
		
		int kernel = scatterShader.FindKernel("CSMain");
		scatterShader.SetTexture(kernel, "_Mask", GetInputValue("mask", mask));
		scatterShader.SetTexture(kernel, "_Heightmap", GetInputValue("heightmap", heightmap));
		scatterShader.SetFloat("_MinHeight", (float)elevationData.minHeight);
		scatterShader.SetFloat("_HeightScale", (float)(elevationData.maxHeight - elevationData.minHeight));
		scatterShader.SetFloat("_TerrainWidth", width);
		scatterShader.SetFloat("_TerrainResolution", elevationData.height.GetLength(0));
		scatterShader.SetInt("_InstanceWidth", instanceWidth);
		scatterShader.SetFloat("_Scale", GetInputValue("scale", scale));
		scatterShader.SetFloat("_CellSize", cell);
		scatterShader.SetFloat("_ScaleJitter", GetInputValue("scaleJitter", scaleJitter));

		ComputeBuffer buffer =
			new ComputeBuffer(instanceWidth * instanceWidth, sizeof(float) * 4 * 4, ComputeBufferType.Append);
		scatterShader.SetBuffer(kernel, "Result", buffer);
		int groups = Mathf.CeilToInt(instanceWidth / 8.0f);
		buffer.SetCounterValue(0);
		scatterShader.Dispatch(kernel, groups, groups, groups);
		transforms = new Matrix4x4[buffer.count];
		buffer.GetData(transforms);
		buffer.Dispose();
		
		callback.Invoke(true);
	}

	public override void Release()
	{
		base.Release();
		mask = null;
		heightmap = null;
		transforms = null;
		elevation = null;
	}
}