using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using XNode;

[CreateNodeMenu("Grass/Merge Masks")]
public class MergeMasksNode : ExtendedNode
{
	[Input] public ComputeShader computeShader;
	[Input] public Texture2D buildingMask;
	[Input] public Texture2D waterMask;

	[Output] public Texture2D mask;
	
	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		if (port.fieldName == "mask")
		{
			return mask;
		}
		return null; // Replace this
	}

	public override void CalculateOutputs(Action<bool> callback)
	{
		int kernel = computeShader.FindKernel("CSMain");
		Texture2D building = GetInputValue("buildingMask", buildingMask);
		computeShader.SetTexture(kernel, "_BuildingMask", building);
		Texture2D water = GetInputValue("waterMask", waterMask);
		computeShader.SetTexture(kernel, "_WaterMask", water);
		int width = Mathf.Max(building.width, water.width);
		RenderTexture tex = new RenderTexture(width, width, 0, GraphicsFormat.R32_SFloat);
		tex.enableRandomWrite = true;
		tex.Create();
		computeShader.SetTexture(kernel, "Result", tex);
		computeShader.Dispatch(kernel, width / 8, width / 8, 1);
		
		RenderTexture active = RenderTexture.active;
		RenderTexture.active = tex;
		mask = new Texture2D(width, width, TextureFormat.RFloat, false);
		mask.ReadPixels(new Rect(0,0, width, width), 0, 0);
		mask.Apply();
		RenderTexture.active = active;
		tex.Release();
		callback.Invoke(true);
	}

	public override void Release()
	{
		base.Release();
		buildingMask = null;
		waterMask = null;
		mask = null;
	}

	#if UNITY_EDITOR
	public override void ApplyGUI()
	{
		base.ApplyGUI();

		if (mask != null)
		{
			EditorGUI.DrawPreviewTexture(new Rect(0,0, 256, 256), mask);
		}
	}
	#endif
}