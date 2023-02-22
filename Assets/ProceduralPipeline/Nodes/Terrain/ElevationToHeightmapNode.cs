using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using XNode;

[CreateNodeMenu("Terrain/To Heightmap")]
public class ElevationToHeightmapNode : ExtendedNode
{
	[Input] public ElevationData elevation;
	[Output] public Texture2D output;
	// Use this for initialization
	protected override void Init() {
		base.Init();

    }

    private Texture2D CreateTerrainHeightMap(ElevationData elevation)
    {
        int width = elevation.height.GetLength(0);
        Texture2D height = new Texture2D(width, width, GraphicsFormat.R16_UNorm, TextureCreationFlags.None);
        height.filterMode = FilterMode.Trilinear;
        
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < width; j++)
            {
                height.SetPixel(i, j, new Color(elevation.height[j, i], 0, 0));
            }
        }

        height.Apply();
        return height;
    }


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
		ElevationData data = GetInputValue("elevation", elevation);
        output = CreateTerrainHeightMap(data);
        Debug.Log(data.minHeight + " " + data.maxHeight + " " + (float)GlobeBoundingBox.LatitudeToMeters(data.box.north - data.box.south));
		callback.Invoke(true);
	}

    public override void Release()
    {
        base.Release();
        elevation = null;
        output = null;
    }
}