using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using XNode;

[CreateNodeMenu("World/Normalise Terrain Elevation")]
public class NormaliseElevationNode : SyncExtendedNode 
{
    [Input] public ElevationData elevation;
    [Output] public ElevationData outputElevation;

    private Texture2D preview;

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
    
    public override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        ElevationData data = GetInputValue("elevation", elevation);
        NormaliseHeights(data.height, (float)data.minHeight, (float)data.maxHeight);
        outputElevation = data;

        //create preview texture from elevation data
        preview = new Texture2D(outputElevation.height.GetLength(0), outputElevation.height.GetLength(1));
        for (int i = 0; i < outputElevation.height.GetLength(0); i++)
        {
            for (int j = 0; j < outputElevation.height.GetLength(1); j++)
            {
                float h = outputElevation.height[i, j];
                preview.SetPixel(i, j, new Color(h, h, h));
            }
        }

        outputElevation.box = data.box;
        preview.Apply();
        callback.Invoke(true);
        yield break;
    }
#if UNITY_EDITOR
    public override void ApplyGUI()
    {
        base.ApplyGUI();

        EditorGUILayout.LabelField(new GUIContent(preview), GUILayout.Width(128), GUILayout.Height(128));
    }
#endif
    public override void Release()
    {
        elevation = null;
        preview = null;
        outputElevation = null;
    }
}