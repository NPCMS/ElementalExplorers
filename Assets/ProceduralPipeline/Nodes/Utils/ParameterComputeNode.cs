using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using XNode;

[CreateNodeMenu("Utils/Parameter Compute Node")]
public class ParameterComputeNode : SyncExtendedNode {

    [Input] public ComputeShader computeShader;
    [Input] public Texture2D input;
    [Input] public float parameter;

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
    public override IEnumerator CalculateOutputs(Action<bool> callback)
	{
        ComputeShader compute = GetInputValue("computeShader", computeShader);
        int kernel = compute.FindKernel("CSMain");
        Texture2D texture = GetInputValue("input", input);
        compute.SetTexture(kernel, "Input", texture);
        compute.SetFloat("_Parameter", GetInputValue("parameter", parameter));
        int width = texture.width;
        RenderTexture tex = new RenderTexture(width, width, 0, GraphicsFormat.R32_SFloat);
        tex.enableRandomWrite = true;
        tex.Create();
        compute.SetTexture(kernel, "Result", tex);
        int groups = Mathf.CeilToInt(width / 8.0f);
        compute.Dispatch(kernel, groups, groups, 1);

        RenderTexture active = RenderTexture.active;
        RenderTexture.active = tex;
        output = new Texture2D(width, width, TextureFormat.RFloat, false);
        output.ReadPixels(new Rect(0, 0, width, width), 0, 0);
        output.Apply();
        RenderTexture.active = active;
        tex.Release();
        callback.Invoke(true);
        yield break;
    }

	public override void Release()
    {
        output = null;
        //input = null;
    }

#if UNITY_EDITOR
    public override void ApplyGUI()
    {
        base.ApplyGUI();

        if (output != null)
        {

            EditorGUILayout.LabelField(new GUIContent(output), GUILayout.Width(128), GUILayout.Height(128));
        }
    }
#endif
}