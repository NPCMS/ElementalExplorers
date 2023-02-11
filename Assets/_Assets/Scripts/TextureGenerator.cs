using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TextureGenerator : MonoBehaviour
{
    private static readonly int Scale = Shader.PropertyToID("_Scale");
    private static readonly int Offset = Shader.PropertyToID("_Offset");
    
    [SerializeField] private Material noise;
    [SerializeField] private int width = 512;
    [SerializeField] private int height = 512;
    [SerializeField] private Vector2 offset;
    [SerializeField] private float scale = 20;
    [SerializeField, Range(1, 10)] private int octaves = 2;
    [SerializeField, Range(0, 1)] private float lacunarity = 0.5f;
    [SerializeField, Range(0, 1)] private float persistence = 0.5f;
    [Space]
    [SerializeField] private Texture2D debug;
    private static readonly int Amplitude = Shader.PropertyToID("_Amplitude");
    private static readonly int Frequency = Shader.PropertyToID("_Frequency");

    private void OnValidate()
    {
        noise.SetVector(Offset, offset);
        noise.SetFloat(Scale, scale);
        debug = RenderMaterialOctaves(width, height, noise, octaves, lacunarity, persistence);
    }

    public Texture2D RenderMaterial(int width, int height, Material material)
    {
        RenderTexture temp = RenderTexture.GetTemporary(width, height);
        Graphics.Blit(temp, temp, material);
        
        Texture2D output = new Texture2D(width, height);
        RenderTexture active = RenderTexture.active;
        RenderTexture.active = temp;
        
        output.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        output.Apply();
        
        RenderTexture.active = active;
        RenderTexture.ReleaseTemporary(temp);
        return output;
    }

    public Texture2D RenderMaterialOctaves(int width, int height, Material material, int octaves, float lacunarity,
        float persistence)
    {
        RenderTexture temp = new RenderTexture(width, height, 0);
        // CommandBuffer cmd = CommandBufferPool.Get();
        // cmd.Blit(temp, temp, material);
        float amplitude = 1;
        float frequency = 1;
        RenderTexture active = RenderTexture.active;
        RenderTexture.active = temp;
        for (int i = 0; i < octaves; i++)
        {
            material.SetFloat(Frequency, frequency);
            material.SetFloat(Amplitude, amplitude);
            // Graphics.ExecuteCommandBuffer(cmd);
            Graphics.Blit(temp, temp, material);
            frequency /= lacunarity;
            amplitude *= persistence;
        }
        
        Texture2D output = new Texture2D(width, height);
        
        output.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        output.Apply();
        
        RenderTexture.active = active;
        temp.Release();
        return output;
    }
}