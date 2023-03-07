using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR;

public class GeneralIndirectInstancer : MonoBehaviour
{
    [SerializeField] private ComputeShader cullShader;
    [SerializeField] private Mesh mesh;
    [SerializeField] private Material material;
    [SerializeField] private float occlusionCullingThreshold = 0.1f;

    private ComputeBuffer argsBuffer;
    private ComputeBuffer unculledBuffer;
    private ComputeBuffer culledBuffer;

    private Transform cameraTransform;
    private Camera cam;

    private int size;

    private void OnValidate()
    {
        cullShader.SetFloat("_OcclusionCullingThreshold", occlusionCullingThreshold);
    }

    public void Setup(Matrix4x4[] transforms, Camera cam)
    {
        this.cam = cam;
        this.cameraTransform = cam.transform;
        this.material = new Material(material);
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

        if (XRSettings.enabled)
        {
            cullShader.EnableKeyword("USING_VR");
        }
        else
        {
            cullShader.DisableKeyword("USING_VR");
        }
    }

    private void InitialiseBuffer(Matrix4x4[] transforms)
    {
        MeshProperties[] props = new MeshProperties[transforms.Length];
        print(transforms[0]);
        for (int i = 0; i < transforms.Length; i++)
        {
            Matrix4x4 mat = transforms[i];
            props[i] = new MeshProperties() { PositionMatrix = mat, InversePositionMatrix = mat.inverse };
        }
        
        // cullShader.SetVector("_CameraForward", cameraTransform.forward);
        // cullShader.SetVector("_CameraPosition", cameraTransform.position);
        // cullShader.SetMatrix("_Projection", (XRSettings.enabled ? cam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left) : cam.projectionMatrix) * cam.worldToCameraMatrix);
        cullShader.SetFloat("_OcclusionCullingThreshold", occlusionCullingThreshold);
        unculledBuffer = new ComputeBuffer(props.Length, MeshProperties.Size(), ComputeBufferType.Default, ComputeBufferMode.Immutable);
        culledBuffer = new ComputeBuffer(props.Length, MeshProperties.Size(), ComputeBufferType.Append, ComputeBufferMode.Immutable);
        unculledBuffer.SetData(props);
        this.material.SetBuffer("VisibleShaderDataBuffer", culledBuffer);
    }

    private void Update()
    {
        if (unculledBuffer == null)
        {
            return;
        }
        cullShader.SetBuffer(0, "Input", unculledBuffer);
        culledBuffer.SetCounterValue(0);
        cullShader.SetBuffer(0, "Result", culledBuffer);
        cullShader.Dispatch(0, size / 8, size / 8, 1);
        ComputeBuffer.CopyCount(culledBuffer, argsBuffer, sizeof(uint));
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, new Bounds(Vector3.zero, new Vector3(5000.0f, 5000.0f, 5000.0f)), argsBuffer);
    }

    private void OnDestroy()
    {
        if (argsBuffer != null)
        {
            argsBuffer.Dispose();
            unculledBuffer.Dispose();
        }
    }

    internal void Initialise()
    {
        throw new NotImplementedException();
    }
}
