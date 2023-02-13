using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XNode;

[CreateNodeMenu("Grass/Combine Masks")]
public class AddMaskNode : ExtendedNode {

	[Input] public Texture2D baseMask;
	[Input] public Texture2D addMask;
	[Output] public Texture2D outputTexture;

    // Use this for initialization
    protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		if (port.fieldName == "outputTexture")
		{
			return outputTexture;
		}
		return null; // Replace this
	}

	public override void CalculateOutputs(Action<bool> callback)
	{
		Texture2D baseTex = GetInputValue("baseMask", baseMask);
		Texture2D addTex = GetInputValue("addMask", addMask);
		bool useBaseResolution = baseTex.width > addTex.width;
		int resolution = useBaseResolution ? baseTex.width : addTex.width;
		outputTexture = new Texture2D(resolution, resolution);
		//Color[] pixels = new Color[resolution * resolution];
		for (int i = 0; i < resolution; i++)
		{
			for (int j = 0; j < resolution; j++)
			{
				float u = (float)i / resolution;
				float v = (float)j / resolution;

				float baseCol = baseTex.GetPixelBilinear(u, v).r;
				float addCol = 0;
				float col = Mathf.Clamp01(baseCol + addCol);
				outputTexture.SetPixel(i, j, new Color(col, col, col));
            }
		}
		outputTexture.Apply();
		callback.Invoke(true);
	}

	public override void ApplyGUI()
	{
		base.ApplyGUI();

        EditorGUILayout.LabelField(new GUIContent(outputTexture), GUILayout.Width(128), GUILayout.Height(128));

    }

	public override void Release()
	{
		baseMask = null;
		addMask = null;
		outputTexture = null;
	}
}