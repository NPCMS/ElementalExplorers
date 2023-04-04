using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XNode;

[CreateNodeMenu("Utils/Compute Texture")]
public class ComputeTextureNode : SyncExtendedNode {

    [Input] public ComputeShader shader;
    [Input] public int width = 256;
    [Input] public int height = 256;
    [Input] public int octaves = 1;
    [Input] public float brightness = 1;
    [Input] public float scale = 10;
    [Input] public Vector2 offset;
    [Input, Range(0, 1)] public float lacunarity = 0.5f;
    [Input, Range(0, 1)] public float persistance = 0.5f;

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

    public override void Release()
    {
        output = null;
    }

#if UNITY_EDITOR
    public override void ApplyGUI()
    {
        base.ApplyGUI();

        EditorGUILayout.LabelField(new GUIContent(output), GUILayout.Width(128), GUILayout.Height(128));
    }
#endif

    public override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        shader.SetFloat("_Scale", GetInputValue("scale", scale));
        shader.SetVector("_Offset", GetInputValue("offset", offset));
        output = TextureGenerator.RenderComputeShader(GetInputValue("width", width), GetInputValue("height", height),
            GetInputValue("shader", shader), GetInputValue("brightness", brightness), GetInputValue("octaves", octaves),
            GetInputValue("lacunarity", lacunarity), GetInputValue("persistance", persistance));
        callback.Invoke(true);
        yield break;
    }
}