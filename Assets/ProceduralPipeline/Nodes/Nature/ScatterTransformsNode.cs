﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using UnityEngine;
using XNode;

[CreateNodeMenu("Nature/Scatter Transforms")]
public class ScatterTransformsNode : SyncExtendedNode {
    [Input] public ComputeShader scatterShader;
    [Input] public Texture2D mask;
    [Input] public Texture2D heightmap;
    [Input] public ElevationData elevation;
    [Input] public float cellSize = 5;
    [Input] public float scale = 1;
    [Input] public float scaleJitter = 0.25f;

    [Output] public Matrix4x4[] transforms;

    // Return the correct value of an output port when requested
    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "transforms")
        {
            return transforms;
        }
        return null; // Replace this
    }

    public override IEnumerator CalculateOutputs(Action<bool> callback)
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
        buffer.SetCounterValue(0);
        scatterShader.SetBuffer(kernel, "Result", buffer);
        int groups = Mathf.CeilToInt(instanceWidth / 8.0f);
        scatterShader.Dispatch(kernel, groups, groups, groups);
        transforms = new Matrix4x4[buffer.count];
        buffer.GetData(transforms);
        Debug.Log(transforms.Length);
        for (int i = 0; i < transforms.Length; i++)
        {
            Debug.Log(transforms[i].GetPosition());
        }

        buffer.Dispose();

        callback.Invoke(true);
        yield break;
    }

	public override void Release()
	{
        mask = null;
        heightmap = null;
        transforms = null;
        elevation = null;
    }
}