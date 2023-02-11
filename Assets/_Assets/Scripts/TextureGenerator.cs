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
    
    [SerializeField] private ComputeShader noiseCompute;
    [SerializeField] private int width = 512;
    [SerializeField] private int height = 512;
    [SerializeField] private Vector2 offset;
    [SerializeField] private float scale = 20;
    [SerializeField, Range(0, 1)] private float brightness = 1;
    [SerializeField, Range(1, 10)] private int octaves = 2;
    [SerializeField, Range(0, 1)] private float lacunarity = 0.5f;
    [SerializeField, Range(0, 1)] private float persistence = 0.5f;
    [Space]
    [SerializeField] private Texture2D debug;
    
    private static readonly int Amplitude = Shader.PropertyToID("_Amplitude");
    private static readonly int Frequency = Shader.PropertyToID("_Frequency");

    private void OnValidate()
    {
        noiseCompute.SetVector(Offset, offset);
        noiseCompute.SetFloat(Scale, scale);
        debug = RenderComputeShader(width, height, noiseCompute, brightness, octaves, lacunarity, persistence);
    }

    public Texture2D RenderComputeShader(int width, int height, ComputeShader compute)
    {
        return RenderComputeShader(width, height, compute, 1, 1, 1, 1);
    }

    public Texture2D RenderComputeShader(int width, int height, ComputeShader compute, float brightness, int octaves, float lacunarity, float persistence)
    {
        RenderTexture temp = new RenderTexture(width, height, 0);
        temp.enableRandomWrite = true;
        temp.Create();
        RenderTexture temp2 = new RenderTexture(width, height, 0);
        temp2.enableRandomWrite = true;
        temp2.Create();
        bool switched = false;
        int kernelHandle = compute.FindKernel("CSMain");
        float frequency = 1;
        float amplitude = 1;
        
        for (int i = 0; i < octaves; i++)
        {
            switched = !switched;
            compute.SetFloat("_Frequency", frequency);
            compute.SetFloat("_Amplitude", amplitude * brightness);
            compute.SetTexture(kernelHandle, "Input", switched ? temp2 : temp);
            compute.SetTexture(kernelHandle, "Result", switched ? temp : temp2);
            compute.Dispatch(kernelHandle, width / 8, height / 8, 1);
            frequency /= lacunarity;
            amplitude *= persistence;
        }
        
        RenderTexture active = RenderTexture.active;
        RenderTexture.active = switched ? temp : temp2;
        
        Texture2D output = new Texture2D(width, height);
        
        output.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        output.Apply();
        
        RenderTexture.active = active;
        temp.Release();
        temp2.Release();
        return output;
    }

    private void OnGUI()
    {
        GUI.DrawTexture(new Rect(10, 10, 256, 256), debug, ScaleMode.ScaleToFit, false, 10.0F);
    }
}