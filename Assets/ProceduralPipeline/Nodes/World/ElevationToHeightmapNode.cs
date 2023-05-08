using System;
using System.Collections;
using UnityEngine;
using XNode;

[CreateNodeMenu("World/Elevation to Heightmap")]
public class ElevationToHeightmapNode : SyncExtendedNode {

    [Input] public ElevationData elevation;
    [Output] public Texture2D output;
    // Use this for initialization
    protected override void Init()
    {
        base.Init();

    }

    // Return the correct value of an output port when requested
    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "output")
        {
            return output;
        }
        return null; // Replace this
    }

    private Texture2D CreateTerrainHeightMap(ElevationData elevation)
    {
        int width = elevation.height.GetLength(0);
        Texture2D height = new Texture2D(width, width, TextureFormat.RFloat, false, true);
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

    public override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        ElevationData data = GetInputValue("elevation", elevation);
        output = CreateTerrainHeightMap(data);
        yield return null;
        callback.Invoke(true);
    }

    public override void Release()
    {
        elevation = null;
        Destroy(output);
        output = null;
    }
}