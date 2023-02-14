using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Unity.VisualStudio.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class TextureGeneratorWindow : EditorWindow
{
    private ComputeShader computeShader;
    private int width, height;
    private Vector2 offset;
    private float scale = 10;
    private float brightness = 1;
    private int octaves = 1;
    private float lacunarity = 0.5f;
    private float persistance = 0.5f;
    private Texture2D noiseTexture;
    [MenuItem("Window/Texture Generator")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        TextureGeneratorWindow window = (TextureGeneratorWindow)EditorWindow.GetWindow(typeof(TextureGeneratorWindow));
        window.Show();
    }

    private void OnGUI()
    {
        computeShader = (ComputeShader)EditorGUILayout.ObjectField(computeShader, typeof(ComputeShader), false);
        width = EditorGUILayout.IntField("Width", width);
        height = EditorGUILayout.IntField("Height", height);
        offset = EditorGUILayout.Vector2Field("Offset", offset);
        scale = EditorGUILayout.FloatField("Scale", scale);
        brightness = EditorGUILayout.FloatField("Brightness", brightness);
        octaves = EditorGUILayout.IntSlider("Octavse", octaves, 1, 10);
        lacunarity = EditorGUILayout.Slider("Lacunarity", lacunarity, 0, 1);
        persistance = EditorGUILayout.Slider("Persistance", persistance, 0, 1);

        computeShader.SetFloat("_Scale", scale);
        computeShader.SetVector("_Offset", offset);
        noiseTexture = TextureGenerator.RenderComputeShader(width, height, computeShader, brightness, octaves,
            lacunarity, persistance);
        if (noiseTexture != null)
        {
            EditorGUI.DrawPreviewTexture( new Rect(100,260, 128, 128), noiseTexture);
        }
    }
}
