using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using XNode;

public class TextureGradientNode : ExtendedNode {
	[Input] public Texture2D texture;
	[Input] public ComputeShader shader;
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
		ComputeShader compute = GetInputValue("shader", shader);
		int kernel = shader.FindKernel("CSMain");
		Texture2D input = GetInputValue("texture", texture);
        shader.SetTexture(kernel, "Input", input);
		RenderTexture rTex = new RenderTexture(input.width, input.height, 0, RenderTextureFormat.ARGBFloat);
		rTex.enableRandomWrite = true;
		rTex.Create();
        shader.SetTexture(kernel, "Result", rTex);
		shader.Dispatch(kernel, input.width / 8, input.height / 8, 1);

		RenderTexture active = RenderTexture.active;
		RenderTexture.active = rTex;
        outputTexture = new Texture2D(input.width, input.height, TextureFormat.RGBAFloat, false);
        outputTexture.ReadPixels(new Rect(0, 0, input.width, input.height), 0, 0);
		outputTexture.Apply();

        rTex.Release();
		RenderTexture.active = active;
        callback.Invoke(true);
    }

    public override void Release()
    {
        base.Release();
        texture = null;
        outputTexture = null;
    }

#if UNITY_EDITOR
    public override void ApplyGUI()
	{
		base.ApplyGUI();

        EditorGUILayout.LabelField(new GUIContent(outputTexture), GUILayout.Width(128), GUILayout.Height(128));
    }
#endif
}