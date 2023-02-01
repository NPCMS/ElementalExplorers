using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class NormaliseElevationNode : ExtendedNode 
{
    [Input] public ElevationData elevation;
    [Output] public ElevationData outputElevation;
	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

    // Return the correct value of an output port when requested
    public override object GetValue(NodePort port) 
    {
        if (port.fieldName == "outputElevation")
        {
            return outputElevation;
        }
		return null; 
	}

    //Terrain heights must be between 0-1, otherwise will be clamped
    //Maps heights between these values
    private void NormaliseHeights(float[,] heights, float min, float max)
    {
        for (int x = 0; x < heights.GetLength(0); x++)
        {
            for (int y = 0; y < heights.GetLength(1); y++)
            {
                heights[x, y] = Mathf.InverseLerp(min, max, heights[x, y]);
            }
        }
    }

    public override void CalculateOutputs(Action<bool> callback)
    {
        ElevationData data = GetInputValue("elevation", elevation);
        NormaliseHeights(data.height, (float)data.minHeight, (float)data.maxHeight);
        outputElevation = data;
        callback.Invoke(true);
    }
}