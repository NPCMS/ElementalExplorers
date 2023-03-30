using System;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.XR;

public class GeneralIndirectInstancer : MonoBehaviour
{
    [SerializeField] private ComputeShader cullShader;
    [SerializeField] private ComputeShader instanceShader;
    [SerializeField] private Mesh mesh;
    [SerializeField] private Material material;
    [SerializeField] private float occlusionCullingThreshold = 0.1f;
    [SerializeField] private float frustrumCullingThreshold = 0.05f;
    [SerializeField] private float distanceThreshold = 0.95f;

    private ComputeBuffer argsBuffer;
    private ComputeBuffer vrArgsBuffer;
    private ComputeBuffer instancedBuffer;
    private ComputeBuffer unculledBuffer;
    private ComputeBuffer culledBuffer;

    private int size;
    private bool vr;

    private void OnValidate()
    {
        InitialiseVariables();
    }

    public void Setup(Matrix4x4[] transforms)
    {
        this.material = material;
        uint[] args = new uint[5];
        args[0] = (uint)mesh.GetIndexCount(0);
        args[1] = (uint)transforms.Length;
        args[2] = (uint)mesh.GetIndexStart(0);
        args[3] = (uint)mesh.GetBaseVertex(0);
        argsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments, ComputeBufferMode.Immutable);
        argsBuffer.SetData(args);
        size = (int)Mathf.Sqrt(transforms.Length);
        cullShader.SetInt("_Size", size);
        InitialiseBuffer(transforms);
        InitialiseVariables();
    }

    private void InitialiseVariables()
    {
        cullShader.SetFloat("_OcclusionCullingThreshold", occlusionCullingThreshold);
        cullShader.SetFloat("_FrustrumCullingThreshold", frustrumCullingThreshold);
        cullShader.SetFloat("_DistanceThreshold", distanceThreshold);
    }

    private void InitialiseBuffer(Matrix4x4[] transforms)
    {
        MeshProperties[] props = new MeshProperties[transforms.Length];
        for (int i = 0; i < transforms.Length; i++)
        {
            Matrix4x4 mat = transforms[i];
            props[i] = new MeshProperties() { PositionMatrix = mat, InversePositionMatrix = mat.inverse };
        }
        
        unculledBuffer = new ComputeBuffer(props.Length, MeshProperties.Size(), ComputeBufferType.Default, ComputeBufferMode.Immutable);
        culledBuffer = new ComputeBuffer(props.Length, MeshProperties.Size(), ComputeBufferType.Append, ComputeBufferMode.Immutable);
        unculledBuffer.SetData(props);
        vr = XRSettings.enabled;
        if (vr)
        {
            vrArgsBuffer = new ComputeBuffer(1, 3 * sizeof(uint), ComputeBufferType.IndirectArguments, ComputeBufferMode.Immutable);
            vrArgsBuffer.SetData(new uint[] { (uint)transforms.Length, 1, 1 });
            instancedBuffer = new ComputeBuffer(transforms.Length, MeshProperties.Size(),
                ComputeBufferType.Counter, ComputeBufferMode.Immutable);
        }
        this.material.SetBuffer("VisibleShaderDataBuffer", culledBuffer);
    }

    private void Update()
    {
        if (unculledBuffer == null)
        {
            return;
        }
        Profiler.BeginSample("GPU Instance Culling");
        cullShader.SetBuffer(0, "Input", unculledBuffer);
        culledBuffer.SetCounterValue(0);
        cullShader.SetBuffer(0, "Result", culledBuffer);
        cullShader.SetTextureFromGlobal(0, "_CameraDepthTexture", "_CameraDepthTexture");
        cullShader.Dispatch(0, size / 8, size / 8, 1);
        Profiler.EndSample();
        
        if (vr)
        {
            Profiler.BeginSample("GPU Instance VR Instancing");
            ComputeBuffer.CopyCount(culledBuffer, vrArgsBuffer, 0);
            instancedBuffer.SetCounterValue(0);
            instanceShader.SetBuffer(0, "Input", culledBuffer);
            instanceShader.SetBuffer(0, "Result", instancedBuffer);
            instanceShader.DispatchIndirect(0, vrArgsBuffer);
            ComputeBuffer.CopyCount(instancedBuffer, argsBuffer, sizeof(uint));
            Profiler.EndSample();
        }
        else
        {
            ComputeBuffer.CopyCount(culledBuffer, argsBuffer, sizeof(uint));
        }
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, new Bounds(Vector3.zero, new Vector3(5000.0f, 5000.0f, 5000.0f)), argsBuffer, 0, null, ShadowCastingMode.Off, true, 0, null, LightProbeUsage.Off);
    }

    private void OnDestroy()
    {
        if (argsBuffer != null)
        {
            argsBuffer.Dispose();
            unculledBuffer.Dispose();
            culledBuffer.Dispose();
            if (vr)
            {
                vrArgsBuffer.Dispose();
                instancedBuffer.Dispose();
            }
        }
    }

    internal void Initialise()
    {
        throw new NotImplementedException();
    }
}
