using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using XNode;

[CreateNodeMenu("Utils/Merge Masks")]
public class MergeMasksNode : SyncExtendedNode {

    [Input] public ComputeShader computeShader;
    [Input] public Texture2D mask1;
    [Input] public Texture2D mask2;

    [Output] public Texture2D mask;

    // Use this for initialization
    protected override void Init()
    {
        base.Init();

    }

    // Return the correct value of an output port when requested
    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "mask")
        {
            return mask;
        }
        return null; // Replace this
    }
    public override IEnumerator CalculateOutputs(Action<bool> callback)
	{
        int kernel = computeShader.FindKernel("CSMain");
        Texture2D firstMask = GetInputValue("mask1", mask1);
        computeShader.SetTexture(kernel, "_Mask1", firstMask);
        Texture2D secondMask = GetInputValue("mask2", mask2);
        computeShader.SetTexture(kernel, "_Mask2", secondMask);
        int width = Mathf.Max(firstMask.width, secondMask.width);
        RenderTexture tex = new RenderTexture(width, width, 0, GraphicsFormat.R32_SFloat);
        tex.enableRandomWrite = true;
        tex.Create();
        computeShader.SetTexture(kernel, "Result", tex);
        int groups = Mathf.CeilToInt(width / 8.0f);
        computeShader.Dispatch(kernel, groups, groups, 1);

        RenderTexture active = RenderTexture.active;
        RenderTexture.active = tex;
        mask = new Texture2D(width, width, TextureFormat.RFloat, false);
        mask.ReadPixels(new Rect(0, 0, width, width), 0, 0);
        mask.Apply();
        RenderTexture.active = active;
        tex.Release();
        callback.Invoke(true);
        yield break;
    }

	public override void Release()
    {
        mask1 = null;
        mask2 = null;
        mask = null;
    }

#if UNITY_EDITOR
    public override void ApplyGUI()
    {
        base.ApplyGUI();

        if (mask != null)
        {

            EditorGUILayout.LabelField(new GUIContent(mask), GUILayout.Width(128), GUILayout.Height(128));
        }
    }
#endif
}