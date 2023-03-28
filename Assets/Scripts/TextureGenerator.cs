using JetBrains.Annotations;
using UnityEngine;

public static class TextureGenerator
{
    public static Texture2D RenderComputeShader(int width, int height, ComputeShader compute)
    {
        RenderTexture temp = new RenderTexture(width, height, 0, RenderTextureFormat.R16);
        temp.enableRandomWrite = true;
        temp.Create();
        int kernelHandle = compute.FindKernel("CSMain");
        compute.SetTexture(kernelHandle, "Result", temp);
        compute.Dispatch(kernelHandle, width / 8, height / 8, 1);
        
        RenderTexture active = RenderTexture.active;
        RenderTexture.active = temp;
        
        Texture2D output = new Texture2D(width, height);
        
        output.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        output.Apply();
        
        RenderTexture.active = active;
        temp.Release();
        return output;
    }

    public static Texture2D RenderComputeShader(int width, int height, ComputeShader compute, float brightness, int octaves, float lacunarity, float persistence)
    {
        RenderTexture temp = new RenderTexture(width, height, 0, RenderTextureFormat.R16);
        temp.enableRandomWrite = true;
        temp.Create();
        RenderTexture temp2 = new RenderTexture(width, height, 0, RenderTextureFormat.R16);
        temp2.enableRandomWrite = true;
        temp2.Create();
        bool switched = false;
        int kernelHandle = compute.FindKernel("CSMain");
        float frequency = 1;
        float amplitude = 1;
        
        for (int i = 0; i < octaves; i++)
        {
            switched = !switched;
            compute.SetFloat("_Amplitude", amplitude * brightness);
            compute.SetFloat("_Frequency", frequency);
            compute.SetTexture(kernelHandle, "Input", switched ? temp2 : temp);
            compute.SetTexture(kernelHandle, "Result", switched ? temp : temp2);
            int groups = Mathf.CeilToInt(width / 8.0f);
            compute.Dispatch(kernelHandle, groups, groups, 1);
            frequency /= lacunarity;
            amplitude *= persistence;
        }
        
        RenderTexture active = RenderTexture.active;
        RenderTexture.active = switched ? temp : temp2;
        
        Texture2D output = new Texture2D(width, height, TextureFormat.ARGB32, false, true);
        
        output.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        output.Apply();
        
        RenderTexture.active = active;
        temp.Release();
        temp2.Release();
        return output;
    }

    public static Texture2D RenderSDF(ComputeShader compute, Texture2D input, int iterations, float amount = 0.125f, float amountIn = 0.05f)
    {
        int width = input.width;
        RenderTexture temp = new RenderTexture(width, width, 0, RenderTextureFormat.R16);
        temp.enableRandomWrite = true;
        temp.Create();
        RenderTexture temp2 = new RenderTexture(width, width, 0, RenderTextureFormat.R16);
        temp2.enableRandomWrite = true;
        temp2.Create();

        Graphics.Blit(input, temp);
        Graphics.Blit(input, temp2);

        int kernelHandle = compute.FindKernel("CSMain");
        int groups = Mathf.CeilToInt(width / 8.0f);

        bool switched = true;
        for (int i = 0; i < iterations; i++)
        {
            switched = !switched;
            compute.SetFloat("_Amount", amount);
            compute.SetFloat("_AmountIn", amountIn);
            compute.SetInt("_Width", width);
            compute.SetTexture(kernelHandle, "Input", switched ? temp2 : temp);
            compute.SetTexture(kernelHandle, "Result", switched ? temp : temp2);
            compute.Dispatch(kernelHandle, groups, groups, 1);
        }

        RenderTexture active = RenderTexture.active;
        RenderTexture.active = switched ? temp : temp2;
        //RenderTexture.active = temp;

        Texture2D output = new Texture2D(width, width, TextureFormat.R16, false, true);

        output.ReadPixels(new Rect(0, 0, width, width), 0, 0);
        output.Apply();

        RenderTexture.active = active;
        temp.Release();
        temp2.Release();
        return output;
    }
}