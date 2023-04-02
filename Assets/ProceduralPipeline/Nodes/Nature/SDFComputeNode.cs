using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using XNode;

[CreateNodeMenu("Nature/SDF Texture")]
public class SDFComputeNode : SyncExtendedNode {
	[Input] public ComputeShader compute;
	[Input] public Texture2D tex;
	[Input] public int blurIterations = 500;
	[Output] public Texture2D output;
	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		if (port.fieldName == "output")
		{
			return output;
		}
		return null; // Replace this
	}

	public override IEnumerator CalculateOutputs(Action<bool> callback)
	{
		ComputeShader shader = GetInputValue("compute", compute);
		output = TextureGenerator.RenderSDF(shader, GetInputValue("tex", tex), GetInputValue("blurIterations", blurIterations));
        callback.Invoke(true);
        yield break;
    }

	public override void Release()
	{
		output = null;
		tex = null;
	}


#if UNITY_EDITOR
    public override void ApplyGUI()
    {
        base.ApplyGUI();

        EditorGUILayout.LabelField(new GUIContent(output), GUILayout.Width(128), GUILayout.Height(128));
    }
#endif
}