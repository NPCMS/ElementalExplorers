using System;
using System.Collections;
using System.Linq.Expressions;
using UnityEditor;
using UnityEngine;
using XNode;

[CreateNodeMenu("Nature/Scatter Transforms")]
public class ScatterTransformsNode : SyncExtendedNode {
    [Input] public ComputeShader scatterShader;
    [Input] public Texture2D densityMap;
    [Input] public Texture2D heightmap;
    [Input] public ElevationData elevation;
    [Input] public float cellSize = 5;
    [Input] public float minScale = 0.5f;
    [Input] public float maxScale = 2;
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
        
        yield return new WaitForSeconds(UnityEngine.Random.value);
        {
            ElevationData elevationData = GetInputValue("elevation", elevation);
            float width = (float)GlobeBoundingBox.LatitudeToMeters(elevationData.box.north - elevationData.box.south);
            float cell = GetInputValue("cellSize", cellSize);
            int instanceWidth = Mathf.FloorToInt(width / cell);

            int kernel = scatterShader.FindKernel("CSMain");
            scatterShader.SetTexture(kernel, "_DensityMap", GetInputValue("densityMap", densityMap));
            scatterShader.SetTexture(kernel, "_Heightmap", GetInputValue("heightmap", heightmap));
            scatterShader.SetFloat("_MinHeight", (float)elevationData.minHeight);
            scatterShader.SetFloat("_HeightScale", (float)(elevationData.maxHeight - elevationData.minHeight));
            scatterShader.SetFloat("_TerrainWidth", width);
            scatterShader.SetFloat("_TerrainResolution", elevationData.height.GetLength(0));
            scatterShader.SetInt("_InstanceWidth", instanceWidth);
            scatterShader.SetFloat("_MinScale", GetInputValue("minScale", minScale));
            scatterShader.SetFloat("_MaxScale", GetInputValue("maxScale", maxScale));
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
            buffer.Dispose();
        }

        yield return new WaitForEndOfFrame();
        callback.Invoke(true);
    }

	public override void Release()
    {
        Destroy(densityMap);
        Destroy(heightmap);
        transforms = null;
        elevation = null;
    }
    public override void ApplyGUI()
    {
        base.ApplyGUI();
#if UNITY_EDITOR
        EditorGUILayout.LabelField($"{transforms.Length} transforms");
#endif
    }
}