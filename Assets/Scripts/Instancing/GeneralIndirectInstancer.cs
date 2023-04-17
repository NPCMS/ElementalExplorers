using System;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.XR;

public class GeneralIndirectInstancer : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs("cullShader")] private ComputeShader cull;
    [SerializeField, FormerlySerializedAs("instanceShader")] private ComputeShader instance;
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

    private int instanceWidth;
    private bool vr;

    private Camera cam;

    private ComputeShader cullShader;
    private ComputeShader instanceShader;

    private void OnValidate()
    {
        if (cullShader == null)
        {
            return;
        }
        InitialiseVariables();
    }

    public void Setup(Matrix4x4[] transforms)
    {
        cullShader = Instantiate(cull);
        instanceShader = Instantiate(instance);
        uint[] args = new uint[5];
        args[0] = (uint)mesh.GetIndexCount(0);
        args[1] = (uint)transforms.Length;
        args[2] = (uint)mesh.GetIndexStart(0);
        args[3] = (uint)mesh.GetBaseVertex(0);
        argsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments, ComputeBufferMode.Immutable);
        argsBuffer.SetData(args);
        InitialiseBuffer(transforms);
        InitialiseVariables();
        int length = transforms.Length;
        cullShader.SetInt("_BufferLength", length);
        instanceWidth = (int)(Mathf.Sqrt(length)) + 1;
        cullShader.SetInt("_Size", instanceWidth);
        cullShader.SetBuffer(0, "Input", unculledBuffer);
        cullShader.SetBuffer(0, "Result", culledBuffer);
        instanceShader.SetBuffer(0, "Input", culledBuffer);
        instanceShader.SetBuffer(0, "Result", instancedBuffer);
        this.material.SetBuffer("VisibleShaderDataBuffer", culledBuffer);
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
    }

    private void Update()
    {
        if (cam == null)
        {
            // Look for the only active camera from all cameras
            foreach (var c in Camera.allCameras)
            {
                if (c.isActiveAndEnabled)
                {
                    cam = c;
                    break;
                }
            }
        }

            if (unculledBuffer == null)
        {
            return;
        }
        Profiler.BeginSample("GPU Instance Culling");
        culledBuffer.SetCounterValue(0);
        if (Camera.current != cam)
        {
            int group = instanceWidth / 8 + 1;
            cullShader.Dispatch(0, group, group, 1);
        }
        Profiler.EndSample();
        
        if (vr)
        {
            Profiler.BeginSample("GPU Instance VR Instancing");
            ComputeBuffer.CopyCount(culledBuffer, vrArgsBuffer, 0);
            instancedBuffer.SetCounterValue(0);
            instanceShader.DispatchIndirect(0, vrArgsBuffer);
            ComputeBuffer.CopyCount(instancedBuffer, argsBuffer, sizeof(uint));
            Profiler.EndSample();
        }
        else
        {
            ComputeBuffer.CopyCount(culledBuffer, argsBuffer, sizeof(uint));
        }
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, new Bounds(cam.transform.position, new Vector3(distanceThreshold, distanceThreshold, distanceThreshold)), argsBuffer, 0, null, ShadowCastingMode.Off, true, 0, null, LightProbeUsage.Off);
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
}
