using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
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
        yield return new WaitForSeconds(UnityEngine.Random.value);
        ComputeShader shader = GetInputValue("compute", compute);
		output = TextureGenerator.RenderSDF(shader, GetInputValue("tex", tex), GetInputValue("blurIterations", blurIterations));

        yield return new WaitForEndOfFrame();
        callback.Invoke(true);
    }

	public override void Release()
	{
		Destroy(output);
		Destroy(tex);
	}


#if UNITY_EDITOR
    public override void ApplyGUI()
    {
        base.ApplyGUI();

        EditorGUILayout.LabelField(new GUIContent(output), GUILayout.Width(128), GUILayout.Height(128));
    }
#endif
}