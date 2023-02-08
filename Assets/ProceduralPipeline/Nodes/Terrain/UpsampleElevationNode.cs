using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XNode;

[CreateNodeMenu("Terrain/Upsample Elevation")]
public class UpsampleElevationNode : ExtendedNode
{

	[Input] public ElevationData elevation;
	[Input] public int extraSubdivisions = 0;
	[Input] public bool bilinear = true;
	[Output] public ElevationData outputElevation;

    private Texture2D preview;

	// Use this for initialization
	protected override void Init() {
		base.Init();

    }    
    //calculate pixels to interpolate for bilinear filtering in heightmap image
    private void GetInterpolation(int i, int lowResWidth, int width, out int from, out int to, out float t)
    {
        //proportion of the image to sample from
        float it = (float)(i + 0.5f) / width;
        //the pixel this point is inside of
        int pixelContained = Mathf.FloorToInt(it * lowResWidth);
        //proportion of the pixel
        t = (it - (float)pixelContained / lowResWidth) * lowResWidth;

        //assume closer to next pixel
        from = pixelContained;
        to = pixelContained + 1;

        //closer to previous pixel
        if (t < 0.5f)
        {
            t += 0.5f;
            from--;
            //first pixel
            if (from < 0)
            {
                from = 0;
            }
            else
            {
                to--;
            }
        }
        //closer to next pixel
        else
        {
            t -= 0.5f;
            if (to >= lowResWidth)
            {
                to = lowResWidth - 1;
            }
        }
    }

    //Unity Terrain requires the heightmap to be of a power of 2 resolution + 1 (e.g. 513x513, 1025x1025 etc.)
    //This function does bilinear filtering for a smooth upsampling
    //TODO: bicubic filtering may give even smoother results
    private float[,] SupersampleToPow2(float[,] heights, bool bilinear, int subdivisions)
    {
        int lowResWidth = heights.GetLength(0);
        int lowResHeight = heights.GetLength(1);
        //Calculate the next heightest power of two width using the highest resolution
        float power = Mathf.Log(Mathf.Max(lowResWidth, lowResHeight), 2);
        //Add one as Unity terrain expects power of 2 resolution + 1
        int width = (int)Mathf.Pow(2, Mathf.Ceil(power + subdivisions)) + 1;
        float[,] newHeights = new float[width, width];

        if (bilinear)
        {
            for (int y = 0; y < width; y++)
            {
                GetInterpolation(y, lowResHeight, width, out int yFrom, out int yTo, out float yT);
                for (int x = 0; x < width; x++)
                {
                    GetInterpolation(x, lowResWidth, width, out int xFrom, out int xTo, out float xT);
                    float yFromLerp = Mathf.Lerp((float)heights[xFrom, yFrom], (float)heights[xFrom, yTo], yT);
                    float yToLerp = Mathf.Lerp((float)heights[xTo, yFrom], (float)heights[xTo, yTo], yT);
                    newHeights[x, y] = Mathf.Lerp(yFromLerp, yToLerp, xT);
                }
            }
        }
        else
        {
            for (int y = 0; y < width; y++)
            {
                int yCoord = Mathf.FloorToInt(((float)y / width) * lowResHeight);
                for (int x = 0; x < width; x++)
                {
                    int xCoord = Mathf.FloorToInt(((float)x / width) * lowResWidth);
                    newHeights[x, y] = heights[xCoord,yCoord];
                }
            }
        }

        return newHeights;
    }

    // Return the correct value of an output port when requested
    public override object GetValue(NodePort port)
	{
		if (port.fieldName == "outputElevation")
		{
            return elevation;
		}
		return null;
	}

    public override void CalculateOutputs(Action<bool> callback)
    {
        ElevationData elevationData = GetInputValue("elevation", elevation);
        if (elevationData == null)
        {
            Debug.Log("Elevation data for upsample is null, values is unconnected or not computed");
            callback.Invoke(false);
            return;
        }
        elevationData.height = SupersampleToPow2(elevationData.height, GetInputValue("bilinear", bilinear), GetInputValue("extraSubdivisions", extraSubdivisions));
        elevation = elevationData;


        preview = new Texture2D(elevation.height.GetLength(0), elevation.height.GetLength(1));
        for (int i = 0; i < elevation.height.GetLength(0); i++)
        {
            for (int j = 0; j < elevation.height.GetLength(1); j++)
            {
                float h = elevation.height[i, j];
                preview.SetPixel(i, j, new Color(h, h, h));
            }
        }
        preview.Apply();

        callback.Invoke(true);
    }

    public override void ApplyGUI()
    {
        base.ApplyGUI();

        EditorGUILayout.LabelField(new GUIContent(preview), GUILayout.Width(128), GUILayout.Height(128));
    }
}
